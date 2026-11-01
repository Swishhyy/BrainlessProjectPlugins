using System;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

internal static class GrenadeNativeContracts
{
    internal static void Run(PEReader game, MetadataReader md, Action<bool, string> check)
    {
        string[] Calls(string type, string method)
        {
            var t = md.TypeDefinitions.Select(md.GetTypeDefinition).Single(t => md.GetString(t.Name) == type);
            var m = t.GetMethods().Select(md.GetMethodDefinition).Single(m => md.GetString(m.Name) == method);
            return VoiceNativeContracts.Calls(game.GetMethodBody(m.RelativeVirtualAddress).GetILBytes(), md).ToArray();
        }

        var fuse = Calls("TimeGrenade", "ServerFuseEnd");
        check(Array.IndexOf(fuse, "OnProjectileExploding") >= 0 && Array.IndexOf(fuse, "OnProjectileExploding") < Array.IndexOf(fuse, "get_IsAllowed"), "utility grenades resolve at the actual cancellable native fuse event");
        var effect = Calls("EffectGrenade", "ServerFuseEnd");
        check(Array.IndexOf(effect, "ServerFuseEnd") < Array.IndexOf(effect, "ServerSpawnEffect") && Array.IndexOf(effect, "ServerSpawnEffect") < Array.IndexOf(effect, "DestroySelf"), "cancelling the early fuse suppresses native effects and requires explicit spent-projectile cleanup");
        var flash = Calls("FlashbangGrenade", "ServerFuseEnd");
        check(Array.IndexOf(flash, "ServerFuseEnd") < Array.IndexOf(flash, "ProcessPlayer"), "flashbang gameplay occurs downstream of the early utility fuse boundary");
        var fog = Calls("Scp244DeployablePickup", "Update");
        check(fog.SequenceEqual(new[] { "UpdateCurrentRoom", "UpdateConditions", "UpdateRange", "UpdateEffects" }), "native SCP-244 initializes room/fog bounds before range and visual updates; its component must stay enabled");
    }
}
