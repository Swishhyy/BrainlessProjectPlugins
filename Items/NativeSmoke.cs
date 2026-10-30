using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features.Pickups;
using HarmonyLib;
using InventorySystem.Items.Usables.Scp244;
using UnityEngine;

namespace Brainless.Items
{
    // Reuse the game's networked SCP-244 fog and particle renderer. No primitives,
    // client asset downloads, amnesia, server hypothermia, or damage-over-time.
    internal static class NativeSmoke
    {
        private static readonly Dictionary<Scp244DeployablePickup, byte> owned = new Dictionary<Scp244DeployablePickup, byte>();
        private static Harmony harmony;
        internal static void Enable()
        {
            harmony = new Harmony("brainless.items.smoke");
            try
            {
                harmony.Patch(AccessTools.Method(typeof(Scp244DeployablePickup), nameof(Scp244DeployablePickup.FogPercentForPoint)), prefix: new HarmonyMethod(typeof(NativeSmoke), nameof(Exposure)));
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
            owned.Remove(pickup.Base);
            if (pickup.Base != null)
                pickup.Destroy();
        }

        internal static void Clear()
        {
            foreach (var cloud in owned.Keys.ToArray())
                if (cloud != null)
                    Exiled.API.Features.Pickups.Pickup.Get(cloud)?.Destroy();
            owned.Clear();
        }

        internal static void Disable()
        {
            Clear();
            harmony?.UnpatchAll(harmony.Id);
            harmony = null;
        }

        private static bool Exposure(Scp244DeployablePickup __instance, ref float __result)
        {
            if (!owned.ContainsKey(__instance))
                return true;
            // This patch exists on the server only. Clients keep the native fog
            // visuals, while server cold damage and SCP shield drain stay zero.
            __result = 0;
            return false;
        }

        private static bool FixedRadius(Scp244DeployablePickup __instance)
        {
            if (!owned.TryGetValue(__instance, out byte size))
                return true;
            __instance.CurrentSizePercent = size / 255f;
            __instance.Network_syncSizePercent = size;
            return false;
        }
    }
}
