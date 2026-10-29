using System;
using System.Collections.Generic;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.API.Features.Pickups;
using Exiled.API.Features.Spawn;
using Exiled.CustomItems.API.Features;
using UnityEngine;

namespace Brainless.Items
{
    public sealed class ItemSpec
    {
        public uint Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public ItemType BaseType { get; set; }
        public bool Enabled { get; set; } = true;
        public int RoundCap { get; set; } = 2;
        public int Charges { get; set; } = 1;
    }

    public sealed class Config : Exiled.API.Interfaces.IConfig
    {
        public bool IsEnabled { get; set; } = true;
        public bool Debug { get; set; }
        public bool WorldLoot { get; set; } = true;
        public int ToolKeybindId { get; set; } = 7100;
        public List<ItemSpec> Items { get; set; } = new List<ItemSpec>
        {
            new ItemSpec
            {
                Id = 7101,
                Name = "Irritant Gas Grenade",
                Description = "Eight-second cloud. Filter or leave it. SCPs are unaffected.",
                BaseType = ItemType.GrenadeHE
            },
            new ItemSpec
            {
                Id = 7102,
                Name = "Cluster Grenade",
                Description = "Three smaller blasts, shared damage cap; keep clear of the beeps.",
                BaseType = ItemType.GrenadeHE
            },
            new ItemSpec
            {
                Id = 7103,
                Name = "Containment Foam Canister",
                Description = "Six-second floor patch slows humans 10% and SCPs 8%.",
                BaseType = ItemType.GrenadeFlash
            },
            new ItemSpec
            {
                Id = 7104,
                Name = "Echo Canister",
                Description = "Six seconds of synthetic footsteps; no damage or flash.",
                BaseType = ItemType.GrenadeFlash
            },
            new ItemSpec
            {
                Id = 7105,
                Name = "Door Brace",
                Description = "Hold tool key at an ordinary closed door. Others can remove it.",
                BaseType = ItemType.Coin
            },
            new ItemSpec
            {
                Id = 7106,
                Name = "Signal Jammer",
                Description = "Hold tool key to deploy a short, noisy SCP-079 room jammer.",
                BaseType = ItemType.Radio
            },
            new ItemSpec
            {
                Id = 7107,
                Name = "Survey Beacon",
                Description = "Deploy with tool key; two audible scans in this room.",
                BaseType = ItemType.Flashlight
            },
            new ItemSpec
            {
                Id = 7108,
                Name = "Trace Chalk",
                Description = "Three numbered floor markers, visible to everyone for 45 seconds.",
                BaseType = ItemType.Coin,
                Charges = 3
            },
            new ItemSpec
            {
                Id = 7109,
                Name = "Field Dressing",
                Description = "Four seconds to heal 35 HP. Sprinting or damage interrupts.",
                BaseType = ItemType.Medkit,
                RoundCap = 4
            },
            new ItemSpec
            {
                Id = 7110,
                Name = "Maintenance Pass",
                Description = "Four-second, one-use LCZ access. No checkpoints, gates, or armories.",
                BaseType = ItemType.KeycardCustomSite02
            },
            new ItemSpec
            {
                Id = 7111,
                Name = "Filter Inhaler",
                Description = "Two seconds to gain 12 seconds of custom gas protection.",
                BaseType = ItemType.Painkillers,
                RoundCap = 4
            },
            new ItemSpec
            {
                Id = 7112,
                Name = "Preserved Specimen",
                Description = "Not SCP-500. Offer it to a zombie; hold tool key to eat it for 80 HP.",
                BaseType = ItemType.SCP500
            },
        };
    }

    // No base event subscriptions: this plugin owns and cleans up its own handlers.
    public sealed class RegisteredItem : CustomItem
    {
        private readonly Plugin owner;
        internal ItemSpec Spec { get; }

        public RegisteredItem(Plugin owner, ItemSpec spec)
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
        public override float Weight { get; set; } = 1;
        public override SpawnProperties SpawnProperties { get; set; } = new SpawnProperties();

        public bool CanIssue(int count) => owner.CanIssue(Spec, count);
        public bool CanIssueBatch(uint[] ids) => owner.CanIssueBatch(ids);
        protected override void SubscribeEvents()
        {
        }

        protected override void UnsubscribeEvents()
        {
        }

        public override void Give(Player player, bool displayMessage = true)
        {
            if (!CanIssue(1) || player.Items.Count >= (Id == 7101 ? 7 : 8))
                return;
            Item item = Item.Create(Type);
            if (!owner.Mint(this, item.Serial))
                return;
            base.Give(player, item, false);
            owner.Setup(item, this);
            if (Id == 7101)
                owner.GiveFilter(player);
            if (displayMessage)
                player.ShowHint($"<b>{Name}</b>\n{Description}", 6);
        }

        public override Pickup Spawn(Vector3 position, Player previousOwner = null)
        {
            if (!CanIssue(1))
                return null;
            Item item = Item.Create(Type);
            if (!owner.Mint(this, item.Serial))
                return null;
            owner.Setup(item, this);
            Pickup pickup = base.Spawn(position, item, previousOwner);
            if (Id == 7101)
                owner.SpawnFilter(position + Vector3.right * .3f);
            return pickup;
        }
    }
}
