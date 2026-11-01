using System;
using System.Linq;
using Brainless.Internal;

internal static class DistributionTests
{
    internal static void Run(Action<bool, string> check)
    {
        var ledger = new DistributionLedger();
        check(ledger.BeginMap() && !ledger.BeginMap(), "round-start/reload cannot replay initial map distribution");
        var life = new object ();
        check(ledger.BeginLife(life) && !ledger.BeginLife(life), "duplicate spawn callbacks cannot reroll one life");
        int attempts = 0;
        check(!ledger.TryGive("user", 0, 1, () =>
        {
            attempts++;
            return true;
        }) && attempts == 0, "a disabled spawn-grant cap cannot issue items");
        check(!ledger.TryGive("user", 3, 0, () => true), "zero per-user grants disables extra gear for that user");
        check(!ledger.TryGive("user", 3, 1, () => false) && ledger.PlayerAwards == 0, "a failed native conversion does not consume the spawn allowance");
        check(ledger.TryGive("user", 3, 1, () => true), "an eligible human receives one successful spawn grant");
        check(ledger.BeginLife(new object ()) && !ledger.TryGive("user", 3, 1, () => true), "death or reconnect cannot reset the same user's earned round limit");
        check(ledger.TryGive("other", 3, 1, () => true) && ledger.PlayerAwards == 2, "another user has an independent grant allowance");
        check(!ledger.TryGive("third", 1, 1, () => true) && ledger.PlayerAwards == 2, "lowering a live grant cap keeps prior awards rather than refilling supply");
        check(!ledger.TryGive("", 3, 1, () => true), "an unidentified player cannot bypass per-user grant accounting");
        var firstRoom = new object ();
        check(!ledger.TryPlace(firstRoom, 1, 3, false, () => false) && ledger.MapAwards == 0, "failed map adoption leaves its allowance and room available");
        check(ledger.TryPlace(firstRoom, 1, 3, false, () => true), "a valid map conversion consumes one map allowance");
        check(!ledger.TryPlace(firstRoom, 1, 3, true, () => true), "replacements and extra drops share their per-room cap");
        check(ledger.TryPlace(new object (), 1, 1, true, () => true) && ledger.ExtraAwards == 1, "extra loot uses a separate channel allowance in another room");
        check(!ledger.TryPlace(new object (), 1, 1, true, () => true), "extra loot cannot exceed its configured count");
        check(!ledger.TryPlace(new object (), 1, 0, false, () => true), "zero map replacements leaves native loot untouched");
        check(!ledger.TryPlace(null, 1, 3, false, () => true), "invalid room anchors cannot receive custom loot");
        var supply = new LimitedBudget();
        supply.Reset(3);
        supply.TryIssue("role-kit", 2, 2);
        check(ledger.TryPlace(new object (), 1, 3, false, () => supply.TryIssue("map", 3)), "map conversions use remaining natural supply after role kits");
        int before = ledger.MapAwards;
        check(!ledger.TryPlace(new object (), 1, 3, false, () => supply.TryIssue("map", 3)) && ledger.MapAwards == before && supply.Total == 3, "exhausted shared natural supply preserves further vanilla pickups");
        check(!ledger.TryGive("third", 3, 1, () => supply.TryIssue("spawn", 3)), "random starting gear cannot bypass supply spent on roles and map loot");
        ledger.Reset();
        check(ledger.BeginMap() && ledger.BeginLife(life) && ledger.MapAwards == 0 && ledger.ExtraAwards == 0 && ledger.PlayerAwards == 0, "a new round clears map, room, life and user distribution state together");
        check(ledger.TryGive("user", 1, 1, () => true) && ledger.TryPlace(firstRoom, 1, 1, false, () => true), "new-round users and rooms regain their configured allowances");
        var random = new Random(9137);
        var percentages = new[]
        {
            new
            {
                Id = "gas",
                Chance = 10d
            },
            new
            {
                Id = "blocked",
                Chance = 20d
            }
        };
        var shares = Enumerable.Range(0, 10000).Select(i => DistributionRules.ChoosePercent(percentages, p => p.Chance, i / 100.0)?.Id ?? "native").GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());
        check(shares["gas"] == 1000 && shares["blocked"] == 2000 && shares["native"] == 7000, "item percentages reserve exact shares and leave unused/blocked rolls native instead of redistributing them");
        check(DistributionRules.ChoosePercent(new[] { new { Chance = 0d }, new { Chance = 100d } }, p => p.Chance, 99.999).Chance == 100, "zero-percent loot cannot win while a single 100-percent rule covers its full pool");
        check(DistributionRules.LegacyPercent(25, 3, 8) == 9.37 && DistributionRules.LegacyPercent(0, 3, 8) == 0 && DistributionRules.LegacyPercent(25, 1, 0) == 0, "old loot gates/weights convert to bounded two-decimal percentages including disabled legacy budgets");
        check(Enumerable.Range(0, 1000).All(_ => !DistributionRules.Roll(0, random)), "zero-percent distribution never grants loot");
        check(Enumerable.Range(0, 1000).All(_ => DistributionRules.Roll(100, random)), "one-hundred-percent distribution always reaches eligibility checks");
        check(!DistributionRules.Roll(float.NaN, random) && !DistributionRules.Roll(float.PositiveInfinity, random), "nonfinite chances cannot issue loot");
        int rolls = Enumerable.Range(0, 20000).Count(_ => DistributionRules.Roll(25, random));
        check(rolls > 4700 && rolls < 5300, "configured twenty-five-percent loot rolls retain their probability");
        var anchors = Enumerable.Range(0, 30).ToArray();
        DistributionRules.Shuffle(anchors, random);
        check(anchors.OrderBy(x => x).SequenceEqual(Enumerable.Range(0, 30)) && !anchors.SequenceEqual(Enumerable.Range(0, 30)), "shuffling spreads candidates without losing or duplicating pickup anchors");
        DistributionRules.Shuffle(Array.Empty<int>(), random);
        check(true, "a map without eligible loot safely skips distribution");
    }
}
