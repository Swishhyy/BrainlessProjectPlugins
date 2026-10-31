# Commands and permissions

Use Remote Admin's text command box without a leading slash. `ci` is EXILED's alias for `customitems`. `<argument>` is required; `[argument]` is optional.

| Command | Permission | Purpose |
| --- | --- | --- |
| `bprole` / `myrole` | `brainless.roles` | Inspect your custom/native role, stats and assignment reason. Admin only. |
| `bprole <playerID>` | `brainless.roles` | Inspect another player. |
| `bprole all` | `brainless.roles` | Inspect all connected players. |
| `bproles [playerID]` | `brainless.roles` | Audit every profile and why it can/cannot roll now; optionally evaluate for a specific player. |
| `bprole set <roleID> [playerID]` | `brainless.roles` | Admin playtest: reset a native kit and apply a profile, ignoring natural chance/population/supply caps. Disabled profiles and missing providers are still reported. |
| `bprole reload` | `brainless.roles` | Read and validate Roles YAML, then apply new settings without resetting assignments/cooldowns. |
| `bpitems reload` | `brainless.items.configure` | Read and validate item tuning; preserve existing charges, fields and spent supply. |
| `bpweapons reload` | `brainless.weapons.configure` | Read and validate weapon tuning; preserve already-issued finite shot budgets. |
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

Items use IDs 7101-7109 and 7111-7122 (7110 is retired); weapons use 7201-7208. See [CATALOG.md](CATALOG.md). Disabled/experimental entries are not registered until enabled.

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

Admin grants do not spend, reset or refill natural supply. Automatic role kits/world loot still use their normal limits: clusters have no natural supply below 12 players and guns share one slot below 24 players/two at 24+. Giving an item still requires an alive, connected, unrestrained recipient with enough inventory room. Smoke needs one free slot and no filter. Admin commands do not enable disabled weapons or refill an existing Last Word's six-shot budget.

For CedMod-managed permissions, add `customitems.*` to the staff group's EXILED/plugin permissions in its panel for item administration. No extra permissions are required to use ordinary equipment, see the role HUD, or run automatic release checks.

## Role checks

All `bprole`/`bproles` commands are registered only in Remote Admin and require `brainless.roles`, including `myrole`. Add that node to the CedMod staff group's EXILED/plugin permissions. Normal players can use the role HUD without a command permission. `bproles` shows actual configuration, missing equipment IDs, experimental switches, population requirements and exhausted natural supply. `READY` means eligible to roll, not guaranteed to spawn.

`bprole set` queues a fresh base role/inventory, then applies the chosen profile after the native spawn finishes. It preserves a matching reinforcement rank, does not spend natural kit/role supply, and reports disabled/missing content. Inspect the result after a moment with `bprole <playerID>`. Native force-class changes alone do not run the random role selector. Doctor still requires its experimental/event switches and enabled profile.

## Locksmith interaction

D06 uses the ordinary interact key at a denied closed door; no item, new command or permission is needed. Default channel time is 15 seconds and cooldown is 120 seconds after success. Doors requiring containment level 3 or armory level 3 are excluded, including requirements inherited from a parent checkpoint. Gates/checkpoints otherwise use their native interaction behavior. Interruptions do not spend cooldown; a successful cooldown persists through role changes/reconnects until round reset. `ci give 7110` no longer grants a Maintenance Pass.

## SCP nearby voice

These are **F1 player-console** commands with a leading dot. No admin or CedMod permission is needed. The native Server Specific Settings menu also supplies a rebindable key (suggested N), a toggle-only nearby route and a receive switch. Always hold your normal voice key to transmit.

| Command | Purpose |
| --- | --- |
| `.scpchat` / `.scpvc` | Toggle nearby/team for an enabled living SCP. |
| `.scpchat local` / `.scpchat team` | Select the route explicitly. |
| `.scpchat status` | Inspect your route and receive/mute state. |
| `.scpchat mute all` / `.scpchat unmute all` | Disable/enable all custom nearby SCP audio, preserving individual mutes. |
| `.scpchat mute <playerID>` / `.scpchat unmute <playerID>` | Block/unblock one connected player's nearby SCP audio for your session. |

Nearby mode replaces team chat for that speech. It excludes distant receivers and spectators. Standard scoreboard individual mutes do not cover speaker-toy audio; use these receive controls. Server administrative voice mutes still apply. See [ScpProximityChat](../ScpProximityChat/README.md) for configuration and native-channel exclusions.

## Balance reloads

Edit the generated YAML first, then run the matching Remote Admin reload command. Add `brainless.items.configure` and `brainless.weapons.configure` to the appropriate CedMod staff group; give/spawn permissions alone do not grant config control. A rejected file leaves active settings intact. IDs, native bodies, item/weapon enabled state, keybind IDs and experimental weapon registration require a full restart. Changes affect new throws/deployments and new kit grants; no command refunds supply, refills finite weapons or resets successful lockpick cooldowns. See [TUNING.md](TUNING.md).

Quick impact test: `ci give 7113 [playerID]`. Quick explosive zombie test: `bprole set Z04 [playerID]`, then confirm with `bprole <playerID>`. Native force-class alone does not assign a custom profile.

Natural map loot and occasional spawn gear need no player command or permission. Staff tune the `distribution` section and use the existing provider reload command. Reload preserves issued counts and does not replay the map pass. See [NATURAL_LOOT.md](NATURAL_LOOT.md). SCP voice route controls now display enabled/disabled hints even while the role HUD is present.

SCP proximity chat 0.1.2 uses a tap-to-toggle key only: N enables nearby, release N, then speak with your normal voice key; the next N press restores team. `.scpchat status` displays route checks/audio sends to help diagnose a live test. Candy Man can be tested with `bprole set S04 [playerID]` and inspected with `bprole <playerID>`.

## Core, Fixes and debugging

Remote Admin, with `brainless.roles`: `bprole set Z04` or `bprole set explosive_zombie` makes yourself Explosive; `bprole set Z04 2` targets player 2. `bprole set G01` tests the Guard Captain 1/2/1 card. Use underscores for role names containing spaces. `bprole` inspects yourself and `bproles` audits IDs.

| Where | Command | Permission |
| --- | --- | --- |
| F1 | `.pbxp`, `.pbstats`, `.pbxp top` | Everyone; own stats/public leaderboard |
| F1 | `.pblink` | Everyone; private one-time Discord linking code |
| Remote Admin | `pbxp <playerID>` | `projectbrainless.stats` |
| Remote Admin | `pbcore status`, `pbcore save` | `projectbrainless.core` |
| Remote Admin | `pbfixes status`, `pbfixes reload` | `projectbrainless.fixes` |
| Discord | `/pb-stats`, `/pb-top`, `/pb-link code:<code>` | Bot must be configured; stats accounts linked with `.pblink` |

[Core](../Core/README.md) owns persistence/Discord. [Fixes](../SlFixes/README.md) owns SCP-914 rules and XP action capture. Linking never grants staff permissions.

## SUP command compatibility

`sci help`, `sci itemlist` (alias `sci items`), and `sci give <ID> [playerID]` (alias `sci gitem`) are Remote Admin commands. Granting needs `customitems.give` or legacy `sci.admin`, confirms the actual inventory result and bypasses natural supply/population limits. It does not bypass gameplay use conditions such as reinforcement timing. SUP IDs 101–112 map to the current definitions; 103 means Anti-096, and Volatile Pills are 7122. No webhook is called. Standard `ci` commands and existing `bpitems reload` / `bpweapons reload` remain. See [SUP_MIGRATION.md](SUP_MIGRATION.md).
