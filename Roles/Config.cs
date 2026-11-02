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

        [Description("Catalog migration marker. Revision 1 adds SCP variants; revision 2 adds Cowboy, Rogues and Containment Engineer; revision 3 replaces kits that used removed items, enables the Doctor and raises each role group to an 80% total once; revision 4 removes retired profiles, adds rank-specific MTF/Chaos roles and resets MTF/Chaos percentages to the new defaults; revision 5 adds Resilient and Big Boi.")]
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

        [Description("Lockpick channel time for a level-one door. Higher requirements add lockpick_extra_seconds_per_level.")]
        [Tune(.05, 3600)]
        public float LockpickDurationSeconds { get; set; } = LockpickRules.DefaultDuration;

        [Description("Cooldown after a successful level-one lockpick. Higher requirements add lockpick_extra_cooldown_per_level. Interruptions do not spend it.")]
        [Tune(0, 86400)]
        public float LockpickCooldownSeconds { get; set; } = LockpickRules.DefaultCooldown;

        [Tune(0, 3600)]
        public float LockpickExtraSecondsPerLevel { get; set; } = 15;

        [Tune(0, 86400)]
        public float LockpickExtraCooldownPerLevel { get; set; } = 60;

        [Tune(0, 100)]
        public float RogueSpawnSeparation { get; set; } = 12;
        public bool AnnounceRogue { get; set; } = true;

        [Description("Resilient (X01): when the player first drops to trigger_health_percent, they gain these boosts for the rest of that life.")]
        public ResilientTuning Resilient { get; set; } = new ResilientTuning();

        [Description("Big Boi (X02): extra healing from medical items.")]
        public BigBoiTuning BigBoi { get; set; } = new BigBoiTuning();

        [Description("Most living humans that may hold a custom role at once, as a percentage of living humans.")]
        [Tune(0, 100)]
        public float CustomHumanSharePercent { get; set; } = 80;

        [Description("Most custom MTF/Chaos roles per respawn wave. 0 means no limit (each profile still appears once per wave).")]
        [Tune(0, 100)]
        public int ReinforcementSpecialistsPerWave { get; set; }
        [Description("Lets one zombie per round become the Doctor (Z05): a weak SCP-049 that can revive. Needs the Z05 profile enabled and its minimum players.")]
        public bool EnableDoctorEvent { get; set; } = true;
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
            UpgradeHumanProfiles();
            UpgradeReworkedProfiles();
            UpgradeFactionProfiles();
            UpgradeAnyHumanProfiles();
            var descriptions = Catalog.Create().ToDictionary(r => r.Id, r => r.Hint);
            Catalog.FillMissingChances(Roles, LegacyFamilyChance);
            foreach (var role in Roles)
            {
                Tuning.Validate(role, "roles." + role.Id);
                role.Id = role.Id.ToUpperInvariant();
                if (role.Id == "D06" && (role.Hint == "Picks doors below level 3. Lockpicking takes time and has a cooldown." || role.Hint?.Contains("Level 3 excluded") == true))
                    role.Hint = "Picks any keycard door. Higher access takes longer.";
                if (!Enum.IsDefined(typeof(RoleSpawnArea), role.SpawnArea) || (role.Neutral && role.BaseRole != PlayerRoles.RoleTypeId.FacilityGuard && role.BaseRole != PlayerRoles.RoleTypeId.Scientist))
                    throw new InvalidOperationException("Invalid spawn area or neutral profile base role: " + role.Id);
                if (role.Id == "S04" && role.Hint == "Three approved candies; painkillers if the allowlist is empty.")
                    role.Hint = "A bag of real SCP-330 candy; candy count and colors are configurable.";
                if (role.Id == "G06" && role.Hint == "Door Brace instead of flashbang; less ammo.")
                    role.Hint = "Door Jammer keycard instead of flashbang; less ammo.";
                role.Hint = GameplayText.Clean(role.Hint, descriptions.TryGetValue(role.Id, out string shortText) ? shortText : "");
                role.CustomItems.RemoveAll(id => id == 7110 || Catalog.RetiredItems.Contains(id) || Catalog.RetiredItemsV4.Contains(id));
                if (role.MatchRoles == null)
                    role.MatchRoles = new List<PlayerRoles.RoleTypeId>();
            }

            foreach (var native in Enum.GetValues(typeof(PlayerRoles.RoleTypeId)).Cast<PlayerRoles.RoleTypeId>().Distinct())
                if (Roles.Where(r => r.Id != "Z05" && Catalog.MatchesNative(r, native)).Sum(r => (decimal)r.ChancePercent.Value) > 100m)
                    throw new InvalidOperationException("chance_percent totals for " + native + " exceed 100. Reduce that group's percentages; disabled profiles also reserve their share.");
        }

        // Revision 3: removed items leave every kit, the eight affected kits get their
        // replacement equipment, the Doctor becomes a usable weak SCP-049, the old
        // experimental gates on Child/Tickler/Muncher are lifted, and each chance group
        // is scaled up to an 80% total (relative shares are kept).
        private void UpgradeReworkedProfiles()
        {
            if (RoleCatalogRevision >= 3)
                return;
            var stock = Catalog.Create().ToDictionary(r => r.Id, StringComparer.OrdinalIgnoreCase);
            foreach (var role in Roles)
            {
                string id = role.Id.ToUpperInvariant();
                if (!stock.TryGetValue(id, out var fresh))
                    continue;
                bool usedRetired = role.CustomItems.Any(Catalog.RetiredItems.Contains) || (id == "C03" && role.Add.Count == 0);
                if (Catalog.ReworkedKits.Contains(id) && usedRetired)
                {
                    role.Remove = fresh.Remove.ToList();
                    role.Add = fresh.Add.ToList();
                    role.CustomItems = fresh.CustomItems.ToList();
                    role.Hint = fresh.Hint;
                }

                if (id == "D03" || id == "P03" || id == "Z02")
                    role.Experimental = false;
                if (id == "P02" && Math.Abs(role.HealthMultiplier - 1.2f) < .001f)
                    role.HealthMultiplier = fresh.HealthMultiplier;
                if (id == "L04" && role.SpeedMultiplier == 1)
                    role.SpeedMultiplier = fresh.SpeedMultiplier;
                if (id == "H03")
                {
                    role.Traits.UseIntercom = false;
                    if (role.Hint != null && role.Hint.Contains("intercom"))
                        role.Hint = fresh.Hint;
                }

                if (id == "Z05")
                {
                    role.Enabled = true;
                    role.Experimental = false;
                    role.Hint = fresh.Hint;
                    role.ShieldMultiplier = 0;
                    if (!role.ChancePercent.HasValue || role.ChancePercent == 1)
                        role.ChancePercent = Catalog.DoctorChance;
                    if (Math.Abs(role.HealthMultiplier - .4f) < .001f)
                        role.HealthMultiplier = Catalog.DoctorHealth;
                    if (role.SpeedMultiplier == 1)
                        role.SpeedMultiplier = fresh.SpeedMultiplier;
                    if (role.Traits.AttackCooldownMultiplier == 1)
                        role.Traits.AttackCooldownMultiplier = fresh.Traits.AttackCooldownMultiplier;
                    if (role.Traits.RecallDurationMultiplier == 1)
                        role.Traits.RecallDurationMultiplier = fresh.Traits.RecallDurationMultiplier;
                }
            }

            EnableDoctorEvent = true;
            Catalog.FillMissingChances(Roles, LegacyFamilyChance);
            // Only the classic family pools are raised; rank-only and any-human profiles
            // (match_roles) keep their own catalog percentages.
            foreach (var group in Roles.Where(r => !r.Id.Equals("Z05", StringComparison.OrdinalIgnoreCase) && (r.MatchRoles == null || r.MatchRoles.Count == 0)).GroupBy(Catalog.ChanceGroup))
            {
                // Invalid values are left for validation to reject with a clear message.
                if (group.Any(r => double.IsNaN(r.ChancePercent.Value) || double.IsInfinity(r.ChancePercent.Value) || r.ChancePercent.Value < 0 || r.ChancePercent.Value > 100))
                    continue;
                decimal total = group.Sum(r => (decimal)r.ChancePercent.Value);
                decimal target = (decimal)Catalog.GroupChancePercent;
                if (total <= 0 || total >= target)
                    continue;
                foreach (var role in group)
                    role.ChancePercent = (double)(Math.Floor((decimal)role.ChancePercent.Value * target / total * 100) / 100);
            }

            RoleCatalogRevision = 3;
        }

        // Revision 4: drop retired profiles, rebuild kits that used newly removed items,
        // append the rank-specific MTF/Chaos roles and the Captured Insurgent, and reset
        // every stock MTF/Chaos percentage to the new defaults (the pool layout changed).
        private void UpgradeFactionProfiles()
        {
            if (RoleCatalogRevision >= 4)
                return;
            var stock = Catalog.Create();
            var byId = stock.ToDictionary(r => r.Id, StringComparer.OrdinalIgnoreCase);
            Roles.RemoveAll(r => Catalog.RetiredRoles.Contains(r.Id));
            foreach (var role in Roles)
            {
                if (!byId.TryGetValue(role.Id, out var fresh))
                    continue;
                string id = role.Id.ToUpperInvariant();
                if (Catalog.ReworkedKitsV4.Contains(id) && (role.CustomItems.Any(Catalog.RetiredItemsV4.Contains) || (id == "M03" && role.Remove.Contains(ItemTypeE11))))
                {
                    role.Remove = fresh.Remove.ToList();
                    role.Add = fresh.Add.ToList();
                    role.CustomItems = fresh.CustomItems.ToList();
                    role.Hint = fresh.Hint;
                }

                if (id == "B02" && Math.Abs(role.HealthMultiplier - .9f) < .001f)
                    role.HealthMultiplier = Catalog.EscapeHealth;
                if (id == "S07" && role.Hint == "Starts in HCZ's Micro room with a containment engineer keycard.")
                    role.Hint = fresh.Hint;
                // A profile deliberately set to 0% stays disabled.
                if ((id.StartsWith("M") || id.StartsWith("C")) && fresh.ChancePercent.HasValue && role.ChancePercent != 0)
                    role.ChancePercent = fresh.ChancePercent;
            }

            Catalog.FillMissingChances(Roles, LegacyFamilyChance);
            foreach (var profile in stock.Where(r => !Roles.Any(existing => existing.Id.Equals(r.Id, StringComparison.OrdinalIgnoreCase))))
            {
                if (!(profile.Id.StartsWith("M") || profile.Id.StartsWith("C") || profile.Id == "D08"))
                    continue;
                // Never overfill a pool the owner already allocated.
                decimal used = Enum.GetValues(typeof(PlayerRoles.RoleTypeId)).Cast<PlayerRoles.RoleTypeId>().Where(t => Catalog.MatchesNative(profile, t)).Select(t => Roles.Where(r => !r.Id.Equals("Z05", StringComparison.OrdinalIgnoreCase) && Catalog.MatchesNative(r, t)).Sum(r => (decimal)r.ChancePercent.Value)).DefaultIfEmpty(0).Max();
                profile.ChancePercent = (double)Math.Max(0, Math.Min((decimal)profile.ChancePercent.Value, 100m - used));
                Roles.Add(profile);
            }

            RoleCatalogRevision = 4;
        }

        private static readonly ItemType ItemTypeE11 = ItemType.GunE11SR;

        // Revision 5: append the "any human" profiles without overfilling any pool.
        private void UpgradeAnyHumanProfiles()
        {
            if (RoleCatalogRevision >= 5)
                return;
            Catalog.FillMissingChances(Roles, LegacyFamilyChance);
            foreach (var profile in Catalog.Create().Where(r => (r.Id == "X01" || r.Id == "X02") && !Roles.Any(existing => existing.Id.Equals(r.Id, StringComparison.OrdinalIgnoreCase))))
            {
                decimal used = Enum.GetValues(typeof(PlayerRoles.RoleTypeId)).Cast<PlayerRoles.RoleTypeId>().Where(t => Catalog.MatchesNative(profile, t)).Select(t => Roles.Where(r => !r.Id.Equals("Z05", StringComparison.OrdinalIgnoreCase) && Catalog.MatchesNative(r, t)).Sum(r => (decimal)r.ChancePercent.Value)).DefaultIfEmpty(0).Max();
                profile.ChancePercent = (double)Math.Max(0, Math.Min((decimal)profile.ChancePercent.Value, 100m - used));
                Roles.Add(profile);
            }

            RoleCatalogRevision = 5;
        }

        private void UpgradeHumanProfiles()
        {
            if (RoleCatalogRevision >= 2)
                return;
            Catalog.FillMissingChances(Roles, LegacyFamilyChance);
            foreach (var profile in Catalog.Create().Where(r => r.Id == "C05" || r.Id == "G07" || r.Id == "S07" || r.Id == "S08"))
            {
                if (Roles.Any(r => r.Id.Equals(profile.Id, StringComparison.OrdinalIgnoreCase)))
                    continue;
                decimal used = Roles.Where(r => Catalog.MatchesNative(r, profile.BaseRole)).Sum(r => (decimal)r.ChancePercent.Value);
                profile.ChancePercent = (double)Math.Min((decimal)profile.ChancePercent.Value, Math.Max(0, 100m - used));
                Roles.Add(profile);
            }

            var chief = Roles.FirstOrDefault(r => r.Id.Equals("S03", StringComparison.OrdinalIgnoreCase));
            if (chief != null)
            {
                if (chief.Name == "Chief Scientist")
                    chief.Name = "Head Scientist";
                if (chief.Hint == "Has better research access, but no medical supplies.")
                    chief.Hint = "Carries a head scientist keycard with improved research access.";
            }

            RoleCatalogRevision = 2;
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

    public sealed class ResilientTuning
    {
        [Tune(1, 99)]
        public float TriggerHealthPercent { get; set; } = 25;

        [Tune(1, 3)]
        public float SpeedMultiplier { get; set; } = 1.25f;

        [Tune(1, 5)]
        public float DamageMultiplier { get; set; } = 1.3f;

        [Tune(0, 500)]
        public float ArtificialHealth { get; set; } = 50;
    }

    public sealed class BigBoiTuning
    {
        [Tune(0, 500)]
        public float MedkitBonusHealth { get; set; } = 35;

        [Tune(0, 500)]
        public float PainkillersBonusHealth { get; set; } = 20;

        [Tune(0, 500)]
        public float Scp500BonusArtificialHealth { get; set; } = 30;

        [Tune(0, 500)]
        public float AdrenalineBonusArtificialHealth { get; set; } = 20;
    }
}
