> Detailed owner notes. For staff, use [the quick guide](../STAFF_GUIDE.md) and [content page](../CATALOG.md). Older version notes below are historical; current spawn percentages are explained in [configuration](../CONFIGURATION.md).

# Project Brainless Round Events 0.1.0

Independent `Brainless.RoundEvents.dll` for EXILED 9.15.2 / SCP:SL 14.2.8. The first round mode is **Infection**. Install in EXILED/Plugins and fully restart. No other Project Brainless DLL is required; optional Core can own its updater service. New plugins need an initial manual install.

## Infection

A group of low-health SCP-3114 skeletons hunts the survivors. A human killed directly by an infected skeleton's slap or strangulation respawns as SCP-3114 at the captured death position after one second. Converted skeletons spread infection in the same way. Infected deaths do not respawn them again. Falls, Teslas, warhead/decontamination, poison, explosions, gunfire, suicides, other SCP attacks and kills merely credited to a skeleton do not convert anyone.

The default skeleton has **150 HP and zero Hume Shield**. Starting infected count is the greater of 3 or 20% of living participants, capped at 8 and reduced to leave at least 2 survivors. At least 4 living, connected, non-Overwatch/non-Tutorial participants are required. Spectators are not forcibly recruited.

Survivors receive a fresh Facility Guard role/loadout with 100 HP. Existing physical positions are retained at event setup; a nonphysical role such as SCP-079 uses the new role's normal spawn point. Initial infected are chosen randomly from eligible living players. Custom starter profiles are cleared by the role change. The event is a deliberate round mode, so starting it mid-round replaces existing combat roles/loadouts.

Both direct attack kinds require a native SCP-3114 killing handler and the exact attacking life to belong to this event. A pre-death position is captured, then only a confirmed, uncancelled death can queue conversion. Tickets are one-use and cancelled by disconnect, another respawn/role change, event stop, round reset or plugin shutdown. Ragdolls are removed only after successful conversion; dropped equipment remains. Death-point conversion adds a configurable 0.05m vertical offset and does not send the victim to a generic SCP spawn room.

Ordinary respawn waves and item-based reinforcement/revival attempts are blocked by default. Late joins spectate by default. Humans cannot damage one another, except themselves; hazard damage remains possible. Disguises are disabled by default so infected remain visible skeletons. All these policies have configuration switches. Staff force-class actions remain possible; combat roles spawned during Infection are normalized to a survivor or infected role.

Survivors win by eliminating all infected; infected win when no humans remain. The ordinary round-ending hook waits for pending conversions before deciding a winner. An admin round lock is respected. Other deaths remain dead for this event under the default reinforcement policy.

## Commands and permission

All commands are Remote Admin only and require **`projectbrainless.events`**. Root alias: `bpevents`.

| Command | Action |
| --- | --- |
| `bpevent list` | List implemented event modes |
| `bpevent status` | Show mode, queued selection, living counts and pending conversions |
| `bpevent infection next` | Queue Infection for the next round |
| `bpevent infection start` | Start Infection in the current running round |
| `bpevent stop` | Stop event rules, cancel pending conversions and clear the queue |
| `bpevent reload` | Validate and reload inactive-event settings |

Queue survives the end/waiting phase of the current round and is consumed at the next round start. A failed minimum-player check skips that queued event. `stop` restores owned infected maximum-health/shield limits without healing anyone; current roles/loadouts remain. Restart the round to return to normal roles. Reloading is rejected while an event is active or scheduled.

## Configuration

Under `project_brainless_round_events` or its separated EXILED YAML:

```yaml
is_enabled: true
debug: false
auto_update:
  enabled: true
  token: ""
  token_environment_variable: BRAINLESS_GITHUB_TOKEN
  check_interval_minutes: 60
  include_prereleases: false
  releases_to_scan: 30
automatic_events: false
automatic_event_chance_percent: 10
round_start_delay_seconds: 1.5
infection:
  enabled: true
  minimum_players: 4
  minimum_survivors: 2
  starting_infected_count: 3
  starting_infected_percent: 20
  maximum_starting_infected: 8
  infected_health: 150
  infected_hume_shield: 0
  survivor_role: FacilityGuard
  survivor_health: 100
  respawn_delay_seconds: 1
  spawn_vertical_offset: 0.05
  ending_grace_seconds: 3
  allow_slap_infections: true
  allow_strangulation_infections: true
  allow_disguises: false
  prevent_human_friendly_fire: true
  block_reinforcements: true
  allow_late_join_survivors: false
  remove_converted_ragdoll: true
```

Automatic selection is **off by default**. To use occasional events, enable `automatic_events` and choose the percentage chance for each new round. Queue/start commands work while automatic selection is off. Survivor role must be a Guard or NTF role to keep the survivor faction consistent. For a two-player test, set `minimum_players: 2` and `minimum_survivors: 1`; the starting count is reduced accordingly.

Existing role/item/weapon/voice plugins stay separate. Survivors use ForceClass setup, so Project Brainless Roles does not randomly reroll profiles for event setup. Existing SCP proximity voice can be used by infected. SCP SL Fixes/Core continue ordinary kill/death/round stat tracking. Their event hooks do not need a Round Events assembly reference.

## Validation

Automated checks cover seed counts, direct attack classification against the real game's slap/strangle/skin-steal handlers, pending-ticket deduplication, role/reconnect/reset cancellation, winner decisions, native configuration and independent DLL loading. Live clients are still required to validate death-point placement, strangulation, shield behavior, disguises, wave blocking and the round-end summary. The screenshot updater error is addressed by the shared runtime-safe JSON codec and direct HTTP handler; see [UPDATES.md](https://github.com/Swishhyy/BrainlessProjectPlugins/blob/main/docs/UPDATES.md).
