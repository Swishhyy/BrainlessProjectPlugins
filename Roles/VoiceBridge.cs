using System;
using System.Reflection;
using Exiled.API.Features;
using UnityEngine;

namespace Brainless.Roles
{
    // Reads the SCP chat mode from Brainless SCP Proximity Chat (VoiceApi) through
    // reflection, so Roles keeps no hard dependency on that DLL.
    internal static class VoiceBridge
    {
        private static MethodInfo status;
        private static float nextLookup;

        internal static string Status(Player player)
        {
            if (status == null && Time.time >= nextLookup)
            {
                nextLookup = Time.time + 10;
                var assembly = Array.Find(AppDomain.CurrentDomain.GetAssemblies(), a => a.GetName().Name == "Brainless.ScpProximityChat");
                status = assembly?.GetType("Brainless.ScpProximityChat.VoiceApi")?.GetMethod("HudStatus", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(Player) }, null);
            }

            try
            {
                return status?.Invoke(null, new object[] { player }) as string;
            }
            catch (Exception)
            {
                status = null;
                return null;
            }
        }
    }
}
