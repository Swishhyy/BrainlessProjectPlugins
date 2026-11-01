# Role availability audit

All 53 profiles have an implementation and compile against the pinned EXILED/game assemblies. This is a code/default-configuration audit, not a live Unity playtest. Use `bprole` to inspect an actual assignment and `bproles [playerID]` for current runtime requirements.

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
| S01 | Mad Scientist | 7.75 | Enabled; normal spawn roll | 7205, 7112 | 0 |
| S02 | Pharmacist | 7.75 | Enabled; normal spawn roll | None | 0 |
| S03 | Head Scientist | 0.77 | Enabled; normal spawn roll | None | 12 |
| S04 | Candy Man | 7.75 | Enabled; normal spawn roll | None | 0 |
| S05 | Auditor | 7.75 | Enabled; normal spawn roll | 7107 | 0 |
| S06 | Anomaly Handler | 7.75 | Enabled; normal spawn roll | 7111, 7112 | 0 |
| G01 | Guard Captain | 8.06 | Enabled; normal spawn roll | None | 0 |
| G02 | New Hire | 8.06 | Enabled; normal spawn roll | None | 0 |
| G03 | Trained Agent | 8.06 | Enabled; normal spawn roll | None | 0 |
| G04 | Coward | 8.06 | Enabled; normal spawn roll | None | 0 |
| G05 | Quartermaster | 8.06 | Enabled; normal spawn roll | 7109, 7109 | 0 |
| G06 | Maintenance Liaison | 8.06 | Enabled; normal spawn roll | 7105 | 0 |
| M01 | Field Medic | 7.5 | Enabled; normal spawn roll | 7109, 7109 | 0 |
| M02 | Signal Officer | 7.5 | Enabled; normal spawn roll | 7106, 7107 | 0 |
| M03 | Containment Specialist | 7.5 | Enabled; normal spawn roll | 7201, 7103 | 0 |
| M04 | Breacher | 7.5 | Enabled; normal spawn roll | 7203 | 0 |
| C01 | Saboteur | 6 | Enabled; normal spawn roll | 7106 | 0 |
| C02 | Courier | 6 | Enabled; normal spawn roll | 7108 | 0 |
| C03 | Interceptor | 6 | Enabled; normal spawn roll | 7202 | 0 |
| C04 | Demolitionist | 6 | Enabled; normal spawn roll | 7102 | 12 |
| P01 | Surgeon | 16.66 | Enabled; normal spawn roll | None | 0 |
| P02 | Brute | 16.66 | Enabled; normal spawn roll | None | 0 |
| P03 | Tickler | 16.66 | Opt-in: allow_experimental_roles | None | 0 |
| Z01 | Giant | 11.25 | Enabled; normal spawn roll | None | 0 |
| Z02 | Muncher | 11.25 | Opt-in: allow_experimental_roles | None | 0 |
| Z03 | Armored | 11.25 | Enabled; normal spawn roll | None | 0 |
| Z04 | Explosive | 11.25 | Enabled; normal spawn roll | None | 0 |
| Z05 | Doctor | 1 (event) | Disabled; event only; enable profile + both event/experimental switches | None | 12 |
| L01 | Stalker | 7.5 | Enabled; normal spawn roll | None | 0 |
| L02 | Wormhole | 7.5 | Enabled; normal spawn roll | None | 0 |
| L03 | Hyper | 7.5 | Enabled; normal spawn roll | None | 0 |
| L04 | Poisonous | 7.5 | Enabled; normal spawn roll | None | 0 |
| B01 | Raisian | 7.5 | Enabled; normal spawn roll | None | 0 |
| B02 | Escape's | 7.5 | Enabled; normal spawn roll | None | 0 |
| B03 | Cocoon | 7.5 | Enabled; normal spawn roll | None | 0 |
| B04 | Boom Boom | 7.5 | Enabled; normal spawn roll | None | 0 |
| H01 | Ambusher | 7.5 | Enabled; normal spawn roll | None | 0 |
| H02 | Alpha | 7.5 | Enabled; normal spawn roll | None | 0 |
| H03 | Mimic | 7.5 | Enabled; normal spawn roll | None | 0 |
| H04 | Venomous | 7.5 | Enabled; normal spawn roll | None | 0 |
| R01 | Anger Issue | 15 | Enabled; normal spawn roll | None | 0 |
| R02 | Ill Temperature | 15 | Enabled; normal spawn roll | None | 0 |
| C05 | Cowboy | 6 | Enabled; normal spawn roll | None | 0 |
| G07 | Rogue Guard | 1.61 | Enabled; normal spawn roll | None | 0 |
| S07 | Containment Engineer | 3.87 | Enabled; normal spawn roll | None | 0 |
| S08 | Rogue Scientist | 1.55 | Enabled; normal spawn roll | None | 0 |

## Why a role can be absent

- Each profile has a direct chance_percent. Groups total at most 100 including disabled/experimental entries; unused and blocked shares keep the native role. Defaults above are two-decimal allocations of the old nominal family budgets.
- Missing/disabled Items or Weapons, spent kit supply, population thresholds, per-round profile caps, living-human specialist limits and reinforcement wave limits can exclude a profile. These restrictions do not redistribute a blocked profile's percentage to another role.
- Head Scientist and Demolitionist require 12 players. Interceptor needs a Chaos subtype with an AK. MTF/Chaos profiles preserve their matching native rank/subtype.
- Child, Tickler and Muncher require the experimental role switch. Doctor also needs its disabled profile enabled and enable_doctor_event; its separate configurable event roll (default 1%) never enters ordinary zombie draws. Redline and Ricochet are weapon prototypes and do not gate the normal role roster.
- Candy roles use real SCP-330 candy; approved_candy can select colors and candy_man_candy_count controls the Candy Man count. Janitor/Athletic/Pharmacist kits use their documented balanced supplies rather than the original Discord brainstorm.
- Rogue Guard/Scientist are neutral internally, hostile to all factions, and keep their Guard/Scientist appearance. They spawn separately in Entrance and queue an early CASSIE warning. Containment Engineer starts by the HCZ Micro H.I.D.; map geometry must provide a safe point. Head Scientist reuses S03 with its research-coordinator keycard.
- Locksmith can pick every keycard tier: 15/30/45-second attempts and 120/180/240-second cooldowns by default. Facility system locks and non-interactable doors keep their native restrictions.
- Native admin force-class changes and escapes do not randomly reroll profiles. Use bprole set <ID> [playerID] for a deliberate admin test. It schedules a fresh native kit and ignores natural chance/population/supply limits, while honoring disabled content and missing providers.

All role commands require brainless.roles and are registered only in Remote Admin. READY in bproles means eligible to roll now, not guaranteed or live-test certified. See PLAYTEST.md for traits, resurrection, client prediction and cleanup checks.

## SCP variants

The SCP-106, SCP-173, SCP-939 and SCP-096 profiles are now included. Their skills use normal SCP controls. Stalker and Ambusher reduce footsteps; they keep native ambient sounds. Mimic has a short sound cooldown and intercom access; normal intercom distance, mute and timing restrictions apply. Poison refreshes one effect, does not stack, and medical supplies cure it. Boom Boom uses its separate explosive_scp173 settings.

Brute, Giant, Muncher, Raisian and Alpha apply their configured model sizes when allow_model_scaling is true. Child, Tickler and Muncher remain opt-in; Doctor remains disabled/event-only.
