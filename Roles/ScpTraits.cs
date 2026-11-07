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

        [Description("Multiplies the damage this role deals (SCP attacks for SCPs, any damage for humans).")]
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

        [Description("Health regained per second after the SCP has taken no damage for health_regen_delay_seconds (like SCP-173's shield, but for health). 0 disables.")]
        [Tune(0, 1000)]
        public float HealthRegenPerSecond { get; set; }

        [Tune(0, 120)]
        public float HealthRegenDelaySeconds { get; set; } = 10;

        [Description("Regen stops at this much health. 0 means it can heal to full.")]
        [Tune(0, 100000)]
        public float HealthRegenMax { get; set; }

        [Description("Humans within this many metres are poisoned (uses poison_damage_per_second/duration). 0 disables.")]
        [Tune(0, 30)]
        public float PoisonAuraRadius { get; set; }

        [Description("Health regained for each kill.")]
        [Tune(0, 10000)]
        public float HealOnKill { get; set; }

        [Description("Health regained each time this role's attack damages a human.")]
        [Tune(0, 10000)]
        public float HealOnHit { get; set; }

        [Description("SCP-106: health regained whenever a human dies in the pocket dimension.")]
        [Tune(0, 10000)]
        public float PocketDeathHeal { get; set; }

        [Description("SCP-106: every human death in the pocket dimension refills vigor to full.")]
        public bool PocketDeathVigor { get; set; }

        [Description("Attack hits make humans bleed for this many seconds. 0 disables.")]
        [Tune(0, 120)]
        public float BleedOnHitSeconds { get; set; }

        [Description("Chance per attack hit to destroy the victim's armor.")]
        [Tune(0, 100)]
        public float ArmorBreakChancePercent { get; set; }

        [Description("Leaves a slowing SCP-173 tantrum puddle every this many seconds while moving. 0 disables.")]
        [Tune(0, 120)]
        public float TarTrailSeconds { get; set; }

        [Description("SCP-173: Breakneck Speeds cooldown multiplier.")]
        [Tune(.05, 5)]
        public float BreakneckCooldownMultiplier { get; set; } = 1;

        [Description("SCP-173: tantrum cooldown multiplier.")]
        [Tune(.05, 5)]
        public float TantrumCooldownMultiplier { get; set; } = 1;

        [Description("SCP-096: seconds of rage added per kill while enraged.")]
        [Tune(0, 60)]
        public float RageExtendOnKillSeconds { get; set; }

        [Description("SCP-049: zombies this role revives get this health multiplier on top of their own.")]
        [Tune(.1, 10)]
        public float RevivedZombieHealthMultiplier { get; set; } = 1;

        [Description("SCP-049: zombies this role revives move this many percent faster.")]
        [Tune(0, 100)]
        public byte RevivedZombieSpeedPercent { get; set; }

        [Description("SCP-049: each zombie this role revives becomes a random zombie variant (Common to Rare).")]
        public bool RandomZombieVariant { get; set; }

        [Description("Once per life, a killing blow leaves the player at 1 health with 3 seconds of invulnerability and a speed burst.")]
        public bool SecondWind { get; set; }

        [Description("SCP-079: auxiliary power regeneration multiplier.")]
        [Tune(.1, 5)]
        public float AuxRegenMultiplier { get; set; } = 1;

        [Description("SCP-079: power cost multiplier for every action.")]
        [Tune(0, 5)]
        public float AuxCostMultiplier { get; set; } = 1;

        [Description("SCP-079: extra power cost multiplier for switching cameras.")]
        [Tune(0, 5)]
        public float CameraCostMultiplier { get; set; } = 1;

        [Description("SCP-079: extra power cost multiplier for opening and closing doors.")]
        [Tune(0, 5)]
        public float DoorCostMultiplier { get; set; } = 1;

        [Description("SCP-079: experience gain multiplier.")]
        [Tune(0, 10)]
        public float ExperienceMultiplier { get; set; } = 1;

        [Description("SCP-079: blackout and lockdown cooldown multiplier.")]
        [Tune(.05, 5)]
        public float AbilityCooldownMultiplier { get; set; } = 1;

        [Description("SCP-079: starts at this access tier (1-5; 0 keeps the native start).")]
        [Tune(0, 5)]
        public int StartTier { get; set; }

        [Description("Extra random SCP items in the starting kit.")]
        [Tune(0, 4)]
        public int RandomScpItems { get; set; }
    }

    internal static class ScpTraitRules
    {
        internal static float Vigor(float before, float requested, float gainMultiplier) => requested <= before ? requested : System.Math.Min(1, before + (requested - before) * gainMultiplier);
        // Integrate only the remaining lifetime: a short poison must not gain an
        // extra full tick or stack damage when the same victim is hit repeatedly.
        internal static float PoisonDamage(float now, float lastTick, float expires, float perSecond) => System.Math.Max(0, System.Math.Min(now, expires) - lastTick) * perSecond;
    }
}
