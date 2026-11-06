using System;
using System.IO;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Interfaces;
using Exiled.Loader;

namespace Brainless.Internal
{
    internal static class ConfigMigration
    {
        // Deletes leftover numbered migration backups in the EXILED config folder (once
        // per server process).
        internal static void Tidy()
        {
            const string key = "Brainless.ConfigBackupsTidied";
            if (AppDomain.CurrentDomain.GetData(key) != null)
                return;
            AppDomain.CurrentDomain.SetData(key, true);
            try
            {
                int removed = ConfigDocument.CleanupBackups(Paths.Configs);
                if (removed > 0)
                    Log.Info("[Brainless] Removed " + removed + " old config migration backup file(s).");
            }
            catch (Exception exception)
            {
                Log.Warn("[Brainless] Could not tidy old config backups (" + exception.GetType().Name + ").");
            }
        }

        internal static void Save<T>(Plugin<T> plugin)
            where T : class, IConfig, new()
        {
            try
            {
                bool separated = LoaderPlugin.Config.ConfigType == ConfigType.Separated;
                string path = separated ? plugin.ConfigPath : Paths.Config;
                string before = File.ReadAllText(path);
                string next = ConfigDocument.Merge(before, plugin.Prefix, Loader.Serializer.Serialize(plugin.Config), separated);
                Tidy();
                if (!ConfigDocument.Replace(path, before, next))
                    Log.Warn("[" + plugin.Name + "] Config changed during migration; kept the newer file. Active migrated settings remain in memory.");
                else
                    Log.Info("[" + plugin.Name + "] New settings saved; previous config backed up with .before-brainless-migration.");
            }
            catch (Exception exception)
            {
                // YAML/parser errors can quote credentials. Never log the body.
                Log.Warn("[" + plugin.Name + "] Could not save migrated settings (" + exception.GetType().Name + "). Active settings remain in memory; check config-file write access.");
            }
        }
    }
}
