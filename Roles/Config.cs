using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Brainless.Internal;
using Exiled.API.Interfaces;
using InventorySystem.Items.Usables.Scp330;

namespace Brainless.Roles
{
    public enum HudSide
    {
        Left,
        Right
    }

    public sealed class Config : IConfig
    {
        public bool IsEnabled { get; set; } = true;
        public bool Debug { get; set; }
        public Brainless.Internal.Updates.AutoUpdateConfig AutoUpdate { get; set; } = new Brainless.Internal.Updates.AutoUpdateConfig();

        [Description("Show the current custom or native role at the side of the screen. Temporarily yields to other hints.")]
        public bool ShowRoleHud { get; set; } = true;
        public bool RoleHudShowDescription { get; set; } = true;

        [Description("Screen side for the role HUD: Left or Right.")]
        public HudSide RoleHudSide { get; set; } = HudSide.Left;

        [Description("Inset from the chosen edge, as a percentage of the hint area.")]
        public float RoleHudEdgePaddingPercent { get; set; } = .5f;

        [Description("Width of the role HUD column, as a percentage of the hint area.")]
        public float RoleHudWidthPercent { get; set; } = 30;

        [Description("Vertical offset in text em units; negative values move the role display down.")]
        public float RoleHudVerticalOffset { get; set; }

        [Description("Layout migration marker. Set to 2 to keep a deliberately chosen old edge inset.")]
        public int RoleHudLayoutRevision { get; set; }

        internal void UpgradeHudLayout()
        {
            if (RoleHudLayoutRevision >= 2)
                return;
            // Existing YAML retains the former bottom-center offset.
            if (RoleHudLayoutRevision < 1 && RoleHudVerticalOffset == -11)
                RoleHudVerticalOffset = 0;
            if (RoleHudEdgePaddingPercent == 3)
                RoleHudEdgePaddingPercent = .5f;
            RoleHudLayoutRevision = 2;
        }

        public bool AllowExperimentalRoles { get; set; }
        public bool AllowModelScaling { get; set; } = true;

        [Description("Catalog migration marker. Revision 1 adds the SCP-106/173/939/096 profiles once and restores their editable traits.")]
        public int RoleCatalogRevision { get; set; }
        public bool GiveClassDCoins { get; set; } = true;

        [Description("Z04 bursts on confirmed deaths. Zero limits mean unlimited; separate death/LOS/damage controls are available.")]
        public ExplosiveZombieTuning ExplosiveZombie { get; set; } = new ExplosiveZombieTuning();
        public ExplosiveZombieTuning ExplosiveScp173 { get; set; } = new ExplosiveZombieTuning
        {
            WarningSeconds = 1.5f,
            Radius = 4,
            HumanDamage = 100
        };

        [Description("Seconds a Locksmith must stay at the door control to complete a lockpick.")]
        [Tune(.05, 3600)]
        public float LockpickDurationSeconds { get; set; } = LockpickRules.DefaultDuration;

        [Description("Cooldown after a successful lockpick. Interrupted attempts do not spend it.")]
        [Tune(0, 86400)]
        public float LockpickCooldownSeconds { get; set; } = LockpickRules.DefaultCooldown;
        public bool EnableDoctorEvent { get; set; }
        public bool ExtremeMuncherEventPreset { get; set; }

        [Description("Legacy migration only for profiles with missing/null chance_percent. Explicit per-role percentages override this field.")]
        [Tune(0, 100)]
        public float ClassDChance { get; set; } = 45;

        [Description("Legacy migration only for profiles with missing/null chance_percent. Explicit per-role percentages override this field.")]
        [Tune(0, 100)]
        public float ScientistChance { get; set; } = 45;

        [Description("Legacy migration only for profiles with missing/null chance_percent. Explicit per-role percentages override this field.")]
        [Tune(0, 100)]
        public float GuardChance { get; set; } = 50;

        [Description("Legacy migration only for profiles with missing/null chance_percent. Explicit per-role percentages override this field.")]
        [Tune(0, 100)]
        public float ReinforcementChance { get; set; } = 30;

        [Description("Legacy migration only for profiles with missing/null chance_percent. Explicit per-role percentages override this field.")]
        [Tune(0, 100)]
        public float DoctorVariantChance { get; set; } = 50;

        [Description("Legacy migration only for profiles with missing/null chance_percent. Explicit per-role percentages override this field.")]
        [Tune(0, 100)]
        public float ZombieChance { get; set; } = 45;

        [Tune(1, 6)]
        public int CandyManCandyCount { get; set; } = 3;

        [Description("Candy colors for candy roles. Empty uses registered Rainbow/Yellow/Purple/Red/Green/Blue candies; no painkiller fallback. Seasonal/explosive colors require explicit entries.")]
        public List<CandyKindID> ApprovedCandy { get; set; } = new List<CandyKindID>();
        public List<RoleDefinition> Roles { get; set; } = Catalog.Create();

        internal void Prepare()
        {
            UpgradeHudLayout();
            Tuning.Validate(this, "brainless_roles");
            if (ApprovedCandy == null || Roles == null || Roles.Any(r => r == null || string.IsNullOrWhiteSpace(r.Id) || string.IsNullOrWhiteSpace(r.Name) || r.CustomItems == null || r.Add == null || r.Remove == null) || Roles.Select(r => r.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() != Roles.Count)
                throw new InvalidOperationException("Role profiles need unique IDs, nonempty names and non-null equipment/candy lists.");
            UpgradeCatalog();
            var descriptions = Catalog.Create().ToDictionary(r => r.Id, r => r.Hint);
            Catalog.FillMissingChances(Roles, LegacyFamilyChance);
            foreach (var role in Roles)
            {
                Tuning.Validate(role, "roles." + role.Id);
                role.Id = role.Id.ToUpperInvariant();
                if (role.Id == "S04" && role.Hint == "Three approved candies; painkillers if the allowlist is empty.")
                    role.Hint = "A bag of real SCP-330 candy; candy count and colors are configurable.";
                if (role.Id == "G06" && role.Hint == "Door Brace instead of flashbang; less ammo.")
                    role.Hint = "Door Jammer keycard instead of flashbang; less ammo.";
                role.Hint = GameplayText.Clean(role.Hint, descriptions.TryGetValue(role.Id, out string shortText) ? shortText : "");
                role.CustomItems.RemoveAll(id => id == 7110);
            }

            foreach (var native in Enum.GetValues(typeof(PlayerRoles.RoleTypeId)).Cast<PlayerRoles.RoleTypeId>().Distinct())
                if (Roles.Where(r => r.Id != "Z05" && Catalog.MatchesNative(r, native)).Sum(r => (decimal)r.ChancePercent.Value) > 100m)
                    throw new InvalidOperationException("chance_percent totals for " + native + " exceed 100. Reduce that group's percentages; disabled profiles also reserve their share.");
        }

        private void UpgradeCatalog()
        {
            if (RoleCatalogRevision >= 1)
                return;
            var defaults = Catalog.Create();
            var newIds = new HashSet<string>
            {
                "L01",
                "L02",
                "L03",
                "L04",
                "B01",
                "B02",
                "B03",
                "B04",
                "H01",
                "H02",
                "H03",
                "H04",
                "R01",
                "R02"
            };
            var existingGroups = Roles.Where(r => !newIds.Contains(r.Id.ToUpperInvariant())).Select(r => r.BaseRole).ToHashSet();
            foreach (var role in defaults.Where(r => newIds.Contains(r.Id)))
                if (!Roles.Any(r => r.Id.Equals(role.Id, StringComparison.OrdinalIgnoreCase)))
                {
                    // Keep custom native-SCP budgets valid. Owners can allocate
                    // percentages to these new entries themselves in that case.
                    if (existingGroups.Contains(role.BaseRole))
                        role.ChancePercent = 0;
                    Roles.Add(role);
                }

            foreach (var role in Roles)
            {
                var stock = defaults.FirstOrDefault(r => r.Id.Equals(role.Id, StringComparison.OrdinalIgnoreCase));
                if (stock == null)
                    continue;
                if (role.Traits == null)
                    throw new InvalidOperationException("roles." + role.Id + ".traits cannot be null.");
                if (role.Id.Equals("P01", StringComparison.OrdinalIgnoreCase))
                {
                    if (role.Traits.RecallDurationMultiplier == 1)
                        role.Traits.RecallDurationMultiplier = .75f;
                    if (role.Traits.ShieldRegenDelayMultiplier == 1)
                        role.Traits.ShieldRegenDelayMultiplier = .85f;
                }

                if (role.Id.Equals("P03", StringComparison.OrdinalIgnoreCase) && role.Traits.AttackCooldownMultiplier == 1)
                    role.Traits.AttackCooldownMultiplier = .9f;
                if (role.Id.Equals("Z02", StringComparison.OrdinalIgnoreCase) && role.Traits.AttackDamageMultiplier == 1)
                    role.Traits.AttackDamageMultiplier = .5f;
                if ((role.Id.Equals("P02", StringComparison.OrdinalIgnoreCase) || role.Id.Equals("Z01", StringComparison.OrdinalIgnoreCase) || role.Id.Equals("Z02", StringComparison.OrdinalIgnoreCase)) && role.Scale == 1)
                    role.Scale = stock.Scale;
            }

            RoleCatalogRevision = 1;
        }

        private double LegacyFamilyChance(RoleDefinition role) => role.BaseRole == PlayerRoles.RoleTypeId.ClassD ? ClassDChance : role.BaseRole == PlayerRoles.RoleTypeId.Scientist ? ScientistChance : role.BaseRole == PlayerRoles.RoleTypeId.FacilityGuard ? GuardChance : role.BaseRole == PlayerRoles.RoleTypeId.Scp049 ? DoctorVariantChance : role.BaseRole == PlayerRoles.RoleTypeId.Scp0492 ? ZombieChance : ReinforcementChance;
    }
}
