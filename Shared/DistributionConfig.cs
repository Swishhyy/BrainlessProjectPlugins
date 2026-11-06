using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using Exiled.API.Enums;
using PlayerRoles;

namespace Brainless.Internal
{
    public class DistributionConfig
    {
        public DistributionConfig()
        {
        }

        protected DistributionConfig(DistributionConfig defaults)
        {
            foreach (var property in typeof(DistributionConfig).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                property.SetValue(this, property.GetValue(defaults));
        }

        public bool Enabled { get; set; } = true;
        public bool MapReplacements { get; set; } = true;

        [Description("Rarity mode: each item's rarity tier sets its loot chance and how many of that tier can appear per round (see rarity). A rule's chance_percent of 0 still turns it off; weight splits a tier between items that share a source. False uses the per-rule chance_percent values.")]
        public bool UseRarity { get; set; } = true;

        [Description("Loot chance and per-round limit for each rarity tier. Items and Weapons share the per-round counts.")]
        public RarityTable Rarity { get; set; } = new RarityTable();

        [Description("Starting gear rolls at this fraction of the tier chances (map loot uses them in full).")]
        [Tune(0, 1)]
        public double StartingGearRarityScale { get; set; } = .3;

        [Description("Legacy migration budget for map rules with missing/null chance_percent. Explicit rule percentages are not multiplied by this.")]
        [Tune(0, 100)]
        public float MapChancePercent { get; set; } = 25;

        [Tune(0, 1000)]
        public int MaxMapReplacements { get; set; } = 3;

        [Tune(1, 100)]
        public int MaxMapItemsPerRoom { get; set; } = 1;

        [Tune(.5, 30)]
        public float MapDelaySeconds { get; set; } = 1.5f;

        [Description("Extra custom items scattered on open floor in random rooms (Light, Heavy and Entrance zones) each round, by player count. Each one rolls a rarity tier; a room never gets more than max_map_items_per_room.")]
        [Tune(0, 100)]
        public int FloorLootSmall { get; set; } = 6;

        [Tune(0, 100)]
        public int FloorLootMedium { get; set; } = 10;

        [Tune(0, 100)]
        public int FloorLootLarge { get; set; } = 14;

        [Tune(0, 1000)]
        public int FloorLootMediumPlayers { get; set; } = 12;

        [Tune(1, 1000)]
        public int FloorLootLargePlayers { get; set; } = 24;

        internal int FloorLoot(int players) => players < FloorLootMediumPlayers ? FloorLootSmall : players < FloorLootLargePlayers ? FloorLootMedium : FloorLootLarge;
        public bool PlayerLoadouts { get; set; } = true;

        [Description("Legacy migration budget for player rules with missing/null chance_percent. Explicit rule percentages are not multiplied by this.")]
        [Tune(0, 100)]
        public float PlayerChancePercent { get; set; } = 30;

        [Tune(0, 1000)]
        public int MaxPlayerGrants { get; set; } = 3;

        [Tune(0, 1000)]
        public int MaxGrantsPerPlayer { get; set; } = 1;

        [Tune(.5, 30)]
        public float PlayerDelaySeconds { get; set; } = .75f;
        public bool SkipPlayersWithCustomItems { get; set; } = true;
        public bool ShowSpawnHint { get; set; } = true;
        public List<SpawnReason> AllowedSpawnReasons { get; set; } = new List<SpawnReason>
        {
            SpawnReason.RoundStart,
            SpawnReason.LateJoin,
            SpawnReason.Respawn,
            SpawnReason.RespawnMiniwave
        };
        public List<MapLootRule> MapRules { get; set; } = new List<MapLootRule>();
        public List<PlayerLootRule> PlayerRules { get; set; } = new List<PlayerLootRule>();

        internal void Validate(Func<uint, bool> supported)
        {
            Tuning.Validate(this, "distribution");
            if (Rarity == null)
                throw new InvalidOperationException("distribution.rarity cannot be null.");
            Rarity.Validate("distribution.rarity");
            if (AllowedSpawnReasons == null || AllowedSpawnReasons.Any(r => !Enum.IsDefined(typeof(SpawnReason), r)) || MapRules == null || PlayerRules == null)
                throw new InvalidOperationException("Distribution lists/reasons must be valid and non-null.");
            foreach (var rule in MapRules)
            {
                if (rule == null || !supported(rule.CustomId) || rule.SourceTypes == null || rule.SourceTypes.Count == 0 || rule.SourceTypes.Any(t => t == ItemType.None || !Enum.IsDefined(typeof(ItemType), t)) || rule.Zones == null || rule.Zones.Any(z => !Enum.IsDefined(typeof(ZoneType), z)))
                    throw new InvalidOperationException("Invalid map distribution rule. Use a supported custom ID, source bodies and zones.");
                Tuning.Validate(rule, "distribution.map_rules." + rule.CustomId);
            }

            foreach (var rule in PlayerRules)
            {
                if (rule == null || !supported(rule.CustomId) || rule.Roles == null || rule.ReplaceTypes == null || rule.Roles.Any(r => !Enum.IsDefined(typeof(RoleTypeId), r)) || rule.ReplaceTypes.Any(t => t == ItemType.None || !Enum.IsDefined(typeof(ItemType), t)))
                    throw new InvalidOperationException("Invalid player distribution rule. Use a supported custom ID, role list and replacement bodies.");
                Tuning.Validate(rule, "distribution.player_rules." + rule.CustomId);
            }

            PrepareChances();
            foreach (var source in MapRules.SelectMany(r => r.SourceTypes).Distinct())
                foreach (var zone in Enum.GetValues(typeof(ZoneType)).Cast<ZoneType>().Distinct())
                    if (MapRules.Where(r => r.SourceTypes.Contains(source) && InZone(r, zone)).Sum(r => (decimal)r.ChancePercent.Value) > 100m)
                        throw new InvalidOperationException("Map chance_percent exceeds 100 for " + source + " in " + zone + ".");
            foreach (var role in Enum.GetValues(typeof(RoleTypeId)).Cast<RoleTypeId>().Distinct())
                if (PlayerRules.Where(r => ForRole(r, role)).Sum(r => (decimal)r.ChancePercent.Value) > 100m)
                    throw new InvalidOperationException("Player chance_percent exceeds 100 for " + role + ".");
        }

        private static bool InZone(MapLootRule rule, ZoneType zone) => rule.Zones.Count == 0 || rule.Zones.Contains(zone);
        private static bool ForRole(PlayerLootRule rule, RoleTypeId role) => rule.Roles.Count == 0 || rule.Roles.Contains(role);
        internal void PrepareChances()
        {
            var zones = Enum.GetValues(typeof(ZoneType)).Cast<ZoneType>().Distinct().ToArray();
            var roles = Enum.GetValues(typeof(RoleTypeId)).Cast<RoleTypeId>().Distinct().ToArray();
            foreach (var rule in MapRules.Where(r => !r.ChancePercent.HasValue))
            {
                long total = rule.SourceTypes.SelectMany(source => zones.Where(zone => InZone(rule, zone)).Select(zone => MapRules.Where(other => other.SourceTypes.Contains(source) && InZone(other, zone)).Sum(other => (long)Math.Max(0, other.Weight)))).DefaultIfEmpty(0).Max();
                rule.ChancePercent = DistributionRules.LegacyPercent(MapChancePercent, rule.Weight, total);
            }

            foreach (var rule in PlayerRules.Where(r => !r.ChancePercent.HasValue))
            {
                long total = roles.Where(role => ForRole(rule, role)).Select(role => PlayerRules.Where(other => ForRole(other, role)).Sum(other => (long)Math.Max(0, other.Weight))).DefaultIfEmpty(0).Max();
                rule.ChancePercent = DistributionRules.LegacyPercent(PlayerChancePercent, rule.Weight, total);
            }
        }

        // Retired content leaves both loot tables so its share is no longer rolled.
        internal void RemoveRetired(ICollection<uint> retired)
        {
            MapRules?.RemoveAll(r => r != null && retired.Contains(r.CustomId));
            PlayerRules?.RemoveAll(r => r != null && retired.Contains(r.CustomId));
        }

        internal void EnsureEntries(IEnumerable<KeyValuePair<uint, ItemType>> entries)
        {
            foreach (var entry in entries)
            {
                if (MapRules?.Count > 0 && !MapRules.Any(r => r != null && r.CustomId == entry.Key))
                    MapRules.Add(new MapLootRule { CustomId = entry.Key, ChancePercent = 0, Weight = 0, SourceTypes = new List<ItemType> { entry.Value } });
                if (PlayerRules?.Count > 0 && !PlayerRules.Any(r => r != null && r.CustomId == entry.Key))
                    PlayerRules.Add(new PlayerLootRule { CustomId = entry.Key, ChancePercent = 0, Weight = 0, ReplaceTypes = new List<ItemType> { entry.Value } });
            }
        }

        // Rarity mode: per source, each tier's chance is split by weight between the
        // enabled rules of that tier.
        internal string DescribeChances(Func<uint, Rarity> rarityOf, string roundStatus)
        {
            if (!UseRarity)
                return DescribeChances();
            string Map(MapLootRule r)
            {
                if (r.ChancePercent == 0 || r.Weight <= 0)
                    return r.CustomId + ": off; " + string.Join(", ", r.SourceTypes);
                var tier = rarityOf(r.CustomId);
                double tierWeight = MapRules.Where(o => o.ChancePercent != 0 && o.Weight > 0 && rarityOf(o.CustomId) == tier && o.SourceTypes.Intersect(r.SourceTypes).Any()).Sum(o => (double)o.Weight);
                return r.CustomId + ": " + tier + " ~" + RarityRules.Share(Rarity, tier, r.Weight, tierWeight).ToString("0.##") + "%; " + string.Join(", ", r.SourceTypes);
            }

            string Player(PlayerLootRule r) => r.CustomId + ": " + (r.ChancePercent == 0 || r.Weight <= 0 ? "off" : rarityOf(r.CustomId).ToString()) + "; " + (r.Roles.Count == 0 ? "any human" : string.Join(", ", r.Roles)) + "; " + (r.ReplaceTypes.Count == 0 ? "extra item" : "replaces " + string.Join(", ", r.ReplaceTypes));
            string limits = string.Join(", ", RarityRules.All.Select(t => t + " " + Rarity.Get(t).ChancePercent.ToString("0.##") + "% (max " + (Rarity.Get(t).RoundLimit == 0 ? "none" : Rarity.Get(t).RoundLimit + (Rarity.Get(t).RoundLimitMax > Rarity.Get(t).RoundLimit ? "-" + Rarity.Get(t).RoundLimitMax : "")) + ")"));
            return "Rarity tiers: " + limits + "\nThis round: " + roundStatus + "\nMap loot (chance per matching pickup):\n" + string.Join("\n", MapRules.Select(Map)) + "\nStarting gear (tier chances x" + StartingGearRarityScale.ToString("0.##") + "):\n" + string.Join("\n", PlayerRules.Select(Player));
        }

        internal string DescribeChances() => "Map loot (per matching source/zone):\n" + string.Join("\n", MapRules.Select(r => r.CustomId + ": " + r.ChancePercent.Value.ToString("0.######") + "%; " + string.Join(", ", r.SourceTypes))) + "\nStarting gear (per matching human spawn):\n" + string.Join("\n", PlayerRules.Select(r => r.CustomId + ": " + r.ChancePercent.Value.ToString("0.######") + "%; " + (r.Roles.Count == 0 ? "any human" : string.Join(", ", r.Roles)) + "; " + (r.ReplaceTypes.Count == 0 ? "extra item" : "replaces " + string.Join(", ", r.ReplaceTypes))));
        public static DistributionConfig Items() => new DistributionConfig
        {
            MaxMapReplacements = 8,
            MaxPlayerGrants = 6,
            MapRules = new List<MapLootRule>
            {
                Map(7105, 2, ItemType.KeycardJanitor, 30),
                Map(7103, 2, ItemType.GrenadeFlash, 25),
                Map(7101, 3, ItemType.GrenadeFlash, 30),
                Map(7113, 2, ItemType.GrenadeHE, 40),
                Map(7102, 1, ItemType.GrenadeHE, 25),
                Map(7133, 2, ItemType.Adrenaline, 10),
                Map(7134, 1, ItemType.GrenadeFlash, 8),
                Map(7136, 1, ItemType.GrenadeFlash, 5),
                Map(7135, 1, ItemType.Coin, 10),
                Map(7138, 1, ItemType.Coin, 10),
                Map(7137, 1, ItemType.Radio, 8),
            },
            PlayerRules = new List<PlayerLootRule>
            {
                Add(7105, 2, RoleTypeId.ClassD, 8),
                Replace(7103, 2, ItemType.GrenadeFlash, 10),
                Replace(7101, 3, ItemType.GrenadeFlash, 12),
                Replace(7113, 2, ItemType.GrenadeHE, 15),
                Replace(7102, 1, ItemType.GrenadeHE, 10),
                Replace(7133, 1, ItemType.Adrenaline, 5),
                Replace(7137, 1, ItemType.Radio, 5),
            }
        };

        // Items revision 13: the grenades and Door Jammer drop far more often. Raises
        // (never lowers) each live chance and limit; a 0% rule stays switched off.
        internal void RaiseUniqueLoot()
        {
            if (MapRules == null || PlayerRules == null || MapRules.Any(r => r == null) || PlayerRules.Any(r => r == null))
                return;
            PrepareChances();
            var stock = Items();
            var mapBefore = MapRules.ToDictionary(r => r, r => r.ChancePercent);
            var playerBefore = PlayerRules.ToDictionary(r => r, r => r.ChancePercent);
            foreach (var rule in MapRules)
            {
                double? target = stock.MapRules.FirstOrDefault(r => r.CustomId == rule.CustomId)?.ChancePercent;
                if (target.HasValue && rule.ChancePercent > 0 && rule.ChancePercent < target)
                    rule.ChancePercent = target;
            }

            foreach (var rule in PlayerRules)
            {
                double? target = stock.PlayerRules.FirstOrDefault(r => r.CustomId == rule.CustomId)?.ChancePercent;
                if (target.HasValue && rule.ChancePercent > 0 && rule.ChancePercent < target)
                    rule.ChancePercent = target;
            }

            // A heavily edited pool could pass 100% once raised: keep its old chances then.
            try
            {
                Validate(_ => true);
            }
            catch (InvalidOperationException)
            {
                foreach (var pair in mapBefore)
                    pair.Key.ChancePercent = pair.Value;
                foreach (var pair in playerBefore)
                    pair.Key.ChancePercent = pair.Value;
            }

            MaxMapReplacements = Math.Max(MaxMapReplacements, stock.MaxMapReplacements);
            MaxPlayerGrants = Math.Max(MaxPlayerGrants, stock.MaxPlayerGrants);
        }
        public static DistributionConfig Weapons() => new DistributionConfig
        {
            FloorLootSmall = 0,
            FloorLootMedium = 1,
            FloorLootLarge = 2,
            MapChancePercent = 20,
            MaxMapReplacements = 1,
            PlayerChancePercent = 15,
            MaxPlayerGrants = 1,
            PlayerDelaySeconds = 1,
            MapRules = new List<MapLootRule>
            {
                Map(7207, 1, ItemType.GunLogicer),
                Map(7209, 2, ItemType.GunCOM18)
            },
            PlayerRules = new List<PlayerLootRule>
            {
                Replace(7207, 1, ItemType.GunLogicer),
                Replace(7209, 2, ItemType.GunCOM18)
            }
        };
        private static MapLootRule Map(uint id, int weight, ItemType source, double? chance = null) => new MapLootRule
        {
            CustomId = id,
            Weight = weight,
            ChancePercent = chance,
            SourceTypes = new List<ItemType>
            {
                source
            }
        };
        private static PlayerLootRule Replace(uint id, int weight, ItemType source, double? chance = null) => new PlayerLootRule
        {
            CustomId = id,
            Weight = weight,
            ChancePercent = chance,
            ReplaceTypes = new List<ItemType>
            {
                source
            }
        };
        private static PlayerLootRule Add(uint id, int weight, RoleTypeId role, double? chance = null) => new PlayerLootRule
        {
            CustomId = id,
            Weight = weight,
            ChancePercent = chance,
            Roles = new List<RoleTypeId> { role }
        };
    }

    // Concrete per-provider constructors keep the right defaults even when an
    // operator writes only part of a distribution block in existing YAML.
    public sealed class ItemDistributionConfig : DistributionConfig
    {
        internal void UpgradeSup()
        {
            if (MapRules == null || PlayerRules == null)
                return;
            foreach (uint id in new uint[]
            {
                7114,
                7115,
                7116,
                7117,
                7122
            }

            )
            {
                AddSup(id, ItemType.SCP500, 1);
                if (PlayerRules.Count > 0 && !PlayerRules.Any(r => r.CustomId == id))
                    PlayerRules.Add(new PlayerLootRule { CustomId = id, Weight = 1, Roles = new List<RoleTypeId> { RoleTypeId.ClassD, RoleTypeId.Scientist } });
            }

            AddSup(7119, ItemType.KeycardJanitor, 1);
            AddSup(7120, ItemType.Radio, 1);
        }

        private void AddSup(uint id, ItemType body, int weight)
        {
            if (MapRules != null && MapRules.Count > 0 && !MapRules.Any(r => r.CustomId == id))
                MapRules.Add(new MapLootRule { CustomId = id, Weight = weight, SourceTypes = new List<ItemType> { body } });
            if (PlayerRules != null && PlayerRules.Count > 0 && id >= 7118 && !PlayerRules.Any(r => r.CustomId == id))
                PlayerRules.Add(new PlayerLootRule { CustomId = id, Weight = weight, ReplaceTypes = new List<ItemType> { body } });
        }

        // Items revision 15: loot rules for the general items (only where the pools are in use).
        internal void UpgradeGeneral()
        {
            var stock = Items();
            foreach (var rule in stock.MapRules.Where(r => r.CustomId >= 7133 && r.CustomId <= 7138))
                if (MapRules != null && MapRules.Count > 0 && !MapRules.Any(r => r.CustomId == rule.CustomId))
                    MapRules.Add(rule);
                else if (MapRules != null)
                    foreach (var existing in MapRules.Where(r => r.CustomId == rule.CustomId && r.ChancePercent == 0 && r.Weight == 0))
                    {
                        existing.ChancePercent = rule.ChancePercent;
                        existing.Weight = rule.Weight;
                        existing.SourceTypes = rule.SourceTypes;
                    }

            foreach (var rule in stock.PlayerRules.Where(r => r.CustomId >= 7133 && r.CustomId <= 7138))
                if (PlayerRules != null && PlayerRules.Count > 0 && !PlayerRules.Any(r => r.CustomId == rule.CustomId))
                    PlayerRules.Add(rule);
        }

        internal void UpgradeSmokeBody()
        {
            foreach (var rule in MapRules?.Where(r => r != null && r.CustomId == 7101) ?? Enumerable.Empty<MapLootRule>())
                if (rule.SourceTypes?.SequenceEqual(new[] { ItemType.GrenadeHE }) == true)
                    rule.SourceTypes = new List<ItemType>
                    {
                        ItemType.GrenadeFlash
                    };
            foreach (var rule in PlayerRules?.Where(r => r != null && r.CustomId == 7101) ?? Enumerable.Empty<PlayerLootRule>())
                if (rule.ReplaceTypes?.SequenceEqual(new[] { ItemType.GrenadeHE }) == true)
                    rule.ReplaceTypes = new List<ItemType>
                    {
                        ItemType.GrenadeFlash
                    };
        }

        internal void UpgradeDoorJammerBody()
        {
            foreach (var rule in MapRules?.Where(r => r != null && r.CustomId == 7105) ?? Enumerable.Empty<MapLootRule>())
                if (rule.SourceTypes?.SequenceEqual(new[] { ItemType.Coin }) == true)
                    rule.SourceTypes = new List<ItemType>
                    {
                        ItemType.KeycardJanitor
                    };
            foreach (var rule in PlayerRules?.Where(r => r != null && r.CustomId == 7105) ?? Enumerable.Empty<PlayerLootRule>())
                if (rule.ReplaceTypes?.SequenceEqual(new[] { ItemType.Coin }) == true)
                    rule.ReplaceTypes = new List<ItemType>
                    {
                        ItemType.KeycardJanitor
                    };
        }

        public ItemDistributionConfig() : base(Items())
        {
            UpgradeSup();
        }
    }

    public sealed class WeaponDistributionConfig : DistributionConfig
    {
        internal void UpgradeSupWeapons()
        {
            foreach (var entry in new[]
            {
                new
                {
                    Id = 7207u,
                    Body = ItemType.GunLogicer
                },
                new
                {
                    Id = 7209u,
                    Body = ItemType.GunCOM18
                }
            }

            )
            {
                if (MapRules != null && MapRules.Count > 0 && !MapRules.Any(r => r.CustomId == entry.Id))
                    MapRules.Add(new MapLootRule { CustomId = entry.Id, Weight = 1, SourceTypes = new List<ItemType> { entry.Body } });
                if (PlayerRules != null && PlayerRules.Count > 0 && !PlayerRules.Any(r => r.CustomId == entry.Id))
                    PlayerRules.Add(new PlayerLootRule { CustomId = entry.Id, Weight = 1, ReplaceTypes = new List<ItemType> { entry.Body } });
            }
        }

        public WeaponDistributionConfig() : base(Weapons())
        {
            UpgradeSupWeapons();
        }
    }

    public sealed class MapLootRule
    {
        public uint CustomId { get; set; }

        [Description("Direct percentage per matching map pickup/extra-loot anchor. Matching source/zone totals must be at most 100. Null migrates old weights; 0 disables this route.")]
        [Tune(0, 100)]
        public double? ChancePercent { get; set; }

        [Description("Legacy selection weight, used only when chance_percent is missing/null.")]
        [Tune(0, 1000000)]
        public int Weight { get; set; } = 1;
        public List<ItemType> SourceTypes { get; set; } = new List<ItemType>();
        public List<ZoneType> Zones { get; set; } = new List<ZoneType>
        {
            ZoneType.LightContainment,
            ZoneType.HeavyContainment,
            ZoneType.Entrance
        };
    }

    public sealed class PlayerLootRule
    {
        public uint CustomId { get; set; }

        [Description("Direct percentage per matching human spawn. Matching role totals must be at most 100. Null migrates old weights; 0 disables this route.")]
        [Tune(0, 100)]
        public double? ChancePercent { get; set; }

        [Description("Legacy selection weight, used only when chance_percent is missing/null.")]
        [Tune(0, 1000000)]
        public int Weight { get; set; } = 1;
        // Empty Roles means any living human; empty ReplaceTypes gives an extra item.
        public List<RoleTypeId> Roles { get; set; } = new List<RoleTypeId>();
        public List<ItemType> ReplaceTypes { get; set; } = new List<ItemType>();
    }
}
