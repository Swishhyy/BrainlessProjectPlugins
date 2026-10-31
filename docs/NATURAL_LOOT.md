# Natural loot and starting gear

Items and Weapons 0.3.0 automatically distribute their content through native map loot, locker contents and occasional human spawn gear. No admin command or role-provider DLL is required for these routes. Each provider remains an independent DLL with its own config and supply budget.

## Shipped defaults

| Setting | Items | Weapons |
| --- | --- | --- |
| Native map replacement roll | 25% | 20% |
| Maximum map replacements per round | 3 | 1 |
| Map bundles per room, per provider | 1 | 1 |
| Human spawn roll | 30% | 15% |
| Maximum successful spawn grants per round | 3 | 1 |
| Successful grants per user per round | 1 | 1 |
| Delay after spawning, allowing role kits to finish | 0.75s | 1s |
| Initial map pass delay | 1.5s | 1.5s |
| Skip players already carrying registered custom equipment | true | true |

These are maxima and probabilities. Existing role kits, per-item caps, disabled providers, lobby thresholds, inventory space and the provider's remaining global supply can reduce the actual number. All normal routes share the original utility/firearm budgets. Administrative give/spawn still bypasses natural limits.

The map pass takes a snapshot at round start, shuffles it and converts eligible native pickups in place. Their serial, physical position, locker membership and access restrictions remain. This includes closed and delayed-spawn locker contents; converted items appear through the native locker behavior. Keycards, ammo, objective items and SCP objects are absent from the default replacement pools.

Smoke grenades use one flashbang body and one inventory/supply slot. They do not receive a paired filter; the native smoke is visual cover only.

The former `world_loot` switch controls **additional** bundles. When enabled, they now scatter beside real accessible loose-loot anchors throughout configured zones, using `tuning.supply.world_loot_count` as their maximum. They also respect room/supply caps and the configured map rule pool. They no longer depend on a room type containing the word Armory. Smoke bundles spend one utility supply slot. No valid loose anchor or available supply means no extra drop.

`world_loot: false` disables additional bundles; map replacements and player loadouts have their own switches. `distribution.enabled: false` disables all three automatic routes for that provider.

## Default content pools

| Route | Eligible content |
| --- | --- |
| Item map conversions | Medkit → Field Dressing; painkillers → Filter; coin → Chalk; janitor keycard → Door Jammer; flashbang → Smoke, Echo or Foam; HE → Impact or rare Cluster; flashlight → Beacon; radio → Jammer |
| Class-D extra spawn gear | Chalk, Door Jammer or Field Dressing |
| Other human item spawn conversions | A matching native medicine, flashbang or HE grenade can become one of the above custom variants |
| Weapon map/spawn conversions | E11 → Nullifier; Crossvec → Rattler; Shotgun → Breach Shotgun |

Cluster keeps its configured population gate (12 players by default) and round caps. Last Word, Grenade Launcher, experimental weapons and Preserved Specimen are not in the default random pools; configured role kits/admin grants still provide them. Add supported IDs to the pools if desired. Disabling content prevents selection; a missing ID is skipped without consuming an award.

Converted firearms use their configured loaded magazine where supported, preserving the native loadout's reserve rather than granting additional reserve ammo. Map conversion skips a configured magazine larger than the native pickup capacity; additional item grants can initialize their own native firearm instance. Finite guns retain their serial-based shot ledger if an operator adds them to a pool. Native firearm bodies must match for in-place conversion.

## Configuration

The new `distribution` block is generated under both `brainless_items` and `brainless_weapons`, including full default rule lists. Existing configs receive the new property defaults on loading the updated DLL. Edit the generated block, save, then use `bpitems reload` or `bpweapons reload` with the existing configure permission.

This abbreviated example shows the fields and a deliberately small item pool. Replacing a rule list with this example restricts the pool to the listed entries.

```yaml
brainless_items:
  world_loot: true
  distribution:
    enabled: true
    map_replacements: true
    map_chance_percent: 25
    max_map_replacements: 3
    max_map_items_per_room: 1
    map_delay_seconds: 1.5
    player_loadouts: true
    player_chance_percent: 30
    max_player_grants: 3
    max_grants_per_player: 1
    player_delay_seconds: 0.75
    skip_players_with_custom_items: true
    show_spawn_hint: true
    allowed_spawn_reasons:
    - RoundStart
    - LateJoin
    - Respawn
    - RespawnMiniwave
    map_rules:
    - custom_id: 7109
      weight: 6
      source_types: [Medkit]
      zones: [LightContainment, HeavyContainment, Entrance]
    - custom_id: 7101
      weight: 3
      source_types: [GrenadeHE]
      zones: [HeavyContainment, Entrance]
    - custom_id: 7113
      weight: 2
      source_types: [GrenadeHE]
      zones: [HeavyContainment, Entrance]
    player_rules:
    - custom_id: 7108
      weight: 3
      roles: [ClassD]
      replace_types: []
    - custom_id: 7109
      weight: 6
      roles: []
      replace_types: [Medkit]
```

`weight` is relative among eligible choices for that particular pickup/player. Zero disables that rule. The outer chance is separate: raising weights does not raise the overall chance of receiving custom loot. Each map rule needs a nonempty `source_types` list. Empty `zones` allows any captured non-pocket room; defaults limit content to LCZ, HCZ and Entrance. Default map pools exclude Surface, but you can explicitly allow it.

For player rules, empty `roles` means any connected living human, and empty `replace_types` means an additional item. A nonempty replacement list converts one matching ordinary native item, preserving the rest of the inventory. Replacements require the custom item to use the same native body; adding a new item can use a different body. Full inventories skip additional items; smoke replacements preserve the existing slot. Existing registered custom items, including other plugins' items, are never converted.

Default spawn reasons exclude escapes, SCP resurrection, forced class changes and item-induced respawns. The spawn must assign a native inventory and still be the same living human role when its delayed callback runs. Duplicate callbacks cannot roll a life twice, and successful awards remain tied to the user ID through deaths/reconnects until round reset. Users with custom role kits are skipped by default; the independent providers also see each other's registered custom gear.

## Applying and checking changes

Reload changes future decisions without resetting awards, user caps or supply. It does not replay the initial map pass. Change map settings before the next round to see a fresh distribution. Counts set to zero disable that award channel; chances accept 0-100. Registration/native-body changes still need a full server restart.

For a test round, temporarily set the relevant chance to 100, keep enough remaining supply and use `debug: true`. The server logs actual replacement/extra counts and skipped conversions. Use `ci list registered` for availability, `ci list insideinventories` for tracked holders, and `bprole` for role-kit inspection. Restore intended chances after testing. Verify locked/closed lockers, deferred spawns, smoke, role-kit overlap, reinforcement waves and cleanup with real clients; headless tests do not simulate Unity physics or clients.

Items 0.4.0 changes smoke 7101 to the flashbang body and migrates old single-HE smoke conversion rules. It needs one slot and no filter. SCP-914 crafting lives in SlFixes and spends the same Items/Weapons supply; it does not add another random map/spawn roll.
