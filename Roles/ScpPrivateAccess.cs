using System;
using System.Diagnostics;
using System.Reflection;
using HarmonyLib;
using PlayerRoles.PlayableScps.Scp096;
using PlayerRoles.PlayableScps.Scp173;
using PlayerRoles.PlayableScps.Scp049;

namespace Brainless.Roles
{
    // These native members are private on the real server. A publicized build
    // reference never grants the plugin access to them at runtime.
    internal static class ScpPrivateAccess
    {
        internal static readonly float AttackCooldown = Convert.ToSingle((AccessTools.Field(typeof(Scp049AttackAbility), "CooldownTime") ?? throw new MissingFieldException("SCP-049 attack cooldown")).GetValue(null));
        private static readonly FieldInfo duration = AccessTools.Field(typeof(Scp173BreakneckSpeedsAbility), "_duration") ?? throw new MissingFieldException("SCP-173 breakneck duration");
        private static readonly FieldInfo rageTime = AccessTools.Field(typeof(Scp096RageCycleAbility), "_timeToChangeState") ?? throw new MissingFieldException("SCP-096 rage transition timer");
        private static readonly MethodInfo voiceOwner = AccessTools.PropertyGetter(typeof(PlayerRoles.Voice.VoiceModuleBase), "Owner") ?? throw new MissingMethodException("Voice module owner getter");
        internal static void RestartBreakneck(Scp173BreakneckSpeedsAbility ability) => ((Stopwatch)duration.GetValue(ability)).Restart();
        internal static void ShortenRageWindup(Scp096RageCycleAbility ability, float seconds) => rageTime.SetValue(ability, Math.Min((float)rageTime.GetValue(ability), seconds));
        internal static ReferenceHub VoiceOwner(PlayerRoles.Voice.VoiceModuleBase module) => (ReferenceHub)voiceOwner.Invoke(module, null);
    }
}
