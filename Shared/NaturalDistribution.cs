using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.API.Features.Lockers;
using Exiled.API.Features.Pickups;
using Exiled.CustomItems.API.Features;
using Exiled.Events.EventArgs.Player;
using PlayerRoles;
using UnityEngine;

namespace Brainless.Internal
{
    internal sealed class NaturalContent
    {
        internal uint Id;
        internal string Name;
        internal ItemType Body;
        internal Rarity Rarity;
        internal int RequiredSlots = 1;
        internal Func<bool> CanIssue;
        internal Func<Player, string> Give;
        internal Func<Player, Item, string> AdoptItem;
        internal Func<Pickup, Chamber, string> AdoptPickup;
        internal Func<Vector3, Pickup> Spawn;
    }

    internal sealed class NaturalDistribution : IDisposable
    {
        private readonly string label;
        private readonly Lifetime lifetime;
        private readonly Func<DistributionConfig> config;
        private readonly Func<IEnumerable<NaturalContent>> content;
        private readonly Func<int> extraCount;
        private readonly Func<bool> debug;
        private readonly System.Random random = new System.Random();
        private readonly DistributionLedger ledger = new DistributionLedger();
        private bool disposed;
        internal NaturalDistribution(string label, Lifetime lifetime, Func<DistributionConfig> config, Func<IEnumerable<NaturalContent>> content, Func<int> extraCount, Func<bool> debug)
        {
            this.label = label;
            this.lifetime = lifetime;
            this.config = config;
            this.content = content;
            this.extraCount = extraCount;
            this.debug = debug;
            Exiled.Events.Handlers.Player.Spawned += Spawned;
        }

        internal void Reset() => ledger.Reset();
        internal void RoundStarted()
        {
            // Capture only the round's initial map pickups. Later player drops,
            // deaths and SCP-914 products cannot become new loot-roll targets.
            if (!ledger.BeginMap())
                return;
            var slots = CaptureMap();
            lifetime.Later(config().MapDelaySeconds, () => RunMap(slots));
        }

        private void Spawned(SpawnedEventArgs ev)
        {
            var settings = config();
            if (disposed || !settings.Enabled || !settings.PlayerLoadouts || !settings.AllowedSpawnReasons.Contains(ev.Reason) || (ev.SpawnFlags & RoleSpawnFlags.AssignInventory) == 0 || !ev.Player.IsHuman)
                return;
            Player player = ev.Player;
            var life = player.Role.Base;
            if (!ledger.BeginLife(life))
                return;
            lifetime.Later(settings.PlayerDelaySeconds, () => GiveSpawn(player, life));
        }

        private void GiveSpawn(Player player, PlayerRoles.PlayerRoleBase life)
        {
            var settings = config();
            if (disposed || !Round.IsStarted || !settings.Enabled || !settings.PlayerLoadouts || !player.IsConnected || !player.IsAlive || !player.IsHuman || player.IsCuffed || player.Role.Base != life || !ledger.CanGive(player.UserId, settings.MaxPlayerGrants, settings.MaxGrantsPerPlayer) || (settings.SkipPlayersWithCustomItems && player.Items.Any(i => IsCustom(i.Serial))))
                return;
            var providers = content().ToDictionary(c => c.Id);
            var eligible = settings.PlayerRules.Where(r => r.Roles.Count == 0 || r.Roles.Contains(player.Role.Type));
            PlayerLootRule rule;
            NaturalContent selected;
            if (settings.UseRarity)
                selected = RollRarity(settings, eligible, r => r.CustomId, r => r.ChancePercent, r => r.Weight, providers, (r, c) => r.ReplaceTypes.Count == 0 || player.Items.Any(i => i.Type == c.Body && r.ReplaceTypes.Contains(i.Type) && !IsCustom(i.Serial)), settings.StartingGearRarityScale, out rule);
            else
            {
                rule = DistributionRules.ChoosePercent(eligible, r => r.ChancePercent.Value, random.NextDouble() * 100);
                selected = rule != null && providers.TryGetValue(rule.CustomId, out var found) ? found : null;
            }

            if (rule == null || selected == null || !selected.CanIssue())
                return;
            if (player.Items.Count + selected.RequiredSlots - (rule.ReplaceTypes.Count > 0 ? 1 : 0) > 8)
                return;
            Item original = rule.ReplaceTypes.Count == 0 ? null : player.Items.FirstOrDefault(i => i.Type == selected.Body && rule.ReplaceTypes.Contains(i.Type) && !IsCustom(i.Serial));
            if (rule.ReplaceTypes.Count > 0 && original == null)
                return;
            string failure = null;
            bool granted = ledger.TryGive(player.UserId, settings.MaxPlayerGrants, settings.MaxGrantsPerPlayer, () =>
            {
                failure = original == null ? selected.Give(player) : selected.AdoptItem(player, original);
                return failure == null;
            });
            if (granted)
                RarityLedger.Take(Scope, RoundKey, selected.Rarity);
            if (granted && settings.ShowSpawnHint)
                player.ShowHint("<b>Spawn gear: " + selected.Name + "</b> (" + RarityRules.Tag(selected.Rarity) + ")", 3);
            if (debug())
                Log.Info("[" + label + " distribution] Spawn gear " + selected.Id + (granted ? " issued." : " skipped: " + failure));
        }

        private void RunMap(List<MapSlot> slots)
        {
            var settings = config();
            if (disposed || !Round.IsStarted || !settings.Enabled)
                return;
            var providers = content().ToDictionary(c => c.Id);
            DistributionRules.Shuffle(slots, random);
            if (settings.MapReplacements)
                foreach (var slot in slots)
                {
                    if (ledger.MapAwards >= settings.MaxMapReplacements)
                        break;
                    if (!slot.Available || !ledger.CanPlace(slot.Room, settings.MaxMapItemsPerRoom))
                        continue;
                    var selected = Choose(settings, settings.MapRules.Where(r => Matches(r, slot)), providers, c => c.Body == slot.Pickup.Type);
                    if (selected == null || selected.Body != slot.Pickup.Type || !selected.CanIssue())
                        continue;
                    string failure = null;
                    if (ledger.TryPlace(slot.Room, settings.MaxMapItemsPerRoom, settings.MaxMapReplacements, false, () =>
                    {
                        failure = selected.AdoptPickup(slot.Pickup, slot.Chamber);
                        return failure == null;
                    }))
                        RarityLedger.Take(Scope, RoundKey, selected.Rarity);
                    if (debug() && failure != null)
                        Log.Info("[" + label + " distribution] Map conversion skipped: " + failure);
                }

            // The old WorldLoot switch now scatters its optional extra drops at
            // real loose-loot anchors instead of one hard-coded armory origin.
            int extras = extraCount();
            DistributionRules.Shuffle(slots, random);
            foreach (var slot in slots)
            {
                if (ledger.ExtraAwards >= extras)
                    break;
                if (!slot.Available || slot.Chamber != null || !slot.Pickup.IsSpawned || slot.Pickup.IsLocked || !ledger.CanPlace(slot.Room, settings.MaxMapItemsPerRoom))
                    continue;
                var selected = Choose(settings, settings.MapRules.Where(r => Matches(r, slot)), providers, c => true);
                if (selected == null || !selected.CanIssue())
                    continue;
                if (ledger.TryPlace(slot.Room, settings.MaxMapItemsPerRoom, extras, true, () => selected.Spawn(slot.Pickup.Position + Vector3.up * .12f) != null))
                    RarityLedger.Take(Scope, RoundKey, selected.Rarity);
            }

            if (debug())
                Log.Info($"[{label} distribution] {ledger.MapAwards} map replacements, {ledger.ExtraAwards} extra loot bundles; native supply caps remain active.");
        }

        // Items and Weapons share one set of per-round rarity counts.
        internal const string Scope = "items";
        internal static int RoundKey => Round.UptimeRounds;

        private NaturalContent Choose(DistributionConfig settings, IEnumerable<MapLootRule> rules, Dictionary<uint, NaturalContent> providers, Func<NaturalContent, bool> fits)
        {
            if (settings.UseRarity)
                return RollRarity(settings, rules, r => r.CustomId, r => r.ChancePercent, r => r.Weight, providers, (r, c) => fits(c), 1, out MapLootRule _);
            var rule = DistributionRules.ChoosePercent(rules, r => r.ChancePercent.Value, random.NextDouble() * 100);
            return rule != null && providers.TryGetValue(rule.CustomId, out var found) ? found : null;
        }

        // Rarity mode: roll a tier, then pick among that tier's matching items by weight.
        // No fall-through: a tier with nothing here (or at its round limit) gives nothing.
        private NaturalContent RollRarity<TRule>(DistributionConfig settings, IEnumerable<TRule> rules, Func<TRule, uint> id, Func<TRule, double?> chance, Func<TRule, int> weight, Dictionary<uint, NaturalContent> providers, Func<TRule, NaturalContent, bool> fits, double scale, out TRule chosen)
            where TRule : class
        {
            chosen = null;
            var options = rules.Select(r => new Option<TRule> { Rule = r, Content = providers.TryGetValue(id(r), out var c) ? c : null, Weight = weight(r) }).Where(o => o.Content != null && chance(o.Rule) != 0 && o.Weight > 0 && fits(o.Rule, o.Content)).ToList();
            if (options.Count == 0)
                return null;
            var tier = RarityRules.RollTier(settings.Rarity, random.NextDouble() * 100, scale: scale);
            if (!tier.HasValue)
                return null;
            var picked = RarityRules.Pick(options, o => o.Content.Rarity, o => o.Weight, tier.Value, t => RarityLedger.Open(Scope, RoundKey, t, settings.Rarity.Get(t), random), o => o.Content.CanIssue(), random.NextDouble, false);
            chosen = picked?.Rule;
            return picked?.Content;
        }

        private sealed class Option<TRule>
        {
            internal TRule Rule;
            internal NaturalContent Content;
            internal int Weight;
        }

        private static bool Matches(MapLootRule rule, MapSlot slot) => rule.SourceTypes.Contains(slot.Pickup.Type) && (rule.Zones.Count == 0 || rule.Zones.Contains(slot.Room.Zone));
        private static bool IsCustom(int serial) => CustomItem.Registered.Any(c => c.TrackedSerials.Contains((ushort)serial));
        private static List<MapSlot> CaptureMap()
        {
            var stored = new Dictionary<Pickup, Chamber>();
            foreach (var locker in Locker.List.ToArray())
                foreach (var chamber in locker.Chambers)
                    foreach (var pickup in chamber.Base.Content.Concat(chamber.Base.ToBeSpawned).Where(p => p != null).Select(Pickup.Get))
                        if (pickup != null)
                            stored[pickup] = chamber;
            return Pickup.List.ToArray().Union(stored.Keys).Where(p => p?.Base != null).Select(p =>
            {
                stored.TryGetValue(p, out var chamber);
                return new MapSlot
                {
                    Pickup = p,
                    Chamber = chamber,
                    Room = chamber?.Locker.Room ?? Room.Get(p.Position)
                };
            }).Where(s => s.Room != null && s.Room.Type != RoomType.Pocket && s.Available).ToList();
        }

        // A paired gas filter stays in the same native locker chamber, including
        // delayed-spawn chambers. Original pickups never move or lose their locks.
        internal static void PlaceCompanion(Pickup companion, Pickup original, Chamber chamber)
        {
            if (chamber == null)
                return;
            companion.UnSpawn();
            companion.Transform.SetParent(original.Transform.parent);
            companion.Position = original.Position + Vector3.right * .08f;
            companion.Rotation = original.Rotation;
            companion.IsLocked = original.IsLocked;
            if (companion.Rigidbody != null && original.Rigidbody != null)
                companion.Rigidbody.isKinematic = original.Rigidbody.isKinematic;
            chamber.Base.Content.Add(companion.Base);
            if (chamber.Base.ToBeSpawned.Contains(original.Base) && !chamber.Base.WasEverOpened)
                chamber.Base.ToBeSpawned.Add(companion.Base);
            else
                companion.Spawn();
        }

        internal static void RemoveCompanion(Pickup companion, Chamber chamber)
        {
            if (companion?.Base == null)
                return;
            chamber?.Base.Content.Remove(companion.Base);
            chamber?.Base.ToBeSpawned.Remove(companion.Base);
            companion.Destroy();
        }

        public void Dispose()
        {
            disposed = true;
            Exiled.Events.Handlers.Player.Spawned -= Spawned;
            ledger.Reset();
        }

        private sealed class MapSlot
        {
            internal Pickup Pickup;
            internal Chamber Chamber;
            internal Room Room;
            internal bool Available => Pickup?.Base != null && !IsCustom(Pickup.Serial) && (Pickup.PreviousOwner == null || Pickup.PreviousOwner.IsHost);
        }
    }
}
