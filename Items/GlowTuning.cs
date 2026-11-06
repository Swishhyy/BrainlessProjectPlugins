using System.ComponentModel;
using Brainless.Internal;

namespace Brainless.Items
{
    // Pickup glow: every item (custom or vanilla) has a rarity; Uncommon and up glow in
    // its colour. Items in closed lockers stay dark until opened.
    public sealed class GlowTuning
    {
        public bool Enabled { get; set; } = true;

        [Description("Also make vanilla items glow. Off by default: lighting every pickup on the map costs too much frame rate.")]
        public bool GlowVanillaItems { get; set; }

        [Description("Lowest rarity that glows (Common, Uncommon, Rare, Epic, Legendary, Mythical).")]
        public Rarity MinimumRarity { get; set; } = Rarity.Uncommon;

        [Tune(0, 10)]
        public float Intensity { get; set; } = .6f;

        [Description("Light radius in metres.")]
        [Tune(.1, 10)]
        public float Range { get; set; } = .9f;

        [Tune(-1, 2)]
        public float HeightOffset { get; set; } = .15f;

        [Description("How often glows follow moving pickups and appear/disappear.")]
        [Tune(.1, 5)]
        public float UpdateSeconds { get; set; } = .5f;

        [Description("Most glowing pickups at once (protects client frame rate).")]
        [Tune(0, 1000)]
        public int MaxLights { get; set; } = 150;

        [Description("Show the vanilla item's rarity when it is picked up.")]
        public bool PickupHint { get; set; } = true;

        [Description("Rarity of each vanilla item, shown when it is picked up (and its glow colour with glow_vanilla_items). Spawn rates are the game's own. Unlisted items are Common.")]
        public System.Collections.Generic.Dictionary<ItemType, Rarity> VanillaRarity { get; set; } = DefaultVanilla();

        internal static System.Collections.Generic.Dictionary<ItemType, Rarity> DefaultVanilla()
        {
            var map = new System.Collections.Generic.Dictionary<ItemType, Rarity>();
            void Set(Rarity rarity, params string[] names)
            {
                foreach (string name in names)
                    if (System.Enum.TryParse(name, out ItemType type))
                        map[type] = rarity;
            }

            Set(Rarity.Common, "Coin", "Flashlight", "Painkillers", "Radio", "KeycardJanitor", "KeycardScientist", "Ammo12gauge", "Ammo556x45", "Ammo44cal", "Ammo762x39", "Ammo9x19", "ArmorLight", "Lantern");
            Set(Rarity.Uncommon, "Medkit", "Adrenaline", "GrenadeFlash", "KeycardGuard", "KeycardZoneManager", "KeycardResearchCoordinator", "ArmorCombat", "GunCOM15", "GunCOM18", "GunFSP9");
            Set(Rarity.Rare, "GrenadeHE", "SCP500", "SCP207", "SCP1853", "SCP244a", "SCP244b", "KeycardMTFPrivate", "KeycardMTFOperative", "KeycardContainmentEngineer", "ArmorHeavy", "GunCrossvec", "GunE11SR", "GunAK", "GunA7", "GunShotgun", "GunRevolver", "GunCom45");
            Set(Rarity.Epic, "GunFRMG0", "GunLogicer", "Jailbird", "SCP268", "SCP018", "SCP2176", "SCP330", "SCP1344", "SCP1576", "AntiSCP207", "KeycardChaosInsurgency", "KeycardMTFCaptain", "KeycardFacilityManager");
            Set(Rarity.Legendary, "MicroHID", "KeycardO5", "ParticleDisruptor", "GunSCP127");
            return map;
        }
    }
}
