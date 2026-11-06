# Project Brainless — Command Table

[Content IDs and usage](CATALOG.md) · [Project overview and settings](../README.md)

**Remote Admin:** no leading slash/dot. `<value>` is required; `[value]` is optional. Omit `[playerID]` to target yourself in-game.

| Command | What it does | Permission |
| --- | --- | --- |
| `ci list registered` | Lists enabled items/weapons and IDs | `customitems.list.registered` |
| `ci info <ID>` | Shows equipment details | `customitems.info` |
| `ci give <ID> [playerID]` | Gives equipment; ignores natural supply/population caps | `PB.moderator` |
| `ci spawn <ID> <playerID>` | Drops equipment at a player | `PB.moderator` |
| `ci spawn <ID> <x> <y> <z>` | Drops equipment at coordinates | `PB.moderator` |
| `ci list insideinventories` | Shows tracked equipment and holders | `customitems.list.insideinventories` |
| `bprole [playerID]` | Inspects role/stats; alias `myrole` | `PB.moderator` |
| `bprole all` | Inspects everyone’s roles | `PB.moderator` |
| `bproles [playerID]` | Lists roles with their rarity, blocked reasons and this round's tier counts | `PB.moderator` |
| `bprole set <roleID> [playerID]` | Assigns a custom profile for testing | `PB.moderator` |
| `bprole hud [playerID]` | Explains what the role panel shows for a player (or who they are spectating) and why | `PB.moderator` |
| `bprole reload` | Applies role config edits | `PB.admin` |
| `bpscpchat status` | Shows active speech gain and range | `PB.moderator` |
| `bpscpchat volume <0-20>` | Changes speech volume immediately; default 10 | `PB.admin` |
| `bpscpchat reload` | Applies saved voice settings to active speakers | `PB.admin` |
| `bpitems chances` | Shows map/starting-gear item percentages | `PB.moderator` |
| `bpitems reload` | Applies item config edits | `PB.admin` |
| `bpweapons chances` | Shows map/starting-gear weapon percentages | `PB.moderator` |
| `bpweapons reload` | Applies weapon config edits | `PB.admin` |
| `bpevent list` | Lists implemented events | `PB.moderator` |
| `bpevent status` | Shows event state and counts | `PB.moderator` |
| `bpevent infection next` | Queues Infection for next round | `PB.admin` |
| `bpevent infection start` | Starts Infection in the running round | `PB.admin` |
| `bpevent deathmatch next` / `start` | Queues or starts Deathmatch (Chaos holds the facility, MTF raids) | `PB.admin` |
| `bpevent juggernaut next` / `start` | Queues or starts Juggernaut | `PB.admin` |
| `bpevent stop` | Stops rules and pending conversions; current roles remain | `PB.admin` |
| `bpevent vote` | Starts a lobby event vote now (staff player minimums) | `PB.admin` |
| `bpevent reload` | Reloads inactive/unscheduled event settings | `PB.admin` |
| `pbcore status` | Checks services | `PB.moderator` |
| `pbcore save` | Saves stats now | `PB.admin` |
| `pbupdate` | Checks every Brainless plugin for updates now, installs them and restarts the server when the round ends | `PB.admin` |
| `pbupdate now` | Same, but restarts after a 15 s warning (also when run in the lobby) | `PB.admin` |
| `pbfixes status` | Checks Fixes | `PB.moderator` |
| `pbfixes reload` | Applies the Fixes config | `PB.admin` |
| `pbxp <playerID>` | Inspects another player’s stats | `PB.moderator` |
| `bpscpswap <playerID> <playerID>` | Swaps two SCPs any time (variants follow the swap mode) | `PB.moderator` |
| `bpscpswap <playerID> <scp173\|scp096\|…>` | Moves an SCP to that SCP, swapping with whoever plays it | `PB.moderator` |
| `getserial [playerID]` | Shows the held item’s serial | `PB.moderator` |
| `coinuses get player [playerID]` | Checks held coin uses | `PB.moderator` |
| `coinuses get serial <serial>` | Checks a coin by serial | `PB.moderator` |
| `coinuses set player <uses>` | Sets your held coin’s uses | `PB.admin` |
| `coinuses set player <playerID> <uses>` | Sets another player’s coin uses | `PB.admin` |
| `coinuses set serial <serial> <uses>` | Sets uses by serial | `PB.admin` |
| `sci itemlist` / `sci help` | Legacy SUP list/help | Remote Admin access |
| `sci give <ID> [playerID]` | Legacy grant; supports mapped old IDs | `PB.moderator` |

**Two permissions cover every Brainless command.** Give staff groups `PB.admin` (everything) or `PB.moderator` (look-only commands plus SCP swap, `bprole set` and handing out custom items). `PB.admin` includes everything `PB.moderator` can do. Only EXILED's own read-only `ci list`/`ci info` still use their `customitems.*` nodes. Disabled content and invalid recipients remain blocked.

**F1 player console:** include the leading dot. These need no staff permission.

| Command | What it does |
| --- | --- |
| `.scpchat` / `.scpvc` | Toggles nearby/team SCP voice |
| `.scpchat local` / `.scpchat team` | Selects a route explicitly |
| `.scpchat status` | Shows route, version, gain and audio-send counters |
| `.scpchat mute all` / `.scpchat unmute all` | Disables/enables nearby SCP audio reception |
| `.scpchat mute <playerID>` / `.scpchat unmute <playerID>` | Mutes/unmutes one nearby SCP stream |
| `.pbxp` / `.pbstats` | Shows your stats |
| `.pbxp top` | Shows the leaderboard |
| `.pblink` | Gets a private Discord linking code when enabled |
| `.pbprivacy [status]` | Shows whether your stats are tracked |
| `.pbprivacy optout` | Stops tracking and deletes your saved stats and Discord link |
| `.pbprivacy optin` | Starts fresh; requires native DNT off |
| `.scpswap list` | Shows SCPs, their custom variants and the swap time left |
| `.scpswap <scp>` | Asks the SCP playing it to swap (e.g. `.scpswap 173`), or takes it if nobody plays it |
| `.scpswap yes` / `.scpswap no` | Accepts or declines a swap request |
| `.scpswap cancel` | Cancels your outgoing request |
| `.optin` | Takes the place of a player who opted out of being a zombie (spectators) |
| `.scpreroll` | Rerolls your custom role once per round, if you were offered it at spawn (30 s; Rare or lower) |
| `.vote` / `.vote <number>` | Shows / casts your lobby event vote (`.vote 0` = no event) |
| `.vote optout` / `.vote optin` | Sit an event round out as a spectator / play it after all |
| `.fill` | Volunteers to replace an SCP who left early in the round |

**Discord, when the stats API and bot are set up ([setup](../README.md#stats-api-and-discord-bot)):** prefix `pb` is the default. No staff permission is needed. Other-player queries work for linked, tracked accounts.

| Command | What it does |
| --- | --- |
| `/pb-stats [user]` | Level, XP, action counts and playtime; omit user for yourself |
| `/pb-playtime [user]` | Server playtime; omit user for yourself |
| `/pb-top` | Top 10 players by XP |
| `/pb-link code:<code>` | Links your game account using `.pblink` |
| `/pb-optout` | Deletes your linked stats and stops tracking; affects only you |

An unlinked player can opt out in-game. Turning DNT off does not cancel a separate manual opt-out. Use `.pbprivacy optin` to rejoin tracking; old progress and links are not restored.

## Examples and common fixes

`ci give 7102 4` gives player 4 a Cluster Grenade. `bprole set Z04` makes you Explosive Zombie. `bprole set S04 4` makes player 4 Candy Man.

1. **Equipment won’t give:** check ID, enabled state, free inventory space and that the target is alive/unrestrained. Admin grants need no `--force`.
2. **Role won’t roll:** read `bproles`. Disabled/experimental profiles and missing equipment still block admin tests. Setting a role replaces the current kit.
3. **Events:** need four living players. Starting one replaces combat roles/kits. After `bpevent stop`, restart the round for normal roles.
4. **Voice:** players use F1 `.scpchat` or tap N, then hold their normal voice key. Staff can adjust `bpscpchat volume <0-20>` live.
5. **Capture:** cuff enemy military and escort them to the normal surface escape zone. The captor must be alive and the prisoner alive for at least ten seconds.
6. **Locksmith:** interact with a denied keycard door and stay nearby. Level 1/2/3 takes 15/30/45 seconds, then a 120/180/240-second cooldown. Facility lockdowns still apply.
7. **Config edits:** save the YAML, then run the matching `reload` command above. A newly installed or updated DLL needs a full server restart.
