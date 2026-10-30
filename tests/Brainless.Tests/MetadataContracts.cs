using System;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

internal static class MetadataContracts
{
    internal static void Run(string root, Action<bool, string> check)
    {
        using var stream = File.OpenRead(Path.Combine(root, ".build", "references", "Assembly-CSharp-Publicized.dll"));
        using var pe = new PEReader(stream);
        var md = pe.GetMetadataReader();
        VoiceNativeContracts.Run(pe, md, check);
        ExplosionNativeContracts.Run(root, pe, md, check);
        MethodDefinition Method(string ns, string name, string method)
        {
            var type = md.TypeDefinitions.Select(h => md.GetTypeDefinition(h)).Single(t => md.GetString(t.Namespace) == ns && md.GetString(t.Name) == name);
            return type.GetMethods().Select(h => md.GetMethodDefinition(h)).Single(m => md.GetString(m.Name) == method);
        }

        var fog = Method("InventorySystem.Items.Usables.Scp244", "Scp244DeployablePickup", "FogPercentForPoint");
        check(fog.GetParameters().Count == 1 && fog.RelativeVirtualAddress != 0, "native smoke can measure ordinary SCP-244 exposure without suppressing fog signalling");
        var subEffect = Method("InventorySystem.Items.Usables.Scp244.Hypothermia", "Hypothermia", "UpdateSubEffect");
        check(subEffect.GetParameters().Count == 2 && subEffect.RelativeVirtualAddress != 0, "the cold guard targets the actual per-player subeffect/exposure boundary");
        var fuse = Method("InventorySystem.Items.ThrowableProjectiles", "TimeGrenade", "ServerFuseEnd");
        check(fuse.GetParameters().Count == 0 && fuse.RelativeVirtualAddress != 0, "impact detonation reaches the native validated zero-argument fuse method");
        var range = Method("InventorySystem.Items.Usables.Scp244", "Scp244DeployablePickup", "UpdateRange");
        check(range.GetParameters().Count == 0 && range.RelativeVirtualAddress != 0, "native smoke fixed-radius patch targets the actual SCP-244 update");
        var sync = Method("InventorySystem.Items.Usables.Scp244", "Scp244DeployablePickup", "set_Network_syncSizePercent");
        check(sync.GetParameters().Count == 1, "native smoke uses the actual network size setter");
        var revive = Method("PlayerRoles.PlayableScps.Scp049", "Scp049ResurrectAbility", "get_Duration");
        check(revive.GetParameters().Count == 0 && revive.RelativeVirtualAddress != 0, "Surgeon resurrection patch has an actual native duration getter");
        var attack = Method("PlayerRoles.PlayableScps.Scp049", "Scp049AttackAbility", "ServerProcessCmd");
        check(attack.GetParameters().Count == 1 && attack.RelativeVirtualAddress != 0, "Tickler cooldown patch matches the actual native attack method");
        var resync = Method("InventorySystem.Items.Firearms.Modules", "AutomaticActionModule", "ServerResync");
        check(resync.GetParameters().Count == 0, "launcher rejected shots can resynchronize the native LMG ammo");
        var interaction = Method("Interactables.Interobjects.DoorUtils", "DoorVariant", "ServerInteract");
        check(interaction.GetParameters().Count == 2 && interaction.RelativeVirtualAddress != 0, "Locksmith completion targets the native door interaction with collider identity");
        using var exiled = File.OpenRead(Path.Combine(root, ".build", "references", "Exiled.API.dll"));
        using var exiledPe = new PEReader(exiled);
        var api = exiledPe.GetMetadataReader();
        var flags = api.TypeDefinitions.Select(h => api.GetTypeDefinition(h)).Single(t => api.GetString(t.Namespace) == "Exiled.API.Enums" && api.GetString(t.Name) == "KeycardPermissions");
        int Flag(string name)
        {
            var field = flags.GetFields().Select(h => api.GetFieldDefinition(h)).Single(f => api.GetString(f.Name) == name);
            return api.GetBlobReader(api.GetConstant(field.GetDefaultValue()).Value).ReadInt32();
        }

        int containment = Flag("ContainmentLevelThree"), armory = Flag("ArmoryLevelThree");
        check(Brainless.Roles.LockpickRules.IsLevelThree(containment, containment, armory) && Brainless.Roles.LockpickRules.IsLevelThree(armory, containment, armory), "Locksmith excludes both level-three categories from the actual EXILED enum");
        check(!Brainless.Roles.LockpickRules.IsLevelThree(Flag("Checkpoints") | Flag("ExitGates") | Flag("ContainmentLevelTwo") | Flag("ArmoryLevelTwo"), containment, armory), "native checkpoint, gate and level-two permissions remain eligible for Locksmith");
        string rolePath = Path.Combine(root, "Roles/bin/Release/Brainless.Roles.dll");
        if (File.Exists(rolePath))
        {
            using var roleStream = File.OpenRead(rolePath);
            using var rolePe = new PEReader(roleStream);
            var roles = rolePe.GetMetadataReader();
            var plugin = roles.TypeDefinitions.Select(roles.GetTypeDefinition).Single(t => roles.GetString(t.Namespace) == "Brainless.Roles" && roles.GetString(t.Name) == "Plugin");
            var candy = plugin.GetMethods().Select(roles.GetMethodDefinition).Single(m => roles.GetString(m.Name) == "Candy");
            var calls = VoiceNativeContracts.Calls(rolePe.GetMethodBody(candy.RelativeVirtualAddress).GetILBytes(), roles).ToArray();
            check(System.Linq.Enumerable.Contains(calls, "Resolve") && Array.IndexOf(calls, "Clear") < Array.IndexOf(calls, "AddCandy") && Array.IndexOf(calls, "Clear") >= 0, "the compiled role candy path selects registered candies and clears native seed candy before filling the bag");
        }

        string voicePath = Path.Combine(root, "ScpProximityChat/bin/Release/Brainless.ScpProximityChat.dll");
        if (File.Exists(voicePath))
        {
            using var voiceStream = File.OpenRead(voicePath);
            using var voicePe = new PEReader(voiceStream);
            var voice = voicePe.GetMetadataReader();
            var state = voice.TypeDefinitions.Select(voice.GetTypeDefinition).Single(t => voice.GetString(t.Namespace) == "Brainless.ScpProximityChat" && voice.GetString(t.Name) == "VoiceState");
            var names = state.GetMethods().Select(voice.GetMethodDefinition).Select(m => voice.GetString(m.Name)).ToArray();
            check(System.Linq.Enumerable.Contains(names, "KeyPressed") && !System.Linq.Enumerable.Contains(names, "SetMode") && !System.Linq.Enumerable.Contains(names, "get_HoldMode"), "the compiled voice state has toggle input and no hold-mode transition");
        }
    }
}
