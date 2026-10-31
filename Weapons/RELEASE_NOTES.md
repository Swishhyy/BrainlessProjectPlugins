Paste a read-only GitHub token directly into `auto_update.token` in this plugin's YAML. No Bisect environment-variable setup is required. The config token takes precedence; existing environment-only setups keep working.

Missing-token and permission errors now explain the appropriate fix without echoing credentials. New configs scan up to 30 releases so all five components can be discovered. Configured scan counts are preserved.

Install this DLL manually once if the old updater cannot authenticate, set the token, and fully stop/start the server. Subsequent automatic downloads retain SHA-256/assembly validation, `.previous` backups and full-restart activation.

This release also includes the previous gameplay changes:

- Fix native explosion damage setup: valid armor penetration replaces zero, and world positions are no longer passed as force. This also respects native HE cover masks.
- Native map/locker firearms and occasional human starting guns can become Nullifier, Rattler or Breach Shotgun variants in place.
- Defaults are 20% per eligible map gun (max 1 replacement), 15% per eligible human spawn (max 1 successful grant / 1 per user), and one map bundle per room.
- Existing firearm/item supply, disabled/experimental settings and custom role kits remain respected. Replacement does not grant extra reserve ammo.
- Last Word, launcher and experimental prototypes are excluded from the default random pools. Pools, chances, roles, zones and limits are configurable.
- `bpweapons reload` updates future decisions without replaying map rolls, resetting supply or refilling finite weapons.

Validation: independent builds against EXILED 9.15.2 / SCP: SL 14.2.8, headless gameplay/updater regression checks and native API contracts. Live loot/locker behavior and client hints/audio still require a server playtest.

See [natural loot](https://github.com/Swishhyy/BrainlessProjectPlugins/blob/main/docs/NATURAL_LOOT.md), [configuration](https://github.com/Swishhyy/BrainlessProjectPlugins/blob/main/docs/TUNING.md) and [updates](https://github.com/Swishhyy/BrainlessProjectPlugins/blob/main/docs/UPDATES.md).

Launcher defaults increase to 70 human / 150 SCP over 3.5m, with 16 shots and existing cadence/fuse preserved. Old untouched stock fields migrate; custom values remain.
