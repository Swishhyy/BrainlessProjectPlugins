using System;
using System.Collections.Generic;
using System.Linq;
using BetterCoinflips.Configs;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.API.Features.Pickups;
using Exiled.CustomItems.API.Features;
using Exiled.Events.EventArgs.Player;
using PlayerRoles;
using UnityEngine;

namespace BetterCoinflips.Types
{
    // Extra coin outcomes. Each entry carries its own weight, so the list cannot drift
    // out of step with a separate weight array. None of the bad outcomes can kill.
    internal static class CoinExtras
    {
        private static Config Config => Plugin.Instance.Config;
        private static CoinTimings T => Config.Timings ?? new CoinTimings();
        private static Configs.Translations Text => Plugin.Instance.Translation;
        private static readonly System.Random Rd = new();

        internal sealed class Weighted
        {
            internal Func<int> Weight;
            internal Func<CoinFlipEffect> Create;
        }

        private static Weighted W(Func<int> weight, Func<CoinFlipEffect> create) => new() { Weight = weight, Create = create };
        private static Weighted W(Func<int> weight, Func<string> message, Action<Player> execute) => W(weight, () => new CoinFlipEffect(message(), execute));

        // ---- Heads --------------------------------------------------------------------
        internal static readonly List<Weighted> Good = new()
        {
            W(() => Config.SpeedBoostChance, () => Text.SpeedBoostMessage, p => p.EnableEffect(EffectType.MovementBoost, T.SpeedBoostPercent, T.SpeedBoostSeconds)),
            W(() => Config.ShieldUpChance, () => Text.ShieldUpMessage, p => p.AddAhp(T.ShieldUpAhp, T.ShieldUpAhp, 0.4f)),
            W(() => Config.ToughSkinChance, () => Text.ToughSkinMessage, p => Modifiers.Taken(p, 1 - T.ToughSkinPercent / 100, T.ToughSkinSeconds)),
            W(() => Config.SecondWindChance, () => Text.SecondWindMessage, p => p.EnableEffect(EffectType.Invigorated, 1, T.SecondWindSeconds)),
            W(() => Config.AmmoRefillChance, () => Text.AmmoRefillMessage, AmmoRefill),
            W(() => Config.KeycardUpgradeChance, () => Text.KeycardUpgradeMessage, KeycardUpgrade),
            W(() => Config.RandomScpItemChance, () => Text.RandomScpItemMessage, p => Pickup.CreateAndSpawn(ScpItems[Rd.Next(ScpItems.Length)], p.Position, Quaternion.identity)),
            W(() => Config.LuckyCoinChance, () => Text.LuckyCoinMessage.Replace("{flips}", T.LuckyCoinFlips.ToString()), p => AdjustCoin(p, T.LuckyCoinFlips)),
            W(() => Config.SecondChanceChance, () => Text.SecondChanceMessage, p => Modifiers.GrantSecondChance(p)),
            W(() => Config.FullHealChance, () => Text.FullHealMessage, p => p.Health = p.MaxHealth),
            W(() => Config.ArmorUpgradeChance, () => Text.ArmorUpgradeMessage, ArmorUpgrade),
            W(() => Config.GrenadePackChance, () => Text.GrenadePackMessage, p =>
            {
                Pickup.CreateAndSpawn(ItemType.GrenadeHE, p.Position, Quaternion.identity);
                Pickup.CreateAndSpawn(ItemType.GrenadeFlash, p.Position, Quaternion.identity);
            }),
            W(() => Config.ExtraCoinChance, () => Text.ExtraCoinMessage, p => Pickup.CreateAndSpawn(ItemType.Coin, p.Position, Quaternion.identity)),
            W(() => Config.LoadedUpChance, () => Text.LoadedUpMessage, LoadedUp),
            W(() => Config.RegenerationChance, () => Text.RegenerationMessage, Regenerate),
            W(() => Config.SilentStepsChance, () => Text.SilentStepsMessage, p => p.EnableEffect(EffectType.SilentWalk, 10, T.SilentStepsSeconds)),
            W(() => Config.CleanseChance, () => Text.CleanseMessage, Cleanse),
            W(() => Config.MedicAuraChance, () => Text.MedicAuraMessage, p =>
            {
                foreach (var ally in Player.List.Where(a => a.IsAlive && a.Role.Side == p.Role.Side && (a.Position - p.Position).sqrMagnitude <= T.MedicAuraRange * T.MedicAuraRange))
                    ally.Heal(T.MedicAuraHeal);
            }),
            W(() => Config.RallyChance, Rally),
            W(() => Config.ReinforcementChance, Reinforcement),
            W(() => Config.JackpotChance, Jackpot),
        };

        // ---- Tails --------------------------------------------------------------------
        internal static readonly List<Weighted> Bad = new()
        {
            W(() => Config.FrozenChance, () => Text.FrozenMessage, p => p.EnableEffect(EffectType.Ensnared, 1, T.FrozenSeconds)),
            W(() => Config.SlowedChance, () => Text.SlowedMessage, p => p.EnableEffect(EffectType.Slowness, T.SlowedPercent, T.SlowedSeconds)),
            W(() => Config.ExhaustedChance, () => Text.ExhaustedMessage, p => p.EnableEffect(EffectType.Exhausted, 1, T.ExhaustedSeconds)),
            W(() => Config.SinkholeChance, () => Text.SinkholeMessage, p => p.EnableEffect(EffectType.SinkHole, 1, T.SinkholeSeconds)),
            W(() => Config.ButterfingersChance, () => Text.ButterfingersMessage, p => p.DropItems()),
            W(() => Config.DropHeldItemChance, () => Text.DropHeldItemMessage, p => p.DropHeldItem()),
            W(() => Config.LoseAmmoChance, () => Text.LoseAmmoMessage, p => p.ClearAmmo()),
            W(() => Config.EmptyMagazineChance, () => Text.EmptyMagazineMessage, EmptyMagazine),
            W(() => Config.BlindedChance, () => Text.BlindedMessage, p => p.EnableEffect(EffectType.Blinded, 1, T.BlindedSeconds)),
            W(() => Config.DeafenedChance, () => Text.DeafenedMessage, p => p.EnableEffect(EffectType.Deafened, 1, T.DeafenedSeconds)),
            W(() => Config.ConcussedChance, () => Text.ConcussedMessage, p => p.EnableEffect(EffectType.Concussed, 1, T.ConcussedSeconds)),
            W(() => Config.FlashedChance, () => Text.FlashedMessage, p => p.EnableEffect(EffectType.Flashed, 1, T.FlashedSeconds)),
            W(() => Config.LocationLeakChance, () => Text.LocationLeakMessage, LocationLeak),
            W(() => Config.CassieCalloutChance, () => Text.CassieCalloutMessage, CassieCallout),
            W(() => Config.LockedInChance, () => Text.LockedInMessage, LockedIn),
            W(() => Config.RoomBlackoutChance, () => Text.RoomBlackoutMessage, p => p.CurrentRoom?.TurnOffLights(T.RoomBlackoutSeconds)),
            W(() => Config.DropKeycardsChance, () => Text.DropKeycardsMessage, p => Drop(p, p.Items.Where(i => i.IsKeycard))),
            W(() => Config.DropArmorChance, () => Text.DropArmorMessage, p => Drop(p, p.Items.Where(i => i.IsArmor))),
            W(() => Config.CursedMaxHpChance, () => Text.CursedMaxHpMessage, CursedMaxHp),
            W(() => Config.BleedingChance, () => Text.BleedingMessage, p => Hazard(p, EffectType.Bleeding, T.BleedingSeconds)),
            W(() => Config.HypothermiaChance, () => Text.HypothermiaMessage, p => Hazard(p, EffectType.Hypothermia, T.HypothermiaSeconds)),
            W(() => Config.StaminaDrainedChance, () => Text.StaminaDrainedMessage, p => p.Stamina = 0),
            W(() => Config.PoisonedChance, () => Text.PoisonedMessage, p => Hazard(p, EffectType.Poisoned, T.PoisonedSeconds)),
            W(() => Config.HuntedChance, () => Text.HuntedMessage, Hunted),
            W(() => Config.BackToSpawnChance, () => Text.BackToSpawnMessage, BackToSpawn),
            W(() => Config.JunkSwapChance, () => Text.JunkSwapMessage, JunkSwap),
            W(() => Config.TripChance, () => Text.TripMessage, p =>
            {
                p.DropHeldItem();
                p.EnableEffect(EffectType.Ensnared, 1, T.TripSeconds);
            }),
            W(() => Config.WeakArmsChance, () => Text.WeakArmsMessage, p => Modifiers.Dealt(p, 1 - T.WeakArmsPercent / 100, T.WeakArmsSeconds)),
            W(() => Config.GlassJawChance, () => Text.GlassJawMessage, p => Modifiers.Taken(p, 1 + T.GlassJawPercent / 100, T.GlassJawSeconds)),
            W(() => Config.UnluckyCoinChance, () => Text.UnluckyCoinMessage, p => AdjustCoin(p, -1000)),
        };

        private static CoinFlipEffect Rally()
        {
            CoinFlipEffect effect = null;
            effect = new CoinFlipEffect(Text.RallyMessage, p =>
            {
                var allies = Player.List.Where(a => a != p && a.IsAlive && a.Role.Side == p.Role.Side && a.Role.Type != RoleTypeId.Scp079).ToArray();
                if (allies.Length == 0)
                {
                    p.Health = p.MaxHealth;
                    effect.Message = Text.FullHealMessage;
                    return;
                }

                p.Teleport(allies[Rd.Next(allies.Length)].Position);
            });
            return effect;
        }

        // A random spectator respawns next to the flipper in the same class.
        private static CoinFlipEffect Reinforcement()
        {
            CoinFlipEffect effect = null;
            effect = new CoinFlipEffect(Text.FullHealMessage, p =>
            {
                var spectators = Player.List.Where(s => s.IsConnected && !s.IsHost && !s.IsNPC && s.Role.Type == RoleTypeId.Spectator).ToArray();
                if (spectators.Length == 0)
                {
                    p.Health = p.MaxHealth;
                    return;
                }

                var helper = spectators[Rd.Next(spectators.Length)];
                var position = p.Position;
                helper.Role.Set(p.Role.Type, SpawnReason.ForceClass, RoleSpawnFlags.AssignInventory);
                helper.Position = position;
                Plugin.Instance.Later(.3f, () =>
                {
                    if (helper.IsConnected && helper.IsAlive)
                        helper.Position = position;
                });
                effect.Message = Text.ReinforcementMessage.Replace("{player}", helper.Nickname);
                EventHandlers.SendBroadcast(helper, Text.ReinforcementJoinMessage.Replace("{player}", p.Nickname));
            });
            return effect;
        }

        // Several other good extra outcomes at once.
        private static CoinFlipEffect Jackpot()
        {
            CoinFlipEffect effect = null;
            effect = new CoinFlipEffect(Text.JackpotMessage, p =>
            {
                // Never Jackpot itself.
                var pool = Good.Where(g => g.Weight() > 0 && g.Create.Method.Name != nameof(Jackpot)).ToList();
                var parts = new List<string>();
                for (int i = 0; i < Math.Max(1, T.JackpotRewards) && pool.Count > 0; i++)
                {
                    int total = pool.Sum(g => g.Weight());
                    int roll = Rd.Next(total);
                    var pick = pool.First(g => (roll -= g.Weight()) < 0);
                    pool.Remove(pick);
                    var reward = pick.Create();
                    reward.Execute(p);
                    parts.Add(reward.Message);
                }

                effect.Message = Text.JackpotMessage + string.Join(" ", parts);
            });
            return effect;
        }

        private static void ArmorUpgrade(Player player)
        {
            ItemType next = player.CurrentArmor == null ? ItemType.ArmorLight : player.CurrentArmor.Type == ItemType.ArmorLight ? ItemType.ArmorCombat : player.CurrentArmor.Type == ItemType.ArmorCombat ? ItemType.ArmorHeavy : ItemType.None;
            if (next == ItemType.None)
            {
                player.AddAhp(T.ShieldUpAhp, T.ShieldUpAhp, 0.4f);
                return;
            }

            if (player.CurrentArmor != null)
                player.RemoveItem(player.CurrentArmor);
            player.AddItem(next);
        }


        private static void LoadedUp(Player player)
        {
            if (!(player.CurrentItem is Firearm gun))
            {
                AmmoRefill(player);
                return;
            }

            gun.MagazineAmmo = gun.MaxMagazineAmmo;
            player.SetAmmo(gun.AmmoType, player.GetAmmoLimit(gun.AmmoType));
        }

        private static void Regenerate(Player player)
        {
            var life = player.Role.Base;
            for (int second = 1; second <= Math.Max(1, (int)T.RegenerationSeconds); second++)
                Plugin.Instance.Later(second, () =>
                {
                    if (player.IsConnected && player.IsAlive && player.Role.Base == life)
                        player.Health = Math.Min(player.MaxHealth, player.Health + T.RegenerationPerSecond);
                });
        }

        private static readonly EffectType[] Ailments =
        {
            EffectType.Bleeding, EffectType.Poisoned, EffectType.Burned, EffectType.Concussed, EffectType.Blinded, EffectType.Deafened,
            EffectType.Ensnared, EffectType.Exhausted, EffectType.Flashed, EffectType.Hemorrhage, EffectType.Hypothermia, EffectType.SinkHole,
            EffectType.Stained, EffectType.Traumatized, EffectType.Disabled, EffectType.Asphyxiated, EffectType.Corroding, EffectType.AmnesiaVision,
            EffectType.AmnesiaItems, EffectType.Slowness,
        };

        // Removes negative status effects and the coin's own Weak arms / Glass jaw.
        private static void Cleanse(Player player)
        {
            foreach (var ailment in Ailments)
                player.DisableEffect(ailment);
            Modifiers.ClearCurses(player);
        }

        // The 0.1% item wipe is rolled separately, before any other tails outcome.
        internal static CoinFlipEffect ItemWipe() => new(Text.ItemWipeMessage, p => p.ClearInventory());

        private static readonly ItemType[] ScpItems = { ItemType.SCP500, ItemType.SCP207, ItemType.SCP268, ItemType.SCP1853, ItemType.SCP018, ItemType.SCP244a, ItemType.SCP2176 };
        private static readonly ItemType[] Junk = { ItemType.SCP2176, ItemType.Coin, ItemType.Flashlight };
        private static readonly ItemType[] CardLadder =
        {
            ItemType.KeycardJanitor, ItemType.KeycardScientist, ItemType.KeycardResearchCoordinator, ItemType.KeycardZoneManager,
            ItemType.KeycardGuard, ItemType.KeycardMTFPrivate, ItemType.KeycardContainmentEngineer, ItemType.KeycardMTFOperative,
            ItemType.KeycardMTFCaptain, ItemType.KeycardFacilityManager, ItemType.KeycardChaosInsurgency, ItemType.KeycardO5,
        };

        private static bool IsCustom(Item item) => CustomItem.Registered.Any(c => c.TrackedSerials.Contains(item.Serial));

        private static void Drop(Player player, IEnumerable<Item> items)
        {
            foreach (var item in items.ToArray())
                player.DropItem(item);
        }

        private static void AmmoRefill(Player player)
        {
            foreach (var gun in player.Items.OfType<Firearm>())
                player.SetAmmo(gun.AmmoType, player.GetAmmoLimit(gun.AmmoType));
        }

        // The best native keycard moves one step up the ladder; no card gives a Janitor card.
        private static void KeycardUpgrade(Player player)
        {
            var cards = player.Items.Where(i => i.IsKeycard && !IsCustom(i) && Array.IndexOf(CardLadder, i.Type) >= 0).ToArray();
            if (cards.Length == 0)
            {
                player.AddItem(ItemType.KeycardJanitor);
                return;
            }

            var best = cards.OrderByDescending(i => Array.IndexOf(CardLadder, i.Type)).First();
            int next = Array.IndexOf(CardLadder, best.Type) + 1;
            if (next >= CardLadder.Length)
            {
                player.Health = player.MaxHealth;
                return;
            }

            player.RemoveItem(best);
            player.AddItem(CardLadder[next]);
        }

        // Adds or removes flips on the coin being flipped (the held coin).
        private static void AdjustCoin(Player player, int flips)
        {
            if (player.CurrentItem == null || player.CurrentItem.Type != ItemType.Coin)
                return;
            ushort serial = player.CurrentItem.Serial;
            int left = EventHandlers.CoinUses.TryGetValue(serial, out int uses) ? uses : 0;
            EventHandlers.CoinUses[serial] = Math.Max(0, left + flips);
        }

        private static void EmptyMagazine(Player player)
        {
            if (player.CurrentItem is Firearm gun)
            {
                gun.MagazineAmmo = 0;
                gun.BarrelAmmo = 0;
            }
        }

        private static string ZoneName(Player player) => player.Zone switch
        {
            ZoneType.LightContainment => "Light Containment",
            ZoneType.HeavyContainment => "Heavy Containment",
            ZoneType.Entrance => "Entrance Zone",
            ZoneType.Surface => "Surface",
            _ => "the facility",
        };

        private static void LocationLeak(Player player)
        {
            string text = Text.LocationLeakBroadcast.Replace("{player}", player.Nickname).Replace("{zone}", ZoneName(player));
            foreach (var other in Player.List.Where(p => p != player && p.IsConnected && !p.IsHost))
                other.Broadcast(5, text, Broadcast.BroadcastFlags.Normal, false);
        }

        private static void CassieCallout(Player player)
        {
            string zone = player.Zone switch
            {
                ZoneType.LightContainment => "light containment zone",
                ZoneType.HeavyContainment => "heavy containment zone",
                ZoneType.Entrance => "entrance zone",
                ZoneType.Surface => "surface",
                _ => "facility",
            };
            Exiled.API.Features.Cassie.MessageTranslated("security breach detected in " + zone, "Security breach detected in " + ZoneName(player) + ".");
        }

        // Closes and locks the room's ordinary doors (never elevators) for a short time.
        private static void LockedIn(Player player)
        {
            var room = player.CurrentRoom;
            if (room == null)
                return;
            foreach (var door in room.Doors.Where(d => !d.IsElevator && !d.IsGate))
            {
                door.IsOpen = false;
                door.Lock(T.LockedInSeconds, DoorLockType.AdminCommand);
            }
        }

        private static void CursedMaxHp(Player player)
        {
            player.MaxHealth = Math.Max(1, player.MaxHealth - T.CursedMaxHp);
            if (player.Health > player.MaxHealth)
                player.Health = player.MaxHealth;
        }

        // Damage-over-time from the coin can never take the player below 1 HP.
        private static void Hazard(Player player, EffectType effect, float seconds)
        {
            player.EnableEffect(effect, 1, seconds);
            Modifiers.ProtectFromHazards(player, seconds + 1);
        }

        private static void Hunted(Player player)
        {
            string text = Text.HuntedHint.Replace("{player}", player.Nickname).Replace("{zone}", ZoneName(player));
            foreach (var scp in Player.List.Where(p => p.IsAlive && p.IsScp))
                scp.ShowHint(text, 6);
        }

        private static void BackToSpawn(Player player)
        {
            var spawn = player.Role.Type.GetRandomSpawnLocation();
            if (spawn != null && spawn.Position != Vector3.zero)
                player.Teleport(spawn.Position);
        }

        // One random native item (never a custom item or the coin being flipped) becomes junk.
        private static void JunkSwap(Player player)
        {
            var candidates = player.Items.Where(i => i != player.CurrentItem && !IsCustom(i) && !Junk.Contains(i.Type)).ToArray();
            if (candidates.Length == 0)
            {
                player.DropHeldItem();
                return;
            }

            player.RemoveItem(candidates[Rd.Next(candidates.Length)]);
            player.AddItem(Junk[Rd.Next(Junk.Length)]);
        }
    }

    // Timed damage modifiers (Tough skin, Weak arms, Glass jaw), Second chance and the
    // non-lethal guard for coin hazards. Cleared on role change and round reset.
    internal static class Modifiers
    {
        private static readonly Dictionary<Player, (float multiplier, float until)> taken = new();
        private static readonly Dictionary<Player, (float multiplier, float until)> dealt = new();
        private static readonly Dictionary<Player, float> hazardSafeUntil = new();
        private static readonly HashSet<Player> secondChance = new();
        private static readonly Dictionary<Player, float> unkillableUntil = new();
        private static readonly DamageType[] HazardDamage = { DamageType.Bleeding, DamageType.Poison, DamageType.Hypothermia };

        internal static void Taken(Player player, float multiplier, float seconds) => taken[player] = (multiplier, Time.time + seconds);
        internal static void Dealt(Player player, float multiplier, float seconds) => dealt[player] = (multiplier, Time.time + seconds);
        internal static void ProtectFromHazards(Player player, float seconds) => hazardSafeUntil[player] = Time.time + seconds;
        internal static void GrantSecondChance(Player player) => secondChance.Add(player);
        internal static void Unkillable(Player player, float seconds) => unkillableUntil[player] = Time.time + seconds;

        internal static void Hurting(HurtingEventArgs ev)
        {
            if (!ev.IsAllowed || ev.Player == null || ev.Amount <= 0)
                return;
            if (taken.TryGetValue(ev.Player, out var t) && Time.time < t.until)
                ev.Amount *= t.multiplier;
            if (ev.Attacker != null && ev.Attacker != ev.Player && dealt.TryGetValue(ev.Attacker, out var d) && Time.time < d.until)
                ev.Amount *= d.multiplier;
            bool lethal = ev.Amount >= ev.Player.Health;
            if (!lethal)
                return;
            bool hazard = hazardSafeUntil.TryGetValue(ev.Player, out float until) && Time.time < until && HazardDamage.Contains(ev.DamageHandler.Type);
            bool unkillable = unkillableUntil.TryGetValue(ev.Player, out float safe) && Time.time < safe;
            if (hazard || unkillable || secondChance.Remove(ev.Player))
            {
                if (ev.Player.Health <= 1)
                    ev.IsAllowed = false;
                else
                    ev.Amount = ev.Player.Health - 1;
            }
        }

        // Cleanse: drop Weak arms and Glass jaw but keep Tough skin and Second chance.
        internal static void ClearCurses(Player player)
        {
            dealt.Remove(player);
            if (taken.TryGetValue(player, out var t) && t.multiplier > 1)
                taken.Remove(player);
        }

        internal static void Forget(Player player)
        {
            taken.Remove(player);
            dealt.Remove(player);
            hazardSafeUntil.Remove(player);
            secondChance.Remove(player);
            unkillableUntil.Remove(player);
        }

        internal static void ChangingRole(ChangingRoleEventArgs ev)
        {
            if (ev.IsAllowed)
                Forget(ev.Player);
        }

        internal static void Clear()
        {
            taken.Clear();
            dealt.Clear();
            hazardSafeUntil.Clear();
            secondChance.Clear();
            unkillableUntil.Clear();
            TwistedWishes.Clear();
        }
    }
}
