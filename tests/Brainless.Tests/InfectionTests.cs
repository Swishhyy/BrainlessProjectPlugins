using System;
using System.Collections.Generic;
using System.Linq;
using Brainless.RoundEvents;

internal static class InfectionTests
{
    internal static void Run(Action<bool, string> check)
    {
        SpreadTests(check);
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
        typeof(PlayerRoles.PlayableScps.Scp3114.Scp3114DamageHandler).GetProperty("Subtype").SetValue(native, PlayerRoles.PlayableScps.Scp3114.Scp3114DamageHandler.HandlerType.Slap);
        check(NativeAttack.Classify(native) == InfectionAttack.Slap, "the actual game slap handler is recognized as an infection source");
        typeof(PlayerRoles.PlayableScps.Scp3114.Scp3114DamageHandler).GetProperty("Subtype").SetValue(native, PlayerRoles.PlayableScps.Scp3114.Scp3114DamageHandler.HandlerType.Strangulation);
        check(NativeAttack.Classify(native) == InfectionAttack.Strangulation, "the actual game strangulation handler is recognized as an infection source");
        typeof(PlayerRoles.PlayableScps.Scp3114.Scp3114DamageHandler).GetProperty("Subtype").SetValue(native, PlayerRoles.PlayableScps.Scp3114.Scp3114DamageHandler.HandlerType.SkinSteal);
        check(NativeAttack.Classify(native) == InfectionAttack.Other && NativeAttack.Classify(null) == InfectionAttack.Other, "skin stealing and absent damage handlers do not count as killing attacks");
        typeof(PlayerRoles.PlayableScps.Scp3114.Scp3114DamageHandler).GetProperty("Subtype").SetValue(native, PlayerRoles.PlayableScps.Scp3114.Scp3114DamageHandler.HandlerType.Slap);
        typeof(PlayerRoles.PlayableScps.Scp3114.Scp3114DamageHandler).GetProperty("StartingRagdoll").SetValue(native, true);
        check(NativeAttack.Classify(native) == InfectionAttack.Other, "synthetic starting-ragdoll data cannot infect a player");
    }
#endif

    // A 10 x 6 grid of rooms 20 m apart, like a spread-out facility.
    private static List<SpreadPlanner.Point> Grid(int width, int depth, double spacing) =>
        Enumerable.Range(0, width * depth).Select(i => new SpreadPlanner.Point(i % width * spacing, 0, i / width * spacing)).ToList();

    private static void SpreadTests(Action<bool, string> check)
    {
        var rooms = Grid(10, 6, 20);
        var random = new Random(4);
        int[] plan = SpreadPlanner.Plan(rooms, 4, 20, 35, random);
        var skeletons = plan.Take(4).ToArray();
        var survivors = plan.Skip(4).ToArray();
        double Gap(int a, int b) => rooms[a].Distance(rooms[b]);
        check(plan.Length == 24 && plan.All(r => r >= 0 && r < rooms.Count), "every Infection player gets a start room");
        check(survivors.All(h => skeletons.All(s => Gap(h, s) >= 35)), "no survivor starts within the minimum distance of a skeleton");
        check(survivors.Distinct().Count() == 20 && !survivors.Intersect(skeletons).Any(), "with enough rooms every survivor starts in a different room from everyone");
        double skeletonGap = skeletons.SelectMany((a, i) => skeletons.Skip(i + 1).Select(b => Gap(a, b))).Min();
        check(skeletonGap >= 60, "skeletons start spread across the map instead of together");

        int[] crowded = SpreadPlanner.Plan(Grid(4, 2, 20), 2, 30, 35, new Random(1));
        var crowdedHumans = crowded.Skip(2).ToArray();
        var perRoom = crowdedHumans.GroupBy(r => r).Select(g => g.Count()).ToArray();
        check(crowded.All(r => r >= 0) && perRoom.Max() - perRoom.Min() <= 1, "with more survivors than safe rooms, survivors share rooms evenly");

        int[] tiny = SpreadPlanner.Plan(Grid(2, 1, 10), 1, 5, 35, new Random(2));
        check(tiny.Skip(1).All(r => r != tiny[0]), "on a map too small for the distance rule, survivors still avoid the skeleton's room");
        check(SpreadPlanner.Plan(new List<SpreadPlanner.Point>(), 2, 3, 35, new Random(3)).All(r => r == -1), "with no usable rooms everyone keeps their current position");
        var seen = new HashSet<string>();
        for (int seed = 0; seed < 20; seed++)
            seen.Add(string.Join(",", SpreadPlanner.Plan(rooms, 3, 5, 35, new Random(seed)).Take(3).OrderBy(r => r)));
        check(seen.Count > 5, "skeleton start rooms vary from round to round");
    }
}
