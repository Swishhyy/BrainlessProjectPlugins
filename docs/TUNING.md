# Balance and configuration

Project Brainless Roles 0.2.3 and Items/Weapons 0.3.1 expose their gameplay settings in YAML. Each is a separate DLL and config section. No shared runtime plugin is required.

## Where to edit and apply

With standard EXILED configs, edit `EXILED/Configs/<port>-config.yml` and find `brainless_items`, `brainless_weapons` or `brainless_roles`. With EXILED's separated config mode, edit `EXILED/Configs/Plugins/<prefix>/<port>.yml` (a plugin's `ConfigPath`). The reload command reads EXILED's configured path and prefix in either mode; the host's file manager may add a parent directory.

Save the file, then use Remote Admin:

| Command | CedMod/EXILED plugin permission |
| --- | --- |
| `bpitems reload` | `brainless.items.configure` |
| `bpweapons reload` | `brainless.weapons.configure` |
| `bprole reload` | `brainless.roles` |

Settings validate before application. Invalid YAML, null tuning sections, nonfinite numbers, unsupported IDs, duplicate names/IDs and unsafe numeric ranges reject the reload with a reason. A missing field uses its shipped default. These commands do not write over your YAML or reload another plugin.

New throws/deployments and new grants use the new tuning. Existing grenade payloads, gas/foam fields, timed devices, charges, finite ammo budgets, spent supply and earned lockpick cooldowns retain their state. Global interaction/filter settings apply on subsequent checks. Role stats already assigned remain until a new assignment; queued corpse blasts retain the captured settings. Reloading a plugin's updater settings restarts its own release check loop.

Item/weapon IDs, native bodies, enabled entries, experimental weapon registration and item keybind ID require a full server restart. To disable a plugin, stop/restart with `is_enabled: false`. Native firearm cadence, penetration, recoil, attachments and the game's model/particle assets retain native constraints. The knobs below control the custom mechanics; they do not replace the native weapon system.

## Grenades: complete default example

Merge this into the existing `brainless_items` section; do not add a second section with the same name. The other generated tuning sections remain in place.

```yaml
brainless_items:
  grenade_balance_revision: 1
  tuning:
    gas_grenade:
      fuse_seconds: 2.5
      duration_seconds: 8
      radius: 4
      human_slow_percent: 20
      scp_slow_percent: 0
      human_damage_per_second: 0
      scp_damage_per_second: 0
      affect_allies: false
      affect_owner: true
      same_room_only: true
      require_line_of_sight: true
      smoke_emitters: 2
    cluster_grenade:
      fuse_seconds: 3
      grenade_count: 4
      parent_radius: 4.5
      parent_human_damage: 90
      parent_scp_damage: 250
      child_fuse_seconds: 1.2
      child_fuse_stagger_seconds: 0.08
      horizontal_speed: 3
      upward_speed: 3
      radius: 4
      human_damage: 70
      scp_damage: 180
      shared_human_damage_cap: 150
      shared_scp_damage_cap: 500
      armor_penetration: 50
      require_line_of_sight: true
      affect_owner: true
    impact_grenade:
      arming_delay_seconds: 0.1
      safety_fuse_seconds: 10
      detonate_on_players: false
      radius: 5
      human_damage: 140
      scp_damage: 350
      armor_penetration: 50
      require_line_of_sight: true
      affect_owner: true
```

Cluster has a controlled parent blast and launches the configured real child grenades; none becomes another cluster. Parent and children share each target's configured raw damage allowance. Zero caps disable damage. Default parent is 90/250 over 4.5m, with four children at 70/180 over 4m and 150/500 total caps. Children launch at 3m/s outward/up and explode after 1.2s plus stagger. Counts support 1-32; all parent/child damage, fuses, spread, radius, caps and penetration are independently editable.

Impact (7113) ignores the thrower's collider and detonates once on a qualifying physical contact after arming. Continued contact with the ground also qualifies after the delay. Player contacts are optional. The safety fuse prevents an airborne projectile from lasting forever; setting it shorter than the arming delay makes it expire by timer first. Native grenade validation/events still apply.

Blast damage is center damage with linear distance falloff; normal friendly-fire, god mode and damage-event rules apply. Custom blasts do not apply native HE burn/deafen/concussion or door damage. Disabling the additional line-of-sight check does not bypass restrictions in native target enumeration or another plugin's denial.

Gas uses native SCP-244 fog with no gas primitives. The custom field applies the configured slow/DPS; all DPS defaults to zero. `smoke_emitters` (1-6) adjusts overlapping native fog density. The visual radius is capped by the installed SCP-244 prefab's maximum diameter even if the custom gameplay radius is larger; keep radius near the default 4m and verify visually before expanding. Walls/rooms also affect native fog. A filter blocks the configured custom slow/damage, not world smoke or ordinary SCP-244. Harmful native cold subeffects from owned clouds are removed on the server while ordinary/forced cold exposure and native immunity remain. Native frost/fog visual cues can remain on clients.

`affect_allies` broadens field slowing; optional custom damage still obeys server friendly-fire rules. `same_room_only` and `require_line_of_sight` control custom field checks, independently of the native fog renderer.

## Every other item

All names below are children of `brainless_items.tuning`. Units are seconds/meters unless specified.

| Section / registry ID | Fields and shipped defaults |
| --- | --- |
| `containment_foam` / 7103 | `fuse_seconds: 2`, `duration_seconds: 6`, `radius: 3`, `human_slow_percent: 10`, `scp_slow_percent: 8`, both `*_damage_per_second: 0`, `affect_allies: false`, `affect_owner: true`, `same_room_only: true`, `require_line_of_sight: true` |
| `echo_canister` / 7104 | `fuse_seconds: 1`, `duration_seconds: 6`, `audio_range: 16`, `audio_volume: 0.65` |
| `door_brace` / 7105 | `deploy_seconds: 2`, `remove_seconds: 3`, `duration_seconds: 8`, `reach: 3` |
| `signal_jammer` / 7106 | `deploy_seconds: 2`, `arming_seconds: 2`, `active_seconds: 6`, `room_cooldown_seconds: 30`, `remove_seconds: 2`, `max_active: 1` |
| `survey_beacon` / 7107 | `deploy_seconds: 2`, `duration_seconds: 45`, `scan_seconds: 3`, `scans: 2`, `remove_seconds: 2` |
| `trace_chalk` / 7108 | `duration_seconds: 45`, `max_markers: 9`; charges come from its item-list entry (default 3) |
| `field_dressing` / 7109 | `use_seconds: 4`, `heal_amount: 35` |
| `filter_inhaler` / 7111 | `use_seconds: 2`, `duration_seconds: 12`, `blocks_slow: true`, `blocks_damage: true` |
| `preserved_specimen` / 7112 | `use_seconds: 4`, `heal_amount: 80`, `reach: 2`, `once_per_player_per_round: true` |
| `interaction` | `device_reach: 3`, `move_tolerance: 0.6`, `cancel_on_damage: true`, `tick_seconds: 0.1` |
| `supply` | `small_lobby_capacity: 6`, `medium_lobby_capacity: 10`, `large_lobby_capacity: 14`, `medium_lobby_players: 12`, `large_lobby_players: 24`, `cluster_min_players: 12`, `small_lobby_clusters: 0`, `medium_lobby_clusters: 1`, `large_lobby_clusters: 2`, `world_loot_count: 2` (extra bundles), `pair_gas_with_filter: true` |

The item list (`brainless_items.items`) controls each entry's `name`, `description`, `enabled`, `round_cap`, `charges` and `weight`; retain its `id` and native `base_type`. Charges support 1-1000. Throwable behavior is per grenade; multiple tool charges apply to reusable tools, not extra thrown projectiles. Text descriptions/hints are operator-editable strings and do not automatically recalculate when you change numeric tuning.

The old Maintenance Pass (7110) is removed even from existing lists. Impact 7113 is appended when `catalog_revision` is missing/0. Set `catalog_revision: 1` when deliberately removing it, or keep an entry with `enabled: false`. A reload does not save migrations back to disk; use EXILED's normal config save or edit the list/revision yourself when making permanent catalog changes.

Round caps and supply apply to natural role kits/world loot. Administrative `ci give`/`ci spawn` grants bypass these limits without an extra force permission. Gas pairs with a filter by default and uses two utility slots; disabling pairing makes it one. Existing spent counts are never refunded by changing capacities. The normal cluster allowance is the smaller of its item cap and the current lobby's configured cluster cap, with the additional minimum-player gate. World-loot count is an upper bound on additional bundles; a paired filter still spends its own supply slot. Valid loose-loot anchors, room caps and remaining supply may produce fewer items. See [NATURAL_LOOT.md](NATURAL_LOOT.md) for replacement and starting-gear settings.

## Explosive zombie (Z04)

```yaml
brainless_roles:
  explosive_zombie:
    warning_seconds: 2
    radius: 3
    human_damage: 80
    scp_damage: 0
    armor_penetration: 50
    require_line_of_sight: true
    require_enemy_kill: false
    trigger_on_environmental_deaths: true
    trigger_on_explosion_deaths: true
    max_per_player_per_round: 0
    max_concurrent_bursts: 0
    show_warning: true
    play_warning_sound: true
    warning_range: 10
```

Only a confirmed death of an assigned Z04 arms a native explosion. Capturing its identity before native role cleanup prevents losing the trait when the player becomes Spectator. A canceled Dying event, role swap or disconnect does not independently trigger one. Default settings allow gun, explosion and environmental deaths. `require_enemy_kill` is an optional additional filter. Zero limits mean unlimited here; positive limits restrict the captured user or simultaneous pending corpse bursts. A zero warning still uses a minimum 0.05s native fuse.

Role list entries retain their editable health/shield/speed/stamina/reserve/scale multipliers, kit lists, spawn weights, population gates and round caps. Chance fields are percentages (0-100). Existing role safety/experimental gates and native constraints still apply. Locksmith uses root `lockpick_duration_seconds: 15` and `lockpick_cooldown_seconds: 120`; level-three exclusion remains mandatory.

## Weapons

`brainless_weapons.weapons` controls each weapon's `name`, `description`, `magazine`, `reserve`, `human_damage`, `scp_damage`, `round_cap`, `weight`, `enabled` and `experimental`. Keep IDs and supported native bodies. Ordinary gun damage values are multipliers. Launcher blast damage is absolute and uses the separate fields below. Magazine 0 leaves a normal weapon's native loaded capacity; positive values set the configured capacity. Last Word and the launcher require 1-255 and use that number as the serial's total finite shot budget. World pickups never recreate reserve ammo.

To make the launcher fire 24 grenades, change only its existing 7207 entry's `magazine: 24`, save and run `bpweapons reload`; grant a new launcher. Existing 16-shot launchers retain their original remaining budget. Neither attachments, transfers, reload nor a config reload refill finite weapons.

| Root field | Default |
| --- | --- |
| `launcher_shot_interval` | 0.65 |
| `launcher_speed` | 18 |
| `launcher_fuse` | 1.8 |
| `launcher_blast_radius` | 3.5 |
| `launcher_human_damage` / `launcher_scp_damage` | 70 / 150 |
| `launcher_armor_penetration` | 50 |
| `launcher_require_line_of_sight` / `launcher_affect_owner` | true / true |

The Logicer body remains mandatory for 7207. These legacy launcher field names remain at the root for compatibility. Validated values are applied directly rather than silently clamped to the old balance defaults. Native shooting cadence may still limit how quickly a shot can be accepted.

| Section under `brainless_weapons.tuning` | Fields and shipped defaults |
| --- | --- |
| `nullifier` | `shield_damage_multiplier: 1.2` |
| `rattler` | `hits_to_tag: 3`, `hit_window_seconds: 2`, `tag_duration_seconds: 3`, `immunity_seconds: 11`, `audio_range: 16`, `audio_volume: 0.65` |
| `breach_shotgun` | `shells_per_cycle: 2`, `cycles_to_breach: 3`, `progress_reset_seconds: 6`, `reach: 2` |
| `redline` | `heat_per_shot: 9`, `max_heat: 100`, `vent_seconds: 3`, `cooling_delay_seconds: 0.6`, `cooling_per_second: 25` |
| `ricochet` | `range: 20`, `bounce_damage_multiplier: 0.5`, `tracer_seconds: 0.12` |
| `supply` | `small_lobby_capacity: 1`, `large_lobby_capacity: 2`, `large_lobby_players: 24`, `world_loot_count: 1` |

Redline/Ricochet remain experimental and disabled by default. Native shotgun firing cycles can already spend more than one shell; `shells_per_cycle` ensures at least that configured expenditure and never refunds native ammo.

## Practical bounds

The validator rejects NaN/infinity and places broad safety bounds on simulation values: cluster children 1-32, gas emitters 1-6, slow percentages 0-100, most durations up to 120s or 3600s for persistent fields/devices, blast radii up to 60m, and blast peaks/caps up to 100000. These are validation limits, not recommended balance values. Rejected settings include their property path and permitted range in the command response. Use a test server for physics, native smoke, audio and balance; the repository's headless checks do not simulate Unity clients.

## Natural distribution

Items/Weapons 0.3.0 add `distribution` for weighted native pickup/locker replacements and occasional human starting gear. The existing `world_loot` switch controls additional scattered bundles only. `distribution.enabled: false` disables all automatic routes for that provider. Config reload changes future spawn decisions but does not rerun initial map distribution, reset per-user awards or refund supply. Full defaults, examples and caps are in [NATURAL_LOOT.md](NATURAL_LOOT.md).

## Explosion fix and older configs

This game build's native explosion constructor returns before setting damage/attacker when armor penetration is zero. Custom explosions now use valid penetration, default 50 to match native HE. Configure `armor_penetration` for Impact/Cluster and `explosive_zombie`, or root `launcher_armor_penetration` for the launcher. Valid range is 1-100; set the damage fields to zero when disabling damage. The force passed to the handler is now zero rather than world coordinates. Additional blast cover checks use the native HE blocker mask, allowing its normal wall/door cover behavior without treating loose pickup/ragdoll bodies as walls.

Under Items/Weapons, `grenade_balance_revision: 0` (including a missing marker) upgrades individual fields that still exactly equal the former stock damage/radius/cap/timing values. Edited values and grenade counts survive. Runtime upgrade sets revision 1 without overwriting the YAML file. When intentionally retaining/restoring old numeric values, save `grenade_balance_revision: 1` in that provider's root section. New cluster parent-blast fields default to the values above; set parent damage to zero to disable that stage. Queued projectiles keep captured tuning. Normal native armor, friendly-fire, god mode, spawn protection and damage-event rules remain.

## Candy roles

Under `brainless_roles`, `candy_man_candy_count: 3` controls the number of actual SCP-330 candies (1-6). `approved_candy: []` uses registered Rainbow/Yellow/Purple/Red/Green/Blue candies. An explicit list selects only those registered colors, including special colors if deliberately listed. No pill fallback is used; unavailable explicit colors do not silently become medicine. Native random seed candy is cleared before filling the bag, preserving the configured count. Save and run `bprole reload`; new assignments use the new count/pool.
