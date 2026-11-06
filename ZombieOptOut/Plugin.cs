// Port of ZombieOptOut by Kadava (MIT, see LICENSE in this folder) to EXILED, with
// Brainless Roles profiles carried to whoever fills and Brainless Core XP.
using System;
using System.Collections.Generic;
using System.Linq;
using Brainless.Internal;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using PlayerRoles;
using UnityEngine;
using UserSettings.ServerSpecific;
using P = Exiled.Events.Handlers.Player;
using S = Exiled.Events.Handlers.Server;

namespace Brainless.ZombieOptOut
{
    public sealed class Plugin : Plugin<Config>
    {
        public override string Name => "BrainlessZombieOptOut";
        public override string Prefix => "brainless_zombie_opt_out";
        public override string Author => "Kadava; Brainless Project";
        public override Version Version => new Version(0, 1, 1);
        public override Version RequiredExiledVersion => new Version(9, 15, 3);
        public static Plugin Instance { get; private set; }

        private readonly Lifetime lifetime = new Lifetime();
        private readonly System.Random random = new System.Random();
        private readonly Dictionary<Player, Preference> preferences = new Dictionary<Player, Preference>();
        private ServerSpecificSettingBase[] settings;
        private IDisposable updates;

        // Opted-out zombie slots waiting for a spectator (.optin).
        private readonly Queue<ZombieSlot> zombieSlots = new Queue<ZombieSlot>();
        // Lost SCP slots (disconnect / pit suicide) waiting for .fill volunteers.
        private readonly Queue<ScpSlot> scpSlots = new Queue<ScpSlot>();
        private readonly List<Player> fillPool = new List<Player>();
        private readonly HashSet<string> offenders = new HashSet<string>();
        private readonly HashSet<string> spawnedWithCustomRole = new HashSet<string>();
        private ScpSlot activeScpSlot;
        private bool withinRoundStart;

        private sealed class Preference
        {
            internal bool OptOut, AutoFill;
        }

        private sealed class ZombieSlot
        {
            internal Vector3 Position;
            internal string Profile;
            internal string OptedOut;
            internal bool Filled;
        }

        private sealed class ScpSlot
        {
            internal RoleTypeId Role;
            internal float Health;
            internal string Profile, ProfileName;
        }

        public override void OnEnabled()
        {
            int id = Config.SettingsIdBase;
            var existing = ServerSpecificSettingsSync.DefinedSettings ?? Array.Empty<ServerSpecificSettingBase>();
            if (id < 0 || id > int.MaxValue - 2 || existing.Any(s => s != null && s.SettingId >= id && s.SettingId <= id + 2))
                throw new InvalidOperationException("Zombie opt-out setting IDs collide with another plugin. Change settings_id_base.");
            settings = new ServerSpecificSettingBase[]
            {
                new SSGroupHeader(id, "Zombie Opt-out", false, "Skip being revived as SCP-049-2, or take the place of players who skip it."),
                new SSTwoButtonsSetting(id + 1, "Opt out of being a zombie", "Yes", "No", true, "When SCP-049 revives you, you go back to spectator and someone else can take your place.", byte.MaxValue, false),
                new SSTwoButtonsSetting(id + 2, "Auto-fill for opted-out zombies", "Yes", "No", true, "While spectating, automatically take the place of a player who opted out.", byte.MaxValue, false),
            };
            ServerSpecificSettingsSync.DefinedSettings = existing.Concat(settings).ToArray();
            ServerSpecificSettingsSync.ServerOnSettingValueReceived += SettingReceived;
            P.ChangingRole += ChangingRole;
            P.Destroying += Destroying;
            P.ReceivingEffect += ReceivingEffect;
            P.Left += Left;
            S.RoundStarted += RoundStarted;
            S.WaitingForPlayers += Reset;
            S.RestartingRound += Reset;
            Instance = this;
            ServerSpecificSettingsSync.SendToAll();
            base.OnEnabled();
            updates = Brainless.Internal.Updates.PluginUpdates.Start(GetType().Assembly, "ZombieOptOut", Config.AutoUpdate);
        }

        public override void OnDisabled()
        {
            updates?.Dispose();
            updates = null;
            ServerSpecificSettingsSync.ServerOnSettingValueReceived -= SettingReceived;
            P.ChangingRole -= ChangingRole;
            P.Destroying -= Destroying;
            P.ReceivingEffect -= ReceivingEffect;
            P.Left -= Left;
            S.RoundStarted -= RoundStarted;
            S.WaitingForPlayers -= Reset;
            S.RestartingRound -= Reset;
            Reset();
            preferences.Clear();
            if (settings != null)
            {
                // Remove only our definitions; other plugins' settings stay registered.
                ServerSpecificSettingsSync.DefinedSettings = (ServerSpecificSettingsSync.DefinedSettings ?? Array.Empty<ServerSpecificSettingBase>()).Where(s => !settings.Contains(s)).ToArray();
                ServerSpecificSettingsSync.SendToAll();
                settings = null;
            }

            Instance = null;
            base.OnDisabled();
        }

        private void Reset()
        {
            lifetime.Clear();
            zombieSlots.Clear();
            scpSlots.Clear();
            fillPool.Clear();
            offenders.Clear();
            spawnedWithCustomRole.Clear();
            activeScpSlot = null;
            withinRoundStart = false;
        }

        private Preference Prefs(Player player)
        {
            if (!preferences.TryGetValue(player, out var value))
                preferences[player] = value = new Preference();
            return value;
        }

        private void SettingReceived(ReferenceHub hub, ServerSpecificSettingBase setting) => Safe.Run(() =>
        {
            if (settings == null || !settings.Contains(setting.OriginalDefinition) || !(setting is SSTwoButtonsSetting buttons))
                return;
            var player = Player.Get(hub);
            if (player == null || player.IsHost)
                return;
            if (setting.SettingId == Config.SettingsIdBase + 1)
                Prefs(player).OptOut = buttons.SyncIsA;
            else if (setting.SettingId == Config.SettingsIdBase + 2)
                Prefs(player).AutoFill = buttons.SyncIsA;
        });

        private void Left(LeftEventArgs ev)
        {
            preferences.Remove(ev.Player);
            fillPool.RemoveAll(p => p == ev.Player);
        }

        private void RoundStarted()
        {
            Reset();
            withinRoundStart = Config.AfkReplacement;
            // Record who spawned with a custom role once Roles has assigned them.
            lifetime.Later(3, () =>
            {
                foreach (var player in Player.List.Where(p => p.IsConnected && RolesBridge.ProfileId(p) != null))
                    spawnedWithCustomRole.Add(player.UserId ?? "");
            });
            lifetime.Later(Math.Max(0, Config.AfkReplacementValidTime), () => withinRoundStart = false);
        }

        // ---- Zombie opt-out -------------------------------------------------------------

        private void ChangingRole(ChangingRoleEventArgs ev) => Safe.Run(() =>
        {
            if (!ev.IsAllowed || ev.Reason != SpawnReason.Revived || ev.NewRole != RoleTypeId.Scp0492 || !Prefs(ev.Player).OptOut)
                return;
            var target = ev.Player;
            // Give Roles time to restore/roll the zombie profile so it can be handed on.
            lifetime.Later(1, () =>
            {
                if (!target.IsConnected || target.Role.Type != RoleTypeId.Scp0492)
                    return;
                var slot = new ZombieSlot { Position = target.Position, Profile = RolesBridge.ProfileId(target), OptedOut = target.Nickname };
                target.Broadcast(5, "<size=36>[Zombie Opt-out] You opted out of being revived as a zombie in your Settings.</size>", Broadcast.BroadcastFlags.Normal, true);
                target.Role.Set(RoleTypeId.Spectator, SpawnReason.ForceClass, RoleSpawnFlags.None);
                OfferZombieSlot(slot, target);
            });
        });

        private void OfferZombieSlot(ZombieSlot slot, Player optedOut)
        {
            var auto = Player.List.FirstOrDefault(p => p != optedOut && p.IsConnected && !p.IsHost && !p.IsNPC && p.Role.Type == RoleTypeId.Spectator && Prefs(p).AutoFill);
            if (auto != null)
            {
                FillZombie(slot, auto);
                foreach (var doctor in Player.List.Where(p => p.Role.Type == RoleTypeId.Scp049))
                    doctor.Broadcast(5, "<size=36>[Zombie Opt-out] <b>" + slot.OptedOut + "</b> opted out and was replaced by <b>" + auto.Nickname + "</b>.</size>", Broadcast.BroadcastFlags.Normal, true);
                return;
            }

            zombieSlots.Enqueue(slot);
            foreach (var spectator in Player.List.Where(p => p != optedOut && p.IsConnected && !p.IsHost && !p.IsNPC && p.Role.Type == RoleTypeId.Spectator))
                spectator.Broadcast(6, "<size=36>[Zombie Opt-out] <b>" + slot.OptedOut + "</b> opted out of being a zombie. Take their place: type <b>.optin</b> in your console (~)!</size>", Broadcast.BroadcastFlags.Normal, true);
            lifetime.Later(Math.Max(1, Config.ZombieFillDuration), () =>
            {
                if (slot.Filled)
                    return;
                slot.Filled = true;
                foreach (var doctor in Player.List.Where(p => p.IsAlive && p.Role.Type == RoleTypeId.Scp049))
                {
                    doctor.Heal(Config.HealthCompensation);
                    doctor.Broadcast(5, "<size=36>[Zombie Opt-out] A zombie opted out and nobody replaced it: <b>+" + Config.HealthCompensation.ToString("0") + " HP</b>.</size>", Broadcast.BroadcastFlags.Normal, true);
                }
            });
        }

        internal bool OptIn(Player player, out string response)
        {
            while (zombieSlots.Count > 0 && zombieSlots.Peek().Filled)
                zombieSlots.Dequeue();
            if (zombieSlots.Count == 0)
            {
                response = "No zombie spot is waiting to be filled. Turn on Auto-fill in Settings to never miss one.";
                return false;
            }

            if (player.Role.Type != RoleTypeId.Spectator)
            {
                response = "Only spectators can take a zombie's place.";
                return false;
            }

            FillZombie(zombieSlots.Dequeue(), player);
            response = "You took the place of a zombie who opted out. Thanks!";
            return true;
        }

        private void FillZombie(ZombieSlot slot, Player player)
        {
            slot.Filled = true;
            // ForceClass so Roles does not roll a new profile; the opted-out zombie's
            // Brainless profile (if any) is carried over instead.
            player.Role.Set(RoleTypeId.Scp0492, SpawnReason.ForceClass, RoleSpawnFlags.AssignInventory);
            player.Position = slot.Position;
            lifetime.Later(.5f, () =>
            {
                if (!player.IsConnected || player.Role.Type != RoleTypeId.Scp0492)
                    return;
                player.Position = slot.Position;
                if (slot.Profile != null)
                {
                    string failure = RolesBridge.AssignProfile(player, slot.Profile);
                    if (failure != null && Config.Debug)
                        Log.Debug("Could not carry zombie profile " + slot.Profile + ": " + failure);
                }
            });
            Award(player, "zombie_fill", Config.ZombieFillXp);
        }

        // ---- Lost SCP replacement (.fill) ----------------------------------------------

        private void Destroying(DestroyingEventArgs ev) => Safe.Run(() =>
        {
            var player = ev.Player;
            if (player == null || player.IsNPC || !player.IsAlive)
                return;
            QueueScp(player);
        });

        // An SCP that throws itself into a pit with no enemy nearby counts as a suicide.
        private void ReceivingEffect(ReceivingEffectEventArgs ev) => Safe.Run(() =>
        {
            var player = ev.Player;
            if (!ev.IsAllowed || ev.Intensity == 0 || player == null || player.IsNPC || ev.Effect == null || ev.Effect.GetType().Name != "PitDeath")
                return;
            if (!player.IsScp || player.Role.Type == RoleTypeId.Scp0492)
                return;
            // SCP-173 pushed over a pit while being looked at is not a suicide.
            if (player.Role.Base is PlayerRoles.PlayableScps.Scp173.Scp173Role scp173 && scp173.SubroutineModule.TryGetSubroutine(out PlayerRoles.PlayableScps.Scp173.Scp173BlinkTimer timer) && RemainingSustain(timer) > 0)
                return;
            if (player.CurrentRoom != null && player.CurrentRoom.Players.Any(p => p != player && p.IsAlive && p.Role.Side != player.Role.Side))
                return;
            offenders.Add(player.UserId ?? "");
            QueueScp(player);
        });

        private void QueueScp(Player player)
        {
            if (!withinRoundStart || Warhead.IsDetonated || !player.IsScp || player.Role.Type == RoleTypeId.Scp0492)
                return;
            var slot = new ScpSlot
            {
                Role = player.Role.Type,
                Health = player.Health > 0 && player.Health < player.MaxHealth ? player.Health : -1,
                Profile = RolesBridge.ProfileId(player),
                ProfileName = RolesBridge.ProfileName(player),
            };
            if (scpSlots.Any(s => s.Role == slot.Role) || activeScpSlot?.Role == slot.Role)
                return;
            scpSlots.Enqueue(slot);
            if (activeScpSlot == null)
                OpenNextScpSlot();
        }

        private void OpenNextScpSlot()
        {
            fillPool.Clear();
            activeScpSlot = scpSlots.Count > 0 && withinRoundStart && !Warhead.IsDetonated ? scpSlots.Dequeue() : null;
            if (activeScpSlot == null)
                return;
            var slot = activeScpSlot;
            string what = "<b>" + ScpName(slot.Role) + "</b>" + (slot.ProfileName != null ? " (" + slot.ProfileName + ")" : "") + (slot.Health > 0 ? " [" + slot.Health.ToString("0") + " HP]" : "");
            foreach (var player in Player.List.Where(p => p.IsConnected && !p.IsHost && !p.IsNPC && !p.IsScp && RolesBridge.ProfileId(p) == null))
                player.Broadcast(10, "<size=36>[SCP Fill] " + what + " left the game!\nTake their place: type <b>.fill</b> in your console (~).</size>", Broadcast.BroadcastFlags.Normal, true);
            lifetime.Later(Math.Max(1, Config.ScpFillDuration), () =>
            {
                var candidates = fillPool.Where(p => p.IsConnected && !p.IsScp).ToList();
                if (candidates.Count > 0)
                    FillScp(slot, candidates[random.Next(candidates.Count)]);
                OpenNextScpSlot();
            });
        }

        internal bool Fill(Player player, out string response)
        {
            if (activeScpSlot == null || !withinRoundStart)
            {
                response = withinRoundStart ? "No SCP needs a replacement right now." : "It's too late in the round to fill an SCP.";
                return false;
            }

            if (player.IsScp)
            {
                response = "You can't fill while playing an SCP.";
                return false;
            }

            if (fillPool.Contains(player))
            {
                response = "You're already in the fill queue.";
                return false;
            }

            if (offenders.Contains(player.UserId ?? ""))
            {
                response = "You left an SCP this round, so you can't fill one.";
                return false;
            }

            int entries = spawnedWithCustomRole.Contains(player.UserId ?? "") ? 1 : Math.Max(1, Config.FillChanceMultiplierForNoCustomRole);
            for (int i = 0; i < entries; i++)
                fillPool.Add(player);
            int others = fillPool.Distinct().Count() - 1;
            response = "You joined the fill queue (" + others + " other player" + (others == 1 ? "" : "s") + ")." + (entries > 1 ? " You didn't spawn with a custom role, so you're " + entries + "x as likely to be picked." : "");
            return true;
        }

        private void FillScp(ScpSlot slot, Player player)
        {
            player.Role.Set(slot.Role, SpawnReason.ForceClass, RoleSpawnFlags.All);
            lifetime.Later(.5f, () =>
            {
                if (!player.IsConnected || player.Role.Type != slot.Role)
                    return;
                if (slot.Profile != null)
                    RolesBridge.AssignProfile(player, slot.Profile);
            });
            // Restore the remaining health after any profile health change has applied.
            lifetime.Later(3, () =>
            {
                if (slot.Health > 0 && player.IsConnected && player.Role.Type == slot.Role)
                    player.Health = Mathf.Clamp(slot.Health, 1, player.MaxHealth);
            });
            foreach (var other in Player.List.Where(p => p.IsConnected && !p.IsHost))
                other.Broadcast(5, "[SCP Fill] " + ScpName(slot.Role) + " has been replaced by " + player.Nickname + "!", Broadcast.BroadcastFlags.Normal, true);
            Award(player, "scp_fill", Config.ScpFillXp);
        }

        // Private in the original game assembly; read through cached reflection.
        private static readonly System.Reflection.PropertyInfo Sustain = HarmonyLib.AccessTools.Property(typeof(PlayerRoles.PlayableScps.Scp173.Scp173BlinkTimer), "RemainingSustain");

        private static float RemainingSustain(PlayerRoles.PlayableScps.Scp173.Scp173BlinkTimer timer)
        {
            try
            {
                return Sustain?.GetValue(timer) is float value ? value : 0;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        private static string ScpName(RoleTypeId role) => role.ToString().Replace("Scp", "SCP-");

        private static void Award(Player player, string action, int xp)
        {
            if (xp > 0 && !string.IsNullOrEmpty(player.UserId) && CoreBridge.CanTrack(player.UserId))
                CoreBridge.Record(player.UserId, player.Nickname, action, xp, action + ":" + Guid.NewGuid().ToString("N"));
        }
    }
}
