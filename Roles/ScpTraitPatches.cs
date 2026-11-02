using System;
using Exiled.API.Features;
using HarmonyLib;
using Mirror;
using PlayerRoles.PlayableScps.Scp096;
using PlayerRoles.PlayableScps.Scp106;

namespace Brainless.Roles
{
    [HarmonyPatch(typeof(Scp106VigorAbilityBase), "VigorAmount", MethodType.Setter)]
    internal static class VariantVigor
    {
        private static void Prefix(Scp106VigorAbilityBase __instance, ref float __0)
        {
            // A failing prefix would throw out of the native setter and freeze vigor,
            // so any error leaves the native value untouched.
            try
            {
                if (!NetworkServer.active)
                    return;
                float multiplier = Plugin.Instance?.Traits(Player.Get(__instance.Owner))?.VigorGainMultiplier ?? 1;
                if (multiplier != 1)
                    __0 = ScpTraitRules.Vigor(((Exiled.API.Features.Roles.Scp106Role)Player.Get(__instance.Owner).Role).Vigor, __0, multiplier);
            }
            catch (Exception exception)
            {
                Log.Debug("[Brainless Roles] Vigor multiplier skipped: " + exception.Message);
            }
        }
    }

    [HarmonyPatch(typeof(Scp096RageCycleAbility), "UpdateServerside")]
    internal static class VariantRageWindup
    {
        private static void Prefix(Scp096RageCycleAbility __instance)
        {
            // Runs every frame for every SCP-096. Never let a failure escape into the
            // native rage cycle: that is what froze plain 096s mid-enrage in 0.5.0.
            try
            {
                if (!NetworkServer.active || __instance.CastRole.StateController.RageState != Scp096RageState.Distressed)
                    return;
                float? seconds = Plugin.Instance?.Traits(Player.Get(__instance.Owner))?.RageWindupSeconds;
                if (seconds.HasValue)
                    ScpPrivateAccess.ShortenRageWindup(__instance, seconds.Value);
            }
            catch (Exception exception)
            {
                Log.Debug("[Brainless Roles] Rage windup skipped: " + exception.Message);
            }
        }
    }

    // Mimic intercom access was removed: an SCP-939 transmitting on the intercom
    // disconnected listeners whose clients cannot play intercom audio from an SCP.
}
