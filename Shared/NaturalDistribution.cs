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
            if (disposed || !Round.IsStarted || !settings.Enabled || !settings.PlayerLoadouts || !player.IsConnected || !player.IsAlive || !player.IsHuman || player.IsCuffed || player.Role.Base != life || !ledger.CanGive(player.UserId, settings.MaxPlayerGrants, settings.MaxGrantsPerPlayer) || (settings.SkipPlayersWithCustomItems && player.Items.Any(i => IsCustom(i.Serial))) || !DistributionRules.Roll(settings.PlayerChancePercent, random))
                return;
            var providers = content().ToDictionary(c => c.Id);
            var rule = Rules.ChooseEligible(settings.PlayerRules, r => providers.TryGetValue(r.CustomId, out var c) && c.CanIssue() && (r.Roles.Count == 0 || r.Roles.Contains(player.Role.Type)) && (r.ReplaceTypes.Count == 0 ? player.Items.Count + c.RequiredSlots <= 8 : player.Items.Count + c.RequiredSlots - 1 <= 8 && player.Items.Any(i => i.Type == c.Body && r.ReplaceTypes.Contains(i.Type) && !IsCustom(i.Serial))), r => r.Weight, random);
            if (rule == null)
                return;
            var selected = providers[rule.CustomId];
            Item original = rule.ReplaceTypes.Count == 0 ? null : player.Items.FirstOrDefault(i => i.Type == selected.Body && rule.ReplaceTypes.Contains(i.Type) && !IsCustom(i.Serial));
            if (rule.ReplaceTypes.Count > 0 && original == null)
                return;
            string failure = null;
            bool granted = ledger.TryGive(player.UserId, settings.MaxPlayerGrants, settings.MaxGrantsPerPlayer, () =>
            {
                failure = original == null ? selected.Give(player) : selected.AdoptItem(player, original);
                return failure == null;
            });
            if (granted && settings.ShowSpawnHint)
                player.ShowHint("<b>Spawn gear: " + selected.Name + "</b>", 3);
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
                    if (!slot.Available || !ledger.CanPlace(slot.Room, settings.MaxMapItemsPerRoom) || !DistributionRules.Roll(settings.MapChancePercent, random))
                        continue;
                    var rule = Rules.ChooseEligible(settings.MapRules, r => providers.TryGetValue(r.CustomId, out var c) && c.Body == slot.Pickup.Type && c.CanIssue() && Matches(r, slot), r => r.Weight, random);
                    if (rule == null)
                        continue;
                    var selected = providers[rule.CustomId];
                    string failure = null;
                    ledger.TryPlace(slot.Room, settings.MaxMapItemsPerRoom, settings.MaxMapReplacements, false, () =>
                    {
                        failure = selected.AdoptPickup(slot.Pickup, slot.Chamber);
                        return failure == null;
                    });
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
                var rule = Rules.ChooseEligible(settings.MapRules, r => providers.TryGetValue(r.CustomId, out var c) && c.CanIssue() && Matches(r, slot), r => r.Weight, random);
                if (rule == null)
                    continue;
                ledger.TryPlace(slot.Room, settings.MaxMapItemsPerRoom, extras, true, () => providers[rule.CustomId].Spawn(slot.Pickup.Position + Vector3.up * .12f) != null);
            }

            if (debug())
                Log.Info($"[{label} distribution] {ledger.MapAwards} map replacements, {ledger.ExtraAwards} extra loot bundles; native supply caps remain active.");
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
