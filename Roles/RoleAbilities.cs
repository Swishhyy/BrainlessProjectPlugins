using System;
using System.Collections.Generic;
using System.Linq;
using Brainless.Internal;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Hazards;
using Exiled.API.Features.Items;
using Exiled.Events.EventArgs.Player;
using PlayerRoles;
using PlayerStatsSystem;
using UnityEngine;

namespace Brainless.Roles
{
    // Revision 12 abilities: heal on hit/kill, bleed and armor break, poison aura,
    // pocket-dimension healing, rage extension, faster Breakneck/tantrum cooldowns,
    // tar trails, and SCP-049 revive boosts / random zombie variants.
    public sealed partial class Plugin
    {
        private readonly Dictionary<string, Player> revivers = new Dictionary<string, Player>();
        private readonly List<Hazards.TantrumEnvironmentalHazard> tarPuddles = new List<Hazards.TantrumEnvironmentalHazard>();
        private const float TarSeconds = 15;
        private const int TarMaxPerPlayer = 4;

        private void SubscribeAbilities()
        {
            Exiled.Events.Handlers.Player.Hurt += AbilityHit;
            Exiled.Events.Handlers.Player.Died += AbilityDeath;
            Exiled.Events.Handlers.Scp173.PlacingTantrum += QuickTantrum;
            Exiled.Events.Handlers.Scp049.FinishingRecall += RecordReviver;
        }

        private void UnsubscribeAbilities()
        {
            Exiled.Events.Handlers.Player.Hurt -= AbilityHit;
            Exiled.Events.Handlers.Player.Died -= AbilityDeath;
            Exiled.Events.Handlers.Scp173.PlacingTantrum -= QuickTantrum;
            Exiled.Events.Handlers.Scp049.FinishingRecall -= RecordReviver;
        }

        private void ClearAbilities()
        {
            revivers.Clear();
            foreach (var puddle in tarPuddles)
                Safe.Run(() =>
                {
                    if (puddle != null && puddle.IsActive)
                        puddle.ServerDestroy();
                });
            tarPuddles.Clear();
        }

        // Glutton heals, Rust makes victims bleed and can shred armor.
        private void AbilityHit(HurtEventArgs ev) => Safe.Run(() =>
        {
            if (ev.HandlerOutput != DamageHandlerBase.HandlerOutput.Damaged || ev.Amount <= 0 || ev.Attacker == null || ev.Attacker == ev.Player || !ev.Player.IsHuman || !IsScpAttack(ev.DamageHandler.Type))
                return;
            var traits = Traits(ev.Attacker);
            if (traits == null)
                return;
            if (traits.HealOnHit > 0)
                ev.Attacker.Heal(traits.HealOnHit);
            if (traits.BleedOnHitSeconds > 0 && ev.Player.IsAlive)
                ev.Player.EnableEffect(EffectType.Bleeding, 1, traits.BleedOnHitSeconds, false);
            if (traits.ArmorBreakChancePercent > 0 && ev.Player.IsAlive && random.NextDouble() * 100 < traits.ArmorBreakChancePercent)
            {
                var armor = ev.Player.Items.FirstOrDefault(i => i is Armor);
                if (armor != null)
                {
                    ev.Player.RemoveItem(armor);
                    ev.Player.ShowHint("Your armor rusted away!", 3);
                }
            }
        });

        private void AbilityDeath(DiedEventArgs ev) => Safe.Run(() =>
        {
            var killer = ev.Attacker;
            if (killer != null && killer != ev.Player && killer.IsAlive && Traits(killer) is ScpTraits traits)
            {
                if (traits.HealOnKill > 0)
                    killer.Heal(traits.HealOnKill);
                if (traits.RageExtendOnKillSeconds > 0 && killer.Role is Exiled.API.Features.Roles.Scp096Role rage && rage.RageState == PlayerRoles.PlayableScps.Scp096.Scp096RageState.Enraged)
                {
                    rage.EnragedTimeLeft += traits.RageExtendOnKillSeconds;
                    killer.ShowHint("Kill! +" + traits.RageExtendOnKillSeconds.ToString("0") + "s rage", 2);
                }
            }

            if (ev.DamageHandler.Type == DamageType.PocketDimension)
                foreach (var pair in active.ToArray())
                {
                    var digger = pair.Value.Definition.Traits;
                    if (!pair.Key.IsAlive || pair.Key.Role != pair.Value.Role)
                        continue;
                    if (digger.PocketDeathHeal > 0)
                    {
                        pair.Key.Heal(digger.PocketDeathHeal);
                        pair.Key.ShowHint("A victim died in your pocket dimension: +" + digger.PocketDeathHeal.ToString("0") + " HP", 3);
                    }

                    if (digger.PocketDeathVigor && pair.Key.Role is Exiled.API.Features.Roles.Scp106Role larry)
                    {
                        larry.Vigor = 1;
                        pair.Key.ShowHint("A victim died in your pocket dimension: vigor refilled", 3);
                    }
                }
        });

        private void QuickTantrum(Exiled.Events.EventArgs.Scp173.PlacingTantrumEventArgs ev) => Safe.Run(() =>
        {
            float multiplier = Traits(ev.Player)?.TantrumCooldownMultiplier ?? 1;
            if (!ev.IsAllowed || multiplier == 1)
                return;
            var player = ev.Player;
            lifetime.Later(.1f, () =>
            {
                if (player.Role is Exiled.API.Features.Roles.Scp173Role statue && statue.RemainingTantrumCooldown > 0)
                    statue.RemainingTantrumCooldown *= multiplier;
            });
        });

        private void RecordReviver(Exiled.Events.EventArgs.Scp049.FinishingRecallEventArgs ev) => Safe.Run(() =>
        {
            if (ev.IsAllowed && ev.Target != null && ev.Player != null)
                revivers[ev.Target.UserId] = ev.Player;
        });

        private ScpTraits ReviverTraits(Player zombie) => zombie?.UserId != null && revivers.TryGetValue(zombie.UserId, out var doctor) && doctor.IsConnected && doctor.Role.Type == RoleTypeId.Scp049 ? Traits(doctor) : null;

        // Mortician: a revived zombie becomes a random zombie variant (Common to Rare).
        private bool RandomZombie(Player player)
        {
            if (ReviverTraits(player)?.RandomZombieVariant != true)
                return false;
            var pool = Config.Roles.Where(r => r.Id != "Z05" && r.Rarity <= Rarity.Rare && (r.ChancePercent ?? 0) > 0 && Matches(r, player) && TierOpen(player, r.Rarity)).ToList();
            while (pool.Count > 0)
            {
                var pick = pool[random.Next(pool.Count)];
                if (Availability(pick, player) == null)
                {
                    Apply(player, pick, false);
                    RarityLedger.Take(RarityScope(player), RoundKey, pick.Rarity);
                    assignmentNotes[player] = "Mortician revive: random variant " + pick.Id + ".";
                    return true;
                }

                pool.Remove(pick);
            }

            return false;
        }

        // Conductor/Necromancer: revived zombies get extra health and speed, after any
        // profile has been applied.
        private void BoostRevived(Player player)
        {
            if (player.Role.Type != RoleTypeId.Scp0492)
                return;
            var traits = ReviverTraits(player);
            revivers.Remove(player.UserId);
            if (traits == null)
                return;
            if (traits.RevivedZombieHealthMultiplier != 1)
            {
                player.MaxHealth *= traits.RevivedZombieHealthMultiplier;
                player.Health = player.MaxHealth;
            }

            if (traits.RevivedZombieSpeedPercent > 0)
                player.EnableEffect(EffectType.MovementBoost, traits.RevivedZombieSpeedPercent, 0, false);
        }

        // Plague Bearer: humans standing close are poisoned (one refreshing poison).
        private void UpdatePoisonAuras()
        {
            foreach (var pair in active.ToArray())
            {
                var traits = pair.Value.Definition.Traits;
                if (traits.PoisonAuraRadius <= 0 || !pair.Key.IsAlive || pair.Key.Role != pair.Value.Role)
                    continue;
                float range = traits.PoisonAuraRadius * traits.PoisonAuraRadius;
                foreach (var human in Player.List.Where(p => p.IsAlive && p.IsHuman && (p.Position - pair.Key.Position).sqrMagnitude <= range).ToArray())
                    ApplyPoison(human, pair.Key, traits);
            }
        }

        // Blinker: each new Breakneck cooldown is scaled once when it starts.
        private static void ScaleBreakneckCooldown(Player player, ActiveRole state)
        {
            float multiplier = state.Definition.Traits.BreakneckCooldownMultiplier;
            if (multiplier == 1 || !(player.Role is Exiled.API.Features.Roles.Scp173Role statue))
                return;
            float cooldown = statue.RemainingBreakneckCooldown;
            if (cooldown > state.LastBreakneckCooldown + .5f)
            {
                cooldown *= multiplier;
                statue.RemainingBreakneckCooldown = cooldown;
            }

            state.LastBreakneckCooldown = cooldown;
        }

        // Tar Pit: drops a short-lived tantrum puddle where it stood a few seconds ago,
        // never on top of anyone (a tantrum spawned on a player crashed the server).
        private void LeaveTar(Player player, ActiveRole state)
        {
            float interval = state.Definition.Traits.TarTrailSeconds;
            if (interval <= 0 || Time.time < state.NextTar)
                return;
            state.NextTar = Time.time + interval;
            var previous = state.TarAnchor;
            bool hidden = player.Role is Exiled.API.Features.Roles.Scp106Role ghost && (ghost.IsSubmerged || ghost.IsStalking);
            state.TarAnchor = hidden || player.CurrentRoom?.Type == RoomType.Pocket ? (Vector3?)null : TarFloor(player.Position);
            if (!previous.HasValue || TantrumHazard.TantrumPrefab == null)
                return;
            float clearance = Math.Max(1.5f, TantrumHazard.TantrumPrefab.MaxDistance + .75f);
            Vector3 floor = previous.Value;
            if (Player.List.Any(p => p.IsAlive && Math.Abs(p.Position.y - floor.y) < 2.5f && new Vector2(p.Position.x - floor.x, p.Position.z - floor.z).sqrMagnitude <= clearance * clearance))
                return;
            tarPuddles.RemoveAll(p => p == null || !p.IsActive);
            state.Tar.RemoveAll(p => p == null || !p.IsActive);
            if (state.Tar.Count >= TarMaxPerPlayer)
            {
                var oldest = state.Tar[0];
                state.Tar.RemoveAt(0);
                Safe.Run(() => oldest.ServerDestroy());
            }

            // Same path as SCP-173's own tantrum (and the foam grenade): one spawn in Start().
            var puddle = UnityEngine.Object.Instantiate(TantrumHazard.TantrumPrefab);
            puddle.SynchronizedPosition = new RelativePositioning.RelativePosition(floor + Vector3.up * 1.25f);
            puddle.HazardDuration = TarSeconds;
            tarPuddles.Add(puddle);
            state.Tar.Add(puddle);
        }

        private static Vector3? TarFloor(Vector3 point)
        {
            foreach (var hit in Physics.RaycastAll(point + Vector3.up, Vector3.down, 4, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance))
                if (hit.collider.GetComponentInParent<ReferenceHub>() == null && hit.collider.GetComponentInParent<InventorySystem.Items.Pickups.ItemPickupBase>() == null && hit.collider.GetComponentInParent<AdminToys.AdminToyBase>() == null)
                    return hit.point;
            return null;
        }

        private void GiveRandomScpItems(Player player, int count)
        {
            var pool = Catalog.ScpItems.ToList();
            for (int i = 0; i < count && pool.Count > 0; i++)
            {
                var type = pool[random.Next(pool.Count)];
                pool.Remove(type);
                player.AddItem(type);
            }
        }
    }
}
