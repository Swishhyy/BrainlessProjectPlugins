using System;
using Exiled.API.Enums;
using Exiled.API.Features;
using UnityEngine;

namespace Brainless.Roles
{
    internal sealed class QuietStepLayer
    {
        private Player owner;
        private byte applied, previous;
        private float previousExpiry;
        internal void Set(Player player, byte stacks)
        {
            if (stacks == 0)
            {
                Clear();
                return;
            }

            owner = player;
            var effect = player.GetEffect(EffectType.SilentWalk);
            if (effect == null)
                return;
            bool external = effect.IsEnabled && (applied == 0 || effect.Intensity != applied || effect.TimeLeft > .65f || effect.Duration <= 0);
            if (external)
            {
                previous = effect.Intensity;
                previousExpiry = effect.Duration <= 0 ? float.PositiveInfinity : Time.time + effect.TimeLeft;
                applied = 0;
            }

            if (effect.IsEnabled && effect.Intensity >= stacks && applied == 0)
                return;
            if (!effect.IsEnabled && applied == 0)
            {
                previous = 0;
                previousExpiry = 0;
            }

            player.EnableEffect(EffectType.SilentWalk, stacks, .6f);
            applied = stacks;
        }

        internal void Clear()
        {
            if (owner != null && owner.IsConnected && applied > 0)
            {
                var effect = owner.GetEffect(EffectType.SilentWalk);
                if (effect != null && effect.Intensity == applied && effect.TimeLeft <= .65f && effect.Duration > 0)
                {
                    if (previous > 0 && previousExpiry > Time.time)
                        owner.EnableEffect(EffectType.SilentWalk, previous, float.IsPositiveInfinity(previousExpiry) ? 0 : Math.Max(.01f, previousExpiry - Time.time));
                    else
                        owner.DisableEffect(EffectType.SilentWalk);
                }
            }

            owner = null;
            applied = previous = 0;
        }
    }
}
