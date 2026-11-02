using System.ComponentModel;
using Brainless.Internal;

namespace Brainless.Roles
{
    public sealed class ScpTraits
    {
        [Tune(.1, 5)]
        public float RecallDurationMultiplier { get; set; } = 1;

        [Tune(.1, 5)]
        public float AttackCooldownMultiplier { get; set; } = 1;

        [Tune(0, 5)]
        public float AttackDamageMultiplier { get; set; } = 1;

        [Tune(0, 10)]
        public float ShieldRegenRateMultiplier { get; set; } = 1;

        [Tune(.1, 10)]
        public float ShieldRegenDelayMultiplier { get; set; } = 1;

        [Tune(0, 10)]
        public float VigorGainMultiplier { get; set; } = 1;

        [Tune(0, 10)]
        public byte QuietFootstepStacks { get; set; }
        public bool ExplosionImmune { get; set; }
        public bool RefreshBreakneckOnKill { get; set; }

        [Tune(0, 60)]
        public float? RageWindupSeconds { get; set; }

        [Tune(.1, 3)]
        public float CalmSpeedMultiplier { get; set; } = 1;

        [Tune(0, 120)]
        public float? MimicSoundCooldownSeconds { get; set; }
        [Description("Retired. SCP-939 intercom access disconnected players and is no longer applied.")]
        public bool UseIntercom { get; set; }

        [Tune(0, 1000)]
        public float PoisonDamagePerSecond { get; set; }

        [Tune(0, 120)]
        public float PoisonDurationSeconds { get; set; }
        public bool PoisonInCloud { get; set; }
    }

    internal static class ScpTraitRules
    {
        internal static float Vigor(float before, float requested, float gainMultiplier) => requested <= before ? requested : System.Math.Min(1, before + (requested - before) * gainMultiplier);
        // Integrate only the remaining lifetime: a short poison must not gain an
        // extra full tick or stack damage when the same victim is hit repeatedly.
        internal static float PoisonDamage(float now, float lastTick, float expires, float perSecond) => System.Math.Max(0, System.Math.Min(now, expires) - lastTick) * perSecond;
    }
}
