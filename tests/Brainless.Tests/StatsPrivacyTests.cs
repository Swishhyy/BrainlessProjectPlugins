using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Brainless.Core;

internal static class StatsPrivacyTests
{
    internal static void Run(Action<bool, string> check)
    {
        string folder = Path.Combine(Path.GetTempPath(), "brainless-privacy-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        const string hidden = "76561198000009999@steam", discord = "111111111111119999", name = "Private_Player_Name";
        try
        {
            string path = Path.Combine(folder, "stats.json");
            var store = new StatsStore(path);
            store.Record(hidden, name, "escape", 35, "old-event");
            check(store.AddPlaytime(hidden, name, 3661) && store.Get(hidden).PlaytimeSeconds == 3661 && !store.AddPlaytime(hidden, name, -1), "server playtime accumulates without awarding XP or accepting negative time");
            var now = DateTime.UtcNow;
            string code = store.CreateLinkCode(hidden, name, now);
            store.Link(discord, code, now);
            store.Record("other@steam", "Other", "escape", 20, "other");
            store.Save();
            store.Save();
            File.WriteAllText(path + ".corrupt-fixture", "old private archive " + hidden + name);
            string pendingCode = store.CreateLinkCode(hidden, name, now.AddSeconds(31));
            check(store.OptOutDiscord(discord), "the Discord account owner can opt their linked game account out");
            check(!store.CanTrack(hidden) && store.Get(hidden) == null && store.FindDiscord(discord) == null && store.Top().All(p => p.UserId != hidden), "opt-out hides the player from every stats/Discord/leaderboard read");
            check(!store.Record(hidden, name, "escape", 35, "new") && !store.AddPlaytime(hidden, name, 15) && store.CreateLinkCode(hidden, name, now.AddMinutes(1)) == null && !store.Link("attacker", pendingCode, now.AddMinutes(1)), "opt-out blocks XP, playtime and linking and revokes outstanding link codes");
            check(Directory.GetFiles(folder, "stats.json.corrupt-*").Length == 0 && Directory.GetFiles(folder).All(f =>
            {
                string text = File.ReadAllText(f);
                return !text.Contains(hidden) && !text.Contains(name) && !text.Contains(discord);
            }), "privacy erasure removes raw identity, links and stats from the main database, backups and old damaged archives");
            check(store.Get("other@steam").Xp == 20, "erasing one player preserves every other consenting player's stats");
            var loaded = new StatsStore(path);
            check(!loaded.CanTrack(hidden) && !loaded.Record(hidden, name, "action", 1, "after-restart"), "a salted opt-out marker prevents tracking from silently resuming after restart");
            loaded.OptIn(hidden);
            check(loaded.CanTrack(hidden) && loaded.Get(hidden) == null && loaded.Record(hidden, name, "craft", 5, "fresh") && loaded.Get(hidden).Xp == 5 && loaded.Get(hidden).PlaytimeSeconds == 0, "explicit opt-in starts fresh without recovering erased XP, playtime or account links");
            loaded.SetDoNotTrack(hidden, true);
            check(!loaded.CanTrack(hidden) && loaded.Get(hidden) == null && !loaded.AddPlaytime(hidden, name, 1), "the native DNT gate erases existing data and suppresses new playtime");
            loaded = new StatsStore(path);
            check(!loaded.CanTrack(hidden), "DNT suppression also protects recovery/restarts before the next authenticated flag check");
            loaded.OptOut(hidden);
            loaded.SetDoNotTrack(hidden, false);
            check(!loaded.CanTrack(hidden), "turning off DNT never overrides an explicit manual opt-out");
            loaded.OptIn(hidden);
            loaded.AddPlaytime(hidden, name, 10);
            loaded.Save();
            check(new StatsStore(path).Get(hidden).PlaytimeSeconds == 10, "playtime survives a database roundtrip independently of round XP resets");
            Parallel.Invoke(() =>
            {
                for (int i = 0; i < 30; i++)
                    loaded.Record(hidden, name, "action", 1, "parallel-" + i);
            }, () => loaded.OptOut(hidden), () => loaded.Save());
            check(loaded.Get(hidden) == null && !new StatsStore(path).CanTrack(hidden) && !File.ReadAllText(path + ".backup").Contains(hidden), "concurrent gameplay, autosave and opt-out cannot resurrect an erased profile in memory or backups");
            string legacy = Path.Combine(folder, "legacy.json");
            File.WriteAllText(legacy, "{\"Schema\":1,\"Players\":{\"legacy\":{\"UserId\":\"legacy\",\"Name\":\"Legacy\",\"Xp\":100,\"DiscordUserId\":null,\"Actions\":{}}}}");
            var old = new StatsStore(legacy);
            check(old.Get("legacy").Xp == 100 && old.Get("legacy").PlaytimeSeconds == 0, "previous stats databases load without resetting XP when playtime is introduced");
            File.WriteAllText(path + ".privacy", "broken");
            File.WriteAllText(path + ".privacy.backup", "broken");
            bool refused = false;
            try
            {
                _ = new StatsStore(path);
            }
            catch (Exception)
            {
                refused = true;
            }

            check(refused, "unreadable privacy preferences fail closed rather than discarding opt-out choices");
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }
}
