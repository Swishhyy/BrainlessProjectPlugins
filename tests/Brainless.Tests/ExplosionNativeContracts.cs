using System;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

internal static class ExplosionNativeContracts
{
    internal static void Run(string root, PEReader game, MetadataReader md, Action<bool, string> check)
    {
        var type = md.TypeDefinitions.Select(md.GetTypeDefinition).Single(t => md.GetString(t.Namespace) == "PlayerStatsSystem" && md.GetString(t.Name) == "ExplosionDamageHandler");
        var ctor = type.GetMethods().Select(md.GetMethodDefinition).Single(m => md.GetString(m.Name) == ".ctor");
        byte[] body = game.GetMethodBody(ctor.RelativeVirtualAddress).GetILBytes();
        check(body.Length > 10 && body[6] == 0x0e && body[7] == 4 && body[8] == 0x2d && body[9] == 1 && body[10] == 0x2a, "the actual native explosion constructor returns before damage setup when penetration is zero");
        foreach (string component in new[]
        {
            "Roles/Brainless.Roles.dll",
            "Items/Brainless.Items.dll",
            "Weapons/Brainless.Weapons.dll",
            "BetterCoinflips/BetterCoinflips.dll"
        }

        )
        {
            string path = Path.Combine(root, component.Split('/')[0], "bin/Release", Path.GetFileName(component));
            if (!File.Exists(path))
                continue;
            using var stream = File.OpenRead(path);
            using var dll = new PEReader(stream);
            var reader = dll.GetMetadataReader();
            var source = reader.TypeDefinitions.Select(reader.GetTypeDefinition).Single(t => reader.GetString(t.Namespace) == "Brainless.Internal" && reader.GetString(t.Name) == "DamageSource");
            var blast = source.GetMethods().Select(reader.GetMethodDefinition).Single(m => reader.GetString(m.Name) == "Blast");
            var calls = VoiceNativeContracts.Calls(dll.GetMethodBody(blast.RelativeVirtualAddress).GetILBytes(), reader).ToArray();
            check(System.Linq.Enumerable.Contains(calls, "Penetration") && System.Linq.Enumerable.Contains(calls, "get_zero"), component + " supplies nonzero native penetration and zero force instead of world coordinates");
        }
    }
}
