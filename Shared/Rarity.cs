using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Brainless.Internal
{
    // Compiled separately into Items, Weapons and Roles. Every custom item, weapon and
    // role has one of these tiers; the tier sets how often it rolls and how many of
    // that tier may appear in one round.
    public enum Rarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary,
        Mythical
    }

    [TuningSection]
    public sealed class RarityTier
    {
        [Description("Chance out of 100 that a roll lands on this tier.")]
        [Tune(0, 100)]
        public double ChancePercent { get; set; }

        [Description("Most of this tier per round (0 = no limit).")]
        [Tune(0, 1000)]
        public int RoundLimit { get; set; }

        [Description("Above round_limit: each round picks its limit between the two (e.g. 1-2).")]
        [Tune(0, 1000)]
        public int RoundLimitMax { get; set; }
    }

    [TuningSection]
    public sealed class RarityTable
    {
        public RarityTier Common { get; set; } = new RarityTier { ChancePercent = 40 };
        public RarityTier Uncommon { get; set; } = new RarityTier { ChancePercent = 25, RoundLimit = 12 };
        public RarityTier Rare { get; set; } = new RarityTier { ChancePercent = 12, RoundLimit = 6 };
        public RarityTier Epic { get; set; } = new RarityTier { ChancePercent = 6, RoundLimit = 4 };
        public RarityTier Legendary { get; set; } = new RarityTier { ChancePercent = 3, RoundLimit = 2 };
        public RarityTier Mythical { get; set; } = new RarityTier { ChancePercent = 1, RoundLimit = 1, RoundLimitMax = 2 };

        internal RarityTier Get(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Uncommon: return Uncommon;
                case Rarity.Rare: return Rare;
                case Rarity.Epic: return Epic;
                case Rarity.Legendary: return Legendary;
                case Rarity.Mythical: return Mythical;
                default: return Common;
            }
        }

        internal void Validate(string path)
        {
            foreach (var tier in RarityRules.All)
            {
                var settings = Get(tier);
                Tuning.Validate(settings, path + "." + tier);
                if (settings.RoundLimitMax != 0 && settings.RoundLimitMax < settings.RoundLimit)
                    throw new InvalidOperationException(path + "." + tier + ".round_limit_max must be 0 or at least round_limit.");
            }

            if (RarityRules.All.Sum(t => (decimal)Get(t).ChancePercent) > 100m)
                throw new InvalidOperationException(path + ": the tier chances must add up to 100 or less.");
        }
    }

    internal static class RarityRules
    {
        internal static readonly Rarity[] All = { Rarity.Common, Rarity.Uncommon, Rarity.Rare, Rarity.Epic, Rarity.Legendary, Rarity.Mythical };

        // One 0-100 roll over the tiers in order (scaled by scale). With normalize, the
        // tiers up to max fill the whole roll, so some tier is always chosen.
        internal static Rarity? RollTier(RarityTable table, double roll, Rarity max = Rarity.Mythical, double scale = 1, bool normalize = false)
        {
            var tiers = All.Where(t => t <= max).ToArray();
            double total = tiers.Sum(t => Math.Max(0, table.Get(t).ChancePercent) * scale);
            if (normalize)
            {
                if (total <= 0)
                    return null;
                roll = roll / 100 * total;
            }

            double cursor = 0;
            foreach (var tier in tiers)
            {
                double end = cursor + Math.Max(0, table.Get(tier).ChancePercent) * scale;
                if (roll >= cursor && roll < end)
                    return tier;
                cursor = end;
            }

            return null;
        }

        // From the rolled tier: optionally fall to lower tiers, then optionally climb to
        // higher ones (up to max) until a tier is open and has a usable candidate. Inside a
        // tier, weight decides (0 = never).
        internal static T Pick<T>(IEnumerable<T> candidates, Func<T, Rarity> rarity, Func<T, double> weight, Rarity start, Func<Rarity, bool> open, Func<T, bool> usable, Func<double> random, bool fallDown, bool climb = false, Rarity max = Rarity.Mythical)
            where T : class
        {
            var list = candidates.Where(c => weight(c) > 0).ToList();
            var order = new List<Rarity> { start };
            if (fallDown)
                order.AddRange(All.Where(t => t < start).Reverse());
            if (climb)
                order.AddRange(All.Where(t => t > start && t <= max));
            foreach (var tier in order)
            {
                if (!open(tier))
                    continue;
                var pool = list.Where(c => rarity(c) == tier).ToList();
                while (pool.Count > 0)
                {
                    double total = pool.Sum(weight);
                    double roll = random() * total;
                    T chosen = pool[pool.Count - 1];
                    foreach (var candidate in pool)
                    {
                        roll -= weight(candidate);
                        if (roll < 0)
                        {
                            chosen = candidate;
                            break;
                        }
                    }

                    if (usable(chosen))
                        return chosen;
                    pool.Remove(chosen);
                }
            }

            return null;
        }

        // Share of a tier for one candidate when several of the same tier compete.
        internal static double Share(RarityTable table, Rarity tier, double weight, double tierWeight, double scale = 1) => tierWeight <= 0 || weight <= 0 ? 0 : table.Get(tier).ChancePercent * scale * weight / tierWeight;

        internal static string Color(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Uncommon: return "#4CD964";
                case Rarity.Rare: return "#3D9BFF";
                case Rarity.Epic: return "#B45CFF";
                case Rarity.Legendary: return "#FFA726";
                case Rarity.Mythical: return "#FF2D55";
                default: return "#C8C8C8";
            }
        }

        internal static string Tag(Rarity rarity) => "<color=" + Color(rarity) + ">" + rarity + "</color>";
    }

    // Per-round tier counts kept in AppDomain data, so separate plugin DLLs (Items and
    // Weapons both use scope "items") share one set of limits without referencing each
    // other. The round key resets the counts; a ranged limit (Mythical 1-2) is rolled
    // once per round by whichever plugin asks first.
    internal static class RarityLedger
    {
        private const string Key = "Brainless.RarityLedger.v1";
        private static readonly object Gate = new object();

        private static Dictionary<string, int> Store(string scope, int round)
        {
            var store = AppDomain.CurrentDomain.GetData(Key) as Dictionary<string, int>;
            if (store == null)
            {
                store = new Dictionary<string, int>();
                AppDomain.CurrentDomain.SetData(Key, store);
            }

            string marker = scope + "#round";
            if (!store.TryGetValue(marker, out int current) || current != round)
            {
                foreach (var key in store.Keys.Where(k => k.StartsWith(scope + "#", StringComparison.Ordinal)).ToArray())
                    store.Remove(key);
                store[marker] = round;
            }

            return store;
        }

        internal static int Limit(string scope, int round, Rarity tier, RarityTier settings, Random random)
        {
            lock (Gate)
            {
                var store = Store(scope, round);
                string key = scope + "#" + tier + "#limit";
                if (!store.TryGetValue(key, out int limit))
                {
                    limit = settings.RoundLimitMax > settings.RoundLimit ? random.Next(settings.RoundLimit, settings.RoundLimitMax + 1) : settings.RoundLimit;
                    store[key] = limit;
                }

                return limit;
            }
        }

        internal static int Count(string scope, int round, Rarity tier)
        {
            lock (Gate)
                return Store(scope, round).TryGetValue(scope + "#" + tier, out int count) ? count : 0;
        }

        internal static bool Open(string scope, int round, Rarity tier, RarityTier settings, Random random)
        {
            int limit = Limit(scope, round, tier, settings, random);
            return limit == 0 || Count(scope, round, tier) < limit;
        }

        internal static void Take(string scope, int round, Rarity tier)
        {
            lock (Gate)
            {
                var store = Store(scope, round);
                string key = scope + "#" + tier;
                store[key] = (store.TryGetValue(key, out int count) ? count : 0) + 1;
            }
        }

        internal static void Release(string scope, int round, Rarity tier)
        {
            lock (Gate)
            {
                var store = Store(scope, round);
                string key = scope + "#" + tier;
                if (store.TryGetValue(key, out int count) && count > 0)
                    store[key] = count - 1;
            }
        }

        internal static string Describe(string scope, int round, RarityTable table, Random random) => string.Join(", ", RarityRules.All.Select(t =>
        {
            int limit = Limit(scope, round, t, table.Get(t), random);
            return t + " " + Count(scope, round, t) + "/" + (limit == 0 ? "∞" : limit.ToString());
        }));
    }
}
