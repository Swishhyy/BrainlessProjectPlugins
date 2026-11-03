using System;
using System.Collections.Generic;
using System.Linq;
using Brainless.Core;

internal static class StatsApiTests
{
    // Shared with BrainlessDiscordBot test/bot.test.mjs so the plugin and the bot agree.
    internal const string Secret = "0123456789abcdef0123456789abcdef";

    internal static void Run(Action<bool, string> check)
    {
        check(StatsApiRules.Sign(Secret, 1700000000, "n0nce000000000001", "GET", "/api/v1/discord/123456789012345678") == "048eeb8034465085732b1438fcb9f028f9417a3ed8da1b4907952266d36c0b8a", "stats API signatures match the reference HMAC-SHA256 vector");
        check(StatsApiRules.Sign(Secret, 1700000000, "n0nce000000000002", "post", "/api/v1/discord/123456789012345678/link?code=ABCD2345") == "57383927aa84c3f78d7edb06a655ec5d2c8769e1b6a116d26df3e2bc231adbaf", "signed link requests cover the method and query string");

        long now = 1700000000;
        var guard = new SignatureGuard(Secret);
        string path = "/api/v1/discord/42/optout";
        const string nonce = "aaaaaaaaaaaaaaaa1", nonce2 = "aaaaaaaaaaaaaaaa2";
        string good = StatsApiRules.Sign(Secret, now, nonce, "POST", path);
        check(guard.Verify(now.ToString(), nonce, good, "POST", path, now + 5), "a fresh correctly signed request is accepted");
        check(!guard.Verify(now.ToString(), nonce, good, "POST", path, now + 6), "a captured signed request cannot be replayed");
        check(guard.Verify(now.ToString(), nonce2, StatsApiRules.Sign(Secret, now, nonce2, "POST", path), "POST", path, now + 6), "the same command repeated within a second is still accepted with a new nonce");
        string other = StatsApiRules.Sign(Secret, now, nonce, "POST", "/api/v1/discord/43/optout");
        check(!guard.Verify(now.ToString(), nonce, other, "POST", path, now), "a signature for one Discord account cannot act on another");
        string old = StatsApiRules.Sign(Secret, now - 400, nonce, "GET", path);
        check(!guard.Verify((now - 400).ToString(), nonce, old, "GET", path, now), "signatures older than five minutes are rejected");
        check(!guard.Verify(now.ToString(), nonce, StatsApiRules.Sign("wrong-secret-wrong-secret-wrong-secret", now, nonce, "GET", path), "GET", path, now), "a request signed with another secret is rejected");
        check(!guard.Verify(now.ToString(), "short", StatsApiRules.Sign(Secret, now, "short", "GET", path), "GET", path, now), "signed requests need a proper random nonce");
        check(!new SignatureGuard("short").Enabled && !new SignatureGuard("").Verify(now.ToString(), nonce, good, "POST", path, now), "private endpoints stay closed without a strong shared secret");
        check(StatsApiRules.ResolveSecret("", _ => " env-secret ", "BRAINLESS_API_SECRET") == "env-secret" && StatsApiRules.ResolveSecret(" yaml ", _ => throw new Exception(), "X") == "yaml", "the shared secret comes from YAML first, then the environment");

        var limiter = new ApiRateLimiter(3);
        var t = new DateTime(2026, 1, 1, 0, 0, 10, DateTimeKind.Utc);
        check(limiter.Allow("a", t) && limiter.Allow("a", t) && limiter.Allow("a", t) && !limiter.Allow("a", t) && limiter.Allow("b", t), "the rate limit applies per client address");
        check(limiter.Allow("a", t.AddMinutes(1)), "the rate limit resets each minute");

        var players = new List<PlayerStats>
        {
            new PlayerStats { UserId = "1@steam", Name = "<script>\"Top\"", Xp = 400, PlaytimeSeconds = 5400 },
            new PlayerStats { UserId = "2@steam", Name = "Second", Xp = 100, PlaytimeSeconds = 36000 }
        };
        string board = StatsApiRules.Leaderboard(players, 100, false, new DateTime(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc));
        using (var json = System.Text.Json.JsonDocument.Parse(board))
        {
            var rows = json.RootElement.GetProperty("players");
            check(rows.GetArrayLength() == 2 && rows[0].GetProperty("rank").GetInt32() == 1 && rows[0].GetProperty("level").GetInt32() == 2 && rows[0].GetProperty("playtimeHours").GetDouble() == 1.5 && json.RootElement.GetProperty("updated").GetString() == "2026-11-02T12:00:00Z", "the leaderboard JSON lists rank, name, level, XP and playtime");
            check(rows[0].GetProperty("name").GetString() == "script\"Top\"", "leaderboard names are sanitized and safely escaped");
        }

        check(!board.Contains("@steam") && !board.Contains('<'), "the public leaderboard never exposes game account IDs or raw HTML");
        using (var json = System.Text.Json.JsonDocument.Parse(StatsApiRules.Player(players[1], 100)))
            check(json.RootElement.GetProperty("linked").GetBoolean() && json.RootElement.GetProperty("playtimeSeconds").GetInt64() == 36000 && !json.RootElement.ToString().Contains("@steam"), "bot player lookups return stats without the game account ID");
        check(StatsApiRules.Player(null, 100) == "{\"linked\":false}", "an unlinked Discord account gets a plain not-linked reply");
        check(StatsApiRules.Limit("500", 10, 100) == 100 && StatsApiRules.Limit("x", 10, 100) == 10 && StatsApiRules.Limit("-3", 10, 100) == 1, "leaderboard sizes are clamped to 1-100");
        check(StatsApiRules.DiscordId.IsMatch("123456789012345678") && !StatsApiRules.DiscordId.IsMatch("12a") && StatsApiRules.LinkCode.IsMatch("ABCD2345") && !StatsApiRules.LinkCode.IsMatch("AB/../x"), "Discord IDs and link codes are validated before use");
        check(StatsText.Playtime(90061) == "25h 1m", "server playtime display does not wrap back to zero after one day");
        check(!StatsText.SafeName("@everyone **test**\n").Contains('@') && !StatsText.SafeName("@everyone **test**\n").Contains('\n'), "leaderboard names cannot generate mentions or inject extra output lines");
    }
}
