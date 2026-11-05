using System;
using System.Reflection;
using Exiled.API.Features;

namespace Brainless.Internal
{
    // Optional runtime integration with Brainless Roles (Brainless.Roles.RolesApi),
    // without a hard Roles DLL reference. Every call degrades to "no custom role"
    // when Roles is missing, disabled or older than this API.
    internal static class RolesBridge
    {
        private static Type Api => Array.Find(AppDomain.CurrentDomain.GetAssemblies(), a => a.GetName().Name == "Brainless.Roles")?.GetType("Brainless.Roles.RolesApi");

        internal static bool IsReady
        {
            get
            {
                try
                {
                    return (bool?)Api?.GetProperty("IsReady", BindingFlags.Public | BindingFlags.Static)?.GetValue(null) ?? false;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        internal static string ProfileId(Player player) => Call("ProfileId", new[] { typeof(Player) }, player) as string;
        internal static string ProfileName(Player player) => Call("ProfileName", new[] { typeof(Player) }, player) as string;

        // Null on success, otherwise the reason it was refused.
        internal static string AssignProfile(Player player, string id) => IsReady ? Call("AssignProfile", new[] { typeof(Player), typeof(string) }, player, id) as string : "Brainless Roles is not installed or enabled";

        // The assigned profile's name, or null when the roll kept the native role.
        internal static string RollProfile(Player player, bool inPlace) => Call("RollProfile", new[] { typeof(Player), typeof(bool) }, player, inPlace) as string;

        // The granted profile's name, or null when no profile was available.
        internal static string GrantRandomProfile(Player player, string[] allowedIds) => Call("GrantRandomProfile", new[] { typeof(Player), typeof(string[]) }, player, allowedIds ?? new string[0]) as string;

        private static object Call(string name, Type[] types, params object[] args)
        {
            try
            {
                var method = Api?.GetMethod(name, BindingFlags.Public | BindingFlags.Static, null, types, null);
                return method?.Invoke(null, args);
            }
            catch (Exception exception)
            {
                Log.Debug("Brainless Roles integration call " + name + " failed: " + exception.GetBaseException().GetType().Name);
                return null;
            }
        }
    }
}
