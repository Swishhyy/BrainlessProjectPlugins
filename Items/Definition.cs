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
        public float Weight { get; set; } = 1;
    }

    public sealed class Config : Exiled.API.Interfaces.IConfig
    {
        public bool IsEnabled { get; set; } = true;
        public bool Debug { get; set; }
        public Brainless.Internal.Updates.AutoUpdateConfig AutoUpdate { get; set; } = new Brainless.Internal.Updates.AutoUpdateConfig();
        public bool WorldLoot { get; set; } = true;
        public int ToolKeybindId { get; set; } = 7100;
        public ItemTuning Tuning { get; set; } = new ItemTuning();
        public ItemDistributionConfig Distribution { get; set; } = new ItemDistributionConfig();
        public int CatalogRevision { get; set; }
        public int GrenadeBalanceRevision { get; set; }
        public int AudioBalanceRevision { get; set; }
        public List<ItemSpec> Items { get; set; } = new List<ItemSpec>
        {
            new ItemSpec
            {
                Id = 7101,
                Name = "Irritant Gas Grenade",
                Description = "Eight seconds of native SCP smoke. Obscures vision and slows humans 20%; no gas damage. Filter blocks the slow.",
                BaseType = ItemType.GrenadeHE
            },
            new ItemSpec
            {
                Id = 7102,
                Name = "Cluster Grenade",
                Description = "Parent blast plus four real grenades launched outward. Shared damage cap; no recursive clusters.",
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
                Name = "Door Jammer",
                Description = "Equip this keycard and interact with a closed ordinary door to jam it. Others can remove it.",
                BaseType = ItemType.KeycardJanitor
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
            CreateImpact(),
        };

        private static ItemSpec CreateImpact() => new ItemSpec
        {
            Id = 7113,
            Name = "Impact Grenade",
            BaseType = ItemType.GrenadeHE,
            Description = "Detonates on surface contact after a short arming delay. Configurable blast and safety fuse."
        };
        internal void Prepare()
        {
            if (Items == null || Items.Any(i => i == null))
                throw new InvalidOperationException("Items cannot contain null entries.");
            Items.RemoveAll(item => item.Id == 7110);
            if (CatalogRevision < 1 && !Items.Any(i => i.Id == 7113))
                Items.Add(CreateImpact());
            if (CatalogRevision < 2)
                foreach (var item in Items.Where(i => i.Id == 7105))
                {
                    if (item.BaseType == ItemType.Coin)
                        item.BaseType = ItemType.KeycardJanitor;
                    if (item.Name == "Door Brace")
                        item.Name = "Door Jammer";
                    if (item.Description == "Hold tool key at an ordinary closed door. Others can remove it.")
                        item.Description = "Equip this keycard and interact with a closed ordinary door to jam it. Others can remove it.";
                    if (item.BaseType == ItemType.KeycardJanitor)
                        Distribution?.UpgradeDoorJammerBody();
                }

            CatalogRevision = 2;
            Brainless.Internal.Tuning.Validate(Tuning, "tuning");
            GrenadeBalanceRevision = Tuning.UpgradeGrenades(GrenadeBalanceRevision);
            AudioBalanceRevision = Tuning.UpgradeAudio(AudioBalanceRevision);
            if (Distribution == null)
                throw new InvalidOperationException("distribution cannot be null.");
            Distribution.Validate(id => id >= 7101 && id <= 7113 && id != 7110);
            if (Tuning.Supply.MediumLobbyPlayers >= Tuning.Supply.LargeLobbyPlayers)
                throw new InvalidOperationException("The large-lobby threshold must exceed the medium-lobby threshold.");
            if (Items.Any(i => i.Id < 7101 || i.Id > 7113 || i.Charges < 1 || i.Charges > 1000 || i.RoundCap < 0 || float.IsNaN(i.Weight) || float.IsInfinity(i.Weight) || i.Weight < 0))
                throw new InvalidOperationException("Item IDs must be 7101-7113 (7110 retired), charges 1-1000, and caps/weight nonnegative.");
            if (Items.Select(i => i.Id).Distinct().Count() != Items.Count || Items.Any(i => string.IsNullOrWhiteSpace(i.Name)) || Items.Select(i => i.Name).Distinct().Count() != Items.Count)
                throw new InvalidOperationException("Item IDs/names must be nonempty and unique.");
            foreach (var i in Items)
            {
                bool throwable = i.Id == 7101 || i.Id == 7102 || i.Id == 7103 || i.Id == 7104 || i.Id == 7113;
                if (throwable && i.BaseType != (i.Id == 7103 || i.Id == 7104 ? ItemType.GrenadeFlash : ItemType.GrenadeHE))
                    throw new InvalidOperationException("Throwable " + i.Id + " must keep its native grenade body.");
            }
        }
    }

    // No base event subscriptions: this plugin owns and cleans up its own handlers.
    public sealed class RegisteredItem : CustomItem, IAdminItem
    {
        private readonly Plugin owner;
        internal ItemSpec Spec { get; private set; }

        public RegisteredItem(Plugin owner, ItemSpec spec)
        {
            this.owner = owner;
            if (spec.Id == 7101 && spec.Description == "Eight-second cloud. Filter or leave it. SCPs are unaffected.")
                spec.Description = "Eight seconds of native SCP smoke. Obscures vision and slows humans 20%; no gas damage. Filter blocks the slow.";
            if (spec.Id == 7102 && spec.Description == "Three smaller blasts, shared damage cap; keep clear of the beeps.")
                spec.Description = "First detonation launches four real grenades outward. Short fuses and a shared damage cap; no recursive clusters.";
            if (spec.Id == 7102 && spec.Description == "First detonation launches four real grenades outward. Short fuses and a shared damage cap; no recursive clusters.")
                spec.Description = "Parent blast plus four real grenades launched outward. Shared damage cap; no recursive clusters.";
            Spec = spec;
            Id = spec.Id;
            Name = spec.Name;
            Description = spec.Description;
            Type = spec.BaseType;
            Weight = spec.Weight;
        }

        internal void Retune(ItemSpec spec)
        {
            Spec = spec;
            Name = spec.Name;
            Description = spec.Description;
            Weight = spec.Weight;
        }

        internal string AdoptItem(Player player, Item item)
        {
            if (item.Type != Type || !player.Items.Contains(item))
                return "The native inventory item changed.";
            bool paired = Id == 7101 && owner.Config.Tuning.Supply.PairGasWithFilter;
            if (paired && player.Items.Count >= 8)
                return "The gas filter needs one free inventory slot.";
            string failure = owner.IssueFailure(Spec, 1, false);
            if (failure != null)
                return failure;
            if (!owner.Mint(this, item.Serial))
                return "Natural supply was exhausted.";
            if (paired && (failure = owner.GiveFilter(player, false)) != null)
            {
                owner.Unmint(this, item.Serial, false);
                return failure;
            }

            TrackedSerials.Add(item.Serial);
            return null;
        }

        internal string AdoptPickup(Pickup pickup, Chamber chamber)
        {
            if (pickup.Base == null || pickup.Type != Type)
                return "The native map pickup changed.";
            if (Id == 7101 && owner.Config.Tuning.Supply.PairGasWithFilter && chamber == null && (!pickup.IsSpawned || pickup.IsLocked))
                return "A paired gas filter needs an accessible loose pickup or a known locker chamber.";
            string failure = owner.IssueFailure(Spec, 1, false);
            if (failure != null)
                return failure;
            if (!owner.Mint(this, pickup.Serial))
                return "Natural supply was exhausted.";
            if (Id == 7101 && owner.Config.Tuning.Supply.PairGasWithFilter && (failure = owner.SpawnMapFilter(pickup, chamber)) != null)
            {
                owner.Unmint(this, pickup.Serial, false);
                return failure;
            }

            TrackedSerials.Add(pickup.Serial);
            pickup.Weight = Weight;
            return null;
        }

        public override uint Id { get; set; }
        public override string Name { get; set; }
        public override string Description { get; set; }
        public override float Weight { get; set; } = 1;
        public override SpawnProperties SpawnProperties { get; set; } = new SpawnProperties();

        public bool CanIssue(int count) => owner.CanIssue(Spec, count);
        public bool CanIssueBatch(uint[] ids) => owner.CanIssueBatch(ids);
        public string GiveForRole(Player player, bool admin) => TryGive(player, admin, false);
        protected override void SubscribeEvents()
        {
        }

        protected override void UnsubscribeEvents()
        {
        }

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
            bool paired = Id == 7101 && owner.Config.Tuning.Supply.PairGasWithFilter;
            int slots = paired ? 2 : 1;
            if (player.Items.Count + slots > 8)
                return $"Inventory needs {slots} free slot(s).";
            string failure = owner.IssueFailure(Spec, 1, ignoreLimits);
            if (failure != null)
                return failure;
            Item item = Item.Create(Type);
            if (!owner.Mint(this, item.Serial, ignoreLimits))
                return "Supply changed before the grant could complete; retry.";
            base.Give(player, item, false);
            if (!player.Items.Any(i => i.Serial == item.Serial))
            {
                owner.Unmint(this, item.Serial, ignoreLimits);
                return "The server did not add the item to the inventory; check the server log.";
            }

            if (paired)
            {
                failure = owner.GiveFilter(player, ignoreLimits);
                if (failure != null)
                {
                    player.RemoveItem(item);
                    owner.Unmint(this, item.Serial, ignoreLimits);
                    return "Gas kit cancelled because its paired filter could not be given: " + failure;
                }
            }

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
        internal string TrySpawn(Vector3 position, bool ignoreLimits, out Pickup pickup, Player previousOwner = null)
        {
            pickup = null;
            string failure = owner.IssueFailure(Spec, 1, ignoreLimits);
            if (failure != null)
                return failure;
            Item item = Item.Create(Type);
            if (!owner.Mint(this, item.Serial, ignoreLimits))
                return "Supply changed before the spawn could complete; retry.";
            pickup = base.Spawn(position, item, previousOwner);
            if (pickup == null)
            {
                owner.Unmint(this, item.Serial, ignoreLimits);
                return "The server did not create the pickup; check the server log.";
            }

            if (Id == 7101 && owner.Config.Tuning.Supply.PairGasWithFilter)
            {
                failure = owner.SpawnFilter(position + Vector3.right * .3f, ignoreLimits);
                if (failure != null)
                {
                    pickup.Destroy();
                    pickup = null;
                    owner.Unmint(this, item.Serial, ignoreLimits);
                    return "Gas kit cancelled because its paired filter could not spawn: " + failure;
                }
            }

            return null;
        }
    }
}
