# Project Brainless — Staff Quick Guide

[What we have and how to use it](CATALOG.md) · [All commands and permissions](COMMANDS.md)

Use **Remote Admin → text console**, without `/` or `.`. `[playerID]` is optional in-game; the hosting console needs a target ID.

| Task | Command | Permission |
| --- | --- | --- |
| List equipment IDs | `ci list registered` | `customitems.list.registered` |
| Give equipment | `ci give <ID> [playerID]` | `customitems.give` |
| Drop equipment at a player | `ci spawn <ID> <playerID>` | `customitems.spawn` |
| List roles, chances and blocked reasons | `bproles` | `brainless.roles` |
| Inspect a player’s role | `bprole [playerID]` | `brainless.roles` |
| Set a custom role | `bprole set <ID> [playerID]` | `brainless.roles` |
| Queue Infection | `bpevent infection next` | `projectbrainless.events` |
| Start Infection now | `bpevent infection start` | `projectbrainless.events` |
| Check / stop an event | `bpevent status` / `bpevent stop` | `projectbrainless.events` |

Examples: `ci give 7102 4` gives player 4 a Cluster Grenade. `bprole set Z04` makes you Explosive Zombie. `bprole set S04 4` makes player 4 Candy Man.

1. **Equipment won’t give:** check ID, enabled state, free inventory space and that the target is alive/unrestrained. Admin grants need no `--force`.
2. **Role won’t roll:** read `bproles`. Disabled/experimental profiles and missing equipment still block admin tests. Setting a role replaces the current kit.
3. **Infection:** needs four living players by default. Starting it replaces combat roles/kits. After stopping, restart the round for normal roles.
4. **Voice:** players use F1 `.scpchat` or tap N, then hold their normal voice key. `.scpchat status` checks it; `.scpchat mute all` / `.scpchat unmute all` controls reception.
5. **Stats:** players use F1 `.pbxp` / `.pbxp top`. Staff use `pbcore status` or `pbfixes status` to check services.

Save configuration edits, then use the matching reload command in the [command table](COMMANDS.md). A newly installed DLL needs a full server restart. For chance settings, use [Owner Settings](CONFIGURATION.md).
