using System.Collections.Generic;
using System;
using System.ComponentModel;
using System.Linq;
using Brainless.Internal;
using PlayerRoles;

namespace Brainless.Roles
{
    public enum RoleSpawnArea
    {
        Normal,
        Entrance,
        MicroRoom
    }

    public sealed class RoleDefinition
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public RoleTypeId BaseRole { get; set; }
        public string Hint { get; set; }
        public ScpTraits Traits { get; set; } = new ScpTraits();
        public bool Enabled { get; set; } = true;
        public bool Experimental { get; set; }
        public bool Neutral { get; set; }
        public bool ReplaceInventory { get; set; }
        public bool FillAmmoToLimit { get; set; }
        public RoleSpawnArea SpawnArea { get; set; }

        [Description("Rarity tier (Common, Uncommon, Rare, Epic, Legendary, Mythical). With use_rarity the tier sets how often this role rolls and the per-round limit (role_rarity).")]
        public Rarity Rarity { get; set; }

        [Description("With use_rarity: weight against other roles of the same tier and class (0 turns the role off). Without it: chance out of 100 on a matching new spawn, group totals at most 100. Null uses legacy settings for migration. Doctor uses its separate event roll.")]
        [Tune(0, 100)]
        public double? ChancePercent { get; set; }

        [Description("Legacy migration only when chance_percent is missing/null. Explicit chance_percent takes precedence.")]
        [Tune(0, int.MaxValue)]
        public int Weight { get; set; } = 10;

        [Tune(0, 1000)]
        public int MinPlayers { get; set; }

        [Tune(0, int.MaxValue)]
        public int MaxPerRound { get; set; } = 99;

        [Tune(0, 100000)]
        public float Health { get; set; }

        [Tune(.01, 100)]
        public float HealthMultiplier { get; set; } = 1;

        [Tune(0, 100)]
        public float ShieldMultiplier { get; set; } = 1;

        [Description("Fixed maximum hume shield; 0 uses shield_multiplier instead.")]
        [Tune(0, 100000)]
        public float Shield { get; set; }

        [Tune(.01, 10)]
        public float SpeedMultiplier { get; set; } = 1;

        [Tune(.01, 100)]
        public float SprintDurationMultiplier { get; set; } = 1;

        [Tune(0, 100)]
        public float ReserveMultiplier { get; set; } = 1;

        [Tune(.1, 10)]
        public float Scale { get; set; } = 1;

        [Description("Extra horizontal (width/depth) model scale on top of scale. Needs allow_model_scaling.")]
        [Tune(.5, 3)]
        public float WidthScale { get; set; } = 1;
        public List<ItemType> Remove { get; set; } = new List<ItemType>();
        public List<ItemType> Add { get; set; } = new List<ItemType>();
        public List<uint> CustomItems { get; set; } = new List<uint>();

        [Description("Native roles this profile can roll from. Empty uses the base role (or every MTF/Chaos rank for M/C profiles).")]
        public List<RoleTypeId> MatchRoles { get; set; } = new List<RoleTypeId>();

        [Description("Changes the player to this role in place when the profile is applied. None keeps the spawned role.")]
        public RoleTypeId SpawnAs { get; set; } = RoleTypeId.None;

        internal int InventorySlots(int current, int removed, int extra) => (ReplaceInventory ? 0 : current - removed) + Add.Count + CustomItems.Count + extra;
    }

    public static class Catalog
    {
        internal static string ChanceGroup(RoleDefinition role) => role.MatchRoles?.Count > 0 ? "Only:" + string.Join(",", role.MatchRoles.OrderBy(r => r)) : role.Id.StartsWith("M", StringComparison.OrdinalIgnoreCase) ? "MTF" : role.Id.StartsWith("C", StringComparison.OrdinalIgnoreCase) ? "Chaos" : role.BaseRole.ToString();
        internal static bool MatchesNative(RoleDefinition role, RoleTypeId type)
        {
            if (role.MatchRoles?.Count > 0)
                return role.MatchRoles.Contains(type);
            if (role.Id.StartsWith("M", StringComparison.OrdinalIgnoreCase))
                return type == RoleTypeId.NtfPrivate || type == RoleTypeId.NtfSergeant || type == RoleTypeId.NtfSpecialist || type == RoleTypeId.NtfCaptain;
            if (role.Id.StartsWith("C", StringComparison.OrdinalIgnoreCase))
                return type == RoleTypeId.ChaosConscript || type == RoleTypeId.ChaosRifleman || type == RoleTypeId.ChaosMarauder || type == RoleTypeId.ChaosRepressor;
            return role.BaseRole == type;
        }

        internal static void FillMissingChances(IEnumerable<RoleDefinition> roles, Func<RoleDefinition, double> legacyChance)
        {
            var list = roles.ToArray();
            foreach (var group in list.Where(r => !r.Id.Equals("Z05", StringComparison.OrdinalIgnoreCase)).GroupBy(ChanceGroup))
            {
                long total = group.Sum(r => (long)Math.Max(0, r.Weight));
                foreach (var role in group)
                    if (!role.ChancePercent.HasValue)
                        role.ChancePercent = RoleChances.FromLegacy(legacyChance(role), role.Weight, total);
            }

            foreach (var doctor in list.Where(r => r.Id.Equals("Z05", StringComparison.OrdinalIgnoreCase)))
                if (!doctor.ChancePercent.HasValue)
                    doctor.ChancePercent = 1;
        }

        // Revision 11 rarity tiers for the shipped roles.
        internal static readonly Dictionary<string, Rarity> DefaultRarities = new Dictionary<string, Rarity>(StringComparer.OrdinalIgnoreCase)
        {
            ["D01"] = Rarity.Common, ["D02"] = Rarity.Common, ["D03"] = Rarity.Uncommon, ["D05"] = Rarity.Uncommon, ["D04"] = Rarity.Rare, ["D06"] = Rarity.Rare, ["D08"] = Rarity.Epic,
            ["X01"] = Rarity.Uncommon, ["X02"] = Rarity.Rare,
            ["S02"] = Rarity.Common, ["S05"] = Rarity.Common, ["S04"] = Rarity.Uncommon, ["S06"] = Rarity.Uncommon, ["S01"] = Rarity.Rare, ["S07"] = Rarity.Rare, ["S03"] = Rarity.Epic,
            ["G02"] = Rarity.Common, ["G04"] = Rarity.Common, ["G03"] = Rarity.Uncommon, ["G05"] = Rarity.Uncommon, ["G06"] = Rarity.Rare, ["G01"] = Rarity.Epic,
            ["M01"] = Rarity.Common, ["M03"] = Rarity.Uncommon, ["M05"] = Rarity.Common, ["M06"] = Rarity.Uncommon, ["M07"] = Rarity.Rare,
            ["M08"] = Rarity.Uncommon, ["M09"] = Rarity.Rare, ["M10"] = Rarity.Epic, ["M12"] = Rarity.Uncommon, ["M11"] = Rarity.Rare, ["M13"] = Rarity.Epic, ["M14"] = Rarity.Legendary,
            ["C02"] = Rarity.Common, ["C09"] = Rarity.Common, ["C12"] = Rarity.Common, ["C04"] = Rarity.Uncommon, ["C05"] = Rarity.Uncommon, ["C08"] = Rarity.Uncommon, ["C10"] = Rarity.Rare, ["C11"] = Rarity.Rare,
            ["C06"] = Rarity.Epic, ["C07"] = Rarity.Legendary,
            ["P03"] = Rarity.Common, ["P01"] = Rarity.Uncommon, ["P02"] = Rarity.Rare,
            ["Z02"] = Rarity.Common, ["Z01"] = Rarity.Uncommon, ["Z03"] = Rarity.Uncommon, ["Z04"] = Rarity.Rare, ["Z05"] = Rarity.Legendary,
            ["R02"] = Rarity.Uncommon, ["R01"] = Rarity.Epic,
            ["L01"] = Rarity.Common, ["L02"] = Rarity.Uncommon, ["L03"] = Rarity.Uncommon, ["L04"] = Rarity.Rare,
            ["B03"] = Rarity.Common, ["B01"] = Rarity.Uncommon, ["B02"] = Rarity.Rare, ["B04"] = Rarity.Epic,
            ["H01"] = Rarity.Common, ["H02"] = Rarity.Uncommon, ["H03"] = Rarity.Uncommon, ["H04"] = Rarity.Rare,
        };

        internal static Rarity DefaultRarity(string id) => id != null && DefaultRarities.TryGetValue(id, out var rarity) ? rarity : Rarity.Common;

        // Every chance group shares this total; the remainder keeps the plain native role.
        internal const double GroupChancePercent = 80;
        internal const float DoctorHealth = .35f;
        // Rolled once per round, on the first zombie spawn while SCP-049 is alive.
        internal const double DoctorChance = 25;
        // Kits that used removed items: Survey Beacon, Trace Chalk, Field Dressing,
        // Preserved Specimen, SCRAMBLE Goggles, Rattler, Redline SMG and Last Word.
        internal static readonly HashSet<uint> RetiredItems = new HashSet<uint> { 7107, 7108, 7109, 7112, 7121, 7202, 7204, 7205 };
        internal static readonly string[] ReworkedKits = { "S01", "S05", "S06", "G05", "M01", "M02", "C02", "C03" };
        // Revision 4: profiles removed from the catalog and kits rebuilt again.
        internal static readonly HashSet<string> RetiredRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "D07", "M02", "M04", "C01", "C03" };
        // Revision 7: Rogue Guard and Rogue Scientist were removed.
        internal static readonly HashSet<string> RetiredRolesV7 = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "G07", "S08" };
        internal static readonly string[] ReworkedKitsV4 = { "S06", "M03" };
        internal static readonly HashSet<uint> RetiredItemsV4 = new HashSet<uint> { 7104, 7106, 7111, 7118, 7201, 7203, 7208 };
        internal static readonly ItemType[] Firearms = { ItemType.GunAK, ItemType.GunLogicer, ItemType.GunShotgun, ItemType.GunCrossvec, ItemType.GunA7, ItemType.GunRevolver, ItemType.GunFRMG0, ItemType.GunE11SR, ItemType.GunFSP9, ItemType.GunCOM15, ItemType.GunCOM18 };
        internal static readonly ItemType[] ScpItems = { ItemType.SCP500, ItemType.SCP207, ItemType.AntiSCP207, ItemType.SCP018, ItemType.SCP268, ItemType.SCP1853, ItemType.SCP2176, ItemType.SCP244a, ItemType.SCP244b, ItemType.SCP1576, ItemType.SCP1344 };
        internal const float EscapeHealth = .55f;
        internal static readonly RoleTypeId[] HumanRoles = { RoleTypeId.ClassD, RoleTypeId.Scientist, RoleTypeId.FacilityGuard, RoleTypeId.NtfPrivate, RoleTypeId.NtfSergeant, RoleTypeId.NtfSpecialist, RoleTypeId.NtfCaptain, RoleTypeId.ChaosConscript, RoleTypeId.ChaosRifleman, RoleTypeId.ChaosMarauder, RoleTypeId.ChaosRepressor };
        private static double DefaultFamilyChance(RoleDefinition role) => GroupChancePercent;
        private static RoleDefinition R(string id, string name, RoleTypeId baseRole, string hint) => new RoleDefinition
        {
            Id = id,
            Name = name,
            BaseRole = baseRole,
            Hint = hint
        };
        public static List<RoleDefinition> Create()
        {
            var list = new List<RoleDefinition>();
            var r = R("D01", "Athletic", RoleTypeId.ClassD, "Runs for longer, but has less health. Starts with painkillers.");
            r.Health = 90;
            r.SprintDurationMultiplier = 1.25f;
            r.Add.Add(ItemType.Painkillers);
            list.Add(r);
            r = R("D02", "Janitor", RoleTypeId.ClassD, "Carries a janitor keycard and flashlight.");
            r.Add.AddRange(new[] { ItemType.KeycardJanitor, ItemType.Flashlight });
            list.Add(r);
            r = R("D03", "Child", RoleTypeId.ClassD, "A smaller Class-D with less health and a piece of candy.");
            r.Scale = .9f;
            r.Health = 85;
            r.Add.Add(ItemType.Painkillers);
            list.Add(r);
            r = R("D04", "Thief", RoleTypeId.ClassD, "Starts with one random stolen item.");
            r.MaxPerRound = 1;
            list.Add(r);
            r = R("D05", "Survivor", RoleTypeId.ClassD, "Carries a Zone Manager keycard and painkillers, but has less health.");
            r.Health = 85;
            r.Add.AddRange(new[] { ItemType.KeycardZoneManager, ItemType.Painkillers });
            list.Add(r);
            r = R("D06", "Locksmith", RoleTypeId.ClassD, "Picks any keycard door. Higher access takes longer.");
            r.Health = 90;
            list.Add(r);
            r = R("S01", "Mad Scientist", RoleTypeId.Scientist, "An armed scientist with a revolver, a spare cylinder and a can of SCP-207.");
            r.Remove.Add(ItemType.Medkit);
            r.Add.AddRange(new[] { ItemType.GunRevolver, ItemType.SCP207 });
            r.MaxPerRound = 1;
            list.Add(r);
            r = R("S02", "Pharmacist", RoleTypeId.Scientist, "Carries extra medkits and painkillers to share.");
            r.Remove.Add(ItemType.Medkit);
            r.Add.AddRange(new[] { ItemType.Medkit, ItemType.Medkit, ItemType.Painkillers });
            list.Add(r);
            r = R("S03", "Head Scientist", RoleTypeId.Scientist, "Carries a head scientist keycard with improved research access.");
            r.Weight = 1;
            r.MinPlayers = 12;
            r.MaxPerRound = 1;
            r.Remove.AddRange(new[] { ItemType.KeycardScientist, ItemType.Medkit });
            r.Add.Add(ItemType.KeycardResearchCoordinator);
            list.Add(r);
            r = R("S04", "Candy Man", RoleTypeId.Scientist, "Carries a bag of SCP-330 candy.");
            r.Remove.Add(ItemType.Medkit);
            list.Add(r);
            r = R("S05", "Auditor", RoleTypeId.Scientist, "Carries a Zone Manager keycard for wider access and a radio.");
            r.Remove.AddRange(new[] { ItemType.Medkit, ItemType.KeycardScientist });
            r.Add.AddRange(new[] { ItemType.KeycardZoneManager, ItemType.Radio });
            list.Add(r);
            r = R("S06", "Anomaly Handler", RoleTypeId.Scientist, "Carries an Irritant Gas Grenade to cover an escape.");
            r.Remove.Add(ItemType.Medkit);
            r.CustomItems.Add(7101);
            list.Add(r);
            r = R("G01", "Guard Captain", RoleTypeId.FacilityGuard, "Leads with a Crossvec, heavy armor and better access. Carries less spare ammo.");
            r.Remove.AddRange(new[] { ItemType.GunFSP9, ItemType.ArmorLight });
            r.Add.AddRange(new[] { ItemType.GunCrossvec, ItemType.ArmorHeavy });
            r.ReserveMultiplier = .75f;
            r.MaxPerRound = 1;
            list.Add(r);
            r = R("G02", "New Hire", RoleTypeId.FacilityGuard, "A lightly equipped guard with a pistol and extra flashbangs.");
            r.Remove.AddRange(new[] { ItemType.GunFSP9, ItemType.GrenadeFlash });
            r.Add.AddRange(new[] { ItemType.GunCOM18, ItemType.GrenadeFlash, ItemType.GrenadeFlash });
            list.Add(r);
            r = R("G03", "Trained Agent", RoleTypeId.FacilityGuard, "A faster guard with combat armor.");
            r.Remove.Add(ItemType.ArmorLight);
            r.Add.AddRange(new[] { ItemType.ArmorCombat, ItemType.Flashlight });
            r.SpeedMultiplier = 1.02f;
            r.ReserveMultiplier = .75f;
            list.Add(r);
            r = R("G04", "Coward", RoleTypeId.FacilityGuard, "Sprints for longer without armor. Wearing armor removes the bonus.");
            r.Remove.Add(ItemType.ArmorLight);
            r.SprintDurationMultiplier = 1.3f;
            list.Add(r);
            r = R("G05", "Quartermaster", RoleTypeId.FacilityGuard, "Carries an extra medkit and painkillers, but less spare ammo.");
            r.Add.AddRange(new[] { ItemType.Medkit, ItemType.Painkillers });
            r.ReserveMultiplier = .75f;
            list.Add(r);
            r = R("G06", "Maintenance Liaison", RoleTypeId.FacilityGuard, "Carries a Door Jammer keycard, but less spare ammo.");
            r.Remove.Add(ItemType.GrenadeFlash);
            r.CustomItems.Add(7105);
            r.ReserveMultiplier = .75f;
            list.Add(r);
            r = R("M01", "Field Medic", RoleTypeId.NtfPrivate, "Carries an extra medkit and adrenaline for the squad, but less spare ammo.");
            r.ChancePercent = 10;
            r.Add.AddRange(new[] { ItemType.Medkit, ItemType.Adrenaline });
            r.ReserveMultiplier = .75f;
            list.Add(r);
            r = R("M03", "Containment Specialist", RoleTypeId.NtfPrivate, "Carries containment foam that covers a wide area in SCP-173's sticky mess.");
            r.Remove.Add(ItemType.GrenadeHE);
            r.CustomItems.Add(7103);
            r.ChancePercent = 10;
            list.Add(r);
            r = R("C02", "Courier", RoleTypeId.ChaosRifleman, "A lightly armored runner with extra stamina and an adrenaline shot.");
            r.Remove.AddRange(new[] { ItemType.ArmorCombat, ItemType.ArmorHeavy });
            r.Add.AddRange(new[] { ItemType.ArmorLight, ItemType.Adrenaline });
            r.SprintDurationMultiplier = 1.25f;
            r.ChancePercent = 8;
            list.Add(r);
            r = R("C04", "Demolitionist", RoleTypeId.ChaosRifleman, "Carries a Cluster Grenade, but less spare ammo.");
            r.MinPlayers = 12;
            r.CustomItems.Add(7102);
            r.ReserveMultiplier = .75f;
            r.ChancePercent = 8;
            list.Add(r);
            r = R("P01", "Surgeon", RoleTypeId.Scp049, "Revives zombies faster and recovers shields sooner, but has less health.");
            r.HealthMultiplier = .85f;
            r.ShieldMultiplier = .9f;
            r.Traits.RecallDurationMultiplier = .75f;
            r.Traits.ShieldRegenDelayMultiplier = .85f;
            list.Add(r);
            r = R("P02", "Brute", RoleTypeId.Scp049, "A tougher, slower SCP-049 with more health and shields.");
            r.HealthMultiplier = 1.5f;
            r.ShieldMultiplier = 1.1f;
            r.SpeedMultiplier = .92f;
            r.Scale = 1.1f;
            list.Add(r);
            r = R("P03", "Tickler", RoleTypeId.Scp049, "A faster SCP-049 with quicker attacks, but less health and shields.");
            r.HealthMultiplier = .85f;
            r.ShieldMultiplier = .85f;
            r.SpeedMultiplier = 1.12f;
            r.Traits.AttackCooldownMultiplier = .9f;
            list.Add(r);
            r = R("Z01", "Giant", RoleTypeId.Scp0492, "A larger, tougher zombie that moves more slowly.");
            r.HealthMultiplier = 1.5f;
            r.SpeedMultiplier = .9f;
            r.Scale = 1.15f;
            list.Add(r);
            r = R("Z02", "Muncher", RoleTypeId.Scp0492, "A small, fast zombie with less health and weaker attacks.");
            r.HealthMultiplier = .5f;
            r.SpeedMultiplier = 1.18f;
            r.Scale = .85f;
            r.Traits.AttackDamageMultiplier = .5f;
            list.Add(r);
            r = R("Z03", "Armored", RoleTypeId.Scp0492, "A heavily built zombie with more health, but less speed.");
            r.HealthMultiplier = 1.75f;
            r.SpeedMultiplier = .85f;
            list.Add(r);
            r = R("Z04", "Explosive", RoleTypeId.Scp0492, "Explodes after death. Has less health than a normal zombie.");
            r.HealthMultiplier = .75f;
            list.Add(r);
            r = R("Z05", "Doctor", RoleTypeId.Scp0492, "A weakened SCP-049 that can revive the dead. Low health, slower, and never has a hume shield.");
            r.ChancePercent = DoctorChance;
            r.Weight = 1;
            r.MinPlayers = 12;
            r.MaxPerRound = 1;
            r.HealthMultiplier = DoctorHealth;
            r.ShieldMultiplier = 0;
            r.SpeedMultiplier = .9f;
            r.Traits.AttackCooldownMultiplier = 1.4f;
            r.Traits.RecallDurationMultiplier = 1.5f;
            list.Add(r);
            r = R("L01", "Stalker", RoleTypeId.Scp106, "Quieter footsteps, but less health.");
            r.HealthMultiplier = .8f;
            r.Traits.QuietFootstepStacks = 8;
            list.Add(r);
            r = R("L02", "Wormhole", RoleTypeId.Scp106, "Gains vigor faster, but moves more slowly.");
            r.SpeedMultiplier = .9f;
            r.Traits.VigorGainMultiplier = 1.5f;
            list.Add(r);
            r = R("L03", "Hyper", RoleTypeId.Scp106, "Moves a little faster, but gains vigor more slowly.");
            r.SpeedMultiplier = 1.08f;
            r.Traits.VigorGainMultiplier = .45f;
            list.Add(r);
            r = R("L04", "Poisonous", RoleTypeId.Scp106, "Touches poison humans, but has a weaker 200 hume shield and gains vigor more slowly. Medical supplies cure the poison.");
            r.HealthMultiplier = .9f;
            r.Shield = 200;
            r.Traits.VigorGainMultiplier = .75f;
            r.Traits.PoisonDamagePerSecond = 4;
            r.Traits.PoisonDurationSeconds = 5;
            list.Add(r);
            r = R("B01", "Raisian", RoleTypeId.Scp173, "Smaller and faster, but has half the normal health.");
            r.Scale = .85f;
            r.SpeedMultiplier = 1.2f;
            r.HealthMultiplier = .5f;
            r.ShieldMultiplier = .75f;
            list.Add(r);
            r = R("B02", "Escape's", RoleTypeId.Scp173, "Kills refresh breakneck speed and its cooldown, but it has much less health.");
            r.HealthMultiplier = EscapeHealth;
            r.Traits.RefreshBreakneckOnKill = true;
            list.Add(r);
            r = R("B03", "Cocoon", RoleTypeId.Scp173, "Tougher and slower, with shields that recover more slowly.");
            r.HealthMultiplier = 1.35f;
            r.ShieldMultiplier = 1.25f;
            r.SpeedMultiplier = .82f;
            r.Traits.ShieldRegenRateMultiplier = .6f;
            r.Traits.ShieldRegenDelayMultiplier = 1.4f;
            list.Add(r);
            r = R("B04", "Boom Boom", RoleTypeId.Scp173, "Immune to explosions. Explodes after death.");
            r.HealthMultiplier = .9f;
            r.ShieldMultiplier = .9f;
            r.Traits.ExplosionImmune = true;
            list.Add(r);
            r = R("H01", "Ambusher", RoleTypeId.Scp939, "Quieter footsteps and faster movement, but less health.");
            r.HealthMultiplier = .8f;
            r.SpeedMultiplier = 1.1f;
            r.Traits.QuietFootstepStacks = 8;
            list.Add(r);
            r = R("H02", "Alpha", RoleTypeId.Scp939, "Larger and tougher, but slower.");
            r.Scale = 1.1f;
            r.HealthMultiplier = 1.2f;
            r.ShieldMultiplier = 1.1f;
            r.SpeedMultiplier = .9f;
            list.Add(r);
            r = R("H03", "Mimic", RoleTypeId.Scp939, "Reuses mimic sounds much faster, but has less health.");
            r.HealthMultiplier = .9f;
            r.Traits.MimicSoundCooldownSeconds = .25f;
            list.Add(r);
            r = R("H04", "Venomous", RoleTypeId.Scp939, "Claws and amnestic clouds poison humans. Medical supplies cure it.");
            r.HealthMultiplier = .9f;
            r.Traits.PoisonDamagePerSecond = 3;
            r.Traits.PoisonDurationSeconds = 4;
            r.Traits.PoisonInCloud = true;
            list.Add(r);
            r = R("R01", "Anger Issue", RoleTypeId.Scp096, "Enrages immediately, with 1,400 health. Heals back up to 900 after a few seconds out of combat.");
            r.Health = 1400;
            r.Traits.RageWindupSeconds = 0;
            r.Traits.HealthRegenPerSecond = 20;
            r.Traits.HealthRegenDelaySeconds = 8;
            r.Traits.HealthRegenMax = 900;
            list.Add(r);
            r = R("R02", "Ill Temperature", RoleTypeId.Scp096, "Moves faster while calm.");
            r.Traits.CalmSpeedMultiplier = 1.25f;
            list.Add(r);
            r = R("C05", "Cowboy", RoleTypeId.ChaosRifleman, "Carries a fully supplied revolver, light armor, stims and painkillers.");
            r.ReplaceInventory = true;
            r.FillAmmoToLimit = true;
            r.Add.AddRange(new[] { ItemType.KeycardChaosInsurgency, ItemType.GunRevolver, ItemType.ArmorLight, ItemType.Adrenaline, ItemType.Adrenaline, ItemType.Painkillers });
            r.ChancePercent = 8;
            list.Add(r);
            r = R("S07", "Containment Engineer", RoleTypeId.Scientist, "Starts just outside HCZ's Micro room with a containment engineer keycard.");
            r.Remove.Add(ItemType.KeycardScientist);
            r.Add.Add(ItemType.KeycardContainmentEngineer);
            r.SpawnArea = RoleSpawnArea.MicroRoom;
            r.Weight = 5;
            r.MaxPerRound = 1;
            list.Add(r);
            r = R("D08", "Captured Insurgent", RoleTypeId.ClassD, "A captured Chaos Insurgency conscript held in the Class-D cells, with no equipment.");
            r.SpawnAs = RoleTypeId.ChaosConscript;
            r.ReplaceInventory = true;
            r.MaxPerRound = 2;
            list.Add(r);
            r = R("C06", "Juggernaut", RoleTypeId.ChaosRepressor, "A Repressor with 200 health, but slower.");
            r.MatchRoles.Add(RoleTypeId.ChaosRepressor);
            r.ChancePercent = 16;
            r.Health = 200;
            r.SpeedMultiplier = .85f;
            list.Add(r);
            r = R("C07", "Squad Leader", RoleTypeId.ChaosRepressor, "Carries a keycard that opens every door except the Alpha Warhead, a COM-15, heavy armor and SCP-1853.");
            r.MatchRoles.Add(RoleTypeId.ChaosRepressor);
            r.ChancePercent = 16;
            r.ReplaceInventory = true;
            r.FillAmmoToLimit = true;
            r.Add.AddRange(new[] { ItemType.GunCOM15, ItemType.ArmorHeavy, ItemType.SCP1853 });
            list.Add(r);
            r = R("C08", "Scout", RoleTypeId.ChaosRifleman, "Trades the AK for an FSP-9 and less health for much more speed.");
            r.MatchRoles.Add(RoleTypeId.ChaosRifleman);
            r.ChancePercent = 16;
            r.Remove.Add(ItemType.GunAK);
            r.Add.Add(ItemType.GunFSP9);
            r.Health = 80;
            r.SpeedMultiplier = 1.25f;
            list.Add(r);
            r = R("C09", "Desperate", RoleTypeId.ChaosRifleman, "Spawns with a Jailbird instead of a gun.");
            r.ChancePercent = 8;
            r.Remove.AddRange(Firearms);
            r.Add.Add(ItemType.Jailbird);
            list.Add(r);
            r = R("C10", "Paranoid", RoleTypeId.ChaosRifleman, "Slower, but carries heavy armor and twice the spare ammo. May also carry SCP-1344 goggles.");
            r.MatchRoles.AddRange(new[] { RoleTypeId.ChaosRifleman, RoleTypeId.ChaosMarauder });
            r.ChancePercent = 16;
            r.Remove.AddRange(new[] { ItemType.ArmorLight, ItemType.ArmorCombat });
            r.Add.Add(ItemType.ArmorHeavy);
            r.ReserveMultiplier = 2;
            r.SpeedMultiplier = .85f;
            list.Add(r);
            r = R("C11", "SCP Handler", RoleTypeId.ChaosRifleman, "Carries one random SCP item.");
            r.ChancePercent = 8;
            list.Add(r);
            r = R("C12", "Sweet Tooth", RoleTypeId.ChaosRifleman, "Carries a bag of SCP-330 candy.");
            r.ChancePercent = 8;
            list.Add(r);
            r = R("M05", "Scout", RoleTypeId.NtfPrivate, "Trades the main gun for an FSP-9 and less health for much more speed.");
            r.MatchRoles.Add(RoleTypeId.NtfPrivate);
            r.ChancePercent = 20;
            r.Remove.AddRange(new[] { ItemType.GunCrossvec, ItemType.GunE11SR });
            r.Add.Add(ItemType.GunFSP9);
            r.Health = 80;
            r.SpeedMultiplier = 1.25f;
            list.Add(r);
            r = R("M06", "Agent", RoleTypeId.NtfPrivate, "Carries an Irritant Gas Grenade and a flashbang.");
            r.MatchRoles.Add(RoleTypeId.NtfPrivate);
            r.ChancePercent = 20;
            r.Add.Add(ItemType.GrenadeFlash);
            r.CustomItems.Add(7101);
            list.Add(r);
            r = R("M07", "Personnel", RoleTypeId.NtfPrivate, "Carries a shotgun and a Cluster Grenade.");
            r.MatchRoles.Add(RoleTypeId.NtfPrivate);
            r.ChancePercent = 20;
            r.Remove.AddRange(new[] { ItemType.GunCrossvec, ItemType.GunE11SR });
            r.Add.Add(ItemType.GunShotgun);
            r.CustomItems.Add(7102);
            list.Add(r);
            r = R("M08", "Leader", RoleTypeId.NtfSergeant, "A Sergeant with 115 health, but no grenade.");
            r.MatchRoles.Add(RoleTypeId.NtfSergeant);
            r.ChancePercent = 20;
            r.Health = 115;
            r.Remove.Add(ItemType.GrenadeHE);
            list.Add(r);
            r = R("M09", "Explosiphile", RoleTypeId.NtfSergeant, "Carries a Cluster Grenade and an Impact Grenade, with a Crossvec as the main gun.");
            r.MatchRoles.Add(RoleTypeId.NtfSergeant);
            r.ChancePercent = 20;
            r.Remove.AddRange(new[] { ItemType.GunE11SR, ItemType.GrenadeHE });
            r.Add.Add(ItemType.GunCrossvec);
            r.CustomItems.AddRange(new uint[] { 7102, 7113 });
            list.Add(r);
            r = R("M10", "Swordsman", RoleTypeId.NtfSergeant, "Fights with SCP-1509, with 200 health, more speed and an adrenaline shot to close the gap.");
            r.MatchRoles.Add(RoleTypeId.NtfSergeant);
            r.ChancePercent = 20;
            r.Remove.AddRange(new[] { ItemType.GunE11SR, ItemType.GunCrossvec });
            r.Add.AddRange(new[] { ItemType.SCP1509, ItemType.Adrenaline });
            r.Health = 200;
            r.SpeedMultiplier = 1.2f;
            list.Add(r);
            r = R("M11", "General", RoleTypeId.NtfCaptain, "A Captain with 110 health who trades the frag grenade for an Irritant Gas Grenade and a Cluster Grenade.");
            r.MatchRoles.Add(RoleTypeId.NtfCaptain);
            r.ChancePercent = 15;
            r.Health = 110;
            r.Remove.Add(ItemType.GrenadeHE);
            r.CustomItems.AddRange(new uint[] { 7101, 7102 });
            list.Add(r);
            r = R("M12", "Blubber", RoleTypeId.NtfCaptain, "Carries far more spare ammo, but moves more slowly.");
            r.MatchRoles.Add(RoleTypeId.NtfCaptain);
            r.ChancePercent = 15;
            r.ReserveMultiplier = 3;
            r.SpeedMultiplier = .85f;
            list.Add(r);
            r = R("M13", "Juggernaut", RoleTypeId.NtfCaptain, "A Captain with 200 health, but slower.");
            r.MatchRoles.Add(RoleTypeId.NtfCaptain);
            r.ChancePercent = 15;
            r.Health = 200;
            r.SpeedMultiplier = .85f;
            list.Add(r);
            r = R("M14", "Squad Leader", RoleTypeId.NtfCaptain, "Carries a keycard that opens every door except the Alpha Warhead, a COM-15, heavy armor and SCP-1853.");
            r.MatchRoles.Add(RoleTypeId.NtfCaptain);
            r.ChancePercent = 15;
            r.ReplaceInventory = true;
            r.FillAmmoToLimit = true;
            r.Add.AddRange(new[] { ItemType.GunCOM15, ItemType.ArmorHeavy, ItemType.SCP1853 });
            list.Add(r);
            r = R("X01", "Resilient", RoleTypeId.ClassD, "Any human. The first time you drop to 25% health you get a burst of speed, extra damage and artificial health.");
            r.MatchRoles.AddRange(HumanRoles);
            r.ChancePercent = 2;
            r.MaxPerRound = 1;
            list.Add(r);
            r = R("X02", "Big Boi", RoleTypeId.ClassD, "Any human. Wider, with 50% more health but slower. Medical items heal you more.");
            r.MatchRoles.AddRange(HumanRoles);
            r.ChancePercent = 3;
            r.MaxPerRound = 2;
            r.HealthMultiplier = 1.5f;
            r.SpeedMultiplier = .85f;
            r.WidthScale = 1.15f;
            list.Add(r);
            FillMissingChances(list, DefaultFamilyChance);
            foreach (var role in list)
                role.Rarity = DefaultRarity(role.Id);
            return list;
        }
    }
}
