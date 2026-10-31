using System;
using System.Collections.Generic;
using System.Linq;
using Brainless.Internal;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.API.Features.Toys;
using Exiled.Events.EventArgs.Player;
using MEC;
using UnityEngine;

namespace Brainless.Weapons
{
    public sealed partial class Plugin
    {
        private readonly Dictionary<Player, RailCharge> railCharges = new Dictionary<Player, RailCharge>();
        private void RailReset() => railCharges.Clear();
        private void StartRailgun(ShootingEventArgs ev, WeaponState state)
        {
            ev.IsAllowed = false;
            var cfg = Config.Tuning.Railgun;
            if (railCharges.ContainsKey(ev.Player) || railCharges.Count >= cfg.MaxConcurrentCharges || shots.Remaining(ev.Item.Serial) < 1)
            {
                ev.Player.ShowHint("Railgun unavailable: empty, already charging, or charge limit reached.", 2);
                return;
            }

            var charge = new RailCharge
            {
                Gun = ev.Firearm,
                Role = ev.Player.Role.Base,
                Tuning = cfg,
                State = state
            };
            railCharges[ev.Player] = charge;
            ev.Player.ShowHint("Railgun charging...", 2);
            PlayAudio(ev.Player.Position, cfg.ChargeSeconds + cfg.DischargeDelaySeconds, "beep");
            lifetime.Run(ChargeRailgun(ev.Player, charge));
        }

        private bool RailValid(Player player, RailCharge charge) => player.IsConnected && player.IsAlive && player.Role.Base == charge.Role && player.CurrentItem == charge.Gun && states.TryGetValue(charge.Gun.Serial, out var current) && ReferenceEquals(current, charge.State) && railCharges.TryGetValue(player, out var owned) && ReferenceEquals(owned, charge);
        private IEnumerator<float> ChargeRailgun(Player player, RailCharge charge)
        {
            var cfg = charge.Tuning;
            float started = Time.time, nextHint = 0;
            try
            {
                if (cfg.ShowChargingEffects)
                    for (int i = 0; i < 3 * cfg.RingSegments; i++)
                        charge.Markers.Add(World.Marker(lifetime, player.CameraTransform.position, Color.cyan, Vector3.one * .025f, PrimitiveType.Sphere));
                while (Time.time - started < cfg.ChargeSeconds + cfg.DischargeDelaySeconds)
                {
                    if (!RailValid(player, charge))
                        yield break;
                    float elapsed = Time.time - started;
                    if (cfg.ShowChargeProgress && Time.time >= nextHint)
                    {
                        nextHint = Time.time + .5f;
                        player.ShowHint(elapsed < cfg.ChargeSeconds ? $"Railgun charging: {Math.Min(100, elapsed / cfg.ChargeSeconds * 100):0}%" : "Railgun discharging...", .6f);
                    }

                    for (int i = 0; i < charge.Markers.Count; i++)
                    {
                        int ring = i / cfg.RingSegments;
                        float angle = i % cfg.RingSegments * Mathf.PI * 2 / cfg.RingSegments + elapsed * (ring == 1 ? -3 : 3);
                        float progress = Mathf.Clamp01(elapsed / cfg.ChargeSeconds);
                        float radius = (.12f + ring * .07f) * (1 + progress * .3f);
                        charge.Markers[i].Position = player.CameraTransform.position + player.CameraTransform.forward * .8f + player.CameraTransform.right * (Mathf.Cos(angle) * radius) + player.CameraTransform.up * (Mathf.Sin(angle) * radius);
                        charge.Markers[i].Color = Color.Lerp(Color.blue, Color.cyan, progress) * (1 + Mathf.Sin(elapsed * 5) * .15f);
                    }

                    yield return Timing.WaitForSeconds(.05f);
                }

                if (!RailValid(player, charge) || !shots.Consume(charge.Gun.Serial))
                    yield break;
                charge.Gun.BarrelAmmo = 0;
                charge.Gun.MagazineAmmo = shots.Remaining(charge.Gun.Serial);
                FireRailgun(player, charge);
            }
            finally
            {
                foreach (var marker in charge.Markers)
                    lifetime.Release(marker);
                if (railCharges.TryGetValue(player, out var current) && ReferenceEquals(current, charge))
                    railCharges.Remove(player);
            }
        }

        private void FireRailgun(Player player, RailCharge charge)
        {
            var cfg = charge.Tuning;
            Vector3 from = player.CameraTransform.position, direction = player.CameraTransform.forward.normalized;
            float length = cfg.Range;
            RaycastHit? obstruction = null;
            foreach (var hit in Physics.RaycastAll(from, direction, cfg.Range, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance))
            {
                if (hit.collider.GetComponentInParent<ReferenceHub>() != null || hit.collider.GetComponentInParent<AdminToys.AdminToyBase>() != null)
                    continue;
                obstruction = hit;
                length = hit.distance;
                break;
            }

            Vector3 end = from + direction * length;
            RailBeam(from, end, cfg.BeamSeconds);
            var source = new DamageSource(player);
            foreach (var target in Player.List.Where(p => p != player && source.CanAffect(p)).OrderBy(p => Vector3.Distance(from, p.Position)).ToArray())
            {
                Vector3 delta = target.Position - from;
                float forward = Vector3.Dot(delta, direction), side = (delta - forward * direction).magnitude;
                if (!SupRules.InsideBeam(forward, side, length, cfg.BeamRadius) || !World.ClearLine(from, target.Position))
                    continue;
                source.Hurt(target, target.IsScp ? cfg.ScpDamage : cfg.HumanDamage, "Railgun beam");
                target.ShowHint("Hit by a railgun beam!", 2);
            }

            if (cfg.ImpactExplosion && obstruction.HasValue)
                grenades.Launch(end + obstruction.Value.normal * .1f, Vector3.zero, .05f, source, cfg.ImpactRadius, cfg.ImpactHumanDamage, cfg.ImpactScpDamage, requireLineOfSight: true, affectOwner: false);
            PlayAudio(from, .15f, "beep");
            player.ShowHint($"Railgun fired: {shots.Remaining(charge.Gun.Serial)} charges remaining.", 3);
        }

        private void RailBeam(Vector3 from, Vector3 to, float duration)
        {
            Vector3 delta = to - from;
            if (delta.sqrMagnitude < .01f)
                return;
            for (int layer = 0; layer < 2; layer++)
            {
                float width = layer == 0 ? .04f : .08f;
                var beam = World.Marker(lifetime, (from + to) * .5f, layer == 0 ? Color.cyan : new Color(.15f, .4f, 1, .4f), new Vector3(width, delta.magnitude / 2, width));
                beam.Rotation = Quaternion.LookRotation(delta) * Quaternion.Euler(90, 0, 0);
                lifetime.Run(FadeBeam(beam, duration));
            }
        }

        private IEnumerator<float> FadeBeam(Primitive beam, float duration)
        {
            float started = Time.time;
            Color initial = beam.Color;
            while (beam != null && Time.time - started < duration)
            {
                Color color = initial;
                color.a *= 1 - (Time.time - started) / duration;
                beam.Color = color;
                yield return Timing.WaitForSeconds(.05f);
            }

            lifetime.Release(beam);
        }

        private void DiscardEmptyLauncher(int serial)
        {
            if (!Config.LauncherDiscardWhenEmpty || shots.Remaining(serial) > 0 || Find(serial)?.Definition.Id != 7207)
                return;
            foreach (var player in Player.List)
            {
                var item = player.Items.FirstOrDefault(i => i.Serial == serial);
                if (item != null)
                {
                    player.RemoveItem(item);
                    player.ShowHint("Empty grenade launcher discarded.", 3);
                }
            }

            var pickup = Exiled.API.Features.Pickups.Pickup.List.FirstOrDefault(p => p.Serial == serial);
            pickup?.Destroy();
            Find(serial)?.Definition.TrackedSerials.Remove((ushort)serial);
            states.Remove(serial);
            shots.Forget(serial);
        }

        private sealed class RailCharge
        {
            internal Firearm Gun;
            internal PlayerRoles.PlayerRoleBase Role;
            internal WeaponState State;
            internal RailgunTuning Tuning;
            internal readonly List<Primitive> Markers = new List<Primitive>();
        }
    }
}
