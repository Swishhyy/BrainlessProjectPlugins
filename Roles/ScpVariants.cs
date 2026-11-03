using System;
using System.Collections.Generic;
using System.Linq;
using Brainless.Internal;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp939;
using PlayerStatsSystem;
using UnityEngine;

namespace Brainless.Roles
{
    public sealed partial class Plugin
    {
        private readonly Dictionary<Player, Poison> poisons = new Dictionary<Player, Poison>();
        internal ScpTraits Traits(Player player) => player != null && active.TryGetValue(player, out var state) && player.IsAlive && player.Role == state.Role ? state.Definition.Traits : null;
        private static bool IsScpAttack(DamageType type) => type == DamageType.Scp0492 || type == DamageType.Scp106 || type == DamageType.Scp939 || type == DamageType.Scp096 || type == DamageType.Scp173;
        private void SubscribeVariants()
        {
            Exiled.Events.Handlers.Player.Hurt += PoisonedByAttack;
            Exiled.Events.Handlers.Player.Hurt += ResilientHurt;
            Exiled.Events.Handlers.Player.UsedItem += BigBoiConsumable;
            Exiled.Events.Handlers.Player.UsedItem += CurePoison;
            Exiled.Events.Handlers.Scp939.PlayingSound += MimicSound;
        }

        private void UnsubscribeVariants()
        {
            Exiled.Events.Handlers.Player.Hurt -= PoisonedByAttack;
            Exiled.Events.Handlers.Player.Hurt -= ResilientHurt;
            Exiled.Events.Handlers.Player.UsedItem -= BigBoiConsumable;
            Exiled.Events.Handlers.Player.UsedItem -= CurePoison;
            Exiled.Events.Handlers.Scp939.PlayingSound -= MimicSound;
        }

        private void MimicSound(Exiled.Events.EventArgs.Scp939.PlayingSoundEventArgs ev)
        {
            float? cooldown = Traits(ev.Player)?.MimicSoundCooldownSeconds;
            if (ev.IsAllowed && cooldown.HasValue)
                ev.Cooldown = cooldown.Value;
        }

        private void PoisonedByAttack(HurtEventArgs ev)
        {
            if (ev.HandlerOutput != DamageHandlerBase.HandlerOutput.Damaged || ev.Amount <= 0 || !ev.Player.IsHuman || (ev.DamageHandler.Type != DamageType.Scp106 && ev.DamageHandler.Type != DamageType.Scp939))
                return;
            var traits = Traits(ev.Attacker);
            if (traits != null)
                ApplyPoison(ev.Player, ev.Attacker, traits);
        }

        // Resilient (X01): one-time last stand at low health. Checked on damage and on
        // every role update tick, so it also triggers for damage the Hurt event misses.
        private void ResilientHurt(HurtEventArgs ev) => Safe.Run(() =>
        {
            var player = ev.Player;
            if (player != null && active.TryGetValue(player, out var state))
                TryResilient(player, state);
        });

        private void TryResilient(Player player, ActiveRole state)
        {
            if (player == null || !player.IsAlive || state.Definition.Id != "X01" || state.ResilientActive || player.Role != state.Role)
                return;
            if (player.Health > player.MaxHealth * Config.Resilient.TriggerHealthPercent / 100)
                return;
            state.ResilientActive = true;
            if (Config.Resilient.ArtificialHealth > 0)
                player.AddAhp(Config.Resilient.ArtificialHealth, Config.Resilient.ArtificialHealth, 0, .7f, 0, true);
            player.ShowHint("<b><color=#e74c3c>RESILIENT</color></b>\nFaster, harder hitting and shielded for the rest of this life.", 5);
        }

        // Big Boi (X02): medical items do more.
        private void BigBoiConsumable(UsedItemEventArgs ev) => Safe.Run(() =>
        {
            if (ev.CausedByHolstering || ev.Item == null || !active.TryGetValue(ev.Player, out var state) || state.Definition.Id != "X02" || ev.Player.Role != state.Role || !ev.Player.IsAlive)
                return;
            var cfg = Config.BigBoi;
            string bonus = null;
            switch (ev.Item.Type)
            {
                case ItemType.Medkit when cfg.MedkitBonusHealth > 0:
                    ev.Player.Heal(cfg.MedkitBonusHealth);
                    bonus = $"+{cfg.MedkitBonusHealth:0} HP";
                    break;
                case ItemType.Painkillers when cfg.PainkillersBonusHealth > 0:
                    ev.Player.Heal(cfg.PainkillersBonusHealth);
                    bonus = $"+{cfg.PainkillersBonusHealth:0} HP";
                    break;
                case ItemType.SCP500 when cfg.Scp500BonusArtificialHealth > 0:
                    ev.Player.AddAhp(cfg.Scp500BonusArtificialHealth);
                    bonus = $"+{cfg.Scp500BonusArtificialHealth:0} shield";
                    break;
                case ItemType.Adrenaline when cfg.AdrenalineBonusArtificialHealth > 0:
                    ev.Player.AddAhp(cfg.AdrenalineBonusArtificialHealth);
                    bonus = $"+{cfg.AdrenalineBonusArtificialHealth:0} shield";
                    break;
            }

            if (bonus != null)
                ev.Player.ShowHint($"<b>Big Boi</b> bonus: {bonus}", 3);
        });

        private void ApplyPoison(Player player, Player source, ScpTraits traits)
        {
            if (traits.PoisonDamagePerSecond <= 0 || traits.PoisonDurationSeconds <= 0 || !player.IsAlive || !player.IsHuman)
                return;
            var damage = new DamageSource(source);
            if (!damage.CanAffect(player))
                return;
            if (poisons.TryGetValue(player, out var current) && ReferenceEquals(current.Life, player.Role.Base))
            {
                if (current.PerSecond > traits.PoisonDamagePerSecond)
                    return;
                current.Expires = Time.time + traits.PoisonDurationSeconds;
                current.PerSecond = traits.PoisonDamagePerSecond;
                current.Source = damage;
                return;
            }

            poisons[player] = new Poison
            {
                Life = player.Role.Base,
                Source = damage,
                Expires = Time.time + traits.PoisonDurationSeconds,
                LastTick = Time.time,
                PerSecond = traits.PoisonDamagePerSecond
            };
            player.ShowHint("Poisoned! Use a medkit, painkillers or SCP-500.", 3);
        }

        private void CurePoison(UsedItemEventArgs ev)
        {
            if (!ev.CausedByHolstering && (ev.Item.Type == ItemType.Medkit || ev.Item.Type == ItemType.Painkillers || ev.Item.Type == ItemType.SCP500) && poisons.Remove(ev.Player))
                ev.Player.ShowHint("Poison cured.", 2);
        }

        private void ForgetPoisons(Player source)
        {
            foreach (var pair in poisons.Where(p => p.Key == source || p.Value.Source.Owner == source).ToArray())
                poisons.Remove(pair.Key);
        }

        private void UpdatePoisons()
        {
            // Native hazard membership supplies its real radius/state/geometry.
            // Repeated cloud exposure refreshes one poison rather than stacking it.
            foreach (var cloud in Scp939AmnesticCloudInstance.ActiveInstances.ToArray())
            {
                if (cloud == null || !cloud.IsActive)
                    continue;
                var source = Player.Get(cloud.Owner);
                var traits = Traits(source);
                if (traits?.PoisonInCloud != true)
                    continue;
                foreach (var hub in cloud.AffectedPlayers.ToArray())
                    if (Player.Get(hub)is Player player && player.IsHuman)
                        ApplyPoison(player, source, traits);
            }

            float now = Time.time;
            foreach (var pair in poisons.ToArray())
            {
                var player = pair.Key;
                var poison = pair.Value;
                if (!player.IsConnected || !player.IsAlive || !player.IsHuman || !ReferenceEquals(player.Role.Base, poison.Life))
                {
                    poisons.Remove(player);
                    continue;
                }

                float damage = ScpTraitRules.PoisonDamage(now, poison.LastTick, poison.Expires, poison.PerSecond);
                poison.LastTick = now;
                if (now >= poison.Expires)
                    poisons.Remove(player);
                poison.Source.Hurt(player, damage, "SCP venom");
            }
        }

        // Escape's (B02): a kill during breakneck restarts its stare timer, and the
        // cooldown that follows is cleared. A kill while it is recharging clears it now.
        private void RefreshBreakneck(DiedEventArgs ev)
        {
            if (ev.Attacker == null || ev.Player == ev.Attacker || !IsScpAttack(ev.DamageHandler.Type) || Traits(ev.Attacker)?.RefreshBreakneckOnKill != true || !(ev.Attacker.Role is Exiled.API.Features.Roles.Scp173Role statue) || !active.TryGetValue(ev.Attacker, out var state))
                return;
            if (statue.BreakneckActive)
            {
                ScpPrivateAccess.RestartBreakneck(statue.BreakneckSpeedsAbility);
                state.BreakneckKill = true;
                ev.Attacker.ShowHint("Kill! Breakneck refreshed.", 2);
                return;
            }

            statue.RemainingBreakneckCooldown = 0;
            ev.Attacker.ShowHint("Kill! Breakneck ready.", 2);
        }

        private static void ClearRefreshedCooldown(Player player, ActiveRole state)
        {
            if (!state.BreakneckKill || !(player.Role is Exiled.API.Features.Roles.Scp173Role statue) || statue.BreakneckActive)
                return;
            state.BreakneckKill = false;
            if (statue.RemainingBreakneckCooldown > 0)
                statue.RemainingBreakneckCooldown = 0;
        }

        private sealed class Poison
        {
            internal PlayerRoleBase Life;
            internal DamageSource Source;
            internal float LastTick, Expires, PerSecond;
        }
    }
}
