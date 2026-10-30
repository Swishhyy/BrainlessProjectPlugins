using Brainless.Internal;

namespace Brainless.Roles
{
    public sealed class ExplosiveZombieTuning
    {
        [Tune(0, 120)]
        public float WarningSeconds { get; set; } = 2;

        [Tune(.1, 60)]
        public float Radius { get; set; } = 3;

        [Tune(0, 100000)]
        public float HumanDamage { get; set; } = 80;

        [Tune(0, 100000)]
        public float ScpDamage { get; set; }
        public bool RequireLineOfSight { get; set; } = true;
        public bool RequireEnemyKill { get; set; }
        public bool TriggerOnEnvironmentalDeaths { get; set; } = true;
        public bool TriggerOnExplosionDeaths { get; set; } = true;

        [Tune(0, 1000)]
        public int MaxPerPlayerPerRound { get; set; }

        [Tune(0, 1000)]
        public int MaxConcurrentBursts { get; set; }
        public bool ShowWarning { get; set; } = true;
        public bool PlayWarningSound { get; set; } = true;

        [Tune(.1, 100)]
        public float WarningRange { get; set; } = 10;

        internal bool Allows(bool hasAttacker, bool enemy, bool explosion) => (!RequireEnemyKill || enemy) && (TriggerOnEnvironmentalDeaths || hasAttacker) && (TriggerOnExplosionDeaths || !explosion);
    }
}
