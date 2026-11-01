# Project Brainless — Command Table

[Staff quick guide](STAFF_GUIDE.md) · [Content IDs and usage](CATALOG.md) · [Owner settings](CONFIGURATION.md)

**Remote Admin:** no leading slash/dot. `<value>` is required; `[value]` is optional. Omit `[playerID]` to target yourself in-game.

| Command | What it does | Permission |
| --- | --- | --- |
| `ci list registered` | Lists enabled items/weapons and IDs | `customitems.list.registered` |
| `ci info <ID>` | Shows equipment details | `customitems.info` |
| `ci give <ID> [playerID]` | Gives equipment; ignores natural supply/population caps | `customitems.give` |
| `ci spawn <ID> <playerID>` | Drops equipment at a player | `customitems.spawn` |
| `ci spawn <ID> <x> <y> <z>` | Drops equipment at coordinates | `customitems.spawn` |
| `ci list insideinventories` | Shows tracked equipment and holders | `customitems.list.insideinventories` |
| `bprole [playerID]` | Inspects role/stats; alias `myrole` | `brainless.roles` |
| `bprole all` | Inspects everyone’s roles | `brainless.roles` |
| `bproles [playerID]` | Lists roles, active percentages and blocked reasons | `brainless.roles` |
| `bprole set <roleID> [playerID]` | Assigns a custom profile for testing | `brainless.roles` |
| `bprole reload` | Applies role config edits | `brainless.roles` |
| `bpitems chances` | Shows map/starting-gear item percentages | `brainless.items.configure` |
| `bpitems reload` | Applies item config edits | `brainless.items.configure` |
| `bpweapons chances` | Shows map/starting-gear weapon percentages | `brainless.weapons.configure` |
| `bpweapons reload` | Applies weapon config edits | `brainless.weapons.configure` |
| `bpevent list` | Lists implemented events | `projectbrainless.events` |
| `bpevent status` | Shows event state and counts | `projectbrainless.events` |
| `bpevent infection next` | Queues Infection for next round | `projectbrainless.events` |
| `bpevent infection start` | Starts Infection in the running round | `projectbrainless.events` |
| `bpevent stop` | Stops rules and pending conversions; current roles remain | `projectbrainless.events` |
| `bpevent reload` | Reloads inactive/unscheduled event settings | `projectbrainless.events` |
| `pbcore status` / `pbcore save` | Checks services / saves stats | `projectbrainless.core` |
| `pbfixes status` / `pbfixes reload` | Checks Fixes / applies its config | `projectbrainless.fixes` |
| `pbxp <playerID>` | Inspects another player’s stats | `projectbrainless.stats` |
| `getserial [playerID]` | Shows the held item’s serial | `bc.getserial` |
| `coinuses get player [playerID]` | Checks held coin uses | `bc.coinuses.get` |
| `coinuses get serial <serial>` | Checks a coin by serial | `bc.coinuses.get` |
| `coinuses set player <uses>` | Sets your held coin’s uses | `bc.coinuses.set` |
| `coinuses set player <playerID> <uses>` | Sets another player’s coin uses | `bc.coinuses.set` |
| `coinuses set serial <serial> <uses>` | Sets uses by serial | `bc.coinuses.set` |
| `sci itemlist` / `sci help` | Legacy SUP list/help | Remote Admin access |
| `sci give <ID> [playerID]` | Legacy grant; supports mapped old IDs | `customitems.give` or `sci.admin` |

Add nodes to the staff group’s **EXILED/plugin permissions in CedMod**. `customitems.*` covers the equipment administration rows. No extra bypass permission is needed for admin grants. Disabled content and invalid recipients remain blocked.

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

**Discord, when configured:** `/pb-stats`, `/pb-top`, `/pb-link code:<code>`. Prefix `pb` is the default.
