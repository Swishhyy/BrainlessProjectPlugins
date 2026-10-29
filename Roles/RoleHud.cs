using System;
using System.Collections.Generic;
using System.Linq;
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
                string name = Escape(role?.Name ?? player.Role.Name);
                string color = ColorUtility.ToHtmlStringRGB(player.Role.Color);
                string label = role == null ? "ROLE" : "CUSTOM ROLE · " + role.Id;
                string detail = role != null && config.RoleHudShowDescription ? "\n<size=14>" + Escape(role.Hint) + "</size>" : "";
                float offset = Math.Max(-18, Math.Min(8, config.RoleHudVerticalOffset));
                string text = "<align=center><size=20><voffset=" + offset.ToString(System.Globalization.CultureInfo.InvariantCulture) + "em>" + "<color=#AAAAAA><size=12>" + label + "</size></color>\n<b><color=#" + color + ">" + name + "</color></b>" + detail + "</voffset></size></align>";
                player.ShowHint(text, 1.2f);
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
    }
}
