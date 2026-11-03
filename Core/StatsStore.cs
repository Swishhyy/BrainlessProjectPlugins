using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Brainless.Internal.Updates;
using System.Security.Cryptography;

namespace Brainless.Core
{
    [DataContract]
    public sealed class PlayerStats
    {
        [DataMember]
        public string UserId { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public long Xp { get; set; }

        [DataMember]
        public string DiscordUserId { get; set; }

        [DataMember]
        public Dictionary<string, long> Actions { get; set; } = new Dictionary<string, long>();

        [DataMember]
        public long PlaytimeSeconds { get; set; }

        public int Level(int step) => (int)Math.Min(int.MaxValue, Math.Floor(Math.Sqrt(Xp / (double)Math.Max(1, step))));
        internal PlayerStats Copy() => new PlayerStats
        {
            UserId = UserId,
            Name = Name,
            Xp = Xp,
            DiscordUserId = DiscordUserId,
            PlaytimeSeconds = PlaytimeSeconds,
            Actions = new Dictionary<string, long>(Actions)
        };
    }

    [DataContract]
    internal sealed class StatsDatabase
    {
        [DataMember]
        public int Schema { get; set; } = 2;

        [DataMember]
        public Dictionary<string, PlayerStats> Players { get; set; } = new Dictionary<string, PlayerStats>();
    }

    // No game objects are stored here. Gateway, persistence and gameplay calls
    // share this lock; disk serialization happens on immutable copies.
    public sealed partial class StatsStore
    {
        private readonly object gate = new object ();
        private readonly object saveGate = new object ();
        private readonly string path;
        private readonly int maxRoundXp;
        private StatsDatabase database;
        private readonly HashSet<string> receipts = new HashSet<string>();
        private readonly Dictionary<string, int> earned = new Dictionary<string, int>();
        private readonly Dictionary<string, LinkCode> codes = new Dictionary<string, LinkCode>();
        private readonly Dictionary<string, DateTime> linkIssued = new Dictionary<string, DateTime>();
        public bool RecoveredBackup { get; private set; }

        public StatsStore(string path, int maxRoundXp = 500)
        {
            this.path = Path.GetFullPath(path);
            this.maxRoundXp = Math.Max(0, maxRoundXp);
            Directory.CreateDirectory(Path.GetDirectoryName(this.path));
            LoadPrivacy();
            database = Load(this.path);
            foreach (var id in database.Players.Keys.Where(id => !CanTrack(id)).ToArray())
            {
                database.Players.Remove(id);
                purgeBackups = true;
            }

            if (purgeBackups)
                Save();
        }

        private static StatsDatabase Read(string file)
        {
            using (var stream = File.OpenRead(file))
            {
                using (var buffer = new MemoryStream())
                {
                    stream.CopyTo(buffer);
                    var value = JsonContract.Read<StatsDatabase>(buffer.ToArray());
                    if (value == null || (value.Schema != 1 && value.Schema != 2) || value.Players == null || value.Players.Any(p => p.Value == null || p.Key != p.Value.UserId || p.Value.Actions == null || p.Value.Xp < 0 || p.Value.PlaytimeSeconds < 0))
                        throw new SerializationException("Invalid XP data schema.");
                    return value;
                }
            }
        }

        private StatsDatabase Load(string file)
        {
            if (!File.Exists(file) && !File.Exists(file + ".backup"))
                return new StatsDatabase();
            try
            {
                return Read(file);
            }
            catch (Exception failure)when (failure is IOException || failure is SerializationException || failure is System.Xml.XmlException || failure is FormatException || failure is System.Text.DecoderFallbackException)
            {
                if (!File.Exists(file + ".backup"))
                    throw new InvalidDataException("XP data could not be read. The existing file was preserved.", failure);
                var recovered = Read(file + ".backup");
                if (File.Exists(file))
                    File.Move(file, file + ".corrupt-" + Guid.NewGuid().ToString("N"));
                File.Copy(file + ".backup", file);
                RecoveredBackup = true;
                return recovered;
            }
        }

        public void BeginRound()
        {
            lock (gate)
            {
                receipts.Clear();
                earned.Clear();
            }
        }

        public bool Record(string userId, string name, string action, int xp, string eventId)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(action) || action.Length > 64 || string.IsNullOrWhiteSpace(eventId) || eventId.Length > 256 || xp < 0 || xp > 10000)
                return false;
            lock (gate)
            {
                if (!CanTrackLocked(userId))
                    return false;
                string receipt = userId + "\n" + action + "\n" + eventId;
                if (receipts.Count >= 100000 || !receipts.Add(receipt))
                    return false;
                if (!database.Players.TryGetValue(userId, out var stats))
                    database.Players[userId] = stats = new PlayerStats
                    {
                        UserId = userId
                    };
                stats.Name = string.IsNullOrWhiteSpace(name) ? "Player" : name.Substring(0, Math.Min(64, name.Length));
                stats.Actions.TryGetValue(action, out long count);
                stats.Actions[action] = count == long.MaxValue ? count : count + 1;
                earned.TryGetValue(userId, out int before);
                int amount = Math.Min(xp, Math.Max(0, maxRoundXp - before));
                stats.Xp = Math.Min(long.MaxValue - amount, stats.Xp) + amount;
                earned[userId] = before + amount;
                return true;
            }
        }

        public PlayerStats Get(string userId)
        {
            lock (gate)
                return CanTrackLocked(userId) && database.Players.TryGetValue(userId, out var stats) ? stats.Copy() : null;
        }

        public PlayerStats FindDiscord(string discordId)
        {
            lock (gate)
                return string.IsNullOrWhiteSpace(discordId) ? null : database.Players.Values.FirstOrDefault(p => p.DiscordUserId == discordId && CanTrackLocked(p.UserId))?.Copy();
        }

        public PlayerStats[] Top(int count = 10) => Top(count, false);

        // Leaderboard by XP (default) or by playtime. Opted-out players never appear.
        public PlayerStats[] Top(int count, bool byPlaytime)
        {
            lock (gate)
            {
                var tracked = database.Players.Values.Where(p => CanTrackLocked(p.UserId));
                var ordered = byPlaytime ? tracked.OrderByDescending(p => p.PlaytimeSeconds).ThenByDescending(p => p.Xp) : tracked.OrderByDescending(p => p.Xp).ThenByDescending(p => p.PlaytimeSeconds);
                return ordered.ThenBy(p => p.Name, StringComparer.Ordinal).Take(Math.Max(1, Math.Min(100, count))).Select(p => p.Copy()).ToArray();
            }
        }

        public string CreateLinkCode(string userId, string name, DateTime now)
        {
            lock (gate)
            {
                if (!CanTrackLocked(userId))
                    return null;
                if (linkIssued.TryGetValue(userId, out var last) && now - last < TimeSpan.FromSeconds(30))
                    return null;
                foreach (var key in codes.Where(p => p.Value.Expires <= now || p.Value.UserId == userId).Select(p => p.Key).ToArray())
                    codes.Remove(key);
                if (!database.Players.TryGetValue(userId, out var stats))
                    database.Players[userId] = stats = new PlayerStats
                    {
                        UserId = userId,
                        Name = name
                    };
                string code;
                using (var random = RandomNumberGenerator.Create())
                {
                    var bytes = new byte[8];
                    const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
                    do
                    {
                        random.GetBytes(bytes);
                        code = new string (bytes.Select(b => alphabet[b % alphabet.Length]).ToArray());
                    }
                    while (codes.ContainsKey(code));
                }

                codes[code] = new LinkCode
                {
                    UserId = userId,
                    Expires = now.AddMinutes(10)
                };
                linkIssued[userId] = now;
                return code;
            }
        }

        public bool Link(string discordId, string code, DateTime now)
        {
            if (string.IsNullOrWhiteSpace(discordId) || string.IsNullOrWhiteSpace(code))
                return false;
            lock (gate)
            {
                string key = code.Trim().ToUpperInvariant();
                if (!codes.TryGetValue(key, out var link) || link.Expires <= now || !CanTrackLocked(link.UserId) || !database.Players.ContainsKey(link.UserId))
                    return false;
                codes.Remove(key);
                foreach (var old in database.Players.Values.Where(p => p.DiscordUserId == discordId))
                    old.DiscordUserId = null;
                database.Players[link.UserId].DiscordUserId = discordId;
                return true;
            }
        }

        public void Save()
        {
            lock (saveGate)
            {
                StatsDatabase snapshot;
                byte[] privacySnapshot;
                bool scrub;
                lock (gate)
                {
                    snapshot = new StatsDatabase
                    {
                        Players = database.Players.Where(p => CanTrackLocked(p.Key)).ToDictionary(p => p.Key, p => p.Value.Copy())
                    };
                    privacySnapshot = privacyDirty ? JsonContract.Write(new PrivacyPreferences { Key = Convert.ToBase64String(privacyKey), Manual = manualSuppression.ToList(), Dnt = dntSuppression.ToList() }) : null;
                    scrub = purgeBackups;
                }

                // Persist suppression before writing stats, so even recovery of
                // an older backup cannot expose or resume an opted-out profile.
                if (privacySnapshot != null)
                {
                    WriteAtomic(path + ".privacy", privacySnapshot);
                    File.Copy(path + ".privacy", path + ".privacy.backup", true);
                    lock (gate)
                        privacyDirty = false;
                }

                WriteAtomic(path, JsonContract.Write(snapshot));
                if (scrub)
                {
                    File.Copy(path, path + ".backup", true);
                    foreach (string archive in Directory.GetFiles(Path.GetDirectoryName(path), Path.GetFileName(path) + ".corrupt-*"))
                        File.Delete(archive);
                    lock (gate)
                        purgeBackups = false;
                }
            }
        }

        private static void WriteAtomic(string destination, byte[] data)
        {
            string temporary = destination + ".pending";
            try
            {
                using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    stream.Write(data, 0, data.Length);
                    stream.Flush(true);
                }

                if (File.Exists(destination))
                    File.Replace(temporary, destination, destination + ".backup");
                else
                    File.Move(temporary, destination);
            }
            finally
            {
                if (File.Exists(temporary))
                    File.Delete(temporary);
            }
        }

        private sealed class LinkCode
        {
            internal string UserId;
            internal DateTime Expires;
        }
    }
}
