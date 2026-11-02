using System.ComponentModel;
using Brainless.Internal.Updates;
using Exiled.API.Interfaces;

namespace Brainless.Core
{
    public sealed class Config : IConfig
    {
        public bool IsEnabled { get; set; } = true;
        public bool Debug { get; set; }
        public AutoUpdateConfig AutoUpdate { get; set; } = new AutoUpdateConfig();
        public string DataDirectory { get; set; } = "";
        public int SaveIntervalSeconds { get; set; } = 30;
        public int XpPerLevelSquared { get; set; } = 100;
        public int MaxXpPerPlayerPerRound { get; set; } = 500;
        [Description("Stats API for the website leaderboard and the Discord bot (discord-bot folder). Put it behind Cloudflare for HTTPS.")]
        public StatsApiConfig StatsApi { get; set; } = new StatsApiConfig();
    }
}
