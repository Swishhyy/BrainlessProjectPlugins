using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Brainless.Internal.Updates
{
    internal sealed class ReleaseClient : IDisposable
    {
        private readonly HttpClient http;
        private readonly bool ownsHttp;
        private readonly Func<string> getToken;
        private readonly Dictionary<long, UpdateManifest> manifests = new Dictionary<long, UpdateManifest>();
        private GithubRelease[] cachedReleases;
        private string releasesEtag;
        private int cachedScan;
        internal ReleaseClient(Func<string> getToken, HttpClient http = null)
        {
            this.getToken = getToken;
            ownsHttp = http == null;
            this.http = http ?? new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false, AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate });
        }

        internal async Task<string> CheckAndInstallAsync(string id, string assemblyName, string targetPath, Version runningVersion, Version exiled, Version game, AutoUpdateConfig config, CancellationToken cancellation)
        {
            int scan = Math.Max(1, Math.Min(30, config.ReleasesToScan));
            GithubRelease[] releases = await GetReleasesAsync(scan, cancellation).ConfigureAwait(false);
            string incompatible = null;
            foreach (GithubRelease release in releases.Where(r => r != null && !r.Draft && (config.IncludePrereleases || !r.Prerelease)).OrderByDescending(r => Published(r.PublishedAt)))
            {
                cancellation.ThrowIfCancellationRequested();
                if (release.Assets == null)
                    continue;
                GithubAsset dll = FindAsset(release, assemblyName + ".dll");
                GithubAsset manifestAsset = FindAsset(release, UpdatePolicy.ManifestName(assemblyName));
                if (dll == null || manifestAsset == null)
                    continue;
                if (!manifests.TryGetValue(manifestAsset.Id, out var manifest))
                {
                    byte[] json = await DownloadAssetAsync(manifestAsset, 256 * 1024, cancellation).ConfigureAwait(false);
                    manifest = UpdatePolicy.ReadJson<UpdateManifest>(json);
                    if (manifest == null || manifest.SchemaVersion != 1 || !string.Equals(manifest.Repository, UpdatePolicy.Repository, StringComparison.OrdinalIgnoreCase) || manifest.Plugins == null)
                        throw new UpdateException("Release manifest has an unsupported schema or repository.");
                    if (manifests.Count >= 30)
                        manifests.Clear();
                    manifests[manifestAsset.Id] = manifest;
                }

                var entries = manifest.Plugins.Where(p => p != null && p.Id == id).ToArray();
                if (entries.Length == 0)
                    continue;
                if (entries.Length != 1)
                    throw new UpdateException("Release manifest contains duplicate entries for this plugin.");
                UpdateEntry entry = entries[0];
                incompatible = UpdatePolicy.Validate(entry, id, assemblyName, exiled, game);
                if (incompatible != null)
                    continue;
                Version version = UpdatePolicy.ParseVersion(entry.Version);
                if (version <= UpdatePolicy.Normalize(runningVersion))
                    return "No newer compatible release for this plugin.";
                if (!string.IsNullOrEmpty(dll.Digest) && !string.Equals(dll.Digest, "sha256:" + entry.Sha256, StringComparison.OrdinalIgnoreCase))
                    throw new UpdateException("Release DLL digest disagrees with the manifest; refusing the update.");
                // Avoid repeatedly downloading the same pending update while the old assembly runs.
                if (File.Exists(targetPath))
                {
                    var disk = System.Reflection.AssemblyName.GetAssemblyName(targetPath);
                    if (disk.Name != assemblyName)
                        throw new UpdateException("The installed DLL changed identity; refusing the update.");
                    if (UpdatePolicy.Normalize(disk.Version) >= version)
                        return "Version " + disk.Version.ToString(3) + " is on disk; it takes effect on the next full server restart.";
                }

                byte[] bytes = await DownloadAssetAsync(dll, UpdatePolicy.MaxDllBytes, cancellation).ConfigureAwait(false);
                InstallOutcome outcome = UpdateFiles.Install(targetPath, assemblyName, version, entry.Sha256, bytes, cancellation);
                if (outcome == InstallOutcome.Busy)
                    return "Another server process is updating this DLL; this plugin will retry later.";
                return outcome == InstallOutcome.Installed ? "Downloaded and verified v" + version.ToString(3) + ". Previous DLL saved as .previous. The update takes effect on the next full server restart." : "A newer DLL is already on disk and will load on the next full server restart.";
            }

            return incompatible ?? "No matching published release assets found yet; this plugin stays installed.";
        }

        private async Task<GithubRelease[]> GetReleasesAsync(int scan, CancellationToken cancellation)
        {
            string etag = cachedScan == scan ? releasesEtag : null;
            // Fetch a full page before sorting component tags by publication.
            var response = await GetBytesAsync(new Uri("https://api.github.com/repos/" + UpdatePolicy.Repository + "/releases?per_page=100"), 2 * 1024 * 1024, "application/vnd.github+json", etag, cancellation).ConfigureAwait(false);
            if (response.NotModified)
            {
                if (cachedReleases == null)
                    throw new UpdateException("GitHub returned an empty cached release response.");
                return cachedReleases;
            }

            cachedReleases = (UpdatePolicy.ReadJson<GithubRelease[]>(response.Data) ?? new GithubRelease[0]).Where(r => r != null).OrderByDescending(r => Published(r.PublishedAt)).Take(scan).ToArray();
            releasesEtag = response.Etag;
            cachedScan = scan;
            return cachedReleases;
        }

        private static GithubAsset FindAsset(GithubRelease release, string name)
        {
            var matches = release.Assets.Where(a => a != null && a.Name == name && a.State == "uploaded").ToArray();
            if (matches.Length > 1)
                throw new UpdateException("Release contains ambiguous asset names.");
            return matches.SingleOrDefault();
        }

        private async Task<byte[]> DownloadAssetAsync(GithubAsset asset, int limit, CancellationToken cancellation)
        {
            if (asset.Id <= 0 || asset.Size <= 0 || asset.Size > limit)
                throw new UpdateException("Release asset has an invalid or excessive size.");
            var response = await GetBytesAsync(new Uri("https://api.github.com/repos/" + UpdatePolicy.Repository + "/releases/assets/" + asset.Id), limit, "application/octet-stream", null, cancellation).ConfigureAwait(false);
            if (response.Data.LongLength != asset.Size)
                throw new UpdateException("Release asset download is incomplete or its size changed.");
            return response.Data;
        }

        private async Task<Download> GetBytesAsync(Uri uri, int limit, string accept, string etag, CancellationToken cancellation)
        {
            using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation))
            {
                timeout.CancelAfter(TimeSpan.FromSeconds(45));
                for (int redirects = 0; redirects < 5; redirects++)
                {
                    if (!UpdatePolicy.IsTrustedDownload(uri))
                        throw new UpdateException("Release download redirected outside trusted HTTPS GitHub hosts.");
                    using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
                    {
                        request.Headers.UserAgent.ParseAdd("BrainlessProjectPlugins-Updater/1.0");
                        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(accept));
                        bool api = uri.Host.Equals("api.github.com", StringComparison.OrdinalIgnoreCase) && uri.AbsolutePath.StartsWith("/repos/" + UpdatePolicy.Repository + "/", StringComparison.OrdinalIgnoreCase);
                        if (api)
                        {
                            request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
                            string token = getToken?.Invoke();
                            if (!string.IsNullOrWhiteSpace(token))
                                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Trim());
                            if (!string.IsNullOrEmpty(etag))
                                request.Headers.TryAddWithoutValidation("If-None-Match", etag);
                        }

                        using (var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false))
                        {
                            int status = (int)response.StatusCode;
                            if (status == 304)
                                return new Download
                                {
                                    NotModified = true
                                };
                            if (status == 301 || status == 302 || status == 303 || status == 307 || status == 308)
                            {
                                if (response.Headers.Location == null)
                                    throw new UpdateException("GitHub returned a redirect with no destination.");
                                uri = new Uri(uri, response.Headers.Location);
                                etag = null;
                                continue;
                            }

                            if (!response.IsSuccessStatusCode)
                            {
                                string help = "";
                                if (api && (status == 401 || status == 403 || status == 404))
                                    help = request.Headers.Authorization == null ? " No GitHub token was supplied. Set auto_update.token in this plugin's YAML, or use the optional token_environment_variable." : " Check that the configured GitHub token is valid and has Contents: read access to Swishhyy/BrainlessProjectPlugins; also check release availability and GitHub rate limits.";
                                throw new UpdateException("GitHub release request returned HTTP " + status + "." + help);
                            }

                            if (response.Content.Headers.ContentLength > limit)
                                throw new UpdateException("GitHub response exceeds the updater's size limit.");
                            using (var input = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                            using (var output = new MemoryStream())
                            {
                                byte[] buffer = new byte[81920];
                                int read;
                                while ((read = await input.ReadAsync(buffer, 0, buffer.Length, timeout.Token).ConfigureAwait(false)) > 0)
                                {
                                    if (output.Length + read > limit)
                                        throw new UpdateException("GitHub response exceeds the updater's size limit.");
                                    output.Write(buffer, 0, read);
                                }

                                return new Download
                                {
                                    Data = output.ToArray(),
                                    Etag = response.Headers.ETag?.ToString()
                                };
                            }
                        }
                    }
                }
            }

            throw new UpdateException("GitHub release download exceeded the redirect limit.");
        }

        private static DateTimeOffset Published(string value) => DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date) ? date : DateTimeOffset.MinValue;
        public void Dispose()
        {
            if (ownsHttp)
                http.Dispose();
        }

        private sealed class Download
        {
            internal byte[] Data;
            internal string Etag;
            internal bool NotModified;
        }
    }
}
