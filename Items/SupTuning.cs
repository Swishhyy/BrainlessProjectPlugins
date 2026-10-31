using System;
using System.Collections.Generic;
using System.Linq;
using Brainless.Internal;
using Exiled.API.Enums;

namespace Brainless.Items
{
    public sealed class SupTuning
    {
        public AdrenalinePillTuning AdrenalinePills { get; set; } = new AdrenalinePillTuning();
        public Anti096Tuning Anti096Pills { get; set; } = new Anti096Tuning();
        public ExpiredPillTuning ExpiredPills { get; set; } = new ExpiredPillTuning();
        public InvisiblePillTuning InvisibilityPills { get; set; } = new InvisiblePillTuning();
        public BioTuning BioGrenade { get; set; } = new BioTuning();
        public HackingTuning HackingChip { get; set; } = new HackingTuning();
        public ReinforcementTuning ReinforcementRadio { get; set; } = new ReinforcementTuning();
        public ScrambleTuning ScrambleGoggles { get; set; } = new ScrambleTuning();
        public VolatilePillTuning VolatilePills { get; set; } = new VolatilePillTuning();

        internal void Validate()
        {
            Tuning.Validate(this, "sup");
            ExpiredPills.Validate();
        }
    }

    public class PillTuning
    {
        [Tune(0, 30)]
        public float UseSeconds { get; set; } = 2;

        [Tune(0, 3600)]
        public float CooldownSeconds { get; set; } = 5;
    }

    public sealed class AdrenalinePillTuning : PillTuning
    {
        [Tune(1, 2)]
        public float SpeedMultiplier { get; set; } = 1.35f;

        [Tune(.1, 300)]
        public float DurationSeconds { get; set; } = 25;

        [Tune(0, 100)]
        public float StaminaRestorePercent { get; set; } = 100;

        [Tune(0, 120)]
        public float ExhaustionSeconds { get; set; } = 5;
    }

    public sealed class Anti096Tuning : PillTuning
    {
        public bool ProvideTemporaryImmunity { get; set; }

        [Tune(0, 300)]
        public float ImmunitySeconds { get; set; } = 15;
    }

    public sealed class InvisiblePillTuning : PillTuning
    {
        [Tune(.1, 120)]
        public float DurationSeconds { get; set; } = 7;
        public bool OncePerPlayerPerRound { get; set; } = true;
        public bool HideFrom939 { get; set; } = true;
        public bool Hide939Footsteps { get; set; } = true;
        public bool BreakOnAttack { get; set; } = true;
    }

    public sealed class ExpiredPillTuning : PillTuning
    {
        [Tune(.1, 120)]
        public float DurationSeconds { get; set; } = 10;

        [Tune(0, 100)]
        public float PositiveWeight { get; set; } = 50;

        [Tune(0, 100)]
        public float NegativeWeight { get; set; } = 30;

        [Tune(0, 100)]
        public float ScpWeight { get; set; } = 20;

        [Tune(0, 1000)]
        public float FallbackMinHeal { get; set; } = 15;

        [Tune(0, 1000)]
        public float FallbackMaxHeal { get; set; } = 35;
        public List<RandomPillEffect> Effects { get; set; } = new List<RandomPillEffect>
        {
            E(EffectType.Scp207, "Positive", 10, 1, 3),
            E(EffectType.Invigorated, "Positive", 15, 1, 5),
            E(EffectType.BodyshotReduction, "Positive", 10, 1, 3),
            E(EffectType.Invisible, "Positive", 5, 1, 2),
            E(EffectType.Vitality, "Positive", 15, 1, 4),
            E(EffectType.DamageReduction, "Positive", 15, 1, 3),
            E(EffectType.AntiScp207, "Positive", 10, 1, 1),
            E(EffectType.Concussed, "Negative", 10, 1, 8),
            E(EffectType.Bleeding, "Negative", 12, 1, 10),
            E(EffectType.Burned, "Negative", 8, 1, 6),
            E(EffectType.Deafened, "Negative", 10, 1, 8),
            E(EffectType.Exhausted, "Negative", 12, 1, 10),
            E(EffectType.Flashed, "Negative", 10, 1, 6),
            E(EffectType.Disabled, "Negative", 5, 1, 6),
            E(EffectType.Ensnared, "Negative", 8, 1, 8),
            E(EffectType.Hemorrhage, "Negative", 5, 1, 10),
            E(EffectType.Poisoned, "Negative", 10, 1, 10),
            E(EffectType.Asphyxiated, "SCP", 15, 1, 10),
            E(EffectType.CardiacArrest, "SCP", 10, 1, 10),
            E(EffectType.Decontaminating, "SCP", 15, 1, 8),
            E(EffectType.SeveredHands, "SCP", 10, 1, 1),
            E(EffectType.Stained, "SCP", 10, 1, 8),
            E(EffectType.AmnesiaVision, "SCP", 15, 1, 1),
            E(EffectType.Corroding, "SCP", 10, 1, 8)
        };

        private static RandomPillEffect E(EffectType effect, string category, float weight, byte min, byte max) => new RandomPillEffect
        {
            Effect = effect,
            Category = category,
            Weight = weight,
            MinIntensity = min,
            MaxIntensity = max
        };
        internal void Validate()
        {
            if (FallbackMinHeal > FallbackMaxHeal || Effects == null || Effects.Any(e => e == null || !Enum.IsDefined(typeof(EffectType), e.Effect) || e.Effect == EffectType.None || !(e.Category == "Positive" || e.Category == "Negative" || e.Category == "SCP") || e.MinIntensity < 1 || e.MaxIntensity < e.MinIntensity))
                throw new InvalidOperationException("Invalid expired pill effects or healing range.");
            foreach (var effect in Effects)
                Tuning.Validate(effect, "sup.expired_pills.effects");
        }
    }

    public sealed class RandomPillEffect
    {
        public EffectType Effect { get; set; }
        public string Category { get; set; } = "Positive";

        [Tune(0, 100000)]
        public float Weight { get; set; } = 10;
        public byte MinIntensity { get; set; } = 1;
        public byte MaxIntensity { get; set; } = 1;

        [Tune(0, 120)]
        public float DurationSeconds { get; set; }
    }

    public sealed class BioTuning
    {
        [Tune(.1, 30)]
        public float FuseSeconds { get; set; } = 3;

        [Tune(.1, 180)]
        public float DurationSeconds { get; set; } = 30;

        [Tune(.1, 20)]
        public float Radius { get; set; } = 5;

        [Tune(.1, 120)]
        public float ExposureSeconds { get; set; } = 10;

        [Tune(.1, 30)]
        public float CorrodingSeconds { get; set; } = 3;
        public bool AffectOwner { get; set; } = true;
        public bool AffectScps { get; set; } = true;
        public bool RequireLineOfSight { get; set; } = true;
        public bool SameRoomOnly { get; set; } = true;
        public bool FilterProtects { get; set; } = true;

        [Tune(0, 1000)]
        public float AllyHealAmount { get; set; }

        [Tune(0, 1000)]
        public float AllyAhpAmount { get; set; }

        [Tune(.25, 60)]
        public float HealingIntervalSeconds { get; set; } = 5;
        public bool ContinuousHealing { get; set; } = true;
    }

    public sealed class HackingTuning
    {
        [Tune(.1, 60)]
        public float UseSeconds { get; set; } = 8;

        [Tune(.5, 5)]
        public float Reach { get; set; } = 2;

        [Tune(0, 2)]
        public int MaxAccessLevel { get; set; } = 2;
        public bool AllowCheckpoints { get; set; }
    }

    public sealed class ReinforcementTuning
    {
        [Tune(0, 3600)]
        public float CooldownSeconds { get; set; } = 300;

        [Tune(0, 120)]
        public float ArrivalSeconds { get; set; } = 15;

        [Tune(1, 10)]
        public int PlayersPerCall { get; set; } = 3;

        [Tune(0, 10)]
        public int CallsPerFactionPerRound { get; set; } = 1;

        [Tune(0, 1000)]
        public int MinimumPlayers { get; set; }

        [Tune(0, 1800)]
        public float EarliestRoundSeconds { get; set; } = 60;

        [Tune(.1, 30)]
        public float UseSeconds { get; set; } = 3;
    }

    public sealed class ScrambleTuning
    {
        [Tune(.1, 1800)]
        public float BatterySeconds { get; set; } = 60;

        [Tune(0, 100)]
        public float FailureChancePercent { get; set; }
    }

    public sealed class VolatilePillTuning : PillTuning
    {
        [Tune(0, 100)]
        public float SurvivalChancePercent { get; set; } = 5;

        [Tune(1, 1000)]
        public float SurvivalHealth { get; set; } = 5;

        [Tune(.1, 30)]
        public float FuseSeconds { get; set; } = .5f;

        [Tune(.1, 20)]
        public float Radius { get; set; } = 6;

        [Tune(0, 1000)]
        public float HumanDamage { get; set; } = 70;

        [Tune(0, 5000)]
        public float ScpDamage { get; set; } = 200;

        [Tune(1, 100)]
        public int ArmorPenetration { get; set; } = 50;
        public bool RequireLineOfSight { get; set; } = true;
    }
}
