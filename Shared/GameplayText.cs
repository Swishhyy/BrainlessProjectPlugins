using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Brainless.Internal
{
    internal static class GameplayText
    {
        private static readonly HashSet<string> legacy = new HashSet<string>(StringComparer.Ordinal)
        {
            "120% Hume damage, 70% SCP health damage, 85% human damage.",
            "18% faster; half health and melee damage.",
            "20% more HP, 10% more shield; 8% slower.",
            "25% faster resurrection; lower health and shield; faster shield recovery.",
            "25% longer sprint; 90 HP.",
            "30% longer sprint with no armor. Armor suspends the benefit.",
            "5% faster, 10% faster touch; 15% less health and shield.",
            "50% more HP; 10% slower.",
            "75% HP. Explodes after a confirmed death; warning and blast are configurable.",
            "75% more HP; 15% slower; normal melee damage.",
            "90 HP. Interact to lockpick: 15 seconds, 120-second cooldown. Level 3 excluded.",
            "A bag of real SCP-330 candy; candy count and colors are configurable.",
            "Breach Shotgun and flashbang; six shells to breach an ordinary door.",
            "COM-18 and two flashbangs; limited ammunition.",
            "Calls up to three eligible spectators for your faction after 15 seconds. Limited per round.",
            "Cluster replaces grenade or medkit; less ammo.",
            "Combat armor, 5% speed, less ammo. Heavy armor suspends speed.",
            "Crossvec, heavy armor, stronger badge; less reserve ammo.",
            "Deploy with tool key; two audible scans in this room.",
            "Detonates on surface contact after a short arming delay. Configurable blast and safety fuse.",
            "Door Brace instead of flashbang; less ammo.",
            "Door Jammer keycard instead of flashbang; less ammo.",
            "E11 body; charged beam stops at walls and pierces aligned enemies. Three finite charges, no ordinary reload.",
            "Eight seconds of native SCP smoke. Obscures vision and slows humans 20%; no gas damage. Filter blocks the slow.",
            "Eight-second cloud. Filter or leave it. SCPs are unaffected.",
            "Equip and use the tool key at a door: eight-second hack below access level 3. Cannot bypass hard locks.",
            "Equip this keycard and interact with a closed ordinary door to jam it. Others can remove it.",
            "Event-only SCP-049 conversion; one eligible zombie revive.",
            "Explodes after use, usually killing the user. Rare survival roll; nearby blast respects cover and friendly fire.",
            "Filter and specimen; no medkit or passive SCP resistance.",
            "First detonation launches four real grenades outward. Short fuses and a shared damage cap; no recursive clusters.",
            "Flashbang body releasing native SCP-244 smoke only. No flash, blast, custom slow or damage.",
            "Four seconds to heal 35 HP. Sprinting or damage interrupts.",
            "Heat prototype: short bursts; 12 shots overheat for three seconds. Native cadence pending client validation.",
            "Hold tool key at an ordinary closed door. Others can remove it.",
            "Hold tool key to deploy a short, noisy SCP-079 room jammer.",
            "Jammer and Survey Beacon instead of grenade; less ammo.",
            "Jammer replaces grenade or medkit; less ammo.",
            "Janitor card and flashlight; trade or find a route.",
            "Light armor, chalk, 25% longer sprint; heavier armor suspends stamina.",
            "Logicer body; 16 grenades total, no reload. Small 2.5m blast: up to 45 human / 90 SCP damage. Tap fire every 0.65s.",
            "Logicer body; 16 grenades total, no reload. Small 3.5m blast: up to 70 human / 150 SCP damage. Tap fire every 0.65s.",
            "Logicer body; each bullet launches one grenade. Maximum 16 rounds, no reload. Small configurable blast.",
            "Native SCP-244 smoke. Sustained unfiltered exposure causes corrosion; leave the cloud.",
            "Not SCP-500. Offer it to a zombie; hold tool key to eat it for 80 HP.",
            "Nullifier and foam; weaker against human targets and SCP health.",
            "One Echo Canister; throw it to bluff a route.",
            "One bounce from approved static mesh geometry. 85% direct damage; 50% body damage after bouncing.",
            "One random contraband item.",
            "One weighted random positive, negative or SCP effect. A gamble.",
            "Parent blast plus four real grenades launched outward. Shared damage cap; no recursive clusters.",
            "Rattler trades damage for audible target tags.",
            "Removes you from SCP-096 target lists. Only consumed when targeted.",
            "Research Supervisor access; no medical supply.",
            "Seven seconds of stealth; attacking breaks it. Once per player per round.",
            "Six seconds of synthetic footsteps; no damage or flash.",
            "Six shots in Last Word; one specimen; no medkit.",
            "Six shots total. Cannot reload, unload or expand its magazine.",
            "Six-second floor patch slows humans 10% and SCPs 8%.",
            "Small experimental silhouette; 85 HP; approved candy only.",
            "Survey Beacon checks one room; no medkit.",
            "Three approved candies; painkillers if the allowlist is empty.",
            "Three firing cycles and six shells to breach an ordinary door. 90% human damage.",
            "Three hits in two seconds create a public three-second audible tag. 80% damage.",
            "Three numbered floor markers, visible to everyone for 45 seconds.",
            "Three smaller blasts, shared damage cap; keep clear of the beeps.",
            "Timed speed boost and stamina restoration, followed by exhaustion.",
            "Toggle with the tool key or flashlight control. Blocks visual SCP-096 targeting for 60 battery seconds; shooting 096 still angers him.",
            "Two Field Dressings instead of medkit; less ammo.",
            "Two Field Dressings instead of the medkit; less ammo.",
            "Two medkits and painkillers to share.",
            "Two seconds to gain 12 seconds of custom gas protection.",
            "Zone Manager card and painkillers; 85 HP.",
        };
        private static readonly Regex technical = new Regex(@"\b(config\w*|framework|plugin|prototype|experimental|placeholder|native|validation|pending|serial|registered|allowlist|preset)\b|recursive|static mesh|shared damage cap", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        internal static string Clean(string text, string fallback)
        {
            string clean = Regex.Replace(text ?? "", @"\s+", " ").Trim();
            if (clean.Length == 0 || clean.Length > 110 || legacy.Contains(clean) || technical.IsMatch(clean))
                return fallback ?? "";
            return clean;
        }
    }
}
