# Project Brainless — What We Have

[Staff commands](STAFF_GUIDE.md) · [Full command table](COMMANDS.md) · [Owner settings](CONFIGURATION.md)

Current shipped content: **21 items, 8 weapons, 49 role profiles and 1 round event**. Values below are defaults; server settings can differ.

**Tool key:** Settings → Server Specific → Use custom item or nearby device (default **V**). Hold for timed actions; tap for instant actions. Grenades use normal throw controls.

## Items

| ID | Item | What it does | How to use |
| --- | --- | --- | --- |
| 7101 | Irritant Gas Grenade | Creates thick smoke to block vision. | Throw normally; use the smoke as cover. |
| 7102 | Cluster Grenade | Bursts into smaller grenades that scatter and explode. | Throw normally; keep clear of the follow-up grenades. |
| 7103 | Containment Foam Canister | Slows anyone walking through the foam. | Throw at a route you want to slow. |
| 7104 | Echo Canister | Plays fake footsteps to distract nearby players. | Throw to play distracting footsteps. |
| 7105 | Door Jammer | Use this card on a closed door to jam it briefly. | Equip the card and interact with a closed ordinary door. |
| 7106 | Signal Jammer | Deploy to disrupt SCP-079 in your room. | Equip and hold the tool key to deploy in your room. |
| 7107 | Survey Beacon | Deploy and scan your room for humans and SCPs. | Deploy with the tool key; then look at the beacon and hold the tool key to scan. |
| 7108 | Trace Chalk | Leaves numbered markers to help you find your way. | Equip and tap the tool key, or flip the coin, to mark the floor. |
| 7109 | Field Dressing | Hold still to bandage yourself and recover health. | Use like a medkit; stay still while bandaging. |
| 7111 | Filter Inhaler | Protects you from toxic clouds for a short time. | Use like painkillers; wait for the filter hint. |
| 7112 | Preserved Specimen | Drop it for a zombie to eat and recover health. | Drop for a zombie; the zombie holds the tool key nearby to eat it. |
| 7113 | Impact Grenade | Explodes when it hits a surface. | Throw normally; it detonates on contact after arming. |
| 7114 | SCP500-A Adrenaline Pills | Boosts your speed and stamina, then leaves you exhausted. | Use the pill normally. |
| 7115 | SCP500-B Anti-096 Pills | Removes you from SCP-096's target list. | Use while SCP-096 is targeting you. |
| 7116 | SCP500-C Expired Pills | Gives one random helpful or harmful effect. | Use the pill; the result is random. |
| 7117 | SCP500-D Invisibility Pills | Briefly turns you invisible. Attacking reveals you. | Use the pill; attacking ends the effect. |
| 7118 | Bio Grenade | Releases toxic smoke. Staying inside causes corrosion. | Throw normally; leave the toxic cloud. |
| 7119 | Hacking Chip | Use this card to hack doors below level 3. | Equip and interact with an eligible door, or hold the tool key while looking at it. |
| 7120 | Reinforcement Radio | Calls a small group of reinforcements for your team. | Activate the radio/tool key; requires eligible spectators and allowed round timing. |
| 7121 | SCRAMBLE Goggles | Blocks visual SCP-096 targeting while powered; can fail. | Equip and toggle with the flashlight button or tool key; uses battery. |
| 7122 | SCP500-E Volatile Pills | Makes you explode. You have a small chance to survive. | Eat the pill to detonate; usually fatal to the user. |

## Weapons

| ID | Weapon | What it does | How to use |
| --- | --- | --- | --- |
| 7201 | Nullifier Rifle | Deals extra shield damage, but less health damage. | Shoot normally; strongest against Hume Shield. |
| 7202 | Rattler | Repeated hits make your target give off a tracking sound. | Land repeated hits to trigger the tracking sound. |
| 7203 | Breach Shotgun | Repeated close-range shots can break ordinary doors. | Shoot ordinary doors at close range to breach them. |
| 7204 | Redline SMG **(opt-in)** | Builds heat while firing. Overheating forces a brief cooldown. | Fire short bursts; let it cool after overheating. |
| 7205 | Last Word | A powerful pistol with six shots and no reload. | Shoot normally; six total shots and no reload. |
| 7206 | Ricochet Pistol **(opt-in)** | Shots can bounce once off solid surfaces. | Shoot toward solid geometry for one reflected shot. |
| 7207 | Grenade Launcher | Fires a grenade with each shot. Holds up to 16; cannot reload. | Fire normally; each bullet launches a grenade. No reload. |
| 7208 | Railgun | Charge a piercing energy beam. Limited shots; cannot reload. | Press fire to charge; keep equipped and aim until it fires. |

Redline and Ricochet require experimental weapons to be enabled. Give equipment with `ci give <ID> [playerID]`; use `ci list registered` to see what is enabled.

## Roles

Traits apply automatically. Use `bprole set <ID> [playerID]` to test; `bproles` shows percentages and blocked reasons.

| ID | Role | What it does | How to use | Default status |
| --- | --- | --- | --- | --- |
| D01 | Athletic | Runs for longer, but has less health. Starts with painkillers. | Sprint normally. | Enabled |
| D02 | Janitor | Carries a janitor keycard and flashlight. | Use the starting keycard/flashlight. | Enabled |
| D03 | Child | A smaller Class-D with less health and a piece of candy. | Use the candy bag; smaller model is opt-in. | Opt-in |
| D04 | Thief | Starts with one random stolen item. | Use the random starting item. | Enabled |
| D05 | Survivor | Carries a Zone Manager keycard and painkillers, but has less health. | Use the starting keycard/medicine. | Enabled |
| D06 | Locksmith | Picks doors below level 3. Lockpicking takes time and has a cooldown. | Interact with a denied closed door; stay nearby for 15s. Success has a 120s cooldown. | Enabled |
| D07 | Noise Complaint | Carries an Echo Canister to distract enemies with fake footsteps. | Throw the starting Echo Canister. | Enabled |
| S01 | Mad Scientist | An armed scientist with a six-shot pistol and an SCP specimen. | Use the pistol; drop the specimen for a zombie. | Enabled |
| S02 | Pharmacist | Carries extra medkits and painkillers to share. | Use or share the starting medicine. | Enabled |
| S03 | Chief Scientist | Has better research access, but no medical supplies. | Use the improved keycard. | Enabled |
| S04 | Candy Man | Carries a bag of SCP-330 candy. | Open the SCP-330 bag and choose candy. | Enabled |
| S05 | Auditor | Uses a Survey Beacon to check a room for humans and SCPs. | Deploy the beacon, then use it to scan. | Enabled |
| S06 | Anomaly Handler | Carries a gas filter and a specimen that heals zombies. | Use the filter; drop the specimen for a zombie. | Enabled |
| G01 | Guard Captain | Leads with a Crossvec, heavy armor and better access. Carries less spare ammo. | Use the Crossvec and upgraded keycard. | Enabled |
| G02 | New Hire | A lightly equipped guard with a pistol and extra flashbangs. | Use the pistol and flashbangs. | Enabled |
| G03 | Trained Agent | A faster guard with combat armor. Heavy armor removes the speed bonus. | Move normally; heavy armor removes the speed bonus. | Enabled |
| G04 | Coward | Sprints for longer without armor. Wearing armor removes the bonus. | Sprint without armor. | Enabled |
| G05 | Quartermaster | Carries extra bandages, but less spare ammo. | Use or share the bandages. | Enabled |
| G06 | Maintenance Liaison | Carries a Door Jammer keycard, but less spare ammo. | Use the Door Jammer card on a closed door. | Enabled |
| M01 | Field Medic | Carries extra bandages to heal the squad, but less spare ammo. | Use or share bandages. | Enabled |
| M02 | Signal Officer | Carries a room jammer and a beacon to locate enemies. | Deploy the jammer/beacon with the tool key. | Enabled |
| M03 | Containment Specialist | Uses a shield-breaking rifle and slowing foam to contain SCPs. | Shoot shields; throw foam to slow a route. | Enabled |
| M04 | Breacher | Uses a shotgun to breach doors and a flashbang to clear rooms. | Shoot ordinary doors up close to breach. | Enabled |
| C01 | Saboteur | Carries a jammer to disrupt SCP-079, but less spare ammo. | Deploy the jammer with the tool key. | Enabled |
| C02 | Courier | A lightly armored runner with extra stamina and trail markers. | Sprint and place chalk markers. | Enabled |
| C03 | Interceptor | Repeated shots reveal targets with an audible tracking sound. | Land repeated hits with the Rattler. | Enabled |
| C04 | Demolitionist | Carries a Cluster Grenade, but less spare ammo. | Throw the Cluster Grenade. | Enabled |
| P01 | Surgeon | Revives zombies faster and recovers shields sooner, but has less health. | Use SCP-049's normal revive control. | Enabled |
| P02 | Brute | A tougher, slower SCP-049 with more health and shields. | Attack normally. | Enabled |
| P03 | Tickler | A faster SCP-049 with quicker attacks, but less health and shields. | Attack normally. | Opt-in |
| Z01 | Giant | A larger, tougher zombie that moves more slowly. | Attack normally. | Enabled |
| Z02 | Muncher | Faster zombie with less health and weaker attacks. | Attack normally. | Opt-in |
| Z03 | Armored | A heavily built zombie with more health, but less speed. | Attack normally. | Enabled |
| Z04 | Explosive | Explodes after death. Has less health than a normal zombie. | Automatic explosion after death. | Enabled |
| Z05 | Doctor | Becomes a weaker SCP-049 who can revive one zombie. | Use SCP-049's revive control on one eligible zombie. | Off / event only |
| L01 | Stalker | Quieter footsteps, but less health. | Move normally; quieter footsteps are automatic. | Enabled |
| L02 | Wormhole | Gains vigor faster, but moves more slowly. | Use the normal stalk/atlas controls. | Enabled |
| L03 | Hyper | Moves faster, but gains vigor more slowly. | Move normally; conserve vigor. | Enabled |
| L04 | Poisonous | Touches poison humans. Medical supplies cure the poison. | Touch humans to poison them. | Enabled |
| B01 | Raisian | Smaller and faster, but has half the normal health. | Move and blink normally. | Enabled |
| B02 | Escape's | Kills refresh breakneck speed and its cooldown. | Kill a human to refresh breakneck speed. | Enabled |
| B03 | Cocoon | Tougher and slower, with shields that recover more slowly. | Move and blink normally; allow more time for shields to recover. | Enabled |
| B04 | Boom Boom | Immune to explosions. Explodes after death. | Automatic explosion after death. | Enabled |
| H01 | Ambusher | Quieter footsteps and faster movement, but less health. | Move normally; quieter footsteps also work while sprinting. | Enabled |
| H02 | Alpha | Larger and tougher, but slower. | Attack normally. | Enabled |
| H03 | Mimic | Reuses mimic sounds quickly and can use the intercom. | Use mimic sound controls; stand at the intercom microphone and hold the normal voice key. | Enabled |
| H04 | Venomous | Claws and amnestic clouds poison humans. Medical supplies cure it. | Claw humans or place an amnestic cloud. | Enabled |
| R01 | Anger Issue | Enrages immediately, but has less health and shields. | Use the normal rage control when targets are available. | Enabled |
| R02 | Ill Temperature | Moves faster while calm. | Move normally while calm. | Enabled |

Model sizes apply when allow_model_scaling is enabled. The extreme Muncher preset is optional. New SCP-106/173/939/096 profiles have editable traits; see [role settings](CONFIGURATION.md).

## Round events

| Event | What happens | Staff controls |
| --- | --- | --- |
| Infection | Low-health SCP-3114 hunt humans. Their direct kills convert victims at the death point. Other deaths stay dead. | `bpevent infection next` or `bpevent infection start` |

Infection defaults: 150 HP, zero shield, four living players minimum, automatic selection off. `bpevent status` checks it; `bpevent stop` stops rules, then restart the round for normal roles.

## Other features

| Feature | What it does | How to use |
| --- | --- | --- |
| SCP nearby voice | Lets enabled SCPs speak to nearby living players. | Tap N or use F1 `.scpchat`; hold the normal voice key to talk. |
| XP and stats | Tracks completed actions and survival. | F1 `.pbxp` or `.pbxp top`. |
| Discord stats | Links in-game stats to Discord when the bot is enabled. | F1 `.pblink`, then Discord `/pb-link`; use `/pb-stats` or `/pb-top`. |
| BetterCoinflips | Gives configured random outcomes from ordinary coin flips. | Equip a normal coin and flip it. |
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
| Medkit | 1:1 | Field Dressing (7109) |
| SCP-500 | Fine | Adrenaline Pills (7114) |
| SCP-500 | Very Fine | Anti-096 Pills (7115) |
| SCP-500 | Coarse | Expired Pills (7116) |
| SCP-500 | Very Fine | Invisibility Pills (7117) |
| Flashbang | Very Fine | Bio Grenade (7118) |
| Janitor keycard | Very Fine | Hacking Chip (7119) |
| Radio | Very Fine | Reinforcement Radio (7120) |
| Flashlight | Fine | SCRAMBLE Goggles (7121) |
| SCP-500 | Rough | Volatile Pills (7122) |
| E11-SR | Very Fine | Railgun (7208) |

For equipment that will not give: check its ID, enabled state, inventory space and whether the target is alive. For a role that will not roll: read its `bproles` reason.
