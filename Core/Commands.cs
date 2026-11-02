using System;
using System.Linq;
using CommandSystem;
using Exiled.API.Features;
using Exiled.Permissions.Extensions;

namespace Brainless.Core
{
    [CommandHandler(typeof(ClientCommandHandler))]
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public sealed class StatsCommand : ICommand
    {
        public string Command => "pbxp";
        public string[] Aliases => new[]
        {
            "pbstats"
        };
        public string Description => "View your XP/stats; administrators can specify a player ID. pbxp top shows the leaderboard.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var plugin = Plugin.Instance;
            if (plugin?.Store == null)
            {
                response = "Project Brainless stats are unavailable.";
                return false;
            }

            if (arguments.Count == 1 && arguments.Array[arguments.Offset].Equals("top", StringComparison.OrdinalIgnoreCase))
            {
                var top = plugin.Store.Top();
                response = top.Length == 0 ? "No XP recorded yet." : string.Join("\n", top.Select((p, i) => (i + 1) + ". " + StatsText.SafeName(p.Name) + ": " + p.Xp + " XP, level " + p.Level(plugin.Config.XpPerLevelSquared)));
                return true;
            }

            if (arguments.Count > 1)
            {
                response = "Usage: pbxp [top|playerID].";
                return false;
            }

            if (arguments.Count == 1 && !sender.CheckPermission("projectbrainless.stats"))
            {
                response = "Inspecting another player requires projectbrainless.stats.";
                return false;
            }

            var target = arguments.Count == 0 ? Player.Get(sender) : Player.Get(arguments.Array[arguments.Offset]);
            if (target == null || !target.IsConnected || target.IsHost)
            {
                response = "Choose a connected player; server-console users must provide an ID.";
                return false;
            }

            var stats = plugin.Store.Get(target.UserId) ?? new PlayerStats
            {
                UserId = target.UserId,
                Name = target.Nickname
            };
            if (!plugin.CanTrack(target))
            {
                response = "Stats tracking is disabled for this player.";
                return false;
            }

            response = StatsText.Describe(stats, plugin.Config.XpPerLevelSquared);
            return true;
        }
    }

    [CommandHandler(typeof(ClientCommandHandler))]
    public sealed class LinkCommand : ICommand
    {
        public string Command => "pblink";
        public string[] Aliases => Array.Empty<string>();
        public string Description => "Generate a private one-time code for linking your Project Brainless Discord stats.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var player = Player.Get(sender);
            var plugin = Plugin.Instance;
            if (player == null || !player.IsConnected || player.IsHost || plugin?.Store == null)
            {
                response = "Connected player and Core stats required.";
                return false;
            }

            if (arguments.Count != 0)
            {
                response = "Usage: pblink";
                return false;
            }

            if (plugin.Http == null || !plugin.Http.PrivateEndpointsEnabled)
            {
                response = "Discord linking is not set up on this server yet.";
                return false;
            }

            if (!plugin.CanTrack(player))
            {
                response = "Tracking is disabled. Turn off DNT and use .pbprivacy optin before linking.";
                return false;
            }

            string code = plugin.Store.CreateLinkCode(player.UserId, player.Nickname, DateTime.UtcNow);
            response = code == null ? "Wait 30 seconds before requesting another code." : "Use /" + plugin.Config.StatsApi.DiscordCommandPrefix + "-link code:" + code + " in Discord. Code expires in ten minutes; requesting a new one invalidates the old code.";
            return code != null;
        }
    }

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public sealed class CoreCommand : ICommand
    {
        public string Command => "pbcore";
        public string[] Aliases => Array.Empty<string>();
        public string Description => "Inspect shared-service status or save XP data: pbcore [status|save].";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (!sender.CheckPermission("projectbrainless.core"))
            {
                response = "Missing projectbrainless.core permission.";
                return false;
            }

            var plugin = Plugin.Instance;
            if (plugin == null)
            {
                response = "Core is not enabled.";
                return false;
            }

            if (arguments.Count == 1 && arguments.Array[arguments.Offset].Equals("save", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    plugin.Store?.Save();
                    response = plugin.Store == null ? "Stats unavailable." : "Stats saved.";
                    return plugin.Store != null;
                }
                catch (Exception failure)
                {
                    response = "Save failed (" + failure.GetType().Name + ").";
                    return false;
                }
            }

            if (arguments.Count > 1 || (arguments.Count == 1 && !arguments.Array[arguments.Offset].Equals("status", StringComparison.OrdinalIgnoreCase)))
            {
                response = "Usage: pbcore [status|save].";
                return false;
            }

            response = "Core: enabled; stats: " + (Api.IsReady ? "ready" : "unavailable") + "; shared updaters: " + UpdateHub.Count + "; stats API: " + (plugin.Http == null ? "off" : plugin.Http.PrivateEndpointsEnabled ? "on (port " + plugin.Config.StatsApi.Port + ", Discord bot endpoints enabled)" : "on (port " + plugin.Config.StatsApi.Port + ", leaderboard only)") + ".";
            return true;
        }
    }
}
