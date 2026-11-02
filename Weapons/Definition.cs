using System;
using System.Collections.Generic;
using System.Linq;
using Brainless.Internal;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.API.Features.Lockers;
using Exiled.API.Features.Pickups;
using Exiled.API.Features.Spawn;
using Exiled.CustomItems.API.Features;
using UnityEngine;

namespace Brainless.Weapons
{
    public sealed class WeaponSpec
    {
        public uint Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public ItemType BaseType { get; set; }
        public bool Enabled { get; set; } = true;
        public bool Experimental { get; set; }
        public int Magazine { get; set; } = 20;
        public int Reserve { get; set; } = 60;
        public float HumanDamage { get; set; } = 1;
        public float ScpDamage { get; set; } = 1;
        public int RoundCap { get; set; } = 2;
        public float Weight { get; set; } = 2;
    }

    public sealed class Config : Exiled.API.Interfaces.IConfig
    {
        public Config()
        {
            Distribution.EnsureEntries(Weapons.Select(w => new KeyValuePair<uint, ItemType>(w.Id, w.BaseType)));
            Distribution.PrepareChances();
        }

        public bool IsEnabled { get; set; } = true;
        public bool Debug { get; set; }
        public Brainless.Internal.Updates.AutoUpdateConfig AutoUpdate { get; set; } = new Brainless.Internal.Updates.AutoUpdateConfig();
        public bool AllowExperimentalWeapons { get; set; }
        public bool IndividualItemFiles { get; set; }
        public bool LauncherDiscardWhenEmpty { get; set; } = true;
        public bool WorldLoot { get; set; }
        public WeaponTuning Tuning { get; set; } = new WeaponTuning();
        public WeaponDistributionConfig Distribution { get; set; } = new WeaponDistributionConfig();

        [Tune(0, 100)]
        public float LauncherSpeed { get; set; } = 18;

        [Tune(.05, 120)]
        public float LauncherFuse { get; set; } = 1.8f;

        [Tune(.1, 60)]
        public float LauncherBlastRadius { get; set; } = 3.5f;

        [Tune(0, 100000)]
        public float LauncherHumanDamage { get; set; } = 70;

        [Tune(0, 100000)]
        public float LauncherScpDamage { get; set; } = 150;

        [Tune(1, 100)]
        public int LauncherArmorPenetration { get; set; } = ExplosionRules.DefaultPenetration;
        public int GrenadeBalanceRevision { get; set; }
        public int AudioBalanceRevision { get; set; }
        public bool LauncherRequireLineOfSight { get; set; } = true;
        public bool LauncherAffectOwner { get; set; } = true;
        public bool LauncherDamageEnvironment { get; set; } = true;
        public List<WeaponSpec> Weapons { get; set; } = new List<WeaponSpec>
        {
            new WeaponSpec
            {
                Id = 7206,
                Name = "Ricochet Pistol",
                Description = "Shots can bounce once off solid surfaces.",
                BaseType = ItemType.GunCOM18,
                Magazine = 10,
                Reserve = 20,
                HumanDamage = .85f,
                ScpDamage = .85f,
                Experimental = true,
                RoundCap = 1
            },
            CreateLauncher(),
        };

        private static WeaponSpec CreateLauncher() => new WeaponSpec
        {
            Id = 7207,
            Name = "Grenade Launcher",
            Description = "Fires a grenade with each shot. Holds up to 16; cannot reload.",
            BaseType = ItemType.GunLogicer,
            Magazine = 16,
            Reserve = 0,
            RoundCap = 1
        };
        // EXILED preserves existing YAML lists rather than appending new defaults.
        // Add this newly introduced weapon once; later deletions/edits are respected.
        public int CatalogRevision { get; set; }

        // Removed: Rattler, Redline SMG and Last Word (revision 3); Nullifier Rifle, Breach
        // Shotgun and Railgun (revision 4).
        internal static readonly HashSet<uint> Retired = new HashSet<uint> { 7201, 7202, 7203, 7204, 7205, 7208 };

        internal void UpgradeCatalog()
        {
            Weapons.RemoveAll(w => Retired.Contains(w.Id));
            Distribution?.RemoveRetired(Retired);
            if (CatalogRevision < 1 && !Weapons.Any(w => w.Id == 7207))
                Weapons.Add(CreateLauncher());
            if (CatalogRevision < 2)
            {
                Distribution?.UpgradeSupWeapons();
            }

            CatalogRevision = Math.Max(4, CatalogRevision);
        }

        internal void Prepare()
        {
            if (Weapons == null || Weapons.Any(w => w == null))
                throw new InvalidOperationException("Weapons cannot contain null entries.");
            UpgradeCatalog();
            LauncherBlastRadius = ExplosionRules.UpgradeStock(LauncherBlastRadius, 2.5f, 3.5f, GrenadeBalanceRevision);
            LauncherHumanDamage = ExplosionRules.UpgradeStock(LauncherHumanDamage, 45, 70, GrenadeBalanceRevision);
            LauncherScpDamage = ExplosionRules.UpgradeStock(LauncherScpDamage, 90, 150, GrenadeBalanceRevision);
            GrenadeBalanceRevision = Math.Max(1, GrenadeBalanceRevision);
            Brainless.Internal.Tuning.Validate(this, "brainless_weapons");
            AudioBalanceRevision = Tuning.UpgradeAudio(AudioBalanceRevision);
            if (Distribution == null)
                throw new InvalidOperationException("distribution cannot be null.");
            Distribution.EnsureEntries(Weapons.Select(w => new KeyValuePair<uint, ItemType>(w.Id, w.BaseType)));
            Distribution.Validate(id => id >= 7201 && id <= 7208 && !Retired.Contains(id));
            if (Weapons.Select(w => w.Id).Distinct().Count() != Weapons.Count || Weapons.Any(w => string.IsNullOrWhiteSpace(w.Name)) || Weapons.Select(w => w.Name).Distinct().Count() != Weapons.Count)
                throw new InvalidOperationException("Weapon IDs and names must be unique and nonempty.");
            foreach (var w in Weapons)
                if (w.Id == 7207)
                {
                    w.Magazine = LauncherRules.Capacity(w.Magazine);
                    w.Reserve = 0;
                    if (w.Description == "Logicer body; 16 grenades total, no reload. Small 3.5m blast: up to 70 human / 150 SCP damage. Tap fire every 0.65s.")
                        w.Description = "Logicer body; each bullet launches one grenade. Maximum 16 rounds, no reload. Small configurable blast.";
                }

            foreach (var w in Weapons)
                if (w.Id < 7201 || w.Id > 7208 || w.Magazine < 0 || w.Magazine > 255 || (w.Id == 7207 && w.Magazine < 1) || w.Reserve < 0 || w.Reserve > ushort.MaxValue || w.RoundCap < 0 || float.IsNaN(w.HumanDamage) || float.IsInfinity(w.HumanDamage) || w.HumanDamage < 0 || float.IsNaN(w.ScpDamage) || float.IsInfinity(w.ScpDamage) || w.ScpDamage < 0 || float.IsNaN(w.Weight) || float.IsInfinity(w.Weight) || w.Weight < 0)
                    throw new InvalidOperationException("Invalid weapon settings: " + w.Id + ". Magazine must fit 0-255 (finite weapons 1-255), reserve 0-65535; damage, weight and cap must be finite/nonnegative.");
            foreach (var w in Weapons)
                if (w.Id == 7207 && w.BaseType != ItemType.GunLogicer)
                    throw new InvalidOperationException("The launcher must use the Logicer body.");
            var descriptions = new Config().Weapons.ToDictionary(i => i.Id, i => i.Description);
            foreach (var entry in Weapons)
                entry.Description = GameplayText.Clean(entry.Description, descriptions.TryGetValue(entry.Id, out string shortText) ? shortText : "");
        }
    }

    public sealed class RegisteredWeapon : CustomItem, IAdminItem
    {
        internal WeaponSpec Spec { get; private set; }

        private readonly Plugin owner;
        public RegisteredWeapon(Plugin owner, WeaponSpec spec)
        {
            this.owner = owner;
            if (spec.Id == 7207 && spec.Description == "Logicer body; 16 grenades total, no reload. Small 2.5m blast: up to 45 human / 90 SCP damage. Tap fire every 0.65s.")
                spec.Description = "Logicer body; each bullet launches one grenade. Maximum 16 rounds, no reload. Small configurable blast.";
            Spec = spec;
            Id = spec.Id;
            Name = spec.Name;
            Description = spec.Description;
            Type = spec.BaseType;
            Weight = spec.Weight;
        }

        internal void Retune(WeaponSpec spec)
        {
            Spec = spec;
            Name = spec.Name;
            Description = spec.Description;
            Weight = spec.Weight;
        }

        // Stable optional crafting API used by Brainless.SlFixes. A successful
        // conversion keeps the native serial/body and spends normal supply.
        public string TryCraftInventory(Player player, Item item) => AdoptItem(player, item);
        public string TryCraftPickup(Pickup pickup, Vector3 output)
        {
            string failure = AdoptPickup(pickup, null);
            if (failure == null)
                pickup.Position = output;
            return failure;
        }

        internal string AdoptItem(Player player, Item item)
        {
            if (!(item is Firearm firearm) || item.Type != Type || !player.Items.Contains(item))
                return "The native firearm changed.";
            string failure = owner.IssueFailure(Spec, 1, false);
            if (failure != null)
                return failure;
            if (!owner.Mint(this, firearm))
                return "Natural firearm supply was exhausted.";
            TrackedSerials.Add(item.Serial);
            // Replacement keeps the native loadout's reserve; it grants no new reserve ammo.
            return null;
        }

        internal string AdoptPickup(Pickup pickup, Chamber chamber)
        {
            if (!(pickup is FirearmPickup firearm) || pickup.Base == null || pickup.Type != Type)
                return "The native map firearm changed.";
            if (Spec.Magazine > firearm.MaxAmmo)
                return "The configured magazine exceeds this native pickup's capacity; use an additional item grant instead.";
            string failure = owner.IssueFailure(Spec, 1, false);
            if (failure != null)
                return failure;
            if (!owner.MintSerial(this, pickup.Serial))
                return "Natural firearm supply was exhausted.";
            if (Spec.Magazine > 0)
                firearm.Ammo = Spec.Magazine;
            TrackedSerials.Add(pickup.Serial);
            pickup.Weight = Weight;
            return null;
        }

        public override uint Id { get; set; }
        public override string Name { get; set; }
        public override string Description { get; set; }
        public override float Weight { get; set; } = 2;
        public override SpawnProperties SpawnProperties { get; set; } = new SpawnProperties();

        protected override void SubscribeEvents()
        {
        }

        protected override void UnsubscribeEvents()
        {
        }

        public bool CanIssue(int count) => owner.CanIssue(Spec, count);
        public bool CanIssueBatch(uint[] ids) => owner.CanIssueBatch(ids);
        public string GiveForRole(Player player, bool admin) => TryGive(player, admin, false);
        public override void Give(Player player, bool displayMessage = true)
        {
            string failure = TryGive(player, false, displayMessage);
            if (failure != null && displayMessage)
                player?.ShowHint("Cannot give " + Name + ": " + failure, 6);
        }

        string IAdminItem.TryGive(Player player, bool ignoreLimits, bool displayMessage) => TryGive(player, ignoreLimits, displayMessage);
        internal string TryGive(Player player, bool ignoreLimits, bool displayMessage)
        {
            if (player == null || !player.IsConnected)
                return "Player is disconnected.";
            if (!player.IsAlive)
                return "Player must be alive.";
            if (player.IsCuffed)
                return "Player is restrained.";
            if (player.Items.Count >= 8)
                return "Inventory is full.";
            string failure = owner.IssueFailure(Spec, 1, ignoreLimits);
            if (failure != null)
                return failure;
            Firearm firearm = Item.Create<Firearm>(Type);
            if (!owner.Mint(this, firearm, ignoreLimits))
                return "Supply changed before the grant could complete; retry.";
            base.Give(player, firearm, false);
            if (!player.Items.Any(i => i.Serial == firearm.Serial))
            {
                owner.Unmint(this, firearm.Serial, ignoreLimits);
                return "The server did not add the weapon to the inventory; check the server log.";
            }

            if (Id != 7207)
                player.AddAmmo(firearm.AmmoType, (ushort)Math.Max(0, Spec.Reserve));
            if (displayMessage)
                player.ShowHint($"<b>{Name}</b>\n{Description}", 6);
            return null;
        }

        public override Pickup Spawn(Vector3 position, Player previousOwner = null)
        {
            TrySpawn(position, false, out var pickup, previousOwner);
            return pickup;
        }

        string IAdminItem.TrySpawn(Vector3 position, bool ignoreLimits) => TrySpawn(position, ignoreLimits, out _);
        private string TrySpawn(Vector3 position, bool ignoreLimits, out Pickup pickup, Player previousOwner = null)
        {
            pickup = null;
            string failure = owner.IssueFailure(Spec, 1, ignoreLimits);
            if (failure != null)
                return failure;
            Firearm firearm = Item.Create<Firearm>(Type);
            if (!owner.Mint(this, firearm, ignoreLimits))
                return "Supply changed before the spawn could complete; retry.";
            // World pickups have their loaded rounds only; reserve ammo is never recreated on pickup.
            pickup = base.Spawn(position, firearm, previousOwner);
            if (pickup == null)
                owner.Unmint(this, firearm.Serial, ignoreLimits);
            return pickup == null ? "The server did not create the pickup; check the server log." : null;
        }
    }
}
