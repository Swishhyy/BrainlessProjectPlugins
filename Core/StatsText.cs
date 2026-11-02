using System;
using System.Globalization;
using System.Linq;

namespace Brainless.Core
{
    // Player-facing text for stats replies (F1 console and API responses).
    internal static class StatsText
    {
        internal static string SafeName(string name)
        {
            string clean = string.Concat((name ?? "Player").Where(c => !char.IsControl(c) && c != '@' && c != '`' && c != '*' && c != '_' && c != '~' && c != '<' && c != '>'));
            return clean.Length == 0 ? "Player" : clean.Substring(0, Math.Min(48, clean.Length));
        }

        internal static string Describe(PlayerStats player, int step)
        {
            if (player == null)
                return "No stats recorded yet.";
            string actions = string.Join(", ", player.Actions.OrderBy(x => x.Key).Select(x => x.Key.Replace('_', ' ') + ": " + x.Value));
            return SafeName(player.Name) + " — level " + player.Level(step) + ", " + player.Xp + " XP\nServer playtime: " + Playtime(player.PlaytimeSeconds) + "\n" + (actions.Length == 0 ? "No recorded actions yet." : actions);
        }

        internal static string Playtime(long seconds) => (seconds / 3600).ToString(CultureInfo.InvariantCulture) + "h " + ((seconds % 3600) / 60).ToString(CultureInfo.InvariantCulture) + "m";
    }
}
