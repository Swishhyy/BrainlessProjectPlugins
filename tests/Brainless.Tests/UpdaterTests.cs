using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Brainless.Internal.Updates;

internal static class UpdaterTests
{
    private const string Assembly = "Brainless.UpdateFixture";
    internal static async Task Run(Action<bool, string> check)
    {
        byte[] old = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Old", Assembly + ".dll"));
        byte[] next = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "New", Assembly + ".dll"));
        string directory = Path.Combine(Path.GetTempPath(), "brainless-update-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string target = Path.Combine(directory, Assembly + ".dll");
        try
        {
            void Reset()
            {
                File.WriteAllBytes(target, old);
                if (File.Exists(target + ".previous"))
                    File.Delete(target + ".previous");
            }

            Reset();
            var credentials = new AutoUpdateConfig
            {
                Token = "  config-test-token  ",
                TokenEnvironmentVariable = "SHOULD_NOT_BE_READ"
            };
            check(credentials.ResolveToken(_ => throw new Exception("Config token must not need a host environment variable.")) == "config-test-token", "a pasted config token works without host setup and overrides the environment");
            credentials.Token = " \t ";
            credentials.TokenEnvironmentVariable = " TEST_GITHUB_TOKEN ";
            check(credentials.ResolveToken(name => name == "TEST_GITHUB_TOKEN" ? " environment-test-token " : null) == "environment-test-token", "an empty config token preserves existing environment authentication and trims surrounding whitespace");
            credentials.TokenEnvironmentVariable = null;
            check(credentials.ResolveToken(_ => throw new Exception("A disabled environment fallback must not be read.")) == null, "both authentication sources can be left empty for public releases");
            credentials.TokenEnvironmentVariable = "TEST_GITHUB_TOKEN";
            check(credentials.ResolveToken(_ => null) == null && credentials.ResolveToken(_ => " \t ") == null, "missing or blank environment credentials do not create an authorization header");
            check(UpdatePolicy.ParseVersion("1.1.0") == new Version(1, 1, 0, 0), "updater normalizes three-part and assembly versions");
            check(Rejects(() => UpdatePolicy.ParseVersion("1.1.0-beta")), "updater rejects ambiguous manifest versions");
            check(UpdatePolicy.IsTrustedDownload(new Uri("https://release-assets.githubusercontent.com/a")), "updater accepts GitHub's asset CDN");
            check(!UpdatePolicy.IsTrustedDownload(new Uri("http://api.github.com/a")) && !UpdatePolicy.IsTrustedDownload(new Uri("https://github.com.attacker.invalid/a")), "updater rejects HTTP and lookalike redirect hosts");
            check(!UpdatePolicy.IsTrustedDownload(new Uri("https://token@api.github.com/a")) && !UpdatePolicy.IsTrustedDownload(new Uri("https://api.github.com:444/a")), "updater refuses credential URLs and alternate ports");
            string sha = UpdatePolicy.Sha256(next);
            check(UpdateFiles.Install(target, Assembly, new Version(1, 1, 0), sha, next, CancellationToken.None) == InstallOutcome.Installed, "updater atomically installs a newer verified DLL");
            check(File.ReadAllBytes(target).SequenceEqual(next) && File.ReadAllBytes(target + ".previous").SequenceEqual(old), "updater preserves the exact prior DLL as a backup");
            check(Directory.GetFiles(directory, "*.dll").Length == 1 && !Directory.GetFiles(directory, "*.pending").Any(), "backup and temporary files cannot become duplicate plugins");
            check(UpdateFiles.Install(target, Assembly, new Version(1, 1, 0), sha, next, CancellationToken.None) == InstallOutcome.AlreadyPresent, "updater does not reinstall the same pending version");
            check(UpdateFiles.Install(target, Assembly, new Version(1, 0, 0), UpdatePolicy.Sha256(old), old, CancellationToken.None) == InstallOutcome.AlreadyPresent, "updater never automatically downgrades a plugin");
            Reset();
            check(Rejects(() => UpdateFiles.Install(target, Assembly, new Version(1, 1, 0), new string ('0', 64), next, CancellationToken.None)), "updater refuses a mismatched checksum");
            check(File.ReadAllBytes(target).SequenceEqual(old) && !File.Exists(target + ".previous"), "a failed checksum cannot touch the working DLL");
            byte[] wrong = File.ReadAllBytes(typeof(UpdaterTests).Assembly.Location);
            check(Rejects(() => UpdateFiles.Install(target, Assembly, new Version(1, 1, 0), UpdatePolicy.Sha256(wrong), wrong, CancellationToken.None)), "updater rejects a different assembly with a valid checksum");
            check(Rejects(() => UpdateFiles.Install(target, Assembly, new Version(1, 2, 0), sha, next, CancellationToken.None)), "updater checks the actual DLL version against the manifest");
            byte[] invalid = Encoding.UTF8.GetBytes("not a DLL");
            check(Rejects(() => UpdateFiles.Install(target, Assembly, new Version(1, 1, 0), UpdatePolicy.Sha256(invalid), invalid, CancellationToken.None)), "a correctly hashed non-DLL file cannot be installed");
            check(File.ReadAllBytes(target).SequenceEqual(old) && !Directory.GetFiles(directory, "*.pending").Any(), "failed assembly validation cleans staging and preserves the installed DLL");
            using (var fileLock = new FileStream(target + ".brainless-lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                check(UpdateFiles.Install(target, Assembly, new Version(1, 1, 0), sha, next, CancellationToken.None) == InstallOutcome.Busy, "two server processes cannot replace the same DLL concurrently");
            using (var cancelled = new CancellationTokenSource())
            {
                cancelled.Cancel();
                bool stopped = false;
                try
                {
                    UpdateFiles.Install(target, Assembly, new Version(1, 1, 0), sha, next, cancelled.Token);
                }
                catch (OperationCanceledException)
                {
                    stopped = true;
                }

                check(stopped && File.ReadAllBytes(target).SequenceEqual(old), "disabling an updater cancels before modifying a DLL");
            }

            var entry = Entry(sha);
            check(UpdatePolicy.Validate(entry, "Items", Assembly, new Version(9, 15, 2), new Version(14, 2, 8)) == null, "updater accepts a matching game/framework release");
            check(UpdatePolicy.Validate(entry, "Items", Assembly, new Version(9, 14, 0), new Version(14, 2, 8)) != null, "updater blocks a release requiring newer EXILED");
            check(UpdatePolicy.Validate(entry, "Items", Assembly, new Version(10, 0, 0), new Version(14, 2, 8)) != null, "updater blocks an incompatible EXILED major version");
            check(UpdatePolicy.Validate(entry, "Items", Assembly, new Version(9, 15, 2), new Version(14, 3, 0)) != null, "updater blocks a different game build");
            entry.Asset = "../Other.dll";
            check(Rejects(() => UpdatePolicy.Validate(entry, "Items", Assembly, new Version(9, 15, 2), new Version(14, 2, 8))), "manifest paths cannot redirect installation to another DLL");
            async Task<string> RunClient(FakeGitHub fake, bool prereleases = false, AutoUpdateConfig config = null)
            {
                config ??= new AutoUpdateConfig
                {
                    Token = "unit-test-token",
                    TokenEnvironmentVariable = ""
                };
                config.IncludePrereleases = prereleases;
                using var http = new HttpClient(fake);
                using var client = new ReleaseClient(() => config.ResolveToken(_ => "environment-test-token"), http);
                return await client.CheckAndInstallAsync("Items", Assembly, target, new Version(1, 0, 0), new Version(9, 15, 2), new Version(14, 2, 8), config, CancellationToken.None);
            }

            var good = new FakeGitHub(next);
            string installed = await RunClient(good);
            check(installed.Contains("next full server restart") && File.ReadAllBytes(target).SequenceEqual(next), "release API, manifest, private asset download and install work end to end");
            check(good.Requests.Count(r => r.Uri.Host == "api.github.com") == 3 && good.Requests.Where(r => r.Uri.Host == "api.github.com").All(r => r.Authorization == "Bearer unit-test-token"), "private release and asset requests use the configured read-only token");
            check(good.Requests.Single(r => r.Uri.Host == "release-assets.githubusercontent.com").Authorization == null, "GitHub asset redirects never receive the authorization token");
            Reset();
            var bundle = new FakeGitHub(next)
            {
                CombinedRelease = true
            };
            await RunClient(bundle);
            check(File.ReadAllBytes(target).SequenceEqual(next), "a combined release with all eight plugins and an install ZIP updates this component independently");
            check(bundle.Requests.Count(r => r.Uri.Host == "api.github.com") == 3 && Directory.GetFiles(directory, "*.dll").Length == 1, "a combined release does not download its ZIP, peer manifests or uninstalled plugins");
            Reset();
            var environment = new FakeGitHub(next);
            await RunClient(environment, config: new AutoUpdateConfig { TokenEnvironmentVariable = "TEST_GITHUB_TOKEN" });
            check(File.ReadAllBytes(target).SequenceEqual(next) && environment.Requests.Where(r => r.Uri.Host == "api.github.com").All(r => r.Authorization == "Bearer environment-test-token"), "existing environment-only server setups can still authenticate and install updates end to end");
            check(environment.Requests.Single(r => r.Uri.Host == "release-assets.githubusercontent.com").Authorization == null, "environment credentials also stay off the asset CDN");
            Reset();
            var anonymous = new FakeGitHub(next)
            {
                Status = HttpStatusCode.NotFound
            };
            string missingCredentialMessage = await Failure(() => RunClient(anonymous, config: new AutoUpdateConfig { TokenEnvironmentVariable = "" }));
            check(missingCredentialMessage.Contains("No GitHub token was supplied") && missingCredentialMessage.Contains("auto_update.token") && anonymous.Requests.All(r => r.Authorization == null), "a missing private-repo token explains the YAML fix without sending credentials");
            var denied = new FakeGitHub(next)
            {
                Status = HttpStatusCode.NotFound
            };
            string deniedMessage = await Failure(() => RunClient(denied));
            check(deniedMessage.Contains("Contents: read") && !deniedMessage.Contains("unit-test-token") && !deniedMessage.Contains("No GitHub token was supplied"), "an inaccessible private repo reports token permissions without logging the configured token");
            check(File.ReadAllBytes(target).SequenceEqual(old) && !File.Exists(target + ".previous"), "credential failures leave the installed DLL and backup untouched");
            var mixedOrder = new FakeGitHub(next)
            {
                OldUnrelatedFirst = true
            };
            await RunClient(mixedOrder, config: new AutoUpdateConfig { Token = "unit-test-token", TokenEnvironmentVariable = "", ReleasesToScan = 1 });
            check(File.ReadAllBytes(target).SequenceEqual(next) && mixedOrder.Requests[0].Uri.Query.Contains("per_page=100"), "component discovery fetches a full release page and sorts publication times before applying a short scan limit");
            Reset();
            var draft = new FakeGitHub(next)
            {
                Draft = true
            };
            await RunClient(draft);
            check(File.ReadAllBytes(target).SequenceEqual(old) && draft.Requests.Count == 1, "draft releases are never downloaded");
            var beta = new FakeGitHub(next)
            {
                Prerelease = true
            };
            await RunClient(beta);
            check(File.ReadAllBytes(target).SequenceEqual(old) && beta.Requests.Count == 1, "prereleases are excluded by default");
            await RunClient(new FakeGitHub(next) { Prerelease = true }, true);
            check(File.ReadAllBytes(target).SequenceEqual(next), "prereleases require an explicit config opt-in");
            Reset();
            var different = new FakeGitHub(next)
            {
                PluginId = "Weapons"
            };
            await RunClient(different);
            check(File.ReadAllBytes(target).SequenceEqual(old), "a release for another plugin cannot update this DLL");
            var mismatched = new FakeGitHub(next)
            {
                Checksum = new string ('0', 64)
            };
            check(await RejectsAsync(() => RunClient(mismatched)), "release digest and manifest disagreement aborts the update");
            check(File.ReadAllBytes(target).SequenceEqual(old), "a rejected release leaves the original file intact");
            var hostile = new FakeGitHub(next)
            {
                Redirect = "https://attacker.invalid/plugin"
            };
            check(await RejectsAsync(() => RunClient(hostile)) && !hostile.Requests.Any(r => r.Uri.Host == "attacker.invalid"), "untrusted redirects are blocked before sending any request");
            var missing = new FakeGitHub(next)
            {
                Status = HttpStatusCode.NotFound
            };
            check(await RejectsAsync(() => RunClient(missing)) && File.ReadAllBytes(target).SequenceEqual(old), "private-repo authentication failures leave gameplay files intact");
            var shortDownload = new FakeGitHub(next)
            {
                Truncate = true
            };
            check(await RejectsAsync(() => RunClient(shortDownload)) && File.ReadAllBytes(target).SequenceEqual(old), "truncated release DLL downloads cannot replace a plugin");
            var corrupt = new FakeGitHub(next)
            {
                Corrupt = true
            };
            check(await RejectsAsync(() => RunClient(corrupt)) && File.ReadAllBytes(target).SequenceEqual(old), "same-size corrupted DLL downloads fail checksum validation");
            using (var stop = new CancellationTokenSource())
            using (var http = new HttpClient(new FakeGitHub(next) { WaitForCancellation = true }))
            using (var client = new ReleaseClient(() => null, http))
            {
                stop.CancelAfter(50);
                bool stopped = false;
                try
                {
                    await client.CheckAndInstallAsync("Items", Assembly, target, new Version(1, 0, 0), new Version(9, 15, 2), new Version(14, 2, 8), new AutoUpdateConfig(), stop.Token);
                }
                catch (OperationCanceledException)
                {
                    stopped = true;
                }

                check(stopped && File.ReadAllBytes(target).SequenceEqual(old), "cancelling during an asset request prevents installation");
            }

            var cache = new FakeGitHub(next);
            using (var http = new HttpClient(cache))
            using (var client = new ReleaseClient(() => null, http))
            {
                await client.CheckAndInstallAsync("Items", Assembly, target, new Version(1, 0, 0), new Version(9, 15, 2), new Version(14, 2, 8), new AutoUpdateConfig(), CancellationToken.None);
                int first = cache.Requests.Count;
                await client.CheckAndInstallAsync("Items", Assembly, target, new Version(1, 0, 0), new Version(9, 15, 2), new Version(14, 2, 8), new AutoUpdateConfig(), CancellationToken.None);
                check(cache.Requests.Count == first + 1 && cache.Requests.Last().Conditional, "unchanged release lists use ETags and do not redownload a pending DLL");
            }
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static UpdateEntry Entry(string sha) => new UpdateEntry
    {
        Id = "Items",
        AssemblyName = Assembly,
        Asset = Assembly + ".dll",
        Version = "1.1.0",
        Sha256 = sha,
        ExiledMin = "9.15.2",
        GameVersion = "14.2.8",
        TargetFramework = "net48"
    };
    private static bool Rejects(Action action)
    {
        try
        {
            action();
            return false;
        }
        catch (UpdateException)
        {
            return true;
        }
    }

    private static async Task<bool> RejectsAsync(Func<Task<string>> action)
    {
        try
        {
            await action();
            return false;
        }
        catch (UpdateException)
        {
            return true;
        }
    }

    private static async Task<string> Failure(Func<Task<string>> action)
    {
        try
        {
            await action();
        }
        catch (UpdateException exception)
        {
            return exception.Message;
        }

        throw new Exception("Expected an updater authentication failure.");
    }

    private sealed class FakeGitHub : HttpMessageHandler
    {
        private readonly byte[] dll;
        internal readonly List<(Uri Uri, string Authorization, bool Conditional)> Requests = new();
        internal bool Draft, Prerelease, Truncate, Corrupt, WaitForCancellation, OldUnrelatedFirst, CombinedRelease;
        internal string PluginId = "Items", Checksum, Redirect = "https://release-assets.githubusercontent.com/file";
        internal HttpStatusCode Status = HttpStatusCode.OK;
        internal FakeGitHub(byte[] dll)
        {
            this.dll = dll;
            Checksum = UpdatePolicy.Sha256(dll);
        }

        private byte[] Manifest => JsonSerializer.SerializeToUtf8Bytes(new { schema_version = 1, repository = UpdatePolicy.Repository, plugins = new[] { new { id = PluginId, assembly_name = Assembly, asset = Assembly + ".dll", version = "1.1.0", sha256 = Checksum, exiled_min = "9.15.2", game_version = "14.2.8", target_framework = "net48" } } });

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add((request.RequestUri, request.Headers.Authorization?.ToString(), request.Headers.Contains("If-None-Match")));
            if (WaitForCancellation && request.RequestUri.AbsolutePath.EndsWith("/assets/2"))
                await Task.Delay(Timeout.Infinite, cancellationToken);
            HttpResponseMessage response;
            if (Status != HttpStatusCode.OK)
                response = new HttpResponseMessage(Status);
            else if (request.RequestUri.AbsolutePath.EndsWith("/releases"))
            {
                if (request.Headers.Contains("If-None-Match"))
                    response = new HttpResponseMessage(HttpStatusCode.NotModified);
                else
                {
                    var assets = new List<object>
                    {
                        new
                        {
                            id = 1,
                            name = UpdatePolicy.ManifestName(Assembly),
                            state = "uploaded",
                            size = Manifest.Length,
                            digest = (string)null
                        },
                        new
                        {
                            id = 2,
                            name = Assembly + ".dll",
                            state = "uploaded",
                            size = dll.Length,
                            digest = "sha256:" + UpdatePolicy.Sha256(dll)
                        }
                    };
                    if (CombinedRelease)
                    {
                        assets.Insert(0, new { id = 99, name = "ProjectBrainless-Plugins.zip", state = "uploaded", size = 4096, digest = (string)null });
                        int id = 10;
                        foreach (string peer in new[]
                        {
                            "Brainless.Roles",
                            "Brainless.Weapons",
                            "BetterCoinflips",
                            "Brainless.ScpProximityChat",
                            "Brainless.Core",
                            "Brainless.SlFixes",
                            "Brainless.RoundEvents"
                        }

                        )
                        {
                            assets.Add(new { id = id++, name = peer + ".dll", state = "uploaded", size = 1024, digest = (string)null });
                            assets.Add(new { id = id++, name = peer + ".update.json", state = "uploaded", size = 512, digest = (string)null });
                        }
                    }

                    object release = new
                    {
                        id = 1,
                        tag_name = CombinedRelease ? "pb-test-snapshot" : "items-v1.1.0",
                        draft = Draft,
                        prerelease = Prerelease,
                        published_at = "2026-10-29T12:00:00Z",
                        assets = assets
                    };
                    object[] releases = OldUnrelatedFirst ? new object[]
                    {
                        new
                        {
                            id = 99,
                            tag_name = "older-other-component",
                            draft = false,
                            prerelease = false,
                            published_at = "2020-01-01T00:00:00Z",
                            assets = Array.Empty<object>()
                        },
                        release
                    }

                    : new[]
                    {
                        release
                    };
                    response = Bytes(JsonSerializer.SerializeToUtf8Bytes(releases));
                    response.Headers.ETag = new EntityTagHeaderValue("\"release-list\"");
                }
            }
            else if (request.RequestUri.AbsolutePath.EndsWith("/assets/1"))
                response = Bytes(Manifest);
            else if (request.RequestUri.AbsolutePath.EndsWith("/assets/2"))
            {
                response = new HttpResponseMessage(HttpStatusCode.Found);
                response.Headers.Location = new Uri(Redirect);
            }
            else if (request.RequestUri.Host == "release-assets.githubusercontent.com")
            {
                byte[] bytes = Truncate ? dll.Take(10).ToArray() : dll.ToArray();
                if (Corrupt)
                    bytes[bytes.Length - 1] ^= 1;
                response = Bytes(bytes);
            }
            else
                throw new Exception("Unexpected mock request host/path.");
            return response;
        }

        private static HttpResponseMessage Bytes(byte[] bytes) => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(bytes)
        };
    }
}
