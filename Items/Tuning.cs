using Brainless.Internal;

namespace Brainless.Items
{
    public sealed class ItemTuning
    {
        public GasTuning GasGrenade { get; set; } = new GasTuning();
        public ClusterTuning ClusterGrenade { get; set; } = new ClusterTuning();
        public ImpactTuning ImpactGrenade { get; set; } = new ImpactTuning();
        public FoamTuning ContainmentFoam { get; set; } = new FoamTuning();
        public EchoTuning EchoCanister { get; set; } = new EchoTuning();
        public BraceTuning DoorBrace { get; set; } = new BraceTuning();
        public JammerTuning SignalJammer { get; set; } = new JammerTuning();
        public BeaconTuning SurveyBeacon { get; set; } = new BeaconTuning();
        public ChalkTuning TraceChalk { get; set; } = new ChalkTuning();
        public DressingTuning FieldDressing { get; set; } = new DressingTuning();
        public FilterTuning FilterInhaler { get; set; } = new FilterTuning();
        public SpecimenTuning PreservedSpecimen { get; set; } = new SpecimenTuning();
        public InteractionTuning Interaction { get; set; } = new InteractionTuning();
        public SupplyTuning Supply { get; set; } = new SupplyTuning();
        public EffectAudioTuning Audio { get; set; } = new EffectAudioTuning();

        internal int UpgradeAudio(int revision)
        {
            EchoCanister.AudioVolume = AudioRules.UpgradeStock(EchoCanister.AudioVolume, .65f, AudioRules.EffectVolume, revision);
            return System.Math.Max(1, revision);
        }

        internal int UpgradeGrenades(int revision)
        {
            if (revision >= 2)
                return revision;
            var c = ClusterGrenade;
            c.Radius = ExplosionRules.UpgradeStock(c.Radius, 2.75f, 4, revision);
            c.HumanDamage = ExplosionRules.UpgradeStock(c.HumanDamage, 40, 70, revision);
            c.ScpDamage = ExplosionRules.UpgradeStock(c.ScpDamage, 100, 180, revision);
            c.SharedHumanDamageCap = ExplosionRules.UpgradeStock(c.SharedHumanDamageCap, 85, 150, revision);
            c.SharedScpDamageCap = ExplosionRules.UpgradeStock(c.SharedScpDamageCap, 225, 500, revision);
            c.ChildFuseSeconds = ExplosionRules.UpgradeStock(c.ChildFuseSeconds, 1.5f, 1.2f, revision);
            c.HorizontalSpeed = ExplosionRules.UpgradeStock(c.HorizontalSpeed, 4.5f, 3, revision);
            c.UpwardSpeed = ExplosionRules.UpgradeStock(c.UpwardSpeed, 4, 3, revision);
            var impact = ImpactGrenade;
            impact.Radius = ExplosionRules.UpgradeStock(impact.Radius, 3.5f, 5, revision);
            impact.HumanDamage = ExplosionRules.UpgradeStock(impact.HumanDamage, 80, 140, revision);
            impact.ScpDamage = ExplosionRules.UpgradeStock(impact.ScpDamage, 180, 350, revision);
            c.Radius = ExplosionRules.UpgradeStock(c.Radius, 4, 5.5f, revision - 1);
            c.HumanDamage = ExplosionRules.UpgradeStock(c.HumanDamage, 70, 120, revision - 1);
            c.ScpDamage = ExplosionRules.UpgradeStock(c.ScpDamage, 180, 300, revision - 1);
            c.ParentRadius = ExplosionRules.UpgradeStock(c.ParentRadius, 4.5f, 6, revision - 1);
            c.ParentHumanDamage = ExplosionRules.UpgradeStock(c.ParentHumanDamage, 90, 180, revision - 1);
            c.ParentScpDamage = ExplosionRules.UpgradeStock(c.ParentScpDamage, 250, 500, revision - 1);
            c.SharedHumanDamageCap = ExplosionRules.UpgradeStock(c.SharedHumanDamageCap, 150, 300, revision - 1);
            c.SharedScpDamageCap = ExplosionRules.UpgradeStock(c.SharedScpDamageCap, 500, 1200, revision - 1);
            c.ChildFuseSeconds = ExplosionRules.UpgradeStock(c.ChildFuseSeconds, 1.2f, .9f, revision - 1);
            c.ChildFuseStaggerSeconds = ExplosionRules.UpgradeStock(c.ChildFuseStaggerSeconds, .08f, .06f, revision - 1);
            c.HorizontalSpeed = ExplosionRules.UpgradeStock(c.HorizontalSpeed, 3, 5, revision - 1);
            c.UpwardSpeed = ExplosionRules.UpgradeStock(c.UpwardSpeed, 3, 3.5f, revision - 1);
            return 2;
        }
    }

    public class FieldTuning
    {
        [Tune(.05, 120)]
        public float FuseSeconds { get; set; } = 2;

        [Tune(.1, 3600)]
        public float DurationSeconds { get; set; } = 6;

        [Tune(.5, 30)]
        public float Radius { get; set; } = 3;

        [Tune(0, 100)]
        public float HumanSlowPercent { get; set; } = 10;

        [Tune(0, 100)]
        public float ScpSlowPercent { get; set; } = 8;

        [Tune(0, 10000)]
        public float HumanDamagePerSecond { get; set; }

        [Tune(0, 10000)]
        public float ScpDamagePerSecond { get; set; }
        public bool AffectAllies { get; set; }
        public bool AffectOwner { get; set; } = true;
        public bool SameRoomOnly { get; set; } = true;
        public bool RequireLineOfSight { get; set; } = true;
    }

    public sealed class GasTuning
    {
        internal void Upgrade(int catalogRevision)
        {
            if (catalogRevision < 3 && DurationSeconds == 8)
                DurationSeconds = 20;
        }

        [Tune(.05, 120)]
        public float FuseSeconds { get; set; } = 2.5f;

        [Tune(.1, 3600)]
        public float DurationSeconds { get; set; } = 20;

        [Tune(1, 6)]
        public int SmokeEmitters { get; set; } = 1;
    }

    public sealed class FoamTuning : FieldTuning
    {
        public FoamTuning()
        {
            DurationSeconds = 8;
            HumanSlowPercent = 25;
            ScpSlowPercent = 15;
        }

        internal void Upgrade(int catalogRevision)
        {
            if (catalogRevision >= 5)
                return;
            if (DurationSeconds == 6)
                DurationSeconds = 8;
            if (HumanSlowPercent == 10)
                HumanSlowPercent = 25;
            if (ScpSlowPercent == 8)
                ScpSlowPercent = 15;
        }
    }

    public class BlastTuning
    {
        [Tune(1, 100)]
        public int ArmorPenetration { get; set; } = ExplosionRules.DefaultPenetration;

        [Tune(.1, 60)]
        public float Radius { get; set; } = 5;

        [Tune(0, 100000)]
        public float HumanDamage { get; set; } = 140;

        [Tune(0, 100000)]
        public float ScpDamage { get; set; } = 350;
        public bool RequireLineOfSight { get; set; } = true;
        public bool AffectOwner { get; set; } = true;
    }

    public sealed class ClusterTuning : BlastTuning
    {
        public bool DamageEnvironment { get; set; } = true;

        [Tune(.1, 60)]
        public float ParentRadius { get; set; } = 6;

        [Tune(0, 100000)]
        public float ParentHumanDamage { get; set; } = 180;

        [Tune(0, 100000)]
        public float ParentScpDamage { get; set; } = 500;

        public ClusterTuning()
        {
            Radius = 5.5f;
            HumanDamage = 120;
            ScpDamage = 300;
        }

        [Tune(.05, 120)]
        public float FuseSeconds { get; set; } = 3;

        [Tune(1, 32)]
        public int GrenadeCount { get; set; } = 4;

        [Tune(.05, 120)]
        public float ChildFuseSeconds { get; set; } = .9f;

        [Tune(0, 10)]
        public float ChildFuseStaggerSeconds { get; set; } = .06f;

        [Tune(0, 50)]
        public float HorizontalSpeed { get; set; } = 5;

        [Tune(0, 50)]
        public float UpwardSpeed { get; set; } = 3.5f;

        [Tune(0, 100000)]
        public float SharedHumanDamageCap { get; set; } = 300;

        [Tune(0, 100000)]
        public float SharedScpDamageCap { get; set; } = 1200;
    }

    public sealed class ImpactTuning : BlastTuning
    {
        [Tune(0, 5)]
        public float ArmingDelaySeconds { get; set; } = .1f;

        [Tune(.1, 120)]
        public float SafetyFuseSeconds { get; set; } = 10;
        public bool DetonateOnPlayers { get; set; }
    }

    public sealed class EchoTuning
    {
        [Tune(.05, 120)]
        public float FuseSeconds { get; set; } = 1;

        [Tune(.1, 120)]
        public float DurationSeconds { get; set; } = 6;

        [Tune(1, 100)]
        public float AudioRange { get; set; } = 16;

        [Tune(0, 10)]
        public float AudioVolume { get; set; } = AudioRules.EffectVolume;

        [Tune(.1, 100)]
        public float AudioFullVolumeRange { get; set; } = AudioRules.FullVolumeRange;
    }

    public sealed class BraceTuning
    {
        internal void Upgrade(int catalogRevision)
        {
            if (catalogRevision < 6 && DurationSeconds == 8)
                DurationSeconds = 25;
        }

        [Tune(0, 120)]
        public float DeploySeconds { get; set; } = 2;

        [Tune(0, 120)]
        public float RemoveSeconds { get; set; } = 3;

        [Tune(.1, 3600)]
        public float DurationSeconds { get; set; } = 25;

        [Tune(.5, 10)]
        public float Reach { get; set; } = 3;
    }

    public sealed class JammerTuning
    {
        [Tune(0, 120)]
        public float DeploySeconds { get; set; } = 2;

        [Tune(0, 120)]
        public float ArmingSeconds { get; set; } = 2;

        [Tune(.1, 3600)]
        public float ActiveSeconds { get; set; } = 6;

        [Tune(0, 3600)]
        public float RoomCooldownSeconds { get; set; } = 30;

        [Tune(0, 120)]
        public float RemoveSeconds { get; set; } = 2;

        [Tune(1, 64)]
        public int MaxActive { get; set; } = 1;
    }

    public sealed class BeaconTuning
    {
        [Tune(0, 120)]
        public float DeploySeconds { get; set; } = 2;

        [Tune(.1, 3600)]
        public float DurationSeconds { get; set; } = 45;

        [Tune(0, 120)]
        public float ScanSeconds { get; set; } = 3;

        [Tune(1, 100)]
        public int Scans { get; set; } = 2;

        [Tune(0, 120)]
        public float RemoveSeconds { get; set; } = 2;
    }

    public sealed class ChalkTuning
    {
        [Tune(.1, 3600)]
        public float DurationSeconds { get; set; } = 45;

        [Tune(1, 200)]
        public int MaxMarkers { get; set; } = 9;
    }

    public sealed class DressingTuning
    {
        [Tune(0, 120)]
        public float UseSeconds { get; set; } = 4;

        [Tune(0, 100000)]
        public float HealAmount { get; set; } = 35;
    }

    public sealed class FilterTuning
    {
        [Tune(0, 120)]
        public float UseSeconds { get; set; } = 2;

        [Tune(.1, 3600)]
        public float DurationSeconds { get; set; } = 12;
        public bool BlocksSlow { get; set; } = true;
        public bool BlocksDamage { get; set; } = true;
    }

    public sealed class SpecimenTuning
    {
        [Tune(0, 120)]
        public float UseSeconds { get; set; } = 4;

        [Tune(0, 100000)]
        public float HealAmount { get; set; } = 80;

        [Tune(.5, 10)]
        public float Reach { get; set; } = 2;
        public bool OncePerPlayerPerRound { get; set; } = true;
    }

    public sealed class InteractionTuning
    {
        [Tune(.5, 10)]
        public float DeviceReach { get; set; } = 3;

        [Tune(.01, 10)]
        public float MoveTolerance { get; set; } = .6f;
        public bool CancelOnDamage { get; set; } = true;

        [Tune(.05, 2)]
        public float TickSeconds { get; set; } = .1f;
    }

    public sealed class SupplyTuning
    {
        [Tune(0, 10000)]
        public int SmallLobbyCapacity { get; set; } = 6;

        [Tune(0, 10000)]
        public int MediumLobbyCapacity { get; set; } = 10;

        [Tune(0, 10000)]
        public int LargeLobbyCapacity { get; set; } = 14;

        [Tune(0, 1000)]
        public int MediumLobbyPlayers { get; set; } = 12;

        [Tune(1, 1000)]
        public int LargeLobbyPlayers { get; set; } = 24;

        [Tune(0, 1000)]
        public int ClusterMinPlayers { get; set; } = 12;

        [Tune(0, 1000)]
        public int SmallLobbyClusters { get; set; }

        [Tune(0, 1000)]
        public int MediumLobbyClusters { get; set; } = 1;

        [Tune(0, 1000)]
        public int LargeLobbyClusters { get; set; } = 2;

        [Tune(0, 100)]
        public int WorldLootCount { get; set; } = 2;

        [System.ComponentModel.Description("Legacy setting; smoke-only grenades no longer require or create a filter.")]
        public bool PairGasWithFilter { get; set; }

        internal int Capacity(int players) => players < MediumLobbyPlayers ? SmallLobbyCapacity : players < LargeLobbyPlayers ? MediumLobbyCapacity : LargeLobbyCapacity;
        internal int Clusters(int players) => players < MediumLobbyPlayers ? SmallLobbyClusters : players < LargeLobbyPlayers ? MediumLobbyClusters : LargeLobbyClusters;
    }
}
