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

        [Description("Server Specific Settings IDs base..base+2 for the HUD fade keybind and show/hide switch. Change if another plugin uses them.")]
        public int HudSettingsIdBase { get; set; } = 58100;

        [Description("Suggested key for fading the role / SCP chat HUD in and out. Players can rebind it in Settings > Server-specific.")]
        public UnityEngine.KeyCode HudToggleKey { get; set; } = UnityEngine.KeyCode.H;

        [Description("Seconds the role HUD takes to fade in or out.")]
        [Tune(0, 3)]
        public float HudFadeSeconds { get; set; } = .6f;

        [Description("Spectators see the custom role of the player they are watching.")]
        public bool ShowSpectatedRole { get; set; } = true;

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

        [Description("Catalog migration marker. Revision 1 adds SCP variants; revision 2 adds Cowboy, Rogues and Containment Engineer; revision 3 replaces kits that used removed items, enables the Doctor and raises each role group to an 80% total once; revision 4 removes retired profiles, adds rank-specific MTF/Chaos roles and resets MTF/Chaos percentages to the new defaults; revision 5 adds Resilient and Big Boi; revision 6 and 7 apply playtest balance to untouched stock values; revision 7 also removes Rogue Guard and Rogue Scientist; revision 8 raises Hyper's vigor gain; revision 9 resets any Poisonous 106 slowdown to normal speed; revision 10 gives Boom Boom a bigger, harder death blast; revision 11 assigns rarity tiers.")]
        public int RoleCatalogRevision { get; set; }
        public bool GiveClassDCoins { get; set; } = true;

        [Description("Rarity mode: a spawn first rolls a rarity tier (role_rarity), then a role of that tier for the class. If the class has nothing available in that tier (or the tier hit its round limit), the next tier down is tried. chance_percent then only weights roles inside a tier. False uses the old per-role chance_percent roll.")]
        public bool UseRarity { get; set; } = true;

        [Description("Chance per spawn for each tier, and how many roles of each tier can appear per round (Mythical: 1-2).")]
        public RarityTable RoleRarity { get; set; } = new RarityTable();

        [Description(".scpreroll: some players who roll a custom role are offered one reroll per round, capped at max_rarity.")]
        public RerollTuning Reroll { get; set; } = new RerollTuning();

        [Description("Z04 bursts on confirmed deaths. Zero limits mean unlimited; separate death/LOS/damage controls are available.")]
        public ExplosiveZombieTuning ExplosiveZombie { get; set; } = new ExplosiveZombieTuning();
        [Description("B04 Boom Boom's death blast.")]
        public ExplosiveZombieTuning ExplosiveScp173 { get; set; } = new ExplosiveZombieTuning
        {
            WarningSeconds = 1.5f,
            Radius = 7,
            HumanDamage = 175,
            WarningRange = 14
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

        [Description("When staff use the Remote Admin forceclass command, roll a custom role for the new class with the normal per-role chances. Plugin role changes (events, bprole set) are unaffected.")]
        public bool ForceClassCustomRoles { get; set; } = true;

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
            UpgradeRoleBalance();
            UpgradePlaytestBalance();
            UpgradeHyperVigor();
            UpgradePoisonousSpeed();
            UpgradeBoomBoom();
            UpgradeRarity();
            if (RoleRarity == null || Reroll == null)
                throw new InvalidOperationException("role_rarity and reroll cannot be null.");
            RoleRarity.Validate("role_rarity");
            if (!Enum.IsDefined(typeof(Rarity), Reroll.MaxRarity) || Roles.Any(r => !Enum.IsDefined(typeof(Rarity), r.Rarity)))
                throw new InvalidOperationException("Unknown rarity tier. Use Common, Uncommon, Rare, Epic, Legendary or Mythical.");
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

            // In rarity mode chance_percent is only a weight inside a tier, so totals may pass 100.
            foreach (var native in Enum.GetValues(typeof(PlayerRoles.RoleTypeId)).Cast<PlayerRoles.RoleTypeId>().Distinct())
                if (!UseRarity && Roles.Where(r => r.Id != "Z05" && Catalog.MatchesNative(r, native)).Sum(r => (decimal)r.ChancePercent.Value) > 100m)
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
        // Revision 6: playtest balance. Untouched old values move to the new defaults:
        // Trained Agent +2% speed, Poisonous 106 5% slower with 75% vigor gain, Hyper 106
        // 35% vigor gain, Tickler +12% speed, Anger Issue 1,400 HP / 120 shield, Big Boi
        // 1.15 width. General always drops the frag grenade so its kit fits, and
        // Resilient/Big Boi get their chance back if the revision 5 cap left them at 0.
        private void UpgradeRoleBalance()
        {
            if (RoleCatalogRevision >= 6)
                return;
            var fresh = Catalog.Create().ToDictionary(r => r.Id, StringComparer.OrdinalIgnoreCase);
            bool Near(float a, float b) => Math.Abs(a - b) < .001f;
            foreach (var role in Roles)
            {
                switch (role.Id.ToUpperInvariant())
                {
                    case "G03":
                        if (Near(role.SpeedMultiplier, 1.05f))
                            role.SpeedMultiplier = 1.02f;
                        break;
                    case "L04":
                        if (Near(role.SpeedMultiplier, .85f))
                            role.SpeedMultiplier = .95f;
                        if (Near(role.Traits.VigorGainMultiplier, 1))
                            role.Traits.VigorGainMultiplier = .75f;
                        if (role.Hint == "Touches poison humans, but it walks more slowly. Medical supplies cure the poison.")
                            role.Hint = fresh["L04"].Hint;
                        break;
                    case "L03":
                        if (Near(role.Traits.VigorGainMultiplier, .5f))
                            role.Traits.VigorGainMultiplier = .35f;
                        break;
                    case "P03":
                        if (Near(role.SpeedMultiplier, 1.05f))
                            role.SpeedMultiplier = 1.12f;
                        break;
                    case "R01":
                        if (role.Health == 0 && Near(role.HealthMultiplier, .7f))
                        {
                            role.Health = 1400;
                            role.HealthMultiplier = 1;
                        }

                        if (role.Shield == 0 && Near(role.ShieldMultiplier, .8f))
                        {
                            role.Shield = 120;
                            role.ShieldMultiplier = 1;
                        }

                        if (role.Hint == "Enrages immediately, but has less health and shields.")
                            role.Hint = fresh["R01"].Hint;
                        break;
                    case "M11":
                        if (role.CustomItems.Count > 0 && !role.Remove.Contains(ItemType.GrenadeHE))
                            role.Remove.Add(ItemType.GrenadeHE);
                        if (role.Hint == "A Captain with 110 health, an Irritant Gas Grenade and a Cluster Grenade.")
                            role.Hint = fresh["M11"].Hint;
                        break;
                    case "X02":
                        if (Near(role.WidthScale, 1.3f))
                            role.WidthScale = 1.15f;
                        break;
                }
            }

            // A roster the owner switched off entirely (every chance 0) stays off.
            bool IsAnyHuman(RoleDefinition r) => r.Id.Equals("X01", StringComparison.OrdinalIgnoreCase) || r.Id.Equals("X02", StringComparison.OrdinalIgnoreCase);
            bool rosterActive = Roles.Any(r => !IsAnyHuman(r) && !r.Id.Equals("Z05", StringComparison.OrdinalIgnoreCase) && r.ChancePercent > 0);
            foreach (var role in Roles.Where(r => rosterActive && IsAnyHuman(r) && r.ChancePercent == 0))
            {
                decimal used = Enum.GetValues(typeof(PlayerRoles.RoleTypeId)).Cast<PlayerRoles.RoleTypeId>().Where(t => Catalog.MatchesNative(role, t)).Select(t => Roles.Where(r => r != role && !r.Id.Equals("Z05", StringComparison.OrdinalIgnoreCase) && Catalog.MatchesNative(r, t)).Sum(r => (decimal)(r.ChancePercent ?? 0))).DefaultIfEmpty(0).Max();
                role.ChancePercent = (double)Math.Max(0, Math.Min((decimal)fresh[role.Id].ChancePercent.Value, 100m - used));
            }

            RoleCatalogRevision = 6;
        }

        // Revision 7: playtest balance. Only untouched old stock values change:
        // Escape's 173 55% health, Hyper 106 +8% speed, Poisonous 106 normal speed with a
        // 200 hume shield, Anger Issue native shield plus out-of-combat health regen, and
        // Swordsman 200 HP, +20% speed and an adrenaline shot.
        private void UpgradePlaytestBalance()
        {
            if (RoleCatalogRevision >= 7)
                return;
            RetireRogues();
            var fresh = Catalog.Create().ToDictionary(r => r.Id, StringComparer.OrdinalIgnoreCase);
            bool Near(float a, float b) => Math.Abs(a - b) < .001f;
            void Hint(RoleDefinition role, string old)
            {
                if (role.Hint == old)
                    role.Hint = fresh[role.Id].Hint;
            }

            foreach (var role in Roles)
            {
                switch (role.Id.ToUpperInvariant())
                {
                    case "G03":
                        Hint(role, "A faster guard with combat armor. Heavy armor removes the speed bonus.");
                        break;
                    case "B02":
                        if (Near(role.HealthMultiplier, .71f))
                            role.HealthMultiplier = Catalog.EscapeHealth;
                        Hint(role, "Kills refresh breakneck speed and its cooldown.");
                        break;
                    case "L03":
                        if (Near(role.SpeedMultiplier, 1.18f))
                            role.SpeedMultiplier = 1.08f;
                        Hint(role, "Moves faster, but gains vigor more slowly.");
                        break;
                    case "L04":
                        if (Near(role.SpeedMultiplier, .95f))
                            role.SpeedMultiplier = 1;
                        if (role.Shield == 0 && Near(role.ShieldMultiplier, 1))
                            role.Shield = 200;
                        Hint(role, "Touches poison humans, but it walks a little slower and gains vigor more slowly. Medical supplies cure the poison.");
                        break;
                    case "R01":
                        if (Near(role.Shield, 120))
                            role.Shield = 0;
                        if (role.Traits.HealthRegenPerSecond == 0)
                        {
                            role.Traits.HealthRegenPerSecond = 20;
                            role.Traits.HealthRegenDelaySeconds = 8;
                            role.Traits.HealthRegenMax = 900;
                        }

                        Hint(role, "Enrages immediately, with 1,400 health and a 120 hume shield.");
                        break;
                    case "M10":
                        if (Near(role.Health, 150))
                            role.Health = 200;
                        if (Near(role.SpeedMultiplier, 1.1f))
                            role.SpeedMultiplier = 1.2f;
                        if (role.Add.Contains(ItemType.SCP1509) && !role.Add.Contains(ItemType.Adrenaline))
                            role.Add.Add(ItemType.Adrenaline);
                        Hint(role, "Fights with SCP-1509, with 150 health and a little more speed.");
                        break;
                }
            }

            RoleCatalogRevision = 7;
        }

        // Rogue Guard / Rogue Scientist are removed. Their share goes to the other active
        // profiles of the same pool (proportionally, rounded down) so custom roles stay
        // as common as before; a pool the owner switched off stays off.
        private void RetireRogues()
        {
            foreach (var rogue in Roles.Where(r => Catalog.RetiredRolesV7.Contains(r.Id)).ToArray())
            {
                Roles.Remove(rogue);
                decimal share = (decimal)(rogue.ChancePercent ?? 0);
                var pool = Roles.Where(r => !r.Id.Equals("Z05", StringComparison.OrdinalIgnoreCase) && r.Enabled && r.ChancePercent > 0 && Catalog.ChanceGroup(r) == Catalog.ChanceGroup(rogue)).ToArray();
                decimal total = pool.Sum(r => (decimal)r.ChancePercent.Value);
                if (share <= 0 || total <= 0)
                    continue;
                foreach (var role in pool)
                    role.ChancePercent = (double)(Math.Floor((decimal)role.ChancePercent.Value * (total + share) / total * 100) / 100);
            }
        }

        // Revision 8: Hyper 106 gains vigor a little faster (35% -> 45% of normal).
        private void UpgradeHyperVigor()
        {
            if (RoleCatalogRevision >= 8)
                return;
            foreach (var role in Roles.Where(r => r.Id.Equals("L03", StringComparison.OrdinalIgnoreCase) && Math.Abs(r.Traits.VigorGainMultiplier - .35f) < .001f))
                role.Traits.VigorGainMultiplier = .45f;
            RoleCatalogRevision = 8;
        }

        // Revision 9: Poisonous 106 always moves at normal speed (owner request). Any saved
        // slowdown is removed, whatever value it had; owners can still raise it above 1.
        private void UpgradePoisonousSpeed()
        {
            if (RoleCatalogRevision >= 9)
                return;
            foreach (var role in Roles.Where(r => r.Id.Equals("L04", StringComparison.OrdinalIgnoreCase) && r.SpeedMultiplier < 1))
                role.SpeedMultiplier = 1;
            RoleCatalogRevision = 9;
        }

        // Revision 10: Boom Boom's death blast 4 m / 100 -> 7 m / 175 (untouched stock values).
        private void UpgradeBoomBoom()
        {
            if (RoleCatalogRevision >= 10)
                return;
            var blast = ExplosiveScp173;
            if (blast != null && Math.Abs(blast.Radius - 4) < .001f && Math.Abs(blast.HumanDamage - 100) < .001f)
            {
                blast.Radius = 7;
                blast.HumanDamage = 175;
                if (Math.Abs(blast.WarningRange - 10) < .001f)
                    blast.WarningRange = 14;
            }

            RoleCatalogRevision = 10;
        }

        // Revision 11: rarity tiers for the shipped roles (new property, so every saved
        // role starts at Common until this runs once).
        private void UpgradeRarity()
        {
            if (RoleCatalogRevision >= 11)
                return;
            foreach (var role in Roles)
                role.Rarity = Catalog.DefaultRarity(role.Id);
            RoleCatalogRevision = 11;
        }

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

    public sealed class RerollTuning
    {
        public bool Enabled { get; set; } = true;

        [Description("Chance that a player who rolls a custom role is offered a reroll.")]
        [Tune(0, 100)]
        public double OfferChancePercent { get; set; } = 35;

        [Description("Seconds after spawning that the offer lasts.")]
        [Tune(5, 300)]
        public float WindowSeconds { get; set; } = 30;

        [Description("Highest rarity a reroll can give.")]
        public Rarity MaxRarity { get; set; } = Rarity.Rare;
    }
}
