using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using YamlDotNet.Serialization;

namespace Brainless.Core
{
    internal static class DiscordProtocol
    {
        internal static object Read(string json) => new DeserializerBuilder().Build().Deserialize<object>(json);
        internal static string Write(object value) => new SerializerBuilder().JsonCompatible().Build().Serialize(value);
        internal static object At(object value, string key) => value is IDictionary map && map.Contains(key) ? map[key] : null;
        internal static string Text(object value) => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
        internal static int Number(object value) => int.TryParse(Text(value), NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) ? n : 0;
        internal static Dictionary<string, object> Object(params object[] values)
        {
            var result = new Dictionary<string, object>();
            for (int i = 0; i < values.Length; i += 2)
                result[(string)values[i]] = values[i + 1];
            return result;
        }

        internal static string SafeName(string name)
        {
            string clean = string.Concat((name ?? "Player").Where(c => !char.IsControl(c) && c != '@' && c != '`' && c != '*' && c != '_' && c != '~' && c != '<' && c != '>'));
            return clean.Length == 0 ? "Player" : clean.Substring(0, Math.Min(48, clean.Length));
        }

        internal static string Describe(PlayerStats player, int step)
        {
            if (player == null)
                return "No linked stats yet. Use .pblink in the game's F1 console, then /pb-link code in Discord.";
            string actions = string.Join(", ", player.Actions.OrderBy(x => x.Key).Select(x => x.Key.Replace('_', ' ') + ": " + x.Value));
            return SafeName(player.Name) + " — level " + player.Level(step) + ", " + player.Xp + " XP\n" + (actions.Length == 0 ? "No recorded actions yet." : actions);
        }
    }
}
