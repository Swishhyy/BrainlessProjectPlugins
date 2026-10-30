using Brainless.Internal;
using Exiled.API.Features.Pickups.Projectiles;
using UnityEngine;

namespace Brainless.Items
{
    // Lives only on a tracked impact projectile; ordinary grenades are untouched.
    public sealed class ImpactGrenade : MonoBehaviour
    {
        private TimeGrenadeProjectile projectile;
        private ReferenceHub owner;
        private ImpactTrigger trigger;
        internal void Initialize(TimeGrenadeProjectile projectile, ReferenceHub owner, ImpactTuning config)
        {
            this.projectile = projectile;
            this.owner = owner;
            trigger = new ImpactTrigger(Time.time + config.ArmingDelaySeconds, config.DetonateOnPlayers);
        }

        private void OnCollisionEnter(Collision collision) => Touch(collision);
        private void OnCollisionStay(Collision collision) => Touch(collision);
        private void Touch(Collision collision)
        {
            var collider = collision?.collider;
            if (trigger == null || projectile == null || projectile.Base == null || projectile.IsAlreadyDetonated || collider == null || collider.isTrigger)
                return;
            var player = collider.GetComponentInParent<ReferenceHub>();
            if (!trigger.TryContact(Time.time, player != null, player != null && player == owner))
                return;
            Safe.Run(() => projectile.Explode());
        }
    }
}
