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
using InventorySystem.Items.Usables.Scp330;
using MEC;
using PlayerRoles;
using PlayerRoles.PlayableScps.HumeShield;
using PlayerRoles.PlayableScps.Scp049;
using UnityEngine;
using P = Exiled.Events.Handlers.Player;
using S = Exiled.Events.Handlers.Server;
using U = Exiled.Events.Handlers.Scp914;
using Random = System.Random;
using Scp049Role = Exiled.API.Features.Roles.Scp049Role;
using FxAudio = Brainless.Internal.Audio;

namespace Brainless.Roles
{
    public sealed partial class Plugin : Plugin<Config>
    {
        public override string Name => "BrainlessRoles";
        public override string Prefix => "brainless_roles";
        public override string Author => "Brainless Project";
        public override Version Version => new Version(0, 8, 0);
        public override Version RequiredExiledVersion => new Version(9, 15, 3);
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
        private readonly Dictionary<int, int> badges = new Dictionary<int, int>();
        private bool doctorRolled;
        private float waveStart;
        private int waveCount;
        private Harmony harmony;
        private RoleHud roleHud;
        private Lockpicking lockpicking;
        private IDisposable updates;
        public override void OnEnabled()
        {
            bool migrateConfig = Config.RoleCatalogRevision < 5 || Config.RoleHudLayoutRevision < 2;
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
            SubscribeVariants();
            P.InteractingDoor += LockpickDoor;
            P.Left += Left;
            S.WaitingForPlayers += Reset;
            S.RoundEnded += Ended;
            U.UpgradingPickup += UpgradePickup;
            U.UpgradingInventoryItem += UpgradeInventory;
            lifetime.Run(Tick());
            if (migrateConfig)
                ConfigMigration.Save(this);
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
            UnsubscribeVariants();
            P.InteractingDoor -= LockpickDoor;
            P.Left -= Left;
            S.WaitingForPlayers -= Reset;
            S.RoundEnded -= Ended;
            U.UpgradingPickup -= UpgradePickup;
            U.UpgradingInventoryItem -= UpgradeInventory;
            foreach (var player in active.Keys.ToArray())
            {
                Remove(player);
            }

            bursts.Clear();
            pendingBursts.Clear();
            poisons.Clear();
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
            poisons.Clear();
            reasons.Clear();
            zombieHistory.Clear();
            issued.Clear();
            assignmentNotes.Clear();
            burstSpent.Clear();
            badges.Clear();
            doctorRolled = false;
            rogueAlertQueued = false;
            waveCount = 0;
            if (Config.IsEnabled)
                lifetime.Run(Tick());
        }

        private void Left(LeftEventArgs ev)
        {
            ForgetPoisons(ev.Player);
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
            poisons.Remove(ev.Player);
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

        private bool Matches(RoleDefinition role, Player player) => Catalog.MatchesNative(role, player.Role.Type);
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

            var doctorEvent = Config.Roles.FirstOrDefault(r => r.Id == "Z05" && r.Enabled && r.ChancePercent > 0);
            if (player.Role.Type == RoleTypeId.Scp0492 && Config.EnableDoctorEvent && doctorEvent != null && (!doctorEvent.Experimental || Config.AllowExperimentalRoles) && !doctorRolled && Player.List.Count(p => p.IsConnected && !p.IsHost) >= doctorEvent.MinPlayers && Count("Z05") < doctorEvent.MaxPerRound && Player.List.Any(p => p.Role.Type == RoleTypeId.Scp049))
            {
                doctorRolled = true;
                if (random.NextDouble() * 100 < doctorEvent.ChancePercent.Value)
                {
                    ConvertDoctor(player);
                    return;
                }
            }

            var selected = RoleChances.Choose(Config.Roles.Where(r => r.Id != "Z05" && Matches(r, player)), r => r.ChancePercent.Value, random.NextDouble() * 100);
            if (selected == null)
            {
                assignmentNotes[player] = "Normal role: the roll landed in the unused percentage. Run bproles " + player.Id + " for configured chances.";
                return;
            }

            string blocked = Availability(selected, player);
            if (blocked != null)
            {
                assignmentNotes[player] = "Normal role: rolled " + selected.Id + " (" + selected.ChancePercent.Value.ToString("0.######") + "%), but " + blocked + ".";
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
            if (!admin && definition.ChancePercent <= 0)
                return "chance_percent is 0";
            int population = Player.List.Count(p => p.IsConnected && !p.IsHost);
            if (!admin && population < definition.MinPlayers)
                return "requires " + definition.MinPlayers + " players; currently " + population;
            if (!admin && Count(definition.Id) >= definition.MaxPerRound)
                return "per-round role cap reached";
            if (player != null && !Matches(definition, player))
                return "requires " + definition.BaseRole + " or matching faction";
            if (player != null && definition.SpawnArea != RoleSpawnArea.Normal && !TryProfileSpawn(player, definition.SpawnArea, out _))
                return "no safe " + definition.SpawnArea + " spawn is available";
            if (!admin && player != null && player.IsHuman)
            {
                int living = Player.List.Count(p => p.IsHuman && p.IsAlive);
                int cap = Math.Max(1, (int)Math.Ceiling(living * Config.CustomHumanSharePercent / 100));
                if (active.Count(p => p.Key.IsHuman && p.Key.IsAlive) >= cap)
                    return "living-human specialist cap reached";
                if (population < 12 && player.Role.Type == RoleTypeId.FacilityGuard && active.Any(p => p.Key.Role.Type == RoleTypeId.FacilityGuard))
                    return "small-lobby Guard specialist cap reached";
                if (definition.Id.StartsWith("M") || definition.Id.StartsWith("C"))
                {
                    int currentWaveCount = Time.time - waveStart > 8 ? 0 : waveCount;
                    bool waveFull = Config.ReinforcementSpecialistsPerWave > 0 && currentWaveCount >= Config.ReinforcementSpecialistsPerWave;
                    if (waveFull || active.Any(p => p.Value.Definition.Id == definition.Id && Time.time - p.Value.Start < 8))
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
                int extra = 0;
                if (definition.Id == "D03")
                    extra++;
                if (definition.Id == "S04" || definition.Id == "C12")
                    extra++;
                if (definition.Id == "C07" || definition.Id == "M14" || definition.Id == "C10" || definition.Id == "C11")
                    extra++;
                if (definition.InventorySlots(player.Items.Count, removed, extra) > 8)
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
            var definition = Config.Roles.FirstOrDefault(r => RoleLookup.Matches(r.Id, r.Name, id));
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
            RoleTypeId fallbackRole = player.Role.Type;
            Vector3? customSpawn = null;
            if (!revived && definition.SpawnArea != RoleSpawnArea.Normal)
            {
                if (!TryProfileSpawn(player, definition.SpawnArea, out Vector3 destination))
                    throw new InvalidOperationException("No safe spawn is available for " + definition.Name + ".");
                customSpawn = destination;
            }

            if (!revived && definition.SpawnAs != RoleTypeId.None && definition.SpawnAs != player.Role.Type)
            {
                // Change role in place (keeps the spawn position, e.g. the Class-D cells).
                player.Role.Set(definition.SpawnAs, SpawnReason.ForceClass, RoleSpawnFlags.None);
                if (player.Role.Type != definition.SpawnAs)
                    throw new InvalidOperationException("The role change to " + definition.SpawnAs + " was blocked.");
            }

            var state = new ActiveRole(player, definition);
            active[player] = state;
            try
            {
                if (!revived)
                {
                    if (definition.ReplaceInventory)
                        player.ClearInventory();
                    foreach (var type in definition.Remove)
                        foreach (var item in player.Items.Where(i => i.Type == type).ToArray())
                            player.RemoveItem(item);
                    foreach (var type in definition.Add)
                        if (player.AddItem(type) is Firearm added)
                            added.MagazineAmmo = added.MaxMagazineAmmo;
                    if (definition.Id == "C04")
                    {
                        var item = player.Items.FirstOrDefault(i => i.Type == ItemType.GrenadeHE) ?? player.Items.FirstOrDefault(i => i.Type == ItemType.Medkit);
                        if (item != null)
                            player.RemoveItem(item);
                    }

                    foreach (var ammo in player.Ammo.ToArray())
                        player.SetAmmo(ammo.Key.GetAmmoType(), (ushort)(ammo.Value * definition.ReserveMultiplier));
                    if (definition.Id == "S01")
                        player.SetAmmo(AmmoType.Ammo44Cal, (ushort)Math.Max((int)player.GetAmmo(AmmoType.Ammo44Cal), 6));
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
                    if (definition.Id == "S04" || definition.Id == "C12")
                        Candy(player, Config.CandyManCandyCount);
                    if (definition.Id == "C07" || definition.Id == "M14")
                        GiveSquadCard(player);
                    if (definition.Id == "C10" && random.NextDouble() < .3)
                        player.AddItem(ItemType.SCP1344);
                    if (definition.Id == "C11")
                        player.AddItem(Catalog.ScpItems[random.Next(Catalog.ScpItems.Length)]);
                    if (definition.Id == "G02")
                    {
                        player.ClearAmmo();
                        var gun = player.Items.OfType<Firearm>().FirstOrDefault();
                        if (gun != null)
                            player.SetAmmo(gun.AmmoType, (ushort)gun.MaxMagazineAmmo);
                    }

                    if (definition.FillAmmoToLimit)
                        foreach (var gun in player.Items.OfType<Firearm>())
                        {
                            gun.MagazineAmmo = gun.MaxMagazineAmmo;
                            player.SetAmmo(gun.AmmoType, player.GetAmmoLimit(gun.AmmoType));
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
                ApplyShield(player, state, true);

                if ((definition.Scale != 1 || definition.WidthScale != 1) && Config.AllowModelScaling)
                    player.Scale = Vector3.Scale(state.Scale, new Vector3(definition.Scale * definition.WidthScale, definition.Scale, definition.Scale * definition.WidthScale));
                if (definition.Id == "Z02" && Config.ExtremeMuncherEventPreset)
                    player.Scale = new Vector3(state.Scale.x, state.Scale.y * .5f, state.Scale.z);
                if (player.Role is IHumeShieldRole shieldRole && shieldRole.HumeShieldModule is DynamicHumeShieldController hs && !NoShield(definition))
                {
                    state.RegenCooldown = hs.RegenerationCooldown;
                    state.RegenRate = hs.RegenerationRate;
                    hs.RegenerationCooldown *= definition.Traits.ShieldRegenDelayMultiplier;
                    hs.RegenerationRate *= definition.Traits.ShieldRegenRateMultiplier;
                }

                if (player.Role.Type == RoleTypeId.Scp0492)
                    zombieHistory[player.UserId] = definition;
                player.CustomInfo = ValidCustomInfo(definition.Name);
                if (definition.Neutral)
                    ConfigureRogue(player, state);
                if (customSpawn.HasValue)
                    player.Position = customSpawn.Value;
                assignmentNotes[player] = admin ? "Assigned by admin test command." : revived ? "Restored after resurrection." : "Assigned by normal spawn roll.";
                player.ShowHint($"<b>{definition.Name}</b>\n{definition.Hint}", 10);
            }
            catch
            {
                Remove(player);
                // A provider can be disabled after preflight. Restore a complete native
                // kit instead of leaving a partially modified inventory behind.
                if (player.IsConnected && player.IsAlive)
                    player.Role.Set(fallbackRole, RoleSpawnFlags.AssignInventory);
                throw;
            }
        }

        private void Candy(Player player, int count)
        {
            var standard = new[]
            {
                CandyKindID.Rainbow,
                CandyKindID.Yellow,
                CandyKindID.Purple,
                CandyKindID.Red,
                CandyKindID.Green,
                CandyKindID.Blue
            };
            var pool = CandyPool.Resolve(Config.ApprovedCandy, Scp330.AvailableCandies.Keys.Where(c => c != CandyKindID.None), standard);
            if (pool.Length == 0)
                throw new InvalidOperationException("No configured SCP-330 candy is registered in this game build.");
            var bag = player.Items.OfType<Scp330>().FirstOrDefault() ?? player.AddItem(ItemType.SCP330) as Scp330;
            if (bag == null)
                throw new InvalidOperationException("The server could not give the SCP-330 bag.");
            // Native creation can seed a random candy. Replace that seed so the
            // role receives exactly its configured count, with no extra color.
            bag.Base.Candies.Clear();
            for (int i = 0; i < count; i++)
                if (!bag.AddCandy(pool[random.Next(pool.Length)]))
                    throw new InvalidOperationException("The native candy bag rejected a role candy.");
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
            // Replace only native cards. A custom keycard body (Door Jammer, Hacking
            // Chip) belongs to the role kit and must survive the badge swap.
            foreach (var item in player.Items.OfType<Keycard>().Where(i => !CustomItem.Registered.Any(c => c.TrackedSerials.Contains(i.Serial))).ToArray())
                player.RemoveItem(item);
            int tier = id == "G01" ? 2 : id == "G02" || id == "G04" ? 0 : 1;
            var card = (Exiled.API.Features.Items.Keycards.CustomKeycardItem)player.AddItem(ItemType.KeycardCustomTaskForce);
            card.KeycardLevels = new Interactables.Interobjects.DoorUtils.KeycardLevels(tier > 0 ? 1 : 0, tier == 2 ? 2 : 1, 1, true);
            card.ItemName = id == "G01" ? "Guard Captain Badge" : "Facility Security Badge";
            badges[card.Serial] = tier;
        }

        // Squad Leaders: every door except the Alpha Warhead (Containment 3, Armory 3, Admin 2).
        private static void GiveSquadCard(Player player)
        {
            var card = (Exiled.API.Features.Items.Keycards.CustomKeycardItem)player.AddItem(ItemType.KeycardCustomTaskForce);
            card.KeycardLevels = new Interactables.Interobjects.DoorUtils.KeycardLevels(3, 3, 2, true);
            card.ItemName = "Squad Leader Keycard";
        }

        private void UpgradePickup(UpgradingPickupEventArgs ev)
        {
            if (!badges.Remove(ev.Pickup.Serial))
                return;
            ev.IsAllowed = false;
            var replacement = Exiled.API.Features.Pickups.Pickup.CreateAndSpawn(ItemType.KeycardGuard, ev.Pickup.Position);
            ev.Pickup.Destroy();
            Native914.ProcessPickup(replacement.Base, true, ev.KnobSetting);
        }

        private void UpgradeInventory(UpgradingInventoryItemEventArgs ev)
        {
            if (!badges.Remove(ev.Item.Serial))
                return;
            ev.IsAllowed = false;
            ev.Player.RemoveItem(ev.Item);
            var replacement = ev.Player.AddItem(ItemType.KeycardGuard);
            Native914.ProcessInventory(replacement.Base, ev.KnobSetting);
        }

        private void Remove(Player player)
        {
            lockpicking?.Cancel(player);
            if (!active.TryGetValue(player, out var state))
                return;
            active.Remove(player);
            RemoveRogue(player, state);
            if (!player.IsConnected || player.Role != state.Role)
                return;
            state.QuietSteps.Clear();
            if (player.Role is FpcRole fpc)
            {
                fpc.StaminaUsageMultiplier /= state.StaminaApplied;
                // Remove only our multiplicative layer, leaving other plugins' changes intact.
                state.Movement.Clear();
            }

            if (player.Role is IHumeShieldRole shieldRole && state.RegenCooldown.HasValue && shieldRole.HumeShieldModule is DynamicHumeShieldController hs)
            {
                if (hs.RegenerationCooldown == state.RegenCooldown.Value * state.Definition.Traits.ShieldRegenDelayMultiplier)
                    hs.RegenerationCooldown = state.RegenCooldown.Value;
                if (state.RegenRate.HasValue && hs.RegenerationRate == state.RegenRate.Value * state.Definition.Traits.ShieldRegenRateMultiplier)
                    hs.RegenerationRate = state.RegenRate.Value;
            }

            player.MaxHealth = state.MaxHealth;
            player.Health = Math.Min(player.Health, player.MaxHealth);
            // A negative maximum clears the stat override so the native shield returns.
            if (state.ShieldOverridden)
                player.MaxHumeShield = -1;
            if (player.Scale != state.Scale)
                player.Scale = state.Scale;
            player.CustomInfo = ValidCustomInfo(state.Info);
        }

        private static string ValidCustomInfo(string value) => string.IsNullOrEmpty(value) || NicknameSync.ValidateCustomInfo(value, out _) ? value ?? "" : "";
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
                Safe.Run(UpdatePoisons);
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
            if (player.Role is Exiled.API.Features.Roles.Scp096Role shy && shy.RageState == PlayerRoles.PlayableScps.Scp096.Scp096RageState.Docile)
                multiplier *= state.Definition.Traits.CalmSpeedMultiplier;
            bool nativeBoost = player.GetEffect(EffectType.MovementBoost)?.IsEnabled == true || player.GetEffect(EffectType.Scp207)?.IsEnabled == true || player.GetEffect(EffectType.Invigorated)?.IsEnabled == true;
            if (state.Definition.Id == "Z02" && Config.ExtremeMuncherEventPreset)
                multiplier = 2;
            if (state.Movement.IsOwnBoost(player))
                nativeBoost = player.GetEffect(EffectType.Scp207)?.IsEnabled == true || player.GetEffect(EffectType.Invigorated)?.IsEnabled == true;
            if (player.IsHuman && (nativeBoost || player.CurrentArmor?.Type == ItemType.ArmorHeavy))
                multiplier = 1;
            if (state.ResilientActive)
                multiplier = Math.Max(multiplier, 1) * Config.Resilient.SpeedMultiplier;
            state.Movement.Set(player, multiplier);
            state.QuietSteps.Set(player, state.Definition.Traits.QuietFootstepStacks);
            bool stamina = true;
            if (state.Definition.Id == "G04" && player.CurrentArmor != null)
                stamina = false;
            if (state.Definition.Id == "C02" && player.CurrentArmor != null && player.CurrentArmor.Type != ItemType.ArmorLight)
                stamina = false;
            float nextStamina = 1 / (stamina ? Math.Max(1, state.Definition.SprintDurationMultiplier) : 1);
            fpc.StaminaUsageMultiplier = fpc.StaminaUsageMultiplier / state.StaminaApplied * nextStamina;
            state.StaminaApplied = nextStamina;
            ApplyShield(player, state, false);
            if (state.Definition.Traits.RefreshBreakneckOnKill)
                ClearRefreshedCooldown(player, state);
        }

        private static bool NoShield(RoleDefinition definition) => definition.Id == "Z05" || definition.ShieldMultiplier <= 0;

        // Leave the native, dynamic shield alone unless the profile scales it. A
        // scaled shield follows the module's live maximum; the Doctor never has one.
        private static void ApplyShield(Player player, ActiveRole state, bool refill)
        {
            if (!(player.Role is IHumeShieldRole shieldRole) || shieldRole.HumeShieldModule == null)
                return;
            var definition = state.Definition;
            if (NoShield(definition))
            {
                state.ShieldOverridden = true;
                player.MaxHumeShield = 0;
                player.HumeShield = 0;
                return;
            }

            if (definition.ShieldMultiplier == 1)
                return;
            state.ShieldOverridden = true;
            player.MaxHumeShield = shieldRole.HumeShieldModule.HsMax * definition.ShieldMultiplier;
            player.HumeShield = refill ? Math.Min(player.MaxHumeShield, state.Shield * definition.ShieldMultiplier) : Math.Min(player.HumeShield, player.MaxHumeShield);
        }

        private void Hurting(HurtingEventArgs ev)
        {
            if (ev.IsAllowed && ev.Amount > 0)
                lockpicking?.Cancel(ev.Player, "you took damage");
            if (Traits(ev.Player)?.ExplosionImmune == true && ev.DamageHandler.Type == DamageType.Explosion)
                ev.IsAllowed = false;
            if (ev.IsAllowed && ev.Amount > 0 && IsScpAttack(ev.DamageHandler.Type))
                ev.Amount *= Traits(ev.Attacker)?.AttackDamageMultiplier ?? 1;
            if (ev.IsAllowed && ev.Amount > 0 && ev.Attacker != null && ev.Attacker != ev.Player && active.TryGetValue(ev.Attacker, out var attacker) && attacker.ResilientActive && ev.Attacker.Role == attacker.Role)
                ev.Amount *= Config.Resilient.DamageMultiplier;
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
            string profile = RoleId(ev.Player);
            if (!ev.IsAllowed || (profile != "Z04" && profile != "B04"))
                return;
            var pending = new PendingBurst
            {
                Event = ev,
                UserId = ev.Player.UserId,
                Position = ev.Player.Position,
                Source = new DamageSource(ev.Player),
                Tuning = profile == "B04" ? Config.ExplosiveScp173 : Config.ExplosiveZombie,
                NativeRole = ev.Player.Role.Type,
                Profile = profile,
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
            poisons.Remove(ev.Player);
            Safe.Run(() => RefreshBreakneck(ev));
            if (!pendingBursts.Confirm(ev.Player, p => p.Event.IsAllowed && ev.TargetOldRole == p.NativeRole, out var pending))
                return;
            var tuning = pending.Tuning;
            if (!tuning.Allows(pending.HasAttacker, pending.Enemy, pending.Event.DamageHandler.Type == DamageType.Explosion))
                return;
            bursts.Prune();
            string burstKey = pending.UserId + ":" + pending.Profile;
            int used = burstSpent.TryGetValue(burstKey, out int before) ? before : 0;
            if ((tuning.MaxPerPlayerPerRound > 0 && used >= tuning.MaxPerPlayerPerRound) || (tuning.MaxConcurrentBursts > 0 && bursts.Count >= tuning.MaxConcurrentBursts))
                return;
            bursts.Launch(pending.Position + Vector3.up * .15f, Vector3.zero, Math.Max(.05f, tuning.WarningSeconds), pending.Source, tuning.Radius, tuning.HumanDamage, tuning.ScpDamage, requireLineOfSight: tuning.RequireLineOfSight, affectOwner: false, armorPenetration: tuning.ArmorPenetration, scale: Vector3.one * .001f, stationary: true);
            burstSpent[burstKey] = used + 1;
            if (tuning.ShowWarning)
                World.Cue(pending.Position, "Explosive corpse - move away!", tuning.WarningRange);
            if (tuning.PlayWarningSound)
                FxAudio.Play(lifetime, pending.Position, tuning.WarningSeconds, "beep", range: tuning.WarningRange);
            if (Config.Debug)
                Log.Info("[" + pending.Profile + "] Confirmed death armed a native explosion.");
        });
        private void BurstExploding(Exiled.Events.EventArgs.Map.ExplodingGrenadeEventArgs ev) => Safe.Run(() => bursts.Exploding(ev));
        private sealed class PendingBurst
        {
            internal DyingEventArgs Event;
            internal string UserId;
            internal Vector3 Position;
            internal DamageSource Source;
            internal ExplosiveZombieTuning Tuning;
            internal RoleTypeId NativeRole;
            internal string Profile;
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

        private sealed class ActiveRole
        {
            internal RoleDefinition Definition;
            internal Role Role;
            internal float MaxHealth, MaxShield, Shield, Stamina, StaminaApplied = 1, Start;
            internal float? RegenCooldown, RegenRate;
            internal QuietStepLayer QuietSteps = new QuietStepLayer();
            internal string PreviousUniqueRole;
            internal bool ShieldOverridden, BreakneckKill, ResilientActive;
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
            __result *= Plugin.Instance?.Traits(Player.Get(__instance.Owner))?.RecallDurationMultiplier ?? 1;
        }
    }

    [HarmonyPatch(typeof(Scp049AttackAbility), "ServerProcessCmd")]
    internal static class TicklerCooldown
    {
        private static void Postfix(Scp049AttackAbility __instance)
        {
            var player = Player.Get(__instance.Owner);
            float factor = Plugin.Instance?.Traits(player)?.AttackCooldownMultiplier ?? 1;
            if (factor != 1 && player.Role is Scp049Role doctor && doctor.RemainingAttackCooldown > ScpPrivateAccess.AttackCooldown - .1f)
                doctor.RemainingAttackCooldown = ScpPrivateAccess.AttackCooldown * factor;
        }
    }
}
