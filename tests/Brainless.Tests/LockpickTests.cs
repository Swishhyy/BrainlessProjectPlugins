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
        check(LockpickRules.Duration(1, 15, 15) == 15 && LockpickRules.Duration(2, 15, 15) == 30 && LockpickRules.Duration(3, 15, 15) == 45, "all keycard tiers are pickable with progressively longer attempts");
        check(LockpickRules.Cooldown(1, 120, 60) == 120 && LockpickRules.Cooldown(2, 120, 60) == 180 && LockpickRules.Cooldown(3, 120, 60) == 240, "successful higher-tier picks receive progressively longer cooldowns");
        check(LockpickRules.Duration(3, 8, 4) == 16 && LockpickRules.Cooldown(3, 60, 30) == 120, "operators can tune base and per-level lockpick timing independently");
        check(LockpickRules.Duration(0, 15, 15) == 15 && LockpickRules.Duration(99, 15, 15) == 45, "unknown access levels remain within the supported timing tiers");
    }
}
