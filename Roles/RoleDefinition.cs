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

        [Description("Chance out of 100 on a matching new spawn. Group totals must be at most 100; unused or blocked shares keep the normal role. Null uses legacy settings for migration. Doctor uses its separate event roll.")]
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

        [Tune(.01, 10)]
        public float SpeedMultiplier { get; set; } = 1;

        [Tune(.01, 100)]
        public float SprintDurationMultiplier { get; set; } = 1;

        [Tune(0, 100)]
        public float ReserveMultiplier { get; set; } = 1;

        [Tune(.1, 10)]
        public float Scale { get; set; } = 1;
        public List<ItemType> Remove { get; set; } = new List<ItemType>();
        public List<ItemType> Add { get; set; } = new List<ItemType>();
        public List<uint> CustomItems { get; set; } = new List<uint>();

        internal int InventorySlots(int current, int removed, int extra) => (ReplaceInventory ? 0 : current - removed) + Add.Count + CustomItems.Count + extra;
    }

    public static class Catalog
    {
        internal static string ChanceGroup(RoleDefinition role) => role.Id.StartsWith("M", StringComparison.OrdinalIgnoreCase) ? "MTF" : role.Id.StartsWith("C", StringComparison.OrdinalIgnoreCase) ? "Chaos" : role.BaseRole.ToString();
        internal static bool MatchesNative(RoleDefinition role, RoleTypeId type)
        {
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

        // Every chance group shares this total; the remainder keeps the plain native role.
        internal const double GroupChancePercent = 80;
        internal const float DoctorHealth = .35f;
        // Rolled once per round, on the first zombie spawn while SCP-049 is alive.
        internal const double DoctorChance = 25;
        // Kits that used removed items: Survey Beacon, Trace Chalk, Field Dressing,
        // Preserved Specimen, SCRAMBLE Goggles, Rattler, Redline SMG and Last Word.
        internal static readonly HashSet<uint> RetiredItems = new HashSet<uint> { 7107, 7108, 7109, 7112, 7121, 7202, 7204, 7205 };
        internal static readonly string[] ReworkedKits = { "S01", "S05", "S06", "G05", "M01", "M02", "C02", "C03" };
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
            r = R("D07", "Noise Complaint", RoleTypeId.ClassD, "Carries an Echo Canister to distract enemies with fake footsteps.");
            r.CustomItems.Add(7104);
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
            r = R("S06", "Anomaly Handler", RoleTypeId.Scientist, "Carries an Irritant Gas Grenade and a Filter Inhaler to stay safe in it.");
            r.Remove.Add(ItemType.Medkit);
            r.CustomItems.AddRange(new uint[] { 7101, 7111 });
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
            r = R("G03", "Trained Agent", RoleTypeId.FacilityGuard, "A faster guard with combat armor. Heavy armor removes the speed bonus.");
            r.Remove.Add(ItemType.ArmorLight);
            r.Add.AddRange(new[] { ItemType.ArmorCombat, ItemType.Flashlight });
            r.SpeedMultiplier = 1.05f;
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
            r.Add.AddRange(new[] { ItemType.Medkit, ItemType.Adrenaline });
            r.ReserveMultiplier = .75f;
            list.Add(r);
            r = R("M02", "Signal Officer", RoleTypeId.NtfPrivate, "Carries a Signal Jammer that blinds SCP-079 in a room, plus a flashbang.");
            r.Remove.Add(ItemType.GrenadeHE);
            r.Add.Add(ItemType.GrenadeFlash);
            r.CustomItems.Add(7106);
            r.ReserveMultiplier = .75f;
            list.Add(r);
            r = R("M03", "Containment Specialist", RoleTypeId.NtfPrivate, "Uses a shield-breaking rifle and slowing foam to contain SCPs.");
            r.Remove.AddRange(new[] { ItemType.GunE11SR, ItemType.GunCrossvec, ItemType.GunFRMG0, ItemType.GrenadeHE });
            r.CustomItems.AddRange(new uint[] { 7201, 7103 });
            list.Add(r);
            r = R("M04", "Breacher", RoleTypeId.NtfPrivate, "Uses a shotgun to breach doors and a flashbang to clear rooms.");
            r.Remove.AddRange(new[] { ItemType.GunE11SR, ItemType.GunCrossvec, ItemType.GunFRMG0, ItemType.GrenadeHE });
            r.Add.Add(ItemType.GrenadeFlash);
            r.CustomItems.Add(7203);
            list.Add(r);
            r = R("C01", "Saboteur", RoleTypeId.ChaosRifleman, "Carries a jammer to disrupt SCP-079, but less spare ammo.");
            r.CustomItems.Add(7106);
            r.ReserveMultiplier = .75f;
            list.Add(r);
            r = R("C02", "Courier", RoleTypeId.ChaosRifleman, "A lightly armored runner with extra stamina and an adrenaline shot.");
            r.Remove.AddRange(new[] { ItemType.ArmorCombat, ItemType.ArmorHeavy });
            r.Add.AddRange(new[] { ItemType.ArmorLight, ItemType.Adrenaline });
            r.SprintDurationMultiplier = 1.25f;
            list.Add(r);
            r = R("C03", "Interceptor", RoleTypeId.ChaosRifleman, "Swaps the AK for a Logicer machine gun to hold corridors.");
            r.Remove.Add(ItemType.GunAK);
            r.Add.Add(ItemType.GunLogicer);
            list.Add(r);
            r = R("C04", "Demolitionist", RoleTypeId.ChaosRifleman, "Carries a Cluster Grenade, but less spare ammo.");
            r.MinPlayers = 12;
            r.CustomItems.Add(7102);
            r.ReserveMultiplier = .75f;
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
            r.SpeedMultiplier = 1.05f;
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
            r = R("L03", "Hyper", RoleTypeId.Scp106, "Moves faster, but gains vigor more slowly.");
            r.SpeedMultiplier = 1.18f;
            r.Traits.VigorGainMultiplier = .5f;
            list.Add(r);
            r = R("L04", "Poisonous", RoleTypeId.Scp106, "Touches poison humans, but it walks more slowly. Medical supplies cure the poison.");
            r.HealthMultiplier = .9f;
            r.SpeedMultiplier = .85f;
            r.Traits.PoisonDamagePerSecond = 4;
            r.Traits.PoisonDurationSeconds = 5;
            list.Add(r);
            r = R("B01", "Raisian", RoleTypeId.Scp173, "Smaller and faster, but has half the normal health.");
            r.Scale = .85f;
            r.SpeedMultiplier = 1.2f;
            r.HealthMultiplier = .5f;
            r.ShieldMultiplier = .75f;
            list.Add(r);
            r = R("B02", "Escape's", RoleTypeId.Scp173, "Kills refresh breakneck speed and its cooldown.");
            r.HealthMultiplier = .9f;
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
            r = R("R01", "Anger Issue", RoleTypeId.Scp096, "Enrages immediately, but has less health and shields.");
            r.HealthMultiplier = .7f;
            r.ShieldMultiplier = .8f;
            r.Traits.RageWindupSeconds = 0;
            list.Add(r);
            r = R("R02", "Ill Temperature", RoleTypeId.Scp096, "Moves faster while calm.");
            r.Traits.CalmSpeedMultiplier = 1.25f;
            list.Add(r);
            r = R("C05", "Cowboy", RoleTypeId.ChaosRifleman, "Carries a fully supplied revolver, light armor, stims and painkillers.");
            r.ReplaceInventory = true;
            r.FillAmmoToLimit = true;
            r.Add.AddRange(new[] { ItemType.KeycardChaosInsurgency, ItemType.GunRevolver, ItemType.ArmorLight, ItemType.Adrenaline, ItemType.Adrenaline, ItemType.Painkillers });
            list.Add(r);
            r = R("G07", "Rogue Guard", RoleTypeId.FacilityGuard, "An unaffiliated guard, hostile to everyone. Can be captured and recruited.");
            r.Neutral = true;
            r.ReplaceInventory = true;
            r.SpawnArea = RoleSpawnArea.Entrance;
            r.Health = 100;
            r.Weight = 2;
            r.MaxPerRound = 1;
            r.Add.AddRange(new[] { ItemType.KeycardGuard, ItemType.GunFSP9, ItemType.ArmorLight, ItemType.Medkit, ItemType.Radio });
            r.FillAmmoToLimit = true;
            list.Add(r);
            r = R("S07", "Containment Engineer", RoleTypeId.Scientist, "Starts in HCZ's Micro room with a containment engineer keycard.");
            r.Remove.Add(ItemType.KeycardScientist);
            r.Add.Add(ItemType.KeycardContainmentEngineer);
            r.SpawnArea = RoleSpawnArea.MicroRoom;
            r.Weight = 5;
            r.MaxPerRound = 1;
            list.Add(r);
            r = R("S08", "Rogue Scientist", RoleTypeId.Scientist, "An unaffiliated scientist, hostile to everyone. Can be captured and recruited.");
            r.Neutral = true;
            r.ReplaceInventory = true;
            r.SpawnArea = RoleSpawnArea.Entrance;
            r.Health = 100;
            r.Weight = 2;
            r.MaxPerRound = 1;
            r.Add.AddRange(new[] { ItemType.KeycardScientist, ItemType.Medkit, ItemType.Flashlight });
            list.Add(r);
            FillMissingChances(list, DefaultFamilyChance);
            return list;
        }
    }
}
