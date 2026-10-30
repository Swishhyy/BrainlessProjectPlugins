using System;
using CommandSystem;
using Exiled.Permissions.Extensions;

namespace Brainless.Weapons
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public sealed class WeaponsCommand : ICommand
    {
        public string Command => "bpweapons";
        public string[] Aliases => Array.Empty<string>();
        public string Description => "Reload Project Brainless weapon tuning from YAML: bpweapons reload.";

        public bool Execute(ArraySegment<string> args, ICommandSender sender, out string response)
        {
            if (!sender.CheckPermission("brainless.weapons.configure"))
            {
                response = "Missing brainless.weapons.configure permission.";
                return false;
            }

            if (args.Count != 1 || args.Array[args.Offset].ToLowerInvariant() != "reload")
            {
                response = "Usage: bpweapons reload";
                return false;
            }

            if (Plugin.Instance == null)
            {
                response = "Weapons is not enabled.";
                return false;
            }

            return Plugin.Instance.ReloadTuning(out response);
        }
    }
}
