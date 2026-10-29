using System;
using System.Collections.Generic;
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
        public bool AllowExperimentalWeapons { get; set; }
        public bool WorldLoot { get; set; }
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
        };
    }

    public sealed class RegisteredWeapon : CustomItem
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
        public override void Give(Player player, bool displayMessage = true)
        {
            if (!CanIssue(1) || player.Items.Count >= 8)
                return;
            Firearm firearm = Item.Create<Firearm>(Type);
            if (!owner.Mint(this, firearm))
                return;
            base.Give(player, firearm, false);
            player.AddAmmo(firearm.AmmoType, (ushort)Math.Max(0, Spec.Reserve));
            if (displayMessage)
                player.ShowHint($"<b>{Name}</b>\n{Description}", 6);
        }

        public override Pickup Spawn(Vector3 position, Player previousOwner = null)
        {
            if (!CanIssue(1))
                return null;
            Firearm firearm = Item.Create<Firearm>(Type);
            if (!owner.Mint(this, firearm))
                return null;
            // World pickups have their loaded rounds only; reserve ammo is never recreated on pickup.
            return base.Spawn(position, firearm, previousOwner);
        }
    }
}
