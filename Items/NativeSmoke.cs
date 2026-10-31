using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features.Pickups;
using HarmonyLib;
using InventorySystem.Items.Usables.Scp244;
using Mirror;
using UnityEngine;

namespace Brainless.Items
{
    internal static class NativeSmoke
    {
        private static readonly Dictionary<Scp244DeployablePickup, float> owned = new Dictionary<Scp244DeployablePickup, float>();
        private static Harmony harmony;
        internal static void Enable()
        {
            harmony = new Harmony("brainless.items.smoke");
            try
            {
                harmony.Patch(AccessTools.Method(typeof(Scp244DeployablePickup), nameof(Scp244DeployablePickup.FogPercentForPoint)), prefix: new HarmonyMethod(typeof(NativeSmoke), nameof(VisualOnly)));
            }
            catch
            {
                Disable();
                throw;
            }
        }

        internal static Scp244Pickup Spawn(Vector3 position, float duration)
        {
            var pickup = Pickup.Create<Scp244Pickup>(ItemType.SCP244a);
            owned.Add(pickup.Base, Time.time + duration);
            try
            {
                pickup.IsLocked = true;
                pickup.Health = float.MaxValue;
                // Use the native FULL-size fog instead of a small size percentage
                // which can sit below the prefab's particle-emission threshold.
                pickup.State = Scp244State.Active;
                pickup.Base.CurrentSizePercent = 1;
                pickup.Base.Network_syncSizePercent = byte.MaxValue;
                // Stop only the server's native lifetime from overwriting size.
                // Clients keep the original renderer, room bounds and particles.
                pickup.Base.enabled = false;
                pickup.Spawn(position);
                pickup.Base.Network_syncState = (byte)Scp244State.Active;
                pickup.Base.Network_syncSizePercent = byte.MaxValue;
                if (pickup.Rigidbody != null)
                {
                    pickup.Rigidbody.isKinematic = true;
                    pickup.Rigidbody.useGravity = false;
                }

                return pickup;
            }
            catch
            {
                Release(pickup);
                throw;
            }
        }

        internal static void Prune()
        {
            foreach (var pair in owned.Where(p => p.Key == null || Time.time >= p.Value).ToArray())
                if (pair.Key == null)
                    owned.Remove(pair.Key);
                else if (Pickup.Get(pair.Key)is Scp244Pickup pickup)
                    Release(pickup);
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
                if (cloud != null && Pickup.Get(cloud)is Scp244Pickup pickup)
                    Release(pickup);
            owned.Clear();
        }

        internal static void Disable()
        {
            Clear();
            harmony?.UnpatchAll(harmony.Id);
            harmony = null;
        }

        private static bool VisualOnly(Scp244DeployablePickup __instance, Vector3 __0, ref float __result)
        {
            if (!NetworkServer.active || (!owned.ContainsKey(__instance) && !(Plugin.Instance?.FilterProtectsAt(__0) ?? false)))
                return true;
            // This is a server-only exposure query. Native client fog rendering is
            // untouched; ordinary unfiltered SCP-244 keeps its normal behavior.
            __result = 0;
            return false;
        }
    }
}
