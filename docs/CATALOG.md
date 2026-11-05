# Project Brainless — What We Have

[Command table](COMMANDS.md) · [Project overview and settings](../README.md)

Current shipped content: **22 items, 5 weapons, 66 role profiles and 3 round events**. Values below are defaults; server settings can differ.

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
| 7123 | Cursed Keycard | Opens every door except the Alpha Warhead, but each door tells the SCPs where you are. Crumbles after 5 doors. | Use it on any door except the Alpha Warhead. Each door it opens pings the SCPs; it crumbles after 5 doors. |
| 7124 | Cursed SCP-500 | 50/50: a super heal (full heal, cures, +50 AHP and regeneration) or nothing at all. | Use it like a normal SCP-500. 50% super heal, 50% nothing. |
| 7125 | Cursed Adrenaline | +60% speed for 10 seconds, then you crash: frozen for 5 seconds and blinded for 3. | Use it like normal adrenaline. Run during the rush, then hide before the crash. |
| 7126 | Monkey's Paw | Flip it to make a wish. You get what you wished for... with a twist. Single use. | Flip it like a coin to make a twisted wish (needs BetterCoinflips). Single use. |
| 7127 | Cursed Medkit | Heals you to full instantly, but each use lowers your max HP by 10. 3 uses. | Use it like a normal medkit: full heal, but -10 max HP each time. 3 uses. |
| 7128 | Cursed Grenade | Double blast radius and damage, but a 1 in 4 chance it goes off in your hand when you pull the pin. | Throw normally for a doubled blast; 1 in 4 pin pulls explode in your hand. |
| 7129 | Cursed Flashbang | Blinds everyone around much longer, but it always blinds you too. | Throw normally; everyone in sight is blinded longer, and so are you. |
| 7130 | Cursed Hat | Invisible for 60 seconds instead of 15, but you're 30% slower while invisible. 2 uses. | Put it on like SCP-268: 60 s invisible, 30% slower. 2 uses. |
| 7131 | Cursed Cola | Four colas' worth of speed at once, but your health drains much faster. | Drink it like SCP-207 for four colas' worth of speed; your health drains fast. |
| 7132 | Cursed Lantern | Throw it to black out your whole zone for 30 seconds, but you're blinded too. | Throw it like SCP-2176: your whole zone blacks out for 30 s and you're blinded. |

## Weapons

| ID | Weapon | What it does | How to use |
| --- | --- | --- | --- |
| 7206 | Ricochet Pistol **(opt-in)** | Shots can bounce once off solid surfaces. | Shoot toward solid geometry for one reflected shot. |
| 7207 | Grenade Launcher | Fires a grenade with each shot. Holds up to 16; cannot reload. | Fire normally; each bullet launches a grenade. No reload. |
| 7209 | Stun Gun **(staff only)** | Staff only. Each hit deals 1 damage and knocks the target down with a fake death for a few seconds. Never kills. | Staff only: ci give 7209. Shoot a target to knock them down with a fake death for a few seconds; it never kills. |
| 7210 | Cursed Pistol | Every hit does double damage, but about 1 in 3 trigger pulls jams and wastes the round. | Shoot normally; every hit does double damage, but about 1 in 3 pulls jams. |
| 7211 | Cursed Shotgun | Huge damage, but every shot knocks you back and drains your stamina. | Shoot normally for huge damage; each shot knocks you back and drains stamina. |

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
| S01 | Mad Scientist | An armed scientist with a revolver, a spare cylinder and a can of SCP-207. | Shoot with the revolver; drink SCP-207 for speed when you need it. | 14.28 | Enabled | — | 0 |
| S02 | Pharmacist | Carries extra medkits and painkillers to share. | Use or share the starting medicine. | 14.28 | Enabled | — | 0 |
| S03 | Head Scientist | Carries a head scientist keycard with improved research access. | Use the head scientist keycard for research access. | 1.42 | Enabled | — | 12 |
| S04 | Candy Man | Carries a bag of SCP-330 candy. | Open the SCP-330 bag and choose candy. | 14.28 | Enabled | — | 0 |
| S05 | Auditor | Carries a Zone Manager keycard for wider access and a radio. | Use the Zone Manager card for wider access; coordinate on the radio. | 14.28 | Enabled | — | 0 |
| S06 | Anomaly Handler | Carries an Irritant Gas Grenade to cover an escape. | Throw the gas grenade to cover an escape. | 14.28 | Enabled | 7101 | 0 |
| G01 | Guard Captain | Leads with a Crossvec, heavy armor and better access. Carries less spare ammo. | Use the Crossvec and upgraded keycard. | 13.33 | Enabled | — | 0 |
| G02 | New Hire | A lightly equipped guard with a pistol and extra flashbangs. | Use the pistol and flashbangs. | 13.33 | Enabled | — | 0 |
| G03 | Trained Agent | A faster guard with combat armor. | Move normally; the speed bonus is automatic. | 13.33 | Enabled | — | 0 |
| G04 | Coward | Sprints for longer without armor. Wearing armor removes the bonus. | Sprint without armor. | 13.33 | Enabled | — | 0 |
| G05 | Quartermaster | Carries an extra medkit and painkillers, but less spare ammo. | Use or share the extra medkit and painkillers. | 13.33 | Enabled | — | 0 |
| G06 | Maintenance Liaison | Carries a Door Jammer keycard, but less spare ammo. | Use the Door Jammer card on a closed door. | 13.33 | Enabled | 7105 | 0 |
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
| L03 | Hyper | Moves a little faster, but gains vigor more slowly. | Move normally; conserve vigor. | 20 | Enabled | — | 0 |
| L04 | Poisonous | Touches poison humans, but has a weaker 200 hume shield and gains vigor more slowly. Medical supplies cure the poison. | Touch humans to poison them. | 20 | Enabled | — | 0 |
| B01 | Raisian | Smaller and faster, but has half the normal health. | Move and blink normally. | 20 | Enabled | — | 0 |
| B02 | Escape's | Kills refresh breakneck speed and its cooldown, but it has much less health. | Kill a human to refresh breakneck speed. | 20 | Enabled | — | 0 |
| B03 | Cocoon | Tougher and slower, with shields that recover more slowly. | Move and blink normally; allow more time for shields to recover. | 20 | Enabled | — | 0 |
| B04 | Boom Boom | Immune to explosions. Explodes after death. | Automatic explosion after death. | 20 | Enabled | — | 0 |
| H01 | Ambusher | Quieter footsteps and faster movement, but less health. | Move normally; quieter footsteps also work while sprinting. | 20 | Enabled | — | 0 |
| H02 | Alpha | Larger and tougher, but slower. | Attack normally. | 20 | Enabled | — | 0 |
| H03 | Mimic | Reuses mimic sounds much faster, but has less health. | Use mimic sound controls; they recharge quickly. | 20 | Enabled | — | 0 |
| H04 | Venomous | Claws and amnestic clouds poison humans. Medical supplies cure it. | Claw humans or place an amnestic cloud. | 20 | Enabled | — | 0 |
| R01 | Anger Issue | Enrages immediately, with 1,400 health. Heals back up to 900 after a few seconds out of combat. | Use the normal rage control when targets are available. | 40 | Enabled | — | 0 |
| R02 | Ill Temperature | Moves faster while calm. | Move normally while calm. | 40 | Enabled | — | 0 |
| C05 | Cowboy | Carries a fully supplied revolver, light armor, stims and painkillers. | Shoot the revolver; use the stims and painkillers normally. | 8 | Enabled | — | 0 |
| S07 | Containment Engineer | Starts just outside HCZ's Micro room with a containment engineer keycard. | Grab the Micro H.I.D. from the room next to you. | 7.14 | Enabled | — | 0 |
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
| M10 | Swordsman | Fights with SCP-1509, with 200 health, more speed and an adrenaline shot to close the gap. | Close in and fight with SCP-1509. | 20 | Enabled | — | 0 |
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
- Rank roles only roll from their listed ranks. Custom humans are capped at custom_human_share_percent (80%) of living humans; reinforcement_specialists_per_wave (0 = no cap) limits MTF/Chaos roles per wave.
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
| SCP nearby voice | Lets enabled SCPs (including SCP-939) speak to nearby living players. SCP-939 is heard from its mimic point while one is placed. The role HUD shows whether it is on. | Tap N or use F1 `.scpchat`; hold the normal voice key to talk. |
| Role HUD | Your role (or the spectated player's) and SCP proximity chat status on the side of the screen. | Fade it in/out with the HUD key (default H) or the Role HUD switch in Settings → Server Specific. |
| Zombie opt-out | Players can skip being revived as SCP-049-2; a spectator takes the spot and keeps the zombie's custom role, or SCP-049 gets +200 HP. | Settings → Server Specific → Zombie Opt-out. Spectators type F1 `.optin` or turn on Auto-fill. |
| SCP fill | If an SCP disconnects or jumps into a pit in the first 200 seconds, another player can take over its SCP, custom role and remaining health. | F1 `.fill` when the broadcast appears. |
| XP and stats | Tracks completed actions and connected playtime. DNT and opt-out erase stats. | F1 `.pbxp`, `.pbxp top` or `.pbprivacy`. |
| Discord stats | Shows your or another linked player’s level, XP and playtime; top 10 and opt-out. | `/pb-stats [user]`, `/pb-playtime [user]`, `/pb-top`, `/pb-optout`. Link first with `.pblink` → `/pb-link`. |
| Captive recruitment | Chaos can recruit cuffed MTF/Guards; MTF can recruit cuffed Chaos. | Escort the captive to the normal surface escape area. |
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
