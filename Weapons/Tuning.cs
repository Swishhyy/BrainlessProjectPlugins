using Brainless.Internal;

namespace Brainless.Weapons
{
    public sealed class WeaponTuning
    {
        public NullifierTuning Nullifier { get; set; } = new NullifierTuning();
        public RattlerTuning Rattler { get; set; } = new RattlerTuning();
        public BreachTuning BreachShotgun { get; set; } = new BreachTuning();
        public RedlineTuning Redline { get; set; } = new RedlineTuning();
        public RicochetTuning Ricochet { get; set; } = new RicochetTuning();
        public WeaponSupplyTuning Supply { get; set; } = new WeaponSupplyTuning();
    }

    public sealed class NullifierTuning
    {
        [Tune(0, 100)]
        public float ShieldDamageMultiplier { get; set; } = 1.2f;
    }

    public sealed class RattlerTuning
    {
        [Tune(1, 100)]
        public int HitsToTag { get; set; } = 3;

        [Tune(.01, 120)]
        public float HitWindowSeconds { get; set; } = 2;

        [Tune(.1, 120)]
        public float TagDurationSeconds { get; set; } = 3;

        [Tune(0, 3600)]
        public float ImmunitySeconds { get; set; } = 11;

        [Tune(.1, 100)]
        public float AudioRange { get; set; } = 16;

        [Tune(0, 2)]
        public float AudioVolume { get; set; } = .65f;
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

    public sealed class RedlineTuning
    {
        [Tune(0, 10000)]
        public float HeatPerShot { get; set; } = 9;

        [Tune(1, 10000)]
        public float MaxHeat { get; set; } = 100;

        [Tune(0, 120)]
        public float VentSeconds { get; set; } = 3;

        [Tune(0, 120)]
        public float CoolingDelaySeconds { get; set; } = .6f;

        [Tune(0, 10000)]
        public float CoolingPerSecond { get; set; } = 25;
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
