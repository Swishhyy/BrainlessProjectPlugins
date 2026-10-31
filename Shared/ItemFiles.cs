using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Brainless.Internal
{
    // Optional per-item overrides. Never regenerate an existing file. Call on a
    // detached config before validation/application, including during reload.
    internal static class ItemFiles
    {
        internal sealed class Section
        {
            internal object Owner;
            internal PropertyInfo Property;
            internal object Value => Property.GetValue(Owner);
        }

        internal static Section Bind(object owner, string property) => new Section
        {
            Owner = owner,
            Property = owner.GetType().GetProperty(property) ?? throw new InvalidOperationException("Missing item tuning section.")
        };
        internal static void Apply<T>(string directory, List<T> catalog, Func<T, uint> id, Dictionary<uint, Section> sections)
            where T : class
        {
            Directory.CreateDirectory(directory);
            var serializer = new SerializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).Build();
            var deserializer = new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).IgnoreUnmatchedProperties().Build();
            for (int i = 0; i < catalog.Count; i++)
            {
                uint key = id(catalog[i]);
                sections.TryGetValue(key, out var section);
                Type tuningType = section?.Property.PropertyType ?? typeof(EmptyItemTuning);
                Type modelType = typeof(ItemFile<, >).MakeGenericType(typeof(T), tuningType);
                var specProperty = modelType.GetProperty("Item");
                var tuningProperty = modelType.GetProperty("Tuning");
                string path = Path.Combine(directory, key + ".yml");
                if (!File.Exists(path))
                {
                    object initial = Activator.CreateInstance(modelType);
                    specProperty.SetValue(initial, catalog[i]);
                    tuningProperty.SetValue(initial, section?.Value ?? new EmptyItemTuning());
                    string yaml = "# Per-item override. Existing files are preserved. Disable individual_item_files to use the main YAML.\n" + serializer.Serialize(initial);
                    try
                    {
                        using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                        {
                            byte[] bytes = Encoding.UTF8.GetBytes(yaml);
                            stream.Write(bytes, 0, bytes.Length);
                            stream.Flush(true);
                        }
                    }
                    catch (IOException)when (File.Exists(path))
                    {
                    }
                }

                if (new FileInfo(path).Length > 1024 * 1024)
                    throw new InvalidOperationException("Item file " + key + " exceeds 1 MB.");
                object loaded;
                try
                {
                    loaded = deserializer.Deserialize(File.ReadAllText(path), modelType);
                }
                catch (YamlDotNet.Core.YamlException)
                {
                    throw new InvalidOperationException("Invalid YAML in item file " + key + ".");
                }

                T entry = loaded == null ? null : specProperty.GetValue(loaded) as T;
                object tuning = loaded == null ? null : tuningProperty.GetValue(loaded);
                if (entry == null || id(entry) != key || tuning == null)
                    throw new InvalidOperationException("Item file " + key + " must retain its ID and contain item/tuning blocks.");
                catalog[i] = entry;
                section?.Property.SetValue(section.Owner, tuning);
            }
        }
    }

    public sealed class ItemFile<TItem, TTuning>
    {
        public TItem Item { get; set; }
        public TTuning Tuning { get; set; }
    }

    public sealed class EmptyItemTuning
    {
    }
}
