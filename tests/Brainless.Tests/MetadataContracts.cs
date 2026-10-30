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
    }
}
