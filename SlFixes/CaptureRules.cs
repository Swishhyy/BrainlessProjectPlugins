namespace Brainless.SlFixes
{
    internal enum CaptureSide
    {
        None,
        Foundation,
        Chaos
    }

    internal static class CaptureRules
    {
        internal static CaptureSide Destination(CaptureSide captor, bool foundationMilitary, bool chaosMilitary, bool rogue, bool cuffed, bool captorAlive, double lifeSeconds, double minimumSeconds)
        {
            if (!cuffed || !captorAlive || lifeSeconds < minimumSeconds)
                return CaptureSide.None;
            if (captor == CaptureSide.Chaos && (foundationMilitary || rogue))
                return CaptureSide.Chaos;
            if (captor == CaptureSide.Foundation && (chaosMilitary || rogue))
                return CaptureSide.Foundation;
            return CaptureSide.None;
        }
    }
}
