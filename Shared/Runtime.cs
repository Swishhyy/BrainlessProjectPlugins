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

        internal void Blast(Player target, Vector3 position, float amount, int armorPenetration = 50)
        {
            if (amount > 0 && CanAffect(target))
                // Native zero penetration returns before damage/attacker setup.
                // Its force argument is a vector, not the world-space blast origin.
                target.Hurt(new PlayerStatsSystem.ExplosionDamageHandler(Footprint, Vector3.zero, amount, ExplosionRules.Penetration(armorPenetration), global::ExplosionType.Grenade));
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
        internal static void Play(Lifetime lifetime, Vector3 position, float seconds, string kind, Func<Vector3> follow = null, float volume = AudioRules.EffectVolume, float range = AudioRules.EffectRange, float fullVolumeRange = AudioRules.FullVolumeRange)
        {
            Safe.Run(() => lifetime.Run(Stream(lifetime, position, seconds, kind, follow, volume, range, fullVolumeRange)));
        }

        private static IEnumerator<float> Stream(Lifetime lifetime, Vector3 position, float seconds, string kind, Func<Vector3> follow, float volume, float range, float fullVolumeRange)
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
            speaker.Volume = volume;
            speaker.MinDistance = AudioRules.FullVolumeDistance(fullVolumeRange, range);
            speaker.MaxDistance = range;
            speaker.Spawn();
            var random = new System.Random();
            VoiceChat.Codec.OpusEncoder encoder = null;
            try
            {
                encoder = new VoiceChat.Codec.OpusEncoder(VoiceChat.Codec.Enums.OpusApplicationType.Audio);
                var samples = new float[960];
                var bytes = new byte[4096];
                // Prefer the game's own human footstep clips; generated steps are only a fallback.
                Func<double, float> steps = null;
                if (kind == "steps")
                {
                    var native = NativeFootsteps.Clips;
                    if (native.Count > 0)
                        steps = new NativeStepPlayer(random, native).Sample;
                    else
                        steps = new FootstepSynth(random).Sample;
                }

                int sample = 0;
                for (int frame = 0; frame < seconds * 50; frame++)
                {
                    if (follow != null)
                        speaker.Position = follow();
                    for (int j = 0; j < samples.Length; j++, sample++)
                    {
                        double time = sample / 48000.0;
                        if (steps != null)
                        {
                            samples[j] = steps(time);
                            continue;
                        }

                        double phase = time % .55;
                        double envelope = phase < .08 ? Math.Sin(phase / .08 * Math.PI) : 0;
                        samples[j] = (float)(envelope * .4 * Math.Sin(time * 2 * Math.PI * 920));
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

    // The footstep sounds the game itself plays for human characters, read once from
    // the Class-D character model (the clips every client already has) and converted
    // to 48 kHz mono for the speaker stream. Empty when the server build carries no
    // audio data, in which case callers fall back to generated steps.
    internal static class NativeFootsteps
    {
        private static List<float[]> clips;

        internal static IReadOnlyList<float[]> Clips => clips ?? (clips = Load());

        private static List<float[]> Load()
        {
            var result = new List<float[]>();
            try
            {
                if (!PlayerRoles.PlayerRoleLoader.TryGetRoleTemplate(PlayerRoles.RoleTypeId.ClassD, out PlayerRoles.FirstPersonControl.FpcStandardRoleBase role) || role.FpcModule == null)
                    throw new InvalidOperationException("the Class-D role template is missing");
                const System.Reflection.BindingFlags any = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
                var template = typeof(PlayerRoles.FirstPersonControl.FirstPersonMovementModule).GetField("CharacterModelTemplate", any)?.GetValue(role.FpcModule) as GameObject;
                var model = template == null ? null : template.GetComponentInChildren<PlayerRoles.FirstPersonControl.Thirdperson.AnimatedCharacterModel>(true);
                var found = model == null ? null : typeof(PlayerRoles.FirstPersonControl.Thirdperson.AnimatedCharacterModel).GetField("_footstepClips", any)?.GetValue(model) as AudioClip[];
                foreach (var clip in found ?? Array.Empty<AudioClip>())
                {
                    float[] mono = Read(clip);
                    if (mono != null)
                        result.Add(mono);
                }

                if (result.Count == 0)
                    throw new InvalidOperationException("no readable footstep clips");
            }
            catch (Exception exception)
            {
                Log.Warn("[Brainless] Game footstep sounds are unavailable on this server (" + exception.Message + "); Echo Canister uses generated footsteps.");
                result.Clear();
            }

            return result;
        }

        private static float[] Read(AudioClip clip)
        {
            if (clip == null || clip.samples <= 0 || clip.channels <= 0 || clip.frequency <= 0)
                return null;
            if (clip.loadState != AudioDataLoadState.Loaded)
                clip.LoadAudioData();
            var raw = new float[clip.samples * clip.channels];
            if (!clip.GetData(raw, 0) || raw.All(v => v == 0))
                return null;
            int frames = clip.samples, channels = clip.channels;
            int length = (int)Math.Ceiling(frames * 48000.0 / clip.frequency);
            var output = new float[length];
            for (int i = 0; i < length; i++)
            {
                double position = i * (double)clip.frequency / 48000;
                int index = Math.Min(frames - 1, (int)position);
                int next = Math.Min(frames - 1, index + 1);
                double blend = position - index;
                float a = 0, b = 0;
                for (int c = 0; c < channels; c++)
                {
                    a += raw[index * channels + c];
                    b += raw[next * channels + c];
                }

                output[i] = (float)((a + (b - a) * blend) / channels);
            }

            return output;
        }
    }

    // Plays a random game footstep clip per step at an uneven walking cadence.
    internal sealed class NativeStepPlayer
    {
        private readonly System.Random random;
        private readonly IReadOnlyList<float[]> clips;
        private float[] current;
        private double start, nextStep = .05;
        private float gain;

        internal NativeStepPlayer(System.Random random, IReadOnlyList<float[]> clips)
        {
            this.random = random;
            this.clips = clips;
        }

        internal float Sample(double time)
        {
            if (time >= nextStep)
            {
                current = clips[random.Next(clips.Count)];
                start = time;
                gain = .85f + (float)random.NextDouble() * .15f;
                nextStep = time + .47 + random.NextDouble() * .09;
            }

            if (current == null)
                return 0;
            int index = (int)((time - start) * 48000);
            return index >= 0 && index < current.Length ? Math.Max(-1, Math.Min(1, current[index] * gain)) : 0;
        }
    }

    // Boot steps on a hard floor: each step is a heel strike followed by a softer
    // toe contact. Both are short bursts of low-passed noise with a low body thump
    // and a faint scuff, at an uneven walking cadence that alternates feet.
    internal sealed class FootstepSynth
    {
        private readonly System.Random random;
        private double nextStep = .05, heel = -1, toe = -1, heelGain, toeGain, thump;
        private double low, lower, previous;
        private int step;

        internal FootstepSynth(System.Random random) => this.random = random;

        internal float Sample(double time)
        {
            if (time >= nextStep)
            {
                heel = time;
                heelGain = (step % 2 == 0 ? 1 : .82) * (.85 + random.NextDouble() * .15);
                toe = time + .045 + random.NextDouble() * .025;
                toeGain = heelGain * (.45 + random.NextDouble() * .15);
                thump = 62 + random.NextDouble() * 22;
                step++;
                nextStep = time + .47 + random.NextDouble() * .09;
            }

            double noise = random.NextDouble() * 2 - 1;
            low += (noise - low) * .09;
            lower += (low - lower) * .25;
            double scuff = noise - previous;
            previous = noise;
            double body = 0, click = 0, boom = 0;
            if (heel >= 0 && time - heel < .3)
            {
                double age = time - heel;
                body += heelGain * Math.Exp(-age * 48);
                click += heelGain * Math.Exp(-age * 160);
                boom += heelGain * Math.Exp(-age * 30) * Math.Sin(2 * Math.PI * thump * age);
            }

            if (toe >= 0 && time >= toe && time - toe < .25)
            {
                double age = time - toe;
                body += toeGain * Math.Exp(-age * 65);
                click += toeGain * Math.Exp(-age * 200);
            }

            double value = body * lower * 2.4 + boom * .42 + click * scuff * .1;
            return (float)Math.Max(-1, Math.Min(1, value));
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
