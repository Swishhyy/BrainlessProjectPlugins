using System;
using System.Linq;
using Brainless.Roles;

internal static class HumanProfileTests
{
    internal static void Run(Action<bool, string> check)
    {
        check(RoleLookup.Matches("S03", "Head Scientist", "chief_scientist"), "old scientist names remain usable in admin commands");
#if NATIVE_CONFIG_TESTS
        var config = new Config(); config.Prepare();
        var cowboy = config.Roles.Single(r => r.Id == "C05");
        check(cowboy.ReplaceInventory && cowboy.FillAmmoToLimit && cowboy.Add.Contains(ItemType.GunRevolver) && cowboy.Add.Contains(ItemType.ArmorLight) && cowboy.Add.Count(i => i == ItemType.Adrenaline) == 2 && cowboy.Add.Contains(ItemType.Painkillers), "Cowboy replaces every Chaos subtype kit with a revolver, full light-armor ammo, two stims and painkillers");
        check(cowboy.InventorySlots(8, 0, 0) == 6, "the replacement Cowboy kit fits even when the original inventory is full");
        var oversized = new RoleDefinition { ReplaceInventory = true, Add = Enumerable.Repeat(ItemType.Medkit, 9).ToList() };
        check(oversized.InventorySlots(8, 0, 0) > 8 && new RoleDefinition { Add = new System.Collections.Generic.List<ItemType> { ItemType.Medkit } }.InventorySlots(8, 0, 0) > 8, "inventory preflight still rejects oversized replacements and overflowing additive kits");
        check(!config.Roles.Any(r => r.Id == "G07" || r.Id == "S08") && config.Roles.Count == 114, "Rogue Guard and Rogue Scientist are no longer shipped");
        var rogues = new Config { RoleCatalogRevision = 6 };
        rogues.Roles.Add(new RoleDefinition { Id = "G07", Name = "Rogue Guard", BaseRole = PlayerRoles.RoleTypeId.FacilityGuard, Hint = "Rogue", Neutral = true, SpawnArea = RoleSpawnArea.Entrance, ChancePercent = 0 });
        rogues.Roles.Add(new RoleDefinition { Id = "S08", Name = "Rogue Scientist", BaseRole = PlayerRoles.RoleTypeId.Scientist, Hint = "Rogue", Neutral = true, SpawnArea = RoleSpawnArea.Entrance, ChancePercent = 0 });
        decimal Pool(Config c, PlayerRoles.RoleTypeId native) => c.Roles.Where(r => r.Id != "Z05" && r.Enabled && Catalog.MatchesNative(r, native)).Sum(r => (decimal)(r.ChancePercent ?? 0));
        // Make room for an 8% rogue share in each pool, as an old config would have had.
        foreach (var native in new[] { PlayerRoles.RoleTypeId.FacilityGuard, PlayerRoles.RoleTypeId.Scientist })
        {
            var top = rogues.Roles.Where(r => r.Id != "Z05" && r.MatchRoles.Count == 0 && r.BaseRole == native && r.ChancePercent > 8).First();
            top.ChancePercent -= 8;
        }

        rogues.Roles.Single(r => r.Id == "G07").ChancePercent = 8;
        rogues.Roles.Single(r => r.Id == "S08").ChancePercent = 8;
        decimal guards = Pool(rogues, PlayerRoles.RoleTypeId.FacilityGuard), scientists = Pool(rogues, PlayerRoles.RoleTypeId.Scientist);
        rogues.Prepare();
        check(!rogues.Roles.Any(r => r.Id == "G07" || r.Id == "S08"), "revision 7 removes Rogue Guard and Rogue Scientist from existing configs");
        check(Pool(rogues, PlayerRoles.RoleTypeId.FacilityGuard) <= guards && guards - Pool(rogues, PlayerRoles.RoleTypeId.FacilityGuard) < .2m && scientists - Pool(rogues, PlayerRoles.RoleTypeId.Scientist) < .2m, "the removed rogue share goes to the remaining guard and scientist profiles, so custom roles stay as common");
        check(config.Roles.Single(r => r.Id == "S07").SpawnArea == RoleSpawnArea.MicroRoom && config.Roles.Single(r => r.Id == "S07").Add.Contains(ItemType.KeycardContainmentEngineer), "Containment Engineer receives its native card and HCZ Micro-room spawn");
        check(config.Roles.Single(r => r.Id == "S03").Name == "Head Scientist" && config.Roles.Single(r => r.Id == "S03").Add.Contains(ItemType.KeycardResearchCoordinator), "Head Scientist reuses the existing S03 profile and native research-coordinator card");
        var old = new Config { RoleCatalogRevision = 1, UseRarity = false };
        old.Roles.RemoveAll(r => r.Id == "C05" || r.Id == "G07" || r.Id == "S07" || r.Id == "S08");
        foreach (var role in old.Roles.Where(r => r.Id.StartsWith("C") && Catalog.MatchesNative(r, PlayerRoles.RoleTypeId.ChaosRifleman))) role.ChancePercent = 25;
        old.Prepare();
        check(old.Roles.Count == 114 && old.RoleCatalogRevision == 13 && old.Roles.Single(r => r.Id == "C05").ChancePercent == 0 && old.Roles.Single(r => r.Id == "C02").ChancePercent == 8 && old.Roles.Where(r => r.Id != "Z05" && Catalog.MatchesNative(r, PlayerRoles.RoleTypeId.ChaosRifleman)).Sum(r => (decimal)r.ChancePercent.Value) <= 100 && !old.Roles.Any(r => Catalog.RetiredRoles.Contains(r.Id)), "revision 4 removes retired profiles and resets MTF/Chaos pools to the rank-aware defaults");
        old.Roles.RemoveAll(r => r.Id == "S07"); old.Prepare();
        check(!old.Roles.Any(r => r.Id == "S07"), "removing a new profile after migration is respected on later reloads");
        var balance = new Config { RoleCatalogRevision = 5 };
        RoleDefinition B(string id) => balance.Roles.Single(r => r.Id == id);
        B("G03").SpeedMultiplier = 1.05f; B("L04").SpeedMultiplier = .85f; B("L04").Traits.VigorGainMultiplier = 1; B("L03").Traits.VigorGainMultiplier = .5f; B("P03").SpeedMultiplier = 1.05f;
        B("R01").Health = 0; B("R01").HealthMultiplier = .7f; B("R01").Shield = 0; B("R01").ShieldMultiplier = .8f; B("X02").WidthScale = 1.3f; B("M11").Remove.Clear(); B("X01").ChancePercent = 0;
        B("C10").SpeedMultiplier = .9f; // an owner edit is kept
        balance.Prepare();
        check(balance.RoleCatalogRevision == 13 && B("G03").SpeedMultiplier == 1.02f && B("L04").SpeedMultiplier == 1 && B("L04").Traits.VigorGainMultiplier == .75f && B("L03").Traits.VigorGainMultiplier == .45f && B("P03").SpeedMultiplier == 1.12f, "revision 6 applies the playtest speed and vigor balance to untouched roles");
        check(B("R01").Health == 1400 && B("R01").Shield == 0 && B("R01").Traits.HealthRegenPerSecond == 20 && B("X02").WidthScale == 1.15f && B("M11").Remove.Contains(ItemType.GrenadeHE) && B("C10").SpeedMultiplier == .9f, "Anger Issue ends at 1,400 HP with its native shield and health regen, Big Boi is narrower, General drops the frag grenade, and owner edits are kept");
        var v7 = new Config { RoleCatalogRevision = 6 };
        RoleDefinition V(string id) => v7.Roles.Single(r => r.Id == id);
        V("B02").HealthMultiplier = .71f; V("L03").SpeedMultiplier = 1.18f; V("L04").SpeedMultiplier = .95f; V("L04").Shield = 0; V("R01").Shield = 120; V("R01").Traits.HealthRegenPerSecond = 0;
        V("M10").Health = 150; V("M10").SpeedMultiplier = 1.1f; V("M10").Add.Remove(ItemType.Adrenaline); V("M10").Hint = "Fights with SCP-1509, with 150 health and a little more speed.";
        v7.Prepare();
        check(v7.RoleCatalogRevision == 13 && V("B02").HealthMultiplier == .55f && V("L03").SpeedMultiplier == 1.08f && V("L04").SpeedMultiplier == 1 && V("L04").Shield == 200 && V("R01").Shield == 0 && V("R01").Traits.HealthRegenPerSecond == 20 && V("R01").Traits.HealthRegenDelaySeconds == 8 && V("R01").Traits.HealthRegenMax == 900, "revision 7 nerfs Escape's and Hyper, gives Poisonous normal speed with a 200 shield, and gives Anger Issue its native shield plus health regen up to 900");
        check(V("M10").Health == 200 && V("M10").SpeedMultiplier == 1.2f && V("M10").Add.Contains(ItemType.Adrenaline) && V("M10").Hint.Contains("200 health"), "revision 7 buffs the Swordsman's health, speed and kit");
        var hyper = new Config { RoleCatalogRevision = 7 };
        hyper.Roles.Single(r => r.Id == "L03").Traits.VigorGainMultiplier = .35f;
        hyper.Prepare();
        check(hyper.RoleCatalogRevision == 13 && hyper.Roles.Single(r => r.Id == "L03").Traits.VigorGainMultiplier == .45f, "revision 8 raises Hyper's untouched vigor gain from 35% to 45%");
        var hyperEdited = new Config { RoleCatalogRevision = 7 };
        hyperEdited.Roles.Single(r => r.Id == "L03").Traits.VigorGainMultiplier = .6f;
        hyperEdited.Prepare();
        check(hyperEdited.Roles.Single(r => r.Id == "L03").Traits.VigorGainMultiplier == .6f, "revision 8 keeps an owner-edited Hyper vigor gain");
        foreach (float slow in new[] { .85f, .9f, .95f })
        {
            var poisonous = new Config { RoleCatalogRevision = 8 };
            poisonous.Roles.Single(r => r.Id == "L04").SpeedMultiplier = slow;
            poisonous.Prepare();
            check(poisonous.RoleCatalogRevision == 13 && poisonous.Roles.Single(r => r.Id == "L04").SpeedMultiplier == 1, "revision 9 resets a Poisonous 106 speed of " + slow + " to normal speed");
        }

        check(new Config().Roles.Single(r => r.Id == "L04").SpeedMultiplier == 1, "a fresh Poisonous 106 moves at normal speed");
        var boom = new Config { RoleCatalogRevision = 9 };
        boom.ExplosiveScp173.Radius = 4; boom.ExplosiveScp173.HumanDamage = 100; boom.ExplosiveScp173.WarningRange = 10;
        boom.Prepare();
        check(boom.RoleCatalogRevision == 13 && boom.ExplosiveScp173.Radius == 7 && boom.ExplosiveScp173.HumanDamage == 175 && boom.ExplosiveScp173.WarningRange == 14, "revision 10 widens and strengthens Boom Boom's untouched death blast");
        var boomEdited = new Config { RoleCatalogRevision = 9 };
        boomEdited.ExplosiveScp173.Radius = 5; boomEdited.ExplosiveScp173.HumanDamage = 100;
        boomEdited.Prepare();
        check(boomEdited.ExplosiveScp173.Radius == 5 && boomEdited.ExplosiveScp173.HumanDamage == 100, "revision 10 keeps an owner-edited Boom Boom blast");
        check(new Config().ExplosiveScp173.Radius == 7 && new Config().ExplosiveScp173.HumanDamage == 175, "a fresh Boom Boom has the bigger death blast");
        var edited = new Config { RoleCatalogRevision = 6 };
        edited.Roles.Single(r => r.Id == "B02").HealthMultiplier = .8f;
        edited.Roles.Single(r => r.Id == "M10").Health = 175;
        edited.Prepare();
        check(edited.Roles.Single(r => r.Id == "B02").HealthMultiplier == .8f && edited.Roles.Single(r => r.Id == "M10").Health == 175, "revision 7 keeps owner-edited values");
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
