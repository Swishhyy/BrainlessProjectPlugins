using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Brainless.Core
{
    // Pure logic behind the stats API, kept free of HttpListener so it can be tested.
    //
    // Private endpoints are signed instead of carrying a password: the caller sends
    //   X-PB-Timestamp: <unix seconds>
    //   X-PB-Nonce:     <16-64 random letters/digits, new for every request>
    //   X-PB-Signature: hex(HMAC-SHA256(secret, timestamp + "\n" + nonce + "\n" + METHOD + "\n" + pathAndQuery))
    // The secret never crosses the network, a signature only fits that exact request,
    // and old or repeated signatures are rejected.
    internal static class StatsApiRules
    {
        internal const int MaxClockSkewSeconds = 300;
        internal const int MinSecretLength = 32;
        internal static readonly Regex DiscordId = new Regex("^[0-9]{1,24}$", RegexOptions.CultureInvariant);
        internal static readonly Regex Nonce = new Regex("^[A-Za-z0-9]{16,64}$", RegexOptions.CultureInvariant);
        internal static readonly Regex LinkCode = new Regex("^[A-Za-z0-9]{4,16}$", RegexOptions.CultureInvariant);

        internal static string Sign(string secret, long timestamp, string nonce, string method, string pathAndQuery)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(timestamp.ToString(CultureInfo.InvariantCulture) + "\n" + nonce + "\n" + method.ToUpperInvariant() + "\n" + pathAndQuery));
                return string.Concat(hash.Select(b => b.ToString("x2", CultureInfo.InvariantCulture)));
            }
        }

        internal static bool FixedTimeEquals(string a, string b)
        {
            if (a == null || b == null || a.Length != b.Length)
                return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }

        internal static bool SecretUsable(string secret) => !string.IsNullOrEmpty(secret) && secret.Length >= MinSecretLength;

        internal static string ResolveSecret(string configured, Func<string, string> environment, string variable)
        {
            if (!string.IsNullOrWhiteSpace(configured))
                return configured.Trim();
            return string.IsNullOrWhiteSpace(variable) ? "" : (environment(variable) ?? "").Trim();
        }

        internal static string Json(string value)
        {
            if (value == null)
                return "null";
            var builder = new StringBuilder(value.Length + 2).Append('"');
            foreach (char c in value)
            {
                switch (c)
                {
                    case '"': builder.Append("\\\""); break;
                    case '\\': builder.Append("\\\\"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    case '<': builder.Append("\\u003c"); break;
                    case '>': builder.Append("\\u003e"); break;
                    case '&': builder.Append("\\u0026"); break;
                    default:
                        if (c < 0x20 || c == (char)0x2028 || c == (char)0x2029)
                            builder.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else
                            builder.Append(c);
                        break;
                }
            }

            return builder.Append('"').ToString();
        }

        internal static string Number(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

        internal static string Leaderboard(IEnumerable<PlayerStats> players, int step, bool byPlaytime, DateTime updated)
        {
            var rows = players.Select((p, i) => "{\"rank\":" + (i + 1) + ",\"name\":" + Json(StatsText.SafeName(p.Name)) + ",\"level\":" + p.Level(step) + ",\"xp\":" + p.Xp + ",\"playtimeHours\":" + Number(p.PlaytimeSeconds / 3600.0) + "}");
            return "{\"updated\":" + Json(updated.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture)) + ",\"sort\":" + Json(byPlaytime ? "playtime" : "xp") + ",\"players\":[" + string.Join(",", rows) + "]}";
        }

        internal static string Player(PlayerStats player, int step)
        {
            if (player == null)
                return "{\"linked\":false}";
            string actions = string.Join(",", player.Actions.OrderBy(a => a.Key, StringComparer.Ordinal).Select(a => Json(a.Key) + ":" + a.Value));
            return "{\"linked\":true,\"name\":" + Json(StatsText.SafeName(player.Name)) + ",\"level\":" + player.Level(step) + ",\"xp\":" + player.Xp + ",\"playtimeSeconds\":" + player.PlaytimeSeconds + ",\"actions\":{" + actions + "}}";
        }

        // Public server status for the website and the Discord bot. A snapshot older
        // than 30 seconds means the game loop is not running, so report offline.
        internal static string Status(ServerSnapshot snapshot, DateTime now)
        {
            if (snapshot == null || (now - snapshot.Captured).TotalSeconds > 30)
                return "{\"online\":false}";
            return "{\"online\":true,\"players\":" + snapshot.Players + ",\"maxPlayers\":" + snapshot.MaxPlayers + ",\"roundStarted\":" + (snapshot.RoundStarted ? "true" : "false") + ",\"roundSeconds\":" + snapshot.RoundSeconds + ",\"updated\":" + Json(snapshot.Captured.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture)) + "}";
        }

        internal static int Limit(string raw, int fallback, int max)
        {
            if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
                value = fallback;
            return Math.Max(1, Math.Min(max, value));
        }
    }

    // Player count and round state, captured on the game thread every couple of
    // seconds so API threads never touch Unity or EXILED objects.
    internal sealed class ServerSnapshot
    {
        internal ServerSnapshot(int players, int maxPlayers, bool roundStarted, int roundSeconds, DateTime captured)
        {
            Players = players;
            MaxPlayers = maxPlayers;
            RoundStarted = roundStarted;
            RoundSeconds = roundSeconds;
            Captured = captured;
        }

        internal int Players { get; }
        internal int MaxPlayers { get; }
        internal bool RoundStarted { get; }
        internal int RoundSeconds { get; }
        internal DateTime Captured { get; }
    }

    // Checks private-endpoint signatures and remembers recent ones so a captured
    // request cannot be replayed.
    internal sealed class SignatureGuard
    {
        private readonly string secret;
        private readonly Dictionary<string, long> seen = new Dictionary<string, long>(StringComparer.Ordinal);
        private readonly object gate = new object();

        internal SignatureGuard(string secret) => this.secret = secret;
        internal bool Enabled => StatsApiRules.SecretUsable(secret);

        internal bool Verify(string timestamp, string nonce, string signature, string method, string pathAndQuery, long now)
        {
            if (!Enabled || nonce == null || !StatsApiRules.Nonce.IsMatch(nonce) || string.IsNullOrEmpty(signature) || signature.Length != 64 || !long.TryParse(timestamp, NumberStyles.Integer, CultureInfo.InvariantCulture, out long sent))
                return false;
            if (Math.Abs(now - sent) > StatsApiRules.MaxClockSkewSeconds)
                return false;
            string expected = StatsApiRules.Sign(secret, sent, nonce, method, pathAndQuery);
            if (!StatsApiRules.FixedTimeEquals(expected, signature.ToLowerInvariant()))
                return false;
            lock (gate)
            {
                if (seen.Count > 20000)
                    foreach (var old in seen.Where(p => now - p.Value > StatsApiRules.MaxClockSkewSeconds * 2).Select(p => p.Key).ToArray())
                        seen.Remove(old);
                if (seen.ContainsKey(expected))
                    return false;
                seen[expected] = sent;
                return true;
            }
        }
    }

    // Fixed one-minute window per client address.
    internal sealed class ApiRateLimiter
    {
        private readonly int perMinute;
        private readonly Dictionary<string, Window> clients = new Dictionary<string, Window>(StringComparer.Ordinal);
        private readonly object gate = new object();

        internal ApiRateLimiter(int perMinute) => this.perMinute = perMinute;

        internal bool Allow(string client, DateTime now)
        {
            long minute = now.Ticks / TimeSpan.TicksPerMinute;
            lock (gate)
            {
                if (clients.Count > 50000)
                    foreach (var stale in clients.Where(p => p.Value.Minute != minute).Select(p => p.Key).ToArray())
                        clients.Remove(stale);
                if (!clients.TryGetValue(client ?? "", out var window) || window.Minute != minute)
                    clients[client ?? ""] = window = new Window { Minute = minute };
                return ++window.Count <= perMinute;
            }
        }

        private sealed class Window
        {
            internal long Minute;
            internal int Count;
        }
    }
}
