using System;
using System.Collections.Generic;
using System.Linq;

namespace Brainless.Internal
{
    // Compiled into each DLL. This is source reuse, not a shared runtime plugin.
    internal static class Rules
    {
        internal static T ChooseEligible<T>(IEnumerable<T> candidates, Func<T, bool> eligible, Func<T, int> weight, Random random)
            where T : class
        {
            var pool = candidates.Where(c => weight(c) > 0 && eligible(c)).ToArray();
            int index = WeightedIndex(pool.Select(weight).ToArray(), random);
            return index < 0 ? null : pool[index];
        }

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
        internal bool TryIssue(string key, int itemCap, int count = 1, bool ignoreLimits = false)
        {
            if (count <= 0)
                return false;
            // Explicit admin testing never spends or refunds the natural round budget.
            if (ignoreLimits)
                return true;
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

        internal bool RevokeIssue(string key, int count = 1)
        {
            if (count <= 0 || Count(key) < count)
                return false;
            issued[key] = Count(key) - count;
            Total -= count;
            return true;
        }
    }

    internal sealed class GrantReport
    {
        private readonly List<string> failures = new List<string>();
        internal int Succeeded { get; private set; }
        internal int Attempted { get; private set; }
        internal bool AnySucceeded => Succeeded > 0;

        internal void Record(string target, string failure)
        {
            Attempted++;
            if (failure == null)
                Succeeded++;
            else
                failures.Add(target + ": " + failure);
        }

        internal string Describe(string name) => name + ": given to " + Succeeded + "/" + Attempted + " players." + (failures.Count > 0 ? "\nBlocked:\n" + string.Join("\n", failures) : "");
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
        internal void Forget(int serial) => remaining.Remove(serial);
    }

    internal sealed class BlastBudget
    {
        private readonly Dictionary<string, float> spent = new Dictionary<string, float>();
        private readonly float humanCap, scpCap;
        internal BlastBudget(float humanCap = 75, float scpCap = 200)
        {
            this.humanCap = Math.Max(0, humanCap);
            this.scpCap = Math.Max(0, scpCap);
        }

        internal float Take(string target, bool scp, float requested)
        {
            string key = target ?? "";
            float before = spent.TryGetValue(key, out float value) ? value : 0;
            float amount = Math.Max(0, Math.Min(requested, (scp ? scpCap : humanCap) - before));
            spent[key] = before + amount;
            return amount;
        }
    }
}
