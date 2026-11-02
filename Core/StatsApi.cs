using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Brainless.Core
{
    // Small HTTP API for the website leaderboard and the Discord bot.
    // Serve it behind Cloudflare (or another HTTPS proxy): Cloudflare provides the
    // certificate and encryption to visitors, and private endpoints are additionally
    // HMAC-signed so the shared secret is never sent over any connection.
    //
    // Public:  GET  /api/v1/health
    //          GET  /api/v1/leaderboard?sort=xp|playtime&limit=1-100
    // Signed:  GET  /api/v1/discord/{discordId}
    //          POST /api/v1/discord/{discordId}/link?code=CODE
    //          POST /api/v1/discord/{discordId}/optout
    internal sealed class StatsApi : IDisposable
    {
        private const string Root = "/api/v1/";
        private readonly StatsApiConfig config;
        private readonly StatsStore store;
        private readonly int step;
        private readonly Action<string> log;
        private readonly HttpListener listener = new HttpListener();
        private readonly SignatureGuard signatures;
        private readonly ApiRateLimiter limiter;
        private readonly Dictionary<string, DateTime> linkAttempts = new Dictionary<string, DateTime>(StringComparer.Ordinal);
        private readonly SemaphoreSlim concurrency = new SemaphoreSlim(32, 32);
        private readonly Dictionary<string, Cached> cache = new Dictionary<string, Cached>(StringComparer.Ordinal);
        private readonly Task worker;
        private int disposed;

        internal bool PrivateEndpointsEnabled => signatures.Enabled;

        internal StatsApi(StatsApiConfig config, StatsStore store, int step, Action<string> log)
        {
            this.config = config;
            this.store = store;
            this.step = step;
            this.log = log;
            string secret = StatsApiRules.ResolveSecret(config.SharedSecret, Environment.GetEnvironmentVariable, config.SharedSecretEnvironmentVariable);
            signatures = new SignatureGuard(secret);
            limiter = new ApiRateLimiter(config.RequestsPerMinute);
            listener.Prefixes.Add("http://" + config.BindHost + ":" + config.Port + "/");
            listener.Start();
            worker = Task.Run(Accept);
            log("Stats API listening on port " + config.Port + (signatures.Enabled ? "; Discord bot endpoints enabled." : "; leaderboard only (set stats_api.shared_secret, at least " + StatsApiRules.MinSecretLength + " characters, to enable the Discord bot endpoints)."));
        }

        private async Task Accept()
        {
            while (Volatile.Read(ref disposed) == 0)
            {
                HttpListenerContext context;
                try
                {
                    context = await listener.GetContextAsync().ConfigureAwait(false);
                }
                catch (Exception) when (Volatile.Read(ref disposed) != 0)
                {
                    return;
                }
                catch (Exception failure)
                {
                    log("Stats API accept failed (" + failure.GetType().Name + ").");
                    await Task.Delay(1000).ConfigureAwait(false);
                    continue;
                }

                if (!concurrency.Wait(0))
                {
                    Respond(context, 503, "{\"error\":\"busy\"}", false);
                    continue;
                }

                _ = Task.Run(() =>
                {
                    try
                    {
                        Handle(context);
                    }
                    catch (Exception failure)
                    {
                        log("Stats API request failed (" + failure.GetType().Name + ").");
                        try
                        {
                            Respond(context, 500, "{\"error\":\"internal\"}", false);
                        }
                        catch (Exception)
                        {
                        }
                    }
                    finally
                    {
                        concurrency.Release();
                    }
                });
            }
        }

        private string Client(HttpListenerRequest request)
        {
            if (config.TrustCloudflareHeaders)
            {
                string forwarded = request.Headers["CF-Connecting-IP"];
                if (!string.IsNullOrWhiteSpace(forwarded) && forwarded.Length <= 64)
                    return forwarded.Trim();
            }

            return request.RemoteEndPoint?.Address?.ToString() ?? "unknown";
        }

        private void Handle(HttpListenerContext context)
        {
            var request = context.Request;
            string path = request.Url.AbsolutePath;
            string method = request.HttpMethod.ToUpperInvariant();
            if (!path.StartsWith(Root, StringComparison.Ordinal))
            {
                Respond(context, 404, "{\"error\":\"not_found\"}", false);
                return;
            }

            if (method == "OPTIONS")
            {
                Respond(context, 204, null, true);
                return;
            }

            if (!limiter.Allow(Client(request), DateTime.UtcNow))
            {
                context.Response.AddHeader("Retry-After", "60");
                Respond(context, 429, "{\"error\":\"rate_limited\"}", true);
                return;
            }

            string route = path.Substring(Root.Length).TrimEnd('/');
            if (method == "GET" && route == "health")
            {
                Respond(context, 200, "{\"ok\":true,\"bot\":" + (signatures.Enabled ? "true" : "false") + "}", true);
                return;
            }

            if (method == "GET" && route == "leaderboard")
            {
                bool byPlaytime = string.Equals(request.QueryString["sort"], "playtime", StringComparison.OrdinalIgnoreCase);
                int limit = StatsApiRules.Limit(request.QueryString["limit"], 10, 100);
                Respond(context, 200, Leaderboard(byPlaytime, limit), true, "public, max-age=" + config.LeaderboardCacheSeconds);
                return;
            }

            string[] parts = route.Split('/');
            if (parts.Length < 2 || parts[0] != "discord")
            {
                Respond(context, 404, "{\"error\":\"not_found\"}", false);
                return;
            }

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (!signatures.Verify(request.Headers["X-PB-Timestamp"], request.Headers["X-PB-Nonce"], request.Headers["X-PB-Signature"], method, request.RawUrl, now))
            {
                Respond(context, signatures.Enabled ? 401 : 403, "{\"error\":\"unauthorized\"}", false);
                return;
            }

            string discordId = parts[1];
            if (!StatsApiRules.DiscordId.IsMatch(discordId))
            {
                Respond(context, 400, "{\"error\":\"bad_discord_id\"}", false);
                return;
            }

            if (method == "GET" && parts.Length == 2)
                Respond(context, 200, StatsApiRules.Player(store.FindDiscord(discordId), step), false);
            else if (method == "POST" && parts.Length == 3 && parts[2] == "link")
                Respond(context, 200, Link(discordId, request.QueryString["code"]), false);
            else if (method == "POST" && parts.Length == 3 && parts[2] == "optout")
                Respond(context, 200, "{\"optedOut\":" + (store.OptOutDiscord(discordId) ? "true" : "false") + "}", false);
            else
                Respond(context, 404, "{\"error\":\"not_found\"}", false);
        }

        private string Link(string discordId, string code)
        {
            if (string.IsNullOrEmpty(code) || !StatsApiRules.LinkCode.IsMatch(code))
                return "{\"linked\":false,\"reason\":\"invalid\"}";
            lock (linkAttempts)
            {
                var now = DateTime.UtcNow;
                if (linkAttempts.TryGetValue(discordId, out var last) && now - last < TimeSpan.FromSeconds(10))
                    return "{\"linked\":false,\"reason\":\"wait\"}";
                if (linkAttempts.Count > 10000)
                    linkAttempts.Clear();
                linkAttempts[discordId] = now;
            }

            return store.Link(discordId, code, DateTime.UtcNow) ? "{\"linked\":true}" : "{\"linked\":false,\"reason\":\"invalid\"}";
        }

        private string Leaderboard(bool byPlaytime, int limit)
        {
            string key = (byPlaytime ? "p" : "x") + limit;
            lock (cache)
            {
                if (cache.TryGetValue(key, out var hit) && DateTime.UtcNow < hit.Expires)
                    return hit.Body;
                string body = StatsApiRules.Leaderboard(store.Top(limit, byPlaytime), step, byPlaytime, DateTime.UtcNow);
                cache[key] = new Cached { Body = body, Expires = DateTime.UtcNow.AddSeconds(config.LeaderboardCacheSeconds) };
                return body;
            }
        }

        private void Respond(HttpListenerContext context, int status, string body, bool isPublic, string cacheControl = null)
        {
            var response = context.Response;
            try
            {
                response.StatusCode = status;
                response.AddHeader("X-Content-Type-Options", "nosniff");
                response.AddHeader("Cache-Control", cacheControl ?? "no-store");
                if (isPublic)
                {
                    response.AddHeader("Access-Control-Allow-Origin", string.IsNullOrWhiteSpace(config.AllowedOrigin) ? "*" : config.AllowedOrigin);
                    response.AddHeader("Access-Control-Allow-Methods", "GET, OPTIONS");
                    response.AddHeader("Access-Control-Max-Age", "86400");
                }

                if (body != null)
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(body);
                    response.ContentType = "application/json; charset=utf-8";
                    response.ContentLength64 = bytes.Length;
                    response.OutputStream.Write(bytes, 0, bytes.Length);
                }
            }
            finally
            {
                response.Close();
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0)
                return;
            try
            {
                listener.Stop();
                listener.Close();
            }
            catch (Exception)
            {
            }

            worker.ContinueWith(_ => concurrency.Dispose(), TaskScheduler.Default);
        }

        private sealed class Cached
        {
            internal string Body;
            internal DateTime Expires;
        }
    }
}
