using System;
using CommandSystem;
using Exiled.API.Features;

namespace Brainless.ZombieOptOut
{
    [CommandHandler(typeof(ClientCommandHandler))]
    public sealed class OptInCommand : ICommand
    {
        public string Command => "optin";
        public string[] Aliases => new[] { "zombieoptin", "oi" };
        public string Description => "Take the place of a player who opted out of being a zombie.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var player = Player.Get(sender);
            if (Plugin.Instance == null || player == null || player.IsHost)
            {
                response = "Use this from your F1 console while Zombie Opt-out is enabled.";
                return false;
            }

            return Plugin.Instance.OptIn(player, out response);
        }
    }

    [CommandHandler(typeof(ClientCommandHandler))]
    public sealed class FillCommand : ICommand
    {
        public string Command => "fill";
        public string[] Aliases => Array.Empty<string>();
        public string Description => "Volunteer to replace an SCP who left early in the round.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var player = Player.Get(sender);
            if (Plugin.Instance == null || player == null || player.IsHost)
            {
                response = "Use this from your F1 console while Zombie Opt-out is enabled.";
                return false;
            }

            return Plugin.Instance.Fill(player, out response);
        }
    }
}
