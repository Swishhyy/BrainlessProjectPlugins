# Commands and permissions

Use Remote Admin's text command box without a leading slash. `ci` is EXILED's alias for `customitems`. `<argument>` is required; `[argument]` is optional.

| Command | Permission | Purpose |
| --- | --- | --- |
| `bproles` | `brainless.roles` | List configured role profiles and enabled status. Assignments are automatic; this does not force a role. |
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

Items use IDs 7101-7112; weapons use 7201-7206. See [CATALOG.md](CATALOG.md). Disabled/experimental entries are not registered until enabled.

## Explicit admin testing

Normal grants obey both individual and shared round caps. Clusters have no natural supply below 12 players. Guns share a supply of one below 24 players and two at 24+. A refused grant now returns its reason and zero deliveries instead of an unconditional success. Mixed bulk results show actual successful and failed counts.

Append `--force` to `ci give` or `ci spawn` to bypass only supply/population limits. It requires the normal command permission plus:

| Content | Additional permission |
| --- | --- |
| Items, including Cluster Grenade | `brainless.items.bypasslimits` |
| Firearms | `brainless.weapons.bypasslimits` |

Examples (run one at a time):

```text
ci give 7102 --force
ci give 7102 4 --force
ci give 7201 4 --force
ci spawn 7102 4 --force
```

An admin override does not spend, reset or refill natural supply. It still requires an alive, connected, unrestrained recipient with enough inventory room. Gas needs two free slots and an enabled Filter Inhaler. It does not enable disabled weapons or bypass gameplay mechanics such as Last Word's six-shot budget.

For CedMod-managed permissions, add the same nodes to the staff group's EXILED/plugin permissions in its panel. Existing `customitems.*` grants normal item commands, but does not grant the new `brainless.*.bypasslimits` nodes. No extra permissions are required to use ordinary equipment or see the role HUD.
