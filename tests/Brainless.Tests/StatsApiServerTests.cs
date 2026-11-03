using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using Brainless.Core;

// Starts the real stats API on a loopback port and calls it like the website and bot do.
internal static class StatsApiServerTests
{
    internal static void Run(Action<bool, string> check)
    {
        string directory = Path.Combine(Path.GetTempPath(), "brainless-api-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        int port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        var store = new StatsStore(Path.Combine(directory, "stats.json"), 500);
        store.Record("76561198000000001@steam", "Alpha", "kill_scp", 400, "a");
        store.Record("76561198000000002@steam", "Bravo", "escape", 100, "b");
        var config = new StatsApiConfig { Enabled = true, Port = port, BindHost = "127.0.0.1", SharedSecret = StatsApiTests.Secret, AllowedOrigin = "https://example.com", RequestsPerMinute = 100, LeaderboardCacheSeconds = 0 };
        config.Validate();
        var api = new StatsApi(config, store, 100, _ => { }, () => new ServerSnapshot(12, 25, true, 60, DateTime.UtcNow));
        using var http = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:" + port) };
        HttpResponseMessage Send(HttpMethod method, string path, bool signed = true, long? at = null, string nonceOverride = null)
        {
            var request = new HttpRequestMessage(method, path);
            if (signed)
            {
                long timestamp = at ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                string nonce = nonceOverride ?? Guid.NewGuid().ToString("N");
                request.Headers.Add("X-PB-Timestamp", timestamp.ToString());
                request.Headers.Add("X-PB-Nonce", nonce);
                request.Headers.Add("X-PB-Signature", StatsApiRules.Sign(StatsApiTests.Secret, timestamp, nonce, method.Method, path));
            }

            return http.Send(request);
        }

        string Body(HttpResponseMessage response) => response.Content.ReadAsStringAsync().Result;
        try
        {
            var board = Send(HttpMethod.Get, "/api/v1/leaderboard?limit=5", false);
            string boardJson = Body(board);
            check(board.StatusCode == HttpStatusCode.OK && boardJson.Contains("\"name\":\"Alpha\"") && boardJson.IndexOf("Alpha", StringComparison.Ordinal) < boardJson.IndexOf("Bravo", StringComparison.Ordinal) && !boardJson.Contains("@steam"), "the live leaderboard endpoint serves ranked players without account IDs");
            check(board.Headers.GetValues("Access-Control-Allow-Origin").Single() == "https://example.com", "the leaderboard allows the configured website to read it from a browser");
            check(Body(Send(HttpMethod.Get, "/api/v1/status", false)).Contains("\"players\":12,\"maxPlayers\":25"), "the live status endpoint is public and reports the population");
            check(Send(HttpMethod.Get, "/api/v1/discord/123", false).StatusCode == HttpStatusCode.Unauthorized, "private endpoints refuse unsigned requests");
            check(Send(HttpMethod.Get, "/api/v1/discord/123").StatusCode == HttpStatusCode.OK && Body(Send(HttpMethod.Get, "/api/v1/discord/123")) == "{\"linked\":false}", "a signed lookup of an unlinked Discord account answers not linked");
            long stamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 1;
            check(Send(HttpMethod.Get, "/api/v1/discord/124", true, stamp, "replayreplayreplay1").StatusCode == HttpStatusCode.OK && Send(HttpMethod.Get, "/api/v1/discord/124", true, stamp, "replayreplayreplay1").StatusCode == HttpStatusCode.Unauthorized, "the live API rejects a replayed signed request");
            string code = store.CreateLinkCode("76561198000000001@steam", "Alpha", DateTime.UtcNow);
            check(Body(Send(HttpMethod.Post, "/api/v1/discord/555/link?code=" + code)) == "{\"linked\":true}", "the bot can link a game account with a .pblink code");
            check(Body(Send(HttpMethod.Get, "/api/v1/discord/555")).Contains("\"name\":\"Alpha\""), "linked stats are returned to the bot");
            check(Body(Send(HttpMethod.Post, "/api/v1/discord/555/optout")) == "{\"optedOut\":true}" && Body(Send(HttpMethod.Get, "/api/v1/discord/555")) == "{\"linked\":false}" && !Body(Send(HttpMethod.Get, "/api/v1/leaderboard", false)).Contains("Alpha"), "opting out through the bot erases the player from the API and leaderboard");
            check(Send(HttpMethod.Get, "/other", false).StatusCode == HttpStatusCode.NotFound && Send(HttpMethod.Get, "/api/v1/discord/abc").StatusCode == HttpStatusCode.BadRequest, "unknown paths and malformed Discord IDs are rejected");
        }
        finally
        {
            api.Dispose();
            Directory.Delete(directory, true);
        }
    }
}
