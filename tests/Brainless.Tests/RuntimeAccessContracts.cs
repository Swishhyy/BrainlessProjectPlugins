using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

internal static class RuntimeAccessContracts
{
    internal static void Run(string root, Action<bool, string> check)
    {
        using var stream = File.OpenRead(Path.Combine(root, ".build/references/Assembly-CSharp.dll"));
        using var game = new PEReader(stream);
        var md = game.GetMetadataReader();
        TypeDefinition Type(string name) => md.TypeDefinitions.Select(md.GetTypeDefinition).Single(t => md.GetString(t.Name) == name);
        FieldDefinition Field(string type, string name) => Type(type).GetFields().Select(md.GetFieldDefinition).Single(f => md.GetString(f.Name) == name);
        MethodDefinition Method(string type, string name) => Type(type).GetMethods().Select(md.GetMethodDefinition).Single(m => md.GetString(m.Name) == name);
        var setter = Method("Scp244DeployablePickup", "set_CurrentSizePercent");
        check((setter.Attributes & MethodAttributes.MemberAccessMask) != MethodAttributes.Public, "the real smoke size setter is nonpublic, unlike the publicized build reference");
        var duration = Field("Scp173BreakneckSpeedsAbility", "_duration");
        var signature = md.GetBlobReader(duration.Signature);
        signature.ReadSignatureHeader();
        signature.ReadSignatureTypeCode();
        var stopwatch = md.GetTypeReference((TypeReferenceHandle)signature.ReadTypeHandle());
        check((duration.Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Private && md.GetString(stopwatch.Namespace) == "System.Diagnostics" && md.GetString(stopwatch.Name) == "Stopwatch", "breakneck refresh resolves the actual private System.Diagnostics.Stopwatch through reflection");
        var rage = Field("Scp096RageCycleAbility", "_timeToChangeState");
        var rageSignature = md.GetBlobReader(rage.Signature);
        rageSignature.ReadSignatureHeader();
        check(rageSignature.ReadSignatureTypeCode() == SignatureTypeCode.Single && (rage.Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Private, "rage windup reflection writes the actual private single-precision timer");
        foreach (var pair in new[]
        {
            ("Scp914Upgrader", "ProcessPickup", 3),
            ("Scp914Upgrader", "TryGetProcessor", 2),
            ("Scp914ItemProcessor", "ProcessResultingItem", 3)
        }

        )
        {
            var method = Method(pair.Item1, pair.Item2);
            check((method.Attributes & MethodAttributes.Static) != 0 && method.GetParameters().Count == pair.Item3, "SCP-914 reflection binds the pinned static " + pair.Item2 + " signature");
        }

        string itemsPath = Path.Combine(root, "Items/bin/Release/Brainless.Items.dll");
        if (File.Exists(itemsPath))
        {
            using var itemStream = File.OpenRead(itemsPath);
            using var items = new PEReader(itemStream);
            var itemMd = items.GetMetadataReader();
            var setters = itemMd.MemberReferences.Select(itemMd.GetMemberReference).Where(m => itemMd.GetString(m.Name) == "set_CurrentSizePercent").ToArray();
            check(setters.Length == 1 && setters[0].Parent.Kind == HandleKind.TypeReference && itemMd.GetString(itemMd.GetTypeReference((TypeReferenceHandle)setters[0].Parent).Name) == "Scp244Pickup", "compiled smoke creation and maintenance call the public EXILED wrapper instead of the private native setter");
        }

        string rolesPath = Path.Combine(root, "Roles/bin/Release/Brainless.Roles.dll");
        if (File.Exists(rolesPath))
        {
            using var roleStream = File.OpenRead(rolesPath);
            using var roles = new PEReader(roleStream);
            var roleMd = roles.GetMetadataReader();
            check(!roleMd.MemberReferences.Select(roleMd.GetMemberReference).Any(m => roleMd.GetString(m.Name) == "_duration" || roleMd.GetString(m.Name) == "_timeToChangeState"), "compiled role traits contain no direct private timer-field access");
            var plugin = roleMd.TypeDefinitions.Select(roleMd.GetTypeDefinition).Single(t => roleMd.GetString(t.Name) == "Plugin" && roleMd.GetString(t.Namespace) == "Brainless.Roles");
            var validate = plugin.GetMethods().Select(roleMd.GetMethodDefinition).Single(m => roleMd.GetString(m.Name) == "ValidCustomInfo");
            check(VoiceNativeContracts.Calls(roles.GetMethodBody(validate.RelativeVirtualAddress).GetILBytes(), roleMd).Contains("ValidateCustomInfo"), "role overhead labels pass native validation before reaching the EXILED CustomInfo setter");
        }
    }
}
