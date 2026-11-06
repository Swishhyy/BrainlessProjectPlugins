using System.Collections.Generic;
using Brainless.Internal;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Roles;
using Exiled.Events.EventArgs.Player;
using PlayerRoles;
using UnityEngine;
using P = Exiled.Events.Handlers.Player;

namespace Brainless.Weapons
{
    // Stun Gun (7209): each hit deals 1 damage and knocks the target down. A ragdoll
    // of the target drops in their place (the death animation) while the real player
    // is hidden, rooted and blinded. Nothing dies, so there is no death or CASSIE
    // announcement; the player reappears where they fell when the stun ends.
    public sealed partial class Plugin
    {
        private const uint StunGunId = 7209;
        private readonly Dictionary<Player, Stun> stuns = new Dictionary<Player, Stun>();
        private readonly Dictionary<Player, float> stunImmunity = new Dictionary<Player, float>();

        private void SubscribeStun()
        {
            P.ChangingRole += StunRoleChanging;
            P.Left += StunLeft;
            Exiled.Events.Handlers.Scp173.BlinkingRequest += StunnedBlinkRequest;
            Exiled.Events.Handlers.Scp173.Blinking += StunnedBlink;
            Exiled.Events.Handlers.Scp173.PlacingTantrum += StunnedTantrum;
            Exiled.Events.Handlers.Scp173.UsingBreakneckSpeeds += StunnedBreakneck;
        }

        private void UnsubscribeStun()
        {
            P.ChangingRole -= StunRoleChanging;
            P.Left -= StunLeft;
            Exiled.Events.Handlers.Scp173.BlinkingRequest -= StunnedBlinkRequest;
            Exiled.Events.Handlers.Scp173.Blinking -= StunnedBlink;
            Exiled.Events.Handlers.Scp173.PlacingTantrum -= StunnedTantrum;
            Exiled.Events.Handlers.Scp173.UsingBreakneckSpeeds -= StunnedBreakneck;
            ClearStuns();
        }

        // SCP-173 moves by blinking, which Ensnared cannot stop: a stunned 173 cannot
        // blink, place tantrum or use breakneck speed.
        private void StunnedBlinkRequest(Exiled.Events.EventArgs.Scp173.BlinkingRequestEventArgs ev)
        {
            if (IsStunned(ev.Player))
                ev.IsAllowed = false;
        }

        private void StunnedBlink(Exiled.Events.EventArgs.Scp173.BlinkingEventArgs ev)
        {
            if (IsStunned(ev.Player))
                ev.IsAllowed = false;
        }

        private void StunnedTantrum(Exiled.Events.EventArgs.Scp173.PlacingTantrumEventArgs ev)
        {
            if (IsStunned(ev.Player))
                ev.IsAllowed = false;
        }

        private void StunnedBreakneck(Exiled.Events.EventArgs.Scp173.UsingBreakneckSpeedsEventArgs ev)
        {
            if (IsStunned(ev.Player))
                ev.IsAllowed = false;
        }

        // Keeps a stunned player on their ragdoll whatever movement they try.
        private IEnumerator<float> PinStunned(Player target, Stun stun)
        {
            while (stuns.TryGetValue(target, out var current) && ReferenceEquals(current, stun) && target.IsConnected && target.Role.Base == stun.Role)
            {
                if ((target.Position - stun.Position).sqrMagnitude > .09f)
                    target.Position = stun.Position;
                yield return MEC.Timing.WaitForSeconds(.1f);
            }
        }

        private void ClearStuns()
        {
            foreach (var pair in new List<KeyValuePair<Player, Stun>>(stuns))
                Recover(pair.Key, pair.Value, false);
            stuns.Clear();
            stunImmunity.Clear();
        }

        internal bool IsStunned(Player player) => player != null && stuns.ContainsKey(player);

        // Returns true when the hit was handled as a stun shot.
        private bool StunHit(HurtingEventArgs ev, WeaponState state)
        {
            if (state.Definition.Id != StunGunId)
                return false;
            var target = ev.Player;
            if (target.Health <= 1)
            {
                ev.IsAllowed = false;
                ev.Amount = 0;
            }
            else
                ev.Amount = 1;
            Safe.Run(() => StunTarget(target));
            return true;
        }

        private void StunTarget(Player target)
        {
            var cfg = Config.Tuning.StunGun;
            if (target == null || !target.IsAlive || stuns.ContainsKey(target) || !(target.Role is FpcRole fpc))
                return;
            if (stunImmunity.TryGetValue(target, out float until) && until > Time.time)
                return;
            var ragdoll = Ragdoll.CreateAndSpawn(target.Role.Type, target.Nickname, "Stunned", target.Position, target.Rotation);
            var stun = new Stun { Role = target.Role.Base, Ragdoll = ragdoll, Position = target.Position };
            stuns[target] = stun;
            // Hide the body standing over its own ragdoll two ways: EXILED's ghost mode
            // and the game's own SCP-268 invisibility, which also covers dummies.
            fpc.IsInvisible = true;
            target.EnableEffect(EffectType.Invisible, cfg.StunSeconds);
            target.EnableEffect(EffectType.Ensnared, cfg.StunSeconds);
            if (cfg.BlindTarget)
                target.EnableEffect(EffectType.Blinded, cfg.StunSeconds);
            target.ShowHint("<b>You were stunned!</b>", cfg.StunSeconds);
            lifetime.Run(PinStunned(target, stun));
            lifetime.Later(cfg.StunSeconds, () => Recover(target, stun, true));
        }

        private void Recover(Player target, Stun stun, bool grantImmunity)
        {
            if (!stuns.TryGetValue(target, out var current) || !ReferenceEquals(current, stun))
                return;
            stuns.Remove(target);
            try
            {
                if (stun.Ragdoll?.Base != null)
                    stun.Ragdoll.Destroy();
            }
            catch (System.Exception e)
            {
                Log.Error($"[Stun Gun] Could not remove the stun ragdoll of {target.Nickname}: {e}");
            }
            if (target.IsConnected && target.Role.Base == stun.Role && target.Role is FpcRole fpc)
            {
                fpc.IsInvisible = false;
                target.DisableEffect(EffectType.Invisible);
                target.DisableEffect(EffectType.Ensnared);
                target.DisableEffect(EffectType.Blinded);
            }

            if (grantImmunity)
                stunImmunity[target] = Time.time + Config.Tuning.StunGun.TargetCooldownSeconds;
        }

        private void StunRoleChanging(ChangingRoleEventArgs ev)
        {
            if (ev.IsAllowed && stuns.TryGetValue(ev.Player, out var stun))
                Recover(ev.Player, stun, false);
        }

        private void StunLeft(LeftEventArgs ev)
        {
            if (stuns.TryGetValue(ev.Player, out var stun))
                Recover(ev.Player, stun, false);
            stunImmunity.Remove(ev.Player);
        }

        private sealed class Stun
        {
            internal PlayerRoleBase Role;
            internal Ragdoll Ragdoll;
            internal Vector3 Position;
        }
    }
}
