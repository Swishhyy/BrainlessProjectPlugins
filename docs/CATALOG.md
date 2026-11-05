# Project Brainless — What We Have

[Command table](COMMANDS.md) · [Project overview and settings](../README.md)

Current shipped content: **12 items, 3 weapons, 68 role profiles and 3 round events**. Values below are defaults; server settings can differ.

**Tool key:** Settings → Server Specific → Use custom item or nearby device (default **V**). Hold for timed actions; tap for instant actions. Grenades use normal throw controls.

## Items

| ID | Item | What it does | How to use |
| --- | --- | --- | --- |
| 7101 | Irritant Gas Grenade | Creates thick smoke to block vision. | Throw normally; use the smoke as cover. |
| 7102 | Cluster Grenade | Bursts into smaller grenades that scatter and explode. | Throw normally; keep clear of the follow-up grenades. |
| 7103 | Containment Foam Canister | Covers the floor around it in SCP-173's sticky tantrum mess. | Throw it where SCPs or enemies will chase you; the floor around it gets sticky. |
| 7105 | Door Jammer | Use this card on a closed door to jam it briefly. | Equip the card and interact with a closed ordinary door. |
| 7113 | Impact Grenade | Explodes when it hits a surface. | Throw normally; it detonates on contact after arming. |
| 7114 | SCP500-A Adrenaline Pills | Boosts your speed and stamina, then leaves you exhausted. | Use the pill normally. |
| 7115 | SCP500-B Anti-096 Pills | Removes you from SCP-096's target list. | Use while SCP-096 is targeting you. |
| 7116 | SCP500-C Expired Pills | Gives one random helpful or harmful effect. | Use the pill; the result is random. |
| 7117 | SCP500-D Invisibility Pills | Briefly turns you invisible. Attacking reveals you. | Use the pill; attacking ends the effect. |
| 7119 | Hacking Chip | Use this card to hack doors below level 3. | Equip and interact with an eligible door, or hold the tool key while looking at it. |
| 7120 | Reinforcement Radio | Calls a native mini wave for your faction. | Hold the tool key to call a native mini wave for your faction. |
| 7122 | SCP500-E Volatile Pills | Makes you explode. You have a small chance to survive. | Eat the pill to detonate; usually fatal to the user. |

## Weapons

| ID | Weapon | What it does | How to use |
| --- | --- | --- | --- |
| 7206 | Ricochet Pistol **(opt-in)** | Shots can bounce once off solid surfaces. | Shoot toward solid geometry for one reflected shot. |
| 7207 | Grenade Launcher | Fires a grenade with each shot. Holds up to 16; cannot reload. | Fire normally; each bullet launches a grenade. No reload. |
| 7209 | Stun Gun | Each hit deals 1 damage and knocks the target down with a fake death for a few seconds. Never kills. | Shoot a target to knock them down with a fake death for a few seconds; it never kills. |

Ricochet requires experimental weapons to be enabled. Give equipment with `ci give <ID> [playerID]`; use `ci list registered` to see what is enabled.

## Roles

Traits apply automatically. Use `bprole set <ID> [playerID]` to test; `bproles` shows percentages and blocked reasons.

| ID | Role | What it does | How to use | Chance % | Status | Custom kit | Min players |
| --- | --- | --- | --- | --- | --- | --- | --- |
| D01 | Athletic | Runs for longer, but has less health. Starts with painkillers. | Sprint normally. | 8.88 | Enabled | — | 0 |
| D02 | Janitor | Carries a janitor keycard and flashlight. | Use the starting keycard/flashlight. | 8.88 | Enabled | — | 0 |
| D03 | Child | A smaller Class-D with less health and a piece of candy. | Use the candy bag; smaller model is opt-in. | 8.88 | Enabled | — | 0 |
| D04 | Thief | Starts with one random stolen item. | Use the random starting item. | 8.88 | Enabled | — | 0 |
| D05 | Survivor | Carries a Zone Manager keycard and painkillers, but has less health. | Use the starting keycard/medicine. | 8.88 | Enabled | — | 0 |
| D06 | Locksmith | Picks any keycard door. Higher access takes longer. | Interact with a denied keycard door and stay nearby; higher access takes longer. | 8.88 | Enabled | — | 0 |
| S01 | Mad Scientist | An armed scientist with a revolver, a spare cylinder and a can of SCP-207. | Shoot with the revolver; drink SCP-207 for speed when you need it. | 13.79 | Enabled | — | 0 |
| S02 | Pharmacist | Carries extra medkits and painkillers to share. | Use or share the starting medicine. | 13.79 | Enabled | — | 0 |
| S03 | Head Scientist | Carries a head scientist keycard with improved research access. | Use the head scientist keycard for research access. | 1.37 | Enabled | — | 12 |
| S04 | Candy Man | Carries a bag of SCP-330 candy. | Open the SCP-330 bag and choose candy. | 13.79 | Enabled | — | 0 |
| S05 | Auditor | Carries a Zone Manager keycard for wider access and a radio. | Use the Zone Manager card for wider access; coordinate on the radio. | 13.79 | Enabled | — | 0 |
| S06 | Anomaly Handler | Carries an Irritant Gas Grenade to cover an escape. | Throw the gas grenade to cover an escape. | 13.79 | Enabled | 7101 | 0 |
| G01 | Guard Captain | Leads with a Crossvec, heavy armor and better access. Carries less spare ammo. | Use the Crossvec and upgraded keycard. | 12.9 | Enabled | — | 0 |
| G02 | New Hire | A lightly equipped guard with a pistol and extra flashbangs. | Use the pistol and flashbangs. | 12.9 | Enabled | — | 0 |
| G03 | Trained Agent | A faster guard with combat armor. Heavy armor removes the speed bonus. | Move normally; heavy armor removes the speed bonus. | 12.9 | Enabled | — | 0 |
| G04 | Coward | Sprints for longer without armor. Wearing armor removes the bonus. | Sprint without armor. | 12.9 | Enabled | — | 0 |
| G05 | Quartermaster | Carries an extra medkit and painkillers, but less spare ammo. | Use or share the extra medkit and painkillers. | 12.9 | Enabled | — | 0 |
| G06 | Maintenance Liaison | Carries a Door Jammer keycard, but less spare ammo. | Use the Door Jammer card on a closed door. | 12.9 | Enabled | 7105 | 0 |
| M01 | Field Medic | Carries an extra medkit and adrenaline for the squad, but less spare ammo. | Heal the squad with the extra medkit; use adrenaline in a pinch. | 10 | Enabled | — | 0 |
| M03 | Containment Specialist | Carries containment foam that covers a wide area in SCP-173's sticky mess. | Throw the containment foam in a corridor to leave a wide sticky area. | 10 | Enabled | 7103 | 0 |
| C02 | Courier | A lightly armored runner with extra stamina and an adrenaline shot. | Sprint between objectives; use adrenaline to keep running. | 8 | Enabled | — | 0 |
| C04 | Demolitionist | Carries a Cluster Grenade, but less spare ammo. | Throw the Cluster Grenade. | 8 | Enabled | 7102 | 12 |
| P01 | Surgeon | Revives zombies faster and recovers shields sooner, but has less health. | Use SCP-049's normal revive control. | 26.66 | Enabled | — | 0 |
| P02 | Brute | A tougher, slower SCP-049 with more health and shields. | Attack normally. | 26.66 | Enabled | — | 0 |
| P03 | Tickler | A faster SCP-049 with quicker attacks, but less health and shields. | Attack normally. | 26.66 | Enabled | — | 0 |
| Z01 | Giant | A larger, tougher zombie that moves more slowly. | Attack normally. | 20 | Enabled | — | 0 |
| Z02 | Muncher | Faster zombie with less health and weaker attacks. | Attack normally. | 20 | Enabled | — | 0 |
| Z03 | Armored | A heavily built zombie with more health, but less speed. | Attack normally. | 20 | Enabled | — | 0 |
| Z04 | Explosive | Explodes after death. Has less health than a normal zombie. | Automatic explosion after death. | 20 | Enabled | — | 0 |
| Z05 | Doctor | A weakened SCP-049 that can revive the dead. Low health, slower, and never has a hume shield. | Revive corpses with the normal SCP-049 controls; stay behind your zombies. | 25 (event) | Event: 25% per round | — | 12 |
| L01 | Stalker | Quieter footsteps, but less health. | Move normally; quieter footsteps are automatic. | 20 | Enabled | — | 0 |
| L02 | Wormhole | Gains vigor faster, but moves more slowly. | Use the normal stalk/atlas controls. | 20 | Enabled | — | 0 |
| L03 | Hyper | Moves faster, but gains vigor more slowly. | Move normally; conserve vigor. | 20 | Enabled | — | 0 |
| L04 | Poisonous | Touches poison humans, but it walks a little slower and gains vigor more slowly. Medical supplies cure the poison. | Touch humans to poison them. | 20 | Enabled | — | 0 |
| B01 | Raisian | Smaller and faster, but has half the normal health. | Move and blink normally. | 20 | Enabled | — | 0 |
| B02 | Escape's | Kills refresh breakneck speed and its cooldown. | Kill a human to refresh breakneck speed. | 20 | Enabled | — | 0 |
| B03 | Cocoon | Tougher and slower, with shields that recover more slowly. | Move and blink normally; allow more time for shields to recover. | 20 | Enabled | — | 0 |
| B04 | Boom Boom | Immune to explosions. Explodes after death. | Automatic explosion after death. | 20 | Enabled | — | 0 |
| H01 | Ambusher | Quieter footsteps and faster movement, but less health. | Move normally; quieter footsteps also work while sprinting. | 20 | Enabled | — | 0 |
| H02 | Alpha | Larger and tougher, but slower. | Attack normally. | 20 | Enabled | — | 0 |
| H03 | Mimic | Reuses mimic sounds much faster, but has less health. | Use mimic sound controls; they recharge quickly. | 20 | Enabled | — | 0 |
| H04 | Venomous | Claws and amnestic clouds poison humans. Medical supplies cure it. | Claw humans or place an amnestic cloud. | 20 | Enabled | — | 0 |
| R01 | Anger Issue | Enrages immediately, with 1,400 health and a 120 hume shield. | Use the normal rage control when targets are available. | 40 | Enabled | — | 0 |
| R02 | Ill Temperature | Moves faster while calm. | Move normally while calm. | 40 | Enabled | — | 0 |
| C05 | Cowboy | Carries a fully supplied revolver, light armor, stims and painkillers. | Shoot the revolver; use the stims and painkillers normally. | 8 | Enabled | — | 0 |
| G07 | Rogue Guard | An unaffiliated guard, hostile to everyone. Can be captured and recruited. | Fight anyone, including other guards; get cuffed and escorted to escape to change sides. | 2.58 | Enabled | — | 0 |
| S07 | Containment Engineer | Starts just outside HCZ's Micro room with a containment engineer keycard. | Grab the Micro H.I.D. from the room next to you. | 6.89 | Enabled | — | 0 |
| S08 | Rogue Scientist | An unaffiliated scientist, hostile to everyone. Can be captured and recruited. | Fight anyone, including other scientists; get cuffed and escorted to escape to change sides. | 2.75 | Enabled | — | 0 |
| D08 | Captured Insurgent | A captured Chaos Insurgency conscript held in the Class-D cells, with no equipment. | Break out of the cells with the Class-D; you have no equipment. | 8.88 | Enabled | — | 0 |
| C06 | Juggernaut | A Repressor with 200 health, but slower. | Soak damage at the front; you are slower. | 16 | Enabled | — | 0 |
| C07 | Squad Leader | Carries a keycard that opens every door except the Alpha Warhead, a COM-15, heavy armor and SCP-1853. | Open almost any door with your keycard; use SCP-1853 before a fight. | 16 | Enabled | — | 0 |
| C08 | Scout | Trades the AK for an FSP-9 and less health for much more speed. | Use your speed to flank and scout. | 16 | Enabled | — | 0 |
| C09 | Desperate | Spawns with a Jailbird instead of a gun. | Close the distance and swing the Jailbird. | 8 | Enabled | — | 0 |
| C10 | Paranoid | Slower, but carries heavy armor and twice the spare ammo. May also carry SCP-1344 goggles. | Hold positions with heavy armor and extra ammo; check if you got SCP-1344. | 16 | Enabled | — | 0 |
| C11 | SCP Handler | Carries one random SCP item. | Use your random SCP item. | 8 | Enabled | — | 0 |
| C12 | Sweet Tooth | Carries a bag of SCP-330 candy. | Eat or share the SCP-330 candy. | 8 | Enabled | — | 0 |
| M05 | Scout | Trades the main gun for an FSP-9 and less health for much more speed. | Use your speed to flank and scout. | 20 | Enabled | — | 0 |
| M06 | Agent | Carries an Irritant Gas Grenade and a flashbang. | Use the gas grenade for cover and the flashbang to clear rooms. | 20 | Enabled | 7101 | 0 |
| M07 | Personnel | Carries a shotgun and a Cluster Grenade. | Push close with the shotgun; throw the Cluster Grenade at groups. | 20 | Enabled | 7102 | 0 |
| M08 | Leader | A Sergeant with 115 health, but no grenade. | Lead the squad; you have more health but no grenade. | 20 | Enabled | — | 0 |
| M09 | Explosiphile | Carries a Cluster Grenade and an Impact Grenade, with a Crossvec as the main gun. | Throw the Cluster and Impact grenades; fight with the Crossvec. | 20 | Enabled | 7102, 7113 | 0 |
| M10 | Swordsman | Fights with SCP-1509, with 150 health and a little more speed. | Close in and fight with SCP-1509. | 20 | Enabled | — | 0 |
| M11 | General | A Captain with 110 health who trades the frag grenade for an Irritant Gas Grenade and a Cluster Grenade. | Throw the gas grenade for cover and the Cluster Grenade at groups. | 15 | Enabled | 7101, 7102 | 0 |
| M12 | Blubber | Carries far more spare ammo, but moves more slowly. | Keep firing; you carry far more spare ammo but move slowly. | 15 | Enabled | — | 0 |
| M13 | Juggernaut | A Captain with 200 health, but slower. | Soak damage at the front; you are slower. | 15 | Enabled | — | 0 |
| M14 | Squad Leader | Carries a keycard that opens every door except the Alpha Warhead, a COM-15, heavy armor and SCP-1853. | Open almost any door with your keycard; use SCP-1853 before a fight. | 15 | Enabled | — | 0 |
| X01 | Resilient | Any human. The first time you drop to 25% health you get a burst of speed, extra damage and artificial health. | Keep fighting when you drop low; your last stand makes you faster and hit harder. | 2 | Enabled | — | 0 |
| X02 | Big Boi | Any human. Wider, with 50% more health but slower. Medical items heal you more. | Use medical items for bonus healing; you are tougher but slower. | 3 | Enabled | — | 0 |

About 80% of each spawn group rolls a custom role by default; the rest keep the plain role. Model sizes apply when allow_model_scaling is enabled. The extreme Muncher preset is optional. SCP-106/173/939/096 profiles have editable traits under `brainless_roles.roles`. Chances above are shipped defaults; `bproles [playerID]` shows the live values and blocked reasons.

### Why a role can be absent

- Each profile has a direct chance_percent. Groups total at most 100 including disabled/experimental entries; unused and blocked shares keep the native role. Defaults share an 80% total per group (two decimals, rounded down).
- Missing/disabled Items or Weapons, spent kit supply, population thresholds, per-round profile caps, living-human specialist limits and reinforcement wave limits can exclude a profile. A blocked profile's percentage is not redistributed to another role.
- Doctor needs enable_doctor_event, its Z05 profile enabled, 12 players and a living SCP-049. Its separate event roll (default 25%, once per round) never enters ordinary zombie draws. It revives like SCP-049, with 35% health, no hume shield, slower movement, attacks and revives.
- Rogue Guard/Scientist can damage, and be damaged by, everyone including their own side. They spawn separately in Entrance and queue an early CASSIE warning. Rank roles only roll from their listed ranks. Custom humans are capped at custom_human_share_percent (80%) of living humans; reinforcement_specialists_per_wave (0 = no cap) limits MTF/Chaos roles per wave.
- Locksmith can pick every keycard tier: 15/30/45-second attempts and 120/180/240-second cooldowns by default. Facility system locks still apply.
- Remote Admin forceclass rolls a profile with the normal chances (force_class_custom_roles); escapes and plugin role changes do not reroll. `bprole set <ID> [playerID]` ignores natural chance/population/supply limits but honors disabled content and missing providers.
- SCP variants use normal SCP controls. Stalker and Ambusher reduce footsteps. Poison refreshes one effect, does not stack, and medical supplies cure it. Brute, Giant, Muncher, Raisian and Alpha apply model sizes only when allow_model_scaling is true. Boom Boom uses its separate explosive_scp173 settings.

## Round events

| Event | What happens | Staff controls |
| --- | --- | --- |
| Infection | Everyone is spread across Heavy Containment and Entrance, survivors in separate rooms away from the skeletons. Low-health SCP-3114 hunt humans; their direct kills convert victims at the death point. Other deaths stay dead. | `bpevent infection next` or `bpevent infection start` |
| Deathmatch | Chaos has taken the facility (spawns in Heavy Containment and Entrance); MTF raids from the surface. No SCPs; Light Containment is locked off. Both sides respawn in waves every 15s; first to 40 kills, or the leader after 15 minutes, wins. | `bpevent deathmatch next` or `bpevent deathmatch start` |
| Juggernaut | One Chaos Juggernaut in heavy armor (1000 HP + 150 per hunter, heals 75 per kill) against NTF hunters. Everyone has SCP-1344 goggle vision, and hunters get a live tracker with the Juggernaut’s distance, direction, room and HP. Light Containment is locked off. Killing the Juggernaut makes you the next one (+3 points); Juggernaut kills are +1. First to 20 points, or the leader after 10 minutes, wins. | `bpevent juggernaut next` or `bpevent juggernaut start` |

Staff commands need four players. Automatic selection is off by default; when enabled it has a 3% chance per round, never runs two rounds in a row, and needs 12 players for Infection, 16 for Deathmatch and 10 for Juggernaut. Arena events lock the warhead, decontamination and the Light Containment checkpoints/elevators, and block normal respawn waves. `bpevent status` checks progress; `bpevent stop` stops the rules, then restart the round for normal roles.

## Other features

| Feature | What it does | How to use |
| --- | --- | --- |
| SCP nearby voice | Lets enabled SCPs speak to nearby living players. | Tap N or use F1 `.scpchat`; hold the normal voice key to talk. |
| XP and stats | Tracks completed actions and connected playtime. DNT and opt-out erase stats. | F1 `.pbxp`, `.pbxp top` or `.pbprivacy`. |
| Discord stats | Shows your or another linked player’s level, XP and playtime; top 10 and opt-out. | `/pb-stats [user]`, `/pb-playtime [user]`, `/pb-top`, `/pb-optout`. Link first with `.pblink` → `/pb-link`. |
| Captive recruitment | Chaos can recruit cuffed MTF/Guards; MTF can recruit cuffed Chaos. Either can recruit Rogues. | Escort the captive to the normal surface escape area. |
| Guard escape | An uncuffed Facility Guard who reaches the surface escape area becomes an NTF Sergeant. | Walk to the normal surface escape area. |
| End-of-round friendly fire | When the round ends, friendly fire turns on so everyone can attack everyone until the restart. It is not punished. | Automatic; owners set `end_of_round_friendly_fire` in the Fixes config. |
| BetterCoinflips | Gives configured random outcomes from ordinary coin flips. Heads can give a Brainless item or weapon or turn you into a fitting custom role; tails can give a cursed item. Coin class changes roll a custom role for the new class. | Equip a normal coin and flip it. |
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
