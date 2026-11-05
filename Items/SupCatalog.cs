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
                Description = "Calls a native mini wave for your faction.",
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

        // Cursed items: obvious trade-offs, from coin tails and rare natural loot.
        internal const uint CursedKeycard = 7123, CursedScp500 = 7124, CursedAdrenaline = 7125, MonkeysPaw = 7126;
        internal const uint CursedMedkit = 7127, CursedGrenade = 7128, CursedFlashbang = 7129, CursedHat = 7130, CursedCola = 7131, CursedLantern = 7132;

        internal static IEnumerable<ItemSpec> CursedDefaults() => new[]
        {
            new ItemSpec
            {
                Id = 7123,
                Name = "Cursed Keycard",
                BaseType = ItemType.KeycardO5,
                Description = "Opens every door except the Alpha Warhead, but each door tells the SCPs where you are. Crumbles after 5 doors.",
                Charges = 5,
                RoundCap = 2
            },
            new ItemSpec
            {
                Id = 7124,
                Name = "Cursed SCP-500",
                BaseType = ItemType.SCP500,
                Description = "50/50: a super heal (full heal, cures, +50 AHP and regeneration) or nothing at all.",
                RoundCap = 2
            },
            new ItemSpec
            {
                Id = 7125,
                Name = "Cursed Adrenaline",
                BaseType = ItemType.Adrenaline,
                Description = "+60% speed for 10 seconds, then you crash: frozen for 5 seconds and blinded for 3.",
                RoundCap = 2
            },
            new ItemSpec
            {
                Id = 7126,
                Name = "Monkey's Paw",
                BaseType = ItemType.Coin,
                Description = "Flip it to make a wish. You get what you wished for... with a twist. Single use.",
                RoundCap = 2
            },
            new ItemSpec
            {
                Id = 7127,
                Name = "Cursed Medkit",
                BaseType = ItemType.Medkit,
                Description = "Heals you to full instantly, but each use lowers your max HP by 10. 3 uses.",
                Charges = 3,
                RoundCap = 2
            },
            new ItemSpec
            {
                Id = 7128,
                Name = "Cursed Grenade",
                BaseType = ItemType.GrenadeHE,
                Description = "Double blast radius and damage, but a 1 in 4 chance it goes off in your hand when you pull the pin.",
                RoundCap = 2
            },
            new ItemSpec
            {
                Id = 7129,
                Name = "Cursed Flashbang",
                BaseType = ItemType.GrenadeFlash,
                Description = "Blinds everyone around much longer, but it always blinds you too.",
                RoundCap = 2
            },
            new ItemSpec
            {
                Id = 7130,
                Name = "Cursed Hat",
                BaseType = ItemType.SCP268,
                Description = "Invisible for 60 seconds instead of 15, but you're 30% slower while invisible. 2 uses.",
                Charges = 2,
                RoundCap = 2
            },
            new ItemSpec
            {
                Id = 7131,
                Name = "Cursed Cola",
                BaseType = ItemType.SCP207,
                Description = "Four colas' worth of speed at once, but your health drains much faster.",
                RoundCap = 2
            },
            new ItemSpec
            {
                Id = 7132,
                Name = "Cursed Lantern",
                BaseType = ItemType.SCP2176,
                Description = "Throw it to black out your whole zone for 30 seconds, but you're blinded too.",
                RoundCap = 2
            }
        };
    }
}
