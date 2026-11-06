using System;
using System.Collections.Generic;
using System.Linq;
using Brainless.Internal;
using Exiled.API.Features;
using Exiled.CustomItems.API.Features;

namespace Brainless.Coinflips.Types
{
    // Optional Brainless integration: custom items from the Items/Weapons DLLs (found in
    // EXILED's CustomItems registry by their GiveForRole method) and custom roles from
    // the Roles DLL (through RolesBridge). No hard reference to any other plugin DLL.
    internal static class BrainlessContent
    {
        private static readonly Random Rd = new();

        internal static CustomItem[] Installed(ICollection<uint> ids) => CustomItem.Registered
            .Where(item => (ids == null || ids.Count == 0 || ids.Contains(item.Id)) && GiveMethod(item) != null)
            .ToArray();

        internal static bool HasItems(ICollection<uint> ids) => Installed(ids).Length > 0;
        internal static bool HasRoles => RolesBridge.IsReady;

        // Gives one random listed item that can be issued right now; returns its name, or null.
        internal static string GiveRandomItem(Player player, ICollection<uint> ids, bool ignoreSupply, IDictionary<uint, double> weights = null)
        {
            // Weighted shuffle: an item's chance of coming first follows its weight; the
            // rest are tried in order if it cannot be given (supply used up, disabled).
            double Weight(CustomItem item) => weights != null && weights.TryGetValue(item.Id, out double weight) ? weight : 1;
            foreach (var item in Installed(ids).Where(i => Weight(i) > 0).OrderBy(i => -Math.Log(1 - Rd.NextDouble()) / Weight(i)))
            {
                try
                {
                    if (GiveMethod(item).Invoke(item, new object[] { player, ignoreSupply }) == null)
                        return item.Name;
                }
                catch (Exception exception)
                {
                    Log.Debug($"Brainless Coinflips could not give custom item {item.Id}: {exception.GetBaseException().Message}");
                }
            }

            return null;
        }

        // Grants a random fitting custom role in place; returns its name, or null.
        internal static string GrantRandomRole(Player player, ICollection<string> ids) => RolesBridge.GrantRandomProfile(player, ids?.ToArray());

        // After a coin changes the player's class, roll a custom role for it as if they spawned.
        internal static void RollAfterClassChange(Player player, PlayerRoles.PlayerRoleBase before)
        {
            if (!Plugin.Instance.Config.RollCustomRoleOnClassChange || !HasRoles || !player.IsAlive || player.Role.Base == before)
                return;
            var role = player.Role.Type;
            Plugin.Instance.Later(.5f, () =>
            {
                if (player.IsConnected && player.IsAlive && player.Role.Type == role && RolesBridge.ProfileId(player) == null)
                    RolesBridge.RollProfile(player, true);
            });
        }

        private static System.Reflection.MethodInfo GiveMethod(CustomItem item) => item.GetType().GetMethod("GiveForRole", new[] { typeof(Player), typeof(bool) });
    }
}
