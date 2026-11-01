# Project Brainless — Owner Settings

[Staff guide](STAFF_GUIDE.md) · [Content page](CATALOG.md) · [Command table](COMMANDS.md)

## Chance settings

| Content | Edit here | Apply / inspect |
| --- | --- | --- |
| Roles | Each profile’s `chance_percent` under `brainless_roles.roles` | `bprole reload` / `bproles` |
| Item map loot | `brainless_items.distribution.map_rules` | `bpitems reload` / `bpitems chances` |
| Item starting gear | `brainless_items.distribution.player_rules` | Same as above |
| Weapon map loot / starting gear | The matching lists under `brainless_weapons.distribution` | `bpweapons reload` / `bpweapons chances` |

A numeric `chance_percent: 10` means a direct 10% roll for that entry. `0` disables that natural route. Matching totals must be 100 or less; unused or blocked shares keep the normal role/item. No automatic normalization or extra family/global roll is applied.

Map rules share a budget when their **native source item and zones overlap**. Starting-gear rules share a budget for each matching human role, including extra-item and replacement entries. Inventory, enabled state, room/round/population caps and remaining supply still apply. Reloading does not replay the map pass or refill supply.

Example map rules: 10% Cluster + 20% Impact leaves 70% ordinary HE grenades in those zones:

```yaml
map_rules:
- custom_id: 7102
  chance_percent: 10
  source_types: [GrenadeHE]
  zones: [LightContainment, HeavyContainment, Entrance]
- custom_id: 7113
  chance_percent: 20
  source_types: [GrenadeHE]
  zones: [LightContainment, HeavyContainment, Entrance]
```

Edit existing entries to retain your other settings. Replacing a whole list with this example restricts it to those entries. Each item/weapon has an editable entry in nonempty routes; previously unlisted content starts at 0%. An empty route stays empty. In `player_rules`, `roles: []` means any human and `replace_types: []` means an extra item.

Missing/null old percentages derive two-decimal values from old budgets/selection weights. Explicit percentages override those legacy settings. The `weight` on the catalog item itself is carrying weight, not spawn chance. Item/weapon chance fields affect natural map loot, extra drops and starting gear; role kits, admin grants and SCP-914 recipe chances are separate.

## Role traits, HUD and voice

Each role has editable `traits` beside its `chance_percent`. Use the [content page](CATALOG.md) for the 53 role IDs. `allow_model_scaling` controls model size; `explosive_scp173` controls Boom Boom. The HUD uses `role_hud_edge_padding_percent: 0.5`; set `0` for the border.

SCP voice uses `volume: 10`, `full_volume_range: 12` and `voice_range: 16`. Try `bpscpchat volume <0-20>` live; save the chosen `volume` in YAML and run `bpscpchat reload` to retain it. Permission: `brainless.scpchat.configure`.

Startup saves migrated Roles, Items and voice fields into your existing file with a `.before-brainless-migration` backup. Other plugin sections remain verbatim. `role_catalog_revision: 2` adds Cowboy, both Rogues and Containment Engineer once. Existing percentages stay unchanged; new entries use available percentage space or start at 0% when the pool is full. S03 reuses the existing scientist entry. Later deliberate removals remain respected.

## New role and capture settings

| Setting | Default / use |
| --- | --- |
| `brainless_roles.lockpick_duration_seconds` | 15 seconds at level 1 |
| `brainless_roles.lockpick_extra_seconds_per_level` | +15 seconds per higher level |
| `brainless_roles.lockpick_cooldown_seconds` | 120 seconds at level 1 |
| `brainless_roles.lockpick_extra_cooldown_per_level` | +60 seconds per higher level |
| `brainless_roles.rogue_spawn_separation` | 12 meters from other living players |
| `brainless_roles.announce_rogue` | Early CASSIE alert enabled |
| `project_brainless_sl_fixes.capture_conversions.enabled` | Escorted recruitment enabled |
| `...capture_conversions.minimum_alive_seconds` | Prisoner must be alive for 10 seconds |
| `...capture_conversions.chaos_role` / `mtf_role` | `ChaosConscript` / `NtfPrivate` |

Each role exposes `add`, `remove`, `replace_inventory`, `fill_ammo_to_limit`, `spawn_area` and its stat fields. Keep Rogue `neutral: true` for its unaffiliated team; `Entrance` and `MicroRoom` spawn areas require a safe point. Facility lockdowns still block Locksmith. Capture conversion happens only while cuffed at the normal surface escape zone with a living enemy military captor; Rogues can join either faction.

## Stats privacy and Discord

Core 0.2.0 counts connected playtime, including lobby/spectator time. It does not award XP for time alone. [Discord setup](owner/Core.md) enables `/pb-stats [user]`, `/pb-playtime [user]`, `/pb-top`, `/pb-link` and `/pb-optout`.

F1 `.pbprivacy optout` or native DNT erases the player's stored profile and link and suppresses further tracking. Salted suppression markers persist the preference across restarts. Manual opt-out survives DNT being turned off. `.pbprivacy optin` starts fresh with DNT off. Preserve the `stats.json.privacy` file and its backup during normal maintenance; they hold those preferences. See [Core notes](owner/Core.md) for storage scope and recovery.

## Reloads and updates

Save YAML, then run the matching reload command. Rejected edits leave the active config intact. Changing registered enabled states, IDs or native bodies requires a full restart. Updates download automatically when configured and load on the next full server restart; they do not force a restart.

Download selected DLLs or the combined ZIP from [Releases](https://github.com/Swishhyy/BrainlessProjectPlugins/releases/latest). For this private repo, each plugin can use `auto_update.token` with a repository-restricted Contents: read token. Keep tokens out of staff screenshots/shared files.

## Detailed owner references

| Need | Reference |
| --- | --- |
| All numeric gameplay tuning | [Tuning](TUNING.md) |
| Loot routing and percentage details | [Natural loot](NATURAL_LOOT.md) |
| Updater setup / rollback | [Updates](UPDATES.md) |
| Role gates and defaults | [Role availability](ROLE_STATUS.md) |
| Discord bot / XP setup | [Core notes](owner/Core.md) |
| SCP-914 / XP capture | [Fixes notes](owner/SlFixes.md) |
| Per-item override files / imported SUP features | [SUP settings](SUP_MIGRATION.md) |
| Developer boundaries / testing | [Architecture](ARCHITECTURE.md) / [Playtest](PLAYTEST.md) |

The `owner/` folder retains detailed plugin notes; older version entries there are historical. Current per-entry percentages are described above.
