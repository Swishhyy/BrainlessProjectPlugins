using System;
using System.Linq;
using System.Reflection;
using Exiled.API.Features;
using HarmonyLib;

namespace Brainless.Roles
{
    // Entry points for other Brainless plugins (BrainlessCoinflips, ScpSwap). They call
    // these through reflection, so no plugin needs a hard Roles DLL reference.
    // Arguments are EXILED players and plain strings; everything runs on the main thread.
    public static class RolesApi
    {
        public static bool IsReady => Plugin.Instance?.Config.IsEnabled == true;

        // Current custom profile ID (e.g. "B04"), or null for a native role.
        public static string ProfileId(Player player) => Plugin.Instance?.RoleId(player);

        // Current custom profile name (e.g. "Boom Boom"), or null for a native role.
        public static string ProfileName(Player player) => Plugin.Instance?.ProfileName(player);

        // Applies one profile to the player's current class without moving them. Ignores
        // chance, population and per-round caps; honours disabled/experimental content and
        // class matching. Returns null on success, otherwise the reason.
        public static string AssignProfile(Player player, string id) => Plugin.Instance == null ? "Brainless Roles is not enabled" : Plugin.Instance.AssignExternal(player, id);

        // A normal role roll for the player's current class, using per-role chances and
        // availability. inPlace skips profiles that relocate the player. Returns the
        // assigned profile name, or null when the roll kept the native role.
        public static string RollProfile(Player player, bool inPlace) => Plugin.Instance?.RollExternal(player, inPlace);

        // A guaranteed profile for the player's current class, without moving them: picks
        // among currently available profiles weighted by chance_percent. allowedIds limits
        // the pool (IDs or names; empty means any). Returns the profile name, or null.
        public static string GrantRandomProfile(Player player, string[] allowedIds) => Plugin.Instance?.GrantExternal(player, allowedIds);
    }

    public sealed partial class Plugin
    {
        internal string ProfileName(Player player) => player != null && active.TryGetValue(player, out var state) && player.Role == state.Role ? state.Definition.Name : null;

        internal string AssignExternal(Player player, string id)
        {
            if (player == null || !player.IsConnected || !player.IsAlive)
                return "the player is not alive";
            var definition = Config.Roles.FirstOrDefault(r => RoleLookup.Matches(r.Id, r.Name, id));
            if (definition == null)
                return "unknown role " + id;
            string blocked = Availability(definition, player, admin: true, inPlace: true);
            return blocked ?? Switch(player, definition, true, "Assigned by another Brainless plugin.");
        }

        internal string RollExternal(Player player, bool inPlace)
        {
            if (player == null || !player.IsConnected || !player.IsAlive || !Round.IsStarted)
                return null;
            var selected = RoleChances.Choose(Config.Roles.Where(r => r.Id != "Z05" && Matches(r, player)), r => r.ChancePercent.Value, random.NextDouble() * 100);
            if (selected == null)
            {
                assignmentNotes[player] = "Normal role: the roll landed in the unused percentage.";
                return null;
            }

            string blocked = Availability(selected, player, inPlace: inPlace);
            if (blocked != null)
            {
                assignmentNotes[player] = "Normal role: rolled " + selected.Id + ", but " + blocked + ".";
                return null;
            }

            return Switch(player, selected, false, "Assigned by a roll from another Brainless plugin.") == null ? selected.Name : null;
        }

        internal string GrantExternal(Player player, string[] allowedIds)
        {
            if (player == null || !player.IsConnected || !player.IsAlive || !Round.IsStarted)
                return null;
            string current = RoleId(player);
            var pool = Config.Roles.Where(r => r.Id != "Z05" && r.Id != current && Matches(r, player)
                    && (allowedIds == null || allowedIds.Length == 0 || allowedIds.Any(a => RoleLookup.Matches(r.Id, r.Name, a)))
                    && Availability(r, player, inPlace: true) == null)
                .ToArray();
            double total = pool.Sum(r => r.ChancePercent.Value);
            if (pool.Length == 0 || total <= 0)
                return null;
            double roll = random.NextDouble() * total;
            var selected = pool.Last();
            foreach (var role in pool)
            {
                roll -= role.ChancePercent.Value;
                if (roll < 0)
                {
                    selected = role;
                    break;
                }
            }

            return Switch(player, selected, false, "Granted by another Brainless plugin.") == null ? selected.Name : null;
        }

        // Replaces any current profile with a new one on the player's current life.
        private string Switch(Player player, RoleDefinition definition, bool admin, string note)
        {
            try
            {
                Remove(player);
                // A replacing kit drops what the player carried instead of deleting it.
                if (definition.ReplaceInventory && player.IsHuman)
                    player.DropItems();
                // The guard badge swap deletes native keycards; drop looted ones instead.
                else if (player.Role.Type == PlayerRoles.RoleTypeId.FacilityGuard)
                    foreach (var card in player.Items.OfType<Exiled.API.Features.Items.Keycard>().Where(i => !Exiled.CustomItems.API.Features.CustomItem.Registered.Any(c => c.TrackedSerials.Contains(i.Serial))).ToArray())
                        player.DropItem(card);
                Apply(player, definition, false, admin);
                assignmentNotes[player] = note;
                return null;
            }
            catch (Exception exception)
            {
                return exception.GetBaseException().Message;
            }
        }
    }

    // Marks role changes made by the Remote Admin forceclass command, so only staff
    // force-classes (not events or other plugins using the same reason) can roll a role.
    [HarmonyPatch]
    internal static class ForceClassScope
    {
        private static int depth;
        internal static bool Active => depth > 0;
        private static MethodBase TargetMethod() => AccessTools.Method("CommandSystem.Commands.RemoteAdmin.ForceRoleCommand:Execute");
        private static bool Prepare() => TargetMethod() != null;
        private static void Prefix() => depth++;

        private static Exception Finalizer(Exception __exception)
        {
            depth = Math.Max(0, depth - 1);
            return __exception;
        }
    }
}
