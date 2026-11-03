using System;
using System.Linq;
using Brainless.Roles;

internal static class HumanProfileTests
{
    internal static void Run(Action<bool, string> check)
    {
        check(RoleLookup.Matches("S03", "Head Scientist", "chief_scientist") && RoleLookup.Matches("G07", "Rogue Guard", "rouge"), "old scientist names and the requested Rogue spelling remain usable in admin commands");
#if NATIVE_CONFIG_TESTS
        var config = new Config(); config.Prepare();
        var cowboy = config.Roles.Single(r => r.Id == "C05");
        check(cowboy.ReplaceInventory && cowboy.FillAmmoToLimit && cowboy.Add.Contains(ItemType.GunRevolver) && cowboy.Add.Contains(ItemType.ArmorLight) && cowboy.Add.Count(i => i == ItemType.Adrenaline) == 2 && cowboy.Add.Contains(ItemType.Painkillers), "Cowboy replaces every Chaos subtype kit with a revolver, full light-armor ammo, two stims and painkillers");
        check(cowboy.InventorySlots(8, 0, 0) == 6 && config.Roles.Single(r => r.Id == "G07").InventorySlots(8, 0, 0) == 5, "replacement Cowboy and Rogue kits fit even when the original inventory is full");
        var oversized = new RoleDefinition { ReplaceInventory = true, Add = Enumerable.Repeat(ItemType.Medkit, 9).ToList() };
        check(oversized.InventorySlots(8, 0, 0) > 8 && new RoleDefinition { Add = new System.Collections.Generic.List<ItemType> { ItemType.Medkit } }.InventorySlots(8, 0, 0) > 8, "inventory preflight still rejects oversized replacements and overflowing additive kits");
        check(new[] { "G07", "S08" }.All(id => config.Roles.Single(r => r.Id == id).Neutral && config.Roles.Single(r => r.Id == id).SpawnArea == RoleSpawnArea.Entrance), "both Rogue appearances have a neutral affiliation and separated Entrance spawns");
        check(config.Roles.Single(r => r.Id == "S07").SpawnArea == RoleSpawnArea.MicroRoom && config.Roles.Single(r => r.Id == "S07").Add.Contains(ItemType.KeycardContainmentEngineer), "Containment Engineer receives its native card and HCZ Micro-room spawn");
        check(config.Roles.Single(r => r.Id == "S03").Name == "Head Scientist" && config.Roles.Single(r => r.Id == "S03").Add.Contains(ItemType.KeycardResearchCoordinator), "Head Scientist reuses the existing S03 profile and native research-coordinator card");
        var old = new Config { RoleCatalogRevision = 1 };
        old.Roles.RemoveAll(r => r.Id == "C05" || r.Id == "G07" || r.Id == "S07" || r.Id == "S08");
        foreach (var role in old.Roles.Where(r => r.Id.StartsWith("C") && Catalog.MatchesNative(r, PlayerRoles.RoleTypeId.ChaosRifleman))) role.ChancePercent = 25;
        old.Prepare();
        check(old.Roles.Count == 68 && old.RoleCatalogRevision == 6 && old.Roles.Single(r => r.Id == "C05").ChancePercent == 0 && old.Roles.Single(r => r.Id == "C02").ChancePercent == 8 && old.Roles.Where(r => r.Id != "Z05" && Catalog.MatchesNative(r, PlayerRoles.RoleTypeId.ChaosRifleman)).Sum(r => (decimal)r.ChancePercent.Value) <= 100 && !old.Roles.Any(r => Catalog.RetiredRoles.Contains(r.Id)), "revision 4 removes retired profiles and resets MTF/Chaos pools to the rank-aware defaults");
        old.Roles.RemoveAll(r => r.Id == "S07"); old.Prepare();
        check(!old.Roles.Any(r => r.Id == "S07"), "removing a new profile after migration is respected on later reloads");
        var balance = new Config { RoleCatalogRevision = 5 };
        RoleDefinition B(string id) => balance.Roles.Single(r => r.Id == id);
        B("G03").SpeedMultiplier = 1.05f; B("L04").SpeedMultiplier = .85f; B("L04").Traits.VigorGainMultiplier = 1; B("L03").Traits.VigorGainMultiplier = .5f; B("P03").SpeedMultiplier = 1.05f;
        B("R01").Health = 0; B("R01").HealthMultiplier = .7f; B("R01").Shield = 0; B("R01").ShieldMultiplier = .8f; B("X02").WidthScale = 1.3f; B("M11").Remove.Clear(); B("X01").ChancePercent = 0;
        B("C10").SpeedMultiplier = .9f; // an owner edit is kept
        balance.Prepare();
        check(balance.RoleCatalogRevision == 6 && B("G03").SpeedMultiplier == 1.02f && B("L04").SpeedMultiplier == .95f && B("L04").Traits.VigorGainMultiplier == .75f && B("L03").Traits.VigorGainMultiplier == .35f && B("P03").SpeedMultiplier == 1.12f, "revision 6 applies the playtest speed and vigor balance to untouched roles");
        check(B("R01").Health == 1400 && B("R01").Shield == 120 && B("X02").WidthScale == 1.15f && B("M11").Remove.Contains(ItemType.GrenadeHE) && B("C10").SpeedMultiplier == .9f, "Anger Issue gets 1,400 HP and a 120 shield, Big Boi is narrower, General drops the frag grenade, and owner edits are kept");
        check(B("X01").ChancePercent > 0, "a Resilient chance zeroed by the revision 5 cap is restored when the pools have room");
        check(new Config().Roles.Single(r => r.Id == "M11").InventorySlots(7, 1, 0) == 8, "the General kit fits the Captain's seven native items without being blocked");
        var tier3 = new Interactables.Interobjects.DoorUtils.KeycardLevels(3, 0, 0, false);
        var parsed = new Interactables.Interobjects.DoorUtils.KeycardLevels(tier3.Permissions);
        check(parsed.HighestLevelValue == 3 && LockpickRules.Duration(parsed.HighestLevelValue, 15, 15) == 45, "a real level-three keycard permission now produces a 45-second pick instead of being rejected");
        var fixes = new Brainless.SlFixes.Config(); fixes.Validate();
        check(fixes.CaptureConversions.Enabled && fixes.CaptureConversions.MinimumAliveSeconds == 10 && fixes.CaptureConversions.ChaosRole == PlayerRoles.RoleTypeId.ChaosConscript && fixes.CaptureConversions.MtfRole == PlayerRoles.RoleTypeId.NtfPrivate, "capture conversion defaults use native opposing military ranks and a ten-second minimum life");
        check(fixes.EndOfRoundFriendlyFire.Enabled && fixes.EndOfRoundFriendlyFire.BroadcastSeconds == 5 && fixes.EndOfRoundFriendlyFire.Message.Contains("FRIENDLY FIRE"), "friendly fire turns on at round end by default with a short broadcast");
        fixes.CaptureConversions.MtfRole = PlayerRoles.RoleTypeId.Tutorial;
        bool invalid = false;
        try { fixes.Validate(); } catch (InvalidOperationException) { invalid = true; }
        check(invalid, "capture configuration rejects a destination outside its military faction");
#endif
    }
}
