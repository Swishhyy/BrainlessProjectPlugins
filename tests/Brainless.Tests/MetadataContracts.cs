using System;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

internal static class MetadataContracts
{
    internal static void Run(string root, Action<bool, string> check)
    {
        RuntimeAccessContracts.Run(root, check);
        using var stream = File.OpenRead(Path.Combine(root, ".build", "references", "Assembly-CSharp-Publicized.dll"));
        using var pe = new PEReader(stream);
        var md = pe.GetMetadataReader();
        VoiceNativeContracts.Run(pe, md, check);
        ExplosionNativeContracts.Run(root, pe, md, check);
        GrenadeNativeContracts.Run(pe, md, check);
        ScpTraitNativeContracts.Run(pe, md, check);
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
        var escape = Method("", "Escape", "ServerHandlePlayer");
        var escapeCalls = VoiceNativeContracts.Calls(pe.GetMethodBody(escape.RelativeVirtualAddress).GetILBytes(), md).ToArray();
        check(Array.IndexOf(escapeCalls, "CanEscape") < Array.IndexOf(escapeCalls, "OnEscaping") && Array.IndexOf(escapeCalls, "get_EscapeScenario") > Array.IndexOf(escapeCalls, "OnEscaping") && Array.IndexOf(escapeCalls, "ServerSetRole") > Array.IndexOf(escapeCalls, "get_NewRole"), "the pinned escape handler checks the actual zone and accepts custom role/scenario before native respawn");
        var escapeZone = Method("", "Escape", "CanEscape");
        var zoneCalls = VoiceNativeContracts.Calls(pe.GetMethodBody(escapeZone.RelativeVirtualAddress).GetILBytes(), md, true).ToArray();
        check(zoneCalls.Contains("EscapeZones") && zoneCalls.Contains("Contains") && !zoneCalls.Contains("get_RoleTypeId"), "military and neutral first-person roles can reach the native escape event inside a real escape zone");
        using var exiled = File.OpenRead(Path.Combine(root, ".build", "references", "Exiled.API.dll"));
        using var exiledPe = new PEReader(exiled);
        var api = exiledPe.GetMetadataReader();
        var dnt = api.TypeDefinitions.Select(api.GetTypeDefinition).Single(t => api.GetString(t.Namespace) == "Exiled.API.Features" && api.GetString(t.Name) == "Player").GetMethods().Select(api.GetMethodDefinition).Single(m => api.GetString(m.Name) == "get_DoNotTrack");
        check(VoiceNativeContracts.Calls(exiledPe.GetMethodBody(dnt.RelativeVirtualAddress).GetILBytes(), api, true).Contains("get_DoNotTrack"), "Core reads the authenticated native DNT property through the pinned EXILED wrapper");
        var flags = api.TypeDefinitions.Select(h => api.GetTypeDefinition(h)).Single(t => api.GetString(t.Namespace) == "Exiled.API.Enums" && api.GetString(t.Name) == "KeycardPermissions");
        int Flag(string name)
        {
            var field = flags.GetFields().Select(h => api.GetFieldDefinition(h)).Single(f => api.GetString(f.Name) == name);
            return api.GetBlobReader(api.GetConstant(field.GetDefaultValue()).Value).ReadInt32();
        }

        int containment = Flag("ContainmentLevelThree"), armory = Flag("ArmoryLevelThree");
        check(containment > 0 && armory > 0 && containment != armory, "Locksmith recognizes both actual level-three permission categories without excluding them");
        var highest = Method("Interactables.Interobjects.DoorUtils", "KeycardLevels", "get_HighestLevelValue");
        var levelMembers = VoiceNativeContracts.Calls(pe.GetMethodBody(highest.RelativeVirtualAddress).GetILBytes(), md, true).ToArray();
        check(levelMembers.Contains("Containment") && levelMembers.Contains("Armory") && levelMembers.Contains("Admin"), "Locksmith timing uses the highest native containment, armory or administration requirement");
        var forceRole = Method("CommandSystem.Commands.RemoteAdmin", "ForceRoleCommand", "Execute");
        check((forceRole.Attributes & System.Reflection.MethodAttributes.Public) != 0, "Roles hooks the native Remote Admin forceclass command by name to roll custom roles for staff force-classes");
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
            // BrainlessCoinflips and ScpSwap reach these by name through RolesBridge.
            var rolesApi = roles.TypeDefinitions.Select(roles.GetTypeDefinition).Single(t => roles.GetString(t.Namespace) == "Brainless.Roles" && roles.GetString(t.Name) == "RolesApi");
            var exposed = rolesApi.GetMethods().Select(roles.GetMethodDefinition)
                .Where(m => (m.Attributes & System.Reflection.MethodAttributes.Public) != 0 && (m.Attributes & System.Reflection.MethodAttributes.Static) != 0)
                .Select(m => roles.GetString(m.Name) + "/" + m.GetParameters().Count).ToArray();
            check(new[] { "get_IsReady/0", "ProfileId/1", "ProfileName/1", "AssignProfile/2", "RollProfile/2", "GrantRandomProfile/2" }.All(name => exposed.Contains(name)), "the compiled Roles API exposes every public static method the cross-plugin bridge calls");
        }

        string itemsPath = Path.Combine(root, "Items/bin/Release/Brainless.Items.dll");
        if (File.Exists(itemsPath))
        {
            using var itemsStream = File.OpenRead(itemsPath);
            using var itemsPe = new PEReader(itemsStream);
            var items = itemsPe.GetMetadataReader();
            var itemPlugin = items.TypeDefinitions.Select(items.GetTypeDefinition).Single(t => items.GetString(t.Namespace) == "Brainless.Items" && items.GetString(t.Name) == "Plugin");
            var methods = itemPlugin.GetMethods().Select(items.GetMethodDefinition).Select(md => items.GetString(md.Name)).ToArray();
            check(methods.Contains("UsingCompleted") && methods.Contains("PillUsing") && methods.Contains("CursedLootRound") && methods.Contains("WarheadPanel") && methods.Contains("CursedLocker") && methods.Contains("CursedGenerator"), "pills use the native use animation and the cursed keycard counts doors, lockers and generators");
            var catalog = items.TypeDefinitions.Select(items.GetTypeDefinition).Single(t => items.GetString(t.Namespace) == "Brainless.Items" && items.GetString(t.Name) == "SupCatalog");
            check(catalog.GetMethods().Select(items.GetMethodDefinition).Any(md => items.GetString(md.Name) == "CursedDefaults"), "the compiled item catalog ships the cursed item definitions");
        }

        string weaponsPath = Path.Combine(root, "Weapons/bin/Release/Brainless.Weapons.dll");
        if (File.Exists(weaponsPath))
        {
            using var weaponsStream = File.OpenRead(weaponsPath);
            using var weaponsPe = new PEReader(weaponsStream);
            var weapons = weaponsPe.GetMetadataReader();
            var weaponPlugin = weapons.TypeDefinitions.Select(weapons.GetTypeDefinition).Single(t => weapons.GetString(t.Namespace) == "Brainless.Weapons" && weapons.GetString(t.Name) == "Plugin");
            var weaponMethods = weaponPlugin.GetMethods().Select(weapons.GetMethodDefinition).Select(md => weapons.GetString(md.Name)).ToArray();
            check(new[] { "StunnedBlinkRequest", "StunnedBlink", "StunnedTantrum", "StunnedBreakneck", "PinStunned", "HasFixedShots" }.All(weaponMethods.Contains), "a stunned SCP-173 cannot blink, tantrum or breakneck, stunned players are pinned, and the Stun Gun has fixed shots");
        }

        // Only SCP voice may answer settings status reports (rate-limited); other plugins
        // resending the whole menu on join/status multiplied traffic and lagged players out.
        foreach (var (dll, ns, forbidden) in new[] { ("Roles/bin/Release/Brainless.Roles.dll", "Brainless.Roles", new[] { "HudStatusReceived", "HudVerified", "SyncHud" }), ("BrainlessZombieOptOut/bin/Release/Brainless.ZombieOptOut.dll", "Brainless.ZombieOptOut", new[] { "StatusReceived", "Verified", "Sync" }) })
        {
            string path = Path.Combine(root, dll);
            if (!File.Exists(path))
                continue;
            using var syncStream = File.OpenRead(path);
            using var syncPe = new PEReader(syncStream);
            var syncReader = syncPe.GetMetadataReader();
            var syncPlugin = syncReader.TypeDefinitions.Select(syncReader.GetTypeDefinition).Single(t => syncReader.GetString(t.Namespace) == ns && syncReader.GetString(t.Name) == "Plugin");
            var syncNames = syncPlugin.GetMethods().Select(syncReader.GetMethodDefinition).Select(md => syncReader.GetString(md.Name)).ToArray();
            check(!forbidden.Any(syncNames.Contains), ns + " does not resend the settings menu on join or status reports");
        }

        string voicePath = Path.Combine(root, "BrainlessScpProxChat/bin/Release/Brainless.ScpProxChat.dll");
        if (File.Exists(voicePath))
        {
            using var voiceStream = File.OpenRead(voicePath);
            using var voicePe = new PEReader(voiceStream);
            var voice = voicePe.GetMetadataReader();
            var state = voice.TypeDefinitions.Select(voice.GetTypeDefinition).Single(t => voice.GetString(t.Namespace) == "Brainless.ScpProxChat" && voice.GetString(t.Name) == "VoiceState");
            var names = state.GetMethods().Select(voice.GetMethodDefinition).Select(m => voice.GetString(m.Name)).ToArray();
            check(System.Linq.Enumerable.Contains(names, "KeyPressed") && !System.Linq.Enumerable.Contains(names, "SetMode") && !System.Linq.Enumerable.Contains(names, "get_HoldMode"), "the compiled voice state has toggle input and no hold-mode transition");
            // Brainless Roles reads the HUD status line through VoiceBridge by name.
            var voiceApi = voice.TypeDefinitions.Select(voice.GetTypeDefinition).Single(t => voice.GetString(t.Namespace) == "Brainless.ScpProxChat" && voice.GetString(t.Name) == "VoiceApi");
            check(voiceApi.GetMethods().Select(voice.GetMethodDefinition).Any(m => voice.GetString(m.Name) == "HudStatus" && m.GetParameters().Count == 1 && (m.Attributes & System.Reflection.MethodAttributes.Static) != 0 && (m.Attributes & System.Reflection.MethodAttributes.Public) != 0), "the compiled voice plugin exposes the HUD status method the Roles HUD calls");
        }
    }
}
