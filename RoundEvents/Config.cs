using System;
using Brainless.Internal;
using Brainless.Internal.Updates;
using Exiled.API.Interfaces;
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
        public float AutomaticEventChancePercent { get; set; } = 10;

        [Tune(0, 15)]
        public float RoundStartDelaySeconds { get; set; } = 1.5f;
        public InfectionTuning Infection { get; set; } = new InfectionTuning();

        internal void Validate()
        {
            if (AutoUpdate == null)
                throw new InvalidOperationException("auto_update cannot be null.");
            Tuning.Validate(this, "round_events");
            if (Infection.MinimumPlayers <= Infection.MinimumSurvivors)
                throw new InvalidOperationException("minimum_players must exceed minimum_survivors.");
            if (!(Infection.SurvivorRole == RoleTypeId.FacilityGuard || Infection.SurvivorRole == RoleTypeId.NtfPrivate || Infection.SurvivorRole == RoleTypeId.NtfSergeant || Infection.SurvivorRole == RoleTypeId.NtfSpecialist || Infection.SurvivorRole == RoleTypeId.NtfCaptain))
                throw new InvalidOperationException("survivor_role must be a native Guard or NTF role.");
        }
    }

    public sealed class InfectionTuning
    {
        public bool Enabled { get; set; } = true;

        [Tune(2, 1000)]
        public int MinimumPlayers { get; set; } = 4;

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
        public bool AllowSlapInfections { get; set; } = true;
        public bool AllowStrangulationInfections { get; set; } = true;
        public bool AllowDisguises { get; set; }
        public bool PreventHumanFriendlyFire { get; set; } = true;
        public bool BlockReinforcements { get; set; } = true;
        public bool AllowLateJoinSurvivors { get; set; }
        public bool RemoveConvertedRagdoll { get; set; } = true;
    }
}
