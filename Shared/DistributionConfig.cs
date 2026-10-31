using System;
using System.Collections.Generic;
using System.Linq;
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

        [Tune(0, 100)]
        public float MapChancePercent { get; set; } = 25;

        [Tune(0, 1000)]
        public int MaxMapReplacements { get; set; } = 3;

        [Tune(1, 100)]
        public int MaxMapItemsPerRoom { get; set; } = 1;

        [Tune(.5, 30)]
        public float MapDelaySeconds { get; set; } = 1.5f;
        public bool PlayerLoadouts { get; set; } = true;

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
        }

        public static DistributionConfig Items() => new DistributionConfig
        {
            MapRules = new List<MapLootRule>
            {
                Map(7109, 6, ItemType.Medkit),
                Map(7111, 3, ItemType.Painkillers),
                Map(7108, 3, ItemType.Coin),
                Map(7105, 2, ItemType.KeycardJanitor),
                Map(7104, 3, ItemType.GrenadeFlash),
                Map(7103, 2, ItemType.GrenadeFlash),
                Map(7101, 3, ItemType.GrenadeHE),
                Map(7113, 2, ItemType.GrenadeHE),
                Map(7102, 1, ItemType.GrenadeHE),
                Map(7107, 1, ItemType.Flashlight),
                Map(7106, 1, ItemType.Radio),
            },
            PlayerRules = new List<PlayerLootRule>
            {
                Add(7108, 3, RoleTypeId.ClassD),
                Add(7105, 2, RoleTypeId.ClassD),
                Add(7109, 2, RoleTypeId.ClassD),
                Replace(7109, 6, ItemType.Medkit),
                Replace(7111, 2, ItemType.Painkillers),
                Replace(7104, 3, ItemType.GrenadeFlash),
                Replace(7103, 2, ItemType.GrenadeFlash),
                Replace(7101, 3, ItemType.GrenadeHE),
                Replace(7113, 2, ItemType.GrenadeHE),
                Replace(7102, 1, ItemType.GrenadeHE),
            }
        };
        public static DistributionConfig Weapons() => new DistributionConfig
        {
            MapChancePercent = 20,
            MaxMapReplacements = 1,
            PlayerChancePercent = 15,
            MaxPlayerGrants = 1,
            PlayerDelaySeconds = 1,
            MapRules = new List<MapLootRule>
            {
                Map(7201, 3, ItemType.GunE11SR),
                Map(7202, 4, ItemType.GunCrossvec),
                Map(7203, 2, ItemType.GunShotgun)
            },
            PlayerRules = new List<PlayerLootRule>
            {
                Replace(7201, 3, ItemType.GunE11SR),
                Replace(7202, 4, ItemType.GunCrossvec),
                Replace(7203, 2, ItemType.GunShotgun)
            }
        };
        private static MapLootRule Map(uint id, int weight, ItemType source) => new MapLootRule
        {
            CustomId = id,
            Weight = weight,
            SourceTypes = new List<ItemType>
            {
                source
            }
        };
        private static PlayerLootRule Replace(uint id, int weight, ItemType source) => new PlayerLootRule
        {
            CustomId = id,
            Weight = weight,
            ReplaceTypes = new List<ItemType>
            {
                source
            }
        };
        private static PlayerLootRule Add(uint id, int weight, params RoleTypeId[] roles) => new PlayerLootRule
        {
            CustomId = id,
            Weight = weight,
            Roles = roles.ToList()
        };
    }

    // Concrete per-provider constructors keep the right defaults even when an
    // operator writes only part of a distribution block in existing YAML.
    public sealed class ItemDistributionConfig : DistributionConfig
    {
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
        }
    }

    public sealed class WeaponDistributionConfig : DistributionConfig
    {
        public WeaponDistributionConfig() : base(Weapons())
        {
        }
    }

    public sealed class MapLootRule
    {
        public uint CustomId { get; set; }

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

        [Tune(0, 1000000)]
        public int Weight { get; set; } = 1;
        // Empty Roles means any living human; empty ReplaceTypes gives an extra item.
        public List<RoleTypeId> Roles { get; set; } = new List<RoleTypeId>();
        public List<ItemType> ReplaceTypes { get; set; } = new List<ItemType>();
    }
}
