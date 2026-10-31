using System;
using System.Linq;

namespace Brainless.Roles
{
    internal static class RoleLookup
    {
        private static string Key(string value) => new string ((value ?? "").Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
        internal static bool Matches(string id, string name, string query)
        {
            string key = Key(query);
            if (key.Length == 0)
                return false;
            return key == Key(id) || key == Key(name) || (id == "Z04" && (key == "explosivezombie" || key == "bomberzombie"));
        }
    }
}
