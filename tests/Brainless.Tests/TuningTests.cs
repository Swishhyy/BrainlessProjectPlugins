using System;
using System.Linq;
using Brainless.Internal;
using Brainless.Items;
using Brainless.Roles;
using Brainless.Weapons;

internal static class TuningTests
{
    internal static void Run(Action<bool, string> check)
    {
        bool Valid(object value)
        {
            try
            {
                Tuning.Validate(value, "settings");
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        var items = new ItemTuning();
        var weapons = new WeaponTuning();
        var zombie = new ExplosiveZombieTuning();
        check(Valid(items) && Valid(weapons) && Valid(zombie), "all shipped tuning defaults validate");
#if NATIVE_CONFIG_TESTS
        var glow = new GlowTuning();
        var vanilla = GlowTuning.DefaultVanilla();
        check(vanilla.Count == 57, "every vanilla item named in the default rarity table exists in this game version (" + vanilla.Count + "/57)");
        check(vanilla[ItemType.Coin] == Rarity.Common && vanilla[ItemType.Medkit] == Rarity.Uncommon && vanilla[ItemType.GrenadeHE] == Rarity.Rare && vanilla[ItemType.Jailbird] == Rarity.Epic && vanilla[ItemType.MicroHID] == Rarity.Legendary, "vanilla items get the approved rarities");
        check(glow.Enabled && glow.MinimumRarity == Rarity.Uncommon && glow.MaxLights == 150 && glow.PickupHint && Valid(glow), "Uncommon and up glow by default, capped at 150 lights");
#endif
        check(items.ClusterGrenade.NativeParentBlast && items.ClusterGrenade.ChildNativeDamagePercent == 40, "the cluster's first blast is a stock HE grenade and each child deals 40% of one");
        var supply = new SupplyTuning { SmallLobbyCapacity = 6, MediumLobbyCapacity = 10, LargeLobbyCapacity = 14, ClusterMinPlayers = 12, SmallLobbyClusters = 0, MediumLobbyClusters = 1, LargeLobbyClusters = 2, WorldLootCount = 2 };
        supply.RaiseLimits();
        check(supply.SmallLobbyCapacity == 10 && supply.MediumLobbyCapacity == 16 && supply.LargeLobbyCapacity == 22 && supply.ClusterMinPlayers == 6 && supply.SmallLobbyClusters == 1 && supply.MediumLobbyClusters == 2 && supply.LargeLobbyClusters == 3 && supply.WorldLootCount == 4, "items revision 13 raises the old supply limits for unique items");
        var roomy = new SupplyTuning { LargeLobbyCapacity = 40, ClusterMinPlayers = 2 };
        roomy.RaiseLimits();
        check(roomy.LargeLobbyCapacity == 40 && roomy.ClusterMinPlayers == 2, "items revision 13 never tightens an owner's looser supply limits");
        check(items.ClusterGrenade.DamageEnvironment && items.DoorBrace.DurationSeconds == 25, "clusters enable native environment damage and Door Jammer lasts 25 seconds by default");
        var brace = new BraceTuning
        {
            DurationSeconds = 8
        };
        brace.Upgrade(5);
        check(brace.DurationSeconds == 25, "the previous eight-second Door Jammer preset upgrades to 25 seconds");
        brace.DurationSeconds = 40;
        brace.Upgrade(5);
        check(brace.DurationSeconds == 40, "Door Jammer migration preserves an edited lifetime");
        brace.DurationSeconds = 8;
        brace.Upgrade(6);
        check(brace.DurationSeconds == 8, "the current catalog marker allows deliberately retaining an eight-second brace");
        var foam = new FoamTuning();
        check(!foam.SinglePuddle && foam.Radius == 6 && foam.Spacing == 3 && foam.MaxPuddles == 12 && foam.DurationSeconds == 30, "containment foam spreads a wide tantrum area by default");
        check(items.ClusterGrenade.GrenadeCount == 4 && items.ClusterGrenade.HumanDamage == 120 && items.ClusterGrenade.Radius == 5.5f && items.ClusterGrenade.ParentHumanDamage == 180, "the stronger cluster retains four children with a stronger six-metre parent and larger child blasts");
        check(typeof(GasTuning).GetProperty("HumanDamagePerSecond") == null && typeof(GasTuning).GetProperty("HumanSlowPercent") == null && items.GasGrenade.DurationSeconds == 20 && !items.Supply.PairGasWithFilter, "smoke-only gas exposes no custom damage or slow fields and needs no paired filter");
        items.ClusterGrenade.GrenadeCount = 32;
        items.ClusterGrenade.HumanDamage = 250;
        items.GasGrenade.DurationSeconds = 40;
        check(Valid(items), "custom grenade count, damage and gas longevity are accepted without legacy gameplay caps");
        items.ClusterGrenade.GrenadeCount = 33;
        check(!Valid(items), "unbounded child-grenade counts are rejected");
        items.ClusterGrenade.GrenadeCount = 4;
        items.ContainmentFoam.Radius = float.NaN;
        check(!Valid(items), "nested NaN tuning cannot enter the live simulation");
        items.ContainmentFoam.Radius = 4;
        items.ContainmentFoam = null;
        check(!Valid(items), "a missing nested tuning section is rejected");
        weapons.Ricochet.Range = float.PositiveInfinity;
        check(!Valid(weapons), "infinite weapon ranges are rejected");
        check(ExplosionRules.DefaultPenetration == 50 && ExplosionRules.Penetration(0) == 1 && ExplosionRules.Penetration(-1) == 1 && ExplosionRules.Penetration(500) == 100, "custom blasts cannot pass zero penetration to the native zero-damage constructor path");
        var fresh = new ItemTuning();
        check(fresh.ImpactGrenade.HumanDamage == 140 && fresh.ImpactGrenade.ScpDamage == 350 && fresh.ImpactGrenade.Radius == 5 && fresh.ImpactGrenade.ArmorPenetration == 50, "impact defaults provide a larger damaging blast with native HE penetration");
        fresh.ImpactGrenade.ArmorPenetration = 0;
        check(!Valid(fresh), "zero-penetration blast settings are rejected during config validation");
        var legacy = new ItemTuning();
        legacy.ClusterGrenade.GrenadeCount = 8;
        legacy.ClusterGrenade.HumanDamage = 40;
        legacy.ClusterGrenade.ScpDamage = 100;
        legacy.ClusterGrenade.Radius = 2.75f;
        legacy.ClusterGrenade.SharedHumanDamageCap = 85;
        legacy.ClusterGrenade.SharedScpDamageCap = 225;
        legacy.ClusterGrenade.ChildFuseSeconds = 1.5f;
        legacy.ClusterGrenade.HorizontalSpeed = 4.5f;
        legacy.ClusterGrenade.UpwardSpeed = 4;
        legacy.ImpactGrenade.HumanDamage = 80;
        legacy.ImpactGrenade.ScpDamage = 180;
        legacy.ImpactGrenade.Radius = 3.5f;
        check(legacy.UpgradeGrenades(0) == 2 && legacy.ClusterGrenade.HumanDamage == 120 && legacy.ClusterGrenade.SharedHumanDamageCap == 300 && legacy.ImpactGrenade.HumanDamage == 140, "original stock YAML migrates through both cluster revisions while retaining the impact buff");
        check(legacy.ClusterGrenade.GrenadeCount == 8 && legacy.ClusterGrenade.ChildFuseSeconds == .9f, "stock tuning migration preserves edited grenade counts and updates untouched child timing");
        legacy.ImpactGrenade.HumanDamage = 333;
        legacy.UpgradeGrenades(0);
        check(legacy.ImpactGrenade.HumanDamage == 333, "operator damage edits survive stock-preset migration");
        legacy.ClusterGrenade.HumanDamage = 40;
        legacy.UpgradeGrenades(2);
        check(legacy.ClusterGrenade.HumanDamage == 40, "a completed balance revision respects deliberate old-value overrides");
        var previous = new ItemTuning();
        previous.ClusterGrenade.HumanDamage = 70;
        previous.ClusterGrenade.Radius = 4;
        previous.ClusterGrenade.ParentHumanDamage = 90;
        previous.ClusterGrenade.ParentRadius = 4.5f;
        previous.ClusterGrenade.ChildFuseSeconds = 1.2f;
        previous.ClusterGrenade.HorizontalSpeed = 3;
        previous.ClusterGrenade.SharedHumanDamageCap = 150;
        previous.ClusterGrenade.ScpDamage = 999;
        check(previous.UpgradeGrenades(1) == 2 && previous.ClusterGrenade.HumanDamage == 120 && previous.ClusterGrenade.Radius == 5.5f && previous.ClusterGrenade.SharedHumanDamageCap == 300, "the immediately previous cluster preset upgrades even after the first balance revision was completed");
        check(previous.ClusterGrenade.ScpDamage == 999 && previous.ClusterGrenade.GrenadeCount == 4 && previous.ClusterGrenade.ChildFuseSeconds == .9f && previous.ClusterGrenade.HorizontalSpeed == 5, "the new cluster migration preserves edited damage and counts while updating stock launch timing");
        var cluster = new ClusterTuning();
        check(Rules.Falloff(3, cluster.ParentRadius, cluster.ParentHumanDamage) >= 3 * Rules.Falloff(3, 4.5f, 90) && Rules.Falloff(3, cluster.Radius, cluster.HumanDamage) > 3 * Rules.Falloff(3, 4, 70), "parent and child blasts now deliver at least three times the old raw damage at three metres");
        var newCap = new BlastBudget(cluster.SharedHumanDamageCap, cluster.SharedScpDamageCap);
        check(newCap.Take("human", false, cluster.ParentHumanDamage) + Enumerable.Range(0, 4).Sum(_ => newCap.Take("human", false, cluster.HumanDamage)) == 300 && newCap.Take("scp", true, cluster.ParentScpDamage) + Enumerable.Range(0, 4).Sum(_ => newCap.Take("scp", true, cluster.ScpDamage)) == 1200, "the stronger cluster still shares one finite per-player cap across its parent and four children");
        check(ExplosionRules.UpgradeStock(45, 45, 70, 0) == 70 && ExplosionRules.UpgradeStock(95, 45, 70, 0) == 95 && ExplosionRules.UpgradeStock(45, 45, 70, 1) == 45, "launcher stock upgrades preserve edits and obey the balance migration marker");
        var cap = new BlastBudget(150, 500);
        check(cap.Take("human", false, 90) + Enumerable.Range(0, 4).Sum(_ => cap.Take("human", false, 70)) == 150, "cluster parent and child damage share the configured human cap");
        check(cap.Take("scp", true, 250) + Enumerable.Range(0, 4).Sum(_ => cap.Take("scp", true, 180)) == 500, "cluster parent and child damage share the configured SCP cap");
        check(new BlastBudget(0, 0).Take("target", false, 100) == 0, "zero blast caps disable their damage rather than becoming unlimited");
        var trigger = new ImpactTrigger(10, false);
        check(!trigger.TryContact(9.99f, false, false), "impact grenades cannot detonate before arming");
        check(!trigger.TryContact(10, true, true), "impact grenades never trigger on their thrower's collider");
        check(!trigger.TryContact(10, true, false), "surface-only impact grenades ignore player colliders");
        check(trigger.TryContact(10, false, false), "continued surface contact detonates once the arming delay expires");
        check(!trigger.TryContact(11, false, false), "repeated collision callbacks cannot produce duplicate explosions");
        check(new ImpactTrigger(0, true).TryContact(1, true, false), "optional player-impact mode detonates on another player");
        check(!new ImpactTrigger(0, true).TryContact(float.NaN, false, false), "invalid collision times cannot arm an impact grenade");
        check(!new ImpactTrigger(0, true).TryContact(float.PositiveInfinity, false, false), "infinite collision times cannot detonate an impact grenade");
        check(zombie.Allows(false, false, false) && zombie.Allows(true, true, true), "explosive zombies accept environmental and explosion deaths by default");
        zombie.RequireEnemyKill = true;
        check(!zombie.Allows(false, false, false) && !zombie.Allows(true, false, false) && zombie.Allows(true, true, false), "enemy-only zombie deaths remain an explicit configuration option");
        zombie.RequireEnemyKill = false;
        zombie.TriggerOnEnvironmentalDeaths = false;
        zombie.TriggerOnExplosionDeaths = false;
        check(!zombie.Allows(false, false, false) && !zombie.Allows(true, true, true), "individual zombie death exclusions work independently");
        var deaths = new DeathCapture<string, Snapshot>();
        var first = new Snapshot();
        deaths.Capture("player", first);
        first.Allowed = false;
        check(!deaths.Confirm("player", s => s.Allowed, out _), "a later death cancellation cannot arm a zombie burst");
        check(!deaths.Confirm("player", _ => true, out _), "canceled captures are consumed rather than leaking to another life");
        var next = new Snapshot();
        deaths.Capture("player", first);
        deaths.Capture("player", next);
        deaths.ForgetIfSame("player", first);
        check(deaths.Confirm("player", s => s.Allowed, out var confirmed) && ReferenceEquals(confirmed, next), "an old cleanup callback cannot discard a newer death capture");
        check(!deaths.Confirm("player", _ => true, out _), "a duplicate Died callback cannot spawn another burst");
        deaths.Capture("player", new Snapshot());
        check(!deaths.Confirm("player", _ => false, out _), "a non-zombie old role cannot consume a zombie explosion into gameplay");
        deaths.Capture("player", new Snapshot());
        deaths.Forget("player");
        check(!deaths.Confirm("player", _ => true, out _), "disconnect cleanup prevents a captured burst from surviving removal");
        deaths.Capture("player", new Snapshot());
        deaths.Clear();
        check(!deaths.Confirm("player", _ => true, out _), "round cleanup removes pending death captures");
        var shots = new ShotLedger();
        shots.Register(1, 24);
        shots.Consume(1);
        shots.Register(1, 50);
        check(shots.Remaining(1) == 23, "retuning an already-issued finite weapon cannot refill its remaining shots");
        shots.Register(2, 50);
        check(shots.Remaining(2) == 50, "new finite weapons use their newly configured shot count");
    }

    private sealed class Snapshot
    {
        internal bool Allowed = true;
    }
}
