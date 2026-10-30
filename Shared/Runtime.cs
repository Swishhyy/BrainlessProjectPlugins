using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Doors;
using Exiled.API.Features.Toys;
using Exiled.API.Enums;
using MEC;
using UnityEngine;

namespace Brainless.Internal
{
    internal sealed class DamageSource
    {
        internal readonly Player Owner;
        internal readonly PlayerRoles.RoleTypeId Role;
        internal readonly Footprinting.Footprint Footprint;
        internal DamageSource(Player owner)
        {
            Owner = owner;
            Role = owner?.Role.Type ?? PlayerRoles.RoleTypeId.None;
            Footprint = new Footprinting.Footprint((owner ?? Server.Host).ReferenceHub);
        }

        internal bool CanAffect(Player target) => target != null && target.IsAlive && !target.IsGodModeEnabled && (Owner == null || Owner == target || Server.FriendlyFire || Owner.IsFriendlyFireEnabled || HitboxIdentity.IsEnemy(Role, target.Role.Type));
        internal void Hurt(Player target, float amount, string reason)
        {
            if (amount <= 0 || !CanAffect(target))
                return;
            var handler = new Exiled.API.Features.DamageHandlers.GenericDamageHandler(target, Owner ?? Server.Host, amount, DamageType.Custom, null, reason, false)
            {
                Attacker = Footprint
            };
            target.Hurt(handler);
        }

        internal void Blast(Player target, Vector3 position, float amount)
        {
            if (amount > 0 && CanAffect(target))
                target.Hurt(new PlayerStatsSystem.ExplosionDamageHandler(Footprint, position, amount, 0, global::ExplosionType.Grenade));
        }
    }

    internal sealed class Lifetime
    {
        private readonly List<CoroutineHandle> handles = new List<CoroutineHandle>();
        private readonly List<AdminToy> toys = new List<AdminToy>();
        internal int Epoch { get; private set; }

        internal void Run(IEnumerator<float> routine) => handles.Add(Timing.RunCoroutine(routine));
        internal void Later(float delay, Action action)
        {
            int epoch = Epoch;
            handles.Add(Timing.CallDelayed(delay, () =>
            {
                if (epoch == Epoch)
                    Safe.Run(action);
            }));
        }

        internal T Own<T>(T toy)
            where T : AdminToy
        {
            toys.Add(toy);
            return toy;
        }

        internal void Release(AdminToy toy)
        {
            toys.Remove(toy);
            if (toy != null)
                Safe.Run(() => toy.Destroy());
        }

        internal void Clear()
        {
            Epoch++;
            foreach (CoroutineHandle handle in handles)
                Timing.KillCoroutines(handle);
            handles.Clear();
            foreach (AdminToy toy in toys.ToArray())
                Safe.Run(() => toy.Destroy());
            toys.Clear();
        }
    }

    internal sealed class MovementLayer
    {
        private Player owner;
        private EffectType type;
        private byte applied, previous;
        private float previousExpiry;
        private bool owns;
        internal bool IsOwnBoost(Player player) => owns && owner == player && type == EffectType.MovementBoost && player.GetEffect(type)?.Intensity == applied;
        internal void Set(Player player, float multiplier)
        {
            EffectType next = multiplier < 1 ? EffectType.Slowness : EffectType.MovementBoost;
            byte intensity = (byte)Math.Min(100, Math.Round(Math.Abs(multiplier - 1) * 100));
            if (owner != player || type != next || intensity == 0)
            {
                Clear();
                owner = player;
                type = next;
            }

            if (intensity == 0)
                return;
            var effect = player.GetEffect(type);
            if (effect == null)
                return;
            bool external = effect.IsEnabled && (!owns || effect.Intensity != applied || effect.TimeLeft > .65f);
            if (external)
            {
                previous = effect.Intensity;
                previousExpiry = effect.Duration <= 0 ? float.PositiveInfinity : Time.time + effect.TimeLeft;
                owns = false;
            }

            if (effect.IsEnabled && effect.Intensity >= intensity && !owns)
                return;
            if (!owns && !effect.IsEnabled)
            {
                previous = 0;
                previousExpiry = 0;
            }

            player.EnableEffect(type, intensity, .6f);
            applied = intensity;
            owns = true;
        }

        internal void Clear()
        {
            if (owner != null && owner.IsConnected && owns)
            {
                var effect = owner.GetEffect(type);
                if (effect != null && effect.Intensity == applied && effect.TimeLeft <= .65f)
                {
                    if (previous > 0 && previousExpiry > Time.time)
                        owner.EnableEffect(type, previous, float.IsPositiveInfinity(previousExpiry) ? 0 : previousExpiry - Time.time);
                    else
                        owner.DisableEffect(type);
                }
            }

            owns = false;
            previous = 0;
            owner = null;
            applied = 0;
        }
    }

    internal static class Audio
    {
        // Uses the codec and speaker supplied with the game. No audio plugin DLL.
        internal static void Play(Lifetime lifetime, Vector3 position, float seconds, string kind, Func<Vector3> follow = null)
        {
            Safe.Run(() => lifetime.Run(Stream(lifetime, position, seconds, kind, follow)));
        }

        private static IEnumerator<float> Stream(Lifetime lifetime, Vector3 position, float seconds, string kind, Func<Vector3> follow)
        {
            var occupied = new HashSet<byte>(Exiled.API.Features.Toys.AdminToy.List.OfType<Speaker>().Select(s => s.ControllerId));
            byte id = 0;
            for (int i = 180; i < 255; i++)
                if (!occupied.Contains((byte)i))
                {
                    id = (byte)i;
                    break;
                }

            if (id == 0)
                yield break;
            Speaker speaker = lifetime.Own(Speaker.Create(position, Vector3.zero, Vector3.one, false));
            speaker.ControllerId = id;
            speaker.IsSpatial = true;
            speaker.Volume = .65f;
            speaker.MinDistance = 1;
            speaker.MaxDistance = 16;
            speaker.Spawn();
            var random = new System.Random();
            VoiceChat.Codec.OpusEncoder encoder = null;
            try
            {
                encoder = new VoiceChat.Codec.OpusEncoder(VoiceChat.Codec.Enums.OpusApplicationType.Audio);
                var samples = new float[960];
                var bytes = new byte[4096];
                int sample = 0;
                for (int frame = 0; frame < seconds * 50; frame++)
                {
                    if (follow != null)
                        speaker.Position = follow();
                    for (int j = 0; j < samples.Length; j++, sample++)
                    {
                        double time = sample / 48000.0;
                        double phase = time % (kind == "steps" ? .44 : .55);
                        double envelope = kind == "steps" ? Math.Exp(-phase * 48) : phase < .08 ? Math.Sin(phase / .08 * Math.PI) : 0;
                        double noise = random.NextDouble() * 2 - 1;
                        samples[j] = (float)(envelope * (kind == "steps" ? .18 * noise + .55 * Math.Sin(time * 2 * Math.PI * 85) : .4 * Math.Sin(time * 2 * Math.PI * 920)));
                    }

                    int length = encoder.Encode(samples, bytes, samples.Length);
                    speaker.Play(bytes, length);
                    yield return Timing.WaitForSeconds(.02f);
                }
            }
            finally
            {
                encoder?.Dispose();
                lifetime.Release(speaker);
            }
        }
    }

    internal static class Safe
    {
        private static float lastError;
        internal static void Run(Action action)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                if (Time.realtimeSinceStartup - lastError > 2)
                {
                    lastError = Time.realtimeSinceStartup;
                    Log.Error($"[{typeof(Safe).Assembly.GetName().Name}] {exception}");
                }
            }
        }
    }

    internal static class World
    {
        internal static bool CanDamage(Player owner, Player target)
        {
            if (target == null || !target.IsAlive || target.IsGodModeEnabled)
                return false;
            if (owner == target)
                return true;
            return owner != null && (Server.FriendlyFire || owner.IsFriendlyFireEnabled || HitboxIdentity.IsEnemy(owner.Role.Type, target.Role.Type));
        }

        internal static bool ClearLine(Vector3 a, Vector3 b)
        {
            Vector3 delta = b - a;
            if (delta.sqrMagnitude < .0025f)
                return true;
            foreach (RaycastHit hit in Physics.RaycastAll(a, delta.normalized, delta.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider == null || hit.collider.GetComponentInParent<ReferenceHub>() != null || hit.collider.GetComponentInParent<AdminToys.AdminToyBase>() != null)
                    continue;
                return false;
            }

            return true;
        }

        internal static bool SameRoom(Vector3 a, Player player) => Room.Get(a) != null && player.CurrentRoom == Room.Get(a);
        internal static bool OrdinaryDoor(Door door) => door != null && door.IsDamageable && !door.IsGate && !door.IsCheckpoint && !door.IsPartOfCheckpoint && !door.IsElevator && !(door.Base is Interactables.Interobjects.BasicNonInteractableDoor) && door.KeycardPermissions == KeycardPermissions.None && ((Exiled.API.Interfaces.IDamageableDoor)door).IsBreakable;
        internal static void Cue(Vector3 position, string text, float radius = 12)
        {
            foreach (Player player in Player.List.Where(p => p.IsAlive && Vector3.Distance(p.Position, position) <= radius))
            {
                player.PlayBeepSound();
                player.ShowHint(text, 1.2f);
            }
        }

        internal static Primitive Marker(Lifetime lifetime, Vector3 position, Color color, Vector3 scale, PrimitiveType type = PrimitiveType.Cylinder)
        {
            Primitive marker = lifetime.Own(Primitive.Create(type, position, Vector3.zero, scale, true));
            marker.Color = color;
            marker.Collidable = false;
            return marker;
        }
    }
}
