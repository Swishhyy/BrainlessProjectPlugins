using System;
using System.Linq;
using CommandSystem;
using Exiled.API.Features;
using Exiled.Permissions.Extensions;

namespace Brainless.Roles
{
    internal static class RoleCommands
    {
        internal static bool Ready(ICommandSender sender, out Plugin plugin, out string response)
        {
            plugin = Plugin.Instance;
            response = null;
            // Inspecting and testing roles is PB.moderator; reloading config is PB.admin.
            if (!Brainless.Internal.Perms.Require(sender, false, out response))
                return false;
            if (plugin == null || !plugin.Config.IsEnabled)
                response = "Brainless Roles is not enabled.";
            return response == null;
        }

        internal static Player Target(string value, ICommandSender sender) => value == null ? Player.Get(sender) : Player.Get(value);
        internal static bool Valid(Player player) => player != null && player.IsConnected && !player.IsHost;
    }

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public sealed class RoleCommand : ICommand
    {
        public string Command => "bprole";
        public string[] Aliases => new[]
        {
            "myrole"
        };
        public string Description => "Inspect roles, test with bprole set <roleID> [playerID], check the role panel with bprole hud [playerID], or reload tuning with bprole reload.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (!RoleCommands.Ready(sender, out var plugin, out response))
                return false;
            string[] args = arguments.ToArray();
            if (args.Length == 1 && args[0].Equals("reload", StringComparison.OrdinalIgnoreCase))
                return Brainless.Internal.Perms.Require(sender, true, out response) && plugin.ReloadTuning(out response);
            if (args.Length >= 1 && args.Length <= 2 && args[0].Equals("hud", StringComparison.OrdinalIgnoreCase))
            {
                Player who = RoleCommands.Target(args.Length == 2 ? args[1] : null, sender);
                if (!RoleCommands.Valid(who))
                {
                    response = "Usage: bprole hud [playerID]";
                    return false;
                }

                response = plugin.HudDiagnosis(who);
                return true;
            }
            if (args.Length == 1 && (args[0] == "*" || args[0].Equals("all", StringComparison.OrdinalIgnoreCase)))
            {
                response = string.Join("\n\n", Player.List.Where(RoleCommands.Valid).OrderBy(p => p.Id).Select(plugin.Inspect));
                if (response.Length == 0)
                    response = "No connected players.";
                return true;
            }

            bool set = args.Length > 0 && args[0].Equals("set", StringComparison.OrdinalIgnoreCase);
            if ((set && (args.Length < 2 || args.Length > 3)) || (!set && args.Length > 1))
            {
                response = "Usage: bprole [playerID|all|reload], or bprole set <roleID> [playerID].";
                return false;
            }

            string target = set ? args.Length == 3 ? args[2] : null : args.Length == 1 ? args[0] : null;
            Player player = RoleCommands.Target(target, sender);
            if (!RoleCommands.Valid(player))
            {
                response = "No connected player matched. Server-console users must specify a player ID.";
                return false;
            }

            if (set)
                return plugin.SetForAdmin(player, args[1], out response);
            response = plugin.Inspect(player);
            return true;
        }
    }

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public sealed class RolesCommand : ICommand
    {
        public string Command => "bproles";
        public string[] Aliases => Array.Empty<string>();
        public string Description => "Audit configured profiles and why each can/cannot roll now. Optionally specify a player ID.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (!RoleCommands.Ready(sender, out var plugin, out response))
                return false;
            if (arguments.Count > 1)
            {
                response = "Usage: bproles [playerID].";
                return false;
            }

            Player player = arguments.Count == 0 ? null : RoleCommands.Target(arguments.Array[arguments.Offset], sender);
            if (arguments.Count > 0 && !RoleCommands.Valid(player))
            {
                response = "No connected player matched.";
                return false;
            }

            response = "Availability for " + (player == null ? "the current server" : player.Nickname + " (" + player.Id + ")") + ". " + (plugin.Config.UseRarity ? "Each spawn rolls a rarity tier, then a role of that tier (weights split a tier); an empty or full tier falls to the next one down." : "Percentages are per matching new spawn; blocked shares keep the normal role.") + " Z05 uses a separate event roll. READY is not guaranteed.\n" + string.Join("\n", plugin.Config.Roles.Select(role =>
            {
                string reason = plugin.Availability(role, player);
                string chance = plugin.Config.UseRarity ? role.Rarity + " (weight " + role.ChancePercent.Value.ToString("0.######") + ")" : role.ChancePercent.Value.ToString("0.######") + "%";
                return role.Id + ": " + role.Name + " - " + chance + " - " + (reason == null ? "READY" : "BLOCKED: " + reason);
            }));
            if (plugin.Config.UseRarity)
                response += "\n" + plugin.RarityStatus();
            return true;
        }
    }

    [CommandHandler(typeof(ClientCommandHandler))]
    public sealed class RerollCommand : ICommand
    {
        public string Command => "scpreroll";
        public string[] Aliases => new[] { "reroll" };
        public string Description => "Reroll your custom role once, if you were offered a reroll when you spawned.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var plugin = Plugin.Instance;
            var player = Player.Get(sender);
            if (plugin == null || !plugin.Config.IsEnabled || !RoleCommands.Valid(player))
            {
                response = "Use this from your ~ player console while Brainless Roles is enabled.";
                return false;
            }

            return plugin.Reroll(player, out response);
        }
    }
}
