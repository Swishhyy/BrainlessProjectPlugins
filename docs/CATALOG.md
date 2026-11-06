# Project Brainless — What We Have

[Command table](COMMANDS.md) · [Project overview and settings](../README.md)

Current shipped content: **28 items, 5 weapons, 121 role profiles and 3 round events**. Values below are defaults; server settings can differ.

**Tool key:** Settings → Server Specific → Use custom item or nearby device (default **V**). Hold for timed actions; tap for instant actions. Grenades use normal throw controls.

## Rarity

Every item, weapon and role has a rarity tier, and a higher tier is always stronger. Each loot spot or spawn rolls a tier first, then something of that tier. Per-round limits (human roles, SCP roles and items are counted separately; Items and Weapons share one count):

| Tier | Chance per roll | Human roles | SCP roles | Items |
| --- | --- | --- | --- | --- |
| Common | 40% | no limit | no limit | no limit |
| Uncommon | 25% | no limit | no limit | no limit |
| Rare | 12% | 8 | 2 | 6 |
| Epic | 6% | 4 | 1 | 4 |
| Legendary | 3% | 2 | 1 | 2 |
| Mythical | 1% | 1 | 1 | 1 |

Every spawn gets a custom role (round start, late join, respawns, revives, escapes and staff forceclass): the tier roll always lands, and a full or empty tier moves to the next one down, then up. When an SCP rolls a Mythical role at round start, each other SCP has a 40% chance to become a human with a custom role instead (at least two SCPs remain). Players offered a reroll at spawn (35%) can use F1 `.scpreroll` once per round within 30 seconds; it always gives a different custom role, Rare or lower.

Map loot uses the tier chance per matching pickup (a tier with nothing for that pickup gives nothing); starting gear rolls at 30% of it. Each round also scatters 6/10/14 custom items (by player count) on open floor in random Light, Heavy and Entrance rooms; each floor spot always rolls an item. Cursed items keep their own 5% roll but still count toward the tier limits.

## Items

| ID | Item | Rarity | What it does | How to use |
| --- | --- | --- | --- | --- |
| 7101 | Irritant Gas Grenade | Common | Creates thick smoke to block vision. | Throw normally; use the smoke as cover. |
| 7102 | Cluster Grenade | Uncommon | Bursts into smaller grenades that scatter and explode. | Throw normally; keep clear of the follow-up grenades. |
| 7103 | Containment Foam Canister | Uncommon | Covers the floor around it in SCP-173's sticky tantrum mess. | Throw it where SCPs or enemies will chase you; the floor around it gets sticky. |
| 7105 | Door Jammer | Common | Use this card on a closed door to jam it briefly. | Equip the card and interact with a closed ordinary door. |
| 7113 | Impact Grenade | Common | Explodes when it hits a surface. | Throw normally; it detonates on contact after arming. |
| 7114 | SCP500-A Adrenaline Pills | Uncommon | Boosts your speed and stamina, then leaves you exhausted. | Use the pill normally. |
| 7115 | SCP500-B Anti-096 Pills | Uncommon | Removes you from SCP-096's target list. | Use while SCP-096 is targeting you. |
| 7116 | SCP500-C Expired Pills | Common | Gives one random helpful or harmful effect. | Use the pill; the result is random. |
| 7117 | SCP500-D Invisibility Pills | Rare | Briefly turns you invisible. Attacking reveals you. | Use the pill; attacking ends the effect. |
| 7119 | Hacking Chip | Rare | Use this card to hack doors below level 3. | Equip and interact with an eligible door, or hold the tool key while looking at it. |
| 7120 | Reinforcement Radio | Legendary | Calls a native mini wave for your faction. | Hold the tool key to call a native mini wave for your faction. |
| 7122 | SCP500-E Volatile Pills | Rare | Makes you explode. You have a small chance to survive. | Eat the pill to detonate; usually fatal to the user. |
| 7123 | Cursed Keycard | Epic | Opens every door, locker and generator except the Alpha Warhead, but each one tells the SCPs where you are. Crumbles after 5 uses. | Use it on doors, keycard lockers and generators (not the Alpha Warhead). Each one pings the SCPs; it crumbles after 5 uses. |
| 7124 | Cursed SCP-500 | Epic | 50/50: a super heal (full heal, cures, +50 AHP and regeneration) or nothing at all. | Use it like a normal SCP-500. 50% super heal, 50% nothing. |
| 7125 | Cursed Adrenaline | Epic | +60% speed for 10 seconds, then you crash: frozen for 5 seconds and blinded for 3. | Use it like normal adrenaline. Run during the rush, then hide before the crash. |
| 7126 | Monkey's Paw | Mythical | Flip it to make a wish. You get what you wished for... with a twist. Single use. | Flip it like a coin to make a twisted wish (needs BrainlessCoinflips). Single use. |
| 7127 | Cursed Medkit | Epic | Heals you to full instantly, but each use lowers your max HP by 10. 3 uses. | Use it like a normal medkit: full heal, but -10 max HP each time. 3 uses. |
| 7128 | Cursed Grenade | Epic | Double blast radius and damage, but a 1 in 4 chance it goes off in your hand when you pull the pin. | Throw normally for a doubled blast; 1 in 4 pin pulls explode in your hand. |
| 7129 | Cursed Flashbang | Epic | Blinds everyone around much longer, but it always blinds you too. | Throw normally; everyone in sight is blinded longer, and so are you. |
| 7130 | Cursed Hat | Legendary | Invisible for 60 seconds instead of 15, but you're 30% slower while invisible. 2 uses. | Put it on like SCP-268: 60 s invisible, 30% slower. 2 uses. |
| 7131 | Cursed Cola | Epic | Four colas' worth of speed at once, but your health drains much faster. | Drink it like SCP-207 for four colas' worth of speed; your health drains fast. |
| 7132 | Cursed Lantern | Epic | Throw it to black out your whole zone for 30 seconds, but you're blinded too. | Throw it like SCP-2176: your whole zone blacks out for 30 s and you're blinded. |
| 7133 | Energy Drink | Common | 20 seconds of 10% extra speed and endless stamina. No healing. | Use it like a normal adrenaline shot. |
| 7134 | Healing Grenade | Uncommon | Heals every human within 6 m by 40 health. No flash. | Throw it at hurt teammates; heals everyone nearby. |
| 7135 | Decoy Corpse | Uncommon | Flip it to drop a fake corpse of yourself with your name. | Flip it like a coin to drop a fake corpse of yourself. |
| 7136 | Teleport Grenade | Legendary | Teleports you to where it pops, if that spot is safe. No flash. | Throw it; you teleport to where it pops if the spot is safe. |
| 7137 | Motion Tracker | Rare | Turn it on to track the nearest SCP for 30 seconds. 3 charges. | Turn it on like a radio (or press the tool key) to track the nearest SCP for 30 s. |
| 7138 | Ammo Box | Common | Flip it to refill the reserve ammo for every gun you carry. Single use. | Flip it like a coin to refill your reserve ammo. |

## Weapons

| ID | Weapon | Rarity | What it does | How to use |
| --- | --- | --- | --- | --- |
| 7206 | Ricochet Pistol **(opt-in)** | Rare | Shots can bounce once off solid surfaces. | Shoot toward solid geometry for one reflected shot. |
| 7207 | Grenade Launcher **(staff only)** | Mythical | Staff only. Fires a grenade with each shot. Holds up to 16; cannot reload. | Staff only: ci give 7207. Fire normally; each bullet launches a grenade. No reload. |
| 7209 | Stun Gun | Legendary | Rare. 5 shots, no reload. Each hit deals 1 damage and knocks the target down with a fake death for a few seconds. Never kills. | Rare loot. 5 shots, no reload: shoot a target to knock them down with a fake death for a few seconds; it never kills. |
| 7210 | Cursed Pistol | Epic | Every hit does double damage, but about 1 in 3 trigger pulls jams and wastes the round. | Shoot normally; every hit does double damage, but about 1 in 3 pulls jams. |
| 7211 | Cursed Shotgun | Epic | Huge damage, but every shot knocks you back and drains your stamina. | Shoot normally for huge damage; each shot knocks you back and drains stamina. |

Ricochet requires experimental weapons to be enabled. Give equipment with `ci give <ID> [playerID]`; use `ci list registered` to see what is enabled.

## Roles

Traits apply automatically. Use `bprole set <ID> [playerID]` to test; `bproles` shows percentages and blocked reasons.

| ID | Role | Rarity | What it does | How to use | Status | Custom kit | Min players |
| --- | --- | --- | --- | --- | --- | --- | --- |
| D01 | Athletic | Common | Runs for longer, but has less health. Starts with painkillers. | Sprint normally. | Enabled | — | 0 |
| D03 | Child | Common | A smaller Class-D with less health and a piece of candy. | Use the candy bag; smaller model is opt-in. | Enabled | — | 0 |
| D02 | Janitor | Uncommon | Carries a janitor keycard and flashlight. | Use the starting keycard/flashlight. | Enabled | — | 0 |
| D04 | Thief | Uncommon | Starts with one random stolen item. | Use the random starting item. | Enabled | — | 0 |
| D08 | Captured Insurgent | Uncommon | A captured Chaos Insurgency conscript held in the Class-D cells, with a smuggled COM-15 (one magazine) and painkillers. | Escape the cells with the smuggled COM-15; you are Chaos. | Enabled | — | 0 |
| D05 | Survivor | Rare | Carries a Zone Manager keycard and painkillers, but has less health. | Use the starting keycard/medicine. | Enabled | — | 0 |
| D06 | Locksmith | Epic | Picks any keycard door. Higher access takes longer. | Interact with a denied keycard door and stay nearby; higher access takes longer. | Enabled | — | 0 |
| D09 | Lucky Bastard | Mythical | Mythical luck: 120 health, a Zone Manager keycard, SCP-500, two extra coins and the Monkey's Paw. | Use the keycard and SCP-500; flip the Monkey's Paw for a twisted wish. | Enabled | 7126 | 0 |
| S02 | Pharmacist | Common | Carries extra medkits and painkillers to share. | Use or share the starting medicine. | Enabled | — | 0 |
| S06 | Anomaly Handler | Common | Carries an Irritant Gas Grenade to cover an escape. | Throw the gas grenade to cover an escape. | Enabled | 7101 | 0 |
| S04 | Candy Man | Uncommon | Carries a bag of SCP-330 candy. | Open the SCP-330 bag and choose candy. | Enabled | — | 0 |
| S05 | Auditor | Uncommon | Carries a Zone Manager keycard for wider access and a radio. | Use the Zone Manager card for wider access; coordinate on the radio. | Enabled | — | 0 |
| S01 | Mad Scientist | Rare | An armed scientist with a revolver, a spare cylinder and a can of SCP-207. | Shoot with the revolver; drink SCP-207 for speed when you need it. | Enabled | — | 0 |
| S03 | Head Scientist | Rare | Carries a head scientist keycard with improved research access. | Use the head scientist keycard for research access. | Enabled | — | 12 |
| S07 | Containment Engineer | Epic | Starts just outside HCZ's Micro room with a containment engineer keycard. | Grab the Micro H.I.D. from the room next to you. | Enabled | — | 0 |
| S09 | Site Director | Mythical | Runs the site: 120 health, a Facility Manager keycard, a COM-18, a radio and SCP-500. | Open almost anything with the Facility Manager keycard; call for help on the radio. | Enabled | — | 0 |
| G02 | New Hire | Common | A lightly equipped guard with a pistol and extra flashbangs. | Use the pistol and flashbangs. | Enabled | — | 0 |
| G04 | Coward | Common | Sprints for longer without armor. Wearing armor removes the bonus. | Sprint without armor. | Enabled | — | 0 |
| G06 | Maintenance Liaison | Common | Carries a Door Jammer keycard, but less spare ammo. | Use the Door Jammer card on a closed door. | Enabled | 7105 | 0 |
| G05 | Quartermaster | Uncommon | Carries an extra medkit and painkillers, but less spare ammo. | Use or share the extra medkit and painkillers. | Enabled | — | 0 |
| G03 | Trained Agent | Rare | A faster guard with combat armor. | Move normally; the speed bonus is automatic. | Enabled | — | 0 |
| G01 | Guard Captain | Epic | Leads with a Crossvec, heavy armor and better access. Carries less spare ammo. | Use the Crossvec and upgraded keycard. | Enabled | — | 0 |
| G08 | Riot Officer | Legendary | 130 health, a shotgun and heavy armor, but a little slower. | Hold chokepoints with the shotgun and heavy armor. | Enabled | — | 0 |
| G09 | Head of Security | Mythical | Mythical: 150 health, an E-11 rifle with full ammo, heavy armor, an MTF Captain keycard and SCP-500. | Lead security with the E-11 and Captain keycard; SCP-500 for emergencies. | Enabled | — | 0 |
| X04 | Lightweight | Common | Any human. Smaller and 10% faster, with 85% health. | Use your speed to get away. | Enabled | — | 0 |
| X02 | Big Boi | Uncommon | Any human. Wider, with 50% more health but slower. Medical items heal you more. | Use medical items for bonus healing; you are tougher but slower. | Enabled | — | 0 |
| X05 | Fast Healer | Uncommon | Any human. Slowly heals back up to 75 health after 8 seconds out of combat. | Break off from fights to heal up. | Enabled | — | 0 |
| X01 | Resilient | Rare | Any human. The first time you drop to 25% health you get a burst of speed, extra damage and artificial health. | Keep fighting when you drop low; your last stand makes you faster and hit harder. | Enabled | — | 0 |
| X06 | Glass Cannon | Rare | Any human. Deals 25% more damage, but has 75% health. | Hit first; you deal more damage but break easily. | Enabled | — | 0 |
| X07 | Second Wind | Legendary | Any human. Once per life, a killing blow leaves you at 1 health with 3 seconds of invulnerability and a burst of speed. | Fight on; your first killing blow leaves you at 1 HP and invulnerable for 3 seconds. | Enabled | — | 0 |
| X03 | Chosen One | Mythical | Any human. Mythical: 150 health, 10% faster, slowly heals out of combat, and starts with SCP-500. | Fight carefully; you heal slowly out of combat and carry SCP-500. | Enabled | — | 0 |
| C02 | Courier | Common | A lightly armored runner with extra stamina and an adrenaline shot. | Sprint between objectives; use adrenaline to keep running. | Enabled | — | 0 |
| C05 | Cowboy | Common | Carries a fully supplied revolver, light armor, stims and painkillers. | Shoot the revolver; use the stims and painkillers normally. | Enabled | — | 0 |
| C12 | Sweet Tooth | Common | Carries a bag of SCP-330 candy. | Eat or share the SCP-330 candy. | Enabled | — | 0 |
| M01 | Field Medic | Common | Carries an extra medkit and adrenaline for the squad, but less spare ammo. | Heal the squad with the extra medkit; use adrenaline in a pinch. | Enabled | — | 0 |
| C04 | Demolitionist | Uncommon | Carries a Cluster Grenade, but less spare ammo. | Throw the Cluster Grenade. | Enabled | 7102 | 12 |
| C09 | Desperate | Uncommon | Spawns with a Jailbird instead of a gun. | Close the distance and swing the Jailbird. | Enabled | — | 0 |
| M03 | Containment Specialist | Uncommon | Carries containment foam that covers a wide area in SCP-173's sticky mess. | Throw the containment foam in a corridor to leave a wide sticky area. | Enabled | 7103 | 0 |
| C11 | SCP Handler | Rare | Carries one random SCP item. | Use your random SCP item. | Enabled | — | 0 |
| M25 | Nine-Tailed Elite | Mythical | Mythical: 160 health, 10% faster, full ammo, SCP-500, SCP-1853 and adrenaline. | Push with the squad; use SCP-500, SCP-1853 and adrenaline. | Enabled | — | 0 |
| M05 | Scout | Common | Trades the main gun for an FSP-9 and less health for much more speed. | Use your speed to flank and scout. | Enabled | — | 0 |
| M06 | Agent | Uncommon | Carries an Irritant Gas Grenade and a flashbang. | Use the gas grenade for cover and the flashbang to clear rooms. | Enabled | 7101 | 0 |
| M15 | Breacher | Uncommon | 110 health and two extra flashbangs for clearing rooms. | Throw the extra flashbangs before entering a room. | Enabled | — | 0 |
| M07 | Personnel | Rare | Carries a shotgun and a Cluster Grenade. | Push close with the shotgun; throw the Cluster Grenade at groups. | Enabled | 7102 | 0 |
| M16 | Radio Operator | Legendary | 110 health, an adrenaline shot and a Reinforcement Radio to call in backup. | Use the Reinforcement Radio to call in backup. | Enabled | 7120 | 0 |
| M08 | Leader | Common | A Sergeant with 115 health, but no grenade. | Lead the squad; you have more health but no grenade. | Enabled | — | 0 |
| M17 | Marksman | Uncommon | 110 health and twice the spare ammo. | Fight at range; you have twice the spare ammo. | Enabled | — | 0 |
| M09 | Explosiphile | Rare | Carries a Cluster Grenade and an Impact Grenade, with a Crossvec as the main gun. | Throw the Cluster and Impact grenades; fight with the Crossvec. | Enabled | 7102, 7113 | 0 |
| M18 | Combat Engineer | Rare | Trades the frag grenade for a Door Jammer and a Containment Foam Canister. | Jam doors behind you and foam corridors to slow SCPs. | Enabled | 7105, 7103 | 0 |
| M10 | Swordsman | Epic | Fights with SCP-1509, with 200 health, more speed and an adrenaline shot to close the gap. | Close in and fight with SCP-1509. | Enabled | — | 0 |
| M19 | Combat Medic | Common | Carries an extra medkit and painkillers, but less spare ammo. | Heal the squad with the extra medkit and painkillers. | Enabled | — | 0 |
| M20 | Recon | Uncommon | Moves faster and sprints for longer. | Scout ahead; you move faster and sprint longer. | Enabled | — | 0 |
| M21 | Grenadier | Rare | Trades the frag grenade for an Impact Grenade and a Cluster Grenade, with less spare ammo. | Throw the Impact and Cluster grenades. | Enabled | 7113, 7102 | 0 |
| M22 | Heavy Gunner | Epic | 120 health, an FR-MG-0 and heavy armor, but slower. | Suppress with the FR-MG-0; you are slower but armored. | Enabled | — | 0 |
| M23 | Anomaly Specialist | Epic | 110 health and two random SCP items. | Use the two random SCP items. | Enabled | — | 0 |
| M12 | Blubber | Common | Carries far more spare ammo, but moves more slowly. | Keep firing; you carry far more spare ammo but move slowly. | Enabled | — | 0 |
| M11 | General | Uncommon | A Captain with 110 health who trades the frag grenade for an Irritant Gas Grenade and a Cluster Grenade. | Throw the gas grenade for cover and the Cluster Grenade at groups. | Enabled | 7101, 7102 | 0 |
| M13 | Juggernaut | Rare | A Captain with 200 health, but slower. | Soak damage at the front; you are slower. | Enabled | — | 0 |
| M14 | Squad Leader | Epic | Carries a keycard that opens every door except the Alpha Warhead, a COM-15, heavy armor and SCP-1853. | Open almost any door with your keycard; use SCP-1853 before a fight. | Enabled | — | 0 |
| M24 | Commander | Mythical | 150 health, a little faster, SCP-500, a keycard that opens every door except the Alpha Warhead and a Reinforcement Radio. | Lead the squad; use the all-access keycard, SCP-500 and the Reinforcement Radio. | Enabled | 7120 | 0 |
| C15 | Insurgent Warlord | Mythical | Mythical: 160 health, a Logicer with full ammo, heavy armor, a keycard that opens every door except the Alpha Warhead, SCP-500 and a medkit. | Lead the raid with the Logicer; the keycard opens every door except the Alpha Warhead. | Enabled | — | 0 |
| C13 | Smuggler | Epic | Smuggled two random SCP items in, but no armor and a little faster. | Use the two smuggled SCP items; you have no armor. | Enabled | — | 0 |
| C08 | Scout | Common | Trades the AK for an FSP-9 and less health for much more speed. | Use your speed to flank and scout. | Enabled | — | 0 |
| C10 | Paranoid | Uncommon | Slower, but carries heavy armor and twice the spare ammo. May also carry SCP-1344 goggles. | Hold positions with heavy armor and extra ammo; check if you got SCP-1344. | Enabled | — | 0 |
| C14 | Berserker | Epic | 130 health and a Jailbird. Every kill heals 40 health. | Close in with the Jailbird; kills heal you. | Enabled | — | 0 |
| C06 | Juggernaut | Rare | A Repressor with 200 health, but slower. | Soak damage at the front; you are slower. | Enabled | — | 0 |
| C07 | Squad Leader | Epic | Carries a keycard that opens every door except the Alpha Warhead, a COM-15, heavy armor and SCP-1853. | Open almost any door with your keycard; use SCP-1853 before a fight. | Enabled | — | 0 |
| P03 | Tickler | Common | A faster SCP-049 with quicker attacks, but less health and shields. | Attack normally. | Enabled | — | 0 |
| P06 | Mortician | Common | Every zombie it revives comes back as a random zombie variant. | Revive bodies; each zombie becomes a random variant. | Enabled | — | 0 |
| P01 | Surgeon | Uncommon | Revives zombies faster and recovers shields sooner, but has less health. | Use SCP-049's normal revive control. | Enabled | — | 0 |
| P02 | Brute | Rare | A tougher, slower SCP-049 with more health and shields. | Attack normally. | Enabled | — | 0 |
| P05 | Conductor | Rare | Has less health, but the zombies it revives get 25% more health and move 10% faster. | Revive often; your zombies come back stronger. | Enabled | — | 0 |
| P04 | Plague Bearer | Epic | Humans who stay within 5 m are slowly poisoned. Medical supplies cure it. A little more health. | Stay close to humans; your aura poisons them. | Enabled | — | 0 |
| P07 | Necromancer | Mythical | Mythical: 40% more health, more shield, faster revives and attacks, a little faster, and tougher zombies. | Revive and fight; you are tougher and faster at everything. | Enabled | — | 0 |
| Z02 | Muncher | Common | Faster zombie with less health and weaker attacks. | Attack normally. | Enabled | — | 0 |
| Z06 | Sprinter | Common | A very fast zombie with much less health. | Chase down runners; you are fast but fragile. | Enabled | — | 0 |
| Z01 | Giant | Uncommon | A larger, tougher zombie that moves more slowly. | Attack normally. | Enabled | — | 0 |
| Z04 | Explosive | Uncommon | Explodes after death. Has less health than a normal zombie. | Automatic explosion after death. | Enabled | — | 0 |
| Z03 | Armored | Rare | A heavily built zombie with more health, but less speed. | Attack normally. | Enabled | — | 0 |
| Z07 | Spitter | Rare | Hits poison humans. Medical supplies cure it. | Hit humans to poison them. | Enabled | — | 0 |
| Z05 | Doctor | Legendary | A weakened SCP-049 that can revive the dead. Low health, slower, and never has a hume shield. | Revive corpses with the normal SCP-049 controls; stay behind your zombies. | Event: 25% per round | — | 12 |
| Z08 | Hulk | Legendary | A huge zombie with three times the health and harder hits, but slow. | Tank damage and hit hard; you are slow. | Enabled | — | 0 |
| Z09 | Patient Zero | Mythical | Mythical: double health, 15% faster, and every hit poisons the human and heals it 50. | Hit humans to poison them and heal yourself. | Enabled | — | 0 |
| R02 | Ill Temperature | Common | Moves faster while calm. | Move normally while calm. | Enabled | — | 0 |
| R03 | Shy Guy | Common | Enrages faster and moves a little faster while calm, but has less health. | Get looked at; you enrage faster. | Enabled | — | 0 |
| R04 | Prowler | Uncommon | Much faster while calm, but has less health. | Roam quickly while calm. | Enabled | — | 0 |
| R05 | Bulwark | Rare | 40% more health and shield, but slower. | Soak damage; you are slower but tougher. | Enabled | — | 0 |
| R06 | Regenerator | Epic | Heals back to full health a few seconds after leaving combat. | Break off between fights to heal back to full. | Enabled | — | 0 |
| R01 | Anger Issue | Legendary | Enrages immediately, with 1,400 health. Heals back up to 900 after a few seconds out of combat. | Use the normal rage control when targets are available. | Enabled | — | 0 |
| R07 | Wrath | Mythical | Mythical: enrages instantly, 25% more health, faster, and every kill adds 5 seconds of rage. | Chain kills to keep your rage going. | Enabled | — | 0 |
| L01 | Stalker | Common | Quieter footsteps, but less health. | Move normally; quieter footsteps are automatic. | Enabled | — | 0 |
| L03 | Hyper | Common | Moves a little faster, but gains vigor more slowly. | Move normally; conserve vigor. | Enabled | — | 0 |
| L02 | Wormhole | Uncommon | Gains vigor faster, but moves more slowly. | Use the normal stalk/atlas controls. | Enabled | — | 0 |
| L05 | Gravedigger | Uncommon | Has less health, but heals 200 whenever a human dies in the pocket dimension. | Send humans to the pocket dimension; each death there heals you. | Enabled | — | 0 |
| L04 | Poisonous | Rare | Touches poison humans, but has a weaker 200 hume shield and gains vigor more slowly. Medical supplies cure the poison. | Touch humans to poison them. | Enabled | — | 0 |
| L06 | Rust | Rare | Hits make humans bleed and have a 30% chance to destroy their armor. | Hit humans to make them bleed and break their armor. | Enabled | — | 0 |
| L07 | Tar Pit | Epic | Leaves a sticky, slowing puddle behind it every few seconds while moving. | Walk the corridors to leave slowing tar behind you. | Enabled | — | 0 |
| L08 | Glutton | Mythical | Mythical: huge, with 60% more health and more shield, and every catch heals 150. | Catch humans to heal; you are huge and tough. | Enabled | — | 0 |
| B01 | Raisian | Common | Smaller and faster, but has half the normal health. | Move and blink normally. | Enabled | — | 0 |
| B05 | Statue | Common | More health and shield, but a little slower. | Hold your ground; you are tougher but slower. | Enabled | — | 0 |
| B02 | Escape's | Uncommon | Kills refresh breakneck speed and its cooldown, but it has much less health. | Kill a human to refresh breakneck speed. | Enabled | — | 0 |
| B03 | Cocoon | Uncommon | Tougher and slower, with shields that recover more slowly. | Move and blink normally; allow more time for shields to recover. | Enabled | — | 0 |
| B06 | Blinker | Rare | Breakneck Speeds recharges twice as fast. | Use Breakneck Speeds often; it recharges twice as fast. | Enabled | — | 0 |
| B07 | Tantrum | Rare | Its tantrum recharges much faster, and it has a little more health. | Place tantrums often; they recharge much faster. | Enabled | — | 0 |
| B04 | Boom Boom | Epic | Immune to explosions. Explodes after death. | Automatic explosion after death. | Enabled | — | 0 |
| B08 | Ghost | Mythical | Mythical: silent, faster, with 20% more health and shield. Kills refresh Breakneck Speeds. | Sneak up silently; kills refresh Breakneck Speeds. | Enabled | — | 0 |
| H01 | Ambusher | Common | Quieter footsteps and faster movement, but less health. | Move normally; quieter footsteps also work while sprinting. | Enabled | — | 0 |
| H03 | Mimic | Common | Reuses mimic sounds much faster, but has less health. | Use mimic sound controls; they recharge quickly. | Enabled | — | 0 |
| H05 | Runt | Common | Smaller and faster, but has much less health. | Hunt fast; you are small and fragile. | Enabled | — | 0 |
| H02 | Alpha | Uncommon | Larger and tougher, but slower. | Attack normally. | Enabled | — | 0 |
| H04 | Venomous | Rare | Claws and amnestic clouds poison humans. Medical supplies cure it. | Claw humans or place an amnestic cloud. | Enabled | — | 0 |
| H06 | Hunter | Rare | Hits 20% harder and moves a little faster, but has less health. | Strike hard; your hits deal more damage. | Enabled | — | 0 |
| H07 | Matriarch | Legendary | Large and tough with poison clouds, but a little slower. Medical supplies cure the poison. | Use amnestic clouds to poison humans; you are big and tough. | Enabled | — | 0 |
| H08 | Apex | Mythical | Mythical: 50% more health and shield, 10% faster, hits 25% harder, silent, with poison clouds. | Stalk silently and use amnestic clouds; you hit harder and take more punishment. | Enabled | — | 0 |
| K01 | Old Hardware | Common | Power regenerates 20% slower, but gains experience 25% faster. | Play patiently; you level up faster but regenerate power slowly. | Enabled | — | 0 |
| K02 | Watcher | Common | Switching cameras costs 50% less power, but doors cost 25% more. | Watch the facility through cameras; doors cost more. | Enabled | — | 0 |
| K03 | Overclocked | Uncommon | Power regenerates 25% faster, but every action costs 10% more. | Spend power freely; it comes back faster. | Enabled | — | 0 |
| K04 | Electrician | Rare | Blackouts and lockdowns recharge 40% faster. | Use blackouts and lockdowns often. | Enabled | — | 0 |
| K05 | Gatekeeper | Epic | Opening and closing doors costs 50% less power. | Control doors cheaply to trap humans. | Enabled | — | 0 |
| K06 | Head Start | Legendary | Starts at access tier 3. | Use your tier 3 abilities from the start. | Enabled | — | 0 |
| K07 | Mainframe | Mythical | Mythical: starts at tier 2, power regenerates 50% faster and every action costs 25% less. | Use everything from tier 2 with cheap actions and fast power. | Enabled | — | 0 |

Every spawn rolls a custom role by default (guarantee_roles). Model sizes apply when allow_model_scaling is enabled. The extreme Muncher preset is optional. SCP-106/173/939/096 profiles have editable traits under `brainless_roles.roles`. Chances above are shipped defaults; `bproles [playerID]` shows the live values and blocked reasons.

### Why a role can be absent

- With use_rarity (default) each spawn rolls a tier from role_rarity, then a role of that tier for the class; chance_percent only weights roles inside a tier (0 turns a role off). With use_rarity off, each profile has a direct chance_percent and groups total at most 100.
- Missing/disabled Items or Weapons, spent kit supply, population thresholds, per-round profile caps, living-human specialist limits and reinforcement wave limits can exclude a profile. In rarity mode another role of the same tier (or the next tier down) is tried instead.
- Doctor needs enable_doctor_event, its Z05 profile enabled, 12 players and a living SCP-049. Its separate event roll (default 25%, once per round) never enters ordinary zombie draws. It revives like SCP-049, with 35% health, no hume shield, slower movement, attacks and revives.
- Rank roles only roll from their listed ranks. Custom humans are capped at custom_human_share_percent (80%) of living humans; reinforcement_specialists_per_wave (0 = no cap) limits MTF/Chaos roles per wave.
- Locksmith can pick every keycard tier: 15/30/45-second attempts and 120/180/240-second cooldowns by default. Facility system locks still apply.
- Remote Admin forceclass and escapes roll a profile like any spawn; other plugin role changes do not reroll. `bprole set <ID> [playerID]` ignores natural chance/population/supply limits but honors disabled content and missing providers.
- SCP variants use normal SCP controls. Stalker and Ambusher reduce footsteps. Poison refreshes one effect, does not stack, and medical supplies cure it. Brute, Giant, Muncher, Raisian and Alpha apply model sizes only when allow_model_scaling is true. Boom Boom uses its separate explosive_scp173 settings.

## Round events

| Event | What happens | Staff controls |
| --- | --- | --- |
| Infection | Everyone is spread across Heavy Containment and Entrance, survivors in separate rooms away from the skeletons. Low-health SCP-3114 hunt humans; their direct kills convert victims at the death point. Other deaths stay dead. | `bpevent infection next` or `bpevent infection start` |
| Deathmatch | Chaos has taken the facility (spawns in Heavy Containment and Entrance); MTF raids from the surface. No SCPs; Light Containment is locked off. Both sides respawn in waves every 15s; first to 40 kills, or the leader after 15 minutes, wins. | `bpevent deathmatch next` or `bpevent deathmatch start` |
| Juggernaut | One Chaos Juggernaut in heavy armor (1000 HP + 150 per hunter, heals 75 per kill) against NTF hunters. Everyone has SCP-1344 goggle vision, and hunters get a live tracker with the Juggernaut’s distance, direction, room and HP. Light Containment is locked off. Killing the Juggernaut makes you the next one (+3 points); Juggernaut kills are +1. First to 20 points, or the leader after 10 minutes, wins. | `bpevent juggernaut next` or `bpevent juggernaut start` |

Staff commands need four players. Automatic selection is off by default; when enabled it has a 3% chance per round, the lobby then pauses for up to 45 seconds while players vote with F1 `.vote` (or pick No event; anyone can sit the event out with `.vote optout`), never runs two rounds in a row, and needs 12 players for Infection, 16 for Deathmatch and 10 for Juggernaut. Arena events lock the warhead, decontamination and the Light Containment checkpoints/elevators, and block normal respawn waves. `bpevent status` checks progress; `bpevent stop` stops the rules, then restart the round for normal roles.

## Other features

| Feature | What it does | How to use |
| --- | --- | --- |
| SCP nearby voice | Lets enabled SCPs (including SCP-939) speak to nearby living players. SCP-939 is heard from its mimic point while one is placed. The role HUD shows whether it is on. | Tap N or use F1 `.scpchat`; hold the normal voice key to talk. |
| Role HUD | Your role (or the spectated player's) and SCP proximity chat status on the side of the screen. | Fade it in/out with the HUD key (default H) or the Role HUD switch in Settings → Server Specific. |
| Zombie opt-out | Players can skip being revived as SCP-049-2; a spectator takes the spot and keeps the zombie's custom role, or SCP-049 gets +200 HP. | Settings → Server Specific → Zombie Opt-out. Spectators type F1 `.optin` or turn on Auto-fill. |
| SCP fill | If an SCP disconnects or jumps into a pit in the first 200 seconds, another player can take over its SCP, custom role and remaining health. | F1 `.fill` when the broadcast appears. |
| XP and stats | Tracks completed actions and connected playtime. DNT and opt-out erase stats. | F1 `.pbxp`, `.pbxp top` or `.pbprivacy`. |
| Discord stats | Shows your or another linked player’s level, XP and playtime; top 10 and opt-out. | `/pb-stats [user]`, `/pb-playtime [user]`, `/pb-top`, `/pb-optout`. Link first with `.pblink` → `/pb-link`. |
| Captive recruitment | Chaos can recruit cuffed MTF/Guards; MTF can recruit cuffed Chaos. | Escort the captive to the normal surface escape area. |
| Guard escape | An uncuffed Facility Guard who reaches the surface escape area becomes an NTF Sergeant. | Walk to the normal surface escape area. |
| End-of-round friendly fire | When the round ends, friendly fire turns on so everyone can attack everyone until the restart. It is not punished. | Automatic; owners set `end_of_round_friendly_fire` in the Fixes config. |
| BrainlessCoinflips | Gives configured random outcomes from ordinary coin flips. Heads can give a Brainless item or weapon or turn you into a fitting custom role; tails can give a cursed item. Coin class changes roll a custom role for the new class. SCP-914 on Very Fine doubles ordinary coins (1 → 2, 2 → 4). | Equip a normal coin and flip it; put coins through SCP-914 on Very Fine to double them. |
| SCP swap | SCPs can trade SCPs (or take an unplayed one) shortly after round start. Custom variants stay with their SCP, so you take over e.g. Boom Boom. | F1 `.scpswap list`, `.scpswap 173`, then the other SCP answers `.scpswap yes`. Staff: `bpscpswap`. |
| Staff force-class roles | A Remote Admin forceclass rolls a custom role for the new class with the normal chances. | Use forceclass as usual; owners set `force_class_custom_roles`. |
| SCP-914 Rough teleport | Teleports a player to a random valid position. | Stand in SCP-914 and run Rough. |
| Custom loot | Replaces some map pickups and gives occasional starting gear. | Automatic; owner controls per-entry percentages. |
| Automatic updates | Downloads verified updates for installed plugins. | Staff load downloaded updates with a full server restart. |

## SCP-914 crafting

Put the input in SCP-914 and run the listed setting. Recipes are chance-based and share natural supply limits.

| Input | Setting | Possible output |
| --- | --- | --- |
| Flashbang | Fine | Smoke Grenade (7101) |
| HE grenade | Very Fine | Cluster Grenade (7102) |
| HE grenade | Fine | Impact Grenade (7113) |
| SCP-500 | Fine | Adrenaline Pills (7114) |
| SCP-500 | Very Fine | Anti-096 Pills (7115) |
| SCP-500 | Coarse | Expired Pills (7116) |
| SCP-500 | Very Fine | Invisibility Pills (7117) |
| Janitor keycard | Very Fine | Hacking Chip (7119) |
| Radio | Very Fine | Reinforcement Radio (7120) |
| SCP-500 | Rough | Volatile Pills (7122) |

For equipment that will not give: check its ID, enabled state, inventory space and whether the target is alive. For a role that will not roll: read its `bproles` reason.
