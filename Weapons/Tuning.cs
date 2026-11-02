using Brainless.Internal;

namespace Brainless.Weapons
{
    public sealed class WeaponTuning
    {
        public RailgunTuning Railgun { get; set; } = new RailgunTuning();
        public NullifierTuning Nullifier { get; set; } = new NullifierTuning();
        public BreachTuning BreachShotgun { get; set; } = new BreachTuning();
        public RicochetTuning Ricochet { get; set; } = new RicochetTuning();
        public WeaponSupplyTuning Supply { get; set; } = new WeaponSupplyTuning();
        public EffectAudioTuning Audio { get; set; } = new EffectAudioTuning();

        internal int UpgradeAudio(int revision)
        {
            return System.Math.Max(1, revision);
        }
    }

    public sealed class RailgunTuning
    {
        [Tune(.1, 30)]
        public float ChargeSeconds { get; set; } = 5;

        [Tune(0, 5)]
        public float DischargeDelaySeconds { get; set; } = .75f;

        [Tune(1, 150)]
        public float Range { get; set; } = 50;

        [Tune(.05, 2)]
        public float BeamRadius { get; set; } = .5f;

        [Tune(.05, 5)]
        public float BeamSeconds { get; set; } = .65f;

        [Tune(0, 1000)]
        public float HumanDamage { get; set; } = 85;

        [Tune(0, 5000)]
        public float ScpDamage { get; set; } = 250;
        public bool ShowChargeProgress { get; set; } = true;
        public bool ShowChargingEffects { get; set; } = true;

        [Tune(3, 16)]
        public int RingSegments { get; set; } = 8;

        [Tune(1, 4)]
        public int MaxConcurrentCharges { get; set; } = 2;
        public bool ImpactExplosion { get; set; } = true;

        [Tune(.1, 10)]
        public float ImpactRadius { get; set; } = 2;

        [Tune(0, 1000)]
        public float ImpactHumanDamage { get; set; } = 20;

        [Tune(0, 5000)]
        public float ImpactScpDamage { get; set; } = 50;
    }

    public sealed class NullifierTuning
    {
        [Tune(0, 100)]
        public float ShieldDamageMultiplier { get; set; } = 2;
    }

    public sealed class BreachTuning
    {
        [Tune(1, 20)]
        public int ShellsPerCycle { get; set; } = 2;

        [Tune(1, 100)]
        public int CyclesToBreach { get; set; } = 3;

        [Tune(.1, 3600)]
        public float ProgressResetSeconds { get; set; } = 6;

        [Tune(.1, 20)]
        public float Reach { get; set; } = 2;
    }

    public sealed class RicochetTuning
    {
        [Tune(.1, 150)]
        public float Range { get; set; } = 20;

        [Tune(0, 100)]
        public float BounceDamageMultiplier { get; set; } = .5f;

        [Tune(.01, 10)]
        public float TracerSeconds { get; set; } = .12f;
    }

    public sealed class WeaponSupplyTuning
    {
        [Tune(0, 10000)]
        public int SmallLobbyCapacity { get; set; } = 1;

        [Tune(0, 10000)]
        public int LargeLobbyCapacity { get; set; } = 2;

        [Tune(1, 1000)]
        public int LargeLobbyPlayers { get; set; } = 24;

        [Tune(0, 100)]
        public int WorldLootCount { get; set; } = 1;

        internal int Capacity(int players) => players < LargeLobbyPlayers ? SmallLobbyCapacity : LargeLobbyCapacity;
    }
}
