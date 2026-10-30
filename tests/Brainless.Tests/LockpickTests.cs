using System;
using Brainless.Roles;

internal static class LockpickTests
{
    internal static void Run(Action<bool, string> check)
    {
        var rules = new LockpickRules();
        check(rules.TryBegin("picker", 0), "a ready Locksmith can start an attempt");
        check(!rules.TryBegin("picker", 10), "repeated door input cannot start parallel lockpicks");
        check(!rules.CanFinish("picker", 14.99f, LockpickRules.DefaultDuration), "lockpicking cannot complete before fifteen seconds");
        check(!rules.Succeed("picker", 14.99f, 15, 120), "premature success cannot start a cooldown or consume progress");
        check(rules.CanFinish("picker", 15, LockpickRules.DefaultDuration), "a full fifteen seconds makes the attempt ready");
        check(rules.Succeed("picker", 15, 15, 120), "a successful pick consumes its completed attempt");
        check(rules.Remaining("picker", 15) == 120, "the full 120-second cooldown begins after success");
        check(!rules.Succeed("picker", 15, 15, 120), "a duplicate completion cannot extend or repeat a successful pick");
        rules.Cancel("picker");
        check(!rules.TryBegin("picker", 100), "role changes or reconnect cancellation cannot clear an earned cooldown");
        check(!rules.TryBegin("picker", 134.99f), "a picker cannot start just before the cooldown expires");
        check(rules.TryBegin("picker", 135), "the cooldown expires exactly 120 seconds after completion");
        rules.Cancel("picker");
        check(rules.TryBegin("picker", 136), "an interrupted attempt does not spend a new cooldown");
        check(!rules.CanFinish("picker", 150.99f, 15), "restarting an interrupted pick requires a new full fifteen seconds");
        check(rules.TryBegin("other player", 136), "different Locksmiths have independent attempt state");
        check(rules.Succeed("other player", 151, 15, 120), "one player's success does not cancel another attempt");
        check(rules.CanFinish("picker", 151, 15), "another Locksmith's cooldown does not affect this player's progress");
        rules.Cancel("picker");
        check(rules.Remaining("picker", 151) == 0, "a blocked native door interaction can cancel without spending cooldown");
        rules.Clear();
        check(rules.TryBegin("other player", 152), "a new round clears the prior round's cooldowns");
        check(!LockpickRules.IsLevelThree(16 | 32 | 128 | 256, 64, 512), "containment and armory levels one and two remain pickable");
        check(!LockpickRules.IsLevelThree(1 | 2 | 4 | 8, 64, 512), "checkpoint, exit, intercom and warhead flags are not misread as numeric level three");
        check(!LockpickRules.IsLevelThree(0, 64, 512), "an ordinary door denied by a lock has no level-three restriction");
        check(LockpickRules.IsLevelThree(64, 64, 512), "containment level three is excluded");
        check(LockpickRules.IsLevelThree(512, 64, 512), "armory level three is excluded");
        check(LockpickRules.IsLevelThree(1 | 32 | 512, 64, 512), "combined permissions cannot hide a level-three requirement");
        check(LockpickRules.IsLevelThree(0 | 64, 64, 512), "a checkpoint parent's level-three requirement also excludes its subdoor");
    }
}
