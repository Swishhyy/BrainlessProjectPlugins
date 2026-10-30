using System;
using CommandSystem;
using Exiled.Permissions.Extensions;

namespace Brainless.Items
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public sealed class ItemsCommand : ICommand
    {
        public string Command => "bpitems";
        public string[] Aliases => Array.Empty<string>();
        public string Description => "Reload Project Brainless item tuning from YAML: bpitems reload.";

        public bool Execute(ArraySegment<string> args, ICommandSender sender, out string response)
        {
            if (!sender.CheckPermission("brainless.items.configure"))
            {
                response = "Missing brainless.items.configure permission.";
                return false;
            }

            if (args.Count != 1 || args.Array[args.Offset].ToLowerInvariant() != "reload")
            {
                response = "Usage: bpitems reload";
                return false;
            }

            if (Plugin.Instance == null)
            {
                response = "Items is not enabled.";
                return false;
            }

            return Plugin.Instance.ReloadTuning(out response);
        }
    }
}
