using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features.Pickups;
using HarmonyLib;
using InventorySystem.Items.Usables.Scp244;
using InventorySystem.Items.Usables.Scp244.Hypothermia;
using Mirror;
using UnityEngine;
using ColdEffect = InventorySystem.Items.Usables.Scp244.Hypothermia.Hypothermia;

namespace Brainless.Items
{
    internal static class NativeSmoke
    {
        private static readonly Dictionary<Scp244DeployablePickup, byte> owned = new Dictionary<Scp244DeployablePickup, byte>();
        private static readonly Dictionary<ColdEffect, ExposureSample> exposures = new Dictionary<ColdEffect, ExposureSample>();
        private static Harmony harmony;
        internal static void Enable()
        {
            harmony = new Harmony("brainless.items.smoke");
            try
            {
                // Keep native exposure/visual signalling. Suppress only gameplay
                // subeffects caused by our clouds, retaining ordinary SCP-244 exposure.
                harmony.Patch(AccessTools.Method(typeof(ColdEffect), nameof(ColdEffect.UpdateSubEffect)), prefix: new HarmonyMethod(typeof(NativeSmoke), nameof(GuardSubEffect)));
                harmony.Patch(AccessTools.Method(typeof(Scp244DeployablePickup), nameof(Scp244DeployablePickup.UpdateRange)), prefix: new HarmonyMethod(typeof(NativeSmoke), nameof(FixedRadius)));
            }
            catch
            {
                Disable();
                throw;
            }
        }

        internal static Scp244Pickup Spawn(Vector3 position, float radius)
        {
            var pickup = Pickup.Create<Scp244Pickup>(ItemType.SCP244a);
            byte size = (byte)Mathf.Clamp(Mathf.RoundToInt(radius * 2 / Mathf.Max(.01f, pickup.MaxDiameter) * 255), 1, 255);
            owned.Add(pickup.Base, size);
            try
            {
                pickup.IsLocked = true;
                pickup.Health = float.MaxValue;
                pickup.State = Scp244State.Active;
                pickup.Base.CurrentSizePercent = size / 255f;
                pickup.Base.Network_syncSizePercent = size;
                pickup.Spawn(position);
                // Initialize at its world position instead of waiting for bottle movement.
                pickup.Base.State = Scp244State.Active;
                pickup.Base._lifeTime.Restart();
                pickup.Base._previousPos = Vector3.positiveInfinity;
                pickup.Base._lastUpdateTime = -100;
                pickup.Base.UpdateCurrentRoom();
                pickup.Base.UpdateConditions();
                if (pickup.Rigidbody != null)
                    pickup.Rigidbody.isKinematic = true;
                return pickup;
            }
            catch
            {
                Release(pickup);
                throw;
            }
        }

        internal static void Release(Scp244Pickup pickup)
        {
            if (pickup == null)
                return;
            if (pickup.Base != null)
            {
                pickup.State = Scp244State.Idle;
                pickup.Base.CurrentSizePercent = 0;
                pickup.Base.Network_syncSizePercent = 0;
                pickup.Destroy();
            }

            owned.Remove(pickup.Base);
            exposures.Clear();
        }

        internal static void Clear()
        {
            foreach (var cloud in owned.Keys.ToArray())
                if (cloud != null && Pickup.Get(cloud)is Scp244Pickup pickup)
                    Release(pickup);
            owned.Clear();
            exposures.Clear();
        }

        internal static void Disable()
        {
            Clear();
            harmony?.UnpatchAll(harmony.Id);
            harmony = null;
        }

        private static void GuardSubEffect(ColdEffect __instance, HypothermiaSubEffectBase __0, ref float __1)
        {
            if (!NetworkServer.active || owned.Count == 0)
                return;
            if (!(__0 is DamageSubEffect) && !(__0 is HumeShieldSubEffect) && !(__0 is TemperatureSubEffect) && !(__0 is InstantStatusSubEffect) && !(__0 is AttackCooldownSubEffect))
                return;
            if (!exposures.TryGetValue(__instance, out var sample) || sample.Frame != Time.frameCount)
            {
                float natural = 0;
                var camera = __instance.Hub.PlayerCameraReference.position;
                foreach (var cloud in Scp244DeployablePickup.Instances)
                    if (cloud != null && !owned.ContainsKey(cloud))
                        natural += cloud.FogPercentForPoint(camera);
                if (__instance._isForced)
                    natural += __instance._forcedExposure;
                sample = new ExposureSample
                {
                    Frame = Time.frameCount,
                    Value = natural
                };
                exposures[__instance] = sample;
            }

            // Never reintroduce exposure that native immunity or another plugin
            // already removed (for example SCP-939's exemption).
            __1 = Mathf.Min(__1, sample.Value);
        }

        private static bool FixedRadius(Scp244DeployablePickup __instance)
        {
            if (!owned.TryGetValue(__instance, out byte size))
                return true;
            __instance.State = Scp244State.Active;
            __instance.CurrentSizePercent = size / 255f;
            __instance.Network_syncSizePercent = size;
            return false;
        }

        private sealed class ExposureSample
        {
            internal int Frame;
            internal float Value;
        }
    }
}
