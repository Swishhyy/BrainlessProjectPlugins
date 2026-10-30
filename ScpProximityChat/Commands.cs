using System;
using System.Linq;
using CommandSystem;
using Exiled.API.Features;

namespace Brainless.ScpProximityChat
{
    [CommandHandler(typeof(ClientCommandHandler))]
    public sealed class VoiceCommand : ICommand
    {
        public string Command => "scpchat";
        public string[] Aliases => new[]
        {
            "scpvc"
        };
        public string Description => "Choose nearby/team SCP voice or mute nearby SCP audio. scpchat [local|team|status|mute|unmute] [playerID|all]";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var plugin = Plugin.Instance;
            var player = Player.Get(sender);
            if (plugin == null || player == null || !player.IsConnected || player.IsHost)
            {
                response = "Use this command from your F1 player console while Brainless SCP voice is enabled.";
                return false;
            }

            var state = plugin.State(player);
            string[] args = arguments.ToArray();
            string action = args.Length == 0 ? "toggle" : args[0].ToLowerInvariant();
            if (args.Length == 1 && action == "status")
            {
                response = "SCP voice: " + (plugin.Eligible(player) ? state.Local ? "NEARBY" : "TEAM" : "native voice (role not enabled)") + ". Hear nearby SCPs: " + (state.HearNearby ? "ON" : "OFF") + ". Individual nearby mutes: " + state.MutedUsers.Count + ". Route checks: " + state.RouteChecks + "; nearby audio sends: " + state.AudioSends + ". Tap the nearby key to toggle; use your normal voice key to speak.";
                return true;
            }

            if ((action == "mute" || action == "unmute") && args.Length == 2)
            {
                bool mute = action == "mute";
                if (args[1].Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    state.HearNearby = !mute;
                    response = "Nearby SCP audio " + (mute ? "muted" : "enabled") + ". Individual mutes are unchanged. This command lasts until you change the SSS receive setting or disconnect.";
                    return true;
                }

                if (!int.TryParse(args[1], out int id) || !(Player.Get(id)is Player target) || target.IsHost || string.IsNullOrEmpty(target.UserId))
                {
                    response = "Specify a connected player's numeric ID, or all.";
                    return false;
                }

                if (mute)
                    state.MutedUsers.Add(target.UserId);
                else
                    state.MutedUsers.Remove(target.UserId);
                response = "Nearby SCP audio from player " + target.Id + (mute ? " muted." : " unmuted.");
                return true;
            }

            if (args.Length <= 1 && (action == "toggle" || action == "local" || action == "team"))
                return plugin.Select(player, action == "local" || (action == "toggle" && !state.Local), out response);
            response = "Usage: .scpchat [local|team|status], or .scpchat <mute|unmute> <playerID|all>. No permission is required.";
            return false;
        }
    }
}
