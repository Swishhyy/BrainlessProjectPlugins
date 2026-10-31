using System;

namespace Brainless.Internal
{
    internal static class AudioRules
    {
        internal const float EffectVolume = 2.5f;
        internal const float FullVolumeRange = 6;
        internal const float EffectRange = 20;
        internal const float VoiceVolume = 3.5f;
        internal static float UpgradeStock(float value, float previous, float updated, int revision) => revision < 1 && value == previous ? updated : value;
        internal static float FullVolumeDistance(float requested, float range)
        {
            if (float.IsNaN(range) || float.IsInfinity(range) || range < .1f)
                range = EffectRange;
            if (float.IsNaN(requested) || float.IsInfinity(requested))
                requested = FullVolumeRange;
            return Math.Max(.1f, Math.Min(requested, range));
        }
    }
}
