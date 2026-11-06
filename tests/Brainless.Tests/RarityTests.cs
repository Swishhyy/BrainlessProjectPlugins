using System;
using System.Collections.Generic;
using System.Linq;
using Brainless.Internal;

internal static class RarityTests
{
    private sealed class Thing
    {
        internal string Name;
        internal Rarity Rarity;
        internal double Weight = 1;
        internal bool Usable = true;
    }

    internal static void Run(Action<bool, string> check)
    {
        var table = new RarityTable();
        check(table.Common.ChancePercent == 40 && table.Uncommon.ChancePercent == 25 && table.Rare.ChancePercent == 12 && table.Epic.ChancePercent == 6 && table.Legendary.ChancePercent == 3 && table.Mythical.ChancePercent == 1, "shipped tier chances are 40/25/12/6/3/1");
        check(table.Common.RoundLimit == 0 && table.Uncommon.RoundLimit == 12 && table.Rare.RoundLimit == 6 && table.Epic.RoundLimit == 4 && table.Legendary.RoundLimit == 2 && table.Mythical.RoundLimit == 1 && table.Mythical.RoundLimitMax == 2, "shipped tier limits: Common unlimited, Mythical 1-2 per round");
        table.Validate("rarity");
        check(RarityRules.RollTier(table, 0) == Rarity.Common && RarityRules.RollTier(table, 39.99) == Rarity.Common && RarityRules.RollTier(table, 40) == Rarity.Uncommon && RarityRules.RollTier(table, 86.5) == Rarity.Mythical && RarityRules.RollTier(table, 87) == null, "a 0-100 roll lands on tiers in order, with the unused remainder giving nothing");
        check(RarityRules.RollTier(table, 15, scale: .3) == Rarity.Uncommon && RarityRules.RollTier(table, 27, scale: .3) == null, "a scaled roll (starting gear) shrinks every tier");
        check(RarityRules.RollTier(table, 99.99, Rarity.Rare, normalize: true) == Rarity.Rare && RarityRules.RollTier(table, 0, Rarity.Rare, normalize: true) == Rarity.Common && Enumerable.Range(0, 1000).All(i => RarityRules.RollTier(table, i / 10.0, Rarity.Rare, normalize: true) <= Rarity.Rare), "a normalized roll up to Rare always gives Common, Uncommon or Rare");
        var bad = new RarityTable();
        bad.Common.ChancePercent = 90;
        check(Throws(() => bad.Validate("rarity")), "tier chances adding past 100 are rejected");
        var badRange = new RarityTable();
        badRange.Epic.RoundLimit = 3; badRange.Epic.RoundLimitMax = 2;
        check(Throws(() => badRange.Validate("rarity")), "a round limit range must not run backwards");

        var things = new List<Thing>
        {
            new Thing { Name = "c1", Rarity = Rarity.Common },
            new Thing { Name = "c2", Rarity = Rarity.Common, Weight = 0 },
            new Thing { Name = "r1", Rarity = Rarity.Rare },
            new Thing { Name = "r2", Rarity = Rarity.Rare, Usable = false },
        };
        var rng = new Random(5);
        Thing Pick(Rarity start, bool fall, bool climb = false, Func<Rarity, bool> open = null, Rarity max = Rarity.Mythical) => RarityRules.Pick(things, t => t.Rarity, t => t.Weight, start, open ?? (_ => true), t => t.Usable, rng.NextDouble, fall, climb, max);
        check(Enumerable.Range(0, 50).All(_ => Pick(Rarity.Rare, false).Name == "r1"), "inside a tier, unusable candidates are skipped for usable ones");
        check(Pick(Rarity.Epic, false) == null && Pick(Rarity.Epic, true).Name == "r1", "an empty tier gives nothing unless it may fall to the next tier down");
        check(Pick(Rarity.Rare, true, open: t => t != Rarity.Rare).Name == "c1", "a tier at its round limit falls through to a lower one");
        check(Enumerable.Range(0, 50).All(_ => Pick(Rarity.Common, true).Name == "c1"), "a zero weight never wins");
        check(Pick(Rarity.Common, true, true, t => t != Rarity.Common, Rarity.Rare).Name == "r1" && Pick(Rarity.Common, true, true, t => t != Rarity.Common, Rarity.Uncommon) == null, "a reroll may climb, but never past its rarity cap");

        // Per-round limits are shared across plugins through AppDomain data.
        string scope = "test-" + Guid.NewGuid().ToString("N");
        var mythical = new RarityTier { ChancePercent = 1, RoundLimit = 1, RoundLimitMax = 2 };
        var seen = new HashSet<int>();
        for (int round = 1; round <= 40; round++)
            seen.Add(RarityLedger.Limit(scope, round, Rarity.Mythical, mythical, new Random(round)));
        check(seen.SetEquals(new[] { 1, 2 }), "each round rolls a Mythical limit of 1 or 2");
        int limit = RarityLedger.Limit(scope, 100, Rarity.Mythical, mythical, new Random(1));
        check(RarityLedger.Limit(scope, 100, Rarity.Mythical, mythical, new Random(99)) == limit, "the rolled limit stays fixed for the rest of the round, whoever asks");
        for (int i = 0; i < limit; i++)
        {
            check(RarityLedger.Open(scope, 100, Rarity.Mythical, mythical, rng), "Mythical is open until its limit");
            RarityLedger.Take(scope, 100, Rarity.Mythical);
        }

        check(!RarityLedger.Open(scope, 100, Rarity.Mythical, mythical, rng), "Mythical closes once the round's limit is reached");
        RarityLedger.Release(scope, 100, Rarity.Mythical);
        check(RarityLedger.Open(scope, 100, Rarity.Mythical, mythical, rng), "a released role (reroll) frees its tier slot");
        check(RarityLedger.Count(scope, 101, Rarity.Mythical) == 0, "a new round starts with empty counts");
        var unlimited = new RarityTier { ChancePercent = 40 };
        for (int i = 0; i < 500; i++)
            RarityLedger.Take(scope, 101, Rarity.Common);
        check(RarityLedger.Open(scope, 101, Rarity.Common, unlimited, rng), "a 0 round limit means unlimited");
        string other = "other-" + Guid.NewGuid().ToString("N");
        RarityLedger.Take(other, 101, Rarity.Common);
        check(RarityLedger.Count(scope, 101, Rarity.Common) == 500 && RarityLedger.Count(other, 101, Rarity.Common) == 1, "scopes (items vs roles) count separately");

#if NATIVE_CONFIG_TESTS
        var roles = new Brainless.Roles.Config();
        roles.Prepare();
        check(roles.Roles.All(r => r.Rarity == Brainless.Roles.Catalog.DefaultRarity(r.Id)) && roles.Roles.Select(r => r.Rarity).Distinct().Count() >= 4, "every shipped role carries its default rarity tier");
        var oldRoles = new Brainless.Roles.Config { RoleCatalogRevision = 10 };
        foreach (var role in oldRoles.Roles) role.Rarity = Rarity.Common;
        oldRoles.Prepare();
        check(oldRoles.RoleCatalogRevision == 14 && oldRoles.Roles.Single(r => r.Id == "B04").Rarity == Rarity.Epic && oldRoles.Roles.Single(r => r.Id == "R01").Rarity == Rarity.Legendary && oldRoles.Roles.Single(r => r.Id == "P07").Rarity == Rarity.Mythical && oldRoles.Roles.Count == 121, "revision 12 gives an existing role list its strength-based rarity tiers and the new roles once");
        var edited = new Brainless.Roles.Config { RoleCatalogRevision = 13 };
        edited.Roles.Single(r => r.Id == "B04").Rarity = Rarity.Mythical;
        edited.Prepare();
        check(edited.Roles.Single(r => r.Id == "B04").Rarity == Rarity.Mythical, "an owner's rarity edit survives later loads");
        check(roles.UseRarity && roles.Reroll.Enabled && roles.Reroll.MaxRarity == Rarity.Rare && roles.Reroll.OfferChancePercent == 35 && roles.Reroll.WindowSeconds == 30, "rerolls are offered to 35% of custom spawns for 30 seconds, capped at Rare");
        var items = new Brainless.Internal.ItemDistributionConfig();
        check(items.UseRarity && items.StartingGearRarityScale == .3 && items.Rarity.Mythical.RoundLimitMax == 2, "item loot uses rarity tiers by default, with starting gear at 30% of map odds");
#endif
    }

    private static bool Throws(Action action)
    {
        try { action(); return false; }
        catch (InvalidOperationException) { return true; }
    }
}
