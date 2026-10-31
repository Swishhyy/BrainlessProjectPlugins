using System;
using System.Reflection;

namespace Brainless.Internal
{
    // Optional runtime integration, deliberately avoiding a hard Core DLL
    // dependency for gameplay plugins. All arguments are ordinary data.
    internal static class CoreBridge
    {
        internal static bool IsReady
        {
            get
            {
                try
                {
                    var assembly = Array.Find(AppDomain.CurrentDomain.GetAssemblies(), a => a.GetName().Name == "Brainless.Core");
                    return (bool? )assembly?.GetType("Brainless.Core.Api")?.GetProperty("IsReady")?.GetValue(null) ?? false;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        internal static bool Record(string userId, string name, string action, int xp, string eventId)
        {
            try
            {
                var assembly = Array.Find(AppDomain.CurrentDomain.GetAssemblies(), a => a.GetName().Name == "Brainless.Core");
                var method = assembly?.GetType("Brainless.Core.Api")?.GetMethod("RecordAction", BindingFlags.Public | BindingFlags.Static);
                return method != null && (bool)method.Invoke(null, new object[] { userId, name, action, xp, eventId });
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
