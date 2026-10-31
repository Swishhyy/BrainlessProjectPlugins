using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Brainless.Internal.Updates
{
    public sealed class AutoUpdateConfig
    {
        public bool Enabled { get; set; } = true;
        public int CheckIntervalMinutes { get; set; } = 60;
        public bool IncludePrereleases { get; set; }

        [Description("Paste a read-only GitHub token here for private releases. Takes precedence over token_environment_variable; leave empty to use the optional environment variable.")]
        public string Token { get; set; } = string.Empty;

        [Description("Optional fallback environment variable NAME, used only when token is empty. The token itself can be pasted into token above.")]
        public string TokenEnvironmentVariable { get; set; } = "BRAINLESS_GITHUB_TOKEN";
        public int ReleasesToScan { get; set; } = 30;

        internal string ResolveToken(Func<string, string> readEnvironment)
        {
            if (!string.IsNullOrWhiteSpace(Token))
                return Token.Trim();
            if (string.IsNullOrWhiteSpace(TokenEnvironmentVariable))
                return null;
            string value = readEnvironment(TokenEnvironmentVariable.Trim());
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }

    [DataContract]
    internal sealed class GithubRelease
    {
        [DataMember(Name = "id")]
        public long Id { get; set; }

        [DataMember(Name = "tag_name")]
        public string Tag { get; set; }

        [DataMember(Name = "draft")]
        public bool Draft { get; set; }

        [DataMember(Name = "prerelease")]
        public bool Prerelease { get; set; }

        [DataMember(Name = "published_at")]
        public string PublishedAt { get; set; }

        [DataMember(Name = "assets")]
        public List<GithubAsset> Assets { get; set; }
    }

    [DataContract]
    internal sealed class GithubAsset
    {
        [DataMember(Name = "id")]
        public long Id { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "state")]
        public string State { get; set; }

        [DataMember(Name = "size")]
        public long Size { get; set; }

        [DataMember(Name = "digest")]
        public string Digest { get; set; }
    }

    [DataContract]
    internal sealed class UpdateManifest
    {
        [DataMember(Name = "schema_version")]
        public int SchemaVersion { get; set; }

        [DataMember(Name = "repository")]
        public string Repository { get; set; }

        [DataMember(Name = "plugins")]
        public List<UpdateEntry> Plugins { get; set; }
    }

    [DataContract]
    internal sealed class UpdateEntry
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }

        [DataMember(Name = "assembly_name")]
        public string AssemblyName { get; set; }

        [DataMember(Name = "asset")]
        public string Asset { get; set; }

        [DataMember(Name = "version")]
        public string Version { get; set; }

        [DataMember(Name = "sha256")]
        public string Sha256 { get; set; }

        [DataMember(Name = "exiled_min")]
        public string ExiledMin { get; set; }

        [DataMember(Name = "game_version")]
        public string GameVersion { get; set; }

        [DataMember(Name = "target_framework")]
        public string TargetFramework { get; set; }
    }

    internal sealed class UpdateException : Exception
    {
        internal UpdateException(string message) : base(message)
        {
        }
    }

    internal static class UpdatePolicy
    {
        internal const string Repository = "Swishhyy/BrainlessProjectPlugins";
        internal static string ManifestName(string assemblyName) => assemblyName + ".update.json";
        internal const int MaxDllBytes = 16 * 1024 * 1024;
        internal static T ReadJson<T>(byte[] data)
        {
            try
            {
                using (var stream = new MemoryStream(data))
                    return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(stream);
            }
            catch (Exception exception)when (exception is SerializationException || exception is System.Xml.XmlException || exception is InvalidCastException)
            {
                throw new UpdateException("Release metadata is not valid JSON for the updater schema.");
            }
        }

        internal static Version ParseVersion(string value)
        {
            if (value == null || !Regex.IsMatch(value, @"^\d+\.\d+\.\d+(\.\d+)?$") || !System.Version.TryParse(value, out var version))
                throw new UpdateException("Release metadata contains an invalid stable version.");
            return Normalize(version);
        }

        internal static Version Normalize(Version version) => new Version(version.Major, version.Minor, Math.Max(0, version.Build), Math.Max(0, version.Revision));
        internal static string Validate(UpdateEntry entry, string id, string assemblyName, Version exiled, Version game)
        {
            if (entry == null || entry.Id != id || entry.AssemblyName != assemblyName || entry.Asset != assemblyName + ".dll")
                throw new UpdateException("Release entry does not match this plugin's identity and asset name.");
            ParseVersion(entry.Version);
            if (entry.Sha256 == null || !Regex.IsMatch(entry.Sha256, "^[a-fA-F0-9]{64}$"))
                throw new UpdateException("Release entry has no valid SHA-256 checksum.");
            if (entry.TargetFramework != "net48")
                return "The release targets a different runtime.";
            Version minimum = ParseVersion(entry.ExiledMin);
            if (exiled.Major != minimum.Major || Normalize(exiled) < minimum)
                return "The release needs a different or newer EXILED version.";
            if (Normalize(game) != ParseVersion(entry.GameVersion))
                return "The release targets a different SCP: SL version.";
            return null;
        }

        internal static bool IsTrustedDownload(Uri uri) => uri != null && uri.Scheme == Uri.UriSchemeHttps && uri.IsDefaultPort && string.IsNullOrEmpty(uri.UserInfo) && (uri.Host.Equals("api.github.com", StringComparison.OrdinalIgnoreCase) || uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) || uri.Host.EndsWith(".githubusercontent.com", StringComparison.OrdinalIgnoreCase));
        internal static string Sha256(byte[] data)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(data)).Replace("-", "").ToLowerInvariant();
        }
    }
}
