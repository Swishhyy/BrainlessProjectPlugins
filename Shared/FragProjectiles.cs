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
    // Compiled separately into Roles, Items and Weapons. Real native grenades provide
    // physics, fuse, sound and explosion VFX; only our damage is replaced.
    internal sealed class FragProjectiles
    {
        private readonly Dictionary<int, Frag> active = new Dictionary<int, Frag>();
        internal int Count => active.Count;

        internal ExplosionGrenadeProjectile Launch(Vector3 position, Vector3 velocity, float fuse, DamageSource source, float radius, float humanPeak, float scpPeak, BlastBudget budget = null, bool requireLineOfSight = true, bool affectOwner = true, int armorPenetration = 50, bool damageEnvironment = false)
        {
            var grenade = Projectile.Create<ExplosionGrenadeProjectile>(ProjectileType.FragGrenade);
            grenade.FuseTime = fuse;
            grenade.MaxRadius = radius;
            grenade.BurnDuration = 0;
            grenade.DeafenDuration = 0;
            grenade.ConcussDuration = 0;
            Track(grenade, source, radius, humanPeak, scpPeak, budget, requireLineOfSight, affectOwner, armorPenetration, damageEnvironment);
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

        internal void Track(ExplosionGrenadeProjectile grenade, DamageSource source, float radius, float humanPeak, float scpPeak, BlastBudget budget = null, bool requireLineOfSight = true, bool affectOwner = true, int armorPenetration = 50, bool damageEnvironment = false)
        {
            grenade.MaxRadius = radius;
            grenade.BurnDuration = grenade.DeafenDuration = grenade.ConcussDuration = 0;
            active[grenade.Serial] = new Frag(grenade, source, radius, humanPeak, scpPeak, budget, requireLineOfSight, affectOwner, armorPenetration, damageEnvironment);
        }

        internal bool Exploding(ExplodingGrenadeEventArgs ev)
        {
            if (ev.Projectile == null || !active.TryGetValue(ev.Projectile.Serial, out var frag))
                return false;
            active.Remove(ev.Projectile.Serial);
            bool allowed = ev.IsAllowed;
            ev.IsAllowed = false;
            // EffectGrenade.ServerFuseEnd already sends the native explosion VFX
            // and removes the projectile, even when EXILED's damage event is denied.
            if (!allowed)
                return true;
            var targets = ev.TargetsToAffect.ToArray();
            if (frag.DamageEnvironment)
            {
                // EXILED trims only player hitboxes from the native overlap set.
                // Keep doors, glass and prop physics without a second player hit.
                // Clear before custom damage, including when a damage hook fails.
                ev.TargetsToAffect.Clear();
                ev.IsAllowed = true;
            }

            foreach (var target in targets.Where(frag.Source.CanAffect))
            {
                float amount = Rules.Falloff(Vector3.Distance(ev.Position, target.Position), frag.Radius, target.IsScp ? frag.ScpPeak : frag.HumanPeak);
                if (amount <= 0 || (!frag.AffectOwner && target == frag.Source.Owner) || (frag.RequireLineOfSight && !ClearBlast(ev.Position, target)))
                    continue;
                if (frag.Budget != null)
                    amount = frag.Budget.Take(target.UserId, target.IsScp, amount);
                frag.Source.Blast(target, ev.Position, amount, frag.ArmorPenetration);
            }

            return true;
        }

        internal static bool ClearBlast(Vector3 origin, Player target) => !Physics.Linecast(origin + Vector3.up * .12f, target.Position, InventorySystem.Items.ThrowableProjectiles.ExplosionGrenade.HitBlockerMask, QueryTriggerInteraction.Ignore);
        internal void Prune()
        {
            foreach (var pair in active.Where(p => p.Value.Projectile.Base == null || Time.time >= p.Value.Expires).ToArray())
            {
                active.Remove(pair.Key);
                if (pair.Value.Projectile.Base != null)
                    Safe.Run(() => pair.Value.Projectile.Destroy());
            }
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
            internal readonly bool RequireLineOfSight, AffectOwner, DamageEnvironment;
            internal readonly float Expires;
            internal readonly int ArmorPenetration;
            internal Frag(ExplosionGrenadeProjectile projectile, DamageSource source, float radius, float humanPeak, float scpPeak, BlastBudget budget, bool requireLineOfSight, bool affectOwner, int armorPenetration, bool damageEnvironment)
            {
                Projectile = projectile;
                Source = source;
                Radius = radius;
                HumanPeak = humanPeak;
                ScpPeak = scpPeak;
                Budget = budget;
                RequireLineOfSight = requireLineOfSight;
                AffectOwner = affectOwner;
                DamageEnvironment = damageEnvironment;
                ArmorPenetration = armorPenetration;
                Expires = Time.time + Math.Max(.05f, projectile.FuseTime) + 5;
            }
        }
    }
}
