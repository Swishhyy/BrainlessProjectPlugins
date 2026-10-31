using System.Collections.Generic;
using System.IO;
using Brainless.Internal;
using Exiled.API.Features;

namespace Brainless.Weapons
{
    public sealed partial class Plugin
    {
        private static void LoadIndividualWeapons(Config config)
        {
            if (!config.IndividualItemFiles)
                return;
            var sections = new Dictionary<uint, ItemFiles.Section>
            {
                [7201] = ItemFiles.Bind(config.Tuning, "Nullifier"),
                [7202] = ItemFiles.Bind(config.Tuning, "Rattler"),
                [7203] = ItemFiles.Bind(config.Tuning, "BreachShotgun"),
                [7204] = ItemFiles.Bind(config.Tuning, "Redline"),
                [7206] = ItemFiles.Bind(config.Tuning, "Ricochet"),
                [7208] = ItemFiles.Bind(config.Tuning, "Railgun")
            };
            var launcher = new LauncherFileRoot();
            foreach (var property in typeof(LauncherFileTuning).GetProperties())
                property.SetValue(launcher.Launcher, typeof(Config).GetProperty(property.Name).GetValue(config));
            sections[7207] = ItemFiles.Bind(launcher, "Launcher");
            ItemFiles.Apply(Path.Combine(Paths.Configs, "ProjectBrainless", Server.Port.ToString(), "Weapons"), config.Weapons, w => w.Id, sections);
            foreach (var property in typeof(LauncherFileTuning).GetProperties())
                typeof(Config).GetProperty(property.Name).SetValue(config, property.GetValue(launcher.Launcher));
        }
    }

    public sealed class LauncherFileRoot
    {
        public LauncherFileTuning Launcher { get; set; } = new LauncherFileTuning();
    }

    public sealed class LauncherFileTuning
    {
        public float LauncherSpeed { get; set; } = 18;
        public float LauncherFuse { get; set; } = 1.8f;
        public float LauncherBlastRadius { get; set; } = 3.5f;
        public float LauncherHumanDamage { get; set; } = 70;
        public float LauncherScpDamage { get; set; } = 150;
        public int LauncherArmorPenetration { get; set; } = 50;
        public bool LauncherRequireLineOfSight { get; set; } = true;
        public bool LauncherAffectOwner { get; set; } = true;
        public bool LauncherDiscardWhenEmpty { get; set; } = true;
    }
}
