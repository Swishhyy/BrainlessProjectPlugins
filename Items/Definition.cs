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
        public Config()
        {
            Distribution.EnsureEntries(Items.Select(i => new KeyValuePair<uint, ItemType>(i.Id, i.BaseType)));
            Distribution.PrepareChances();
        }

        public bool IsEnabled { get; set; } = true;
        public bool Debug { get; set; }
        public Brainless.Internal.Updates.AutoUpdateConfig AutoUpdate { get; set; } = new Brainless.Internal.Updates.AutoUpdateConfig();
        public bool WorldLoot { get; set; } = true;
        public int ToolKeybindId { get; set; } = 7100;
        public SupTuning Sup { get; set; } = new SupTuning();
        public bool IndividualItemFiles { get; set; }
        public ItemTuning Tuning { get; set; } = new ItemTuning();
        public ItemDistributionConfig Distribution { get; set; } = new ItemDistributionConfig();
        public int CatalogRevision { get; set; }

        // Removed items: Survey Beacon, Trace Chalk, Field Dressing, Preserved Specimen and
        // SCRAMBLE Goggles (revision 7); Echo Canister, Signal Jammer, Filter Inhaler and
        // Bio Grenade (revision 8).
        internal static readonly HashSet<uint> Retired = new HashSet<uint> { 7104, 7106, 7107, 7108, 7109, 7111, 7112, 7118, 7121 };
        internal const string FoamDescription = "Covers the floor around it in SCP-173's sticky tantrum mess.";
        public int GrenadeBalanceRevision { get; set; }
        public int AudioBalanceRevision { get; set; }
        public List<ItemSpec> Items { get; set; } = new List<ItemSpec>
        {
            new ItemSpec
            {
                Id = 7101,
                Name = "Irritant Gas Grenade",
                Description = "Creates thick smoke to block vision.",
                BaseType = ItemType.GrenadeFlash
            },
            new ItemSpec
            {
                Id = 7102,
                Name = "Cluster Grenade",
                Description = "Bursts into smaller grenades that scatter and explode.",
                BaseType = ItemType.GrenadeHE
            },
            new ItemSpec
            {
                Id = 7103,
                Name = "Containment Foam Canister",
                Description = "Covers the floor around it in SCP-173's sticky tantrum mess.",
                BaseType = ItemType.GrenadeFlash
            },
            new ItemSpec
            {
                Id = 7105,
                Name = "Door Jammer",
                Description = "Use this card on a closed door to jam it briefly.",
                BaseType = ItemType.KeycardJanitor
            },
            CreateImpact(),
        }.Concat(SupCatalog.Defaults()).ToList();

        private static ItemSpec CreateImpact() => new ItemSpec
        {
            Id = 7113,
            Name = "Impact Grenade",
            BaseType = ItemType.GrenadeHE,
            Description = "Explodes when it hits a surface."
        };
        internal void Prepare()
        {
            if (Items == null || Items.Any(i => i == null))
                throw new InvalidOperationException("Items cannot contain null entries.");
            Items.RemoveAll(item => item.Id == 7110 || Retired.Contains(item.Id));
            Distribution?.RemoveRetired(Retired);
            if (CatalogRevision < 4)
            {
                foreach (var spec in SupCatalog.Defaults())
                    if (!Items.Any(i => i.Id == spec.Id))
                        Items.Add(spec);
                Distribution?.UpgradeSup();
            }

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

            foreach (var item in Items.Where(i => i.Id == 7101))
            {
                item.BaseType = ItemType.GrenadeFlash;
                if (item.Description == "Eight seconds of native SCP smoke. Obscures vision and slows humans 20%; no gas damage. Filter blocks the slow." || item.Description == "Eight-second cloud. Filter or leave it. SCPs are unaffected.")
                    item.Description = "Flashbang body releasing native SCP-244 smoke only. No flash, blast, custom slow or damage.";
            }

            Distribution?.UpgradeSmokeBody();
            Brainless.Internal.Tuning.Validate(Tuning, "tuning");
            Tuning.Supply.PairGasWithFilter = false;
            Tuning.GasGrenade.Upgrade(CatalogRevision);
            foreach (var foam in Items.Where(i => i.Id == 7103 && i.Description == "Slows anyone walking through the foam."))
                foam.Description = FoamDescription;
            Tuning.DoorBrace.Upgrade(CatalogRevision);
            // Revision 9: fewer, wider-spaced foam puddles (untouched 0.8.0 values only).
            if (CatalogRevision < 9 && Tuning.ContainmentFoam.Spacing == 2.5f && Tuning.ContainmentFoam.MaxPuddles == 24)
            {
                Tuning.ContainmentFoam.Spacing = 3;
                Tuning.ContainmentFoam.MaxPuddles = 12;
            }

            // Revision 10: the stretched single puddle crashed the server when a player
            // stood in it at spawn (short throws), so the native-size grid is the default.
            if (CatalogRevision < 10)
                Tuning.ContainmentFoam.SinglePuddle = false;

            CatalogRevision = Math.Max(10, CatalogRevision);
            if (Sup == null)
                throw new InvalidOperationException("sup cannot be null.");
            Sup.Validate();
            GrenadeBalanceRevision = Tuning.UpgradeGrenades(GrenadeBalanceRevision);
            AudioBalanceRevision = Tuning.UpgradeAudio(AudioBalanceRevision);
            if (Distribution == null)
                throw new InvalidOperationException("distribution cannot be null.");
            Distribution.EnsureEntries(Items.Select(i => new KeyValuePair<uint, ItemType>(i.Id, i.BaseType)));
            Distribution.Validate(id => id >= 7101 && id <= 7122 && id != 7110 && !Retired.Contains(id));
            if (Tuning.Supply.MediumLobbyPlayers >= Tuning.Supply.LargeLobbyPlayers)
                throw new InvalidOperationException("The large-lobby threshold must exceed the medium-lobby threshold.");
            if (Items.Any(i => i.Id < 7101 || i.Id > 7122 || i.Charges < 1 || i.Charges > 1000 || i.RoundCap < 0 || float.IsNaN(i.Weight) || float.IsInfinity(i.Weight) || i.Weight < 0))
                throw new InvalidOperationException("Item IDs must be 7101-7122 (7110 retired), charges 1-1000, and caps/weight nonnegative.");
            if (Items.Select(i => i.Id).Distinct().Count() != Items.Count || Items.Any(i => string.IsNullOrWhiteSpace(i.Name)) || Items.Select(i => i.Name).Distinct().Count() != Items.Count)
                throw new InvalidOperationException("Item IDs/names must be nonempty and unique.");
            foreach (var expected in SupCatalog.Defaults())
                if (Items.Any(i => i.Id == expected.Id && i.BaseType != expected.BaseType))
                    throw new InvalidOperationException("SUP item " + expected.Id + " must keep its native body.");
            foreach (var i in Items)
            {
                bool throwable = i.Id == 7101 || i.Id == 7102 || i.Id == 7103 || i.Id == 7113;
                if (throwable && i.BaseType != (i.Id == 7101 || i.Id == 7103 ? ItemType.GrenadeFlash : ItemType.GrenadeHE))
                    throw new InvalidOperationException("Throwable " + i.Id + " must keep its native grenade body.");
            }

            var descriptions = new Config().Items.ToDictionary(i => i.Id, i => i.Description);
            foreach (var entry in Items)
                entry.Description = GameplayText.Clean(entry.Description, descriptions.TryGetValue(entry.Id, out string shortText) ? shortText : "");
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
                spec.Description = "Flashbang body releasing native SCP-244 smoke only. No flash, blast, custom slow or damage.";
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
            if (item.Type != Type || !player.Items.Contains(item))
                return "The native inventory item changed.";
            string failure = owner.IssueFailure(Spec, 1, false);
            if (failure != null)
                return failure;
            if (!owner.Mint(this, item.Serial))
                return "Natural supply was exhausted.";
            TrackedSerials.Add(item.Serial);
            return null;
        }

        internal string AdoptPickup(Pickup pickup, Chamber chamber)
        {
            if (pickup.Base == null || pickup.Type != Type)
                return "The native map pickup changed.";
            string failure = owner.IssueFailure(Spec, 1, false);
            if (failure != null)
                return failure;
            if (!owner.Mint(this, pickup.Serial))
                return "Natural supply was exhausted.";
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
            if (player.Items.Count + 1 > 8)
                return "Inventory needs a free slot.";
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

            return null;
        }
    }
}
