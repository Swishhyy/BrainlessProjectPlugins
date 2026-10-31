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
            var r = R("D01", "Athletic", RoleTypeId.ClassD, "Runs for longer, but has less health. Starts with painkillers.");
            r.Health = 90;
            r.SprintDurationMultiplier = 1.25f;
            r.Add.Add(ItemType.Painkillers);
            list.Add(r);
            r = R("D02", "Janitor", RoleTypeId.ClassD, "Carries a janitor keycard and flashlight.");
            r.Add.AddRange(new[] { ItemType.KeycardJanitor, ItemType.Flashlight });
            list.Add(r);
            r = R("D03", "Child", RoleTypeId.ClassD, "A smaller Class-D with less health and a piece of candy.");
            r.Experimental = true;
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
            r = R("D06", "Locksmith", RoleTypeId.ClassD, "Picks doors below level 3. Lockpicking takes time and has a cooldown.");
            r.Health = 90;
            list.Add(r);
            r = R("D07", "Noise Complaint", RoleTypeId.ClassD, "Carries an Echo Canister to distract enemies with fake footsteps.");
            r.CustomItems.Add(7104);
            list.Add(r);
            r = R("S01", "Mad Scientist", RoleTypeId.Scientist, "An armed scientist with a six-shot pistol and an SCP specimen.");
            r.Remove.Add(ItemType.Medkit);
            r.CustomItems.AddRange(new uint[] { 7205, 7112 });
            r.MaxPerRound = 1;
            list.Add(r);
            r = R("S02", "Pharmacist", RoleTypeId.Scientist, "Carries extra medkits and painkillers to share.");
            r.Remove.Add(ItemType.Medkit);
            r.Add.AddRange(new[] { ItemType.Medkit, ItemType.Medkit, ItemType.Painkillers });
            list.Add(r);
            r = R("S03", "Chief Scientist", RoleTypeId.Scientist, "Has better research access, but no medical supplies.");
            r.Weight = 1;
            r.MinPlayers = 12;
            r.MaxPerRound = 1;
            r.Remove.AddRange(new[] { ItemType.KeycardScientist, ItemType.Medkit });
            r.Add.Add(ItemType.KeycardResearchCoordinator);
            list.Add(r);
            r = R("S04", "Candy Man", RoleTypeId.Scientist, "Carries a bag of SCP-330 candy.");
            r.Remove.Add(ItemType.Medkit);
            list.Add(r);
            r = R("S05", "Auditor", RoleTypeId.Scientist, "Uses a Survey Beacon to check a room for humans and SCPs.");
            r.Remove.Add(ItemType.Medkit);
            r.CustomItems.Add(7107);
            list.Add(r);
            r = R("S06", "Anomaly Handler", RoleTypeId.Scientist, "Carries a gas filter and a specimen that heals zombies.");
            r.Remove.Add(ItemType.Medkit);
            r.CustomItems.AddRange(new uint[] { 7111, 7112 });
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
            r = R("G05", "Quartermaster", RoleTypeId.FacilityGuard, "Carries extra bandages, but less spare ammo.");
            r.Remove.Add(ItemType.Medkit);
            r.CustomItems.AddRange(new uint[] { 7109, 7109 });
            r.ReserveMultiplier = .75f;
            list.Add(r);
            r = R("G06", "Maintenance Liaison", RoleTypeId.FacilityGuard, "Carries a Door Jammer keycard, but less spare ammo.");
            r.Remove.Add(ItemType.GrenadeFlash);
            r.CustomItems.Add(7105);
            r.ReserveMultiplier = .75f;
            list.Add(r);
            r = R("M01", "Field Medic", RoleTypeId.NtfPrivate, "Carries extra bandages to heal the squad, but less spare ammo.");
            r.Remove.Add(ItemType.Medkit);
            r.CustomItems.AddRange(new uint[] { 7109, 7109 });
            r.ReserveMultiplier = .75f;
            list.Add(r);
            r = R("M02", "Signal Officer", RoleTypeId.NtfPrivate, "Carries a room jammer and a beacon to locate enemies.");
            r.Remove.Add(ItemType.GrenadeHE);
            r.CustomItems.AddRange(new uint[] { 7106, 7107 });
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
            r = R("C02", "Courier", RoleTypeId.ChaosRifleman, "A lightly armored runner with extra stamina and trail markers.");
            r.Remove.AddRange(new[] { ItemType.ArmorCombat, ItemType.ArmorHeavy });
            r.Add.Add(ItemType.ArmorLight);
            r.SprintDurationMultiplier = 1.25f;
            r.CustomItems.Add(7108);
            list.Add(r);
            r = R("C03", "Interceptor", RoleTypeId.ChaosRifleman, "Repeated shots reveal targets with an audible tracking sound.");
            r.Remove.Add(ItemType.GunAK);
            r.CustomItems.Add(7202);
            list.Add(r);
            r = R("C04", "Demolitionist", RoleTypeId.ChaosRifleman, "Carries a Cluster Grenade, but less spare ammo.");
            r.MinPlayers = 12;
            r.CustomItems.Add(7102);
            r.ReserveMultiplier = .75f;
            list.Add(r);
            r = R("P01", "Surgeon", RoleTypeId.Scp049, "Revives zombies faster and recovers shields sooner, but has less health.");
            r.HealthMultiplier = .85f;
            r.ShieldMultiplier = .9f;
            list.Add(r);
            r = R("P02", "Brute", RoleTypeId.Scp049, "A tougher, slower SCP-049 with more health and shields.");
            r.HealthMultiplier = 1.2f;
            r.ShieldMultiplier = 1.1f;
            r.SpeedMultiplier = .92f;
            list.Add(r);
            r = R("P03", "Tickler", RoleTypeId.Scp049, "A faster SCP-049 with quicker attacks, but less health and shields.");
            r.Experimental = true;
            r.HealthMultiplier = .85f;
            r.ShieldMultiplier = .85f;
            r.SpeedMultiplier = 1.05f;
            list.Add(r);
            r = R("Z01", "Giant", RoleTypeId.Scp0492, "A larger, tougher zombie that moves more slowly.");
            r.HealthMultiplier = 1.5f;
            r.SpeedMultiplier = .9f;
            list.Add(r);
            r = R("Z02", "Muncher", RoleTypeId.Scp0492, "A small, fast zombie with less health and weaker attacks.");
            r.Experimental = true;
            r.HealthMultiplier = .5f;
            r.SpeedMultiplier = 1.18f;
            list.Add(r);
            r = R("Z03", "Armored", RoleTypeId.Scp0492, "A heavily built zombie with more health, but less speed.");
            r.HealthMultiplier = 1.75f;
            r.SpeedMultiplier = .85f;
            list.Add(r);
            r = R("Z04", "Explosive", RoleTypeId.Scp0492, "Explodes after death. Has less health than a normal zombie.");
            r.HealthMultiplier = .75f;
            list.Add(r);
            r = R("Z05", "Doctor", RoleTypeId.Scp0492, "Becomes a weaker SCP-049 who can revive one zombie.");
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
