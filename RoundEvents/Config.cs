using System;
using System.Collections.Generic;
using System.ComponentModel;
using Exiled.API.Enums;
using Brainless.Internal;
using Brainless.Internal.Updates;
using Exiled.API.Interfaces;
using System.Linq;
using PlayerRoles;

namespace Brainless.RoundEvents
{
    public sealed class Config : IConfig
    {
        public bool IsEnabled { get; set; } = true;
        public bool Debug { get; set; }
        public AutoUpdateConfig AutoUpdate { get; set; } = new AutoUpdateConfig();
        public bool AutomaticEvents { get; set; }

        [Tune(0, 100)]
        public float AutomaticEventChancePercent { get; set; } = 3;

        [Description("Migration marker. Revision 1 lowers the stock 10% automatic event chance to 3%.")]
        public int EventBalanceRevision { get; set; }

        [Tune(0, 15)]
        public float RoundStartDelaySeconds { get; set; } = 1.5f;
        public InfectionTuning Infection { get; set; } = new InfectionTuning();

        [Description("Chaos holds the facility, MTF raids it. Teams respawn in waves; first to the kill target wins.")]
        public DeathmatchTuning Deathmatch { get; set; } = new DeathmatchTuning();

        [Description("One heavily armored Chaos Juggernaut against NTF hunters. Killing the Juggernaut makes you the next one.")]
        public JuggernautTuning Juggernaut { get; set; } = new JuggernautTuning();

        private static readonly RoleTypeId[] ChaosRoles = { RoleTypeId.ChaosConscript, RoleTypeId.ChaosRifleman, RoleTypeId.ChaosMarauder, RoleTypeId.ChaosRepressor };
        private static readonly RoleTypeId[] MtfRoles = { RoleTypeId.NtfPrivate, RoleTypeId.NtfSergeant, RoleTypeId.NtfSpecialist, RoleTypeId.NtfCaptain, RoleTypeId.FacilityGuard };

        internal void Validate()
        {
            if (EventBalanceRevision < 1)
            {
                if (AutomaticEventChancePercent == 10)
                    AutomaticEventChancePercent = 3;
                EventBalanceRevision = 1;
            }

            if (EventBalanceRevision < 2)
            {
                // The Juggernaut tracker now updates live; untouched 30-second reveals speed up.
                if (Juggernaut != null && Juggernaut.RevealIntervalSeconds == 30)
                    Juggernaut.RevealIntervalSeconds = 2;
                EventBalanceRevision = 2;
            }

            if (AutoUpdate == null)
                throw new InvalidOperationException("auto_update cannot be null.");
            Tuning.Validate(this, "round_events");
            if (Infection.MinimumPlayers <= Infection.MinimumSurvivors)
                throw new InvalidOperationException("minimum_players must exceed minimum_survivors.");
            if (Infection.SpawnZones == null)
                throw new InvalidOperationException("infection.spawn_zones cannot be null (use [] for Heavy Containment and Entrance).");
            if (Deathmatch == null || Juggernaut == null || Deathmatch.ChaosRoles == null || Deathmatch.MtfRoles == null || Deathmatch.ChaosZones == null || Juggernaut.HunterRoles == null || Juggernaut.JuggernautZones == null)
                throw new InvalidOperationException("Deathmatch and Juggernaut settings and their role/zone lists cannot be null.");
            if (Deathmatch.ChaosRoles.Any(r => !ChaosRoles.Contains(r)) || Deathmatch.MtfRoles.Any(r => !MtfRoles.Contains(r)) || Juggernaut.HunterRoles.Any(r => !MtfRoles.Contains(r)) || !ChaosRoles.Contains(Juggernaut.JuggernautRole))
                throw new InvalidOperationException("Deathmatch/Juggernaut Chaos roles must be Chaos and MTF/hunter roles must be Foundation forces.");
            if (!(Infection.SurvivorRole == RoleTypeId.FacilityGuard || Infection.SurvivorRole == RoleTypeId.NtfPrivate || Infection.SurvivorRole == RoleTypeId.NtfSergeant || Infection.SurvivorRole == RoleTypeId.NtfSpecialist || Infection.SurvivorRole == RoleTypeId.NtfCaptain))
                throw new InvalidOperationException("survivor_role must be a native Guard or NTF role.");
        }
    }

    public sealed class InfectionTuning
    {
        public bool Enabled { get; set; } = true;

        [Tune(2, 1000)]
        public int MinimumPlayers { get; set; } = 4;

        [Description("Players needed before an automatic roll can pick this event. Staff commands use minimum_players.")]
        [Tune(2, 1000)]
        public int AutomaticMinimumPlayers { get; set; } = 12;

        [Tune(1, 1000)]
        public int MinimumSurvivors { get; set; } = 2;

        [Tune(1, 1000)]
        public int StartingInfectedCount { get; set; } = 3;

        [Tune(0, 100)]
        public float StartingInfectedPercent { get; set; } = 20;

        [Tune(1, 1000)]
        public int MaximumStartingInfected { get; set; } = 8;

        [Tune(1, 10000)]
        public float InfectedHealth { get; set; } = 150;

        [Tune(0, 10000)]
        public float InfectedHumeShield { get; set; }
        public RoleTypeId SurvivorRole { get; set; } = RoleTypeId.FacilityGuard;

        [Tune(1, 5000)]
        public float SurvivorHealth { get; set; } = 100;

        [Tune(.05, 30)]
        public float RespawnDelaySeconds { get; set; } = 1;

        [Tune(0, 1)]
        public float SpawnVerticalOffset { get; set; } = .05f;

        [Tune(0, 30)]
        public float EndingGraceSeconds { get; set; } = 3;

        [Description("Move everyone to separate rooms when Infection starts, with skeletons kept away from survivors. Off keeps players where they stand.")]
        public bool SpreadPlayers { get; set; } = true;

        [Description("Zones used for spread-out starts. Light Containment is not recommended: decontamination still runs during Infection.")]
        public List<ZoneType> SpawnZones { get; set; } = new List<ZoneType> { ZoneType.HeavyContainment, ZoneType.Entrance };

        [Description("Minimum distance in meters between a survivor's start room and any skeleton's start room (relaxed automatically on small maps).")]
        [Tune(0, 500)]
        public float MinimumSpawnDistance { get; set; } = 35;

        [Description("Seconds of spawn protection for everyone at the start, so nobody is killed before they can move.")]
        [Tune(0, 60)]
        public float StartProtectionSeconds { get; set; } = 5;
        public bool AllowSlapInfections { get; set; } = true;
        public bool AllowStrangulationInfections { get; set; } = true;
        public bool AllowDisguises { get; set; }
        public bool PreventHumanFriendlyFire { get; set; } = true;
        public bool BlockReinforcements { get; set; } = true;
        public bool AllowLateJoinSurvivors { get; set; }
        public bool RemoveConvertedRagdoll { get; set; } = true;
    }

    public sealed class DeathmatchTuning
    {
        public bool Enabled { get; set; } = true;

        [Tune(2, 1000)]
        public int MinimumPlayers { get; set; } = 4;

        [Description("Players needed before an automatic roll can pick this event. Staff commands use minimum_players.")]
        [Tune(2, 1000)]
        public int AutomaticMinimumPlayers { get; set; } = 16;

        [Tune(1, 10000)]
        public int KillTarget { get; set; } = 40;

        [Tune(60, 7200)]
        public float TimeLimitSeconds { get; set; } = 900;

        [Tune(1, 300)]
        public float RespawnWaveSeconds { get; set; } = 15;

        [Tune(0, 60)]
        public float SpawnProtectionSeconds { get; set; } = 5;
        public List<RoleTypeId> ChaosRoles { get; set; } = new List<RoleTypeId> { RoleTypeId.ChaosRifleman, RoleTypeId.ChaosRifleman, RoleTypeId.ChaosMarauder, RoleTypeId.ChaosRepressor };
        public List<RoleTypeId> MtfRoles { get; set; } = new List<RoleTypeId> { RoleTypeId.NtfSergeant, RoleTypeId.NtfSergeant, RoleTypeId.NtfSpecialist, RoleTypeId.NtfPrivate };

        [Description("Zones where Chaos spawns inside the facility. Light Containment is excluded by default.")]
        public List<ZoneType> ChaosZones { get; set; } = new List<ZoneType> { ZoneType.HeavyContainment, ZoneType.Entrance };

        [Description("Lock the Light Containment checkpoints and elevators for the whole event.")]
        public bool LockLightContainment { get; set; } = true;
    }

    public sealed class JuggernautTuning
    {
        public bool Enabled { get; set; } = true;

        [Tune(2, 1000)]
        public int MinimumPlayers { get; set; } = 4;

        [Description("Players needed before an automatic roll can pick this event. Staff commands use minimum_players.")]
        [Tune(2, 1000)]
        public int AutomaticMinimumPlayers { get; set; } = 10;

        [Tune(1, 10000)]
        public int ScoreTarget { get; set; } = 20;

        [Tune(60, 7200)]
        public float TimeLimitSeconds { get; set; } = 600;

        [Tune(1, 300)]
        public float RespawnWaveSeconds { get; set; } = 12;

        [Tune(0, 60)]
        public float SpawnProtectionSeconds { get; set; } = 5;
        public RoleTypeId JuggernautRole { get; set; } = RoleTypeId.ChaosRepressor;
        public List<RoleTypeId> HunterRoles { get; set; } = new List<RoleTypeId> { RoleTypeId.NtfSergeant, RoleTypeId.NtfSpecialist };
        public List<ZoneType> JuggernautZones { get; set; } = new List<ZoneType> { ZoneType.HeavyContainment, ZoneType.Entrance };

        [Tune(1, 100000)]
        public float BaseHealth { get; set; } = 1000;

        [Tune(0, 10000)]
        public float HealthPerHunter { get; set; } = 150;

        [Tune(0, 10000)]
        public float KillHeal { get; set; } = 75;

        [Tune(0, 1000)]
        public int PointsPerKill { get; set; } = 1;

        [Tune(0, 1000)]
        public int PointsForJuggernautKill { get; set; } = 3;

        [Description("How often the hunters' Juggernaut tracker (zone, room, distance, direction, HP) refreshes.")]
        [Tune(1, 600)]
        public float RevealIntervalSeconds { get; set; } = 2;

        [Description("Give everyone SCP-1344 goggle vision (see players through walls within about 22 m, 44 m on the surface) for the whole event.")]
        public bool GogglesForEveryone { get; set; } = true;

        [Description("Lock the Light Containment checkpoints and elevators for the whole event.")]
        public bool LockLightContainment { get; set; } = true;
    }
}
