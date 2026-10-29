using System;
using System.Collections.Generic;
using System.Linq;
using Brainless.Internal;
using CommandSystem;
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
        public override Version Version => new Version(0, 1, 0);
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
        private readonly HashSet<string> burstSpent = new HashSet<string>();
        private readonly HashSet<string> doctorSpent = new HashSet<string>();
        private readonly Dictionary<int, int> badges = new Dictionary<int, int>();
        private bool burstArmed, doctorRolled;
        private float waveStart;
        private int waveCount;
        private Harmony harmony;
        public override void OnEnabled()
        {
            Instance = this;
            harmony = new Harmony("brainless.roles.0.1");
            harmony.PatchAll();
            P.ChangingRole += ChangingRole;
            P.Spawned += Spawned;
            P.Dying += Dying;
            P.Hurting += Hurting;
            P.Left += Left;
            S.WaitingForPlayers += Reset;
            S.RoundEnded += Ended;
            D.StartingRecall += StartingRecall;
            D.FinishingRecall += FinishingRecall;
            U.UpgradingPickup += UpgradePickup;
            U.UpgradingInventoryItem += UpgradeInventory;
            lifetime.Run(Tick());
            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            P.ChangingRole -= ChangingRole;
            P.Spawned -= Spawned;
            P.Dying -= Dying;
            P.Hurting -= Hurting;
            P.Left -= Left;
            S.WaitingForPlayers -= Reset;
            S.RoundEnded -= Ended;
            D.StartingRecall -= StartingRecall;
            D.FinishingRecall -= FinishingRecall;
            U.UpgradingPickup -= UpgradePickup;
            U.UpgradingInventoryItem -= UpgradeInventory;
            foreach (var player in active.Keys.ToArray())
                Remove(player);
            lifetime.Clear();
            harmony?.UnpatchAll(harmony.Id);
            Instance = null;
            base.OnDisabled();
        }

        private void Ended(Exiled.Events.EventArgs.Server.RoundEndedEventArgs ev) => Reset();
        private void Reset()
        {
            foreach (var player in active.Keys.ToArray())
                Remove(player);
            lifetime.Clear();
            generations.Clear();
            reasons.Clear();
            zombieHistory.Clear();
            issued.Clear();
            burstSpent.Clear();
            doctorSpent.Clear();
            badges.Clear();
            burstArmed = false;
            doctorRolled = false;
            waveCount = 0;
            if (Config.IsEnabled)
                lifetime.Run(Tick());
        }

        private void Left(LeftEventArgs ev)
        {
            Remove(ev.Player);
            generations.Remove(ev.Player);
            reasons.Remove(ev.Player);
        }

        private void ChangingRole(ChangingRoleEventArgs ev) => Safe.Run(() =>
        {
            if (!ev.IsAllowed)
                return;
            Remove(ev.Player);
            generations[ev.Player] = Generation(ev.Player) + 1;
            reasons[ev.Player] = ev.Reason;
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
                return;
            if (player.Role.Type == RoleTypeId.ClassD && Config.GiveClassDCoins && !player.Items.Any(i => i.Type == ItemType.Coin))
                player.AddItem(ItemType.Coin);
            if (player.Role.Type == RoleTypeId.Scp0492 && zombieHistory.TryGetValue(player.UserId, out var previous))
            {
                Apply(player, previous, true);
                return;
            }

            if (player.Role.Type == RoleTypeId.Scp0492 && Config.EnableDoctorEvent && Config.AllowExperimentalRoles && !doctorRolled && Player.List.Count >= 12 && Player.List.Any(p => p.Role.Type == RoleTypeId.Scp049))
            {
                doctorRolled = true;
                if (random.NextDouble() < .01)
                {
                    ConvertDoctor(player);
                    return;
                }
            }

            if (random.NextDouble() * 100 >= Chance(player))
                return;
            var pool = Config.Roles.Where(r => r.Enabled && r.Id != "Z05" && (!r.Experimental || Config.AllowExperimentalRoles) && Matches(r, player)).ToList();
            int index = Rules.WeightedIndex(pool.Select(r => r.Weight).ToArray(), random);
            if (index < 0)
                return;
            var selected = pool[index];
            if (!CanApply(player, selected))
                return;
            Apply(player, selected, false);
        }

        private bool CanApply(Player player, RoleDefinition definition)
        {
            int population = Player.List.Count(p => p.IsConnected && !p.IsHost);
            if (population < definition.MinPlayers || Count(definition.Id) >= definition.MaxPerRound)
                return false;
            if (definition.Id == "C03" && !player.Items.Any(i => i.Type == ItemType.GunAK))
                return false;
            if (player.IsHuman)
            {
                int living = Player.List.Count(p => p.IsHuman && p.IsAlive);
                if (active.Count(p => p.Key.IsHuman && p.Key.IsAlive) >= Math.Max(1, living / 2))
                    return false;
                if (population < 12 && player.Role.Type == RoleTypeId.FacilityGuard && active.Any(p => p.Key.Role.Type == RoleTypeId.FacilityGuard))
                    return false;
                if (definition.Id.StartsWith("M") || definition.Id.StartsWith("C"))
                {
                    if (Time.time - waveStart > 8)
                    {
                        waveStart = Time.time;
                        waveCount = 0;
                    }

                    if (waveCount >= Rules.WaveCap(population) || active.Any(p => p.Value.Definition.Id == definition.Id && Time.time - p.Value.Start < 8))
                        return false;
                }
            }

            var custom = definition.CustomItems.Select(FindCustom).ToArray();
            if (custom.Any(i => i == null))
                return false;
            foreach (var group in custom.GroupBy(i => i.GetType().Assembly))
            {
                var item = group.First();
                var method = item.GetType().GetMethod("CanIssueBatch", new[] { typeof(uint[]) });
                if (method == null || !(bool)method.Invoke(item, new object[] { definition.CustomItems.ToArray() }))
                    return false;
            }

            return true;
        }

        private CustomItem FindCustom(uint id) => CustomItem.Registered.FirstOrDefault(i => i.Id == id);
        private bool CanGive(CustomItem item, int count)
        {
            if (item == null)
                return false;
            var method = item.GetType().GetMethod("CanIssue", new[] { typeof(int) });
            return method != null && (bool)method.Invoke(item, new object[] { count });
        }

        private int Count(string id) => issued.TryGetValue(id, out int count) ? count : 0;
        internal string RoleId(Player player) => player != null && active.TryGetValue(player, out var a) ? a.Definition.Id : null;
        private void Apply(Player player, RoleDefinition definition, bool revived)
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
                    issued[definition.Id] = Count(definition.Id) + 1;
                    if (definition.Id.StartsWith("M") || definition.Id.StartsWith("C"))
                        waveCount++;
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
            if (RoleId(ev.Attacker) == "Z02" && ev.DamageHandler.Type == DamageType.Scp0492 && ev.Amount > 0)
                ev.Amount *= .5f;
        }

        private void Dying(DyingEventArgs ev) => Safe.Run(() =>
        {
            if (!ev.IsAllowed || RoleId(ev.Player) != "Z04" || ev.Attacker == null || !HitboxIdentity.IsEnemy(ev.Attacker.Role.Type, ev.Player.Role.Type) || burstSpent.Contains(ev.Player.UserId) || burstArmed || ev.DamageHandler.Type == DamageType.Explosion)
                return;
            string userId = ev.Player.UserId;
            Vector3 position = ev.Player.Position;
            var source = new DamageSource(ev.Player);
            // Wait for all death handlers and the native death operation before arming.
            lifetime.Later(0, () =>
            {
                if (!ev.IsAllowed || ev.Player.IsAlive || burstSpent.Contains(userId) || burstArmed)
                    return;
                burstSpent.Add(userId);
                burstArmed = true;
                World.Cue(position, "Explosive corpse - move away!", 10);
                FxAudio.Play(lifetime, position, 2, "beep");
                lifetime.Later(2, () =>
                {
                    burstArmed = false;
                    foreach (var target in Player.List.Where(p => p.IsHuman && source.CanAffect(p)).ToArray())
                    {
                        float amount = Rules.Falloff(Vector3.Distance(target.Position, position), 3, 80);
                        if (amount > 0 && World.ClearLine(position + Vector3.up * .2f, target.Position))
                            source.Blast(target, position, amount);
                    }
                });
            });
        });
        private void ConvertDoctor(Player player)
        {
            var definition = Config.Roles.FirstOrDefault(r => r.Id == "Z05");
            if (definition == null)
                return;
            player.Role.Set(RoleTypeId.Scp049, RoleSpawnFlags.None);
            definition.HealthMultiplier = .4f;
            definition.ShieldMultiplier = .5f;
            lifetime.Later(.1f, () => Apply(player, definition, false));
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

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public sealed class RolesCommand : ICommand
    {
        public string Command => "bproles";
        public string[] Aliases => Array.Empty<string>();
        public string Description => "List configured Brainless role IDs and enabled status.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (!Exiled.Permissions.Extensions.Permissions.CheckPermission(sender, "brainless.roles"))
            {
                response = "Missing brainless.roles permission.";
                return false;
            }

            response = string.Join("\n", Plugin.Instance.Config.Roles.Select(r => $"{r.Id}: {r.Name} enabled={r.Enabled} experimental={r.Experimental}"));
            return true;
        }
    }
}
