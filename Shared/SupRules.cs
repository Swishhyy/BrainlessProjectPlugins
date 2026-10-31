using System;
using System.Collections.Generic;

namespace Brainless.Internal
{
    internal sealed class BatteryLedger
    {
        private readonly Dictionary<int, float> remaining = new Dictionary<int, float>();
        internal void Ensure(int serial, float capacity)
        {
            if (!remaining.ContainsKey(serial))
                remaining[serial] = Math.Max(0, capacity);
        }

        internal float Remaining(int serial) => remaining.TryGetValue(serial, out float value) ? value : 0;
        internal float Spend(int serial, float seconds)
        {
            if (!remaining.ContainsKey(serial))
                return 0;
            return remaining[serial] = Math.Max(0, remaining[serial] - Math.Max(0, seconds));
        }

        internal void Clear() => remaining.Clear();
    }

    internal static class SupRules
    {
        internal static int WeightedIndex(IReadOnlyList<double> weights, double roll)
        {
            double total = 0;
            for (int i = 0; i < weights.Count; i++)
                if (double.IsNaN(weights[i]) || double.IsInfinity(weights[i]) || weights[i] < 0)
                    return -1;
                else
                    total += weights[i];
            if (total <= 0 || double.IsNaN(roll) || roll < 0 || roll >= 1)
                return -1;
            double cursor = roll * total;
            for (int i = 0; i < weights.Count; i++)
            {
                cursor -= weights[i];
                if (cursor < 0)
                    return i;
            }

            return -1;
        }

        internal static bool InsideBeam(float forwardDistance, float sideDistance, float blockedLength, float radius) => forwardDistance >= 0 && forwardDistance <= blockedLength && sideDistance <= radius;
        internal static bool CanHack(bool locked, bool gate, bool elevator, bool checkpoint, bool allowCheckpoints, int containment, int armory, int administration, int max) => !locked && !gate && !elevator && (!checkpoint || allowCheckpoints) && containment <= max && armory <= max && administration <= max;
    }
}
