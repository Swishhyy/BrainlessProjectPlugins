# Role availability audit

All 35 profiles have an implementation and compile against the pinned EXILED/game assemblies. This is a code/default-configuration audit, not a live Unity playtest. Use `bprole` to inspect an actual assignment and `bproles [playerID]` for current runtime requirements.

## Shipped defaults

| ID | Role | Default status | Required custom equipment | Minimum players |
| --- | --- | --- | --- | --- |
| D01 | Athletic | Enabled; normal spawn roll | None | 0 |
| D02 | Janitor | Enabled; normal spawn roll | None | 0 |
| D03 | Child | Opt-in: allow_experimental_roles | None | 0 |
| D04 | Thief | Enabled; normal spawn roll | None | 0 |
| D05 | Survivor | Enabled; normal spawn roll | None | 0 |
| D06 | Locksmith | Enabled; normal spawn roll | 7110 | 0 |
| D07 | Noise Complaint | Enabled; normal spawn roll | 7104 | 0 |
| S01 | Mad Scientist | Enabled; normal spawn roll | 7205, 7112 | 0 |
| S02 | Pharmacist | Enabled; normal spawn roll | None | 0 |
| S03 | Chief Scientist | Enabled; normal spawn roll | None | 12 |
| S04 | Candy Man | Enabled; normal spawn roll | None | 0 |
| S05 | Auditor | Enabled; normal spawn roll | 7107 | 0 |
| S06 | Anomaly Handler | Enabled; normal spawn roll | 7111, 7112 | 0 |
| G01 | Guard Captain | Enabled; normal spawn roll | None | 0 |
| G02 | New Hire | Enabled; normal spawn roll | None | 0 |
| G03 | Trained Agent | Enabled; normal spawn roll | None | 0 |
| G04 | Coward | Enabled; normal spawn roll | None | 0 |
| G05 | Quartermaster | Enabled; normal spawn roll | 7109, 7109 | 0 |
| G06 | Maintenance Liaison | Enabled; normal spawn roll | 7105 | 0 |
| M01 | Field Medic | Enabled; normal spawn roll | 7109, 7109 | 0 |
| M02 | Signal Officer | Enabled; normal spawn roll | 7106, 7107 | 0 |
| M03 | Containment Specialist | Enabled; normal spawn roll | 7201, 7103 | 0 |
| M04 | Breacher | Enabled; normal spawn roll | 7203 | 0 |
| C01 | Saboteur | Enabled; normal spawn roll | 7106 | 0 |
| C02 | Courier | Enabled; normal spawn roll | 7108 | 0 |
| C03 | Interceptor | Enabled; normal spawn roll | 7202 | 0 |
| C04 | Demolitionist | Enabled; normal spawn roll | 7102 | 12 |
| P01 | Surgeon | Enabled; normal spawn roll | None | 0 |
| P02 | Brute | Enabled; normal spawn roll | None | 0 |
| P03 | Tickler | Opt-in: allow_experimental_roles | None | 0 |
| Z01 | Giant | Enabled; normal spawn roll | None | 0 |
| Z02 | Muncher | Opt-in: allow_experimental_roles | None | 0 |
| Z03 | Armored | Enabled; normal spawn roll | None | 0 |
| Z04 | Explosive | Enabled; normal spawn roll | None | 0 |
| Z05 | Doctor | Disabled; event only; enable profile + both event/experimental switches | None | 12 |

## Why a role can be absent

- Chance rolls do not assign a profile to every spawn. Defaults are 45% for Class-D/scientists/zombies, 50% for Guards/SCP-049 and 30% for reinforcements.
- Missing/disabled Items or Weapons, spent kit supply, population thresholds, per-round profile caps, living-human specialist limits and reinforcement wave limits can exclude a profile. The selector now filters these before choosing among valid profiles.
- Chief Scientist and Demolitionist require 12 players. Interceptor needs a Chaos subtype with an AK. MTF/Chaos profiles preserve their matching native rank/subtype.
- Child, Tickler and Muncher require the experimental role switch. Doctor also needs its disabled profile enabled and enable_doctor_event; its separate one-percent event roll never enters ordinary zombie draws. Redline and Ricochet are weapon prototypes and do not gate the normal role roster.
- Candy roles use painkillers until approved_candy is configured. Janitor/Athletic/Pharmacist kits use their documented balanced supplies rather than the original Discord brainstorm.
- Native admin force-class changes and escapes do not randomly reroll profiles. Use bprole set <ID> [playerID] for a deliberate admin test. It schedules a fresh native kit and ignores natural chance/population/supply limits, while honoring disabled content and missing providers.

All role commands require brainless.roles and are registered only in Remote Admin. READY in bproles means eligible to roll now, not guaranteed or live-test certified. See PLAYTEST.md for traits, resurrection, client prediction and cleanup checks.
