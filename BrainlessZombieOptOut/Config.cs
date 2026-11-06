using System.ComponentModel;
using Brainless.Internal.Updates;
using Exiled.API.Interfaces;

namespace Brainless.ZombieOptOut
{
    public sealed class Config : IConfig
    {
        public bool IsEnabled { get; set; } = true;
        public bool Debug { get; set; }
        public AutoUpdateConfig AutoUpdate { get; set; } = new AutoUpdateConfig();

        [Description("Server Specific Settings IDs base..base+2 for the opt-out and auto-fill switches. Change if another plugin uses them.")]
        public int SettingsIdBase { get; set; } = 58200;

        [Description("Seconds spectators have to type .optin after an opted-out player is revived. If nobody fills, SCP-049 gets health_compensation.")]
        public float ZombieFillDuration { get; set; } = 10;

        [Description("Health given to each SCP-049 when an opted-out zombie is not replaced.")]
        public float HealthCompensation { get; set; } = 200;

        [Description("Let players replace an SCP who disconnects or jumps into a pit early in the round (.fill).")]
        public bool AfkReplacement { get; set; } = true;

        [Description("Seconds after round start during which a lost SCP can be replaced.")]
        public float AfkReplacementValidTime { get; set; } = 200;

        [Description("Seconds players have to type .fill before a replacement is picked.")]
        public float ScpFillDuration { get; set; } = 15;

        [Description("Players who did not spawn with a Brainless custom role are this many times as likely to be picked to fill an SCP.")]
        public int FillChanceMultiplierForNoCustomRole { get; set; } = 4;

        [Description("Core XP for filling an opted-out zombie / a lost SCP (needs the Core DLL). 0 disables.")]
        public int ZombieFillXp { get; set; } = 100;
        public int ScpFillXp { get; set; } = 150;
    }
}
