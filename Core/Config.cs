using System.ComponentModel;
using Brainless.Internal.Updates;
using Exiled.API.Interfaces;

namespace Brainless.Core
{
    public sealed class Config : IConfig
    {
        public bool IsEnabled { get; set; } = true;
        public bool Debug { get; set; }
        [Description("Updates every installed Brainless plugin. Paste one read-only GitHub token here; the other plugins need none.")]
        public AutoUpdateConfig AutoUpdate { get; set; } = new AutoUpdateConfig();
        public string DataDirectory { get; set; } = "";
        public int SaveIntervalSeconds { get; set; } = 30;
        public int XpPerLevelSquared { get; set; } = 100;
        public int MaxXpPerPlayerPerRound { get; set; } = 500;
        [Description("Stats API for the website leaderboard and the Discord bot (BrainlessDiscordBot repo). Put it behind Cloudflare for HTTPS; see the Stats API section of the repository README.")]
        public StatsApiConfig StatsApi { get; set; } = new StatsApiConfig();
    }
}
