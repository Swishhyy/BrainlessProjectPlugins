# Commands and permissions

Use Remote Admin's text command box without a leading slash. `ci` is EXILED's alias for `customitems`. `<argument>` is required; `[argument]` is optional.

| Command | Permission | Purpose |
| --- | --- | --- |
| `bprole` / `myrole` | `brainless.roles` | Inspect your custom/native role, stats and assignment reason. Admin only. |
| `bprole <playerID>` | `brainless.roles` | Inspect another player. |
| `bprole all` | `brainless.roles` | Inspect all connected players. |
| `bproles [playerID]` | `brainless.roles` | Audit every profile and why it can/cannot roll now; optionally evaluate for a specific player. |
| `bprole set <roleID> [playerID]` | `brainless.roles` | Admin playtest: reset a native kit and apply a profile, ignoring natural chance/population/supply caps. Disabled profiles and missing providers are still reported. |
| `ci list registered` | `customitems.list.registered` | List available items and weapons with their IDs. |
| `ci info <ID>` | `customitems.info` | Inspect an item or weapon. |
| `ci give <ID> [playerID]` | `customitems.give` | Give to yourself or specified players. `all`/`*` and the framework's player selector syntax are supported. |
| `ci spawn <ID> <playerID/location>` | `customitems.spawn` | Spawn at a living player's feet or native spawn location. |
| `ci spawn <ID> <x> <y> <z>` | `customitems.spawn` | Spawn at coordinates. |
| `ci list insideinventories` | `customitems.list.insideinventories` | Show tracked instances and holders. |
| `getserial [playerID]` | `bc.getserial` | Inspect the held item's serial. |
| `coinuses get player [playerID]` | `bc.coinuses.get` | Inspect remaining uses on a held coin. |
| `coinuses get serial <serial>` | `bc.coinuses.get` | Inspect uses by coin serial. |
| `coinuses set player <uses>` | `bc.coinuses.set` | Set uses on your own held coin. |
| `coinuses set player <playerID> <uses>` | `bc.coinuses.set` | Set uses on another player's held coin. |
| `coinuses set serial <serial> <uses>` | `bc.coinuses.set` | Set uses by serial. |

Items use IDs 7101-7109 and 7111-7112 (7110 is retired); weapons use 7201-7207. See [CATALOG.md](CATALOG.md). Disabled/experimental entries are not registered until enabled.

## Admin grants

`ci give` and `ci spawn` automatically ignore population and round supply caps for Brainless items/weapons. Your ordinary `customitems.give` or `customitems.spawn` permission is enough. For example, a cluster can be given in a four-player server with `ci give 7102`. No `--force` flag or additional bypass permission is needed. Mixed bulk results show actual successful and failed deliveries.

Examples (run one at a time):

```text
ci give 7102
ci give 7102 4
ci give 7201 4
ci give 7207
bprole
bprole 4
bprole all
bproles
bprole set M04 4
ci spawn 7102 4
```

Admin grants do not spend, reset or refill natural supply. Automatic role kits/world loot still use their normal limits: clusters have no natural supply below 12 players and guns share one slot below 24 players/two at 24+. Giving an item still requires an alive, connected, unrestrained recipient with enough inventory room. Gas needs two free slots and an enabled Filter Inhaler. Admin commands do not enable disabled weapons or refill an existing Last Word's six-shot budget.

For CedMod-managed permissions, add `customitems.*` to the staff group's EXILED/plugin permissions in its panel for item administration. No extra permissions are required to use ordinary equipment, see the role HUD, or run automatic release checks.

## Role checks

All `bprole`/`bproles` commands are registered only in Remote Admin and require `brainless.roles`, including `myrole`. Add that node to the CedMod staff group's EXILED/plugin permissions. Normal players can use the role HUD without a command permission. `bproles` shows actual configuration, missing equipment IDs, experimental switches, population requirements and exhausted natural supply. `READY` means eligible to roll, not guaranteed to spawn.

`bprole set` queues a fresh base role/inventory, then applies the chosen profile after the native spawn finishes. It preserves a matching reinforcement rank, does not spend natural kit/role supply, and reports disabled/missing content. Inspect the result after a moment with `bprole <playerID>`. Native force-class changes alone do not run the random role selector. Doctor still requires its experimental/event switches and enabled profile.

## Locksmith interaction

D06 uses the ordinary interact key at a denied closed door; no item, new command or permission is needed. Default channel time is 15 seconds and cooldown is 120 seconds after success. Doors requiring containment level 3 or armory level 3 are excluded, including requirements inherited from a parent checkpoint. Gates/checkpoints otherwise use their native interaction behavior. Interruptions do not spend cooldown; a successful cooldown persists through role changes/reconnects until round reset. `ci give 7110` no longer grants a Maintenance Pass.
