using System;
using System.Linq;
using Brainless.Internal;

internal static class SupTests
{
    internal static void Run(Action<bool, string> check)
    {
        check(SupRules.WeightedIndex(new double[] { 0, 10, 0 }, 0) == 1 && SupRules.WeightedIndex(new double[] { 0, 10, 0 }, .999999) == 1, "expired pill zero weights never win, including probability boundaries");
        check(SupRules.WeightedIndex(new double[] { 50, 30, 20 }, .499) == 0 && SupRules.WeightedIndex(new double[] { 50, 30, 20 }, .5) == 1 && SupRules.WeightedIndex(new double[] { 50, 30, 20 }, .8) == 2, "expired pill categories use normalized weights at their exact boundaries");
        check(SupRules.WeightedIndex(new double[] { 0, 0 }, .5) == -1 && SupRules.WeightedIndex(new double[] { double.NaN, 1 }, .5) == -1 && SupRules.WeightedIndex(new double[] { -1, 1 }, .5) == -1, "invalid or empty effect pools safely select the healing fallback");
        check(SupRules.WeightedIndex(new double[] { int.MaxValue, int.MaxValue }, .75) == 1, "large pill weights do not overflow integer totals");
        check(!SupRules.InsideBeam(-1, 0, 50, .5f) && !SupRules.InsideBeam(21, 0, 20, .5f) && !SupRules.InsideBeam(10, .6f, 50, .5f), "railgun excludes targets behind the shooter, past cover or outside its beam radius");
        check(SupRules.InsideBeam(20, .5f, 20, .5f), "railgun accepts a target exactly on the visible beam boundary");
        check(SupRules.CanHack(false, false, false, false, false, 1, 2, 1, 2) && !SupRules.CanHack(false, false, false, false, false, 1, 2, 3, 2), "hacking allows level-two access while protecting level-three administration");
        check(!SupRules.CanHack(true, false, false, false, true, 0, 0, 0, 2) && !SupRules.CanHack(false, true, false, false, true, 0, 0, 0, 2) && !SupRules.CanHack(false, false, true, false, true, 0, 0, 0, 2), "hard locks, gates and elevators cannot be bypassed by hacking");
        check(!SupRules.CanHack(false, false, false, true, false, 0, 0, 0, 2) && SupRules.CanHack(false, false, false, true, true, 0, 0, 0, 2), "checkpoint hacking requires an explicit configuration opt-in");
        var battery = new BatteryLedger();
        battery.Ensure(10, 60);
        battery.Spend(10, 20);
        battery.Ensure(10, 60);
        check(battery.Remaining(10) == 40, "transferring or reacquiring a goggles serial cannot refill spent battery");
        battery.Spend(10, 100);
        battery.Ensure(10, 600);
        battery.Ensure(11, 60);
        check(battery.Remaining(10) == 0 && battery.Remaining(11) == 60, "exhausted goggles remain exhausted across config edits while other serials remain independent");
        battery.Clear();
        battery.Ensure(10, 60);
        check(battery.Remaining(10) == 60, "a round reset explicitly resets wearable battery ledgers");
        var rail = new Brainless.Weapons.RailgunTuning();
        Tuning.Validate(rail, "railgun");
        rail.ChargeSeconds = float.NaN;
        bool rejected = false;
        try
        {
            Tuning.Validate(rail, "railgun");
        }
        catch (InvalidOperationException)
        {
            rejected = true;
        }

        check(rejected, "invalid railgun charge timing is rejected before tuning application");
#if NATIVE_CONFIG_TESTS
        Native(check);
#endif
    }
#if NATIVE_CONFIG_TESTS
    private static void Native(Action<bool, string> check)
    {
        var tuning = new Brainless.Items.SupTuning();
        tuning.Validate();
        check(tuning.ExpiredPills.Effects.Count == 24 && tuning.ExpiredPills.Effects.Select(e => e.Effect).Distinct().Count() == 24, "all twenty-four SUP random effects are valid current game effects");
        tuning.ExpiredPills.Effects[0].MinIntensity = 20;
        bool rejected = false;
        try { tuning.Validate(); } catch (InvalidOperationException) { rejected = true; }
        check(rejected, "reversed random-effect intensity ranges are rejected");
        var fixes = new Brainless.SlFixes.Config();
        fixes.Validate();
        check(fixes.Recipes.Count == 14 && fixes.Recipes.Select(r => r.CustomId).Distinct().Count() == 14 && fixes.Recipes.Single(r => r.CustomId == 7208).Chance == 5, "SCP-914 adds ten unique SUP recipes with a rare railgun result");
        fixes.Recipes.RemoveAll(r => r.CustomId == 7208);
        fixes.Validate();
        check(fixes.Recipes.Count == 13, "deliberately removed recipes remain removed after the one-time migration");
        var items = new ItemDistributionConfig(); items.UpgradeSup(); items.UpgradeSup();
        check(items.MapRules.Count(r => r.CustomId >= 7114) == 9 && items.MapRules.Single(r => r.CustomId == 7119).SourceTypes.Contains(ItemType.KeycardJanitor), "natural SUP loot migration adds all nine items once with compatible native bodies");
        var weapons = new WeaponDistributionConfig(); weapons.UpgradeSupWeapons(); weapons.UpgradeSupWeapons();
        check(weapons.MapRules.Count(r => r.CustomId == 7208) == 1 && weapons.PlayerRules.Count(r => r.CustomId == 7207) == 1, "railgun and launcher map/player pools migrate without duplicate rolls");
        var emptyLoot = new ItemDistributionConfig { MapRules = new System.Collections.Generic.List<MapLootRule>(), PlayerRules = new System.Collections.Generic.List<PlayerLootRule>() };
        emptyLoot.UpgradeSup();
        var emptyRecipes = new Brainless.SlFixes.Config { Recipes = new System.Collections.Generic.List<Brainless.SlFixes.CraftingRecipe>() };
        emptyRecipes.Validate();
        check(emptyLoot.MapRules.Count == 0 && emptyLoot.PlayerRules.Count == 0 && emptyRecipes.Recipes.Count == 0, "explicit empty loot and recipe pools survive SUP migrations");
        ItemFileTests(check);
    }
    public sealed class FileSpec
    {
        public uint Id { get; set; } = 7208;
        public string Name { get; set; } = "Railgun";
        public bool Enabled { get; set; } = true;
    }
    public sealed class FileRoot
    {
        public Brainless.Weapons.RailgunTuning Railgun { get; set; } = new Brainless.Weapons.RailgunTuning();
    }
    private static void ItemFileTests(Action<bool, string> check)
    {
        string directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pb-item-files-" + Guid.NewGuid().ToString("N"));
        try
        {
            var catalog = new System.Collections.Generic.List<FileSpec> { new FileSpec() };
            var root = new FileRoot();
            var sections = new System.Collections.Generic.Dictionary<uint, ItemFiles.Section> { [7208] = ItemFiles.Bind(root, "Railgun") };
            ItemFiles.Apply(directory, catalog, i => i.Id, sections);
            string path = System.IO.Path.Combine(directory, "7208.yml"), initial = System.IO.File.ReadAllText(path);
            System.IO.File.WriteAllText(path, initial.Replace("human_damage: 85", "human_damage: 133").Replace("enabled: true", "enabled: false"));
            string edited = System.IO.File.ReadAllText(path);
            root.Railgun.HumanDamage = 2; catalog[0].Enabled = true;
            ItemFiles.Apply(directory, catalog, i => i.Id, sections);
            check(root.Railgun.HumanDamage == 133 && !catalog[0].Enabled && System.IO.File.ReadAllText(path) == edited, "per-item YAML overrides catalog and tuning without overwriting saved edits at startup/reload");
            System.IO.File.WriteAllText(path, edited.Replace("id: 7208", "id: 7122"));
            bool rejected = false;
            try { ItemFiles.Apply(directory, catalog, i => i.Id, sections); } catch (InvalidOperationException) { rejected = true; }
            check(rejected, "per-item files cannot change stable item identity");
            System.IO.File.WriteAllText(path, "item: [unterminated");
            rejected = false;
            try { ItemFiles.Apply(directory, catalog, i => i.Id, sections); } catch (InvalidOperationException) { rejected = true; }
            check(rejected && System.IO.File.ReadAllText(path) == "item: [unterminated", "malformed per-item YAML is preserved and rejected rather than regenerated");
        }
        finally { if (System.IO.Directory.Exists(directory)) System.IO.Directory.Delete(directory, true); }
    }
#endif
}
