using System;
using System.Collections.Generic;

namespace Brainless.Internal
{
    // Compiled into each DLL. This is source reuse, not a shared runtime plugin.
    internal static class Rules
    {
        internal static int WeightedIndex(IReadOnlyList<int> weights, Random random)
        {
            long total = 0;
            for (int i = 0; i < weights.Count; i++)
                total += Math.Max(0, weights[i]);
            if (total == 0)
                return -1;
            long roll = (long)(random.NextDouble() * total);
            for (int i = 0; i < weights.Count; i++)
            {
                roll -= Math.Max(0, weights[i]);
                if (roll < 0)
                    return i;
            }

            return -1;
        }

        internal static float Falloff(float distance, float radius, float peak) => radius <= 0 ? 0 : Math.Max(0, peak * (1 - Math.Max(0, distance) / radius));
        // Equivalent incoming damage lets native shield absorption run exactly once.
        internal static float ShieldDamage(float damage, float shield, float shieldFactor, float healthFactor)
        {
            if (damage <= 0)
                return damage;
            float baseToShield = Math.Min(damage, Math.Max(0, shield) / Math.Max(.001f, shieldFactor));
            return baseToShield * shieldFactor + (damage - baseToShield) * healthFactor;
        }

        internal static int UtilityCap(int players) => players < 12 ? 6 : players < 24 ? 10 : 14;
        internal static int WeaponCap(int players) => players < 24 ? 1 : 2;
        internal static int ClusterCap(int players) => players < 12 ? 0 : players < 24 ? 1 : 2;
        internal static int WaveCap(int players) => players < 12 ? 1 : players < 24 ? 2 : 3;
    }

    internal sealed class LimitedBudget
    {
        private readonly Dictionary<string, int> issued = new Dictionary<string, int>();
        internal int Total { get; private set; }
        internal int Capacity { get; set; }

        internal bool CanIssue(string key, int itemCap, int count = 1) => count > 0 && Total + count <= Capacity && Count(key) + count <= itemCap;
        internal int Count(string key) => issued.TryGetValue(key, out int value) ? value : 0;
        internal bool TryIssue(string key, int itemCap, int count = 1)
        {
            if (!CanIssue(key, itemCap, count))
                return false;
            issued[key] = Count(key) + count;
            Total += count;
            return true;
        }

        internal void Reset(int capacity)
        {
            issued.Clear();
            Total = 0;
            Capacity = Math.Max(0, capacity);
        }
    }

    internal sealed class ShotLedger
    {
        private readonly Dictionary<int, int> remaining = new Dictionary<int, int>();
        internal int Remaining(int serial) => remaining.TryGetValue(serial, out int value) ? value : 0;
        internal void Register(int serial, int shots)
        {
            if (!remaining.ContainsKey(serial))
                remaining[serial] = Math.Max(0, shots);
        }

        internal bool Consume(int serial)
        {
            int value = Remaining(serial);
            if (value == 0)
                return false;
            remaining[serial] = value - 1;
            return true;
        }

        internal void Clear() => remaining.Clear();
    }
}
