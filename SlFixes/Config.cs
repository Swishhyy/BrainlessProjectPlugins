using System.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using Brainless.Internal.Updates;
using Exiled.API.Interfaces;
using Scp914;

namespace Brainless.SlFixes
{
    public sealed partial class Config : IConfig
    {
        public bool IsEnabled { get; set; } = true;
        public bool Debug { get; set; }
        public AutoUpdateConfig AutoUpdate { get; set; } = new AutoUpdateConfig();
        public bool RoughTeleportsPlayers { get; set; } = true;
        public double RoughTeleportChance { get; set; } = 100;

        [Description("Rooms SCP-914 Rough may send players to. Only open corridor-type rooms by default: rooms with machinery, cells or furniture in the middle got players stuck. Light Containment rooms are skipped after decontamination.")]
        public List<Exiled.API.Enums.RoomType> RoughTeleportRooms { get; set; } = new List<Exiled.API.Enums.RoomType>
        {
            Exiled.API.Enums.RoomType.LczStraight,
            Exiled.API.Enums.RoomType.LczCurve,
            Exiled.API.Enums.RoomType.LczCrossing,
            Exiled.API.Enums.RoomType.LczTCross,
            Exiled.API.Enums.RoomType.HczStraight,
            Exiled.API.Enums.RoomType.HczStraightC,
            Exiled.API.Enums.RoomType.HczStraightVariant,
            Exiled.API.Enums.RoomType.HczCurve,
            Exiled.API.Enums.RoomType.HczCrossing,
            Exiled.API.Enums.RoomType.HczIntersection,
            Exiled.API.Enums.RoomType.EzStraight,
            Exiled.API.Enums.RoomType.EzCurve,
            Exiled.API.Enums.RoomType.EzCrossing,
            Exiled.API.Enums.RoomType.EzTCross,
        };
        public bool EnableCrafting { get; set; } = true;
        public bool EnableXp { get; set; } = true;
        public CaptureConversionConfig CaptureConversions { get; set; } = new CaptureConversionConfig();

        [Description("An uncuffed Facility Guard who reaches the surface escape zone joins MTF.")]
        public GuardEscapeConfig GuardEscape { get; set; } = new GuardEscapeConfig();

        [Description("When the round ends, turn on friendly fire so everyone can attack everyone until the next round. The game does not punish friendly fire after the round ends.")]
        public EndOfRoundFriendlyFireConfig EndOfRoundFriendlyFire { get; set; } = new EndOfRoundFriendlyFireConfig();
        public int MaxCraftXpAwardsPerPlayerPerRound { get; set; } = 3;
        public int MaxTeleportXpAwardsPerPlayerPerRound { get; set; } = 3;
        public int MinimumRoundSecondsForXp { get; set; } = 120;
        public Dictionary<string, int> XpRewards { get; set; } = new Dictionary<string, int>
        {
            ["kill_human"] = 10,
            ["kill_scp"] = 50,
            ["escape"] = 35,
            ["craft"] = 5,
            ["914_teleport"] = 2,
            ["round_played"] = 5,
            ["round_survived"] = 10,
            ["death"] = 0
        };
        public int RecipeRevision { get; set; }
        public List<CraftingRecipe> Recipes { get; set; } = new List<CraftingRecipe>
        {
            new CraftingRecipe
            {
                Name = "Smoke Grenade",
                Source = ItemType.GrenadeFlash,
                Setting = Scp914KnobSetting.Fine,
                CustomId = 7101,
                Chance = 40,
                MaxPerRound = 3
            },
            new CraftingRecipe
            {
                Name = "Cluster Grenade",
                Source = ItemType.GrenadeHE,
                Setting = Scp914KnobSetting.VeryFine,
                CustomId = 7102,
                Chance = 25,
                MaxPerRound = 2
            },
            new CraftingRecipe
            {
                Name = "Impact Grenade",
                Source = ItemType.GrenadeHE,
                Setting = Scp914KnobSetting.Fine,
                CustomId = 7113,
                Chance = 30,
                MaxPerRound = 2
            }
        };

        private static IEnumerable<CraftingRecipe> SupRecipes() => new[]
        {
            R("Adrenaline Pills", ItemType.SCP500, Scp914KnobSetting.Fine, 7114, 20),
            R("Anti-096 Pills", ItemType.SCP500, Scp914KnobSetting.VeryFine, 7115, 15),
            R("Expired Pills", ItemType.SCP500, Scp914KnobSetting.Coarse, 7116, 40),
            R("Invisibility Pills", ItemType.SCP500, Scp914KnobSetting.VeryFine, 7117, 10),
            R("Hacking Chip", ItemType.KeycardJanitor, Scp914KnobSetting.VeryFine, 7119, 15),
            R("Reinforcement Radio", ItemType.Radio, Scp914KnobSetting.VeryFine, 7120, 10),
            R("Volatile Pills", ItemType.SCP500, Scp914KnobSetting.Rough, 7122, 10)
        };
        private static CraftingRecipe R(string name, ItemType source, Scp914KnobSetting setting, uint id, double chance) => new CraftingRecipe
        {
            Name = name,
            Source = source,
            Setting = setting,
            CustomId = id,
            Chance = chance,
            MaxPerRound = id == 7208 || id == 7120 || id == 7122 ? 1 : 2
        };
        internal void Validate()
        {
            if (EndOfRoundFriendlyFire == null || EndOfRoundFriendlyFire.BroadcastSeconds > 60 || EndOfRoundFriendlyFire.Message == null)
                throw new InvalidOperationException("end_of_round_friendly_fire needs a message and broadcast_seconds of 0-60.");
            if (CaptureConversions == null || double.IsNaN(CaptureConversions.MinimumAliveSeconds) || double.IsInfinity(CaptureConversions.MinimumAliveSeconds) || CaptureConversions.MinimumAliveSeconds < 0 || CaptureConversions.MinimumAliveSeconds > 3600 || !(CaptureConversions.ChaosRole == PlayerRoles.RoleTypeId.ChaosConscript || CaptureConversions.ChaosRole == PlayerRoles.RoleTypeId.ChaosRifleman || CaptureConversions.ChaosRole == PlayerRoles.RoleTypeId.ChaosMarauder || CaptureConversions.ChaosRole == PlayerRoles.RoleTypeId.ChaosRepressor) || !(CaptureConversions.MtfRole == PlayerRoles.RoleTypeId.NtfPrivate || CaptureConversions.MtfRole == PlayerRoles.RoleTypeId.NtfSergeant || CaptureConversions.MtfRole == PlayerRoles.RoleTypeId.NtfSpecialist || CaptureConversions.MtfRole == PlayerRoles.RoleTypeId.NtfCaptain))
                throw new InvalidOperationException("Invalid capture conversion role or minimum_alive_seconds.");
            if (double.IsNaN(RoughTeleportChance) || double.IsInfinity(RoughTeleportChance) || RoughTeleportChance < 0 || RoughTeleportChance > 100 || MaxCraftXpAwardsPerPlayerPerRound < 0 || MaxTeleportXpAwardsPerPlayerPerRound < 0 || MinimumRoundSecondsForXp < 0)
                throw new InvalidOperationException("Invalid teleport/XP limits.");
            if (Recipes == null || XpRewards == null || XpRewards.Any(p => string.IsNullOrWhiteSpace(p.Key) || p.Value < 0 || p.Value > 10000))
                throw new InvalidOperationException("Recipes and nonnegative XP rewards are required.");
            if (RecipeRevision < 1)
            {
                foreach (var recipe in Recipes.Count > 0 ? SupRecipes() : Enumerable.Empty<CraftingRecipe>())
                    if (!Recipes.Any(r => r != null && r.CustomId == recipe.CustomId))
                        Recipes.Add(recipe);
                RecipeRevision = 1;
            }

            // Recipes for removed items (Field Dressing, SCRAMBLE Goggles, Bio Grenade, Railgun).
            Recipes.RemoveAll(r => r != null && RetiredRecipes.Contains(r.CustomId));
            if (GuardEscape == null || !Enum.IsDefined(typeof(PlayerRoles.RoleTypeId), GuardEscape.Role) || !GuardEscapeConfig.FoundationRoles.Contains(GuardEscape.Role) || double.IsNaN(GuardEscape.MinimumAliveSeconds) || GuardEscape.MinimumAliveSeconds < 0)
                throw new InvalidOperationException("guard_escape.role must be a Foundation (MTF/Guard) role and minimum_alive_seconds nonnegative.");
            foreach (var recipe in Recipes)
                if (recipe == null || string.IsNullOrWhiteSpace(recipe.Name) || !Enum.IsDefined(typeof(ItemType), recipe.Source) || recipe.Source == ItemType.None || !Enum.IsDefined(typeof(Scp914KnobSetting), recipe.Setting) || recipe.CustomId < 1 || recipe.MaxPerRound < 0 || double.IsNaN(recipe.Chance) || double.IsInfinity(recipe.Chance) || recipe.Chance < 0 || recipe.Chance > 100)
                    throw new InvalidOperationException("Invalid SCP-914 crafting recipe.");
            if (Recipes.GroupBy(r => r.CustomId).Any(g => g.Count() > 1))
                throw new InvalidOperationException("Use one recipe per custom ID; recipe IDs must be unique.");
        }
    }

    public sealed class CaptureConversionConfig
    {
        public bool Enabled { get; set; } = true;
        public double MinimumAliveSeconds { get; set; } = 10;
        public PlayerRoles.RoleTypeId ChaosRole { get; set; } = PlayerRoles.RoleTypeId.ChaosConscript;
        public PlayerRoles.RoleTypeId MtfRole { get; set; } = PlayerRoles.RoleTypeId.NtfPrivate;
    }

    public sealed partial class Config
    {
        private static readonly uint[] RetiredRecipes = { 7104, 7106, 7109, 7111, 7118, 7121, 7201, 7203, 7208 };
    }

    public sealed class EndOfRoundFriendlyFireConfig
    {
        public bool Enabled { get; set; } = true;

        [Description("Seconds to show the broadcast; 0 for none.")]
        public ushort BroadcastSeconds { get; set; } = 5;
        public string Message { get; set; } = "<b><color=#e74c3c>FRIENDLY FIRE IS ON</color></b>\nThe round is over. Everyone can attack everyone!";
    }

    public sealed class GuardEscapeConfig
    {
        internal static readonly PlayerRoles.RoleTypeId[] FoundationRoles = { PlayerRoles.RoleTypeId.FacilityGuard, PlayerRoles.RoleTypeId.NtfPrivate, PlayerRoles.RoleTypeId.NtfSergeant, PlayerRoles.RoleTypeId.NtfSpecialist, PlayerRoles.RoleTypeId.NtfCaptain };

        public bool Enabled { get; set; } = true;
        public double MinimumAliveSeconds { get; set; } = 10;
        public PlayerRoles.RoleTypeId Role { get; set; } = PlayerRoles.RoleTypeId.NtfSergeant;
    }

    public sealed class CraftingRecipe
    {
        public string Name { get; set; }
        public ItemType Source { get; set; }
        public Scp914KnobSetting Setting { get; set; }
        public uint CustomId { get; set; }
        public double Chance { get; set; } = 25;
        public int MaxPerRound { get; set; } = 2;
        public bool Enabled { get; set; } = true;
    }
}
