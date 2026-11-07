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
        // Rarity tiers by strength: within a class, a higher tier is always the stronger
        // role. Revision 12 (re)assigns these and adds the revision 12 roles.
        internal static readonly Dictionary<string, Rarity> DefaultRarities = new Dictionary<string, Rarity>(StringComparer.OrdinalIgnoreCase)
        {
            // Class-D
            ["D01"] = Rarity.Common, ["D03"] = Rarity.Common, ["D02"] = Rarity.Uncommon, ["D04"] = Rarity.Uncommon, ["D05"] = Rarity.Rare, ["D08"] = Rarity.Uncommon, ["D06"] = Rarity.Epic, ["D09"] = Rarity.Mythical,
            // Any human
            ["X04"] = Rarity.Common, ["X02"] = Rarity.Uncommon, ["X05"] = Rarity.Uncommon, ["X01"] = Rarity.Rare, ["X06"] = Rarity.Rare, ["X07"] = Rarity.Legendary, ["X03"] = Rarity.Mythical,
            // Scientist
            ["S02"] = Rarity.Common, ["S06"] = Rarity.Common, ["S04"] = Rarity.Uncommon, ["S05"] = Rarity.Uncommon, ["S01"] = Rarity.Rare, ["S03"] = Rarity.Rare, ["S07"] = Rarity.Epic, ["S09"] = Rarity.Mythical,
            // Facility Guard
            ["G02"] = Rarity.Common, ["G04"] = Rarity.Common, ["G06"] = Rarity.Common, ["G05"] = Rarity.Uncommon, ["G03"] = Rarity.Rare, ["G01"] = Rarity.Epic, ["G08"] = Rarity.Legendary, ["G09"] = Rarity.Mythical,
            // Shared by MTF and Chaos (every rank)
            ["M01"] = Rarity.Common, ["M03"] = Rarity.Uncommon,
            // MTF / Chaos faction Mythicals (every rank of that faction)
            ["M25"] = Rarity.Mythical, ["C15"] = Rarity.Mythical,
            // Private
            ["M05"] = Rarity.Common, ["M06"] = Rarity.Uncommon, ["M15"] = Rarity.Uncommon, ["M07"] = Rarity.Rare, ["M16"] = Rarity.Legendary,
            // Sergeant
            ["M08"] = Rarity.Common, ["M17"] = Rarity.Uncommon, ["M09"] = Rarity.Rare, ["M18"] = Rarity.Rare, ["M10"] = Rarity.Epic,
            // Specialist
            ["M19"] = Rarity.Common, ["M20"] = Rarity.Uncommon, ["M21"] = Rarity.Rare, ["M22"] = Rarity.Epic, ["M23"] = Rarity.Epic,
            // Captain
            ["M12"] = Rarity.Common, ["M11"] = Rarity.Uncommon, ["M13"] = Rarity.Rare, ["M14"] = Rarity.Epic, ["M24"] = Rarity.Mythical,
            // Shared by MTF and Chaos (every rank)
            ["C02"] = Rarity.Common, ["C05"] = Rarity.Common, ["C12"] = Rarity.Common, ["C04"] = Rarity.Uncommon, ["C09"] = Rarity.Epic, ["C11"] = Rarity.Rare,
            // Chaos rank-specific
            ["C08"] = Rarity.Common, ["C10"] = Rarity.Uncommon, ["C13"] = Rarity.Epic, ["C14"] = Rarity.Legendary, ["C06"] = Rarity.Rare, ["C07"] = Rarity.Epic,
            // SCP-049
            ["P03"] = Rarity.Common, ["P06"] = Rarity.Common, ["P01"] = Rarity.Uncommon, ["P02"] = Rarity.Rare, ["P05"] = Rarity.Rare, ["P04"] = Rarity.Epic, ["P07"] = Rarity.Mythical,
            // SCP-049-2
            ["Z02"] = Rarity.Common, ["Z06"] = Rarity.Common, ["Z01"] = Rarity.Uncommon, ["Z04"] = Rarity.Uncommon, ["Z03"] = Rarity.Rare, ["Z07"] = Rarity.Rare, ["Z08"] = Rarity.Legendary, ["Z05"] = Rarity.Legendary, ["Z09"] = Rarity.Mythical,
            // SCP-096
            ["R02"] = Rarity.Common, ["R03"] = Rarity.Common, ["R04"] = Rarity.Uncommon, ["R05"] = Rarity.Rare, ["R06"] = Rarity.Epic, ["R01"] = Rarity.Legendary, ["R07"] = Rarity.Mythical,
            // SCP-106
            ["L01"] = Rarity.Common, ["L03"] = Rarity.Common, ["L02"] = Rarity.Uncommon, ["L05"] = Rarity.Rare, ["L04"] = Rarity.Rare, ["L06"] = Rarity.Rare, ["L07"] = Rarity.Epic, ["L08"] = Rarity.Mythical,
            // SCP-173
            ["B01"] = Rarity.Common, ["B05"] = Rarity.Common, ["B02"] = Rarity.Uncommon, ["B03"] = Rarity.Uncommon, ["B06"] = Rarity.Rare, ["B07"] = Rarity.Rare, ["B04"] = Rarity.Epic, ["B08"] = Rarity.Mythical,
            // SCP-939
            ["H01"] = Rarity.Common, ["H03"] = Rarity.Common, ["H05"] = Rarity.Common, ["H02"] = Rarity.Uncommon, ["H04"] = Rarity.Rare, ["H06"] = Rarity.Rare, ["H07"] = Rarity.Legendary, ["H08"] = Rarity.Mythical,
            // SCP-079
            ["K01"] = Rarity.Common, ["K02"] = Rarity.Common, ["K03"] = Rarity.Uncommon, ["K04"] = Rarity.Rare, ["K05"] = Rarity.Epic, ["K06"] = Rarity.Legendary, ["K07"] = Rarity.Mythical,
        };

        // Every-rank roles shared by MTF and Chaos (revision 13).
        internal static readonly HashSet<string> SharedFactionRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "M01", "M03", "C02", "C04", "C05", "C09", "C11", "C12" };
        internal static readonly RoleTypeId[] FactionRanks = { RoleTypeId.NtfPrivate, RoleTypeId.NtfSergeant, RoleTypeId.NtfSpecialist, RoleTypeId.NtfCaptain, RoleTypeId.ChaosConscript, RoleTypeId.ChaosRifleman, RoleTypeId.ChaosMarauder, RoleTypeId.ChaosRepressor };
        internal static readonly HashSet<string> RevisionThirteenRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "G09", "M25", "C15", "Z09", "H08", "X03", "X04", "X05", "X06", "X07" };
        internal static readonly HashSet<string> RevisionFourteenRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "K01", "K02", "K03", "K04", "K05", "K06", "K07" };
        internal const string GravediggerHint = "Has less health, but every death in the pocket dimension refills its vigor.";
        internal const string OldGravediggerHint = "Has less health, but heals 200 whenever a human dies in the pocket dimension.";
        internal const string OldGhostHint = "Mythical: silent, faster, with 20% more health and shield. Kills refresh Breakneck Speeds.";
        internal const string GhostHint = "Mythical: faster, with 20% more health and shield. Breakneck Speeds recharges 40% faster and kills refresh it.";
        internal const string CapturedHint = "A captured Chaos Insurgency conscript held in the Class-D cells, with a smuggled COM-15 (one magazine) and painkillers.";
        internal const string OldCapturedHint = "A captured Chaos Insurgency conscript held in the Class-D cells, with no equipment.";
        internal static readonly HashSet<string> RevisionTwelveRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "D09", "S09", "G08", "M15", "M16", "M17", "M18", "M19", "M20", "M21", "M22", "M23", "M24", "C13", "C14", "P04", "P05", "P06", "P07", "Z06", "Z07", "Z08", "R03", "R04", "R05", "R06", "R07", "L05", "L06", "L07", "L08", "B05", "B06", "B07", "B08", "H05", "H06", "H07" };

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
            r.Traits.QuietFootstepStacks = 9;
            list.Add(r);
            r = R("L02", "Wormhole", RoleTypeId.Scp106, "Gains vigor faster, but moves more slowly.");
            r.SpeedMultiplier = .85f;
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
            r.Traits.QuietFootstepStacks = 9;
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
            r = R("D08", "Captured Insurgent", RoleTypeId.ClassD, "A captured Chaos Insurgency conscript held in the Class-D cells, with a smuggled COM-15 (one magazine) and painkillers.");
            r.SpawnAs = RoleTypeId.ChaosConscript;
            r.ReplaceInventory = true;
            r.Add.AddRange(new[] { ItemType.GunCOM15, ItemType.Painkillers });
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
            // Revision 12 roles: every class gets at least seven. Higher rarity = stronger.
            RoleDefinition N(string id, string name, RoleTypeId baseRole, string hint, params RoleTypeId[] only)
            {
                var role = R(id, name, baseRole, hint);
                role.ChancePercent = 10;
                role.MatchRoles.AddRange(only);
                list.Add(role);
                return role;
            }

            r = N("D09", "Lucky Bastard", RoleTypeId.ClassD, "Mythical luck: 120 health, a Zone Manager keycard, SCP-500, two extra coins and the Monkey's Paw.");
            r.Health = 120;
            r.SpeedMultiplier = 1.05f;
            r.MaxPerRound = 1;
            r.Add.AddRange(new[] { ItemType.KeycardZoneManager, ItemType.SCP500, ItemType.Coin, ItemType.Coin });
            r.CustomItems.Add(7126);
            r = N("S09", "Site Director", RoleTypeId.Scientist, "Runs the site: 120 health, a Facility Manager keycard, a COM-18, a radio and SCP-500.");
            r.Health = 120;
            r.MaxPerRound = 1;
            r.FillAmmoToLimit = true;
            r.Remove.Add(ItemType.KeycardScientist);
            r.Add.AddRange(new[] { ItemType.KeycardFacilityManager, ItemType.GunCOM18, ItemType.Radio, ItemType.SCP500 });
            r = N("G08", "Riot Officer", RoleTypeId.FacilityGuard, "130 health, a shotgun and heavy armor, but a little slower.");
            r.Health = 130;
            r.SpeedMultiplier = .95f;
            r.MaxPerRound = 1;
            r.FillAmmoToLimit = true;
            r.Remove.AddRange(new[] { ItemType.GunFSP9, ItemType.ArmorLight });
            r.Add.AddRange(new[] { ItemType.GunShotgun, ItemType.ArmorHeavy });
            r = N("M15", "Breacher", RoleTypeId.NtfPrivate, "110 health and two extra flashbangs for clearing rooms.", RoleTypeId.NtfPrivate);
            r.Health = 110;
            r.Add.AddRange(new[] { ItemType.GrenadeFlash, ItemType.GrenadeFlash });
            r = N("M16", "Radio Operator", RoleTypeId.NtfPrivate, "110 health, an adrenaline shot and a Reinforcement Radio to call in backup.", RoleTypeId.NtfPrivate);
            r.Health = 110;
            r.MaxPerRound = 1;
            r.Add.Add(ItemType.Adrenaline);
            r.CustomItems.Add(7120);
            r = N("M17", "Marksman", RoleTypeId.NtfSergeant, "110 health and twice the spare ammo.", RoleTypeId.NtfSergeant);
            r.Health = 110;
            r.ReserveMultiplier = 2;
            r = N("M18", "Combat Engineer", RoleTypeId.NtfSergeant, "Trades the frag grenade for a Door Jammer and a Containment Foam Canister.", RoleTypeId.NtfSergeant);
            r.Remove.Add(ItemType.GrenadeHE);
            r.CustomItems.AddRange(new uint[] { 7105, 7103 });
            r = N("M19", "Combat Medic", RoleTypeId.NtfSpecialist, "Carries an extra medkit and painkillers, but less spare ammo.", RoleTypeId.NtfSpecialist);
            r.Add.AddRange(new[] { ItemType.Medkit, ItemType.Painkillers });
            r.ReserveMultiplier = .75f;
            r = N("M20", "Recon", RoleTypeId.NtfSpecialist, "Moves faster and sprints for longer.", RoleTypeId.NtfSpecialist);
            r.SpeedMultiplier = 1.15f;
            r.SprintDurationMultiplier = 1.2f;
            r = N("M21", "Grenadier", RoleTypeId.NtfSpecialist, "Trades the frag grenade for an Impact Grenade and a Cluster Grenade, with less spare ammo.", RoleTypeId.NtfSpecialist);
            r.Remove.Add(ItemType.GrenadeHE);
            r.CustomItems.AddRange(new uint[] { 7113, 7102 });
            r.ReserveMultiplier = .75f;
            r = N("M22", "Heavy Gunner", RoleTypeId.NtfSpecialist, "120 health, an FR-MG-0 and heavy armor, but slower.", RoleTypeId.NtfSpecialist);
            r.Health = 120;
            r.SpeedMultiplier = .9f;
            r.FillAmmoToLimit = true;
            r.Remove.AddRange(new[] { ItemType.GunE11SR, ItemType.GunCrossvec, ItemType.ArmorCombat, ItemType.ArmorLight });
            r.Add.AddRange(new[] { ItemType.GunFRMG0, ItemType.ArmorHeavy });
            r = N("M23", "Anomaly Specialist", RoleTypeId.NtfSpecialist, "110 health and two random SCP items.", RoleTypeId.NtfSpecialist);
            r.Health = 110;
            r.Traits.RandomScpItems = 2;
            r = N("M24", "Commander", RoleTypeId.NtfCaptain, "150 health, a little faster, SCP-500, a keycard that opens every door except the Alpha Warhead and a Reinforcement Radio.", RoleTypeId.NtfCaptain);
            r.Health = 150;
            r.SpeedMultiplier = 1.05f;
            r.MaxPerRound = 1;
            r.Remove.AddRange(new[] { ItemType.KeycardMTFCaptain, ItemType.Radio, ItemType.GrenadeHE });
            r.Add.Add(ItemType.SCP500);
            r.CustomItems.Add(7120);
            r = N("C13", "Smuggler", RoleTypeId.ChaosConscript, "Smuggled two random SCP items in, but no armor and a little faster.", RoleTypeId.ChaosConscript);
            r.SpeedMultiplier = 1.05f;
            r.Remove.AddRange(new[] { ItemType.ArmorLight, ItemType.ArmorCombat });
            r.Traits.RandomScpItems = 2;
            r = N("C14", "Berserker", RoleTypeId.ChaosMarauder, "130 health and a Jailbird. Every kill heals 40 health.", RoleTypeId.ChaosMarauder);
            r.Health = 130;
            r.Add.Add(ItemType.Jailbird);
            r.Traits.HealOnKill = 40;
            r = N("P04", "Plague Bearer", RoleTypeId.Scp049, "Humans who stay within 5 m are slowly poisoned. Medical supplies cure it. A little more health.");
            r.HealthMultiplier = 1.1f;
            r.Traits.PoisonAuraRadius = 5;
            r.Traits.PoisonDamagePerSecond = 3;
            r.Traits.PoisonDurationSeconds = 3;
            r = N("P05", "Conductor", RoleTypeId.Scp049, "Has less health, but the zombies it revives get 25% more health and move 10% faster.");
            r.HealthMultiplier = .85f;
            r.Traits.RevivedZombieHealthMultiplier = 1.25f;
            r.Traits.RevivedZombieSpeedPercent = 10;
            r = N("P06", "Mortician", RoleTypeId.Scp049, "Every zombie it revives comes back as a random zombie variant.");
            r.Traits.RandomZombieVariant = true;
            r = N("P07", "Necromancer", RoleTypeId.Scp049, "Mythical: 40% more health, more shield, faster revives and attacks, a little faster, and tougher zombies.");
            r.HealthMultiplier = 1.4f;
            r.ShieldMultiplier = 1.25f;
            r.SpeedMultiplier = 1.05f;
            r.Traits.RecallDurationMultiplier = .6f;
            r.Traits.AttackCooldownMultiplier = .85f;
            r.Traits.RevivedZombieHealthMultiplier = 1.15f;
            r = N("Z06", "Sprinter", RoleTypeId.Scp0492, "A very fast zombie with much less health.");
            r.HealthMultiplier = .6f;
            r.SpeedMultiplier = 1.3f;
            r = N("Z07", "Spitter", RoleTypeId.Scp0492, "Hits poison humans. Medical supplies cure it.");
            r.HealthMultiplier = .9f;
            r.Traits.PoisonDamagePerSecond = 3;
            r.Traits.PoisonDurationSeconds = 5;
            r = N("Z08", "Hulk", RoleTypeId.Scp0492, "A huge zombie with three times the health and harder hits, but slow.");
            r.HealthMultiplier = 3;
            r.SpeedMultiplier = .8f;
            r.Scale = 1.25f;
            r.Traits.AttackDamageMultiplier = 1.25f;
            r = N("R03", "Shy Guy", RoleTypeId.Scp096, "Enrages faster and moves a little faster while calm, but has less health.");
            r.HealthMultiplier = .9f;
            r.Traits.RageWindupSeconds = 3;
            r.Traits.CalmSpeedMultiplier = 1.1f;
            r = N("R04", "Prowler", RoleTypeId.Scp096, "Much faster while calm, but has less health.");
            r.HealthMultiplier = .85f;
            r.Traits.CalmSpeedMultiplier = 1.35f;
            r = N("R05", "Bulwark", RoleTypeId.Scp096, "40% more health and shield, but slower.");
            r.HealthMultiplier = 1.4f;
            r.ShieldMultiplier = 1.4f;
            r.SpeedMultiplier = .92f;
            r = N("R06", "Regenerator", RoleTypeId.Scp096, "Heals back to full health a few seconds after leaving combat.");
            r.Traits.HealthRegenPerSecond = 15;
            r.Traits.HealthRegenDelaySeconds = 6;
            r = N("R07", "Wrath", RoleTypeId.Scp096, "Mythical: enrages instantly, 25% more health, faster, and every kill adds 5 seconds of rage.");
            r.HealthMultiplier = 1.25f;
            r.SpeedMultiplier = 1.15f;
            r.Traits.RageWindupSeconds = 0;
            r.Traits.RageExtendOnKillSeconds = 5;
            r = N("L05", "Gravedigger", RoleTypeId.Scp106, "Has less health, but every death in the pocket dimension refills its vigor.");
            r.HealthMultiplier = .85f;
            r.Traits.PocketDeathVigor = true;
            r = N("L06", "Rust", RoleTypeId.Scp106, "Hits make humans bleed and have a 30% chance to destroy their armor.");
            r.Traits.BleedOnHitSeconds = 8;
            r.Traits.ArmorBreakChancePercent = 30;
            r = N("L07", "Tar Pit", RoleTypeId.Scp106, "Leaves a sticky, slowing puddle behind it every few seconds while moving.");
            r.Traits.TarTrailSeconds = 6;
            r = N("L08", "Glutton", RoleTypeId.Scp106, "Mythical: huge, with 60% more health and more shield, and every catch heals 150.");
            r.Scale = 1.2f;
            r.HealthMultiplier = 1.6f;
            r.ShieldMultiplier = 1.2f;
            r.Traits.HealOnHit = 150;
            r = N("B05", "Statue", RoleTypeId.Scp173, "More health and shield, but a little slower.");
            r.HealthMultiplier = 1.25f;
            r.ShieldMultiplier = 1.15f;
            r.SpeedMultiplier = .9f;
            r = N("B06", "Blinker", RoleTypeId.Scp173, "Breakneck Speeds recharges twice as fast.");
            r.Traits.BreakneckCooldownMultiplier = .5f;
            r = N("B07", "Tantrum", RoleTypeId.Scp173, "Its tantrum recharges much faster, and it has a little more health.");
            r.HealthMultiplier = 1.1f;
            r.Traits.TantrumCooldownMultiplier = .4f;
            r = N("B08", "Ghost", RoleTypeId.Scp173, "Mythical: faster, with 20% more health and shield. Breakneck Speeds recharges 40% faster and kills refresh it.");
            r.HealthMultiplier = 1.2f;
            r.ShieldMultiplier = 1.2f;
            r.SpeedMultiplier = 1.15f;
            r.Traits.BreakneckCooldownMultiplier = .6f;
            r.Traits.RefreshBreakneckOnKill = true;
            r = N("H05", "Runt", RoleTypeId.Scp939, "Smaller and faster, but has much less health.");
            r.Scale = .85f;
            r.SpeedMultiplier = 1.15f;
            r.HealthMultiplier = .7f;
            r = N("H06", "Hunter", RoleTypeId.Scp939, "Hits 20% harder and moves a little faster, but has less health.");
            r.HealthMultiplier = .85f;
            r.SpeedMultiplier = 1.08f;
            r.Traits.AttackDamageMultiplier = 1.2f;
            r = N("H07", "Matriarch", RoleTypeId.Scp939, "Large and tough with poison clouds, but a little slower. Medical supplies cure the poison.");
            r.Scale = 1.15f;
            r.HealthMultiplier = 1.4f;
            r.ShieldMultiplier = 1.3f;
            r.SpeedMultiplier = .9f;
            r.Traits.PoisonDamagePerSecond = 3;
            r.Traits.PoisonDurationSeconds = 4;
            r.Traits.PoisonInCloud = true;
            // Revision 13: a Mythical for every class, more any-human roles, and MTF/Chaos
            // share their every-rank roles.
            r = N("G09", "Head of Security", RoleTypeId.FacilityGuard, "Mythical: 150 health, an E-11 rifle with full ammo, heavy armor, an MTF Captain keycard and SCP-500.");
            r.Health = 150;
            r.MaxPerRound = 1;
            r.FillAmmoToLimit = true;
            r.Remove.AddRange(new[] { ItemType.GunFSP9, ItemType.ArmorLight, ItemType.KeycardGuard });
            r.Add.AddRange(new[] { ItemType.GunE11SR, ItemType.ArmorHeavy, ItemType.KeycardMTFCaptain, ItemType.SCP500 });
            r = N("M25", "Nine-Tailed Elite", RoleTypeId.NtfPrivate, "Mythical: 160 health, 10% faster, full ammo, SCP-500, SCP-1853 and adrenaline.", RoleTypeId.NtfPrivate, RoleTypeId.NtfSergeant, RoleTypeId.NtfSpecialist, RoleTypeId.NtfCaptain);
            r.Health = 160;
            r.SpeedMultiplier = 1.1f;
            r.MaxPerRound = 1;
            r.FillAmmoToLimit = true;
            r.Remove.AddRange(new[] { ItemType.Radio, ItemType.Medkit });
            r.Add.AddRange(new[] { ItemType.SCP500, ItemType.SCP1853, ItemType.Adrenaline });
            r = N("C15", "Insurgent Warlord", RoleTypeId.ChaosRifleman, "Mythical: 160 health, a Logicer with full ammo, heavy armor, a keycard that opens every door except the Alpha Warhead, SCP-500 and a medkit.", RoleTypeId.ChaosConscript, RoleTypeId.ChaosRifleman, RoleTypeId.ChaosMarauder, RoleTypeId.ChaosRepressor);
            r.Health = 160;
            r.MaxPerRound = 1;
            r.ReplaceInventory = true;
            r.FillAmmoToLimit = true;
            r.Add.AddRange(new[] { ItemType.GunLogicer, ItemType.ArmorHeavy, ItemType.SCP500, ItemType.Medkit });
            r = N("Z09", "Patient Zero", RoleTypeId.Scp0492, "Mythical: double health, 15% faster, and every hit poisons the human and heals it 50.");
            r.HealthMultiplier = 2;
            r.SpeedMultiplier = 1.15f;
            r.Traits.PoisonDamagePerSecond = 3;
            r.Traits.PoisonDurationSeconds = 5;
            r.Traits.HealOnHit = 50;
            r = N("H08", "Apex", RoleTypeId.Scp939, "Mythical: 50% more health and shield, 10% faster, hits 25% harder, silent, with poison clouds.");
            r.HealthMultiplier = 1.5f;
            r.ShieldMultiplier = 1.5f;
            r.SpeedMultiplier = 1.1f;
            r.Traits.AttackDamageMultiplier = 1.25f;
            r.Traits.QuietFootstepStacks = 10;
            r.Traits.PoisonDamagePerSecond = 3;
            r.Traits.PoisonDurationSeconds = 4;
            r.Traits.PoisonInCloud = true;
            r = N("X03", "Chosen One", RoleTypeId.ClassD, "Any human. Mythical: 150 health, 10% faster, slowly heals out of combat, and starts with SCP-500.", HumanRoles);
            r.ChancePercent = 5;
            r.Health = 150;
            r.SpeedMultiplier = 1.1f;
            r.MaxPerRound = 1;
            r.Add.Add(ItemType.SCP500);
            r.Traits.HealthRegenPerSecond = 3;
            r.Traits.HealthRegenDelaySeconds = 8;
            r = N("X04", "Lightweight", RoleTypeId.ClassD, "Any human. Smaller and 10% faster, with 85% health.", HumanRoles);
            r.ChancePercent = 5;
            r.Scale = .9f;
            r.SpeedMultiplier = 1.1f;
            r.HealthMultiplier = .85f;
            r = N("X05", "Fast Healer", RoleTypeId.ClassD, "Any human. Slowly heals back up to 75 health after 8 seconds out of combat.", HumanRoles);
            r.ChancePercent = 5;
            r.Traits.HealthRegenPerSecond = 2;
            r.Traits.HealthRegenDelaySeconds = 8;
            r.Traits.HealthRegenMax = 75;
            r = N("X06", "Glass Cannon", RoleTypeId.ClassD, "Any human. Deals 25% more damage, but has 75% health.", HumanRoles);
            r.ChancePercent = 5;
            r.HealthMultiplier = .75f;
            r.Traits.AttackDamageMultiplier = 1.25f;
            r = N("X07", "Second Wind", RoleTypeId.ClassD, "Any human. Once per life, a killing blow leaves you at 1 health with 3 seconds of invulnerability and a burst of speed.", HumanRoles);
            r.ChancePercent = 5;
            r.Traits.SecondWind = true;
            // Revision 14: SCP-079 roles (power, costs, experience, cooldowns, start tier).
            r = N("K01", "Old Hardware", RoleTypeId.Scp079, "Power regenerates 20% slower, but gains experience 25% faster.");
            r.Traits.AuxRegenMultiplier = .8f;
            r.Traits.ExperienceMultiplier = 1.25f;
            r = N("K02", "Watcher", RoleTypeId.Scp079, "Switching cameras costs 50% less power, but doors cost 25% more.");
            r.Traits.CameraCostMultiplier = .5f;
            r.Traits.DoorCostMultiplier = 1.25f;
            r = N("K03", "Overclocked", RoleTypeId.Scp079, "Power regenerates 25% faster, but every action costs 10% more.");
            r.Traits.AuxRegenMultiplier = 1.25f;
            r.Traits.AuxCostMultiplier = 1.1f;
            r = N("K04", "Electrician", RoleTypeId.Scp079, "Blackouts and lockdowns recharge 40% faster.");
            r.Traits.AbilityCooldownMultiplier = .6f;
            r = N("K05", "Gatekeeper", RoleTypeId.Scp079, "Opening and closing doors costs 50% less power.");
            r.Traits.DoorCostMultiplier = .5f;
            r = N("K06", "Head Start", RoleTypeId.Scp079, "Starts at access tier 3.");
            r.Traits.StartTier = 3;
            r = N("K07", "Mainframe", RoleTypeId.Scp079, "Mythical: starts at tier 2, power regenerates 50% faster and every action costs 25% less.");
            r.Traits.StartTier = 2;
            r.Traits.AuxRegenMultiplier = 1.5f;
            r.Traits.AuxCostMultiplier = .75f;
            foreach (var shared in list.Where(role => SharedFactionRoles.Contains(role.Id)))
            {
                shared.MatchRoles.Clear();
                shared.MatchRoles.AddRange(FactionRanks);
            }

            FillMissingChances(list, DefaultFamilyChance);
            foreach (var role in list)
                role.Rarity = DefaultRarity(role.Id);
            return list;
        }
    }
}
