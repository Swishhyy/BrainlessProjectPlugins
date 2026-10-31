using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Interfaces;
using Exiled.Loader;

namespace Brainless.Internal
{
    internal static class ConfigReload
    {
        internal static T Read<T>(Plugin<T> plugin)
            where T : class, IConfig, new()
        {
            try
            {
                return ReadFile(plugin);
            }
            catch (YamlDotNet.Core.YamlException)
            {
                // YAML exceptions may quote credential-bearing config lines.
                throw new InvalidOperationException("Invalid YAML. Check indentation and field types; the active config was not changed.");
            }
        }

        private static T ReadFile<T>(Plugin<T> plugin)
            where T : class, IConfig, new()
        {
            string raw = File.ReadAllText(LoaderPlugin.Config.ConfigType == ConfigType.Separated ? plugin.ConfigPath : Paths.Config);
            if (LoaderPlugin.Config.ConfigType != ConfigType.Separated)
            {
                var sections = Loader.Deserializer.Deserialize<Dictionary<string, object>>(raw);
                if (sections == null || !sections.TryGetValue(plugin.Prefix, out var section))
                    throw new InvalidOperationException("Missing configuration section " + plugin.Prefix + ".");
                raw = Loader.Serializer.Serialize(section);
            }

            return Loader.Deserializer.Deserialize<T>(raw) ?? throw new InvalidOperationException("The configuration is empty.");
        }

        internal static void Apply<T>(T current, T next)
        {
            foreach (var property in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
                if (property.CanRead && property.CanWrite)
                    property.SetValue(current, property.GetValue(next));
        }
    }
}
