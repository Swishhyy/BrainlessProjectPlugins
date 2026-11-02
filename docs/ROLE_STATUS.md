# Role availability audit

All 68 profiles have an implementation and compile against the pinned EXILED/game assemblies. This is a code/default-configuration audit, not a live Unity playtest. Use `bprole` to inspect an actual assignment and `bproles [playerID]` for current runtime requirements.

## Shipped defaults

| ID | Role | Chance % | Default status | Required custom equipment | Minimum players |
| --- | --- | --- | --- | --- | --- |
| D01 | Athletic | 8.88 | Enabled; normal spawn roll | None | 0 |
| D02 | Janitor | 8.88 | Enabled; normal spawn roll | None | 0 |
| D03 | Child | 8.88 | Enabled; normal spawn roll | None | 0 |
| D04 | Thief | 8.88 | Enabled; normal spawn roll | None | 0 |
| D05 | Survivor | 8.88 | Enabled; normal spawn roll | None | 0 |
| D06 | Locksmith | 8.88 | Enabled; normal spawn roll | None | 0 |
| S01 | Mad Scientist | 13.79 | Enabled; normal spawn roll | None | 0 |
| S02 | Pharmacist | 13.79 | Enabled; normal spawn roll | None | 0 |
| S03 | Head Scientist | 1.37 | Enabled; normal spawn roll | None | 12 |
| S04 | Candy Man | 13.79 | Enabled; normal spawn roll | None | 0 |
| S05 | Auditor | 13.79 | Enabled; normal spawn roll | None | 0 |
| S06 | Anomaly Handler | 13.79 | Enabled; normal spawn roll | 7101 | 0 |
| G01 | Guard Captain | 12.9 | Enabled; normal spawn roll | None | 0 |
| G02 | New Hire | 12.9 | Enabled; normal spawn roll | None | 0 |
| G03 | Trained Agent | 12.9 | Enabled; normal spawn roll | None | 0 |
| G04 | Coward | 12.9 | Enabled; normal spawn roll | None | 0 |
| G05 | Quartermaster | 12.9 | Enabled; normal spawn roll | None | 0 |
| G06 | Maintenance Liaison | 12.9 | Enabled; normal spawn roll | 7105 | 0 |
| M01 | Field Medic | 10 | Enabled; normal spawn roll | None | 0 |
| M03 | Containment Specialist | 10 | Enabled; normal spawn roll | 7103 | 0 |
| C02 | Courier | 8 | Enabled; normal spawn roll | None | 0 |
| C04 | Demolitionist | 8 | Enabled; normal spawn roll | 7102 | 12 |
| P01 | Surgeon | 26.66 | Enabled; normal spawn roll | None | 0 |
| P02 | Brute | 26.66 | Enabled; normal spawn roll | None | 0 |
| P03 | Tickler | 26.66 | Enabled; normal spawn roll | None | 0 |
| Z01 | Giant | 20 | Enabled; normal spawn roll | None | 0 |
| Z02 | Muncher | 20 | Enabled; normal spawn roll | None | 0 |
| Z03 | Armored | 20 | Enabled; normal spawn roll | None | 0 |
| Z04 | Explosive | 20 | Enabled; normal spawn roll | None | 0 |
| Z05 | Doctor | 25 (event) | Event only: one zombie per round can become the Doctor | None | 12 |
| L01 | Stalker | 20 | Enabled; normal spawn roll | None | 0 |
| L02 | Wormhole | 20 | Enabled; normal spawn roll | None | 0 |
| L03 | Hyper | 20 | Enabled; normal spawn roll | None | 0 |
| L04 | Poisonous | 20 | Enabled; normal spawn roll | None | 0 |
| B01 | Raisian | 20 | Enabled; normal spawn roll | None | 0 |
| B02 | Escape's | 20 | Enabled; normal spawn roll | None | 0 |
| B03 | Cocoon | 20 | Enabled; normal spawn roll | None | 0 |
| B04 | Boom Boom | 20 | Enabled; normal spawn roll | None | 0 |
| H01 | Ambusher | 20 | Enabled; normal spawn roll | None | 0 |
| H02 | Alpha | 20 | Enabled; normal spawn roll | None | 0 |
| H03 | Mimic | 20 | Enabled; normal spawn roll | None | 0 |
| H04 | Venomous | 20 | Enabled; normal spawn roll | None | 0 |
| R01 | Anger Issue | 40 | Enabled; normal spawn roll | None | 0 |
| R02 | Ill Temperature | 40 | Enabled; normal spawn roll | None | 0 |
| C05 | Cowboy | 8 | Enabled; normal spawn roll | None | 0 |
| G07 | Rogue Guard | 2.58 | Enabled; normal spawn roll | None | 0 |
| S07 | Containment Engineer | 6.89 | Enabled; normal spawn roll | None | 0 |
| S08 | Rogue Scientist | 2.75 | Enabled; normal spawn roll | None | 0 |
| D08 | Captured Insurgent | 8.88 | Enabled; normal spawn roll | None | 0 |
| C06 | Juggernaut | 16 | Enabled; normal spawn roll | None | 0 |
| C07 | Squad Leader | 16 | Enabled; normal spawn roll | None | 0 |
| C08 | Scout | 16 | Enabled; normal spawn roll | None | 0 |
| C09 | Desperate | 8 | Enabled; normal spawn roll | None | 0 |
| C10 | Paranoid | 16 | Enabled; normal spawn roll | None | 0 |
| C11 | SCP Handler | 8 | Enabled; normal spawn roll | None | 0 |
| C12 | Sweet Tooth | 8 | Enabled; normal spawn roll | None | 0 |
| M05 | Scout | 20 | Enabled; normal spawn roll | None | 0 |
| M06 | Agent | 20 | Enabled; normal spawn roll | 7101 | 0 |
| M07 | Personnel | 20 | Enabled; normal spawn roll | 7102 | 0 |
| M08 | Leader | 20 | Enabled; normal spawn roll | None | 0 |
| M09 | Explosiphile | 20 | Enabled; normal spawn roll | 7102, 7113 | 0 |
| M10 | Swordsman | 20 | Enabled; normal spawn roll | None | 0 |
| M11 | General | 15 | Enabled; normal spawn roll | 7101, 7102 | 0 |
| M12 | Blubber | 15 | Enabled; normal spawn roll | None | 0 |
| M13 | Juggernaut | 15 | Enabled; normal spawn roll | None | 0 |
| M14 | Squad Leader | 15 | Enabled; normal spawn roll | None | 0 |
| X01 | Resilient | 2 | Enabled; normal spawn roll | None | 0 |
| X02 | Big Boi | 3 | Enabled; normal spawn roll | None | 0 |

## Why a role can be absent

- Each profile has a direct chance_percent. Groups total at most 100 including disabled/experimental entries; unused and blocked shares keep the native role. Defaults share an 80% total per group (two decimals, rounded down); catalog revision 3 raised existing configs to the same 80% once, keeping relative shares.
- Missing/disabled Items or Weapons, spent kit supply, population thresholds, per-round profile caps, living-human specialist limits and reinforcement wave limits can exclude a profile. These restrictions do not redistribute a blocked profile's percentage to another role.
- Head Scientist and Demolitionist require 12 players. MTF/Chaos profiles preserve their matching native rank/subtype.
- Doctor needs enable_doctor_event, its Z05 profile enabled, 12 players and a living SCP-049. Its separate event roll (default 25%, once per round) never enters ordinary zombie draws. It revives like SCP-049, with 35% health, no hume shield, slower movement, attacks and revives.
- Candy roles use real SCP-330 candy; approved_candy can select colors and candy_man_candy_count controls the Candy Man count. Janitor/Athletic/Pharmacist kits use their documented balanced supplies rather than the original Discord brainstorm.
- Rogue Guard/Scientist stay real Guard/Scientist roles but can damage, and be damaged by, everyone including their own side. They spawn separately in Entrance and queue an early CASSIE warning. Containment Engineer starts just outside the HCZ Micro H.I.D. room; map geometry must provide a safe point. Head Scientist reuses S03 with its research-coordinator keycard.
- Rank roles only roll from their listed ranks (for example Scout from MTF Private or Chaos Rifleman). Captured Insurgent rolls from a Class-D spawn and becomes an empty-handed Chaos Conscript in the cells. Custom humans are capped at custom_human_share_percent (80%) of living humans; reinforcement_specialists_per_wave (0 = no cap) limits MTF/Chaos roles per wave.
- Locksmith can pick every keycard tier: 15/30/45-second attempts and 120/180/240-second cooldowns by default. Facility system locks and non-interactable doors keep their native restrictions.
- Native admin force-class changes and escapes do not randomly reroll profiles. Use bprole set <ID> [playerID] for a deliberate admin test. It schedules a fresh native kit and ignores natural chance/population/supply limits, while honoring disabled content and missing providers.

All role commands require brainless.roles and are registered only in Remote Admin. READY in bproles means eligible to roll now, not guaranteed or live-test certified. See PLAYTEST.md for traits, resurrection, client prediction and cleanup checks.

## SCP variants

The SCP-106, SCP-173, SCP-939 and SCP-096 profiles use normal SCP controls. Stalker and Ambusher reduce footsteps; they keep native ambient sounds. Mimic has a short sound cooldown; intercom access was removed because it disconnected players. Escape's kills restart breakneck and clear the cooldown that follows. Poison refreshes one effect, does not stack, and medical supplies cure it. Boom Boom uses its separate explosive_scp173 settings.

Brute, Giant, Muncher, Raisian and Alpha apply their configured model sizes when allow_model_scaling is true. Child, Tickler and Muncher are no longer experimental. Profiles keep the native hume shield unless their shield multiplier changes it; the Doctor never has one.
