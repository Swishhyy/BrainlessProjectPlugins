using System;
using System.Collections.Generic;
using System.Linq;
using PlayerRoles;

namespace Brainless.ScpSwap
{
    // Pure parsing/naming rules, shared with the headless tests.
    public static class SwapRules
    {
        private static readonly Dictionary<string, RoleTypeId> Names = new Dictionary<string, RoleTypeId>
        {
            { "049", RoleTypeId.Scp049 }, { "49", RoleTypeId.Scp049 }, { "doctor", RoleTypeId.Scp049 }, { "plague", RoleTypeId.Scp049 }, { "plaguedoctor", RoleTypeId.Scp049 },
            { "079", RoleTypeId.Scp079 }, { "79", RoleTypeId.Scp079 }, { "computer", RoleTypeId.Scp079 }, { "pc", RoleTypeId.Scp079 },
            { "096", RoleTypeId.Scp096 }, { "96", RoleTypeId.Scp096 }, { "shyguy", RoleTypeId.Scp096 }, { "shy", RoleTypeId.Scp096 },
            { "106", RoleTypeId.Scp106 }, { "larry", RoleTypeId.Scp106 }, { "oldman", RoleTypeId.Scp106 },
            { "173", RoleTypeId.Scp173 }, { "peanut", RoleTypeId.Scp173 },
            { "939", RoleTypeId.Scp939 }, { "dog", RoleTypeId.Scp939 },
            { "3114", RoleTypeId.Scp3114 }, { "skeleton", RoleTypeId.Scp3114 },
        };

        // Accepts "173", "scp173", "SCP-173", "peanut" and similar; never SCP-049-2.
        public static bool TryParse(string text, out RoleTypeId role)
        {
            role = RoleTypeId.None;
            string key = new string((text ?? "").Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
            if (key.StartsWith("scp") && key.Length > 3)
                key = key.Substring(3);
            return Names.TryGetValue(key, out role);
        }

        public static string Name(RoleTypeId role) => role switch
        {
            RoleTypeId.Scp049 => "SCP-049",
            RoleTypeId.Scp079 => "SCP-079",
            RoleTypeId.Scp096 => "SCP-096",
            RoleTypeId.Scp106 => "SCP-106",
            RoleTypeId.Scp173 => "SCP-173",
            RoleTypeId.Scp939 => "SCP-939",
            RoleTypeId.Scp3114 => "SCP-3114",
            RoleTypeId.Scp0492 => "SCP-049-2",
            _ => role.ToString(),
        };

        // Window check in seconds; a non-positive window means swaps are only for staff.
        public static bool InWindow(double elapsedSeconds, double windowSeconds) => windowSeconds > 0 && elapsedSeconds >= 0 && elapsedSeconds <= windowSeconds;

        // 0 means unlimited.
        public static bool HasSwapsLeft(int used, int max) => max <= 0 || used < max;
    }
}
