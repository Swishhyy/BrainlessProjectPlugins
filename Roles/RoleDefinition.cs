using System.Collections.Generic;
using Brainless.Internal;
using PlayerRoles;

namespace Brainless.Roles
{
    public sealed class RoleDefinition
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public RoleTypeId BaseRole { get; set; }
        public string Hint { get; set; }
        public bool Enabled { get; set; } = true;
        public bool Experimental { get; set; }

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
    }

    public static class Catalog
    {
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
            var r = R("D01", "Athletic", RoleTypeId.ClassD, "25% longer sprint; 90 HP.");
            r.Health = 90;
            r.SprintDurationMultiplier = 1.25f;
            r.Add.Add(ItemType.Painkillers);
            list.Add(r);
            r = R("D02", "Janitor", RoleTypeId.ClassD, "Janitor card and flashlight; trade or find a route.");
            r.Add.AddRange(new[] { ItemType.KeycardJanitor, ItemType.Flashlight });
            list.Add(r);
            r = R("D03", "Child", RoleTypeId.ClassD, "Small experimental silhouette; 85 HP; approved candy only.");
            r.Experimental = true;
            r.Scale = .9f;
            r.Health = 85;
            r.Add.Add(ItemType.Painkillers);
            list.Add(r);
            r = R("D04", "Thief", RoleTypeId.ClassD, "One random contraband item.");
            r.MaxPerRound = 1;
            list.Add(r);
            r = R("D05", "Survivor", RoleTypeId.ClassD, "Zone Manager card and painkillers; 85 HP.");
            r.Health = 85;
            r.Add.AddRange(new[] { ItemType.KeycardZoneManager, ItemType.Painkillers });
            list.Add(r);
            r = R("D06", "Locksmith", RoleTypeId.ClassD, "90 HP. Interact to lockpick: 15 seconds, 120-second cooldown. Level 3 excluded.");
            r.Health = 90;
            list.Add(r);
            r = R("D07", "Noise Complaint", RoleTypeId.ClassD, "One Echo Canister; throw it to bluff a route.");
            r.CustomItems.Add(7104);
            list.Add(r);
            r = R("S01", "Mad Scientist", RoleTypeId.Scientist, "Six shots in Last Word; one specimen; no medkit.");
            r.Remove.Add(ItemType.Medkit);
            r.CustomItems.AddRange(new uint[] { 7205, 7112 });
            r.MaxPerRound = 1;
            list.Add(r);
            r = R("S02", "Pharmacist", RoleTypeId.Scientist, "Two medkits and painkillers to share.");
            r.Remove.Add(ItemType.Medkit);
            r.Add.AddRange(new[] { ItemType.Medkit, ItemType.Medkit, ItemType.Painkillers });
            list.Add(r);
            r = R("S03", "Chief Scientist", RoleTypeId.Scientist, "Research Supervisor access; no medical supply.");
            r.Weight = 1;
            r.MinPlayers = 12;
            r.MaxPerRound = 1;
            r.Remove.AddRange(new[] { ItemType.KeycardScientist, ItemType.Medkit });
            r.Add.Add(ItemType.KeycardResearchCoordinator);
            list.Add(r);
            r = R("S04", "Candy Man", RoleTypeId.Scientist, "A bag of real SCP-330 candy; candy count and colors are configurable.");
            r.Remove.Add(ItemType.Medkit);
            list.Add(r);
            r = R("S05", "Auditor", RoleTypeId.Scientist, "Survey Beacon checks one room; no medkit.");
            r.Remove.Add(ItemType.Medkit);
            r.CustomItems.Add(7107);
            list.Add(r);
            r = R("S06", "Anomaly Handler", RoleTypeId.Scientist, "Filter and specimen; no medkit or passive SCP resistance.");
            r.Remove.Add(ItemType.Medkit);
            r.CustomItems.AddRange(new uint[] { 7111, 7112 });
            list.Add(r);
            r = R("G01", "Guard Captain", RoleTypeId.FacilityGuard, "Crossvec, heavy armor, stronger badge; less reserve ammo.");
            r.Remove.AddRange(new[] { ItemType.GunFSP9, ItemType.ArmorLight });
            r.Add.AddRange(new[] { ItemType.GunCrossvec, ItemType.ArmorHeavy });
            r.ReserveMultiplier = .75f;
            r.MaxPerRound = 1;
            list.Add(r);
            r = R("G02", "New Hire", RoleTypeId.FacilityGuard, "COM-18 and two flashbangs; limited ammunition.");
            r.Remove.AddRange(new[] { ItemType.GunFSP9, ItemType.GrenadeFlash });
            r.Add.AddRange(new[] { ItemType.GunCOM18, ItemType.GrenadeFlash, ItemType.GrenadeFlash });
            list.Add(r);
            r = R("G03", "Trained Agent", RoleTypeId.FacilityGuard, "Combat armor, 5% speed, less ammo. Heavy armor suspends speed.");
            r.Remove.Add(ItemType.ArmorLight);
            r.Add.AddRange(new[] { ItemType.ArmorCombat, ItemType.Flashlight });
            r.SpeedMultiplier = 1.05f;
            r.ReserveMultiplier = .75f;
            list.Add(r);
            r = R("G04", "Coward", RoleTypeId.FacilityGuard, "30% longer sprint with no armor. Armor suspends the benefit.");
            r.Remove.Add(ItemType.ArmorLight);
            r.SprintDurationMultiplier = 1.3f;
            list.Add(r);
            r = R("G05", "Quartermaster", RoleTypeId.FacilityGuard, "Two Field Dressings instead of the medkit; less ammo.");
            r.Remove.Add(ItemType.Medkit);
            r.CustomItems.AddRange(new uint[] { 7109, 7109 });
            r.ReserveMultiplier = .75f;
            list.Add(r);
            r = R("G06", "Maintenance Liaison", RoleTypeId.FacilityGuard, "Door Brace instead of flashbang; less ammo.");
            r.Remove.Add(ItemType.GrenadeFlash);
            r.CustomItems.Add(7105);
            r.ReserveMultiplier = .75f;
            list.Add(r);
            r = R("M01", "Field Medic", RoleTypeId.NtfPrivate, "Two Field Dressings instead of medkit; less ammo.");
            r.Remove.Add(ItemType.Medkit);
            r.CustomItems.AddRange(new uint[] { 7109, 7109 });
            r.ReserveMultiplier = .75f;
            list.Add(r);
            r = R("M02", "Signal Officer", RoleTypeId.NtfPrivate, "Jammer and Survey Beacon instead of grenade; less ammo.");
            r.Remove.Add(ItemType.GrenadeHE);
            r.CustomItems.AddRange(new uint[] { 7106, 7107 });
            r.ReserveMultiplier = .75f;
            list.Add(r);
            r = R("M03", "Containment Specialist", RoleTypeId.NtfPrivate, "Nullifier and foam; weaker against human targets and SCP health.");
            r.Remove.AddRange(new[] { ItemType.GunE11SR, ItemType.GunCrossvec, ItemType.GunFRMG0, ItemType.GrenadeHE });
            r.CustomItems.AddRange(new uint[] { 7201, 7103 });
            list.Add(r);
            r = R("M04", "Breacher", RoleTypeId.NtfPrivate, "Breach Shotgun and flashbang; six shells to breach an ordinary door.");
            r.Remove.AddRange(new[] { ItemType.GunE11SR, ItemType.GunCrossvec, ItemType.GunFRMG0, ItemType.GrenadeHE });
            r.Add.Add(ItemType.GrenadeFlash);
            r.CustomItems.Add(7203);
            list.Add(r);
            r = R("C01", "Saboteur", RoleTypeId.ChaosRifleman, "Jammer replaces grenade or medkit; less ammo.");
            r.CustomItems.Add(7106);
            r.ReserveMultiplier = .75f;
            list.Add(r);
            r = R("C02", "Courier", RoleTypeId.ChaosRifleman, "Light armor, chalk, 25% longer sprint; heavier armor suspends stamina.");
            r.Remove.AddRange(new[] { ItemType.ArmorCombat, ItemType.ArmorHeavy });
            r.Add.Add(ItemType.ArmorLight);
            r.SprintDurationMultiplier = 1.25f;
            r.CustomItems.Add(7108);
            list.Add(r);
            r = R("C03", "Interceptor", RoleTypeId.ChaosRifleman, "Rattler trades damage for audible target tags.");
            r.Remove.Add(ItemType.GunAK);
            r.CustomItems.Add(7202);
            list.Add(r);
            r = R("C04", "Demolitionist", RoleTypeId.ChaosRifleman, "Cluster replaces grenade or medkit; less ammo.");
            r.MinPlayers = 12;
            r.CustomItems.Add(7102);
            r.ReserveMultiplier = .75f;
            list.Add(r);
            r = R("P01", "Surgeon", RoleTypeId.Scp049, "25% faster resurrection; lower health and shield; faster shield recovery.");
            r.HealthMultiplier = .85f;
            r.ShieldMultiplier = .9f;
            list.Add(r);
            r = R("P02", "Brute", RoleTypeId.Scp049, "20% more HP, 10% more shield; 8% slower.");
            r.HealthMultiplier = 1.2f;
            r.ShieldMultiplier = 1.1f;
            r.SpeedMultiplier = .92f;
            list.Add(r);
            r = R("P03", "Tickler", RoleTypeId.Scp049, "5% faster, 10% faster touch; 15% less health and shield.");
            r.Experimental = true;
            r.HealthMultiplier = .85f;
            r.ShieldMultiplier = .85f;
            r.SpeedMultiplier = 1.05f;
            list.Add(r);
            r = R("Z01", "Giant", RoleTypeId.Scp0492, "50% more HP; 10% slower.");
            r.HealthMultiplier = 1.5f;
            r.SpeedMultiplier = .9f;
            list.Add(r);
            r = R("Z02", "Muncher", RoleTypeId.Scp0492, "18% faster; half health and melee damage.");
            r.Experimental = true;
            r.HealthMultiplier = .5f;
            r.SpeedMultiplier = 1.18f;
            list.Add(r);
            r = R("Z03", "Armored", RoleTypeId.Scp0492, "75% more HP; 15% slower; normal melee damage.");
            r.HealthMultiplier = 1.75f;
            r.SpeedMultiplier = .85f;
            list.Add(r);
            r = R("Z04", "Explosive", RoleTypeId.Scp0492, "75% HP. Explodes after a confirmed death; warning and blast are configurable.");
            r.HealthMultiplier = .75f;
            list.Add(r);
            r = R("Z05", "Doctor", RoleTypeId.Scp0492, "Event-only SCP-049 conversion; one eligible zombie revive.");
            r.Enabled = false;
            r.Experimental = true;
            r.Weight = 1;
            r.MinPlayers = 12;
            r.MaxPerRound = 1;
            r.HealthMultiplier = .4f;
            r.ShieldMultiplier = .5f;
            list.Add(r);
            return list;
        }
    }
}
