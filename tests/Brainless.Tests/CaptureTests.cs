using System;
using Brainless.SlFixes;

internal static class CaptureTests
{
    internal static void Run(Action<bool, string> check)
    {
        CaptureSide Pick(CaptureSide captor, bool foundation = false, bool chaos = false, bool rogue = false, bool cuffed = true, bool alive = true, double age = 10) => CaptureRules.Destination(captor, foundation, chaos, rogue, cuffed, alive, age, 10);
        check(Pick(CaptureSide.Chaos, foundation: true) == CaptureSide.Chaos && Pick(CaptureSide.Foundation, chaos: true) == CaptureSide.Foundation, "cuffed Foundation military and Chaos captives convert toward the opposing captor's faction");
        check(Pick(CaptureSide.Foundation, rogue: true) == CaptureSide.Foundation && Pick(CaptureSide.Chaos, rogue: true) == CaptureSide.Chaos, "a neutral Rogue can be recruited by either captor faction");
        check(Pick(CaptureSide.Foundation, foundation: true) == CaptureSide.None && Pick(CaptureSide.Chaos, chaos: true) == CaptureSide.None && Pick(CaptureSide.None, rogue: true) == CaptureSide.None, "allied cuffs and unknown/neutral captors cannot convert a military role");
        check(Pick(CaptureSide.Chaos, foundation: true, cuffed: false) == CaptureSide.None && Pick(CaptureSide.Chaos, foundation: true, alive: false) == CaptureSide.None && Pick(CaptureSide.Chaos, foundation: true, age: 9.99) == CaptureSide.None, "conversion requires active cuffs, a living captor and the full minimum life time");
        check(Pick(CaptureSide.Chaos) == CaptureSide.None && Pick(CaptureSide.Foundation) == CaptureSide.None, "ordinary Class-D/scientist escapes and unrelated roles retain native escape behavior");
    }
}
