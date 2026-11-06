#if NATIVE_CONFIG_TESTS
using System;
using System.Collections.Generic;
using System.Linq;
using Brainless.Internal;
using Exiled.API.Enums;
using PlayerRoles;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

internal static class LootChanceConfigTests
{
    public sealed class Holder
    {
        public ItemDistributionConfig Distribution { get; set; } = Initial();
        private static ItemDistributionConfig Initial() { var c = new ItemDistributionConfig(); c.PrepareChances(); return c; }
    }
    internal static void Run(Action<bool, string> check)
    {
        var reader = new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).Build();
        var writer = new SerializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).Build();
        bool Supported(uint id) => id >= 7101 && id <= 7122 && id != 7110;
        var old = reader.Deserialize<ItemDistributionConfig>("map_chance_percent: 40\nmap_rules:\n- custom_id: 7101\n  source_types: [GrenadeFlash]\n  weight: 1\n- custom_id: 7104\n  source_types: [GrenadeFlash]\n  weight: 3\nplayer_rules: []\n");
        old.Validate(Supported);
        check(old.MapRules[0].ChancePercent == 10 && old.MapRules[1].ChancePercent == 30, "old map rules migrate their source-specific chance/weight ratios");
        var explicitRules = reader.Deserialize<ItemDistributionConfig>("map_chance_percent: 0\nmap_rules:\n- custom_id: 7101\n  source_types: [GrenadeFlash]\n  weight: 0\n  chance_percent: 12.5\nplayer_rules: []\n");
        explicitRules.Validate(Supported);
        check(explicitRules.MapRules.Single().ChancePercent == 12.5, "explicit item percentage overrides the old global gate and zero selection weight");
        var again = reader.Deserialize<ItemDistributionConfig>(writer.Serialize(explicitRules)); again.Validate(Supported);
        check(again.MapRules.Single().ChancePercent == 12.5, "item chance_percent survives real YAML serialization");
        var holder = reader.Deserialize<Holder>("distribution:\n  map_chance_percent: 0\n  player_chance_percent: 0\n");
        holder.Distribution.Validate(Supported);
        var stockChance = DistributionConfig.Items();
        bool Shipped(uint id) => stockChance.MapRules.Any(r => r.CustomId == id && r.ChancePercent.HasValue);
        check(holder.Distribution.MapRules.Where(r => !Shipped(r.CustomId)).All(r => r.ChancePercent == 0) && holder.Distribution.PlayerRules.Where(r => !Shipped(r.CustomId)).All(r => r.ChancePercent == 0), "partial old nested YAML retains a zero budget for rules without their own percentage");
        // Items revision 13: unique loot is raised to the new chances, but a 0% rule stays off.
        var oldStock = new ItemDistributionConfig { MaxMapReplacements = 3, MaxPlayerGrants = 3 };
        foreach (var rule in oldStock.MapRules) rule.ChancePercent = null;
        foreach (var rule in oldStock.PlayerRules) rule.ChancePercent = null;
        oldStock.MapRules.Single(r => r.CustomId == 7101).ChancePercent = 0;
        oldStock.PrepareChances();
        check(oldStock.MapRules.Single(r => r.CustomId == 7102).ChancePercent < 10 && oldStock.MapRules.Single(r => r.CustomId == 7113).ChancePercent < 20, "the old stock cluster/impact chances were low");
        oldStock.RaiseUniqueLoot();
        oldStock.Validate(Supported);
        check(oldStock.MapRules.Single(r => r.CustomId == 7102).ChancePercent == 25 && oldStock.MapRules.Single(r => r.CustomId == 7113).ChancePercent == 40 && oldStock.PlayerRules.Single(r => r.CustomId == 7113).ChancePercent == 15, "revision 13 raises cluster and impact grenade chances");
        check(oldStock.MapRules.Single(r => r.CustomId == 7101).ChancePercent == 0 && oldStock.MaxMapReplacements == 8 && oldStock.MaxPlayerGrants == 6, "revision 13 keeps a disabled 0% rule off and raises the map and starting-gear limits");
        var generous = new ItemDistributionConfig();
        generous.MapRules.Single(r => r.CustomId == 7113).ChancePercent = 70;
        generous.RaiseUniqueLoot();
        check(generous.MapRules.Single(r => r.CustomId == 7113).ChancePercent == 70, "revision 13 never lowers a chance an owner already raised");
        var crowded = new ItemDistributionConfig();
        crowded.MapRules.Single(r => r.CustomId == 7113).ChancePercent = 5;
        crowded.MapRules.Single(r => r.CustomId == 7102).ChancePercent = 1;
        crowded.MapRules.Add(new MapLootRule { CustomId = 7114, ChancePercent = 90, SourceTypes = new List<ItemType> { ItemType.GrenadeHE } });
        crowded.RaiseUniqueLoot();
        check(crowded.MapRules.Single(r => r.CustomId == 7113).ChancePercent == 5 && crowded.MapRules.Single(r => r.CustomId == 7102).ChancePercent == 1, "revision 13 leaves a pool alone when raising it would pass 100%");
        var player = reader.Deserialize<ItemDistributionConfig>("map_rules: []\nplayer_chance_percent: 30\nplayer_rules:\n- custom_id: 7108\n  weight: 1\n  roles: [ClassD]\n- custom_id: 7109\n  weight: 2\n  roles: [ClassD]\n"); player.Validate(Supported);
        check(player.PlayerRules[0].ChancePercent == 10 && player.PlayerRules[1].ChancePercent == 20, "starting-gear percentages migrate independently of map loot");
        var scoped = new ItemDistributionConfig { PlayerRules = new List<PlayerLootRule>(), MapRules = new List<MapLootRule> {
            new MapLootRule { CustomId = 7101, ChancePercent = 100, SourceTypes = new List<ItemType>{ItemType.GrenadeFlash}, Zones = new List<ZoneType>{ZoneType.LightContainment} },
            new MapLootRule { CustomId = 7104, ChancePercent = 100, SourceTypes = new List<ItemType>{ItemType.GrenadeFlash}, Zones = new List<ZoneType>{ZoneType.HeavyContainment} } } };
        scoped.Validate(Supported);
        check(scoped.MapRules.All(r => r.ChancePercent == 100), "non-overlapping source/zone pools may each use 100 percent");
        bool Invalid(DistributionConfig c) { try { c.Validate(Supported); return false; } catch (InvalidOperationException) { return true; } }
        scoped.MapRules[1].Zones.Clear();
        check(Invalid(scoped), "an any-zone rule cannot push an overlapping map source over 100 percent");
        var overlap = new ItemDistributionConfig { MapRules = new List<MapLootRule>(), PlayerRules = new List<PlayerLootRule> {
            new PlayerLootRule { CustomId = 7108, ChancePercent = 60 },
            new PlayerLootRule { CustomId = 7109, ChancePercent = 60, Roles = new List<RoleTypeId>{RoleTypeId.ClassD} } } };
        check(Invalid(overlap), "starting-gear validation counts both any-human and role-specific overlapping shares");
        overlap.PlayerRules[0].ChancePercent = double.NaN;
        check(Invalid(overlap), "non-finite item percentages are rejected before applying configuration");
        var empty = new ItemDistributionConfig { MapRules = new List<MapLootRule>(), PlayerRules = new List<PlayerLootRule>() };
        empty.EnsureEntries(new[]{new KeyValuePair<uint,ItemType>(7112,ItemType.SCP500)});
        check(empty.MapRules.Count == 0 && empty.PlayerRules.Count == 0, "catalog coverage does not repopulate deliberately empty distribution routes");
        var fill = new ItemDistributionConfig();
        fill.EnsureEntries(new[]{new KeyValuePair<uint,ItemType>(7112,ItemType.SCP500),new KeyValuePair<uint,ItemType>(7106,ItemType.Radio)}); fill.Validate(Supported);
        check(fill.MapRules.Single(r=>r.CustomId==7112).ChancePercent == 0 && fill.PlayerRules.Single(r=>r.CustomId==7106).ChancePercent == 0, "previously unlisted content gets editable zero-percent entries without enabling new random drops");
    }
}
#endif

