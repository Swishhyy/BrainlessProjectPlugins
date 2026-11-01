using System;
using System.Collections.Generic;
using System.Linq;
using Brainless.Roles;

internal static class RoleChanceTests
{
    private sealed class Entry
    {
        internal string Id;
        internal double Chance;
    }

    internal static void Run(Action<bool, string> check)
    {
        var pool = new[]
        {
            new Entry
            {
                Id = "A",
                Chance = 10
            },
            new Entry
            {
                Id = "B",
                Chance = 20
            },
            new Entry
            {
                Id = "disabled",
                Chance = 5
            }
        };
        Entry Pick(double roll) => RoleChances.Choose(pool, p => p.Chance, roll);
        check(Pick(0).Id == "A" && Pick(9.999).Id == "A" && Pick(10).Id == "B" && Pick(29.999).Id == "B", "direct role percentages have exact non-overlapping roll boundaries");
        check(Pick(30).Id == "disabled" && Pick(35) == null && Pick(99.999) == null, "blocked-profile shares stay reserved and unused percentage leaves the native role");
        var counts = Enumerable.Range(0, 10000).Select(i => Pick(i / 100.0)?.Id ?? "native").GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());
        check(counts["A"] == 1000 && counts["B"] == 2000 && counts["disabled"] == 500 && counts["native"] == 6500, "a complete deterministic roll sweep preserves configured absolute percentages without normalization");
        check(RoleChances.Choose(new[] { new Entry { Chance = 0 }, new Entry { Id = "certain", Chance = 100 } }, p => p.Chance, 99.999).Id == "certain", "zero-percent profiles never win while 100 percent covers all eligible rolls");
        check(RoleChances.Choose(Array.Empty<Entry>(), p => p.Chance, 50) == null, "an empty role group keeps the native role");
        check(RoleChances.FromLegacy(45, 10, 70) == 6.42 && RoleChances.FromLegacy(45, 0, 70) == 0 && RoleChances.FromLegacy(45, 10, 0) == 0, "legacy weights convert to readable two-decimal percentages rounded down without increasing the group budget");
        bool Reject(double value)
        {
            try
            {
                RoleChances.Choose(pool, p => p.Chance, value);
                return false;
            }
            catch (ArgumentOutOfRangeException)
            {
                return true;
            }
        }

        check(new[] { -1, 100, double.NaN, double.PositiveInfinity }.All(Reject), "invalid random rolls cannot select a role accidentally");
#if NATIVE_CONFIG_TESTS
        var reader = new YamlDotNet.Serialization.DeserializerBuilder().WithNamingConvention(YamlDotNet.Serialization.NamingConventions.UnderscoredNamingConvention.Instance).Build();
        var writer = new YamlDotNet.Serialization.SerializerBuilder().WithNamingConvention(YamlDotNet.Serialization.NamingConventions.UnderscoredNamingConvention.Instance).Build();
        var config = new Config();
        config.Prepare();
        check(config.Roles.Count == 35 && config.Roles.All(r => r.ChancePercent.HasValue), "every shipped role has a numeric default percentage");
        check(config.Roles.Where(r => r.Id != "Z05").GroupBy(Catalog.ChanceGroup).All(g => g.Sum(r => (decimal)r.ChancePercent.Value) <= 100), "all default role groups fit within their 100-percent budget including experimental profiles");
        var legacy = reader.Deserialize<Config>("class_d_chance: 40\nroles:\n- id: D01\n  name: First\n  base_role: ClassD\n  weight: 1\n- id: D02\n  name: Second\n  base_role: ClassD\n  weight: 3\n");
        legacy.Prepare();
        check(legacy.Roles[0].ChancePercent == 10 && legacy.Roles[1].ChancePercent == 30, "old YAML without percentages derives its nominal family budget and relative weights");
        var explicitChance = reader.Deserialize<Config>("class_d_chance: 0\nroles:\n- id: D01\n  name: First\n  base_role: ClassD\n  weight: 0\n  chance_percent: 12.5\n");
        explicitChance.Prepare();
        check(explicitChance.Roles.Single().ChancePercent == 12.5, "explicit role percentage overrides both the legacy family gate and legacy zero weight");
        var roundtrip = reader.Deserialize<Config>(writer.Serialize(explicitChance));
        roundtrip.Prepare();
        check(roundtrip.Roles.Single().ChancePercent == 12.5, "the percentage field roundtrips through real EXILED YAML");
        var doctor = config.Roles.Single(r => r.Id == "Z05");
        doctor.ChancePercent = 100;
        config.Prepare();
        check(doctor.ChancePercent == 100 && !doctor.Enabled, "Doctor has its own configurable event chance without consuming the ordinary zombie pool or enabling the event");
        bool Invalid(Action<Config> edit)
        {
            var next = new Config(); edit(next);
            try { next.Prepare(); return false; }
            catch (InvalidOperationException) { return true; }
        }
        check(Invalid(c => c.Roles[0].ChancePercent = double.NaN) && Invalid(c => c.Roles[0].ChancePercent = -1) && Invalid(c => c.Roles[0].ChancePercent = 101), "invalid role percentages are rejected before configuration is applied");
        check(Invalid(c => { c.Roles[0].ChancePercent = 60; c.Roles[1].ChancePercent = 60; c.Roles[1].Enabled = false; }), "over-100 groups are rejected even if a conflicting profile is currently disabled");
        check(Invalid(c => c.Roles.Add(new RoleDefinition { Id = "Q01", Name = "Private only", BaseRole = PlayerRoles.RoleTypeId.NtfPrivate, ChancePercent = 100 })), "rank-specific custom profiles cannot exceed a matching MTF pool's budget through a different ID prefix");
        check(Catalog.MatchesNative(config.Roles.Single(r => r.Id == "M01"), PlayerRoles.RoleTypeId.NtfCaptain) && !Catalog.MatchesNative(config.Roles.Single(r => r.Id == "M01"), PlayerRoles.RoleTypeId.FacilityGuard), "percentage validation and live selection share the same reinforcement-rank matching rules");
        var zero = new Config();
        foreach (var role in zero.Roles) role.ChancePercent = 0;
        zero.Prepare();
        check(zero.Roles.All(r => r.ChancePercent == 0), "an explicitly all-zero roster stays zero without being regenerated or normalized");
#endif
    }
}
