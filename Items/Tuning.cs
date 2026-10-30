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

    public sealed class GasTuning : FieldTuning
    {
        public GasTuning()
        {
            FuseSeconds = 2.5f;
            DurationSeconds = 8;
            Radius = 4;
            HumanSlowPercent = 20;
            ScpSlowPercent = 0;
        }

        [Tune(1, 6)]
        public int SmokeEmitters { get; set; } = 2;
    }

    public sealed class FoamTuning : FieldTuning
    {
    }

    public class BlastTuning
    {
        [Tune(.1, 60)]
        public float Radius { get; set; } = 3.5f;

        [Tune(0, 100000)]
        public float HumanDamage { get; set; } = 80;

        [Tune(0, 100000)]
        public float ScpDamage { get; set; } = 180;
        public bool RequireLineOfSight { get; set; } = true;
        public bool AffectOwner { get; set; } = true;
    }

    public sealed class ClusterTuning : BlastTuning
    {
        public ClusterTuning()
        {
            Radius = 2.75f;
            HumanDamage = 40;
            ScpDamage = 100;
        }

        [Tune(.05, 120)]
        public float FuseSeconds { get; set; } = 3;

        [Tune(1, 32)]
        public int GrenadeCount { get; set; } = 4;

        [Tune(.05, 120)]
        public float ChildFuseSeconds { get; set; } = 1.5f;

        [Tune(0, 10)]
        public float ChildFuseStaggerSeconds { get; set; } = .08f;

        [Tune(0, 50)]
        public float HorizontalSpeed { get; set; } = 4.5f;

        [Tune(0, 50)]
        public float UpwardSpeed { get; set; } = 4;

        [Tune(0, 100000)]
        public float SharedHumanDamageCap { get; set; } = 85;

        [Tune(0, 100000)]
        public float SharedScpDamageCap { get; set; } = 225;
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

        [Tune(0, 2)]
        public float AudioVolume { get; set; } = .65f;
    }

    public sealed class BraceTuning
    {
        [Tune(0, 120)]
        public float DeploySeconds { get; set; } = 2;

        [Tune(0, 120)]
        public float RemoveSeconds { get; set; } = 3;

        [Tune(.1, 3600)]
        public float DurationSeconds { get; set; } = 8;

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
        public bool PairGasWithFilter { get; set; } = true;

        internal int Capacity(int players) => players < MediumLobbyPlayers ? SmallLobbyCapacity : players < LargeLobbyPlayers ? MediumLobbyCapacity : LargeLobbyCapacity;
        internal int Clusters(int players) => players < MediumLobbyPlayers ? SmallLobbyClusters : players < LargeLobbyPlayers ? MediumLobbyClusters : LargeLobbyClusters;
    }
}
