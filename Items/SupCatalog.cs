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
                Description = "Timed speed boost and stamina restoration, followed by exhaustion.",
                RoundCap = 2
            },
            new ItemSpec
            {
                Id = 7115,
                Name = "SCP500-B Anti-096 Pills",
                BaseType = ItemType.SCP500,
                Description = "Removes you from SCP-096 target lists. Only consumed when targeted.",
                RoundCap = 2
            },
            new ItemSpec
            {
                Id = 7116,
                Name = "SCP500-C Expired Pills",
                BaseType = ItemType.SCP500,
                Description = "One weighted random positive, negative or SCP effect. A gamble.",
                RoundCap = 2
            },
            new ItemSpec
            {
                Id = 7117,
                Name = "SCP500-D Invisibility Pills",
                BaseType = ItemType.SCP500,
                Description = "Seven seconds of stealth; attacking breaks it. Once per player per round.",
                RoundCap = 2
            },
            new ItemSpec
            {
                Id = 7118,
                Name = "Bio Grenade",
                BaseType = ItemType.GrenadeFlash,
                Description = "Native SCP-244 smoke. Sustained unfiltered exposure causes corrosion; leave the cloud.",
                RoundCap = 2
            },
            new ItemSpec
            {
                Id = 7119,
                Name = "Hacking Chip",
                BaseType = ItemType.KeycardJanitor,
                Description = "Equip and use the tool key at a door: eight-second hack below access level 3. Cannot bypass hard locks.",
                RoundCap = 2
            },
            new ItemSpec
            {
                Id = 7120,
                Name = "Reinforcement Radio",
                BaseType = ItemType.Radio,
                Description = "Calls up to three eligible spectators for your faction after 15 seconds. Limited per round.",
                RoundCap = 1
            },
            new ItemSpec
            {
                Id = 7121,
                Name = "SCRAMBLE Goggles",
                BaseType = ItemType.Flashlight,
                Description = "Toggle with the tool key or flashlight control. Blocks visual SCP-096 targeting for 60 battery seconds; shooting 096 still angers him.",
                RoundCap = 2
            },
            new ItemSpec
            {
                Id = 7122,
                Name = "SCP500-E Volatile Pills",
                BaseType = ItemType.SCP500,
                Description = "Explodes after use, usually killing the user. Rare survival roll; nearby blast respects cover and friendly fire.",
                RoundCap = 1
            }
        };
    }
}
