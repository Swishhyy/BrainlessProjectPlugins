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
        check(items.ClusterGrenade.GrenadeCount == 4 && items.ClusterGrenade.HumanDamage == 40 && items.ClusterGrenade.Radius == 2.75f, "the modest cluster buff preserves four children and increases damage/radius");
        check(items.GasGrenade.HumanDamagePerSecond == 0 && items.GasGrenade.ScpDamagePerSecond == 0 && items.GasGrenade.HumanSlowPercent == 20, "gas remains non-damaging with a configurable twenty-percent human slow by default");
        items.ClusterGrenade.GrenadeCount = 32;
        items.ClusterGrenade.HumanDamage = 250;
        items.GasGrenade.DurationSeconds = 40;
        check(Valid(items), "custom grenade count, damage and gas longevity are accepted without legacy gameplay caps");
        items.ClusterGrenade.GrenadeCount = 33;
        check(!Valid(items), "unbounded child-grenade counts are rejected");
        items.ClusterGrenade.GrenadeCount = 4;
        items.GasGrenade.Radius = float.NaN;
        check(!Valid(items), "nested NaN tuning cannot enter the live simulation");
        items.GasGrenade.Radius = 4;
        items.FilterInhaler = null;
        check(!Valid(items), "a missing nested tuning section is rejected");
        weapons.Rattler.TagDurationSeconds = float.PositiveInfinity;
        check(!Valid(weapons), "infinite audio durations are rejected");
        var cap = new BlastBudget(85, 225);
        check(Enumerable.Range(0, 8).Sum(_ => cap.Take("human", false, 40)) == 85, "extra configured cluster children still share the configured human damage cap");
        check(Enumerable.Range(0, 8).Sum(_ => cap.Take("scp", true, 100)) == 225, "extra configured cluster children still share the configured SCP damage cap");
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
