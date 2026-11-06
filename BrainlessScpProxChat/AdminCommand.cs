using System;
using System.Globalization;
using System.Linq;
using CommandSystem;
using Exiled.Permissions.Extensions;

namespace Brainless.ScpProxChat
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    [CommandHandler(typeof(GameConsoleCommandHandler))]
    public sealed class AdminCommand : ICommand
    {
        public string Command => "bpscpchat";
        public string[] Aliases => Array.Empty<string>();
        public string Description => "Check SCP speech, reload settings, or adjust live volume.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            bool lookOnly = arguments.Count == 0 || (arguments.Count == 1 && string.Equals(arguments.Array[arguments.Offset], "status", StringComparison.OrdinalIgnoreCase));
            if (!Brainless.Internal.Perms.Require(sender, !lookOnly, out response))
                return false;

            var plugin = Plugin.Instance;
            if (plugin == null)
            {
                response = "SCP proximity chat is not enabled.";
                return false;
            }

            string[] args = arguments.ToArray();
            if (args.Length == 0 || (args.Length == 1 && args[0].Equals("status", StringComparison.OrdinalIgnoreCase)))
            {
                response = plugin.AudioStatus(null);
                return true;
            }

            if (args.Length == 1 && args[0].Equals("reload", StringComparison.OrdinalIgnoreCase))
                return plugin.ReloadAudio(out response);
            if (args.Length == 2 && args[0].Equals("volume", StringComparison.OrdinalIgnoreCase) && float.TryParse(args[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float volume))
                return plugin.SetVolume(volume, out response);
            response = "bpscpchat status | bpscpchat reload | bpscpchat volume <0-20>";
            return false;
        }
    }
}
