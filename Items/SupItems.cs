using System;
using System.Collections.Generic;
using System.Linq;
using Brainless.Internal;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Doors;
using Exiled.API.Features.Items;
using Exiled.API.Features.Roles;
using Exiled.Events.EventArgs.Player;
using Interactables.Interobjects.DoorUtils;
using PlayerRoles;
using UnityEngine;

namespace Brainless.Items
{
    public sealed partial class Plugin
    {
        private readonly Dictionary<string, float> pillCooldowns = new Dictionary<string, float>();
        private readonly HashSet<string> invisibleUsed = new HashSet<string>();
        private readonly Dictionary<Player, Stealth> invisible = new Dictionary<Player, Stealth>();
        private readonly Dictionary<Player, Boost> boosts = new Dictionary<Player, Boost>();
        private readonly Dictionary<Player, float> anti096 = new Dictionary<Player, float>();
        private readonly Dictionary<string, float> radioCooldowns = new Dictionary<string, float>();
        private readonly Dictionary<bool, int> radioCalls = new Dictionary<bool, int>();
        private float supTick, roundStartedAt;
        private void SupSubscribe()
        {
            Exiled.Events.Handlers.Scp096.AddingTarget += Adding096Target;
            Exiled.Events.Handlers.Scp939.ValidatingVisibility += Validating939;
            Exiled.Events.Handlers.Scp939.PlayingFootstep += Footstep939;
            Exiled.Events.Handlers.Player.Shooting += SupShooting;
        }

        private void SupUnsubscribe()
        {
            Exiled.Events.Handlers.Scp096.AddingTarget -= Adding096Target;
            Exiled.Events.Handlers.Scp939.ValidatingVisibility -= Validating939;
            Exiled.Events.Handlers.Scp939.PlayingFootstep -= Footstep939;
            Exiled.Events.Handlers.Player.Shooting -= SupShooting;
        }

        private void SupShooting(ShootingEventArgs ev)
        {
            bool rail = ev.Item != null && Exiled.CustomItems.API.Features.CustomItem.Registered.Any(i => i.Id == 7208 && i.TrackedSerials.Contains(ev.Item.Serial));
            if ((ev.IsAllowed || rail) && Config.Sup.InvisibilityPills.BreakOnAttack)
                StopStealth(ev.Player);
        }

        private void Adding096Target(Exiled.Events.EventArgs.Scp096.AddingTargetEventArgs ev) => Safe.Run(() =>
        {
            if (!ev.IsAllowed || ev.Target == null)
                return;
            if (anti096.TryGetValue(ev.Target, out float expiry) && expiry > Time.time)
                ev.IsAllowed = false;
        });
        private void Validating939(Exiled.Events.EventArgs.Scp939.ValidatingVisibilityEventArgs ev)
        {
            if (Config.Sup.InvisibilityPills.HideFrom939 && IsStealthed(ev.Target))
                ev.IsAllowed = false;
        }

        private void Footstep939(Exiled.Events.EventArgs.Scp939.PlayingFootstepEventArgs ev)
        {
            if (Config.Sup.InvisibilityPills.Hide939Footsteps && IsStealthed(ev.Target))
                ev.IsAllowed = false;
        }

        private bool IsStealthed(Player player) => player != null && invisible.TryGetValue(player, out var state) && state.Expires > Time.time && player.IsAlive && player.Role.Base == state.Role;
        private void SupLeave(Player player)
        {
            StopStealth(player);
            if (boosts.TryGetValue(player, out var boost))
                boost.Layer.Clear();
            boosts.Remove(player);
            anti096.Remove(player);
        }

        private void StopStealth(Player player)
        {
            if (player != null && invisible.TryGetValue(player, out var state))
            {
                state.Effect.Clear();
                invisible.Remove(player);
            }
        }

        private void SupCleanup()
        {
            foreach (var player in invisible.Keys.ToArray())
                StopStealth(player);
            foreach (var boost in boosts.Values)
                boost.Layer.Clear();
            boosts.Clear();
            anti096.Clear();
        }

        private void SupReset()
        {
            invisibleUsed.Clear();
            pillCooldowns.Clear();
            radioCooldowns.Clear();
            radioCalls.Clear();
            supTick = Time.time;
            roundStartedAt = Time.time;
        }

        private bool ActivateSup(Player player, Item held, RegisteredItem item, bool hold)
        {
            if (item.Id < 7114 || item.Id > 7122)
                return false;
            if (item.Id == 7119)
            {
                HackDoor(player, held, LookDoor(player, Config.Sup.HackingChip.Reach), hold);
                return true;
            }

            if (item.Id == 7120)
            {
                CallReinforcements(player, held, hold);
                return true;
            }

            PillTuning tuning = item.Id == 7114 ? Config.Sup.AdrenalinePills : item.Id == 7115 ? Config.Sup.Anti096Pills : item.Id == 7116 ? Config.Sup.ExpiredPills : item.Id == 7117 ? Config.Sup.InvisibilityPills : (PillTuning)Config.Sup.VolatilePills;
            string key = player.UserId + ":" + item.Id;
            if (pillCooldowns.TryGetValue(key, out float until) && until > Time.time)
            {
                player.ShowHint($"Pills available in {until - Time.time:0.#}s.", 3);
                return true;
            }

            if (item.Id == 7115 && !Targets096(player).Any())
            {
                player.ShowHint("You are not an SCP-096 target. Pills retained.", 3);
                return true;
            }

            if (item.Id == 7117 && Config.Sup.InvisibilityPills.OncePerPlayerPerRound && invisibleUsed.Contains(player.UserId))
            {
                player.ShowHint("Invisibility pills can be used once per round.", 3);
                return true;
            }

            Begin(player, tuning.UseSeconds, held, hold, () =>
            {
                if (item.Id == 7115 && !Targets096(player).Any())
                    return;
                if (!Spend(player, held))
                    return;
                pillCooldowns[key] = Time.time + tuning.CooldownSeconds;
                switch (item.Id)
                {
                    case 7114:
                        if (boosts.TryGetValue(player, out var old))
                            old.Layer.Clear();
                        boosts[player] = new Boost
                        {
                            Role = player.Role.Base,
                            Expires = Time.time + Config.Sup.AdrenalinePills.DurationSeconds,
                            Multiplier = Config.Sup.AdrenalinePills.SpeedMultiplier,
                            Exhaustion = Config.Sup.AdrenalinePills.ExhaustionSeconds
                        };
                        player.Stamina = Mathf.Min(player.StaminaStat.MaxValue, player.Stamina + player.StaminaStat.MaxValue * Config.Sup.AdrenalinePills.StaminaRestorePercent / 100);
                        player.ShowHint("Adrenaline active.", 3);
                        break;
                    case 7115:
                        foreach (var scp in Targets096(player))
                            scp.RemoveTarget(player);
                        if (Config.Sup.Anti096Pills.ProvideTemporaryImmunity)
                            anti096[player] = Time.time + Config.Sup.Anti096Pills.ImmunitySeconds;
                        player.ShowHint("Removed from SCP-096's target list.", 3);
                        break;
                    case 7116:
                        RandomPill(player);
                        break;
                    case 7117:
                        StopStealth(player);
                        invisibleUsed.Add(player.UserId);
                        invisible[player] = new Stealth
                        {
                            Role = player.Role.Base,
                            Expires = Time.time + Config.Sup.InvisibilityPills.DurationSeconds
                        };
                        invisible[player].Effect.Refresh(player);
                        player.ShowHint($"Invisible for {Config.Sup.InvisibilityPills.DurationSeconds:0.#}s. Attacking can break stealth.", 3);
                        break;
                    case 7122:
                        Volatile(player);
                        break;
                }
            });
            return true;
        }

        private static IEnumerable<Scp096Role> Targets096(Player target) => Player.List.Where(p => p.Role.Type == RoleTypeId.Scp096).Select(p => p.Role.As<Scp096Role>()).Where(r => r.HasTarget(target)).ToArray();
        private void RandomPill(Player player)
        {
            var cfg = Config.Sup.ExpiredPills;
            int category = SupRules.WeightedIndex(new double[] { cfg.PositiveWeight, cfg.NegativeWeight, cfg.ScpWeight }, UnityEngine.Random.value);
            string name = category == 0 ? "Positive" : category == 1 ? "Negative" : category == 2 ? "SCP" : "";
            var choices = cfg.Effects.Where(e => e.Category == name).ToArray();
            int selected = SupRules.WeightedIndex(choices.Select(e => (double)e.Weight).ToArray(), UnityEngine.Random.value);
            if (selected < 0)
            {
                player.Heal(UnityEngine.Random.Range(cfg.FallbackMinHeal, cfg.FallbackMaxHeal));
                player.ShowHint("Expired pill partially healed you.", 3);
                return;
            }

            var effect = choices[selected];
            player.EnableEffect(effect.Effect, (byte)UnityEngine.Random.Range(effect.MinIntensity, effect.MaxIntensity + 1), effect.DurationSeconds > 0 ? effect.DurationSeconds : cfg.DurationSeconds);
            player.ShowHint("Expired pill: " + effect.Effect, 4);
        }

        private void Volatile(Player player)
        {
            var cfg = Config.Sup.VolatilePills;
            var role = player.Role.Base;
            bool survives = UnityEngine.Random.Range(0f, 100f) < cfg.SurvivalChancePercent;
            fragments.Launch(player.Position + Vector3.up * .3f, Vector3.zero, cfg.FuseSeconds, new DamageSource(player), cfg.Radius, cfg.HumanDamage, cfg.ScpDamage, requireLineOfSight: cfg.RequireLineOfSight, affectOwner: false, armorPenetration: cfg.ArmorPenetration);
            player.ShowHint(survives ? "Volatile pills: you survived the reaction!" : "Volatile pills are reacting...", 3);
            lifetime.Later(cfg.FuseSeconds, () =>
            {
                if (!player.IsConnected || !player.IsAlive || player.Role.Base != role)
                    return;
                if (survives)
                    player.Health = Mathf.Min(player.MaxHealth, cfg.SurvivalHealth);
                else
                    player.Kill(DamageType.Explosion);
            });
        }

        private bool Hackable(Door door)
        {
            if (door == null || door.Base is Interactables.Interobjects.BasicNonInteractableDoor)
                return false;
            var levels = new KeycardLevels((DoorPermissionFlags)door.KeycardPermissions);
            var cfg = Config.Sup.HackingChip;
            return SupRules.CanHack(door.IsLocked, door.IsGate, door.IsElevator, door.IsCheckpoint || door.IsPartOfCheckpoint, cfg.AllowCheckpoints, levels.Containment, levels.Armory, levels.Admin, cfg.MaxAccessLevel);
        }

        private void HackDoor(Player player, Item held, Door door, bool hold)
        {
            var cfg = Config.Sup.HackingChip;
            if (!Hackable(door) || door.IsOpen || Vector3.Distance(player.Position, door.Position) > cfg.Reach)
            {
                player.ShowHint("Cannot hack this door: level 3, hard locks, gates and protected doors are excluded.", 3);
                return;
            }

            Begin(player, cfg.UseSeconds, held, hold, () =>
            {
                if (!Hackable(door) || door.IsOpen || Vector3.Distance(player.Position, door.Position) > cfg.Reach || LookDoor(player, cfg.Reach) != door || !Spend(player, held))
                    return;
                door.IsOpen = true;
                World.Cue(door.Position, "Door hacked");
            });
            PlayAudio(door.Position, cfg.UseSeconds, "beep");
        }

        private static Player[] Spectators() => Player.List.Where(p => p.IsConnected && !p.IsHost && p.Role.Type == RoleTypeId.Spectator && !p.IsOverwatchEnabled).ToArray();
        private void CallReinforcements(Player player, Item held, bool hold)
        {
            var cfg = Config.Sup.ReinforcementRadio;
            bool chaos = player.Role.Team == Team.ChaosInsurgency;
            if (!chaos && player.Role.Team != Team.FoundationForces && player.Role.Team != Team.Scientists)
            {
                player.ShowHint("Only Foundation personnel and Chaos can use this radio.", 3);
                return;
            }

            radioCalls.TryGetValue(chaos, out int calls);
            if (!Round.IsStarted || Time.time - roundStartedAt < cfg.EarliestRoundSeconds || Population < cfg.MinimumPlayers || calls >= cfg.CallsPerFactionPerRound || Spectators().Length == 0 || (radioCooldowns.TryGetValue(player.UserId, out float until) && until > Time.time))
            {
                player.ShowHint("Reinforcement call unavailable: check population, spectators, round timing and call limits.", 4);
                return;
            }

            Begin(player, cfg.UseSeconds, held, hold, () =>
            {
                radioCalls.TryGetValue(chaos, out int used);
                if (!Round.IsStarted || used >= cfg.CallsPerFactionPerRound || Spectators().Length == 0 || !Spend(player, held))
                    return;
                radioCalls[chaos] = used + 1;
                radioCooldowns[player.UserId] = Time.time + cfg.CooldownSeconds;
                Map.Broadcast(5, (chaos ? "Chaos" : "Foundation") + " reinforcement radio: a mini wave has been called.");
                // Trigger the faction's native mini wave (its own arrival, spawn points,
                // loadouts and custom-role rolls) instead of placing spectators directly.
                lifetime.Later(cfg.ArrivalSeconds, () =>
                {
                    if (Round.IsStarted && !Round.IsEnded)
                        Respawn.ForceWave(chaos ? SpawnableFaction.ChaosMiniWave : SpawnableFaction.NtfMiniWave);
                });
            });
        }

        private System.Collections.Generic.IEnumerator<float> SupUpdates()
        {
            while (true)
            {
                yield return MEC.Timing.WaitForSeconds(.25f);
                Safe.Run(SupTick);
            }
        }

        private void SupTick()
        {
            float now = Time.time, elapsed = Mathf.Clamp(now - supTick, 0, 1);
            supTick = now;
            foreach (var pair in invisible.ToArray())
            {
                if (!IsStealthed(pair.Key))
                {
                    StopStealth(pair.Key);
                    continue;
                }

                pair.Value.Effect.Refresh(pair.Key);
            }

            foreach (var pair in boosts.ToArray())
            {
                var player = pair.Key;
                var boost = pair.Value;
                if (!player.IsConnected || !player.IsAlive || player.Role.Base != boost.Role || now >= boost.Expires)
                {
                    boost.Layer.Clear();
                    boosts.Remove(player);
                    if (player.IsConnected && player.IsAlive && player.Role.Base == boost.Role && boost.Exhaustion > 0)
                        player.EnableEffect(EffectType.Exhausted, 1, boost.Exhaustion);
                }
                else
                    boost.Layer.Set(player, boost.Multiplier);
            }
        }

        private sealed class Stealth
        {
            internal PlayerRoleBase Role;
            internal float Expires;
            internal readonly InvisibleLease Effect = new InvisibleLease();
        }

        private sealed class Boost
        {
            internal PlayerRoleBase Role;
            internal float Expires, Multiplier, Exhaustion;
            internal readonly MovementLayer Layer = new MovementLayer();
        }

        private sealed class InvisibleLease
        {
            private Player owner;
            private byte previous;
            private float previousExpiry;
            private bool owns;
            internal void Refresh(Player player)
            {
                var effect = player.GetEffect(EffectType.Invisible);
                if (effect == null)
                    return;
                bool external = effect.IsEnabled && (!owns || effect.Intensity != 1 || effect.TimeLeft > .65f);
                if (external)
                {
                    previous = effect.Intensity;
                    previousExpiry = effect.Duration <= 0 ? float.PositiveInfinity : Time.time + effect.TimeLeft;
                    owns = false;
                }

                if (effect.IsEnabled && !owns)
                    return;
                owner = player;
                player.EnableEffect(EffectType.Invisible, 1, .6f);
                owns = true;
            }

            internal void Clear()
            {
                var effect = owner?.IsConnected == true ? owner.GetEffect(EffectType.Invisible) : null;
                if (owns && effect?.Intensity == 1 && effect.TimeLeft <= .65f)
                {
                    if (previous > 0 && previousExpiry > Time.time)
                        owner.EnableEffect(EffectType.Invisible, previous, float.IsPositiveInfinity(previousExpiry) ? 0 : previousExpiry - Time.time);
                    else
                        owner.DisableEffect(EffectType.Invisible);
                }

                owns = false;
            }
        }
    }
}
