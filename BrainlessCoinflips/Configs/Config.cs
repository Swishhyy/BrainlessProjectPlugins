using System.Collections.Generic;
using System.ComponentModel;
using Exiled.API.Enums;
using Exiled.API.Interfaces;
using PlayerRoles;

namespace Brainless.Coinflips.Configs
{
    public class Config : IConfig
    {
        [Description("Whether or not the plugin should be enabled. Default: true")]
        public bool IsEnabled { get; set; } = true;

        [Description("Whether or not debug logs should be shown. Default: false")]
        public bool Debug { get; set; } = false;
        public Brainless.Internal.Updates.AutoUpdateConfig AutoUpdate { get; set; } = new Brainless.Internal.Updates.AutoUpdateConfig();

        [Description("Balance migration marker. Revision 2: coins lean toward goodies and harmless pranks with a little gambling; nothing sends players to spectator by default (kicks, spectator swap, instant explosion and Tesla teleport off, damage effects leave 1 HP), and lethal effects are dropped from bad_effects. Only untouched stock values change; set 2 to keep your values. Revision 3 removes the admin-only Stun Gun (7209) from custom_items. Revision 4 stops coins replacing SCP-500 pedestal spawns.")]
        public int BalanceRevision { get; set; }


        [Description("The amount of base game spawned coins that should be removed. Default: 4")]
        public int DefaultCoinsAmount { get; set; } = 4;

        [Description("The ItemType of the item to be replaced with a coin and the amount to be replaced, the item is supposed to be something found in SCP pedestals.")]
        public Dictionary<ItemType, int> ItemToReplace { get; set; } = new();

        [Description("SCP-914 on Very Fine doubles ordinary coins (1 -> 2, 2 -> 4, ...), in the intake or in a player's inventory.")]
        public bool Scp914DoublesCoins { get; set; } = true;

        [Description("Most extra coins one player can get from SCP-914 doubling per round.")]
        public int Scp914CoinMaxPerPlayer { get; set; } = 20;

        [Description("Most extra coins SCP-914 doubling can create per round, for everyone together.")]
        public int Scp914CoinMaxPerRound { get; set; } = 80;

        [Description("No more doubling once this many coins exist (on the floor and in inventories). Keeps the server from filling up with coins.")]
        public int Scp914CoinWorldCap { get; set; } = 150;

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
        };

        [Description("Tails can give a cursed Brainless item. Proportional chance; skipped automatically when none of the listed items is installed.")]
        public int CursedCustomItemChance { get; set; } = 15;
        public List<uint> CursedCustomItems { get; set; } = new()
        {
            7123,
            7124,
            7125,
            7126,
            7127,
            7128,
            7129,
            7130,
            7131,
            7132,
            7210,
            7211,
        };

        [Description("Relative chance of each cursed item on tails (unlisted items are 1, 0 never drops). The Cursed Shotgun is a quarter as likely as the others.")]
        public Dictionary<uint, double> CursedCustomItemWeights { get; set; } = new()
        {
            { 7211, .25 },
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
        public int OneAmmoLogicerChance { get; set; } = 0;
        public int LightbulbChance { get; set; } = 15;
        public int PinkCandyChance { get; set; } = 10;
        public int BadRevoChance { get; set; } = 5;
        public int EmptyHidChance { get; set; } = 0;
        public int ForceRespawnChance { get; set; } = 15;
        public int SizeChangeChance { get; set; } = 20;
        public int RandomItemChance { get; set; } = 35;

        [Description("The chance of these bad effects happening. It's a proportional chance not a % chance.")]
        public int HpReductionChance { get; set; } = 15;
        public int TpToClassDCellsChance { get; set; } = 5;
        public int RandomBadEffectChance { get; set; } = 15;
        public int WarheadChance { get; set; } = 0;
        public int LightsOutChance { get; set; } = 20;
        public int LiveHeChance { get; set; } = 8;
        public int TrollFlashChance { get; set; } = 50;
        public int ScpTpChance { get; set; } = 12;
        public int OneHpLeftChance { get; set; } = 8;
        public int PrimedVaseChance { get; set; } = 20;
        public int ShitPantsChance { get; set; } = 40;
        public int FakeCassieChance { get; set; } = 50;
        public int TurnIntoScpChance { get; set; } = 0;
        public int InventoryResetChance { get; set; } = 0;
        public int ClassSwapChance { get; set; } = 6;
        public int InstantExplosionChance { get; set; } = 0;
        public int PlayerSwapChance { get; set; } = 20;
        public int KickChance { get; set; } = 0;
        public int SpectSwapChance { get; set; } = 0;
        public int TeslaTpChance { get; set; } = 0;
        public int InventorySwapChance { get; set; } = 0;
        public int HandcuffChance { get; set; } = 6;
        public int RandomTeleportChance { get; set; } = 15;

        [Description("Extra good outcomes. Proportional chances like the ones above.")]
        public int SpeedBoostChance { get; set; } = 20;
        public int ShieldUpChance { get; set; } = 20;
        public int ToughSkinChance { get; set; } = 15;
        public int SecondWindChance { get; set; } = 20;
        public int AmmoRefillChance { get; set; } = 20;
        public int KeycardUpgradeChance { get; set; } = 10;
        public int RandomScpItemChance { get; set; } = 15;
        public int LuckyCoinChance { get; set; } = 10;
        public int SecondChanceChance { get; set; } = 8;
        public int FullHealChance { get; set; } = 15;
        public int ArmorUpgradeChance { get; set; } = 15;
        public int GrenadePackChance { get; set; } = 15;
        public int ExtraCoinChance { get; set; } = 10;
        public int LoadedUpChance { get; set; } = 15;
        public int RegenerationChance { get; set; } = 15;
        public int SilentStepsChance { get; set; } = 10;
        public int CleanseChance { get; set; } = 10;
        public int MedicAuraChance { get; set; } = 12;
        public int RallyChance { get; set; } = 8;
        public int ReinforcementChance { get; set; } = 6;
        public int JackpotChance { get; set; } = 3;

        [Description("Extra bad outcomes. Proportional chances like the ones above. None of them can kill.")]
        public int FrozenChance { get; set; } = 15;
        public int SlowedChance { get; set; } = 15;
        public int ExhaustedChance { get; set; } = 15;
        public int SinkholeChance { get; set; } = 10;
        public int ButterfingersChance { get; set; } = 10;
        public int DropHeldItemChance { get; set; } = 15;
        public int LoseAmmoChance { get; set; } = 8;
        public int EmptyMagazineChance { get; set; } = 12;
        public int BlindedChance { get; set; } = 12;
        public int DeafenedChance { get; set; } = 12;
        public int ConcussedChance { get; set; } = 12;
        public int FlashedChance { get; set; } = 12;
        public int LocationLeakChance { get; set; } = 10;
        public int CassieCalloutChance { get; set; } = 8;
        public int LockedInChance { get; set; } = 10;
        public int RoomBlackoutChance { get; set; } = 12;
        public int DropKeycardsChance { get; set; } = 8;
        public int DropArmorChance { get; set; } = 8;
        public int CursedMaxHpChance { get; set; } = 8;
        public int BleedingChance { get; set; } = 10;
        public int HypothermiaChance { get; set; } = 10;
        public int StaminaDrainedChance { get; set; } = 15;
        public int PoisonedChance { get; set; } = 10;
        public int HuntedChance { get; set; } = 8;
        public int BackToSpawnChance { get; set; } = 8;
        public int JunkSwapChance { get; set; } = 8;
        public int TripChance { get; set; } = 12;
        public int WeakArmsChance { get; set; } = 10;
        public int GlassJawChance { get; set; } = 10;
        public int UnluckyCoinChance { get; set; } = 6;

        [Description("Custom item ID of the Monkey's Paw (Brainless Items). Flipping it grants one twisted wish and uses it up.")]
        public uint MonkeysPawId { get; set; } = 7126;

        [Description("Twisted wishes granted by the Monkey's Paw: something good with a catch. Proportional chances.")]
        public int FastThenWindedChance { get; set; } = 10;
        public int TankButHeavyChance { get; set; } = 10;
        public int UnkillableThenOneHpChance { get; set; } = 10;
        public int SilentButDeafChance { get; set; } = 10;
        public int StrongButFragileChance { get; set; } = 10;
        public int EndlessAmmoThenNoneChance { get; set; } = 10;
        public int FullHealThenBlindChance { get; set; } = 10;
        public int GhostThenExposedChance { get; set; } = 10;
        public int RichesButFrailChance { get; set; } = 10;
        public int FreeExitItemsLeftChance { get; set; } = 10;
        public int LuckyThenCursedChance { get; set; } = 10;

        [Description("Percent chance (checked before any other tails outcome) that tails deletes ALL of the player's items. Default 0.1.")]
        public double ItemWipeChancePercent { get; set; } = 0.1;

        [Description("Durations and strengths for the extra outcomes.")]
        public CoinTimings Timings { get; set; } = new CoinTimings();

        private static readonly EffectType[] LethalEffects = { EffectType.Decontaminating, EffectType.CardiacArrest, EffectType.PocketCorroding };
        private static readonly EffectType[] OldBadEffects = { EffectType.Asphyxiated, EffectType.Bleeding, EffectType.Blinded, EffectType.Burned, EffectType.Concussed, EffectType.Corroding, EffectType.CardiacArrest, EffectType.Deafened, EffectType.Decontaminating, EffectType.Disabled, EffectType.Ensnared, EffectType.Exhausted, EffectType.Flashed, EffectType.Hemorrhage, EffectType.Hypothermia, EffectType.InsufficientLighting, EffectType.Poisoned, EffectType.PocketCorroding, EffectType.SeveredHands, EffectType.SinkHole, EffectType.Stained, EffectType.Traumatized };

        // Returns true when values changed and should be saved. Values still at an earlier
        // stock default move to the current default; owner-edited values are kept.
        internal bool UpgradeBalance()
        {
            if (BalanceRevision >= 4)
                return false;
            if (BalanceRevision < 3)
                UpgradeToThree();
            // Revision 4: coins no longer take over SCP-500 spawns (stock value only).
            if (ItemToReplace != null && ItemToReplace.Count == 1 && ItemToReplace.TryGetValue(ItemType.SCP500, out int replaced) && replaced == 2)
                ItemToReplace.Clear();
            BalanceRevision = 4;
            return true;
        }

        private void UpgradeToThree()
        {
            // Revision 3: the Stun Gun (7209) is admin-only, so coins no longer hand it out.
            CustomItems?.Remove(7209);
            if (BalanceRevision == 2)
            {
                BalanceRevision = 3;
                return;
            }

            var stock = new Config();
            var properties = typeof(Config).GetProperties();
            var history = new Dictionary<string, int[]> { { "OneAmmoLogicerChance", new[] { 1 } }, { "EmptyHidChance", new[] { 5 } }, { "HpReductionChance", new[] { 20, 15 } }, { "WarheadChance", new[] { 10, 3 } }, { "ScpTpChance", new[] { 20, 12 } }, { "OneHpLeftChance", new[] { 15, 8 } }, { "TurnIntoScpChance", new[] { 30, 8 } }, { "InventoryResetChance", new[] { 20, 6 } }, { "ClassSwapChance", new[] { 10, 6 } }, { "InstantExplosionChance", new[] { 10, 4 } }, { "KickChance", new[] { 5, 0 } }, { "SpectSwapChance", new[] { 10, 3 } }, { "TeslaTpChance", new[] { 15, 6 } }, { "InventorySwapChance", new[] { 20, 12 } }, { "HandcuffChance", new[] { 10, 6 } }, { "LiveHeChance", new[] { 30, 15 } }, { "RandomBadEffectChance", new[] { 20, 15 } }, { "CustomItemChance", new[] { 30 } }, { "CustomRoleChance", new[] { 20 } } };
            foreach (var pair in history)
            {
                var property = System.Array.Find(properties, p => p.Name == pair.Key);
                if (property != null && System.Array.IndexOf(pair.Value, (int)property.GetValue(this)) >= 0)
                    property.SetValue(this, property.GetValue(stock));
            }

            if (CursedCustomItems != null && CursedCustomItems.Count == 2 && CursedCustomItems.Contains(7116) && CursedCustomItems.Contains(7122))
                CursedCustomItems = stock.CursedCustomItems;
            if (BadEffects != null && BadEffects.SetEquals(OldBadEffects))
                BadEffects.ExceptWith(LethalEffects);
            BalanceRevision = 3;
        }
    }

    public sealed class CoinTimings
    {
        public float FrozenSeconds { get; set; } = 5;
        public float SlowedSeconds { get; set; } = 20;
        public byte SlowedPercent { get; set; } = 30;
        public float ExhaustedSeconds { get; set; } = 20;
        public float SinkholeSeconds { get; set; } = 5;
        public float BlindedSeconds { get; set; } = 5;
        public float DeafenedSeconds { get; set; } = 15;
        public float ConcussedSeconds { get; set; } = 10;
        public float FlashedSeconds { get; set; } = 3;
        public float LockedInSeconds { get; set; } = 10;
        public float RoomBlackoutSeconds { get; set; } = 20;
        public float CursedMaxHp { get; set; } = 10;
        public float BleedingSeconds { get; set; } = 5;
        public float HypothermiaSeconds { get; set; } = 10;
        public float PoisonedSeconds { get; set; } = 5;
        public float TripSeconds { get; set; } = 2;
        public float WeakArmsSeconds { get; set; } = 30;
        public float WeakArmsPercent { get; set; } = 30;
        public float GlassJawSeconds { get; set; } = 20;
        public float GlassJawPercent { get; set; } = 25;
        public float SpeedBoostSeconds { get; set; } = 30;
        public byte SpeedBoostPercent { get; set; } = 20;
        public float ShieldUpAhp { get; set; } = 50;
        public float ToughSkinSeconds { get; set; } = 30;
        public float ToughSkinPercent { get; set; } = 20;
        public float SecondWindSeconds { get; set; } = 30;
        public int LuckyCoinFlips { get; set; } = 2;
        public float RegenerationPerSecond { get; set; } = 3;
        public float RegenerationSeconds { get; set; } = 20;
        public float SilentStepsSeconds { get; set; } = 60;
        public float MedicAuraHeal { get; set; } = 30;
        public float MedicAuraRange { get; set; } = 10;
        public int JackpotRewards { get; set; } = 3;
        public float FastSeconds { get; set; } = 300;
        public byte FastPercent { get; set; } = 25;
        public float WindedSeconds { get; set; } = 300;
        public float WindedRegenPercent { get; set; } = 60;
        public float TankMaxHp { get; set; } = 50;
        public byte TankSlowPercent { get; set; } = 15;
        public float UnkillableSeconds { get; set; } = 15;
        public float SilentDeafSeconds { get; set; } = 300;
        public float StrongFragileSeconds { get; set; } = 120;
        public float StrongFragilePercent { get; set; } = 50;
        public float EndlessAmmoSeconds { get; set; } = 60;
        public float FullHealAhp { get; set; } = 75;
        public float FullHealBlindSeconds { get; set; } = 8;
        public float GhostSeconds { get; set; } = 30;
        public float FrailMaxHp { get; set; } = 25;
        public int LuckyFlips { get; set; } = 3;
    }
}
