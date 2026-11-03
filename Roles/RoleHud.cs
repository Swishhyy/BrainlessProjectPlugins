using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Exiled.API.Features;
using Exiled.API.Features.Roles;
using UnityEngine;

namespace Brainless.Roles
{
    internal sealed class RoleHud
    {
        private readonly Config config;
        private readonly Func<Player, RoleDefinition> getRole;
        private readonly Dictionary<Player, Hint> owned = new Dictionary<Player, Hint>();
        // When another plugin's hints block a spectator's panel for this long, show it anyway.
        private const float SpectatorStarvationSeconds = 4;
        private readonly Dictionary<Player, float> blockedSince = new Dictionary<Player, float>();
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
            foreach (var player in owned.Keys.Where(p => !p.IsConnected || !config.ShowRoleHud || Subject(p) == null).ToArray())
                Forget(player);
            if (!config.ShowRoleHud)
                return;
            foreach (var player in Player.List.Where(p => p.IsConnected && !p.IsHost))
            {
                var subject = Subject(player);
                if (subject == null)
                    continue;
                bool spectating = subject != player;
                // Let item instructions, combat warnings and CedMod hints finish; never
                // replace a hint we did not send. Spectators get an exception: if other
                // hints keep the panel hidden for several seconds, it shows anyway.
                if (Blocked(player))
                {
                    if (!blockedSince.TryGetValue(player, out float since))
                        blockedSince[player] = since = Time.time;
                    if (!spectating || Time.time - since < SpectatorStarvationSeconds)
                        continue;
                }

                blockedSince.Remove(player);
                RoleDefinition role = getRole(subject);
                string name = Escape(SingleLine(role?.Name ?? subject.Role.Name));
                string color = ColorUtility.ToHtmlStringRGB(subject.Role.Color);
                bool right = config.RoleHudSide == HudSide.Right;
                float padding = Bounded(config.RoleHudEdgePaddingPercent, 0, 12, .5f);
                float width = Bounded(config.RoleHudWidthPercent, 20, 40, 30);
                float opposite = 100 - width - padding;
                float offset = Bounded(config.RoleHudVerticalOffset, -18, 8, 0) * 20;
                string vertical = offset.ToString("0.##", CultureInfo.InvariantCulture) + "px";
                var text = new StringBuilder("<align=" + (right ? "right" : "left") + "><margin-left=" + (right ? opposite : padding).ToString("0.##", CultureInfo.InvariantCulture) + "%><margin-right=" + (right ? padding : opposite).ToString("0.##", CultureInfo.InvariantCulture) + "%><line-height=24px>");
                // Fixed line height and an explicit pixel offset on every line avoid
                // voffset changing the gap between the name and its description.
                if (spectating)
                    text.Append("<voffset=").Append(vertical).Append("><size=13><color=#AAAAAA>Spectating ").Append(Escape(SingleLine(subject.Nickname))).Append("</color></size></voffset>\n");
                text.Append("<voffset=").Append(vertical).Append("><size=20><nobr><b><color=#").Append(color).Append(">").Append(name).Append("</color></b>");
                if (role == null && spectating)
                    text.Append(" <size=12><color=#AAAAAA>no subclass</color></size>");
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

        private bool Blocked(Player player) => player.HasHint && (!owned.TryGetValue(player, out var hint) || !ReferenceEquals(player.CurrentHint, hint));

        // Whose role this player's HUD shows: their own while alive, or (for spectators)
        // the living player they are watching.
        private Player Subject(Player player)
        {
            if (player.IsAlive)
                return player;
            if (!config.ShowSpectatedRole || !(player.Role is SpectatorRole spectator))
                return null;
            var target = spectator.SpectatedPlayer;
            return target != null && target != player && target.IsConnected && target.IsAlive ? target : null;
        }

        // Admin diagnostics for "bprole hud <player>": why the panel is or isn't showing.
        internal string Diagnose(Player player)
        {
            if (!config.ShowRoleHud)
                return "Role HUD is off (show_role_hud: false).";
            if (player.IsAlive)
                return player.Nickname + " is alive and sees their own role panel" + (Blocked(player) ? ", currently waiting for another hint to finish." : ".");
            if (!(player.Role is SpectatorRole spectator))
                return player.Nickname + " is " + player.Role.Type + ", not spectating.";
            if (!config.ShowSpectatedRole)
                return "Spectator panel is off (show_spectated_role: false).";
            var target = spectator.SpectatedPlayer;
            if (target == null)
                return player.Nickname + " is spectating, but the game has not reported who they are watching yet (switch target once).";
            if (!target.IsAlive)
                return player.Nickname + " is watching " + target.Nickname + ", who is not alive.";
            var role = getRole(target);
            string what = role == null ? "no subclass (" + target.Role.Type + ")" : role.Id + " - " + role.Name;
            string state = owned.TryGetValue(player, out var hint) && ReferenceEquals(player.CurrentHint, hint) ? "shown" : Blocked(player) ? "waiting for another plugin's hint (shown anyway after " + SpectatorStarvationSeconds + "s)" : "will show on the next refresh";
            return player.Nickname + " is watching " + target.Nickname + ": " + what + ". Panel: " + state + ".";
        }

        internal void Forget(Player player)
        {
            blockedSince.Remove(player);
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
            blockedSince.Clear();
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
