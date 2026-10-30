- Fix native explosion damage setup: valid armor penetration replaces zero, and world positions are no longer passed as force. This also respects native HE cover masks.
- Custom utilities now appear naturally: convert a few matching native map pickups and locker contents, and occasionally give/convert gear on human spawns after native/custom role kits finish.
- Defaults are 25% map chance (max 3 replacements), 30% human-spawn chance (max 3 successful grants / 1 per user), and one map bundle per room. Existing supply/population/item caps remain.
- Class-D can start with chalk, a brace or dressing. Other human medicine/grenades can become matching custom variants. Registered custom gear is skipped by default.
- Gas conversions still include their filter; locker filters respect the original chamber and deferred opening. Keycards/objectives/SCP objects are absent from default pools.
- Optional extra world loot now scatters at real loose-loot anchors instead of one hard-coded armory location.
- Tune distribution switches, chances, weighted pools, zones, roles, spawn reasons, limits and delays in YAML; `bpitems reload` preserves awards/supply and does not replay the map pass.

Validation: independent builds against EXILED 9.15.2 / SCP: SL 14.2.8, headless gameplay/updater regression checks and native API contracts. Live loot/locker behavior and client hints/audio still require a server playtest.

See [natural loot](https://github.com/Swishhyy/BrainlessProjectPlugins/blob/main/docs/NATURAL_LOOT.md), [configuration](https://github.com/Swishhyy/BrainlessProjectPlugins/blob/main/docs/TUNING.md) and [updates](https://github.com/Swishhyy/BrainlessProjectPlugins/blob/main/docs/UPDATES.md).

Stronger grenade defaults: Impact 140/350 peak over 5m; Cluster parent 90/250 over 4.5m plus four 70/180 children over 4m, sharing 150/500 raw caps. Old untouched stock fields migrate; custom values/counts remain.
