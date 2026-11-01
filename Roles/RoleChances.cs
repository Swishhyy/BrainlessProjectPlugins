using System;
using System.Collections.Generic;

namespace Brainless.Roles
{
    internal static class RoleChances
    {
        internal static double FromLegacy(double familyChance, int weight, long totalWeight) => totalWeight <= 0 ? 0 : Math.Floor(familyChance * Math.Max(0, weight) / totalWeight * 100) / 100;
        // One shared 0-100 roll gives each profile a fixed interval. Eligibility
        // is checked after selection: blocked shares become ordinary spawns,
        // rather than silently increasing another profile's configured chance.
        internal static T Choose<T>(IEnumerable<T> candidates, Func<T, double> chance, double roll)
            where T : class
        {
            if (double.IsNaN(roll) || double.IsInfinity(roll) || roll < 0 || roll >= 100)
                throw new ArgumentOutOfRangeException(nameof(roll));
            double cursor = 0;
            foreach (var candidate in candidates)
            {
                double percent = chance(candidate);
                if (double.IsNaN(percent) || double.IsInfinity(percent) || percent < 0 || percent > 100)
                    throw new InvalidOperationException("Role percentages must be finite and between 0 and 100.");
                double end = cursor + percent;
                if (roll >= cursor && roll < end)
                    return candidate;
                cursor = end;
            }

            return null;
        }
    }
}
