using System;
using System.Collections.Generic;
using System.Linq;
using CommandSystem;
using Exiled.API.Features;
using Exiled.CustomItems.API.Features;
using Exiled.Permissions.Extensions;

namespace Brainless.Items
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public sealed class SupCommand : ICommand
    {
        public string Command => "sci";
        public string[] Aliases => Array.Empty<string>();
        public string Description => "SUP-compatible help, itemlist and admin give. No webhook.";

        private static readonly Dictionary<uint, uint> legacy = new Dictionary<uint, uint>
        {
            [101] = 7114,
            [102] = 7116,
            [103] = 7115,
            [104] = 7117,
            [105] = 7119,
            [106] = 7120,
            [108] = 7102,
            [109] = 7113,
            [110] = 7101,
            [111] = 7207,
            [112] = 7208
        };
        public bool Execute(ArraySegment<string> args, ICommandSender sender, out string response)
        {
            response = "sci itemlist | sci give <currentID/legacyID> [playerID] | sci help\nGive requires customitems.give or sci.admin. Legacy 103 is Anti-096; Volatile Pills use 7122.";
            if (args.Count == 0 || args.At(0) == "help" || args.At(0) == "h")
                return true;
            var items = CustomItem.Registered.Where(i => i.GetType().Assembly.GetName().Name == "Brainless.Items" || i.GetType().Assembly.GetName().Name == "Brainless.Weapons").OrderBy(i => i.Id).ToArray();
            if (args.At(0) == "itemlist" || args.At(0) == "items")
            {
                response = string.Join("\n", items.Select(i => i.Id + ": " + i.Name));
                return true;
            }

            if (!(args.At(0) == "give" || args.At(0) == "gitem") || args.Count < 2 || args.Count > 3)
                return false;
            if (!sender.CheckPermission("customitems.give") && !sender.CheckPermission("sci.admin"))
            {
                response = "Permission denied: customitems.give or sci.admin required.";
                return false;
            }

            if (!uint.TryParse(args.At(1), out uint id))
            {
                response = "Invalid item ID.";
                return false;
            }

            if (legacy.TryGetValue(id, out uint mapped))
                id = mapped;
            var player = args.Count == 3 ? Player.Get(args.At(2)) : Player.Get(sender);
            var item = items.FirstOrDefault(i => i.Id == id);
            if (player == null || item == null)
            {
                response = "Player or enabled item not found. Use sci itemlist.";
                return false;
            }

            var grant = item.GetType().GetMethod("GiveForRole", new[] { typeof(Player), typeof(bool) });
            if (grant == null)
            {
                response = "The item does not support confirmed grants.";
                return false;
            }

            try
            {
                string failure = (string)grant.Invoke(item, new object[] { player, true });
                response = failure ?? item.Name + " given to " + player.Nickname + ".";
                return failure == null;
            }
            catch
            {
                response = "Grant failed; check the server log.";
                return false;
            }
        }
    }
}
