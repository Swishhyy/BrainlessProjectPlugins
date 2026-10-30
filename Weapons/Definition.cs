using System;
using System.Collections.Generic;
using System.Linq;
using Brainless.Internal;
using Exiled.API.Features;
using Exiled.API.Features.Items;
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
    }

    public sealed class Config : Exiled.API.Interfaces.IConfig
    {
        public bool IsEnabled { get; set; } = true;
        public bool Debug { get; set; }
        public Brainless.Internal.Updates.AutoUpdateConfig AutoUpdate { get; set; } = new Brainless.Internal.Updates.AutoUpdateConfig();
        public bool AllowExperimentalWeapons { get; set; }
        public bool WorldLoot { get; set; }
        public float LauncherShotInterval { get; set; } = .65f;
        public float LauncherSpeed { get; set; } = 18;
        public float LauncherFuse { get; set; } = 1.8f;
        public float LauncherBlastRadius { get; set; } = 2.5f;
        public float LauncherHumanDamage { get; set; } = 45;
        public float LauncherScpDamage { get; set; } = 90;
        public List<WeaponSpec> Weapons { get; set; } = new List<WeaponSpec>
        {
            new WeaponSpec
            {
                Id = 7201,
                Name = "Nullifier Rifle",
                Description = "120% Hume damage, 70% SCP health damage, 85% human damage.",
                BaseType = ItemType.GunE11SR,
                HumanDamage = .85f,
                ScpDamage = .7f
            },
            new WeaponSpec
            {
                Id = 7202,
                Name = "Rattler",
                Description = "Three hits in two seconds create a public three-second audible tag. 80% damage.",
                BaseType = ItemType.GunCrossvec,
                HumanDamage = .8f,
                ScpDamage = .8f
            },
            new WeaponSpec
            {
                Id = 7203,
                Name = "Breach Shotgun",
                Description = "Three firing cycles and six shells to breach an ordinary door. 90% human damage.",
                BaseType = ItemType.GunShotgun,
                Magazine = 0,
                Reserve = 18,
                HumanDamage = .9f
            },
            new WeaponSpec
            {
                Id = 7204,
                Name = "Redline SMG",
                Description = "Heat prototype: short bursts; 12 shots overheat for three seconds. Native cadence pending client validation.",
                BaseType = ItemType.GunFSP9,
                HumanDamage = .85f,
                ScpDamage = .85f,
                Experimental = true,
                RoundCap = 1
            },
            new WeaponSpec
            {
                Id = 7205,
                Name = "Last Word",
                Description = "Six shots total. Cannot reload, unload or expand its magazine.",
                BaseType = ItemType.GunCOM15,
                Magazine = 6,
                Reserve = 0,
                RoundCap = 1
            },
            new WeaponSpec
            {
                Id = 7206,
                Name = "Ricochet Pistol",
                Description = "One bounce from approved static mesh geometry. 85% direct damage; 50% body damage after bouncing.",
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
            Description = "Logicer body; 16 grenades total, no reload. Small 2.5m blast: up to 45 human / 90 SCP damage. Tap fire every 0.65s.",
            BaseType = ItemType.GunLogicer,
            Magazine = 16,
            Reserve = 0,
            RoundCap = 1
        };
        // EXILED preserves existing YAML lists rather than appending new defaults.
        // Add this newly introduced weapon once; later deletions/edits are respected.
        public int CatalogRevision { get; set; }

        internal void UpgradeCatalog()
        {
            if (CatalogRevision >= 1)
                return;
            if (!Weapons.Any(w => w.Id == 7207))
                Weapons.Add(CreateLauncher());
            CatalogRevision = 1;
        }
    }

    public sealed class RegisteredWeapon : CustomItem, IAdminItem
    {
        internal WeaponSpec Spec { get; }

        private readonly Plugin owner;
        public RegisteredWeapon(Plugin owner, WeaponSpec spec)
        {
            this.owner = owner;
            Spec = spec;
            Id = spec.Id;
            Name = spec.Name;
            Description = spec.Description;
            Type = spec.BaseType;
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

            if (Id != 7207 && Id != 7205)
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
