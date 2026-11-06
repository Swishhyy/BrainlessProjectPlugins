using System;
using System.IO;
using System.Linq;

internal static class ConfigBackupTests
{
    internal static void Run(Action<bool, string> check)
    {
#if NATIVE_CONFIG_TESTS
        string root = Path.Combine(Path.GetTempPath(), "brainless-backups-" + Guid.NewGuid().ToString("N"));
        string plugins = Path.Combine(root, "Plugins", "brainless_roles");
        Directory.CreateDirectory(plugins);
        try
        {
            string config = Path.Combine(root, "7777-config.yml");
            File.WriteAllText(config, "a: 1\n");
            for (int i = 0; i < 5; i++)
                File.WriteAllText(config + ".before-brainless-migration." + Guid.NewGuid().ToString("N"), "old");
            File.WriteAllText(Path.Combine(plugins, "7777.yml.before-brainless-migration." + Guid.NewGuid().ToString("N")), "old");
            string oldStaging = config + ".migration-" + Guid.NewGuid().ToString("N");
            File.WriteAllText(oldStaging, "x");
            File.SetLastWriteTimeUtc(oldStaging, DateTime.UtcNow.AddHours(-1));
            string newStaging = config + ".migration-" + Guid.NewGuid().ToString("N");
            File.WriteAllText(newStaging, "x");
            File.WriteAllText(config + ".before-brainless-migration", "keep");
            int removed = Brainless.Internal.ConfigDocument.CleanupBackups(root);
            check(removed == 7 && File.Exists(config + ".before-brainless-migration") && File.Exists(newStaging) && !Directory.GetFiles(root, "*", SearchOption.AllDirectories).Any(f => Path.GetFileName(f).Contains(".before-brainless-migration.")), "startup deletes every numbered migration backup and stale staging file, keeping the single backup");
            File.Delete(newStaging);

            File.SetLastWriteTimeUtc(config + ".before-brainless-migration", DateTime.UtcNow.AddDays(-1));
            check(Brainless.Internal.ConfigDocument.Replace(config, "a: 1\n", "a: 2\n") && File.ReadAllText(config + ".before-brainless-migration") == "a: 1\n", "a migration on a new boot replaces the old backup with the current config");
            check(Brainless.Internal.ConfigDocument.Replace(config, "a: 2\n", "a: 3\n") && File.ReadAllText(config + ".before-brainless-migration") == "a: 1\n" && File.ReadAllText(config) == "a: 3\n", "a second migration on the same boot keeps the oldest backup");
            check(Directory.GetFiles(root, "*", SearchOption.AllDirectories).Count(f => f.Contains(".before-brainless-migration")) == 1, "migrations never leave more than one backup per config file");
        }
        finally
        {
            Directory.Delete(root, true);
        }

        var coins = new Brainless.Coinflips.Configs.Config();
        check(coins.Scp914DoublesCoins && coins.Scp914CoinMaxPerPlayer == 20 && coins.Scp914CoinMaxPerRound == 80 && coins.Scp914CoinWorldCap == 150, "SCP-914 coin doubling is capped per player (20), per round (80) and by the coins in the world (150)");
#endif
    }
}
