using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Brainless.Core;

internal static class StatsTests
{
    internal static void Run(Action<bool, string> check)
    {
        string directory = Path.Combine(Path.GetTempPath(), "brainless-stats-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string path = Path.Combine(directory, "stats.json");
            var store = new StatsStore(path, 50);
            check(store.Record("one", "One", "kill_human", 40, "victim-life-1"), "XP accepts a confirmed action with an event receipt");
            check(!store.Record("one", "One", "kill_human", 40, "victim-life-1") && store.Get("one").Xp == 40 && store.Get("one").Actions["kill_human"] == 1, "duplicate kill callbacks cannot award duplicate XP or inflate stats");
            store.Record("one", "One", "kill_scp", 50, "victim-life-2");
            check(store.Get("one").Xp == 50 && store.Get("one").Actions["kill_scp"] == 1, "the per-player round XP ceiling still records actions when rewards are capped");
            store.Record("two", "Two", "escape", 35, "escape-life-1");
            check(store.Get("two").Xp == 35, "different players have independent round reward budgets");
            store.Record("one", "One", "death", 0, "victim-life-1");
            check(store.Get("one").Actions["death"] == 1 && store.Get("one").Xp == 50, "zero-XP events remain usable as persistent stat counters");
            check(!store.Record("one", "One", "kill_human", -1, "bad") && !store.Record("one", "One", "kill_human", 10001, "bad") && !store.Record("", "One", "kill_human", 10, "bad"), "invalid XP and anonymous action records cannot alter statistics");
            var copy = store.Get("one");
            copy.Xp = 999999;
            copy.Actions.Clear();
            check(store.Get("one").Xp == 50 && store.Get("one").Actions.Count == 3, "API callers receive snapshots and cannot mutate persisted XP through a returned object");
            store.BeginRound();
            store.Record("one", "One", "craft", 10, "round-2-item");
            check(store.Get("one").Xp == 60 && store.Get("one").Actions["kill_human"] == 1, "round reset clears only receipts/reward limits, retaining lifetime XP and counters");
            check(store.Top()[0].UserId == "one" && store.Get(null) == null && store.FindDiscord(null) == null && store.FindDiscord("") == null, "leaderboard ordering and unlinked Discord lookups do not expose another unlinked account");
            var now = new DateTime(2026, 10, 31, 0, 0, 0, DateTimeKind.Utc);
            string first = store.CreateLinkCode("one", "One", now);
            check(first.Length == 8 && store.CreateLinkCode("one", "One", now.AddSeconds(1)) == null, "account linking uses short-lived private codes with a request cooldown");
            string second = store.CreateLinkCode("one", "One", now.AddSeconds(31));
            check(!store.Link("discord-one", first, now.AddSeconds(32)) && store.Link("discord-one", second.ToLowerInvariant(), now.AddSeconds(32)), "requesting another link code invalidates the old one and current codes are case-insensitive");
            check(!store.Link("discord-attacker", second, now.AddSeconds(33)) && store.FindDiscord("discord-one").UserId == "one", "a link code is consumed once and cannot be replayed to take over a linked account");
            string expired = store.CreateLinkCode("two", "Two", now);
            check(!store.Link("discord-two", expired, now.AddMinutes(10)), "link codes expire at the ten-minute boundary");
            store.Save();
            var loaded = new StatsStore(path, 50);
            check(loaded.Get("one").Xp == 60 && loaded.Get("one").Actions["kill_scp"] == 1 && loaded.FindDiscord("discord-one").UserId == "one", "XP, action counters and verified Discord links survive a disk roundtrip");
            check(!loaded.Link("discord-two", expired, now.AddMinutes(1)), "pending link secrets are not persisted across a server restart");
            loaded.Record("one", "One", "craft", 10, "new-process");
            loaded.Save();
            File.WriteAllText(path, "broken data");
            var recovered = new StatsStore(path, 50);
            check(recovered.RecoveredBackup && recovered.Get("one").Xp == 60 && Directory.GetFiles(directory, "stats.json.corrupt-*").Length == 1, "a damaged main stats file restores its known backup and preserves the damaged original");
            File.WriteAllText(path, "bad main");
            File.WriteAllText(path + ".backup", "bad backup");
            bool refused = false;
            try
            {
                _ = new StatsStore(path);
            }
            catch (Exception)
            {
                refused = true;
            }

            check(refused && File.ReadAllText(path) == "bad main" && File.ReadAllText(path + ".backup") == "bad backup", "unreadable stats and backup fail closed without silently overwriting player data");
            var parallel = new StatsStore(Path.Combine(directory, "parallel.json"), 10000);
            Parallel.For(0, 200, i => parallel.Record("shared", "Shared", "action", 1, "action-" + i));
            parallel.Save();
            check(parallel.Get("shared").Xp == 200 && parallel.Get("shared").Actions["action"] == 200 && new StatsStore(Path.Combine(directory, "parallel.json")).Get("shared").Xp == 200, "concurrent gameplay/bot consumers and persistence retain every distinct action");
            check(new PlayerStats { Xp = 99 }.Level(100) == 0 && new PlayerStats { Xp = 100 }.Level(100) == 1 && new PlayerStats { Xp = 400 }.Level(100) == 2, "level thresholds grow quadratically and remain separate from combat stats");
            check(!Brainless.Internal.CoreBridge.IsReady && !Brainless.Internal.CoreBridge.Record("one", "One", "action", 1, "event"), "missing Core integration is a harmless no-op for standalone gameplay plugins");
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }
}
