using System;
using System.Linq;
using CommandSystem;
using Exiled.API.Features;
using Exiled.Permissions.Extensions;
using PlayerRoles;

namespace Brainless.ScpSwap
{
    [CommandHandler(typeof(ClientCommandHandler))]
    public sealed class SwapCommand : ICommand
    {
        public string Command => "scpswap";
        public string[] Aliases => new[] { "swap" };
        public string Description => "Swap SCPs at round start: scpswap [list|<scp>|yes|no|cancel]";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var plugin = Plugin.Instance;
            var player = Player.Get(sender);
            if (plugin == null || player == null || !player.IsConnected || player.IsHost)
            {
                response = "Use this from your F1 player console while Brainless SCP Swap is enabled.";
                return false;
            }

            string action = arguments.Count == 0 ? "list" : arguments.At(0).ToLowerInvariant();
            switch (action)
            {
                case "list":
                case "status":
                    response = plugin.List(player);
                    return true;
                case "yes":
                case "y":
                case "accept":
                    return plugin.Answer(player, true, out response);
                case "no":
                case "n":
                case "deny":
                case "decline":
                    return plugin.Answer(player, false, out response);
                case "cancel":
                    return plugin.Cancel(player, out response);
            }

            if (SwapRules.TryParse(string.Join("", arguments), out RoleTypeId wanted))
                return plugin.RequestSwap(player, wanted, out response);
            response = "Usage: .scpswap list | .scpswap <scp> (e.g. 173, 096, peanut) | .scpswap yes | .scpswap no | .scpswap cancel";
            return false;
        }
    }

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public sealed class ForceSwapCommand : ICommand
    {
        public string Command => "bpscpswap";
        public string[] Aliases => Array.Empty<string>();
        public string Description => "Staff SCP swap, any time: bpscpswap <playerID> <playerID|scp173|scp096|...>";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var plugin = Plugin.Instance;
            if (plugin == null)
            {
                response = "Brainless SCP Swap is not enabled.";
                return false;
            }

            if (!Brainless.Internal.Perms.Require(sender, false, out response))
                return false;

            if (arguments.Count != 2 || !(Player.Get(arguments.At(0)) is Player first))
            {
                response = "Usage: bpscpswap <playerID> <playerID|scp173>. Swaps two SCPs (variants follow the swap mode), or moves one SCP to an SCP type.";
                return false;
            }

            // A bare number is a player ID here; write SCPs as scp096, scp173, peanut, etc.
            if (int.TryParse(arguments.At(1), out _))
            {
                if (Player.Get(arguments.At(1)) is Player second && second != first)
                    return plugin.ForceSwap(first, second, null, out response);
                response = "No other player has ID " + arguments.At(1) + ". Write SCPs as scp096, scp173, etc.";
                return false;
            }

            if (SwapRules.TryParse(arguments.At(1), out RoleTypeId role))
            {
                var holder = Player.List.FirstOrDefault(p => p != first && p.IsAlive && p.Role.Type == role);
                return holder != null ? plugin.ForceSwap(first, holder, null, out response) : plugin.ForceSwap(first, null, role, out response);
            }

            response = "The second argument must be a player ID or an SCP (e.g. scp173).";
            return false;
        }
    }
}
