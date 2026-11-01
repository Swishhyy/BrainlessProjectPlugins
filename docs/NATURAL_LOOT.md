# Natural Loot — Owner Reference

Items and Weapons 0.6.0 use **`chance_percent` on each distribution rule**. See [Owner Settings](CONFIGURATION.md) for an example.

| Route | How the roll works |
| --- | --- |
| Map replacements | One percentage roll for an original pickup’s source type and zone; success converts it in place. |
| Optional extra drops | One map-rule percentage roll per valid loose-loot anchor, up to the extra-drop cap. |
| Starting gear | One percentage roll for the human’s role after the native/role kit finishes. |

1. Map shares must total at most 100 for every overlapping source/zone. Starting-gear shares must total at most 100 for every matching human role.
2. Blocked, disabled, missing or exhausted entries keep their share as vanilla/nothing. A different item does not inherit that chance.
3. Map replacements preserve native bodies, serials, positions and locker membership. Replacement bodies must match the custom item’s body. Extra drops may use another body.
4. `roles: []` matches any human. `replace_types: []` gives extra gear; otherwise the matching native item must exist in the kit.
5. Existing room, round, player, per-item and population caps remain. Natural routes, role kits and SCP-914 crafting share supply. Admin give/spawn bypasses natural supply.
6. The map snapshot is captured once per round. Player drops, deaths and crafted items are not new map-roll targets. Each new life rolls once, and per-user round limits survive reconnects.
7. `world_loot` controls extra drops. `map_replacements` and `player_loadouts` control their own routes; `distribution.enabled` controls all three.
8. Missing/null percentages migrate from the old global budgets and selection weights. Explicit percentages ignore `map_chance_percent`, `player_chance_percent` and rule `weight`. New/previously unlisted entries use 0% until configured; empty lists remain empty.

Defaults retain bounded supply: Items allow up to 3 map replacements and 3 successful spawn grants per round; Weapons allow 1 of each. Both allow one bundle per room and one successful grant per user by default. Cluster natural supply still requires 12 players. Enabled state and actual server config determine availability—use `ci list registered`, `bpitems chances` and `bpweapons chances`.

Map migrations keep smoke on flashbang sources and Door Jammer on janitor-card sources. Finite weapons retain their serial-based shot limits; conversion does not create reserve ammunition. Save edits and run `bpitems reload` or `bpweapons reload`. Reload does not rerun map distribution or reset supply.
