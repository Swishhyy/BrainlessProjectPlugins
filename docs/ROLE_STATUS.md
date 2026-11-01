# Role availability audit

All 35 profiles have an implementation and compile against the pinned EXILED/game assemblies. This is a code/default-configuration audit, not a live Unity playtest. Use `bprole` to inspect an actual assignment and `bproles [playerID]` for current runtime requirements.

## Shipped defaults

| ID | Role | Chance % | Default status | Required custom equipment | Minimum players |
| --- | --- | --- | --- | --- | --- |
| D01 | Athletic | 6.42 | Enabled; normal spawn roll | None | 0 |
| D02 | Janitor | 6.42 | Enabled; normal spawn roll | None | 0 |
| D03 | Child | 6.42 | Opt-in: allow_experimental_roles | None | 0 |
| D04 | Thief | 6.42 | Enabled; normal spawn roll | None | 0 |
| D05 | Survivor | 6.42 | Enabled; normal spawn roll | None | 0 |
| D06 | Locksmith | 6.42 | Enabled; normal spawn roll | None | 0 |
| D07 | Noise Complaint | 6.42 | Enabled; normal spawn roll | 7104 | 0 |
| S01 | Mad Scientist | 8.82 | Enabled; normal spawn roll | 7205, 7112 | 0 |
| S02 | Pharmacist | 8.82 | Enabled; normal spawn roll | None | 0 |
| S03 | Chief Scientist | 0.88 | Enabled; normal spawn roll | None | 12 |
| S04 | Candy Man | 8.82 | Enabled; normal spawn roll | None | 0 |
| S05 | Auditor | 8.82 | Enabled; normal spawn roll | 7107 | 0 |
| S06 | Anomaly Handler | 8.82 | Enabled; normal spawn roll | 7111, 7112 | 0 |
| G01 | Guard Captain | 8.33 | Enabled; normal spawn roll | None | 0 |
| G02 | New Hire | 8.33 | Enabled; normal spawn roll | None | 0 |
| G03 | Trained Agent | 8.33 | Enabled; normal spawn roll | None | 0 |
| G04 | Coward | 8.33 | Enabled; normal spawn roll | None | 0 |
| G05 | Quartermaster | 8.33 | Enabled; normal spawn roll | 7109, 7109 | 0 |
| G06 | Maintenance Liaison | 8.33 | Enabled; normal spawn roll | 7105 | 0 |
| M01 | Field Medic | 7.5 | Enabled; normal spawn roll | 7109, 7109 | 0 |
| M02 | Signal Officer | 7.5 | Enabled; normal spawn roll | 7106, 7107 | 0 |
| M03 | Containment Specialist | 7.5 | Enabled; normal spawn roll | 7201, 7103 | 0 |
| M04 | Breacher | 7.5 | Enabled; normal spawn roll | 7203 | 0 |
| C01 | Saboteur | 7.5 | Enabled; normal spawn roll | 7106 | 0 |
| C02 | Courier | 7.5 | Enabled; normal spawn roll | 7108 | 0 |
| C03 | Interceptor | 7.5 | Enabled; normal spawn roll | 7202 | 0 |
| C04 | Demolitionist | 7.5 | Enabled; normal spawn roll | 7102 | 12 |
| P01 | Surgeon | 16.66 | Enabled; normal spawn roll | None | 0 |
| P02 | Brute | 16.66 | Enabled; normal spawn roll | None | 0 |
| P03 | Tickler | 16.66 | Opt-in: allow_experimental_roles | None | 0 |
| Z01 | Giant | 11.25 | Enabled; normal spawn roll | None | 0 |
| Z02 | Muncher | 11.25 | Opt-in: allow_experimental_roles | None | 0 |
| Z03 | Armored | 11.25 | Enabled; normal spawn roll | None | 0 |
| Z04 | Explosive | 11.25 | Enabled; normal spawn roll | None | 0 |
| Z05 | Doctor | 1 (event) | Disabled; event only; enable profile + both event/experimental switches | None | 12 |

## Why a role can be absent

- Each profile has a direct chance_percent. Groups total at most 100 including disabled/experimental entries; unused and blocked shares keep the native role. Defaults above are two-decimal allocations of the old nominal family budgets.
- Missing/disabled Items or Weapons, spent kit supply, population thresholds, per-round profile caps, living-human specialist limits and reinforcement wave limits can exclude a profile. These restrictions do not redistribute a blocked profile's percentage to another role.
- Chief Scientist and Demolitionist require 12 players. Interceptor needs a Chaos subtype with an AK. MTF/Chaos profiles preserve their matching native rank/subtype.
- Child, Tickler and Muncher require the experimental role switch. Doctor also needs its disabled profile enabled and enable_doctor_event; its separate configurable event roll (default 1%) never enters ordinary zombie draws. Redline and Ricochet are weapon prototypes and do not gate the normal role roster.
- Candy roles use real SCP-330 candy; approved_candy can select colors and candy_man_candy_count controls the Candy Man count. Janitor/Athletic/Pharmacist kits use their documented balanced supplies rather than the original Discord brainstorm.
- Native admin force-class changes and escapes do not randomly reroll profiles. Use bprole set <ID> [playerID] for a deliberate admin test. It schedules a fresh native kit and ignores natural chance/population/supply limits, while honoring disabled content and missing providers.

All role commands require brainless.roles and are registered only in Remote Admin. READY in bproles means eligible to roll now, not guaranteed or live-test certified. See PLAYTEST.md for traits, resurrection, client prediction and cleanup checks.

## Additional Discord concepts not implemented

The 35-profile audit above covers the shipped catalog, not every role in the later Discord screenshot. These 14 additional concepts are not registered, cannot roll and have no `bprole set` IDs yet:

| Native SCP | Missing profiles |
| --- | --- |
| SCP-106 | Stalker, Wormhole, Hyper, Poisonous |
| SCP-173 | Raisian, Escape’s, Cocoon, Boom Boom |
| SCP-939 | Ambusher, Alpha, Mimic, Venomous |
| SCP-096 | Anger Issue, Ill Temperature |

Existing Discord profiles use balanced reworks: Armored adds health rather than melee damage; Bomber is Explosive (Z04) and explodes on death. Brute currently has health/shield/speed traits without a taller model; Giant also uses the normal model size by default. Child, Tickler and Muncher remain opt-in; Doctor is disabled/event-only.
