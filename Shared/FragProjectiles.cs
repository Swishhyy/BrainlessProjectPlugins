using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Pickups.Projectiles;
using Exiled.Events.EventArgs.Map;
using UnityEngine;

namespace Brainless.Internal
{
    // Compiled separately into Items and Weapons. Real native grenades provide
    // physics, fuse, sound and explosion VFX; only our damage is replaced.
    internal sealed class FragProjectiles
    {
        private readonly Dictionary<int, Frag> active = new Dictionary<int, Frag>();
        internal ExplosionGrenadeProjectile Launch(Vector3 position, Vector3 velocity, float fuse, DamageSource source, float radius, float humanPeak, float scpPeak, BlastBudget budget = null)
        {
            var grenade = Projectile.Create<ExplosionGrenadeProjectile>(ProjectileType.FragGrenade);
            grenade.FuseTime = fuse;
            grenade.MaxRadius = radius;
            grenade.BurnDuration = 0;
            grenade.DeafenDuration = 0;
            grenade.ConcussDuration = 0;
            active.Add(grenade.Serial, new Frag(grenade, source, radius, humanPeak, scpPeak, budget));
            try
            {
                grenade.Spawn(position, shouldBeActive: false, previousOwner: source.Owner ?? Server.Host);
                grenade.Base.PreviousOwner = source.Footprint;
                if (grenade.Rigidbody != null)
                {
                    grenade.Rigidbody.linearVelocity = velocity;
                    grenade.Rigidbody.angularVelocity = UnityEngine.Random.onUnitSphere * 12;
                }

                // Start the native fuse only after ownership, tracking and velocity exist.
                grenade.Activate();
                return grenade;
            }
            catch
            {
                active.Remove(grenade.Serial);
                if (grenade.Base != null)
                    grenade.Destroy();
                throw;
            }
        }

        internal bool Exploding(ExplodingGrenadeEventArgs ev)
        {
            if (!active.TryGetValue(ev.Projectile.Serial, out var frag))
                return false;
            active.Remove(ev.Projectile.Serial);
            bool allowed = ev.IsAllowed;
            ev.IsAllowed = false;
            // EffectGrenade.ServerFuseEnd already sends the native explosion VFX
            // and removes the projectile, even when EXILED's damage event is denied.
            if (!allowed)
                return true;
            foreach (var target in ev.TargetsToAffect.Where(frag.Source.CanAffect).ToArray())
            {
                float amount = Rules.Falloff(Vector3.Distance(ev.Position, target.Position), frag.Radius, target.IsScp ? frag.ScpPeak : frag.HumanPeak);
                if (amount <= 0 || !World.ClearLine(ev.Position + Vector3.up * .12f, target.Position))
                    continue;
                if (frag.Budget != null)
                    amount = frag.Budget.Take(target.UserId, target.IsScp, amount);
                frag.Source.Blast(target, ev.Position, amount);
            }

            return true;
        }

        internal void Clear()
        {
            foreach (var frag in active.Values.ToArray())
                if (frag.Projectile.Base != null)
                    Safe.Run(() => frag.Projectile.Destroy());
            active.Clear();
        }

        private sealed class Frag
        {
            internal readonly ExplosionGrenadeProjectile Projectile;
            internal readonly DamageSource Source;
            internal readonly float Radius, HumanPeak, ScpPeak;
            internal readonly BlastBudget Budget;
            internal Frag(ExplosionGrenadeProjectile projectile, DamageSource source, float radius, float humanPeak, float scpPeak, BlastBudget budget)
            {
                Projectile = projectile;
                Source = source;
                Radius = radius;
                HumanPeak = humanPeak;
                ScpPeak = scpPeak;
                Budget = budget;
            }
        }
    }
}
