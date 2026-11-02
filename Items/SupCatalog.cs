using System.Collections.Generic;

namespace Brainless.Items
{
    internal static class SupCatalog
    {
        internal static IEnumerable<ItemSpec> Defaults() => new[]
        {
            new ItemSpec
            {
                Id = 7114,
                Name = "SCP500-A Adrenaline Pills",
                BaseType = ItemType.SCP500,
                Description = "Boosts your speed and stamina, then leaves you exhausted.",
                RoundCap = 2
            },
            new ItemSpec
            {
                Id = 7115,
                Name = "SCP500-B Anti-096 Pills",
                BaseType = ItemType.SCP500,
                Description = "Removes you from SCP-096's target list.",
                RoundCap = 2
            },
            new ItemSpec
            {
                Id = 7116,
                Name = "SCP500-C Expired Pills",
                BaseType = ItemType.SCP500,
                Description = "Gives one random helpful or harmful effect.",
                RoundCap = 2
            },
            new ItemSpec
            {
                Id = 7117,
                Name = "SCP500-D Invisibility Pills",
                BaseType = ItemType.SCP500,
                Description = "Briefly turns you invisible. Attacking reveals you.",
                RoundCap = 2
            },
            new ItemSpec
            {
                Id = 7118,
                Name = "Bio Grenade",
                BaseType = ItemType.GrenadeFlash,
                Description = "Releases toxic smoke. Staying inside causes corrosion.",
                RoundCap = 2
            },
            new ItemSpec
            {
                Id = 7119,
                Name = "Hacking Chip",
                BaseType = ItemType.KeycardJanitor,
                Description = "Use this card to hack doors below level 3.",
                RoundCap = 2
            },
            new ItemSpec
            {
                Id = 7120,
                Name = "Reinforcement Radio",
                BaseType = ItemType.Radio,
                Description = "Calls a small group of reinforcements for your team.",
                RoundCap = 1
            },
            new ItemSpec
            {
                Id = 7122,
                Name = "SCP500-E Volatile Pills",
                BaseType = ItemType.SCP500,
                Description = "Makes you explode. You have a small chance to survive.",
                RoundCap = 1
            }
        };
    }
}
