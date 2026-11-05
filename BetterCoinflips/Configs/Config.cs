using System.Collections.Generic;
using System.ComponentModel;
using Exiled.API.Enums;
using Exiled.API.Interfaces;
using PlayerRoles;

namespace BetterCoinflips.Configs
{
    public class Config : IConfig
    {
        [Description("Whether or not the plugin should be enabled. Default: true")]
        public bool IsEnabled { get; set; } = true;

        [Description("Whether or not debug logs should be shown. Default: false")]
        public bool Debug { get; set; } = false;
        public Brainless.Internal.Updates.AutoUpdateConfig AutoUpdate { get; set; } = new Brainless.Internal.Updates.AutoUpdateConfig();

        [Description("Balance migration marker. Revision 2: coins lean toward goodies and harmless pranks with a little gambling; nothing sends players to spectator by default (kicks, spectator swap, instant explosion and Tesla teleport off, damage effects leave 1 HP), and lethal effects are dropped from bad_effects. Only untouched stock values change; set 2 to keep your values.")]
        public int BalanceRevision { get; set; }

        [Description("Percent of tails flips that turn lucky and give a heads (good) outcome instead. Keeps coins mostly fun while tails is still a gamble. 0 = plain 50/50.")]
        public int LuckyTailsPercent { get; set; } = 30;

        [Description("The amount of base game spawned coins that should be removed. Default: 4")]
        public int DefaultCoinsAmount { get; set; } = 4;

        [Description("The ItemType of the item to be replaced with a coin and the amount to be replaced, the item is supposed to be something found in SCP pedestals.")]
        public Dictionary<ItemType, int> ItemToReplace { get; set; } = new()
        {
            {
                ItemType.SCP500,
                2
            }
        };

        [Description("The boundaries of the random range of throws each coin will have before it breaks. The upper bound is exclusive.")]
        public List<int> MinMaxDefaultCoins { get; set; } = new()
        {
            1,
            4
        };

        [Description("Time in seconds between coin toses.")]
        public double CoinCooldown { get; set; } = 5;

        [Description("The duration of the broadcast informing you about your 'reward'. Default: 3")]
        public ushort BroadcastTime { get; set; } = 3;

        [Description("The duration of the hint telling you if you got heads or tails. Set to 0 or less to disable.")]
        public float HintDuration { get; set; } = 3;

        [Description("The duration of the map blackout. Default: 10")]
        public float MapBlackoutTime { get; set; } = 10;

        [Description("The fuse time of the grenade falling on your head. Default: 3.25")]
        public double LiveGrenadeFuseTime { get; set; } = 3.25;

        [Description("List of bad effects that can be applied to the players. List available at: https://exiled-team.github.io/EXILED/api/Exiled.API.Enums.EffectType.html")]
        public HashSet<EffectType> BadEffects { get; set; } = new()
        {
            EffectType.Asphyxiated,
            EffectType.Bleeding,
            EffectType.Blinded,
            EffectType.Burned,
            EffectType.Concussed,
            EffectType.Corroding,
            EffectType.Deafened,
            EffectType.Disabled,
            EffectType.Ensnared,
            EffectType.Exhausted,
            EffectType.Flashed,
            EffectType.Hemorrhage,
            EffectType.Hypothermia,
            EffectType.InsufficientLighting,
            EffectType.Poisoned,
            EffectType.SeveredHands,
            EffectType.SinkHole,
            EffectType.Stained,
            EffectType.Traumatized
        };

        [Description("List of good effects that can be applied to the players. List available at: https://exiled-team.github.io/EXILED/api/Exiled.API.Enums.EffectType.html")]
        public HashSet<EffectType> GoodEffects { get; set; } = new()
        {
            EffectType.BodyshotReduction,
            EffectType.DamageReduction,
            EffectType.Invigorated,
            EffectType.Invisible,
            EffectType.MovementBoost,
            EffectType.RainbowTaste,
            EffectType.Scp1853,
            EffectType.Scp207,
            EffectType.Vitality
        };

        [Description("The % chance of receiving a Facility Manager keycard instead of a Containment Engineer one.")]
        public int RedCardChance { get; set; } = 15;

        [Description("The kick reason.")]
        public string KickReason { get; set; } = "The coin kicked your ass.";

        [Description("The list of SCP's that you can turn into by using the coin.")]
        public HashSet<RoleTypeId> ValidScps { get; set; } = new()
        {
            RoleTypeId.Scp049,
            RoleTypeId.Scp096,
            RoleTypeId.Scp106,
            RoleTypeId.Scp173,
            RoleTypeId.Scp0492,
            RoleTypeId.Scp939,
        };

        [Description("List of ignored roles for the PlayerSwap effect (#17)")]
        public HashSet<RoleTypeId> PlayerSwapIgnoredRoles { get; set; } = new()
        {
            RoleTypeId.Spectator,
            RoleTypeId.Filmmaker,
            RoleTypeId.Overwatch,
            RoleTypeId.Scp079,
            RoleTypeId.Tutorial,
        };

        [Description("List of ignored roles for the InventorySwap effect (#17)")]
        public HashSet<RoleTypeId> InventorySwapIgnoredRoles { get; set; } = new()
        {
            RoleTypeId.Spectator,
            RoleTypeId.Filmmaker,
            RoleTypeId.Overwatch,
            RoleTypeId.Scp079,
            RoleTypeId.Tutorial,
            RoleTypeId.Scp049,
            RoleTypeId.Scp079,
            RoleTypeId.Scp096,
            RoleTypeId.Scp106,
            RoleTypeId.Scp173,
            RoleTypeId.Scp0492,
            RoleTypeId.Scp939,
            RoleTypeId.Scp3114,
        };
        public HashSet<ItemType> ItemsToGive { get; set; } = new()
        {
            ItemType.Adrenaline,
            ItemType.Coin,
            ItemType.Flashlight,
            ItemType.Jailbird,
            ItemType.Medkit,
            ItemType.Painkillers,
            ItemType.Radio,
            ItemType.ArmorCombat,
            ItemType.ArmorHeavy,
            ItemType.ArmorLight,
            ItemType.GrenadeFlash,
            ItemType.GrenadeHE,
            ItemType.GunA7,
            ItemType.GunCom45,
            ItemType.GunCrossvec,
            ItemType.GunLogicer,
            ItemType.GunRevolver,
            ItemType.GunShotgun,
            ItemType.GunAK,
            ItemType.GunCOM15,
            ItemType.GunCOM18,
            ItemType.GunE11SR,
            ItemType.GunFSP9,
            ItemType.GunFRMG0,
        };
        public HashSet<RoomType> RoomsToTeleport { get; set; } = new()
        {
            RoomType.EzCafeteria,
            RoomType.EzCheckpointHallwayA,
            RoomType.EzCheckpointHallwayB,
            RoomType.EzCollapsedTunnel,
            RoomType.EzConference,
            RoomType.EzCrossing,
            RoomType.EzCurve,
            RoomType.EzDownstairsPcs,
            RoomType.EzGateA,
            RoomType.EzGateB,
            RoomType.EzIntercom,
            RoomType.EzPcs,
            RoomType.EzStraight,
            RoomType.EzTCross,
            RoomType.EzUpstairsPcs,
            RoomType.EzVent,
            RoomType.Hcz049,
            RoomType.Hcz079,
            RoomType.Hcz096,
            RoomType.Hcz106,
            RoomType.Hcz939,
            RoomType.HczArmory,
            RoomType.HczCrossing,
            RoomType.HczCurve,
            RoomType.HczElevatorA,
            RoomType.HczElevatorB,
            RoomType.HczEzCheckpointA,
            RoomType.HczEzCheckpointB,
            RoomType.HczHid,
            RoomType.HczNuke,
            RoomType.HczStraight,
            RoomType.HczTesla,
            RoomType.HczTestRoom,
            RoomType.Lcz173,
            RoomType.Lcz330,
            RoomType.Lcz914,
            RoomType.LczAirlock,
            RoomType.LczArmory,
            RoomType.LczCafe,
            RoomType.LczCheckpointA,
            RoomType.LczCheckpointB,
            RoomType.LczClassDSpawn,
            RoomType.LczCrossing,
            RoomType.LczCurve,
            RoomType.LczGlassBox,
            RoomType.LczPlants,
            RoomType.LczStraight,
            RoomType.LczTCross,
            RoomType.LczToilets,
            RoomType.Surface,
        };

        [Description("Brainless integration. Heads can give a Brainless custom item (needs the Items/Weapons DLLs). Proportional chance like the effects below; skipped automatically when none of the listed items is installed.")]
        public int CustomItemChance { get; set; } = 40;

        [Description("Brainless item/weapon IDs that heads can give (see docs/CATALOG.md). Empty = any installed Brainless item or weapon.")]
        public List<uint> CustomItems { get; set; } = new()
        {
            7101,
            7102,
            7103,
            7105,
            7113,
            7114,
            7115,
            7117,
            7119,
            7209,
        };

        [Description("Tails can give a cursed Brainless item. Proportional chance; skipped automatically when none of the listed items is installed.")]
        public int CursedCustomItemChance { get; set; } = 15;
        public List<uint> CursedCustomItems { get; set; } = new()
        {
            7116,
            7122,
        };

        [Description("Give coin custom items even when their natural per-round supply is used up. Disabled items are never given.")]
        public bool CustomItemsIgnoreSupply { get; set; } = false;

        [Description("Heads can turn you into a random Brainless custom role that fits your current class, without moving you (needs the Roles DLL). Proportional chance; falls back to a random item if no role fits.")]
        public int CustomRoleChance { get; set; } = 25;

        [Description("Role IDs or names heads can grant, e.g. [C05, S04]. Empty = any enabled profile that fits the player's class.")]
        public List<string> CustomRoles { get; set; } = new();

        [Description("When a coin changes your class (Turn into SCP, UNO reverse), roll a Brainless custom role for the new class using the normal role chances (needs the Roles DLL).")]
        public bool RollCustomRoleOnClassChange { get; set; } = true;

        [Description("The chance of these good effects happening. It's a proportional chance not a % chance.")]
        public int KeycardChance { get; set; } = 20;
        public int MedicalKitChance { get; set; } = 35;
        public int TpToEscapeChance { get; set; } = 5;
        public int HealChance { get; set; } = 10;
        public int MoreHpChance { get; set; } = 10;
        public int HatChance { get; set; } = 10;
        public int RandomGoodEffectChance { get; set; } = 30;
        public int OneAmmoLogicerChance { get; set; } = 1;
        public int LightbulbChance { get; set; } = 15;
        public int PinkCandyChance { get; set; } = 10;
        public int BadRevoChance { get; set; } = 5;
        public int EmptyHidChance { get; set; } = 5;
        public int ForceRespawnChance { get; set; } = 15;
        public int SizeChangeChance { get; set; } = 20;
        public int RandomItemChance { get; set; } = 35;

        [Description("The chance of these bad effects happening. It's a proportional chance not a % chance.")]
        public int HpReductionChance { get; set; } = 15;
        public int TpToClassDCellsChance { get; set; } = 5;
        public int RandomBadEffectChance { get; set; } = 15;
        public int WarheadChance { get; set; } = 3;
        public int LightsOutChance { get; set; } = 20;
        public int LiveHeChance { get; set; } = 8;
        public int TrollFlashChance { get; set; } = 50;
        public int ScpTpChance { get; set; } = 12;
        public int OneHpLeftChance { get; set; } = 8;
        public int PrimedVaseChance { get; set; } = 20;
        public int ShitPantsChance { get; set; } = 40;
        public int FakeCassieChance { get; set; } = 50;
        public int TurnIntoScpChance { get; set; } = 8;
        public int InventoryResetChance { get; set; } = 6;
        public int ClassSwapChance { get; set; } = 6;
        public int InstantExplosionChance { get; set; } = 0;
        public int PlayerSwapChance { get; set; } = 20;
        public int KickChance { get; set; } = 0;
        public int SpectSwapChance { get; set; } = 0;
        public int TeslaTpChance { get; set; } = 0;
        public int InventorySwapChance { get; set; } = 12;
        public int HandcuffChance { get; set; } = 6;
        public int RandomTeleportChance { get; set; } = 15;

        private static readonly EffectType[] LethalEffects = { EffectType.Decontaminating, EffectType.CardiacArrest, EffectType.PocketCorroding };
        private static readonly EffectType[] OldBadEffects = { EffectType.Asphyxiated, EffectType.Bleeding, EffectType.Blinded, EffectType.Burned, EffectType.Concussed, EffectType.Corroding, EffectType.CardiacArrest, EffectType.Deafened, EffectType.Decontaminating, EffectType.Disabled, EffectType.Ensnared, EffectType.Exhausted, EffectType.Flashed, EffectType.Hemorrhage, EffectType.Hypothermia, EffectType.InsufficientLighting, EffectType.Poisoned, EffectType.PocketCorroding, EffectType.SeveredHands, EffectType.SinkHole, EffectType.Stained, EffectType.Traumatized };

        // Returns true when values changed and should be saved. Values still at an earlier
        // stock default move to the current default; owner-edited values are kept.
        internal bool UpgradeBalance()
        {
            if (BalanceRevision >= 2)
                return false;
            var stock = new Config();
            var properties = typeof(Config).GetProperties();
            var history = new Dictionary<string, int[]> { { "HpReductionChance", new[] { 20, 15 } }, { "WarheadChance", new[] { 10, 3 } }, { "ScpTpChance", new[] { 20, 12 } }, { "OneHpLeftChance", new[] { 15, 8 } }, { "TurnIntoScpChance", new[] { 30, 8 } }, { "InventoryResetChance", new[] { 20, 6 } }, { "ClassSwapChance", new[] { 10, 6 } }, { "InstantExplosionChance", new[] { 10, 4 } }, { "KickChance", new[] { 5, 0 } }, { "SpectSwapChance", new[] { 10, 3 } }, { "TeslaTpChance", new[] { 15, 6 } }, { "InventorySwapChance", new[] { 20, 12 } }, { "HandcuffChance", new[] { 10, 6 } }, { "LiveHeChance", new[] { 30, 15 } }, { "RandomBadEffectChance", new[] { 20, 15 } }, { "CustomItemChance", new[] { 30 } }, { "CustomRoleChance", new[] { 20 } } };
            foreach (var pair in history)
            {
                var property = System.Array.Find(properties, p => p.Name == pair.Key);
                if (System.Array.IndexOf(pair.Value, (int)property.GetValue(this)) >= 0)
                    property.SetValue(this, property.GetValue(stock));
            }

            if (BadEffects != null && BadEffects.SetEquals(OldBadEffects))
                BadEffects.ExceptWith(LethalEffects);
            BalanceRevision = 2;
            return true;
        }
    }
}
