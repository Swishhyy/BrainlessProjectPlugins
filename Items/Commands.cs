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
        public string Description => "Inspect spawn chances or reload Project Brainless item tuning from YAML: bpitems reload.";

        public bool Execute(ArraySegment<string> args, ICommandSender sender, out string response)
        {
            if (!sender.CheckPermission("brainless.items.configure"))
            {
                response = "Missing brainless.items.configure permission.";
                return false;
            }

            if (args.Count != 1 || !(args.Array[args.Offset].Equals("reload", StringComparison.OrdinalIgnoreCase) || args.Array[args.Offset].Equals("chances", StringComparison.OrdinalIgnoreCase)))
            {
                response = "Usage: bpitems reload | chances";
                return false;
            }

            if (Plugin.Instance == null)
            {
                response = "Items is not enabled.";
                return false;
            }

            if (args.Array[args.Offset].Equals("chances", StringComparison.OrdinalIgnoreCase))
            {
                response = Plugin.Instance.Config.Distribution.DescribeChances();
                return true;
            }

            return Plugin.Instance.ReloadTuning(out response);
        }
    }
}
