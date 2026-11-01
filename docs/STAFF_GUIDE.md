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
| Tune SCP voice live | `bpscpchat volume 10` | `brainless.scpchat.configure` |
| Queue Infection | `bpevent infection next` | `projectbrainless.events` |
| Start Infection now | `bpevent infection start` | `projectbrainless.events` |
| Check / stop an event | `bpevent status` / `bpevent stop` | `projectbrainless.events` |

Examples: `ci give 7102 4` gives player 4 a Cluster Grenade. `bprole set Z04` makes you Explosive Zombie. `bprole set S04 4` makes player 4 Candy Man.

New role tests: `bprole set C05` Cowboy, `G07` Rogue Guard, `S03` Head Scientist, `S07` Containment Engineer, `S08` Rogue Scientist. Use the full command followed by an optional player ID.

1. **Equipment won’t give:** check ID, enabled state, free inventory space and that the target is alive/unrestrained. Admin grants need no `--force`.
2. **Role won’t roll:** read `bproles`. Disabled/experimental profiles and missing equipment still block admin tests. Setting a role replaces the current kit.
3. **Infection:** needs four living players by default. Starting it replaces combat roles/kits. After stopping, restart the round for normal roles.
4. **Voice:** players use F1 `.scpchat` or tap N, then hold their normal voice key. `.scpchat status` checks it; staff can adjust `bpscpchat volume <0-20>` live; `.scpchat mute all` / `.scpchat unmute all` controls reception.
5. **Capture:** cuff enemy military personnel and escort them to the normal surface escape zone. Chaos recruits Guards/MTF; Guards/MTF recruit Chaos. Either side can recruit a Rogue. The captor must be alive and the prisoner alive for at least ten seconds.
6. **Locksmith:** interact with a denied keycard door and stay nearby. Level 1/2/3 takes 15/30/45 seconds, followed by a 120/180/240-second cooldown. Facility lockdowns still apply.
7. **Stats:** players use F1 `.pbxp` / `.pbxp top`. Link with `.pblink`, then Discord `/pb-link`. `/pb-stats [user]` and `/pb-playtime [user]` show your own or another linked player's stats; `/pb-top` shows the top 10. Staff check services with `pbcore status` or `pbfixes status`.
8. **Privacy:** F1 `.pbprivacy optout` or Discord `/pb-optout` deletes your tracked stats/link and stops collection. In-game DNT does the same. F1 `.pbprivacy optin` starts fresh once DNT is off. These commands affect only the player using them.

Door Jammer holds for 25 seconds by default. Cluster and launcher explosions can damage native breakable doors and props; immune doors remain protected.

Save configuration edits, then use the matching reload command in the [command table](COMMANDS.md). A newly installed DLL needs a full server restart. For chance settings, use [Owner Settings](CONFIGURATION.md).
