using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Exiled.API.Features;
using HarmonyLib;
using Mirror;
using PlayerRoles.PlayableScps.Scp096;
using PlayerRoles.PlayableScps.Scp106;
using PlayerRoles.PlayableScps.Scp939;
using VoiceChat;
using NativeIntercom = PlayerRoles.Voice.Intercom;

namespace Brainless.Roles
{
    [HarmonyPatch(typeof(Scp106VigorAbilityBase), "VigorAmount", MethodType.Setter)]
    internal static class VariantVigor
    {
        private static void Prefix(Scp106VigorAbilityBase __instance, ref float __0)
        {
            if (!NetworkServer.active)
                return;
            float multiplier = Plugin.Instance?.Traits(Player.Get(__instance.Owner))?.VigorGainMultiplier ?? 1;
            if (multiplier != 1)
                __0 = ScpTraitRules.Vigor(((Exiled.API.Features.Roles.Scp106Role)Player.Get(__instance.Owner).Role).Vigor, __0, multiplier);
        }
    }

    [HarmonyPatch(typeof(Scp096RageCycleAbility), "UpdateServerside")]
    internal static class VariantRageWindup
    {
        private static void Prefix(Scp096RageCycleAbility __instance)
        {
            if (!NetworkServer.active || __instance.CastRole.StateController.RageState != Scp096RageState.Distressed)
                return;
            float? seconds = Plugin.Instance?.Traits(Player.Get(__instance.Owner))?.RageWindupSeconds;
            if (seconds.HasValue)
                ScpPrivateAccess.ShortenRageWindup(__instance, seconds.Value);
        }
    }

    [HarmonyPatch(typeof(NativeIntercom), "CheckPlayer")]
    internal static class MimicIntercomAccess
    {
        // Extend only the human eligibility predicate. Keep native distance,
        // transmission state, intercom mute, cooldown and using-event checks.
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            int matches = 0;
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Call && instruction.operand is MethodInfo method && method.Name == "IsHuman" && method.ReturnType == typeof(bool) && method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType == typeof(ReferenceHub))
                {
                    instruction.operand = AccessTools.Method(typeof(MimicIntercomAccess), nameof(HumanOrMimic));
                    matches++;
                }

                yield return instruction;
            }

            if (matches != 1)
                throw new InvalidOperationException("The native intercom eligibility method changed; refusing to bypass its remaining checks.");
        }

        private static bool HumanOrMimic(ReferenceHub hub)
        {
            var player = Player.Get(hub);
            return player != null && (player.IsHuman || (player.Role.Type == PlayerRoles.RoleTypeId.Scp939 && Plugin.Instance?.Traits(player)?.UseIntercom == true));
        }
    }

    [HarmonyPatch(typeof(Scp939VoiceModule), nameof(Scp939VoiceModule.ValidateSend))]
    internal static class MimicIntercomVoice
    {
        private static void Postfix(Scp939VoiceModule __instance, ref VoiceChatChannel __result)
        {
            // Never upgrade an invalid/muted stream or replace recorded mimicry.
            // The native intercom first has to grant this SCP the microphone.
            if (__result != VoiceChatChannel.ScpChat)
                return;
            var hub = ScpPrivateAccess.VoiceOwner(__instance);
            if (Plugin.Instance?.Traits(Player.Get(hub))?.UseIntercom == true && NativeIntercom.CheckPerms(hub))
                __result = VoiceChatChannel.Intercom;
        }
    }
}
