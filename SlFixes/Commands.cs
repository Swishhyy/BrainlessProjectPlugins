using System;
using CommandSystem;
using Exiled.Permissions.Extensions;

namespace Brainless.SlFixes
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public sealed class FixesCommand : ICommand
    {
        public string Command => "pbfixes";
        public string[] Aliases => Array.Empty<string>();
        public string Description => "Inspect SCP-914 recipes/Core integration or reload Fixes tuning: pbfixes [status|reload].";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (!sender.CheckPermission("projectbrainless.fixes"))
            {
                response = "Missing projectbrainless.fixes permission.";
                return false;
            }

            if (Plugin.Instance == null)
            {
                response = "Project Brainless Fixes is not enabled.";
                return false;
            }

            if (arguments.Count == 1 && arguments.Array[arguments.Offset].Equals("reload", StringComparison.OrdinalIgnoreCase))
                return Plugin.Instance.Reload(out response);
            if (arguments.Count > 1 || (arguments.Count == 1 && !arguments.Array[arguments.Offset].Equals("status", StringComparison.OrdinalIgnoreCase)))
            {
                response = "Usage: pbfixes [status|reload].";
                return false;
            }

            response = Plugin.Instance.Status();
            return true;
        }
    }
}
