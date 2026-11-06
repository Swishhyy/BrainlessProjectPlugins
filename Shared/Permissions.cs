using CommandSystem;
using Exiled.Permissions.Extensions;

namespace Brainless.Internal
{
    // Two staff permissions for every Brainless plugin. PB.admin can do everything and
    // includes PB.moderator; PB.moderator gets the look-only commands plus SCP swap,
    // role tests and handing out custom items.
    internal static class Perms
    {
        internal const string Admin = "PB.admin";
        internal const string Moderator = "PB.moderator";

        private static bool Has(ICommandSender sender, string node) => sender != null && (sender.CheckPermission(node) || sender.CheckPermission(node.ToLowerInvariant()));

        internal static bool IsAdmin(ICommandSender sender) => Has(sender, Admin);
        internal static bool IsModerator(ICommandSender sender) => IsAdmin(sender) || Has(sender, Moderator);

        // Returns false and fills response when the sender lacks the node.
        internal static bool Require(ICommandSender sender, bool adminOnly, out string response)
        {
            bool ok = adminOnly ? IsAdmin(sender) : IsModerator(sender);
            response = ok ? null : "Missing permission " + (adminOnly ? Admin : Moderator + " (or " + Admin + ")") + ".";
            return ok;
        }
    }
}
