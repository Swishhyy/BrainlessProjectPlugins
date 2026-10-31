using System;
using Brainless.RoundEvents;

internal static class InfectionTests
{
    internal static void Run(Action<bool, string> check)
    {
        check(InfectionRules.SeedCount(4, 3, 20, 8, 2) == 2 && InfectionRules.SeedCount(10, 3, 20, 8, 2) == 3, "infection starts a swarm while preserving the minimum survivors in small lobbies");
        check(InfectionRules.SeedCount(40, 3, 20, 8, 2) == 8 && InfectionRules.SeedCount(100, 3, 100, 8, 2) == 8, "percentage seeding respects its configured hard maximum");
        check(InfectionRules.SeedCount(1, 3, 20, 8, 2) == 0, "too few living players cannot seed an unwinnable all-infected round");
        check(InfectionRules.Infects(true, true, true, true, InfectionAttack.Slap, true, true) && InfectionRules.Infects(true, true, true, true, InfectionAttack.Strangulation, true, true), "only enabled direct skeleton attack kinds can spread infection");
        check(!InfectionRules.Infects(true, true, true, true, InfectionAttack.Other, true, true), "environmental, explosion, poison and unrelated damage do not infect even with SCP attacker credit");
        check(!InfectionRules.Infects(false, true, true, true, InfectionAttack.Slap, true, true) && !InfectionRules.Infects(true, false, true, true, InfectionAttack.Slap, true, true) && !InfectionRules.Infects(true, true, false, true, InfectionAttack.Slap, true, true) && !InfectionRules.Infects(true, true, true, false, InfectionAttack.Slap, true, true), "inactive events, SCP victims, unowned attackers and self-kills cannot produce conversions");
        check(!InfectionRules.Infects(true, true, true, true, InfectionAttack.Strangulation, true, false), "strangulation conversion can be independently disabled");
        check(InfectionRules.Outcome(0, 0, 1) == InfectionOutcome.Continue && InfectionRules.Outcome(0, 4, 1) == InfectionOutcome.Continue, "a pending death conversion prevents premature round endings");
        check(InfectionRules.Outcome(1, 1, 0) == InfectionOutcome.Continue && InfectionRules.Outcome(1, 0, 0) == InfectionOutcome.Humans && InfectionRules.Outcome(0, 1, 0) == InfectionOutcome.Infected && InfectionRules.Outcome(0, 0, 0) == InfectionOutcome.Draw, "infection determines both winners and a draw from actual living participants");
        var state = new InfectionState<string>();
        var dead = new object ();
        var alive = new object ();
        check(state.Queue("victim", dead, "point") == null, "inactive events cannot queue delayed revives");
        state.Start();
        state.MarkInfected("attacker", alive);
        check(state.IsInfected("attacker", alive) && !state.IsInfected("attacker", new object ()), "infected membership belongs to the exact attacking life");
        var ticket = state.Queue("victim", dead, "death-position");
        check(ticket != null && ticket.Payload == "death-position" && state.Queue("victim", dead, "duplicate") == null && state.PendingCount == 1, "a confirmed death queues exactly one conversion and retains its captured death payload");
        check(state.Take(ticket, dead) && !state.Take(ticket, dead) && state.PendingCount == 0, "a conversion ticket can be consumed only once");
        ticket = state.Queue("victim", dead, "old-life");
        state.LifeChanged("victim", alive);
        check(!state.Take(ticket, alive) && state.PendingCount == 0, "admin respawn or another role change cancels the old death conversion");
        ticket = state.Queue("victim", dead, "old-player");
        state.Forget("victim");
        var newer = state.Queue("victim", new object (), "reconnected-player");
        state.Cancel(ticket);
        check(state.PendingCount == 1 && !state.Take(ticket, dead), "an old disconnected callback cannot cancel a reconnect's newer conversion");
        state.Stop();
        state.Start();
        check(!state.Take(newer, newer.DeadLife) && state.PendingCount == 0 && !state.IsInfected("attacker", alive), "round resets and event stops invalidate all tickets and infected memberships");
        state.MarkInfected("attacker", alive);
        state.LifeChanged("attacker", dead);
        check(!state.IsInfected("attacker", alive), "a dead or changed-role skeleton cannot keep its old infection membership");
#if NATIVE_CONFIG_TESTS
        Native(check);
#endif
    }
#if NATIVE_CONFIG_TESTS
    private static void Native(Action<bool, string> check)
    {
        var config = new Brainless.RoundEvents.Config(); config.Validate();
        check(config.Infection.InfectedHealth == 150 && config.Infection.InfectedHumeShield == 0 && !config.AutomaticEvents && config.Infection.BlockReinforcements, "infection defaults use low-health skeletons, manual scheduling and no ordinary reinforcement waves");
        config.Infection.RespawnDelaySeconds = float.NaN;
        bool invalid = false; try { config.Validate(); } catch (InvalidOperationException) { invalid = true; }
        check(invalid, "invalid conversion timing is rejected before event settings are applied");
        config = new Brainless.RoundEvents.Config(); config.Infection.MinimumSurvivors = config.Infection.MinimumPlayers;
        invalid = false; try { config.Validate(); } catch (InvalidOperationException) { invalid = true; }
        check(invalid, "configuration cannot consume every minimum participant as a survivor and leave no seeds");
        config = new Brainless.RoundEvents.Config(); config.Infection.SurvivorRole = PlayerRoles.RoleTypeId.Scp3114;
        invalid = false; try { config.Validate(); } catch (InvalidOperationException) { invalid = true; }
        check(invalid, "the survivor side cannot be configured as another SCP faction");
        var native = (PlayerRoles.PlayableScps.Scp3114.Scp3114DamageHandler)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(PlayerRoles.PlayableScps.Scp3114.Scp3114DamageHandler));
        native.Subtype = PlayerRoles.PlayableScps.Scp3114.Scp3114DamageHandler.HandlerType.Slap;
        check(NativeAttack.Classify(native) == InfectionAttack.Slap, "the actual game slap handler is recognized as an infection source");
        native.Subtype = PlayerRoles.PlayableScps.Scp3114.Scp3114DamageHandler.HandlerType.Strangulation;
        check(NativeAttack.Classify(native) == InfectionAttack.Strangulation, "the actual game strangulation handler is recognized as an infection source");
        native.Subtype = PlayerRoles.PlayableScps.Scp3114.Scp3114DamageHandler.HandlerType.SkinSteal;
        check(NativeAttack.Classify(native) == InfectionAttack.Other && NativeAttack.Classify(null) == InfectionAttack.Other, "skin stealing and absent damage handlers do not count as killing attacks");
        native.Subtype = PlayerRoles.PlayableScps.Scp3114.Scp3114DamageHandler.HandlerType.Slap; native.StartingRagdoll = true;
        check(NativeAttack.Classify(native) == InfectionAttack.Other, "synthetic starting-ragdoll data cannot infect a player");
    }
#endif
}
