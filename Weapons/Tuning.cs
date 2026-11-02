using Brainless.Internal;

namespace Brainless.Weapons
{
    public sealed class WeaponTuning
    {
        public RicochetTuning Ricochet { get; set; } = new RicochetTuning();
        public StunGunTuning StunGun { get; set; } = new StunGunTuning();
        public WeaponSupplyTuning Supply { get; set; } = new WeaponSupplyTuning();
        public EffectAudioTuning Audio { get; set; } = new EffectAudioTuning();

        internal int UpgradeAudio(int revision)
        {
            return System.Math.Max(1, revision);
        }
    }

    public sealed class StunGunTuning
    {
        [Tune(.5, 30)]
        public float StunSeconds { get; set; } = 3;

        [Tune(0, 300)]
        public float TargetCooldownSeconds { get; set; } = 10;
        public bool BlindTarget { get; set; } = true;
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
