using System;
using System.Linq;
using CommandSystem;
using Exiled.API.Features;

namespace Brainless.Core
{
    [CommandHandler(typeof(ClientCommandHandler))]
    public sealed class PrivacyCommand : ICommand
    {
        public string Command => "pbprivacy";
        public string[] Aliases => new[]
        {
            "pbtracking"
        };
        public string Description => "Control your stats tracking: pbprivacy [status|optout|optin].";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var player = Player.Get(sender);
            var plugin = Plugin.Instance;
            if (player == null || !player.IsConnected || player.IsHost || plugin?.Store == null)
            {
                response = "Use this from your game's F1 console while Core is enabled.";
                return false;
            }

            string action = arguments.Count == 0 ? "status" : arguments.First().ToLowerInvariant();
            if (arguments.Count > 1 || (action != "status" && action != "optout" && action != "optin"))
            {
                response = ".pbprivacy status | .pbprivacy optout | .pbprivacy optin";
                return false;
            }

            try
            {
                if (action == "optout")
                {
                    plugin.Store.OptOut(player.UserId);
                    response = "Tracking disabled. Your XP, playtime, name and Discord link were erased, including stored stats backups. Only a salted opt-out marker is kept.";
                    return true;
                }

                if (action == "optin")
                {
                    if (player.DoNotTrack)
                    {
                        response = "Turn off DNT in your game settings before opting in.";
                        return false;
                    }

                    plugin.Store.SetDoNotTrack(player.UserId, false);
                    plugin.Store.OptIn(player.UserId);
                    response = "Tracking enabled for new activity. Erased stats and links are not restored.";
                    return true;
                }

                response = "Stats tracking: " + (plugin.CanTrack(player) ? "enabled" : "disabled") + ". DNT: " + (player.DoNotTrack ? "on" : "off") + ".";
                return true;
            }
            catch (Exception)
            {
                response = "Your privacy setting is active in memory, but the file save failed. Core will retry; ask staff to check data-directory write access.";
                return false;
            }
        }
    }
}
