# Content catalog

Stable IDs map the design document into independent plugins. All values are playtest defaults; see each generated configuration for tuning.

## Roles

| ID | Name | Base role or faction | Specialty and cost |
| --- | --- | --- | --- |
| D01 | Athletic | ClassD | 25% longer sprint; 90 HP. |
| D02 | Janitor | ClassD | Janitor card and flashlight; trade or find a route. |
| D03 | Child | ClassD | Small experimental silhouette; 85 HP; approved candy only. |
| D04 | Thief | ClassD | One random contraband item. |
| D05 | Survivor | ClassD | Zone Manager card and painkillers; 85 HP. |
| D06 | Locksmith | ClassD | One Maintenance Pass; 90 HP. |
| D07 | Noise Complaint | ClassD | One Echo Canister; throw it to bluff a route. |
| S01 | Mad Scientist | Scientist | Six shots in Last Word; one specimen; no medkit. |
| S02 | Pharmacist | Scientist | Two medkits and painkillers to share. |
| S03 | Chief Scientist | Scientist | Research Supervisor access; no medical supply. |
| S04 | Candy Man | Scientist | Three approved candies; painkillers if the allowlist is empty. |
| S05 | Auditor | Scientist | Survey Beacon checks one room; no medkit. |
| S06 | Anomaly Handler | Scientist | Filter and specimen; no medkit or passive SCP resistance. |
| G01 | Guard Captain | FacilityGuard | Crossvec, heavy armor, stronger badge; less reserve ammo. |
| G02 | New Hire | FacilityGuard | COM-18 and two flashbangs; limited ammunition. |
| G03 | Trained Agent | FacilityGuard | Combat armor, 5% speed, less ammo. Heavy armor suspends speed. |
| G04 | Coward | FacilityGuard | 30% longer sprint with no armor. Armor suspends the benefit. |
| G05 | Quartermaster | FacilityGuard | Two Field Dressings instead of the medkit; less ammo. |
| G06 | Maintenance Liaison | FacilityGuard | Door Brace instead of flashbang; less ammo. |
| M01 | Field Medic | MTF with original rank | Two Field Dressings instead of medkit; less ammo. |
| M02 | Signal Officer | MTF with original rank | Jammer and Survey Beacon instead of grenade; less ammo. |
| M03 | Containment Specialist | MTF with original rank | Nullifier and foam; weaker against human targets and SCP health. |
| M04 | Breacher | MTF with original rank | Breach Shotgun and flashbang; six shells to breach an ordinary door. |
| C01 | Saboteur | Chaos with original subtype | Jammer replaces grenade or medkit; less ammo. |
| C02 | Courier | Chaos with original subtype | Light armor, chalk, 25% longer sprint; heavier armor suspends stamina. |
| C03 | Interceptor | Chaos with original subtype | Rattler trades damage for audible target tags. |
| C04 | Demolitionist | Chaos with original subtype | Cluster replaces grenade or medkit; less ammo. |
| P01 | Surgeon | Scp049 | 25% faster resurrection; lower health and shield; faster shield recovery. |
| P02 | Brute | Scp049 | 20% more HP, 10% more shield; 8% slower. |
| P03 | Tickler | Scp049 | 5% faster, 10% faster touch; 15% less health and shield. |
| Z01 | Giant | Scp0492 | 50% more HP; 10% slower. |
| Z02 | Muncher | Scp0492 | 18% faster; half health and melee damage. |
| Z03 | Armored | Scp0492 | 75% more HP; 15% slower; normal melee damage. |
| Z04 | Explosive | Scp0492 | 75% HP. One enemy combat death arms a two-second warning burst. |
| Z05 | Doctor | Scp0492 | Event-only SCP-049 conversion; one eligible zombie revive. |

Child, Tickler, Muncher and Doctor are experimental. Doctor is excluded from ordinary zombie draws. Candy falls back to painkillers until a seasonal allowlist is approved.

## Items

| Design ID | Registry ID | Name | Behavior |
| --- | --- | --- | --- |
| I01 | 7101 | Irritant Gas Grenade | Eight seconds of native SCP smoke. Obscures vision and slows humans 20%; no gas damage. Filter blocks the slow. |
| I02 | 7102 | Cluster Grenade | First detonation launches four real grenades outward. Short fuses and a shared damage cap; no recursive clusters. |
| I03 | 7103 | Containment Foam Canister | Six-second floor patch slows humans 10% and SCPs 8%. |
| I04 | 7104 | Echo Canister | Six seconds of synthetic footsteps; no damage or flash. |
| I05 | 7105 | Door Brace | Hold tool key at an ordinary closed door. Others can remove it. |
| I06 | 7106 | Signal Jammer | Hold tool key to deploy a short, noisy SCP-079 room jammer. |
| I07 | 7107 | Survey Beacon | Deploy with tool key; two audible scans in this room. |
| I08 | 7108 | Trace Chalk | Three numbered floor markers, visible to everyone for 45 seconds. |
| I09 | 7109 | Field Dressing | Four seconds to heal 35 HP. Sprinting or damage interrupts. |
| I10 | 7110 | Maintenance Pass | Reusable maintenance keycard. Containment level 1 with normal door interactions. |
| I11 | 7111 | Filter Inhaler | Two seconds to gain 12 seconds of custom gas protection. |
| I12 | 7112 | Preserved Specimen | Not SCP-500. Offer it to a zombie; hold tool key to eat it for 80 HP. |

## Weapons

| Design ID | Registry ID | Name | Behavior |
| --- | --- | --- | --- |
| W01 | 7201 | Nullifier Rifle | 120% Hume damage, 70% SCP health damage, 85% human damage. |
| W02 | 7202 | Rattler | Three hits in two seconds create a public three-second audible tag. 80% damage. |
| W03 | 7203 | Breach Shotgun | Three firing cycles and six shells to breach an ordinary door. 90% human damage. |
| W04 | 7204 | Redline SMG | Heat prototype: short bursts; 12 shots overheat for three seconds. Native cadence pending client validation. |
| W05 | 7205 | Last Word | Six shots total. Cannot reload, unload or expand its magazine. |
| W06 | 7206 | Ricochet Pistol | One bounce from approved static mesh geometry. 85% direct damage; 50% body damage after bouncing. |
| W07 | 7207 | Grenade Launcher | Logicer body; 16 grenades total, no reload. Small 2.5m blast: up to 45 human / 90 SCP damage. Tap fire every 0.65s. |

## Initial supply

| Population | Utility items including kits and loot | Custom firearms | Cluster maximum |
| --- | --- | --- | --- |
| Below 12 | 6 | 1 | 0 |
| 12 through 23 | 10 | 1 | 1 |
| 24 or more | 14 | 2 | 2 |

A gas grenade also creates a filter and consumes two utility slots. Guard badges and ordinary coins are outside these utility counts. Limits apply across a whole round, not per wave or per pickup.

Registered item IDs can be used with EXILED CustomItems Remote Admin commands. A missing provider or spent supply budget excludes that role from the draw; another eligible profile can still be selected. Redline and Ricochet require the experimental weapon switch. Admin give/spawn ignores supply caps. The Grenade Launcher uses 16 grenades total and cannot reload from ordinary ammo.
