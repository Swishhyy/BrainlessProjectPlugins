using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using CommandSystem;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.CustomItems.API.Features;
using Exiled.Permissions.Extensions;
using HarmonyLib;
using UnityEngine;

namespace Brainless.Internal
{
    // Each Items/Weapons DLL owns its own interface and patches. A patch handles
    // only definitions implemented by that DLL and leaves all other items alone.
    internal interface IAdminItem
    {
        string TryGive(Player player, bool ignoreLimits, bool displayMessage);
        string TrySpawn(Vector3 position, bool ignoreLimits);
    }

    internal static class AdminItems
    {
        private static Harmony harmony;
        private static string bypassPermission;
        internal static void Enable(string id, string permission)
        {
            bypassPermission = permission;
            harmony = new Harmony(id);
            try
            {
                Patch("Give", nameof(GivePrefix));
                Patch("Spawn", nameof(SpawnPrefix));
            }
            catch
            {
                Disable();
                throw;
            }
        }

        private static void Patch(string command, string prefix)
        {
            Type type = typeof(CustomItem).Assembly.GetType("Exiled.CustomItems.Commands." + command, true);
            MethodInfo method = AccessTools.Method(type, "Execute");
            harmony.Patch(method, prefix: new HarmonyMethod(typeof(AdminItems), prefix));
        }

        internal static void Disable()
        {
            harmony?.UnpatchAll(harmony.Id);
            harmony = null;
        }

        private static bool TryFind(ArraySegment<string> arguments, out CustomItem definition, out IAdminItem item)
        {
            definition = null;
            item = null;
            if (arguments.Count == 0 || !CustomItem.TryGet(arguments.Array[arguments.Offset], out definition))
                return false;
            item = definition as IAdminItem;
            return item != null;
        }

        private static bool Authorize(ICommandSender sender, string permission, bool force, out string response)
        {
            response = null;
            if (!sender.CheckPermission(permission))
                response = "Permission denied: " + permission;
            else if (force && !sender.CheckPermission(bypassPermission))
                response = "The --force option requires " + bypassPermission;
            return response == null;
        }

        private static string[] Arguments(ArraySegment<string> arguments, out bool force)
        {
            force = arguments.Any(a => string.Equals(a, "--force", StringComparison.OrdinalIgnoreCase));
            return arguments.Where(a => !string.Equals(a, "--force", StringComparison.OrdinalIgnoreCase)).ToArray();
        }

        private static bool GivePrefix(ArraySegment<string> arguments, ICommandSender sender, ref string response, ref bool __result)
        {
            if (!TryFind(arguments, out var definition, out var item))
                return true;
            __result = false;
            try
            {
                string[] args = Arguments(arguments, out bool force);
                if (!Authorize(sender, "customitems.give", force, out response))
                    return false;
                Player[] targets;
                if (args.Length == 1)
                {
                    Player self = Player.Get(sender);
                    if (self == null || self.IsHost)
                    {
                        response = "Specify a player: ci give <itemID> <playerID> [--force]";
                        return false;
                    }

                    targets = new[]
                    {
                        self
                    };
                }
                else if (args.Length == 2 && (args[1] == "*" || args[1].Equals("all", StringComparison.OrdinalIgnoreCase)))
                    targets = Player.List.Where(p => p.IsConnected && !p.IsHost).ToArray();
                else
                    targets = Player.GetProcessedData(new ArraySegment<string>(args), 1).Distinct().ToArray();
                if (targets.Length == 0)
                {
                    response = "No matching players. Use their numeric player ID.";
                    return false;
                }

                var report = new GrantReport();
                foreach (Player target in targets)
                {
                    string failure = item.TryGive(target, force, true);
                    report.Record(target.Nickname + " (" + target.Id + ")", failure);
                }

                response = report.Describe(definition.Name, force);
                __result = report.AnySucceeded;
            }
            catch (Exception exception)
            {
                Log.Error("[Brainless custom item give] " + exception);
                response = "Custom item grant failed; check the server log. No successful grant was confirmed.";
            }

            return false;
        }

        private static bool SpawnPrefix(ArraySegment<string> arguments, ICommandSender sender, ref string response, ref bool __result)
        {
            if (!TryFind(arguments, out var definition, out var item))
                return true;
            __result = false;
            try
            {
                string[] args = Arguments(arguments, out bool force);
                if (!Authorize(sender, "customitems.spawn", force, out response))
                    return false;
                Vector3 position;
                if (args.Length == 4)
                {
                    if (!TryCoordinate(args[1], out float x) || !TryCoordinate(args[2], out float y) || !TryCoordinate(args[3], out float z))
                    {
                        response = "Coordinates must be finite numbers: ci spawn <itemID> <x> <y> <z> [--force]";
                        return false;
                    }

                    position = new Vector3(x, y, z);
                }
                else if (args.Length == 2 && Player.Get(args[1])is Player player)
                {
                    if (!player.IsConnected || !player.IsAlive)
                    {
                        response = "Choose a living, connected player.";
                        return false;
                    }

                    position = player.Position;
                }
                else if (args.Length == 2 && Enum.TryParse(args[1], out SpawnLocationType location))
                    position = location.GetPosition();
                else
                {
                    response = "Usage: ci spawn <itemID> <playerID/location> [--force], or ci spawn <itemID> <x> <y> <z> [--force]";
                    return false;
                }

                string failure = item.TrySpawn(position, force);
                __result = failure == null;
                response = failure ?? (definition.Name + " spawned." + (force ? " Admin supply override used." : ""));
            }
            catch (Exception exception)
            {
                Log.Error("[Brainless custom item spawn] " + exception);
                response = "Custom item spawn failed; check the server log.";
            }

            return false;
        }

        private static bool TryCoordinate(string value, out float number) => float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out number) && !float.IsNaN(number) && !float.IsInfinity(number);
    }
}
