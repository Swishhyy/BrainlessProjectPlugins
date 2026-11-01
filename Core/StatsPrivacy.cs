using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using Brainless.Internal.Updates;

namespace Brainless.Core
{
    [DataContract]
    internal sealed class PrivacyPreferences
    {
        [DataMember]
        public string Key { get; set; }

        [DataMember]
        public List<string> Manual { get; set; } = new List<string>();

        [DataMember]
        public List<string> Dnt { get; set; } = new List<string>();
    }

    public sealed partial class StatsStore
    {
        private byte[] privacyKey;
        private HashSet<string> manualSuppression, dntSuppression;
        private bool privacyDirty, purgeBackups;
        private long privacyRevision;
        public long PrivacyRevision
        {
            get
            {
                lock (gate)
                    return privacyRevision;
            }
        }

        private void LoadPrivacy()
        {
            string file = path + ".privacy";
            PrivacyPreferences value = null;
            if (File.Exists(file) || File.Exists(file + ".backup"))
            {
                try
                {
                    value = JsonContract.Read<PrivacyPreferences>(File.ReadAllBytes(file));
                }
                catch (Exception failure)when (failure is IOException || failure is FormatException || failure is SerializationException)
                {
                    if (!File.Exists(file + ".backup"))
                        throw new InvalidDataException("Privacy preferences unavailable; tracking remains disabled.");
                    value = JsonContract.Read<PrivacyPreferences>(File.ReadAllBytes(file + ".backup"));
                    privacyDirty = true;
                }

                if (value?.Key == null || value.Manual == null || value.Dnt == null)
                    throw new InvalidDataException("Invalid privacy preferences.");
                privacyKey = Convert.FromBase64String(value.Key);
                if (privacyKey.Length != 32 || value.Manual.Concat(value.Dnt).Any(h => h == null || h.Length != 64))
                    throw new InvalidDataException("Invalid privacy markers.");
            }
            else
            {
                privacyKey = new byte[32];
                using (var random = RandomNumberGenerator.Create())
                    random.GetBytes(privacyKey);
                privacyDirty = true;
            }

            manualSuppression = new HashSet<string>(value?.Manual ?? new List<string>(), StringComparer.Ordinal);
            dntSuppression = new HashSet<string>(value?.Dnt ?? new List<string>(), StringComparer.Ordinal);
        }

        private string Marker(string userId)
        {
            using (var hash = new HMACSHA256(privacyKey))
                return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(userId))).Replace("-", "").ToLowerInvariant();
        }

        private bool CanTrackLocked(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return false;
            string hash = Marker(userId);
            return !manualSuppression.Contains(hash) && !dntSuppression.Contains(hash);
        }

        public bool CanTrack(string userId)
        {
            lock (gate)
                return CanTrackLocked(userId);
        }

        public bool IsOptedOut(string userId)
        {
            lock (gate)
                return !string.IsNullOrWhiteSpace(userId) && manualSuppression.Contains(Marker(userId));
        }

        public bool SetDoNotTrack(string userId, bool enabled) => ChangePrivacy(userId, enabled, false);
        public bool OptOut(string userId) => ChangePrivacy(userId, true, true);
        public bool OptIn(string userId) => ChangePrivacy(userId, false, true);
        private bool ChangePrivacy(string userId, bool enabled, bool manual)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return false;
            lock (saveGate)
            {
                bool changed;
                lock (gate)
                {
                    var preferences = manual ? manualSuppression : dntSuppression;
                    string marker = Marker(userId);
                    changed = enabled ? preferences.Add(marker) : preferences.Remove(marker);
                    if (changed)
                        privacyRevision++;
                    if (enabled)
                    {
                        bool removed = database.Players.Remove(userId);
                        earned.Remove(userId);
                        linkIssued.Remove(userId);
                        foreach (var code in codes.Where(c => c.Value.UserId == userId).Select(c => c.Key).ToArray())
                            codes.Remove(code);
                        receipts.RemoveWhere(r => r.StartsWith(userId + "\n", StringComparison.Ordinal));
                        purgeBackups |= changed || removed;
                    }

                    privacyDirty |= changed;
                }

                if (changed || purgeBackups || privacyDirty)
                    Save();
                return true;
            }
        }

        public bool OptOutDiscord(string discordId)
        {
            if (string.IsNullOrWhiteSpace(discordId))
                return false;
            lock (saveGate)
            {
                lock (gate)
                {
                    string userId = database.Players.Values.FirstOrDefault(p => p.DiscordUserId == discordId)?.UserId;
                    return userId != null && OptOut(userId);
                }
            }
        }

        public bool AddPlaytime(string userId, string name, long seconds)
        {
            if (seconds <= 0 || seconds > 86400)
                return false;
            lock (gate)
            {
                if (!CanTrackLocked(userId))
                    return false;
                if (!database.Players.TryGetValue(userId, out var stats))
                    database.Players[userId] = stats = new PlayerStats
                    {
                        UserId = userId
                    };
                stats.Name = string.IsNullOrWhiteSpace(name) ? "Player" : name.Substring(0, Math.Min(64, name.Length));
                stats.PlaytimeSeconds = Math.Min(long.MaxValue - seconds, stats.PlaytimeSeconds) + seconds;
                return true;
            }
        }
    }
}
