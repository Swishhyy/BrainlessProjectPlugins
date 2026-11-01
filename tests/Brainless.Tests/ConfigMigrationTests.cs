#if NATIVE_CONFIG_TESTS
using System;
using System.IO;
using Brainless.Internal;
using YamlDotNet.Serialization;

internal static class ConfigMigrationTests
{
    internal static void Run(Action<bool, string> check)
    {
        string current = "# Peer comment\npeer:\n  enabled: true\n  text: 'keep: this'\nbrainless_roles:\n  auto_update:\n    token: fixture-read-only\n  roles: []\n\n# Next plugin comment\nother_peer:\n  value: 4\n";
        string section = "auto_update:\n  token: fixture-read-only\nrole_catalog_revision: 1\nroles:\n- id: L01\n  chance_percent: 7.5\n";
        string next = ConfigDocument.Merge(current, "brainless_roles", section, false);
        var yaml = new DeserializerBuilder().Build();
        var before = yaml.Deserialize<System.Collections.Generic.Dictionary<string, object>>(current);
        var after = yaml.Deserialize<System.Collections.Generic.Dictionary<string, object>>(next);
        var writer = new SerializerBuilder().Build();
        check(writer.Serialize(before["peer"]) == writer.Serialize(after["peer"]) && next.Contains("fixture-read-only") && next.Contains("L01"), "config migration preserves peer-plugin settings and the private updater credential while adding editable role fields");
        check(next.StartsWith(current.Substring(0, current.IndexOf("brainless_roles:", StringComparison.Ordinal)), StringComparison.Ordinal) && next.EndsWith("\n# Next plugin comment\nother_peer:\n  value: 4\n", StringComparison.Ordinal), "config migration preserves other plugins' comments, quotes, ordering and whitespace verbatim");
        check(ConfigDocument.Merge("unrelated: old", "brainless_roles", section, true) == section, "separated configs save only the selected plugin without a root prefix");
        bool rejected = false;
        try { ConfigDocument.Merge("peer: {}", "brainless_roles", section, false); }
        catch (InvalidOperationException) { rejected = true; }
        check(rejected, "a missing plugin section cannot be replaced by an accidental new default configuration");
        string directory = Path.Combine(Path.GetTempPath(), "brainless-config-migration-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string path = Path.Combine(directory, "config.yml");
            File.WriteAllText(path, current);
            check(ConfigDocument.Replace(path, current, next) && File.ReadAllText(path) == next && File.ReadAllText(path + ".before-brainless-migration") == current, "config migration atomically saves the new fields with an exact previous-file backup");
            check(ConfigDocument.Replace(path, next, section) && File.ReadAllText(path + ".before-brainless-migration") == current && Array.FindAll(Directory.GetFiles(directory), p => p.StartsWith(path + ".before-brainless-migration.", StringComparison.Ordinal)).Length == 1, "later plugin migrations cannot overwrite the original configuration backup");
            File.WriteAllText(path, "owner edited this");
            check(!ConfigDocument.Replace(path, current, next) && File.ReadAllText(path) == "owner edited this" && Directory.GetFiles(directory, "*.migration-*").Length == 0, "a concurrent owner edit wins over migration and the unused staging file is removed");
        }
        finally { Directory.Delete(directory, true); }
    }
}
#endif

