using System;

namespace Brainless.Internal
{
    internal static class ExplosionRules
    {
        // Same penetration used by the native HE explosion path. Zero causes
        // this game's handler constructor to return before initializing damage.
        internal const int DefaultPenetration = 50;
        internal static int Penetration(int configured) => Math.Max(1, Math.Min(100, configured));
        internal static float UpgradeStock(float value, float oldDefault, float newDefault, int revision) => revision < 1 && value == oldDefault ? newDefault : value;
    }
}
