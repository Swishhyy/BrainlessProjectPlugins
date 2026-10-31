using System;

namespace Brainless.Weapons
{
    internal static class LauncherRules
    {
        internal const int MaxAmmo = 16;
        internal static int Capacity(int configured) => Math.Min(MaxAmmo, configured);
    }
}
