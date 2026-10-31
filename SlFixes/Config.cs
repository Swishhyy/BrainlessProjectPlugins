using System;
using System.Collections.Generic;
using System.Linq;
using Brainless.Internal.Updates;
using Exiled.API.Interfaces;
using Scp914;

namespace Brainless.SlFixes
{
    public sealed class Config : IConfig
    {
        public bool IsEnabled { get; set; } = true;
        public bool Debug { get; set; }
        public AutoUpdateConfig AutoUpdate { get; set; } = new AutoUpdateConfig();
        public bool RoughTeleportsPlayers { get; set; } = true;
        public double RoughTeleportChance { get; set; } = 100;
        public bool EnableCrafting { get; set; } = true;
        public bool EnableXp { get; set; } = true;
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
            },
            new CraftingRecipe
            {
                Name = "Field Dressing",
                Source = ItemType.Medkit,
                Setting = Scp914KnobSetting.OneToOne,
                CustomId = 7109,
                Chance = 35,
                MaxPerRound = 3
            }
        };

        internal void Validate()
        {
            if (double.IsNaN(RoughTeleportChance) || double.IsInfinity(RoughTeleportChance) || RoughTeleportChance < 0 || RoughTeleportChance > 100 || MaxCraftXpAwardsPerPlayerPerRound < 0 || MaxTeleportXpAwardsPerPlayerPerRound < 0 || MinimumRoundSecondsForXp < 0)
                throw new InvalidOperationException("Invalid teleport/XP limits.");
            if (Recipes == null || XpRewards == null || XpRewards.Any(p => string.IsNullOrWhiteSpace(p.Key) || p.Value < 0 || p.Value > 10000))
                throw new InvalidOperationException("Recipes and nonnegative XP rewards are required.");
            foreach (var recipe in Recipes)
                if (recipe == null || string.IsNullOrWhiteSpace(recipe.Name) || !Enum.IsDefined(typeof(ItemType), recipe.Source) || recipe.Source == ItemType.None || !Enum.IsDefined(typeof(Scp914KnobSetting), recipe.Setting) || recipe.CustomId < 1 || recipe.MaxPerRound < 0 || double.IsNaN(recipe.Chance) || double.IsInfinity(recipe.Chance) || recipe.Chance < 0 || recipe.Chance > 100)
                    throw new InvalidOperationException("Invalid SCP-914 crafting recipe.");
            if (Recipes.GroupBy(r => r.CustomId).Any(g => g.Count() > 1))
                throw new InvalidOperationException("Use one recipe per custom ID; recipe IDs must be unique.");
        }
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
