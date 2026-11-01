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
        foreach (var role in old.Roles.Where(r => Catalog.MatchesNative(r, PlayerRoles.RoleTypeId.ChaosRifleman))) role.ChancePercent = 25;
        old.Prepare();
        check(old.Roles.Count == 53 && old.RoleCatalogRevision == 2 && old.Roles.Single(r => r.Id == "C05").ChancePercent == 0 && old.Roles.Where(r => r.Id != "C05" && Catalog.MatchesNative(r, PlayerRoles.RoleTypeId.ChaosRifleman)).All(r => r.ChancePercent == 25), "the new human catalog appends once without changing an existing full Chaos chance pool");
        old.Roles.RemoveAll(r => r.Id == "S07"); old.Prepare();
        check(!old.Roles.Any(r => r.Id == "S07"), "removing a new profile after migration is respected on later reloads");
        var tier3 = new Interactables.Interobjects.DoorUtils.KeycardLevels(3, 0, 0, false);
        var parsed = new Interactables.Interobjects.DoorUtils.KeycardLevels(tier3.Permissions);
        check(parsed.HighestLevelValue == 3 && LockpickRules.Duration(parsed.HighestLevelValue, 15, 15) == 45, "a real level-three keycard permission now produces a 45-second pick instead of being rejected");
        var commands = Brainless.Core.DiscordProtocol.Commands("pb").ToArray();
        check(commands.Length == 5 && commands.Any(c => (string)c["name"] == "pb-optout") && commands.Any(c => (string)c["name"] == "pb-playtime"), "the native Discord adapter registers stats, playtime, top-10, link and opt-out commands");
        foreach (var definition in commands.Where(c => (string)c["name"] == "pb-stats" || (string)c["name"] == "pb-playtime"))
        {
            var option = ((System.Collections.Generic.Dictionary<string, object>[])definition["options"])[0];
            check((int)option["type"] == 6 && (bool)option["required"] == false, "Discord stats/playtime use an optional native user picker for self or others");
        }
        check(Brainless.Core.DiscordProtocol.Playtime(90061) == "25h 1m", "server playtime display does not wrap back to zero after one day");
        var fixes = new Brainless.SlFixes.Config(); fixes.Validate();
        check(fixes.CaptureConversions.Enabled && fixes.CaptureConversions.MinimumAliveSeconds == 10 && fixes.CaptureConversions.ChaosRole == PlayerRoles.RoleTypeId.ChaosConscript && fixes.CaptureConversions.MtfRole == PlayerRoles.RoleTypeId.NtfPrivate, "capture conversion defaults use native opposing military ranks and a ten-second minimum life");
        fixes.CaptureConversions.MtfRole = PlayerRoles.RoleTypeId.Tutorial;
        bool invalid = false;
        try { fixes.Validate(); } catch (InvalidOperationException) { invalid = true; }
        check(invalid, "capture configuration rejects a destination outside its military faction");
#endif
    }
}
