using System.Collections.Generic;
using System.ComponentModel;
using Exiled.API.Interfaces;
using InventorySystem.Items.Usables.Scp330;

namespace Brainless.Roles
{
    public sealed class Config : IConfig
    {
        public bool IsEnabled { get; set; } = true;
        public bool Debug { get; set; }

        [Description("Show the current custom or native role near the bottom of the screen. Temporarily yields to other hints.")]
        public bool ShowRoleHud { get; set; } = true;
        public bool RoleHudShowDescription { get; set; } = true;

        [Description("Vertical offset in text em units; negative values move the role display down.")]
        public float RoleHudVerticalOffset { get; set; } = -11;
        public bool AllowExperimentalRoles { get; set; }
        public bool GiveClassDCoins { get; set; } = true;
        public bool CaptainGateAccess { get; set; }
        public bool EnableDoctorEvent { get; set; }
        public bool ExtremeMuncherEventPreset { get; set; }
        public float ClassDChance { get; set; } = 45;
        public float ScientistChance { get; set; } = 45;
        public float GuardChance { get; set; } = 50;
        public float ReinforcementChance { get; set; } = 30;
        public float DoctorVariantChance { get; set; } = 50;
        public float ZombieChance { get; set; } = 45;

        [Description("Empty by default because seasonal candy colors can change effects. Approve effects for your installed build first.")]
        public List<CandyKindID> ApprovedCandy { get; set; } = new List<CandyKindID>();
        public List<RoleDefinition> Roles { get; set; } = Catalog.Create();
    }
}
