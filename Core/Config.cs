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
        public DiscordConfig Discord { get; set; } = new DiscordConfig();
    }

    public sealed class DiscordConfig
    {
        public bool Enabled { get; set; }
        public string Token { get; set; } = "";
        public string TokenEnvironmentVariable { get; set; } = "BRAINLESS_DISCORD_TOKEN";
        public string GuildId { get; set; } = "";
        public string CommandPrefix { get; set; } = "pb";
    }
}
