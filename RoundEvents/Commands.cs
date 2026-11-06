using System;
using CommandSystem;
using Exiled.Permissions.Extensions;

namespace Brainless.RoundEvents
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public sealed class EventsCommand : ICommand
    {
        public string Command => "bpevent";
        public string[] Aliases => new[]
        {
            "bpevents"
        };
        public string Description => "Project Brainless round events: list, status, vote (lobby), infection/deathmatch/juggernaut next|start, stop, reload.";

        public bool Execute(ArraySegment<string> args, ICommandSender sender, out string response)
        {
            response = "Permission denied: projectbrainless.events required.";
            if (!sender.CheckPermission("projectbrainless.events"))
                return false;
            var plugin = Plugin.Instance;
            if (plugin == null)
            {
                response = "Round Events is not enabled.";
                return false;
            }

            string first = args.Count > 0 ? args.Array[args.Offset].ToLowerInvariant() : "status";
            if (args.Count <= 1)
            {
                if (first == "status")
                {
                    response = plugin.Status();
                    return true;
                }

                if (first == "list")
                {
                    response = "infection — low-health SCP-3114 swarm; direct SCP kills convert humans at their death point. Enabled: " + plugin.Config.Infection.Enabled
                        + "\ndeathmatch — Chaos holds the facility, MTF raids from the surface; respawn waves, first to the kill target wins. Enabled: " + plugin.Config.Deathmatch.Enabled
                        + "\njuggernaut — one armored Chaos Juggernaut vs NTF hunters; kill the Juggernaut to become it. Enabled: " + plugin.Config.Juggernaut.Enabled;
                    return true;
                }

                if (first == "stop")
                {
                    plugin.StopEvent();
                    response = "Event and queued selection stopped; pending conversions cancelled. Restart the round for its normal roles/loadouts.";
                    return true;
                }

                if (first == "reload")
                    return plugin.Reload(out response);
                if (first == "vote")
                    return plugin.StaffVote(out response);
            }

            if (args.Count == 2 && first == "infection")
            {
                string action = args.Array[args.Offset + 1].ToLowerInvariant();
                if (action == "next")
                    return plugin.QueueInfection(out response);
                if (action == "start")
                    return plugin.StartInfection(out response);
            }

            if (args.Count == 2 && (first == "deathmatch" || first == "dm" || first == "juggernaut" || first == "jugg"))
            {
                var mode = first == "deathmatch" || first == "dm" ? ArenaMode.Deathmatch : ArenaMode.Juggernaut;
                string action = args.Array[args.Offset + 1].ToLowerInvariant();
                if (action == "next")
                    return plugin.QueueArena(mode, out response);
                if (action == "start")
                    return plugin.StartArena(mode, out response);
            }

            response = "bpevent list | status | vote | infection|deathmatch|juggernaut next|start | stop | reload";
            return false;
        }
    }

    [CommandHandler(typeof(ClientCommandHandler))]
    public sealed class VoteCommand : ICommand
    {
        public string Command => "vote";
        public string[] Aliases => new[] { "eventvote" };
        public string Description => "Event vote: .vote <number|name>, .vote optout to sit an event out, .vote optin to play.";

        public bool Execute(ArraySegment<string> args, ICommandSender sender, out string response)
        {
            var plugin = Plugin.Instance;
            var player = Exiled.API.Features.Player.Get(sender);
            if (plugin == null || player == null || player.IsHost || string.IsNullOrEmpty(player.UserId))
            {
                response = "Use this from your ~ console while Round Events is enabled.";
                return false;
            }

            string choice = args.Count > 0 ? args.Array[args.Offset].ToLowerInvariant() : "";
            if (choice == "")
            {
                response = plugin.VoteStatus(player);
                return true;
            }

            if (choice == "optout" || choice == "out" || choice == "sitout")
                return plugin.SetSitOut(player, true, out response);
            if (choice == "optin" || choice == "in")
                return plugin.SetSitOut(player, false, out response);
            return plugin.CastVote(player, choice, out response);
        }
    }
}
