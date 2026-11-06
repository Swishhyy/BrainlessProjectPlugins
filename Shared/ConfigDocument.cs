using System;
using System.IO;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace Brainless.Internal
{
    internal static class ConfigDocument
    {
        internal static string Merge(string current, string prefix, string section, bool separated)
        {
            var replacement = new YamlStream();
            replacement.Load(new StringReader(section));
            if (replacement.Documents.Count != 1 || !(replacement.Documents[0].RootNode is YamlMappingNode))
                throw new InvalidOperationException("The plugin configuration must contain one mapping.");
            if (separated)
                return section;
            var document = new YamlStream();
            document.Load(new StringReader(current));
            if (document.Documents.Count != 1 || !(document.Documents[0].RootNode is YamlMappingNode root) || !root.Children.ContainsKey(new YamlScalarNode(prefix)))
                throw new InvalidOperationException("The existing plugin section was not found; no configuration was overwritten.");
            var entries = root.Children.ToArray();
            int selected = Array.FindIndex(entries, e => e.Key.Equals(new YamlScalarNode(prefix)));
            var key = entries[selected].Key;
            if (LineStart(current, key.Start.Index) != key.Start.Index || (selected + 1 < entries.Length && LineStart(current, entries[selected + 1].Key.Start.Index) != entries[selected + 1].Key.Start.Index))
                throw new InvalidOperationException("Config migration requires normal top-level block sections.");
            int start = LineStart(current, key.Start.Index);
            int end = selected + 1 < entries.Length ? LineStart(current, entries[selected + 1].Key.Start.Index) : current.Length;
            // Leave the next plugin's top-level comments and spacing byte-for-byte
            // intact. Never reserialize peer configs just to add our own fields.
            int boundary = end;
            while (boundary > start)
            {
                int previous = LineStart(current, Math.Max(0, boundary - 1));
                string line = current.Substring(previous, boundary - previous).TrimEnd('\r', '\n');
                if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith("#", StringComparison.Ordinal))
                    break;
                boundary = previous;
            }

            end = boundary;
            string newline = current.Contains("\r\n") ? "\r\n" : "\n";
            string body = string.Join(newline, section.TrimEnd('\r', '\n').Replace("\r\n", "\n").Split('\n').Select(line => "  " + line));
            return current.Substring(0, start) + prefix + ":" + newline + body + newline + current.Substring(end);
        }

        private static int LineStart(string text, int offset)
        {
            if (offset == 0)
                return 0;
            return text.LastIndexOf('\n', Math.Min(offset - 1, text.Length - 1)) + 1;
        }

        internal const string BackupSuffix = ".before-brainless-migration";

        // Removes the numbered backup copies older versions left behind (one per migration)
        // and staging files from interrupted saves. The single current backup stays.
        internal static int CleanupBackups(string directory)
        {
            int removed = 0;
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                return 0;
            // Match by name: a ".*" search pattern also matches the bare backup we keep.
            foreach (string file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
            {
                string name = Path.GetFileName(file);
                bool numbered = name.IndexOf(BackupSuffix + ".", StringComparison.Ordinal) >= 0;
                bool staging = !numbered && name.IndexOf(".migration-", StringComparison.Ordinal) >= 0 && name.IndexOf(BackupSuffix, StringComparison.Ordinal) < 0;
                if (!numbered && !staging)
                    continue;
                try
                {
                    if (staging && DateTime.UtcNow - File.GetLastWriteTimeUtc(file) < TimeSpan.FromMinutes(10))
                        continue;
                    File.Delete(file);
                    removed++;
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }

            return removed;
        }

        internal static bool Replace(string path, string expected, string next)
        {
            string staging = path + ".migration-" + Guid.NewGuid().ToString("N");
            try
            {
                File.WriteAllText(staging, next);
                if (File.ReadAllText(path) != expected)
                    return false;
                // One backup per config file. Several plugins can migrate the same shared
                // config on one boot: the first backup of a boot (the oldest state) is kept,
                // and a backup from an earlier boot is replaced instead of piling up copies.
                string backup = path + BackupSuffix;
                bool fresh = File.Exists(backup) && DateTime.UtcNow - File.GetLastWriteTimeUtc(backup) < TimeSpan.FromMinutes(10);
                if (File.Exists(backup) && !fresh)
                    File.Delete(backup);
                File.Replace(staging, path, fresh ? null : backup);
                if (!fresh && File.Exists(backup))
                    File.SetLastWriteTimeUtc(backup, DateTime.UtcNow);
                return true;
            }
            finally
            {
                if (File.Exists(staging))
                    File.Delete(staging);
            }
        }
    }
}
