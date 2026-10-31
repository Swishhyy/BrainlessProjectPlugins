# Content catalog

Stable IDs map the design document into independent plugins. All values are playtest defaults; see each generated configuration for tuning.

## Roles

| ID | Name | Base role or faction | Specialty and cost |
| --- | --- | --- | --- |
| D01 | Athletic | ClassD | Runs for longer, but has less health. Starts with painkillers. |
| D02 | Janitor | ClassD | Carries a janitor keycard and flashlight. |
| D03 | Child | ClassD | A smaller Class-D with less health and a piece of candy. |
| D04 | Thief | ClassD | Starts with one random stolen item. |
| D05 | Survivor | ClassD | Carries a Zone Manager keycard and painkillers, but has less health. |
| D06 | Locksmith | ClassD | Picks doors below level 3. Lockpicking takes time and has a cooldown. |
| D07 | Noise Complaint | ClassD | Carries an Echo Canister to distract enemies with fake footsteps. |
| S01 | Mad Scientist | Scientist | An armed scientist with a six-shot pistol and an SCP specimen. |
| S02 | Pharmacist | Scientist | Carries extra medkits and painkillers to share. |
| S03 | Chief Scientist | Scientist | Has better research access, but no medical supplies. |
| S04 | Candy Man | Scientist | Carries a bag of SCP-330 candy. |
| S05 | Auditor | Scientist | Uses a Survey Beacon to check a room for humans and SCPs. |
| S06 | Anomaly Handler | Scientist | Carries a gas filter and a specimen that heals zombies. |
| G01 | Guard Captain | FacilityGuard | Leads with a Crossvec, heavy armor and better access. Carries less spare ammo. |
| G02 | New Hire | FacilityGuard | A lightly equipped guard with a pistol and extra flashbangs. |
| G03 | Trained Agent | FacilityGuard | A faster guard with combat armor. Heavy armor removes the speed bonus. |
| G04 | Coward | FacilityGuard | Sprints for longer without armor. Wearing armor removes the bonus. |
| G05 | Quartermaster | FacilityGuard | Carries extra bandages, but less spare ammo. |
| G06 | Maintenance Liaison | FacilityGuard | Carries a Door Jammer keycard, but less spare ammo. |
| M01 | Field Medic | MTF with original rank | Carries extra bandages to heal the squad, but less spare ammo. |
| M02 | Signal Officer | MTF with original rank | Carries a room jammer and a beacon to locate enemies. |
| M03 | Containment Specialist | MTF with original rank | Uses a shield-breaking rifle and slowing foam to contain SCPs. |
| M04 | Breacher | MTF with original rank | Uses a shotgun to breach doors and a flashbang to clear rooms. |
| C01 | Saboteur | Chaos with original subtype | Carries a jammer to disrupt SCP-079, but less spare ammo. |
| C02 | Courier | Chaos with original subtype | A lightly armored runner with extra stamina and trail markers. |
| C03 | Interceptor | Chaos with original subtype | Repeated shots reveal targets with an audible tracking sound. |
| C04 | Demolitionist | Chaos with original subtype | Carries a Cluster Grenade, but less spare ammo. |
| P01 | Surgeon | Scp049 | Revives zombies faster and recovers shields sooner, but has less health. |
| P02 | Brute | Scp049 | A tougher, slower SCP-049 with more health and shields. |
| P03 | Tickler | Scp049 | A faster SCP-049 with quicker attacks, but less health and shields. |
| Z01 | Giant | Scp0492 | A larger, tougher zombie that moves more slowly. |
| Z02 | Muncher | Scp0492 | A small, fast zombie with less health and weaker attacks. |
| Z03 | Armored | Scp0492 | A heavily built zombie with more health, but less speed. |
| Z04 | Explosive | Scp0492 | Explodes after death. Has less health than a normal zombie. |
| Z05 | Doctor | Scp0492 | Becomes a weaker SCP-049 who can revive one zombie. |

Child, Tickler, Muncher and Doctor are experimental. Doctor is excluded from ordinary zombie draws. Candy roles use real SCP-330 candy; an empty allowlist selects registered standard colors.

## Items

| Design ID | Registry ID | Name | Behavior |
| --- | --- | --- | --- |
| I01 | 7101 | Irritant Gas Grenade | Creates thick smoke to block vision. |
| I02 | 7102 | Cluster Grenade | Bursts into smaller grenades that scatter and explode. |
| I03 | 7103 | Containment Foam Canister | Slows anyone walking through the foam. |
| I04 | 7104 | Echo Canister | Plays fake footsteps to distract nearby players. |
| I05 | 7105 | Door Jammer | Use this card on a closed door to jam it briefly. |
| I06 | 7106 | Signal Jammer | Deploy to disrupt SCP-079 in your room. |
| I07 | 7107 | Survey Beacon | Deploy and scan your room for humans and SCPs. |
| I08 | 7108 | Trace Chalk | Leaves numbered markers to help you find your way. |
| I09 | 7109 | Field Dressing | Hold still to bandage yourself and recover health. |
| I11 | 7111 | Filter Inhaler | Protects you from toxic clouds for a short time. |
| I12 | 7112 | Preserved Specimen | Drop it for a zombie to eat and recover health. |
| I13 | 7113 | Impact Grenade | Explodes when it hits a surface. |
| I14 | 7114 | SCP500-A Adrenaline Pills | Boosts your speed and stamina, then leaves you exhausted. |
| I15 | 7115 | SCP500-B Anti-096 Pills | Removes you from SCP-096's target list. |
| I16 | 7116 | SCP500-C Expired Pills | Gives one random helpful or harmful effect. |
| I17 | 7117 | SCP500-D Invisibility Pills | Briefly turns you invisible. Attacking reveals you. |
| I18 | 7118 | Bio Grenade | Releases toxic smoke. Staying inside causes corrosion. |
| I19 | 7119 | Hacking Chip | Use this card to hack doors below level 3. |
| I20 | 7120 | Reinforcement Radio | Calls a small group of reinforcements for your team. |
| I21 | 7121 | SCRAMBLE Goggles | Protects you when looking at SCP-096. Uses battery power. |
| I22 | 7122 | SCP500-E Volatile Pills | Makes you explode. You have a small chance to survive. |

## Weapons

| Design ID | Registry ID | Name | Behavior |
| --- | --- | --- | --- |
| W01 | 7201 | Nullifier Rifle | Deals extra shield damage, but less health damage. |
| W02 | 7202 | Rattler | Repeated hits make your target give off a tracking sound. |
| W03 | 7203 | Breach Shotgun | Repeated close-range shots can break ordinary doors. |
| W04 | 7204 | Redline SMG | Builds heat while firing. Overheating forces a brief cooldown. |
| W05 | 7205 | Last Word | A powerful pistol with six shots and no reload. |
| W06 | 7206 | Ricochet Pistol | Shots can bounce once off solid surfaces. |
| W07 | 7207 | Grenade Launcher | Fires a grenade with each shot. Holds up to 16; cannot reload. |
| W08 | 7208 | Railgun | Charge a piercing energy beam. Limited shots; cannot reload. |

## Initial supply

| Population | Utility items including kits and loot | Custom firearms | Cluster maximum |
| --- | --- | --- | --- |
| Below 12 | 6 | 1 | 0 |
| 12 through 23 | 10 | 1 | 1 |
| 24 or more | 14 | 2 | 2 |

Smoke grenades occupy one slot and create no filter. Guard badges and ordinary coins are outside these utility counts. Limits apply across a whole round, not per wave or per pickup.

Registered item IDs can be used with EXILED CustomItems Remote Admin commands. A missing provider or spent supply budget excludes that role from the draw; another eligible profile can still be selected. Redline and Ricochet require the experimental weapon switch. Admin give/spawn ignores supply caps. The Grenade Launcher uses 16 grenades total and cannot reload from ordinary ammo.
