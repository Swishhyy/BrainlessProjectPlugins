using System;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

internal static class ScpTraitNativeContracts
{
    internal static void Run(PEReader game, MetadataReader md, Action<bool, string> check)
    {
        TypeDefinition Type(string ns, string name) => md.TypeDefinitions.Select(md.GetTypeDefinition).Single(t => md.GetString(t.Namespace) == ns && md.GetString(t.Name) == name);
        string[] Members(TypeDefinition t, string name)
        {
            var m = t.GetMethods().Select(md.GetMethodDefinition).Single(m => md.GetString(m.Name) == name);
            return VoiceNativeContracts.Calls(game.GetMethodBody(m.RelativeVirtualAddress).GetILBytes(), md, true).ToArray();
        }

        var intercom = Members(Type("PlayerRoles.Voice", "Intercom"), "CheckPlayer");
        check(intercom.Count(n => n == "IsHuman") == 1 && intercom.Contains("CheckRange") && intercom.Contains("get_ServerIsSending") && intercom.Contains("IsMuted") && intercom.Contains("OnUsingIntercom") && intercom.Contains("get_IsAllowed"), "Mimic extends one native intercom human predicate while retaining range, transmission, mute and plugin checks");
        var vigor = Members(Type("PlayerRoles.PlayableScps.Scp106", "Scp106VigorAbilityBase"), "set_VigorAmount");
        check(vigor.Contains("OnChangingVigor") && vigor.Contains("Clamp01") && vigor.Contains("OnChangedVigor"), "variant vigor changes still pass native cancellation, clamping and observer events");
        var rage = Members(Type("PlayerRoles.PlayableScps.Scp096", "Scp096RageCycleAbility"), "UpdateServerside");
        check(rage.Contains("_timeToChangeState") && rage.Contains("SetRageState"), "Anger Issue shortens the actual native rage transition timer");
        var quiet = Members(Type("CustomPlayerEffects", "SilentWalk"), "ProcessFootstepOverrides");
        check(quiet.Contains("get_Intensity") && quiet.Contains("Max"), "quiet SCP footsteps use the native intensity-based volume modifier with a nonnegative floor");
        var shield = Type("PlayerRoles.PlayableScps.HumeShield", "DynamicHumeShieldController");
        var fields = shield.GetFields().Select(md.GetFieldDefinition).Select(f => md.GetString(f.Name)).ToArray();
        check(fields.Contains("RegenerationRate") && fields.Contains("RegenerationCooldown"), "Cocoon and Surgeon bind to separate real shield rate and delay controls");
        var breakneck = Type("PlayerRoles.PlayableScps.Scp173", "Scp173BreakneckSpeedsAbility");
        var speedFields = breakneck.GetFields().Select(md.GetFieldDefinition).Select(f => md.GetString(f.Name)).ToArray();
        check(speedFields.Contains("_duration") && speedFields.Contains("Cooldown"), "Escape's refreshes actual breakneck duration and cooldown state");
    }
}
