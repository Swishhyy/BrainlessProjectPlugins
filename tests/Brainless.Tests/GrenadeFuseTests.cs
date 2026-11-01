using System;
using Brainless.Items;

internal static class GrenadeFuseTests
{
    internal static void Run(Action<bool, string> check)
    {
        var fuses = new GrenadeFuses<string>();
        int effects = 0, retired = 0;
        foreach (string name in new[]
        {
            "gas",
            "foam",
            "bio",
            "echo",
            "cluster"
        }

        )
        {
            int serial = effects + 7101;
            fuses.Arm(serial, name);
            check(fuses.Resolve(serial, true, p =>
            {
                check(p == name, name + " keeps its payload across the native throw/fuse boundary");
                effects++;
            }, p => retired++), name + " resolves its first native fuse");
            check(!fuses.Resolve(serial, true, p => effects++, p => retired++), name + " ignores a duplicate explosion callback");
        }

        check(effects == 5 && retired == 5, "utility and HE callbacks cannot apply a grenade twice or leak consumed payloads");
        fuses.Arm(90, "cancelled");
        fuses.Resolve(90, false, _ => effects++, _ => retired++);
        check(effects == 5 && retired == 6 && !fuses.TryGet(90, out _), "another plugin's cancellation retires the native projectile without creating an effect");
        fuses.Arm(91, "reentrant");
        fuses.Resolve(91, true, _ => check(!fuses.Resolve(91, true, __ => effects++, __ => retired++), "a reentrant native callback cannot resurrect a consumed grenade"), _ => retired++);
        fuses.Arm(92, "failing smoke");
        bool failed = false;
        try
        {
            fuses.Resolve(92, true, _ => throw new InvalidOperationException("fixture"), _ => retired++);
        }
        catch (InvalidOperationException)
        {
            failed = true;
        }

        check(failed && retired == 8 && !fuses.TryGet(92, out _), "failed smoke/effect creation still cleans the spent native grenade and does not retry a partial effect");
        fuses.Arm(93, "old round");
        fuses.Clear();
        check(!fuses.Resolve(93, true, _ => effects++, _ => retired++), "a round reset discards armed grenade payloads");
        fuses.Arm(93, "new round");
        check(fuses.Resolve(93, true, p => check(p == "new round", "a recycled item serial cannot reuse an old-round payload"), _ =>
        {
        }), "a fresh grenade can reuse a serial after reset");
    }
}
