using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Exiled.API.Features;
using UnityEngine;

namespace Brainless.Roles
{
    internal sealed class RoleHud
    {
        private readonly Config config;
        private readonly Func<Player, RoleDefinition> getRole;
        private readonly Dictionary<Player, Hint> owned = new Dictionary<Player, Hint>();
        private float nextRefresh;
        internal RoleHud(Config config, Func<Player, RoleDefinition> getRole)
        {
            this.config = config;
            this.getRole = getRole;
        }

        internal void Update()
        {
            if (Time.time < nextRefresh)
                return;
            nextRefresh = Time.time + .75f;
            foreach (var player in owned.Keys.Where(p => !p.IsConnected || !p.IsAlive || !config.ShowRoleHud).ToArray())
                Forget(player);
            if (!config.ShowRoleHud)
                return;
            foreach (var player in Player.List.Where(p => p.IsConnected && p.IsAlive && !p.IsHost))
            {
                // Let item instructions, combat warnings and CedMod hints finish.
                // Never clear or replace a hint we did not send ourselves.
                if (player.HasHint && (!owned.TryGetValue(player, out var hint) || !ReferenceEquals(player.CurrentHint, hint)))
                    continue;
                RoleDefinition role = getRole(player);
                string name = Escape(SingleLine(role?.Name ?? player.Role.Name));
                string color = ColorUtility.ToHtmlStringRGB(player.Role.Color);
                bool right = config.RoleHudSide == HudSide.Right;
                float padding = Bounded(config.RoleHudEdgePaddingPercent, 0, 12, 3);
                float width = Bounded(config.RoleHudWidthPercent, 20, 40, 30);
                float opposite = 100 - width - padding;
                float offset = Bounded(config.RoleHudVerticalOffset, -18, 8, 0) * 20;
                string vertical = offset.ToString("0.##", CultureInfo.InvariantCulture) + "px";
                var text = new StringBuilder("<align=" + (right ? "right" : "left") + "><margin-left=" + (right ? opposite : padding).ToString("0.##", CultureInfo.InvariantCulture) + "%><margin-right=" + (right ? padding : opposite).ToString("0.##", CultureInfo.InvariantCulture) + "%><line-height=24px>");
                // Fixed line height and an explicit pixel offset on every line avoid
                // voffset changing the gap between the name and its description.
                text.Append("<voffset=").Append(vertical).Append("><size=20><nobr><b><color=#").Append(color).Append(">").Append(name).Append("</color></b>");
                if (role != null)
                    text.Append(" <size=12><color=#AAAAAA>[").Append(Escape(SingleLine(role.Id))).Append("]</color></size>");
                text.Append("</nobr></size></voffset>");
                if (role != null && config.RoleHudShowDescription)
                {
                    foreach (string line in Wrap(role.Hint, 34, 4))
                        text.Append("\n<voffset=").Append(vertical).Append("><size=14><color=#CCCCCC>").Append(Escape(line)).Append("</color></size></voffset>");
                }

                text.Append("</line-height></margin></align>");
                player.ShowHint(text.ToString(), 1.2f);
                owned[player] = player.CurrentHint;
            }
        }

        internal void Forget(Player player)
        {
            if (!owned.TryGetValue(player, out var hint))
                return;
            owned.Remove(player);
            if (player.IsConnected && ReferenceEquals(player.CurrentHint, hint))
                player.ShowHint("", .01f);
        }

        internal void Clear()
        {
            foreach (var player in owned.Keys.ToArray())
                Forget(player);
            nextRefresh = 0;
        }

        private static string Escape(string text) => (text ?? "").Replace("<", "＜").Replace(">", "＞");
        private static string SingleLine(string text) => string.Join(" ", (text ?? "").Split((char[])null, StringSplitOptions.RemoveEmptyEntries));
        private static float Bounded(float value, float min, float max, float fallback) => float.IsNaN(value) || float.IsInfinity(value) ? fallback : Math.Max(min, Math.Min(max, value));
        private static IEnumerable<string> Wrap(string value, int columns, int maxLines)
        {
            string remaining = SingleLine(value);
            for (int row = 0; row < maxLines && remaining.Length > 0; row++)
            {
                if (remaining.Length <= columns)
                {
                    yield return remaining;
                    yield break;
                }

                if (row == maxLines - 1)
                {
                    yield return remaining.Substring(0, columns - 1).TrimEnd() + "…";
                    yield break;
                }

                int split = remaining.LastIndexOf(' ', columns);
                if (split < 1)
                    split = columns;
                yield return remaining.Substring(0, split);
                remaining = remaining.Substring(split).TrimStart();
            }
        }
    }
}
