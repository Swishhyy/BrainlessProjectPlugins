using System;
using System.Collections.Generic;
using System.Linq;
using Brainless.Internal;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.API.Features.Roles;
using Exiled.CustomItems.API.Features;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Scp049;
using Exiled.Events.EventArgs.Scp914;
using HarmonyLib;
using MEC;
using PlayerRoles;
using PlayerRoles.PlayableScps.HumeShield;
using PlayerRoles.PlayableScps.Scp049;
using UnityEngine;
using P = Exiled.Events.Handlers.Player;
using S = Exiled.Events.Handlers.Server;
using D = Exiled.Events.Handlers.Scp049;
using U = Exiled.Events.Handlers.Scp914;
using Random = System.Random;
using Scp049Role = Exiled.API.Features.Roles.Scp049Role;
using FxAudio = Brainless.Internal.Audio;

namespace Brainless.Roles
{
    public sealed class Plugin : Plugin<Config>
    {
        public override string Name => "BrainlessRoles";
        public override string Prefix => "brainless_roles";
        public override string Author => "Brainless Project";
        public override Version Version => new Version(0, 2, 0);
        public override Version RequiredExiledVersion => new Version(9, 15, 2);
        public override PluginPriority Priority => PluginPriority.Lower;
        public static Plugin Instance { get; private set; }

        private readonly Random random = new Random();
        private readonly Lifetime lifetime = new Lifetime();
        private readonly Dictionary<Player, ActiveRole> active = new Dictionary<Player, ActiveRole>();
        private readonly Dictionary<Player, int> generations = new Dictionary<Player, int>();
        private readonly Dictionary<Player, SpawnReason> reasons = new Dictionary<Player, SpawnReason>();
        private readonly Dictionary<string, RoleDefinition> zombieHistory = new Dictionary<string, RoleDefinition>();
        private readonly Dictionary<string, int> issued = new Dictionary<string, int>();
        private readonly Dictionary<Player, string> assignmentNotes = new Dictionary<Player, string>();
        private readonly Dictionary<string, int> burstSpent = new Dictionary<string, int>();
        private readonly DeathCapture<Player, PendingBurst> pendingBursts = new DeathCapture<Player, PendingBurst>();
        private readonly FragProjectiles bursts = new FragProjectiles();
        private readonly HashSet<string> doctorSpent = new HashSet<string>();
        private readonly Dictionary<int, int> badges = new Dictionary<int, int>();
        private bool doctorRolled;
        private float waveStart;
        private int waveCount;
        private Harmony harmony;
        private RoleHud roleHud;
        private Lockpicking lockpicking;
        private Brainless.Internal.Updates.PluginUpdates updates;
        public override void OnEnabled()
        {
            Config.Prepare();
            foreach (var role in Config.Roles)
            {
                role.Id = role.Id.ToUpperInvariant();
                // Retire the pass even in previously generated/customized YAML kits.
                role.CustomItems.RemoveAll(id => id == 7110);
                if (role.Id == "D06" && role.Hint == "One Maintenance Pass; 90 HP.")
                    role.Hint = "90 HP. Interact to lockpick: 15 seconds, 120-second cooldown. Level 3 excluded.";
                if (role.Id == "Z04" && role.Hint == "75% HP. One enemy combat death arms a two-second warning burst.")
                    role.Hint = "75% HP. Explodes after a confirmed death; warning and blast are configurable.";
                if (role.Id == "Z05" && role.HealthMultiplier == 1 && role.ShieldMultiplier == 1)
                {
                    role.HealthMultiplier = .4f;
                    role.ShieldMultiplier = .5f;
                }
            }

            Instance = this;
            lockpicking = new Lockpicking(Config, lifetime, RoleId);
            roleHud = new RoleHud(Config, player => active.TryGetValue(player, out var state) ? state.Definition : null);
            harmony = new Harmony("brainless.roles.0.1");
            harmony.PatchAll();
            P.ChangingRole += ChangingRole;
            P.Spawned += Spawned;
            P.Dying += Dying;
            P.Died += Died;
            Exiled.Events.Handlers.Map.ExplodingGrenade += BurstExploding;
            P.Hurting += Hurting;
            P.InteractingDoor += LockpickDoor;
            P.Left += Left;
            S.WaitingForPlayers += Reset;
            S.RoundEnded += Ended;
            D.StartingRecall += StartingRecall;
            D.FinishingRecall += FinishingRecall;
            U.UpgradingPickup += UpgradePickup;
            U.UpgradingInventoryItem += UpgradeInventory;
            lifetime.Run(Tick());
            base.OnEnabled();
            updates = Brainless.Internal.Updates.PluginUpdates.Start(GetType().Assembly, "Roles", Config.AutoUpdate);
        }

        public override void OnDisabled()
        {
            updates?.Dispose();
            updates = null;
            P.ChangingRole -= ChangingRole;
            P.Spawned -= Spawned;
            P.Dying -= Dying;
            P.Died -= Died;
            Exiled.Events.Handlers.Map.ExplodingGrenade -= BurstExploding;
            P.Hurting -= Hurting;
            P.InteractingDoor -= LockpickDoor;
            P.Left -= Left;
            S.WaitingForPlayers -= Reset;
            S.RoundEnded -= Ended;
            D.StartingRecall -= StartingRecall;
            D.FinishingRecall -= FinishingRecall;
            U.UpgradingPickup -= UpgradePickup;
            U.UpgradingInventoryItem -= UpgradeInventory;
            foreach (var player in active.Keys.ToArray())
                Remove(player);
            bursts.Clear();
            pendingBursts.Clear();
            lifetime.Clear();
            lockpicking?.Clear();
            lockpicking = null;
            roleHud?.Clear();
            roleHud = null;
            harmony?.UnpatchAll(harmony.Id);
            Instance = null;
            base.OnDisabled();
        }

        private void Ended(Exiled.Events.EventArgs.Server.RoundEndedEventArgs ev) => Reset();
        private void Reset()
        {
            lockpicking?.Clear();
            roleHud?.Clear();
            foreach (var player in active.Keys.ToArray())
                Remove(player);
            lifetime.Clear();
            generations.Clear();
            bursts.Clear();
            pendingBursts.Clear();
            reasons.Clear();
            zombieHistory.Clear();
            issued.Clear();
            assignmentNotes.Clear();
            burstSpent.Clear();
            doctorSpent.Clear();
            badges.Clear();
            doctorRolled = false;
            waveCount = 0;
            if (Config.IsEnabled)
                lifetime.Run(Tick());
        }

        private void Left(LeftEventArgs ev)
        {
            roleHud?.Forget(ev.Player);
            Remove(ev.Player);
            generations.Remove(ev.Player);
            reasons.Remove(ev.Player);
            assignmentNotes.Remove(ev.Player);
            pendingBursts.Forget(ev.Player);
        }

        private void ChangingRole(ChangingRoleEventArgs ev) => Safe.Run(() =>
        {
            if (!ev.IsAllowed)
                return;
            roleHud?.Forget(ev.Player);
            Remove(ev.Player);
            generations[ev.Player] = Generation(ev.Player) + 1;
            reasons[ev.Player] = ev.Reason;
            assignmentNotes[ev.Player] = "Waiting for spawn assignment (" + ev.Reason + ").";
            if (ev.NewRole != RoleTypeId.Scp0492 && ev.NewRole != RoleTypeId.Spectator)
                zombieHistory.Remove(ev.Player.UserId);
        });
        private int Generation(Player player) => generations.TryGetValue(player, out int value) ? value : 0;
        private void Spawned(SpawnedEventArgs ev)
        {
            int generation = Generation(ev.Player);
            lifetime.Later(.25f, () =>
            {
                if (ev.Player.IsConnected && ev.Player.IsAlive && generation == Generation(ev.Player))
                    Assign(ev.Player);
            });
        }

        private bool Matches(RoleDefinition role, Player player)
        {
            RoleTypeId type = player.Role.Type;
            if (role.Id.StartsWith("M"))
                return type == RoleTypeId.NtfPrivate || type == RoleTypeId.NtfSergeant || type == RoleTypeId.NtfSpecialist || type == RoleTypeId.NtfCaptain;
            if (role.Id.StartsWith("C"))
                return type == RoleTypeId.ChaosConscript || type == RoleTypeId.ChaosRifleman || type == RoleTypeId.ChaosMarauder || type == RoleTypeId.ChaosRepressor;
            return role.BaseRole == type;
        }

        private float Chance(Player player) => player.Role.Type == RoleTypeId.ClassD ? Config.ClassDChance : player.Role.Type == RoleTypeId.Scientist ? Config.ScientistChance : player.Role.Type == RoleTypeId.FacilityGuard ? Config.GuardChance : player.Role.Type == RoleTypeId.Scp049 ? Config.DoctorVariantChance : player.Role.Type == RoleTypeId.Scp0492 ? Config.ZombieChance : Config.ReinforcementChance;
        private void Assign(Player player)
        {
            if (active.ContainsKey(player) || !Round.IsStarted)
                return;
            if (!reasons.TryGetValue(player, out var reason) || !(reason == SpawnReason.RoundStart || reason == SpawnReason.LateJoin || reason == SpawnReason.Respawn || reason == SpawnReason.RespawnMiniwave || reason == SpawnReason.Revived))
            {
                assignmentNotes[player] = "Native role kept for spawn reason " + reason + "; use bprole set for an explicit admin test.";
                return;
            }

            if (player.Role.Type == RoleTypeId.ClassD && Config.GiveClassDCoins && !player.Items.Any(i => i.Type == ItemType.Coin))
                player.AddItem(ItemType.Coin);
            if (player.Role.Type == RoleTypeId.Scp0492 && zombieHistory.TryGetValue(player.UserId, out var previous))
            {
                if (previous.Enabled && (!previous.Experimental || Config.AllowExperimentalRoles))
                {
                    Apply(player, previous, true);
                    return;
                }
            }

            if (player.Role.Type == RoleTypeId.Scp0492 && Config.EnableDoctorEvent && Config.AllowExperimentalRoles && Config.Roles.Any(r => r.Id == "Z05" && r.Enabled) && !doctorRolled && Player.List.Count >= 12 && Player.List.Any(p => p.Role.Type == RoleTypeId.Scp049))
            {
                doctorRolled = true;
                if (random.NextDouble() < .01)
                {
                    ConvertDoctor(player);
                    return;
                }
            }

            if (random.NextDouble() * 100 >= Chance(player))
            {
                assignmentNotes[player] = "Normal role: custom-role chance roll did not pass (" + Chance(player) + "%).";
                return;
            }

            var selected = Rules.ChooseEligible(Config.Roles.Where(r => Matches(r, player)), r => Availability(r, player) == null, r => r.Weight, random);
            if (selected == null)
            {
                assignmentNotes[player] = "No eligible role profile. Run bproles " + player.Id + " to inspect requirements.";
                return;
            }

            Apply(player, selected, false);
        }

        internal string Availability(RoleDefinition definition, Player player = null, bool admin = false)
        {
            if (!definition.Enabled)
                return "disabled in roles config";
            if (definition.Experimental && !Config.AllowExperimentalRoles)
                return "requires allow_experimental_roles";
            if (definition.Id == "Z05" && !Config.EnableDoctorEvent)
                return "requires enable_doctor_event";
            if (!admin && definition.Id == "Z05")
                return "event only: excluded from normal role draws";
            if (!admin && definition.Weight <= 0)
                return "zero spawn weight";
            int population = Player.List.Count(p => p.IsConnected && !p.IsHost);
            if (!admin && population < definition.MinPlayers)
                return "requires " + definition.MinPlayers + " players; currently " + population;
            if (!admin && Count(definition.Id) >= definition.MaxPerRound)
                return "per-round role cap reached";
            if (player != null && !Matches(definition, player))
                return "requires " + definition.BaseRole + " or matching faction";
            if (player != null && definition.Id == "C03" && !player.Items.Any(i => i.Type == ItemType.GunAK))
                return "requires a Chaos subtype with an AK";
            if (!admin && player != null && player.IsHuman)
            {
                int living = Player.List.Count(p => p.IsHuman && p.IsAlive);
                if (active.Count(p => p.Key.IsHuman && p.Key.IsAlive) >= Math.Max(1, living / 2))
                    return "living-human specialist cap reached";
                if (population < 12 && player.Role.Type == RoleTypeId.FacilityGuard && active.Any(p => p.Key.Role.Type == RoleTypeId.FacilityGuard))
                    return "small-lobby Guard specialist cap reached";
                if (definition.Id.StartsWith("M") || definition.Id.StartsWith("C"))
                {
                    int currentWaveCount = Time.time - waveStart > 8 ? 0 : waveCount;
                    if (currentWaveCount >= Rules.WaveCap(population) || active.Any(p => p.Value.Definition.Id == definition.Id && Time.time - p.Value.Start < 8))
                        return "reinforcement wave cap or duplicate-specialist limit reached";
                }
            }

            var custom = definition.CustomItems.Select(FindCustom).ToArray();
            if (custom.Any(i => i == null))
                return "missing/disabled custom item or weapon: " + string.Join(", ", definition.CustomItems.Where(id => FindCustom(id) == null));
            foreach (var group in custom.GroupBy(i => i.GetType().Assembly))
            {
                var item = group.First();
                var method = item.GetType().GetMethod("CanIssueBatch", new[] { typeof(uint[]) });
                if (method == null)
                    return "item provider lacks CanIssueBatch support";
                if (admin && item.GetType().GetMethod("GiveForRole", new[] { typeof(Player), typeof(bool) }) == null)
                    return "update Items/Weapons DLLs before using the admin role test";
                try
                {
                    if (!admin && Round.IsStarted && !(bool)method.Invoke(item, new object[] { definition.CustomItems.ToArray() }))
                        return "item/weapon supply exhausted or kit unavailable in " + item.GetType().Assembly.GetName().Name;
                }
                catch (Exception exception)
                {
                    return "item provider check failed in " + item.GetType().Assembly.GetName().Name + " (" + exception.GetBaseException().GetType().Name + ")";
                }
            }

            if (player != null)
            {
                int removed = player.Items.Count(i => definition.Remove.Contains(i.Type));
                int added = definition.Add.Count + definition.CustomItems.Count + definition.CustomItems.Count(id => id == 7101);
                if (definition.Id == "D03")
                    added++;
                if (definition.Id == "S04")
                    added += Config.ApprovedCandy.Count == 0 ? 3 : 1;
                if (player.Items.Count - removed + added > 8)
                    return "insufficient inventory space for the complete role kit";
            }

            return null;
        }

        private CustomItem FindCustom(uint id) => CustomItem.Registered.FirstOrDefault(i => i.Id == id);
        private int Count(string id) => issued.TryGetValue(id, out int count) ? count : 0;
        internal string RoleId(Player player) => player != null && active.TryGetValue(player, out var a) ? a.Definition.Id : null;
        internal string Inspect(Player player)
        {
            string profile = active.TryGetValue(player, out var state) && player.Role == state.Role ? state.Definition.Id + " - " + state.Definition.Name + "\n" + state.Definition.Hint : "No Brainless custom role; native " + player.Role.Type;
            string note = assignmentNotes.TryGetValue(player, out var value) ? value : "No spawn assignment recorded.";
            return player.Nickname + " (" + player.Id + "): " + profile + "\nBase: " + player.Role.Type + "; HP: " + player.Health.ToString("0") + "/" + player.MaxHealth.ToString("0") + "; shield: " + player.HumeShield.ToString("0") + "/" + player.MaxHumeShield.ToString("0") + "\n" + note;
        }

        internal bool SetForAdmin(Player player, string id, out string response)
        {
            var definition = Config.Roles.FirstOrDefault(r => r.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (definition == null)
            {
                response = "Unknown role ID. Use bproles.";
                return false;
            }

            string blocked = Availability(definition, admin: true);
            if (blocked != null)
            {
                response = definition.Id + " cannot be tested: " + blocked;
                return false;
            }

            if (player == null || !player.IsConnected || player.IsHost)
            {
                response = "Choose a connected player.";
                return false;
            }

            // A fresh native inventory makes repeated admin tests reproducible.
            // Preserve a matching MTF/Chaos rank; Doctor explicitly uses SCP-049.
            RoleTypeId baseRole = definition.Id == "Z05" ? RoleTypeId.Scp049 : Matches(definition, player) ? player.Role.Type : definition.BaseRole;
            var previousRole = player.Role.Base;
            player.Role.Set(baseRole, SpawnReason.ForceClass, RoleSpawnFlags.AssignInventory);
            if (player.Role.Type != baseRole || player.Role.Base == previousRole)
            {
                response = "The base role change was blocked by the server or another plugin.";
                return false;
            }

            int generation = Generation(player);
            lifetime.Later(.3f, () =>
            {
                if (!player.IsConnected || !player.IsAlive || Generation(player) != generation || player.Role.Type != baseRole)
                    return;
                string failure = definition.Id == "Z05" ? null : Availability(definition, player, true);
                if (failure != null)
                {
                    assignmentNotes[player] = "Admin role test blocked: " + failure;
                    player.ShowHint(assignmentNotes[player], 7);
                    return;
                }

                Apply(player, definition, false, true);
            });
            response = "Queued " + definition.Id + " - " + definition.Name + " for " + player.Nickname + ". Check with bprole " + player.Id + " after the spawn finishes.";
            return true;
        }

        private void Apply(Player player, RoleDefinition definition, bool revived, bool admin = false)
        {
            var state = new ActiveRole(player, definition);
            active[player] = state;
            try
            {
                if (!revived)
                {
                    foreach (var type in definition.Remove)
                        foreach (var item in player.Items.Where(i => i.Type == type).ToArray())
                            player.RemoveItem(item);
                    foreach (var type in definition.Add)
                        player.AddItem(type);
                    if (definition.Id == "C01" || definition.Id == "C04")
                    {
                        var item = player.Items.FirstOrDefault(i => i.Type == ItemType.GrenadeHE) ?? player.Items.FirstOrDefault(i => i.Type == ItemType.Medkit);
                        if (item != null)
                            player.RemoveItem(item);
                    }

                    foreach (var ammo in player.Ammo.ToArray())
                        player.SetAmmo(ammo.Key.GetAmmoType(), (ushort)(ammo.Value * definition.ReserveMultiplier));
                    if (definition.Id == "M03" || definition.Id == "M04" || definition.Id == "C03")
                        player.ClearAmmo();
                    foreach (uint id in definition.CustomItems)
                    {
                        var custom = FindCustom(id);
                        if (custom == null)
                            throw new InvalidOperationException($"Optional item {id} was removed during loadout assignment.");
                        int before = custom.TrackedSerials.Count;
                        if (admin)
                        {
                            var method = custom.GetType().GetMethod("GiveForRole", new[] { typeof(Player), typeof(bool) });
                            if (method == null)
                                throw new InvalidOperationException("Update the item/weapon provider before testing this role.");
                            string failure = (string)method.Invoke(custom, new object[] { player, true });
                            if (failure != null)
                                throw new InvalidOperationException(failure);
                        }
                        else
                            custom.Give(player, false);
                        if (custom.TrackedSerials.Count <= before)
                            throw new InvalidOperationException($"Item {id} supply was exhausted.");
                    }

                    if (definition.Id == "D04")
                        Thief(player);
                    if (definition.Id == "D03")
                        Candy(player, 1);
                    if (definition.Id == "S04")
                        Candy(player, 3);
                    if (definition.Id == "G02")
                    {
                        player.ClearAmmo();
                        var gun = player.Items.OfType<Firearm>().FirstOrDefault();
                        if (gun != null)
                            player.SetAmmo(gun.AmmoType, (ushort)gun.MaxMagazineAmmo);
                    }

                    if (player.Role.Type == RoleTypeId.FacilityGuard)
                        GiveBadge(player, definition.Id);
                    if (!admin)
                        issued[definition.Id] = Count(definition.Id) + 1;
                    if (!admin && (definition.Id.StartsWith("M") || definition.Id.StartsWith("C")))
                    {
                        if (Time.time - waveStart > 8)
                        {
                            waveStart = Time.time;
                            waveCount = 0;
                        }

                        waveCount++;
                    }
                }

                player.MaxHealth = definition.Health > 0 ? definition.Health : state.MaxHealth * definition.HealthMultiplier;
                player.Health = player.MaxHealth;
                if (player.Role is IHumeShieldRole)
                {
                    player.MaxHumeShield = state.MaxShield * definition.ShieldMultiplier;
                    player.HumeShield = Math.Min(player.MaxHumeShield, state.Shield * definition.ShieldMultiplier);
                }

                if (definition.Scale != 1 && Config.AllowExperimentalRoles)
                    player.Scale = state.Scale * definition.Scale;
                if (definition.Id == "Z02" && Config.ExtremeMuncherEventPreset)
                    player.Scale = new Vector3(state.Scale.x, state.Scale.y * .5f, state.Scale.z);
                if (player.Role is Scp049Role doctor && doctor.HumeShieldModule is DynamicHumeShieldController hs)
                {
                    state.RegenCooldown = hs.RegenerationCooldown;
                    if (definition.Id == "P01")
                        hs.RegenerationCooldown *= .85f;
                }

                if (player.Role.Type == RoleTypeId.Scp0492)
                    zombieHistory[player.UserId] = definition;
                player.CustomInfo = $"{definition.Name} [{definition.Id}]";
                assignmentNotes[player] = admin ? "Assigned by admin test command." : revived ? "Restored after resurrection." : "Assigned by normal spawn roll.";
                player.ShowHint($"<b>{definition.Name}</b>\n{definition.Hint}", 10);
            }
            catch
            {
                Remove(player);
                // A provider can be disabled after preflight. Restore a complete native
                // kit instead of leaving a partially modified inventory behind.
                if (player.IsConnected && player.IsAlive)
                    player.Role.Set(player.Role.Type, RoleSpawnFlags.AssignInventory);
                throw;
            }
        }

        private void Candy(Player player, int count)
        {
            if (Config.ApprovedCandy.Count == 0)
            {
                for (int i = 0; i < count; i++)
                    player.AddItem(ItemType.Painkillers);
                return;
            }

            var bag = player.Items.OfType<Scp330>().FirstOrDefault() ?? (Scp330)player.AddItem(ItemType.SCP330);
            for (int i = 0; i < count; i++)
                bag.AddCandy(Config.ApprovedCandy[random.Next(Config.ApprovedCandy.Count)]);
        }

        private void Thief(Player player)
        {
            int roll = random.Next(100);
            if (roll < 45)
                player.AddItem(ItemType.Medkit);
            else if (roll < 80)
                player.AddItem(ItemType.KeycardJanitor);
            else if (roll < 95)
                player.AddItem(ItemType.ArmorLight);
            else if (Player.List.Count >= 12)
            {
                var gun = (Firearm)player.AddItem(ItemType.GunCOM15);
                gun.MagazineAmmo = 4;
                gun.BarrelAmmo = 0;
                player.ClearAmmo();
            }
            else
                player.AddItem(ItemType.Medkit);
        }

        private void GiveBadge(Player player, string id)
        {
            foreach (var item in player.Items.OfType<Keycard>().ToArray())
                player.RemoveItem(item);
            int tier = id == "G01" ? 2 : id == "G02" || id == "G04" ? 0 : 1;
            var card = (Exiled.API.Features.Items.Keycards.CustomKeycardItem)player.AddItem(ItemType.KeycardCustomTaskForce);
            card.Permissions = KeycardPermissions.Checkpoints | KeycardPermissions.Intercom | KeycardPermissions.ArmoryLevelOne;
            if (tier > 0)
                card.Permissions |= KeycardPermissions.ContainmentLevelOne;
            if (tier == 2)
                card.Permissions |= KeycardPermissions.ArmoryLevelTwo;
            if (tier == 2 && Config.CaptainGateAccess)
                card.Permissions |= KeycardPermissions.ExitGates;
            card.ItemName = id == "G01" ? "Guard Captain Badge" : "Facility Security Badge";
            badges[card.Serial] = tier;
        }

        private void UpgradePickup(UpgradingPickupEventArgs ev)
        {
            if (!badges.Remove(ev.Pickup.Serial))
                return;
            ev.IsAllowed = false;
            var replacement = Exiled.API.Features.Pickups.Pickup.CreateAndSpawn(ItemType.KeycardGuard, ev.Pickup.Position);
            ev.Pickup.Destroy();
            Scp914.Scp914Upgrader.ProcessPickup(replacement.Base, true, ev.KnobSetting);
        }

        private void UpgradeInventory(UpgradingInventoryItemEventArgs ev)
        {
            if (!badges.Remove(ev.Item.Serial))
                return;
            ev.IsAllowed = false;
            ev.Player.RemoveItem(ev.Item);
            var replacement = ev.Player.AddItem(ItemType.KeycardGuard);
            if (Scp914.Scp914Upgrader.TryGetProcessor(replacement.Type, out var processor))
            {
                var result = processor.UpgradeInventoryItem(ev.KnobSetting, replacement.Base);
                Scp914.Processors.Scp914ItemProcessor.ProcessResultingItem(replacement.Base, result, ev.KnobSetting);
            }
        }

        private void Remove(Player player)
        {
            lockpicking?.Cancel(player);
            if (!active.TryGetValue(player, out var state))
                return;
            active.Remove(player);
            if (!player.IsConnected || player.Role != state.Role)
                return;
            if (player.Role is FpcRole fpc)
            {
                fpc.StaminaUsageMultiplier /= state.StaminaApplied;
                // Remove only our multiplicative layer, leaving other plugins' changes intact.
                state.Movement.Clear();
            }

            if (player.Role is Scp049Role doctor && state.RegenCooldown.HasValue && doctor.HumeShieldModule is DynamicHumeShieldController hs)
                hs.RegenerationCooldown = state.RegenCooldown.Value;
            player.MaxHealth = state.MaxHealth;
            player.Health = Math.Min(player.Health, player.MaxHealth);
            player.MaxHumeShield = state.MaxShield;
            player.Scale = state.Scale;
            player.CustomInfo = state.Info;
        }

        private IEnumerator<float> Tick()
        {
            while (true)
            {
                yield return Timing.WaitForSeconds(.2f);
                foreach (var pair in active.ToArray())
                    Safe.Run(() => Update(pair.Key, pair.Value));
                Safe.Run(() => lockpicking?.Update());
                Safe.Run(() => roleHud?.Update());
                Safe.Run(() => bursts.Prune());
            }
        }

        private void Update(Player player, ActiveRole state)
        {
            if (!player.IsAlive || !player.IsConnected || player.Role != state.Role)
            {
                Remove(player);
                return;
            }

            if (!(player.Role is FpcRole fpc))
                return;
            float multiplier = state.Definition.SpeedMultiplier;
            bool nativeBoost = player.GetEffect(EffectType.MovementBoost)?.IsEnabled == true || player.GetEffect(EffectType.Scp207)?.IsEnabled == true || player.GetEffect(EffectType.Invigorated)?.IsEnabled == true;
            if (state.Definition.Id == "Z02" && Config.ExtremeMuncherEventPreset)
                multiplier = 2;
            if (state.Movement.IsOwnBoost(player))
                nativeBoost = player.GetEffect(EffectType.Scp207)?.IsEnabled == true || player.GetEffect(EffectType.Invigorated)?.IsEnabled == true;
            if (player.IsHuman && (nativeBoost || player.CurrentArmor?.Type == ItemType.ArmorHeavy))
                multiplier = 1;
            state.Movement.Set(player, multiplier);
            bool stamina = true;
            if (state.Definition.Id == "G04" && player.CurrentArmor != null)
                stamina = false;
            if (state.Definition.Id == "C02" && player.CurrentArmor != null && player.CurrentArmor.Type != ItemType.ArmorLight)
                stamina = false;
            float nextStamina = 1 / (stamina ? Math.Max(1, state.Definition.SprintDurationMultiplier) : 1);
            fpc.StaminaUsageMultiplier = fpc.StaminaUsageMultiplier / state.StaminaApplied * nextStamina;
            state.StaminaApplied = nextStamina;
            if (player.Role is Scp049Role doctor && doctor.HumeShieldModule is DynamicHumeShieldController shield)
                player.MaxHumeShield = shield.HsMax * state.Definition.ShieldMultiplier;
        }

        private void Hurting(HurtingEventArgs ev)
        {
            if (ev.IsAllowed && ev.Amount > 0)
                lockpicking?.Cancel(ev.Player, "you took damage");
            if (RoleId(ev.Attacker) == "Z02" && ev.DamageHandler.Type == DamageType.Scp0492 && ev.Amount > 0)
                ev.Amount *= .5f;
        }

        private void LockpickDoor(InteractingDoorEventArgs ev) => Safe.Run(() => lockpicking?.DoorInteraction(ev));
        internal bool ReloadTuning(out string response)
        {
            try
            {
                var next = ConfigReload.Read(this);
                next.Prepare();
                if (!next.IsEnabled)
                    throw new InvalidOperationException("Disable the plugin through a full server restart.");
                ConfigReload.Apply(Config, next);
                updates?.Dispose();
                updates = Brainless.Internal.Updates.PluginUpdates.Start(GetType().Assembly, "Roles", Config.AutoUpdate);
                response = "Role settings reloaded. Existing assigned stats/queued explosions keep their state; new spawns/deaths use the updated configuration.";
                return true;
            }
            catch (Exception exception)
            {
                response = "Role reload rejected; active settings retained. " + exception.Message;
                return false;
            }
        }

        private void Dying(DyingEventArgs ev) => Safe.Run(() =>
        {
            pendingBursts.Forget(ev.Player);
            if (!ev.IsAllowed || RoleId(ev.Player) != "Z04")
                return;
            var pending = new PendingBurst
            {
                Event = ev,
                UserId = ev.Player.UserId,
                Position = ev.Player.Position,
                Source = new DamageSource(ev.Player),
                Tuning = Config.ExplosiveZombie,
                HasAttacker = ev.Attacker != null && ev.Attacker != ev.Player,
                Enemy = ev.Attacker != null && ev.Attacker != ev.Player && HitboxIdentity.IsEnemy(ev.Attacker.Role.Type, ev.Player.Role.Type)
            };
            pendingBursts.Capture(ev.Player, pending);
            // A canceled death never reaches Died. Retire that capture next tick.
            lifetime.Later(0, () =>
            {
                pendingBursts.ForgetIfSame(ev.Player, pending);
            });
        });
        private void Died(DiedEventArgs ev) => Safe.Run(() =>
        {
            if (!pendingBursts.Confirm(ev.Player, p => p.Event.IsAllowed && ev.TargetOldRole == RoleTypeId.Scp0492, out var pending))
                return;
            var tuning = pending.Tuning;
            if (!tuning.Allows(pending.HasAttacker, pending.Enemy, pending.Event.DamageHandler.Type == DamageType.Explosion))
                return;
            bursts.Prune();
            int used = burstSpent.TryGetValue(pending.UserId, out int before) ? before : 0;
            if ((tuning.MaxPerPlayerPerRound > 0 && used >= tuning.MaxPerPlayerPerRound) || (tuning.MaxConcurrentBursts > 0 && bursts.Count >= tuning.MaxConcurrentBursts))
                return;
            var grenade = bursts.Launch(pending.Position + Vector3.up * .15f, Vector3.zero, Math.Max(.05f, tuning.WarningSeconds), pending.Source, tuning.Radius, tuning.HumanDamage, tuning.ScpDamage, requireLineOfSight: tuning.RequireLineOfSight, affectOwner: false);
            grenade.Scale = Vector3.one * .001f;
            if (grenade.Rigidbody != null)
                grenade.Rigidbody.isKinematic = true;
            burstSpent[pending.UserId] = used + 1;
            if (tuning.ShowWarning)
                World.Cue(pending.Position, "Explosive corpse - move away!", tuning.WarningRange);
            if (tuning.PlayWarningSound)
                FxAudio.Play(lifetime, pending.Position, tuning.WarningSeconds, "beep", range: tuning.WarningRange);
            if (Config.Debug)
                Log.Info("[Z04] Confirmed death armed a native explosion.");
        });
        private void BurstExploding(Exiled.Events.EventArgs.Map.ExplodingGrenadeEventArgs ev) => Safe.Run(() => bursts.Exploding(ev));
        private sealed class PendingBurst
        {
            internal DyingEventArgs Event;
            internal string UserId;
            internal Vector3 Position;
            internal DamageSource Source;
            internal ExplosiveZombieTuning Tuning;
            internal bool HasAttacker, Enemy;
        }

        private void ConvertDoctor(Player player)
        {
            var definition = Config.Roles.FirstOrDefault(r => r.Id == "Z05");
            if (definition == null || !definition.Enabled)
                return;
            player.Role.Set(RoleTypeId.Scp049, RoleSpawnFlags.None);
            int generation = Generation(player);
            lifetime.Later(.1f, () =>
            {
                if (player.IsConnected && player.IsAlive && player.Role.Type == RoleTypeId.Scp049 && Generation(player) == generation)
                    Apply(player, definition, false);
            });
        }

        private void StartingRecall(StartingRecallEventArgs ev)
        {
            if (RoleId(ev.Player) == "Z05" && (doctorSpent.Contains(ev.Player.UserId) || ev.Ragdoll.Role != RoleTypeId.Scp0492))
                ev.IsAllowed = false;
        }

        private void FinishingRecall(FinishingRecallEventArgs ev)
        {
            if (RoleId(ev.Player) == "Z05" && ev.IsAllowed)
            {
                if (doctorSpent.Contains(ev.Player.UserId) || ev.Ragdoll.Role != RoleTypeId.Scp0492)
                    ev.IsAllowed = false;
                else
                    doctorSpent.Add(ev.Player.UserId);
            }
        }

        private sealed class ActiveRole
        {
            internal RoleDefinition Definition;
            internal Role Role;
            internal float MaxHealth, MaxShield, Shield, Stamina, StaminaApplied = 1, Start;
            internal float? RegenCooldown;
            internal Vector3 Scale;
            internal string Info;
            internal MovementLayer Movement = new MovementLayer();
            internal ActiveRole(Player p, RoleDefinition r)
            {
                Definition = r;
                Role = p.Role;
                MaxHealth = p.MaxHealth;
                MaxShield = p.MaxHumeShield;
                Shield = p.HumeShield;
                Stamina = (p.Role as FpcRole)?.StaminaUsageMultiplier ?? 1;
                Scale = p.Scale;
                Info = p.CustomInfo;
                Start = Time.time;
            }
        }
    }

    [HarmonyPatch(typeof(Scp049ResurrectAbility), "Duration", MethodType.Getter)]
    internal static class SurgeonDuration
    {
        private static void Postfix(Scp049ResurrectAbility __instance, ref float __result)
        {
            if (Plugin.Instance?.RoleId(Player.Get(__instance.Owner)) == "P01")
                __result *= .75f;
        }
    }

    [HarmonyPatch(typeof(Scp049AttackAbility), "ServerProcessCmd")]
    internal static class TicklerCooldown
    {
        private static void Postfix(Scp049AttackAbility __instance)
        {
            var player = Player.Get(__instance.Owner);
            if (Plugin.Instance?.RoleId(player) == "P03" && player.Role is Scp049Role doctor && doctor.RemainingAttackCooldown > Scp049AttackAbility.CooldownTime - .1f)
                doctor.RemainingAttackCooldown = Scp049AttackAbility.CooldownTime * .9f;
        }
    }
}
