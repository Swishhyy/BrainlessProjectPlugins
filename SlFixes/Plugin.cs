using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Brainless.Internal;
using Brainless.Internal.Updates;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.API.Features.Pickups;
using Exiled.CustomItems.API.Features;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Scp914;
using Exiled.Events.EventArgs.Server;
using PlayerRoles;
using Scp914;
using UnityEngine;
using P = Exiled.Events.Handlers.Player;
using S = Exiled.Events.Handlers.Server;
using U = Exiled.Events.Handlers.Scp914;

namespace Brainless.SlFixes
{
    public sealed partial class Plugin : Plugin<Config>
    {
        public override string Name => "Project Brainless SCP SL Fixes";
        public override string Prefix => "project_brainless_sl_fixes";
        public override string Author => "Project Brainless";
        public override Version Version => new Version(0, 5, 1);
        public override Version RequiredExiledVersion => new Version(9, 15, 3);
        public override PluginPriority Priority => PluginPriority.Last;
        public static Plugin Instance { get; private set; }

        private readonly Lifetime lifetime = new Lifetime();
        private readonly System.Random random = new System.Random();
        private readonly Dictionary<uint, int> crafted = new Dictionary<uint, int>();
        private readonly Dictionary<string, int> xpAwards = new Dictionary<string, int>();
        private readonly Dictionary<Player, int> lives = new Dictionary<Player, int>();
        private readonly Dictionary<string, string> participants = new Dictionary<string, string>();
        private readonly Dictionary<int, float> processed = new Dictionary<int, float>();
        private IDisposable updates;
        private string roundId;
        private int nextLife;
        private float started;
        public override void OnEnabled()
        {
            Config.Validate();
            Instance = this;
            Reset();
            S.RoundStarted += RoundStarted;
            S.WaitingForPlayers += Reset;
            S.RestartingRound += Reset;
            S.RoundEnded += RoundEnded;
            S.RoundEnded += EndRoundFriendlyFire;
            P.Spawned += Spawned;
            P.Left += Left;
            P.Died += Died;
            P.Escaped += Escaped;
            P.Escaping.Subscribe(CaptiveEscaping, 1000);
            U.UpgradingPlayer.Subscribe(UpgradePlayer, -500);
            U.UpgradingPickup.Subscribe(UpgradePickup, -500);
            U.UpgradingInventoryItem.Subscribe(UpgradeInventory, -500);
            updates = PluginUpdates.Start(GetType().Assembly, "SlFixes", Config.AutoUpdate);
            if (Config.EnableXp && !CoreBridge.IsReady)
                Log.Warn("[Project Brainless Fixes] Core stats are unavailable; SCP-914 additions still work. Install/enable Brainless.Core for persistent XP and Discord.");
            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            S.RoundStarted -= RoundStarted;
            S.WaitingForPlayers -= Reset;
            S.RestartingRound -= Reset;
            S.RoundEnded -= RoundEnded;
            S.RoundEnded -= EndRoundFriendlyFire;
            RestoreFriendlyFire();
            P.Spawned -= Spawned;
            P.Left -= Left;
            P.Died -= Died;
            P.Escaped -= Escaped;
            P.Escaping -= CaptiveEscaping;
            U.UpgradingPlayer -= UpgradePlayer;
            U.UpgradingPickup -= UpgradePickup;
            U.UpgradingInventoryItem -= UpgradeInventory;
            lifetime.Clear();
            updates?.Dispose();
            Instance = null;
            base.OnDisabled();
        }

        private void Reset()
        {
            RestoreFriendlyFire();
            lifetime.Clear();
            crafted.Clear();
            xpAwards.Clear();
            lives.Clear();
            participants.Clear();
            processed.Clear();
            roundId = Guid.NewGuid().ToString("N");
            nextLife = 0;
            started = Time.time;
            if (Config.IsEnabled)
                lifetime.Run(PrivacyCleanup());
        }

        private static bool Real(Player player) => player != null && player.IsConnected && !player.IsHost && !string.IsNullOrWhiteSpace(player.UserId);
        private void RoundStarted()
        {
            Reset();
            foreach (var player in Player.List.Where(Real).Where(p => p.IsAlive))
                Track(player);
        }

        private void Track(Player player)
        {
            if (!Trackable(player))
                return;
            lives[player] = ++nextLife;
            participants[player.UserId] = player.Nickname;
        }

        private void Spawned(SpawnedEventArgs ev)
        {
            if (Round.IsStarted && Real(ev.Player) && ev.Player.IsAlive)
                Track(ev.Player);
        }

        private void Left(LeftEventArgs ev)
        {
            lives.Remove(ev.Player);
            if (ev.Player.DoNotTrack || !CoreBridge.CanTrack(ev.Player.UserId))
                ForgetTracking(ev.Player);
        }

        private string Life(Player player)
        {
            if (!lives.TryGetValue(player, out int life))
            {
                Track(player);
                life = lives.TryGetValue(player, out int tracked) ? tracked : ++nextLife;
            }

            return life.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        private void Award(Player player, string action, string receipt, int limit = int.MaxValue)
        {
            if (!Config.EnableXp || !Round.IsStarted || !Trackable(player))
                return;
            string key = player.UserId + ":" + action;
            xpAwards.TryGetValue(key, out int before);
            Config.XpRewards.TryGetValue(action, out int xp);
            if (CoreBridge.Record(player.UserId, player.Nickname, action, before < limit ? xp : 0, roundId + ":" + receipt))
                xpAwards[key] = before + 1;
        }

        private void Died(DiedEventArgs ev) => Safe.Run(() =>
        {
            if (!Round.IsStarted || !Real(ev.Player))
                return;
            string death = "death:" + Life(ev.Player);
            Award(ev.Player, "death", death);
            if (!Real(ev.Attacker) || ev.Attacker == ev.Player)
                return;
            RoleTypeId attackRole = ev.DamageHandler.Base is PlayerStatsSystem.AttackerDamageHandler source ? source.Attacker.Role : ev.Attacker.Role.Type;
            if (!HitboxIdentity.IsEnemy(attackRole, ev.TargetOldRole))
                return;
            Award(ev.Attacker, Exiled.API.Extensions.RoleExtensions.GetTeam(ev.TargetOldRole) == Team.SCPs ? "kill_scp" : "kill_human", death);
        });
        private void Escaped(EscapedEventArgs ev) => Safe.Run(() => Award(ev.Player, "escape", "escape:" + Life(ev.Player)));
        private void RoundEnded(RoundEndedEventArgs ev) => Safe.Run(() =>
        {
            if (!Config.EnableXp)
                return;
            bool longEnough = Time.time - started >= Config.MinimumRoundSecondsForXp;
            foreach (var participant in participants)
            {
                if (!CoreBridge.CanTrack(participant.Key))
                    continue;
                Config.XpRewards.TryGetValue("round_played", out int xp);
                CoreBridge.Record(participant.Key, participant.Value, "round_played", longEnough ? xp : 0, roundId + ":round");
                var player = Player.Get(participant.Key);
                if (Real(player) && player.IsAlive)
                {
                    Config.XpRewards.TryGetValue("round_survived", out int survived);
                    CoreBridge.Record(participant.Key, participant.Value, "round_survived", longEnough ? survived : 0, roundId + ":survived");
                }
            }
        });
        private void UpgradePlayer(UpgradingPlayerEventArgs ev) => Safe.Run(() =>
        {
            if (!ev.IsAllowed || !Config.RoughTeleportsPlayers || ev.KnobSetting != Scp914KnobSetting.Rough || !Real(ev.Player) || !ev.Player.IsAlive || ev.Player.Role.Type == RoleTypeId.Scp079 || random.NextDouble() * 100 >= Config.RoughTeleportChance)
                return;
            if (!TryDestination(ev.Player, out Vector3 destination))
                return;
            ev.OutputPosition = destination;
            Player player = ev.Player;
            var role = player.Role.Base;
            string receipt = "teleport:" + Guid.NewGuid().ToString("N");
            lifetime.Later(.3f, () =>
            {
                if (!ev.IsAllowed || !Real(player) || !player.IsAlive || player.Role.Base != role || Vector3.Distance(player.Position, destination) > 3)
                    return;
                player.ShowHint("SCP-914 sent you somewhere else.", 4);
                Award(player, "914_teleport", receipt, Config.MaxTeleportXpAwardsPerPlayerPerRound);
            });
        });
        private bool TryDestination(Player player, out Vector3 destination)
        {
            destination = Vector3.zero;
            // After detonation every facility room is lethal; keep the normal 914 output.
            if (Warhead.IsDetonated || Config.RoughTeleportRooms == null || Config.RoughTeleportRooms.Count == 0)
                return false;
            var allowed = new HashSet<RoomType>(Config.RoughTeleportRooms);
            var rooms = Room.List.Where(r => r != player.CurrentRoom && allowed.Contains(r.Type) && (r.Zone == ZoneType.HeavyContainment || r.Zone == ZoneType.Entrance || (r.Zone == ZoneType.LightContainment && !Map.IsLczDecontaminated))).OrderBy(_ => random.Next()).ToArray();
            foreach (var room in rooms.Take(24))
                for (int attempt = 0; attempt < 6; attempt++)
                {
                    // Corridor centres are walkable; stay near the middle and probe from head
                    // height so the ray cannot start above the ceiling and land on the roof.
                    Vector3 candidate = room.Position + new Vector3((float)(random.NextDouble() * 3 - 1.5), 1.5f, (float)(random.NextDouble() * 3 - 1.5));
                    if (!Physics.Raycast(candidate, Vector3.down, out var hit, 3, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) || hit.normal.y < .8f)
                        continue;
                    Vector3 point = hit.point + Vector3.up * 1.1f;
                    // Clearance for the whole player plus headroom (the capsule reaches 2.3 m).
                    if (Room.Get(point) != room || Physics.CheckCapsule(hit.point + Vector3.up * .4f, hit.point + Vector3.up * 1.9f, .4f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                        continue;
                    destination = point;
                    return true;
                }

            destination = Vector3.zero;
            return false;
        }

        private bool IsCustom(int serial) => CustomItem.Registered.Any(c => c.TrackedSerials.Contains((ushort)serial));
        private CraftingRecipe Recipe(ItemType input, Scp914KnobSetting setting, int serial)
        {
            if (!Config.EnableCrafting || IsCustom(serial))
                return null;
            if (processed.TryGetValue(serial, out float last) && Time.time - last < .5f)
                return null;
            if (processed.Count > 10000)
                processed.Clear();
            processed[serial] = Time.time;
            return Config.Recipes.FirstOrDefault(r => r.Enabled && r.MaxPerRound > 0 && r.Source == input && r.Setting == setting && (!crafted.TryGetValue(r.CustomId, out int count) || count < r.MaxPerRound) && CustomItem.TryGet(r.CustomId, out var item) && item.Type == input && random.NextDouble() * 100 < r.Chance);
        }

        private void UpgradePickup(UpgradingPickupEventArgs ev) => Safe.Run(() =>
        {
            if (!ev.IsAllowed || ev.Pickup?.Base == null)
                return;
            var recipe = Recipe(ev.Pickup.Type, ev.KnobSetting, ev.Pickup.Serial);
            if (recipe == null || !CustomItem.TryGet(recipe.CustomId, out var item))
                return;
            var method = item.GetType().GetMethod("TryCraftPickup", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(Pickup), typeof(Vector3) }, null);
            if (method == null)
                return;
            string failure = (string)method.Invoke(item, new object[] { ev.Pickup, ev.OutputPosition });
            if (failure != null)
            {
                if (Config.Debug)
                    Log.Info("[PB 914] " + recipe.Name + " unavailable; native upgrade continues.");
                return;
            }

            ev.IsAllowed = false;
            Crafted(recipe, ev.Pickup.PreviousOwner, ev.Pickup.Serial);
        });
        private void UpgradeInventory(UpgradingInventoryItemEventArgs ev) => Safe.Run(() =>
        {
            if (!ev.IsAllowed || ev.Item == null || !Real(ev.Player))
                return;
            var recipe = Recipe(ev.Item.Type, ev.KnobSetting, ev.Item.Serial);
            if (recipe == null || !CustomItem.TryGet(recipe.CustomId, out var item))
                return;
            var method = item.GetType().GetMethod("TryCraftInventory", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(Player), typeof(Item) }, null);
            if (method == null)
                return;
            string failure = (string)method.Invoke(item, new object[] { ev.Player, ev.Item });
            if (failure != null)
            {
                if (Config.Debug)
                    ev.Player.ShowHint(recipe.Name + " unavailable; ordinary SCP-914 result applies.", 3);
                return;
            }

            ev.IsAllowed = false;
            ev.Player.ShowHint("SCP-914 produced " + item.Name + ".", 4);
            Crafted(recipe, ev.Player, ev.Item.Serial);
        });
        private void Crafted(CraftingRecipe recipe, Player player, int serial)
        {
            crafted.TryGetValue(recipe.CustomId, out int before);
            crafted[recipe.CustomId] = before + 1;
            Award(player, "craft", "craft:" + serial, Config.MaxCraftXpAwardsPerPlayerPerRound);
        }

        internal bool Reload(out string response)
        {
            try
            {
                var next = ConfigReload.Read(this);
                next.Validate();
                if (!next.IsEnabled)
                    throw new InvalidOperationException("Enable/disable requires a full server restart.");
                ConfigReload.Apply(Config, next);
                updates?.Dispose();
                updates = PluginUpdates.Start(GetType().Assembly, "SlFixes", Config.AutoUpdate);
                response = "Fixes tuning reloaded; XP/crafting ledgers were preserved.";
                return true;
            }
            catch (Exception failure)
            {
                response = "Config was not changed: " + failure.Message;
                return false;
            }
        }

        internal string Status() => "Core XP: " + (CoreBridge.IsReady ? "ready" : "unavailable; install Brainless.Core") + "; rough teleports: " + Config.RoughTeleportsPlayers + "; SCP-914 crafting: " + Config.EnableCrafting + ".\n" + string.Join("\n", Config.Recipes.Select(r => r.CustomId + " " + r.Name + ": " + r.Source + " on " + r.Setting + ", " + r.Chance + "%, crafted " + (crafted.TryGetValue(r.CustomId, out int n) ? n : 0) + "/" + r.MaxPerRound));
    }
}
