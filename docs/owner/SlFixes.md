> Detailed owner notes. For staff, use [the quick guide](../STAFF_GUIDE.md) and [content page](../CATALOG.md). Older version notes below are historical; current spawn percentages are explained in [configuration](../CONFIGURATION.md).

# Project Brainless SCP SL Fixes 0.2.1

Small configurable gameplay additions, in a separate `Brainless.SlFixes.dll` for EXILED 9.15.2 / SCP:SL 14.2.8. Install it in EXILED/Plugins and fully restart. Install `Brainless.Core.dll` for saved XP and optional Discord stats; SCP-914 behavior still runs if Core is absent. Install Items/Weapons 0.5.0 for their crafting adapters. No gameplay plugin depends on another gameplay DLL.

## SCP-914 rough teleport

`rough_teleports_players: true` and `rough_teleport_chance: 100` make Rough select a random physical facility destination for an eligible living player. SCP-079 and spectators are excluded. Destinations are checked for floor support, headroom, room membership and occupied/blocked space. Surface, pocket dimension, the current room and decontaminated LCZ are excluded. If no safe point is found, native output remains. The normal SCP-914 item-upgrade setting remains in force; this does not disable other plugins' cancellation or guarantee a destination through invalid map geometry.

## SCP-914 recipes

| Input | Setting | Custom output | Chance | Recipe maximum per round |
| --- | --- | --- | --- | --- |
| Flashbang | Fine | Smoke 7101 | 40% | 3 |
| HE grenade | Very Fine | Cluster 7102 | 25% | 2 |
| HE grenade | Fine | Impact 7113 | 30% | 2 |
| Medkit | 1:1 | Field Dressing 7109 | 35% | 3 |

All recipe fields are editable in `project_brainless_sl_fixes.recipes`: `name`, `source`, `setting`, `custom_id`, `chance`, `max_per_round`, `enabled`. Chance is per eligible input; recipes are checked in config order. Zero maximum disables that recipe. If a provider is missing, the roll fails, or normal item supply/population limits deny conversion, the native SCP-914 result applies. Recipes do not use admin supply bypasses. In particular, natural clusters still use the Items population/supply configuration, including the default 12-player gate.

Inventory and dropped pickups are converted in place, retaining their native serial/body, so a full inventory needs no spare slot. Only compatible base bodies are supported. Existing custom items are excluded from fresh recipe rolls and retain their provider's normal SCP-914 behavior. A provider must expose the public TryCraftInventory/TryCraftPickup adapter. Conversions count only after provider confirmation. Per-recipe limits share the provider's ordinary role/map/spawn supply budget.

Random map/locker replacements and starting gear remain owned by Items/Weapons `distribution`; Fixes does not run a second loot roller. Smoke now uses flashbang pools, clusters HE pools. Edit those provider configs for chances, weighted pools, per-player, per-room and per-round limits.

## XP tracking

Enable with `enable_xp: true`. Default `xp_rewards`: enemy human kill 10, SCP kill 50, escape 35, craft 5, rough teleport 2, round played 5, round survived 10, death 0. Deaths still count as statistics. There is no XP for friendly kills or self-kills. Craft and teleport XP is limited to three rewarded actions per player per round; further successful actions still count. Round rewards require 120 seconds by default. Core applies its independent overall XP cap and deduplicates event receipts.

Player commands are `.pbxp`, `.pbxp top` and `.pblink` in F1. Core owns persistence, levels, Discord linking and bot hosting; see [Core](https://github.com/Swishhyy/BrainlessProjectPlugins/blob/main/Core/README.md).

## Admin and updates

`pbfixes status` lists recipes/current craft counts and Core availability. `pbfixes reload` validates YAML before applying edits, preserving the round ledgers. Both require `projectbrainless.fixes`. Common update configuration supports `auto_update.token` directly; full process restart is needed after a downloaded DLL replacement. See [UPDATES.md](https://github.com/Swishhyy/BrainlessProjectPlugins/blob/main/docs/UPDATES.md).

Headless checks validate configuration, native interfaces, persisted XP, receipt/cap behavior and release files. Safe destination geometry, the actual SCP-914 result, client smoke and Discord connectivity still need a live test on your server.

Version 0.2.0 adds ten SUP recipes once through `recipe_revision: 1`; later removals and explicit empty pools remain respected. See [SUP_MIGRATION.md](https://github.com/Swishhyy/BrainlessProjectPlugins/blob/main/docs/SUP_MIGRATION.md) for their inputs, settings and chances. All crafting uses ordinary provider supply, and the new railgun recipe shares the same firearm limit as map/role kits.
