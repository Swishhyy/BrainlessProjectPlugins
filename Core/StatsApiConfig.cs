using System;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace Brainless.Core
{
    public sealed class StatsApiConfig
    {
        public bool Enabled { get; set; }

        [Description("TCP port to listen on. Use a port your host forwards to this server (often the game port itself works, since the game only uses UDP).")]
        public int Port { get; set; }

        [Description("Address to listen on: * for all.")]
        public string BindHost { get; set; } = "*";

        [Description("Secret shared with the Discord bot (at least 32 characters). Signs private requests; it is never sent over the network. Empty keeps only the public leaderboard.")]
        public string SharedSecret { get; set; } = "";

        public string SharedSecretEnvironmentVariable { get; set; } = "BRAINLESS_API_SECRET";

        [Description("Website allowed to read the leaderboard from a browser, e.g. https://example.com. * allows any site.")]
        public string AllowedOrigin { get; set; } = "*";

        [Description("Use Cloudflare's CF-Connecting-IP header for rate limiting. Only safe when the port is reached through Cloudflare.")]
        public bool TrustCloudflareHeaders { get; set; } = true;

        public int RequestsPerMinute { get; set; } = 60;
        public int LeaderboardCacheSeconds { get; set; } = 30;

        [Description("Slash command prefix the Discord bot uses (shown in .pblink replies).")]
        public string DiscordCommandPrefix { get; set; } = "pb";

        internal void Validate()
        {
            if (Enabled && (Port < 1 || Port > 65535))
                throw new InvalidOperationException("stats_api.port must be 1-65535 when the stats API is enabled.");
            if (string.IsNullOrWhiteSpace(BindHost) || BindHost.Contains("/") || RequestsPerMinute < 1 || RequestsPerMinute > 10000 || LeaderboardCacheSeconds < 0 || LeaderboardCacheSeconds > 3600)
                throw new InvalidOperationException("Invalid stats_api settings.");
            if (!Regex.IsMatch(DiscordCommandPrefix ?? "", "^[a-z][a-z0-9]{0,11}$"))
                throw new InvalidOperationException("stats_api.discord_command_prefix must be 1-12 lowercase letters/digits.");
        }
    }
}
