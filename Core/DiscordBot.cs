using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Brainless.Core
{
    // Uses BCL WebSockets/HTTP and EXILED's existing YAML JSON support. No extra
    // audio/Discord library, listener port, or privileged message-content intent.
    internal sealed class DiscordBot : IDisposable
    {
        private readonly DiscordConfig config;
        private readonly StatsStore store;
        private readonly int step;
        private readonly Action<string> log;
        private readonly CancellationTokenSource stop = new CancellationTokenSource();
        private readonly HttpClient http = new HttpClient(new HttpClientHandler { UseProxy = false })
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
        private readonly SemaphoreSlim sends = new SemaphoreSlim(1, 1);
        private readonly Task worker;
        private readonly Dictionary<string, DateTime> linkAttempts = new Dictionary<string, DateTime>();
        private ClientWebSocket socket;
        private int? sequence;
        private volatile bool ack = true;
        private volatile bool connected;
        private bool registered;
        private int disposed;
        private int activeResponses;
        internal bool Connected => connected;

        internal DiscordBot(DiscordConfig config, StatsStore store, int step, Action<string> log)
        {
            this.config = config;
            this.store = store;
            this.step = step;
            this.log = log;
            string token = !string.IsNullOrWhiteSpace(config.Token) ? config.Token.Trim() : string.IsNullOrWhiteSpace(config.TokenEnvironmentVariable) ? null : Environment.GetEnvironmentVariable(config.TokenEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(token))
                throw new InvalidOperationException("Set discord.token before enabling the bot.");
            if (!Regex.IsMatch(config.CommandPrefix ?? "", "^[a-z][a-z0-9]{0,11}$") || (!string.IsNullOrEmpty(config.GuildId) && !Regex.IsMatch(config.GuildId, "^[0-9]{1,24}$")))
                throw new InvalidOperationException("Invalid Discord command prefix or guild ID.");
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bot", token);
            http.DefaultRequestHeaders.UserAgent.ParseAdd("ProjectBrainless/0.1");
            worker = Task.Run(() => Run(token));
        }

        private async Task<object> Request(HttpMethod method, string route, object data = null)
        {
            for (int retry = 0; retry < 3; retry++)
            {
                using (var request = new HttpRequestMessage(method, "https://discord.com/api/v10/" + route))
                {
                    if (data != null)
                        request.Content = new StringContent(DiscordProtocol.Write(data), Encoding.UTF8, "application/json");
                    using (var response = await http.SendAsync(request, stop.Token).ConfigureAwait(false))
                    {
                        string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        if ((int)response.StatusCode == 429)
                        {
                            var limit = DiscordProtocol.Read(body);
                            double.TryParse(DiscordProtocol.Text(DiscordProtocol.At(limit, "retry_after")), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double seconds);
                            await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, Math.Min(60, seconds))), stop.Token).ConfigureAwait(false);
                            continue;
                        }

                        if (!response.IsSuccessStatusCode)
                            throw new DiscordFailure((int)response.StatusCode);
                        return string.IsNullOrWhiteSpace(body) ? null : DiscordProtocol.Read(body);
                    }
                }
            }

            throw new DiscordFailure(429);
        }

        private async Task Send(object value, CancellationToken cancellation)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(DiscordProtocol.Write(value));
            await sends.WaitAsync(cancellation).ConfigureAwait(false);
            try
            {
                await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellation).ConfigureAwait(false);
            }
            finally
            {
                sends.Release();
            }
        }

        private async Task<object> Receive(CancellationToken cancellation)
        {
            var buffer = new byte[8192];
            using (var result = new MemoryStream())
            {
                WebSocketReceiveResult part;
                do
                {
                    part = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellation).ConfigureAwait(false);
                    if (part.MessageType == WebSocketMessageType.Close)
                        return null;
                    result.Write(buffer, 0, part.Count);
                    if (result.Length > 2 * 1024 * 1024)
                        throw new InvalidDataException("Gateway message too large.");
                }
                while (!part.EndOfMessage);
                return DiscordProtocol.Read(Encoding.UTF8.GetString(result.ToArray()));
            }
        }

        private async Task Heartbeat(int milliseconds, CancellationToken cancellation)
        {
            while (!cancellation.IsCancellationRequested)
            {
                await Task.Delay(milliseconds, cancellation).ConfigureAwait(false);
                if (!ack)
                {
                    socket.Abort();
                    return;
                }

                ack = false;
                await Send(DiscordProtocol.Object("op", 1, "d", sequence), cancellation).ConfigureAwait(false);
            }
        }

        private async Task Run(string token)
        {
            int retrySeconds = 5;
            while (!stop.IsCancellationRequested)
            {
                try
                {
                    var gateway = await Request(HttpMethod.Get, "gateway/bot").ConfigureAwait(false);
                    var quota = DiscordProtocol.At(gateway, "session_start_limit");
                    if (quota != null && DiscordProtocol.Number(DiscordProtocol.At(quota, "remaining")) == 0)
                    {
                        await Task.Delay(Math.Max(5000, DiscordProtocol.Number(DiscordProtocol.At(quota, "reset_after"))), stop.Token).ConfigureAwait(false);
                        continue;
                    }

                    if (!registered)
                    {
                        var application = await Request(HttpMethod.Get, "oauth2/applications/@me").ConfigureAwait(false);
                        await Register(DiscordProtocol.Text(DiscordProtocol.At(application, "id"))).ConfigureAwait(false);
                        registered = true;
                    }

                    string url = DiscordProtocol.Text(DiscordProtocol.At(gateway, "url"));
                    if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != "wss" || !uri.Host.EndsWith(".discord.gg", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("Invalid Discord gateway host.");
                    using (var session = CancellationTokenSource.CreateLinkedTokenSource(stop.Token))
                    using (socket = new ClientWebSocket())
                    {
                        sequence = null;
                        ack = true;
                        await socket.ConnectAsync(new Uri(url.TrimEnd('/') + "/?v=10&encoding=json"), stop.Token).ConfigureAwait(false);
                        var hello = await Receive(stop.Token).ConfigureAwait(false);
                        if (DiscordProtocol.Number(DiscordProtocol.At(hello, "op")) != 10)
                            throw new InvalidDataException("Expected gateway hello.");
                        int interval = DiscordProtocol.Number(DiscordProtocol.At(DiscordProtocol.At(hello, "d"), "heartbeat_interval"));
                        if (interval < 1000 || interval > 120000)
                            throw new InvalidDataException("Invalid heartbeat interval.");
                        Task heartbeat = Heartbeat(interval, session.Token);
                        try
                        {
                            await Send(DiscordProtocol.Object("op", 2, "d", DiscordProtocol.Object("token", token, "intents", 1, "properties", DiscordProtocol.Object("os", "linux", "browser", "ProjectBrainless", "device", "ProjectBrainless"))), stop.Token).ConfigureAwait(false);
                            while (!stop.IsCancellationRequested && socket.State == WebSocketState.Open)
                            {
                                var message = await Receive(stop.Token).ConfigureAwait(false);
                                if (message == null)
                                    break;
                                if (DiscordProtocol.At(message, "s") != null)
                                    sequence = DiscordProtocol.Number(DiscordProtocol.At(message, "s"));
                                int op = DiscordProtocol.Number(DiscordProtocol.At(message, "op"));
                                if (op == 11)
                                {
                                    ack = true;
                                    continue;
                                }

                                if (op == 1)
                                {
                                    await Send(DiscordProtocol.Object("op", 1, "d", sequence), stop.Token).ConfigureAwait(false);
                                    continue;
                                }

                                if (op == 7 || op == 9)
                                    break;
                                string type = DiscordProtocol.Text(DiscordProtocol.At(message, "t"));
                                var data = DiscordProtocol.At(message, "d");
                                if (type == "READY")
                                {
                                    connected = true;
                                    log("Discord bot connected.");
                                }
                                else if (type == "INTERACTION_CREATE")
                                    _ = HandleInteraction(data);
                            }
                        }
                        finally
                        {
                            connected = false;
                            session.Cancel();
                            try
                            {
                                await heartbeat.ConfigureAwait(false);
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                }
                catch (OperationCanceledException)when (stop.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception failure)
                {
                    log("Discord connection failed (" + failure.GetType().Name + "); retrying. Tokens and response bodies are not logged.");
                }

                connected = false;
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(retrySeconds), stop.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                retrySeconds = Math.Min(300, retrySeconds * 2);
            }
        }

        private async Task Register(string application)
        {
            if (!Regex.IsMatch(application, "^[0-9]{1,24}$"))
                throw new InvalidDataException("Missing application ID.");
            string route = "applications/" + application + (string.IsNullOrEmpty(config.GuildId) ? "" : "/guilds/" + config.GuildId) + "/commands";
            foreach (var command in DiscordProtocol.Commands(config.CommandPrefix))
            {
                await Request(HttpMethod.Post, route, command).ConfigureAwait(false);
            }

            log("Discord slash commands registered: stats, playtime, top, link and optout (prefix " + config.CommandPrefix + ").");
        }

        private async Task HandleInteraction(object data)
        {
            if (Interlocked.Increment(ref activeResponses) > 16)
            {
                Interlocked.Decrement(ref activeResponses);
                return;
            }

            try
            {
                await Interaction(data).ConfigureAwait(false);
            }
            catch (OperationCanceledException)when (stop.IsCancellationRequested)
            {
            }
            catch (Exception failure)
            {
                log("Discord command failed (" + failure.GetType().Name + ").");
            }
            finally
            {
                Interlocked.Decrement(ref activeResponses);
            }
        }

        private async Task Interaction(object data)
        {
            if (DiscordProtocol.Number(DiscordProtocol.At(data, "type")) != 2)
                return;
            string guild = DiscordProtocol.Text(DiscordProtocol.At(data, "guild_id"));
            if (!string.IsNullOrEmpty(config.GuildId) && guild != config.GuildId)
                return;
            var command = DiscordProtocol.At(data, "data");
            string name = DiscordProtocol.Text(DiscordProtocol.At(command, "name"));
            string user = DiscordProtocol.Text(DiscordProtocol.At(DiscordProtocol.At(DiscordProtocol.At(data, "member"), "user"), "id"));
            if (user.Length == 0)
                user = DiscordProtocol.Text(DiscordProtocol.At(DiscordProtocol.At(data, "user"), "id"));
            if (user.Length == 0)
                return;
            string answer;
            if (name == config.CommandPrefix + "-stats" || name == config.CommandPrefix + "-playtime")
            {
                string selected = DiscordProtocol.Option(command, "user");
                var stats = store.FindDiscord(string.IsNullOrWhiteSpace(selected) ? user : selected);
                answer = name.EndsWith("-playtime", StringComparison.Ordinal) && stats != null ? DiscordProtocol.SafeName(stats.Name) + " — server playtime: " + DiscordProtocol.Playtime(stats.PlaytimeSeconds) : DiscordProtocol.Describe(stats, step);
            }
            else if (name == config.CommandPrefix + "-optout")
                answer = store.OptOutDiscord(user) ? "Tracking disabled. Your game XP, playtime, name and Discord link were erased, including stored stats backups. Only a salted opt-out marker is retained. Use .pbprivacy optin in-game to start fresh." : "No linked stats are stored for this Discord account. Use .pbprivacy optout in-game to disable tracking for your game account.";
            else if (name == config.CommandPrefix + "-top")
            {
                var top = store.Top(10);
                answer = top.Length == 0 ? "No recorded XP yet." : string.Join("\n", top.Select((p, i) => (i + 1) + ". " + DiscordProtocol.SafeName(p.Name) + " — " + p.Xp + " XP (level " + p.Level(step) + ")"));
            }
            else if (name == config.CommandPrefix + "-link")
            {
                lock (linkAttempts)
                {
                    if (linkAttempts.TryGetValue(user, out var attempted) && DateTime.UtcNow - attempted < TimeSpan.FromSeconds(10))
                        answer = "Wait ten seconds before another link attempt.";
                    else
                    {
                        if (linkAttempts.Count > 10000)
                            linkAttempts.Clear();
                        linkAttempts[user] = DateTime.UtcNow;
                        var options = DiscordProtocol.At(command, "options") as IEnumerable;
                        string code = options == null ? "" : options.Cast<object>().Where(o => DiscordProtocol.Text(DiscordProtocol.At(o, "name")) == "code").Select(o => DiscordProtocol.Text(DiscordProtocol.At(o, "value"))).FirstOrDefault();
                        answer = store.Link(user, code, DateTime.UtcNow) ? "Your Project Brainless game account is linked." : "That code is invalid or expired. Use .pblink in the game's F1 console for a fresh code.";
                    }
                }
            }
            else
                return;
            answer = answer.Substring(0, Math.Min(1900, answer.Length));
            string id = DiscordProtocol.Text(DiscordProtocol.At(data, "id"));
            string interactionToken = DiscordProtocol.Text(DiscordProtocol.At(data, "token"));
            if (!Regex.IsMatch(id, "^[0-9]{1,24}$") || interactionToken.Length == 0)
                return;
            try
            {
                await Request(HttpMethod.Post, "interactions/" + id + "/" + Uri.EscapeDataString(interactionToken) + "/callback", DiscordProtocol.Object("type", 4, "data", DiscordProtocol.Object("content", answer, "flags", 64, "allowed_mentions", DiscordProtocol.Object("parse", Array.Empty<string>())))).ConfigureAwait(false);
            }
            catch (Exception failure)
            {
                log("Discord command response failed (" + failure.GetType().Name + ").");
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0)
                return;
            stop.Cancel();
            socket?.Abort();
            worker.ContinueWith(_ =>
            {
                http.Dispose();
                sends.Dispose();
                stop.Dispose();
            }, TaskScheduler.Default);
        }

        private sealed class DiscordFailure : Exception
        {
            internal DiscordFailure(int status) : base("Discord HTTP " + status)
            {
            }
        }
    }
}
