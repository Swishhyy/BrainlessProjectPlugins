#if NATIVE_CONFIG_TESTS
using System;
using System.Linq;
using Brainless.Internal;
using Brainless.Internal.Updates;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

internal static class DistributionConfigTests
{
    internal static void Run(Action<bool, string> check)
    {
        var reader = new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).Build();
        var writer = new SerializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).Build();
        var voice = reader.Deserialize<Brainless.ScpProximityChat.Config>("volume: 1\nfull_volume_range: 2\n");
        voice.UpgradeAudio();
        check(voice.Volume == 3.5f && voice.FullVolumeRange == 6 && voice.AudioBalanceRevision == 1, "old SCP voice YAML receives the audible preset without deleting the server config");
        voice = reader.Deserialize<Brainless.ScpProximityChat.Config>("volume: 0.5\nfull_volume_range: 4\n");
        voice.UpgradeAudio();
        check(voice.Volume == .5f && voice.FullVolumeRange == 4, "edited SCP voice gain and full-volume range survive the audio migration");
        voice = reader.Deserialize<Brainless.ScpProximityChat.Config>("volume: 1\nfull_volume_range: 2\naudio_balance_revision: 1\n");
        voice.UpgradeAudio();
        check(voice.Volume == 1 && voice.FullVolumeRange == 2, "SCP voice owners can explicitly retain the old quieter preset");
        var door = reader.Deserialize<ItemDistributionConfig>("map_rules:\n- custom_id: 7105\n  source_types: [Coin]\n- custom_id: 7108\n  source_types: [Coin]\nplayer_rules:\n- custom_id: 7105\n  replace_types: [Coin]\n");
        door.UpgradeDoorJammerBody();
        check(door.MapRules[0].SourceTypes.SequenceEqual(new[] { ItemType.KeycardJanitor }) && door.PlayerRules.Single().ReplaceTypes.SequenceEqual(new[] { ItemType.KeycardJanitor }) && door.MapRules[1].SourceTypes.SequenceEqual(new[] { ItemType.Coin }), "Door Jammer distribution migrates its old coin body to a keycard without changing chalk loot");
        var emptyDoor = reader.Deserialize<ItemDistributionConfig>("map_rules: []\nplayer_rules: []\n");
        emptyDoor.UpgradeDoorJammerBody();
        check(emptyDoor.MapRules.Count == 0 && emptyDoor.PlayerRules.Count == 0, "the Door Jammer body migration does not reintroduce removed loot or loadout rules");
        var updater = reader.Deserialize<AutoUpdateConfig>("token: ' yaml-test-token '\ntoken_environment_variable: ''\n");
        check(updater.ResolveToken(_ => throw new Exception("YAML token should not read the environment.")) == "yaml-test-token", "real EXILED YAML accepts a directly configured updater token");
        var updaterRoundtrip = reader.Deserialize<AutoUpdateConfig>(writer.Serialize(updater));
        check(updaterRoundtrip.Token == updater.Token && updaterRoundtrip.TokenEnvironmentVariable == "", "updater token and optional empty environment fallback survive YAML serialization");
        var oldUpdater = reader.Deserialize<AutoUpdateConfig>("token_environment_variable: TEST_GITHUB_TOKEN\nreleases_to_scan: 10\n");
        check(oldUpdater.Token == "" && oldUpdater.ResolveToken(_ => "old-environment-test-token") == "old-environment-test-token" && oldUpdater.ReleasesToScan == 10, "old updater YAML retains its environment token and configured scan count");
        bool Valid(DistributionConfig value, bool weapon = false)
        {
            try { value.Validate(id => weapon ? id >= 7201 && id <= 7207 : id >= 7101 && id <= 7113 && id != 7110); return true; }
            catch (InvalidOperationException) { return false; }
        }
        var items = reader.Deserialize<ItemDistributionConfig>("map_chance_percent: 60\n");
        check(items.MapChancePercent == 60 && items.MaxMapReplacements == 3 && items.PlayerChancePercent == 30, "partial item YAML keeps provider-specific numeric defaults");
        check(items.MapRules.Any(r => r.CustomId == 7109) && items.PlayerRules.Any(r => r.CustomId == 7108) && Valid(items), "partial item YAML retains its natural loot and spawn pools");
        var weapons = reader.Deserialize<WeaponDistributionConfig>("map_chance_percent: 55\n");
        check(weapons.MapChancePercent == 55 && weapons.MaxMapReplacements == 1 && weapons.MaxPlayerGrants == 1 && weapons.PlayerChancePercent == 15 && weapons.PlayerDelaySeconds == 1,
            "partial weapon YAML cannot fall back to the more generous item limits");
        check(weapons.MapRules.Select(r => r.CustomId).SequenceEqual(new uint[] { 7201, 7202, 7203 }) && Valid(weapons, true), "partial weapon YAML retains its safe native firearm pool");
        var empty = reader.Deserialize<ItemDistributionConfig>("map_rules: []\nplayer_rules: []\n");
        check(empty.MapRules.Count == 0 && empty.PlayerRules.Count == 0 && Valid(empty), "explicit empty YAML pools remain empty rather than being repopulated");
        var custom = reader.Deserialize<ItemDistributionConfig>("map_rules:\n- custom_id: 7113\n  source_types: [GrenadeHE]\nplayer_rules:\n- custom_id: 7108\n  roles: [ClassD]\n  replace_types: []\n");
        check(custom.MapRules.Count == 1 && custom.MapRules[0].Zones.Count == 3 && custom.MapRules[0].Weight == 1 && Valid(custom), "edited YAML rules preserve row defaults and parse real game enums");
        var roundtrip = reader.Deserialize<ItemDistributionConfig>(writer.Serialize(custom));
        check(roundtrip.MapRules.Single().CustomId == 7113 && roundtrip.PlayerRules.Single().CustomId == 7108 && Valid(roundtrip), "native-YAML serialization roundtrips configured loot pools");
        check(!Valid(reader.Deserialize<ItemDistributionConfig>("map_rules: null\n")), "a null map pool is rejected before config application");
        check(!Valid(reader.Deserialize<ItemDistributionConfig>("player_rules: null\n")), "a null player pool is rejected before config application");
        check(!Valid(reader.Deserialize<ItemDistributionConfig>("player_chance_percent: .nan\n")), "YAML NaN cannot become a spawn probability");
        check(!Valid(reader.Deserialize<ItemDistributionConfig>("map_chance_percent: .inf\n")), "YAML infinity cannot become a map probability");
        check(!Valid(reader.Deserialize<ItemDistributionConfig>("map_rules:\n- custom_id: 7110\n  source_types: [Coin]\n")), "retired Maintenance Pass cannot reenter natural loot through YAML");
        check(!Valid(reader.Deserialize<WeaponDistributionConfig>("player_rules:\n- custom_id: 7109\n"), true), "a weapon config cannot issue another provider's custom IDs");
        check(!Valid(reader.Deserialize<ItemDistributionConfig>("map_rules:\n- custom_id: 7109\n  source_types: []\n")), "map rules require explicit native source bodies");
        items.MapRules.Clear();
        check(new ItemDistributionConfig().MapRules.Count > 0 && new WeaponDistributionConfig().MapRules.Count == 3, "provider defaults never share mutable rule-list instances");
    }
}
#endif

