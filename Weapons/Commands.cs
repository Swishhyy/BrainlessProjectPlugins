using System;
using System.Linq;
using CommandSystem;
using Exiled.Permissions.Extensions;

namespace Brainless.Weapons
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public sealed class WeaponsCommand : ICommand
    {
        public string Command => "bpweapons";
        public string[] Aliases => Array.Empty<string>();
        public string Description => "Inspect spawn chances or reload Project Brainless weapon tuning from YAML: bpweapons reload.";

        public bool Execute(ArraySegment<string> args, ICommandSender sender, out string response)
        {
            // chances is PB.moderator; reload is PB.admin.
            bool lookOnly = args.Count == 1 && args.Array[args.Offset].Equals("chances", StringComparison.OrdinalIgnoreCase);
            if (!Brainless.Internal.Perms.Require(sender, !lookOnly, out response))
                return false;

            if (args.Count != 1 || !(args.Array[args.Offset].Equals("reload", StringComparison.OrdinalIgnoreCase) || args.Array[args.Offset].Equals("chances", StringComparison.OrdinalIgnoreCase)))
            {
                response = "Usage: bpweapons reload | chances";
                return false;
            }

            if (Plugin.Instance == null)
            {
                response = "Weapons is not enabled.";
                return false;
            }

            if (args.Array[args.Offset].Equals("chances", StringComparison.OrdinalIgnoreCase))
            {
                response = Plugin.Instance.Config.Distribution.DescribeChances(id => Plugin.Instance.Config.Weapons.FirstOrDefault(w => w.Id == id)?.Rarity ?? Brainless.Internal.Rarity.Common, Brainless.Internal.RarityLedger.Describe(Brainless.Internal.NaturalDistribution.Scope, Brainless.Internal.NaturalDistribution.RoundKey, Plugin.Instance.Config.Distribution.Rarity, new System.Random()));
                return true;
            }

            return Plugin.Instance.ReloadTuning(out response);
        }
    }
}
