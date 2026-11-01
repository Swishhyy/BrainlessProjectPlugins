# Project Brainless — Staff Guide

Use **Remote Admin → text console** for staff commands. Do not add `/` or `.`. Replace `<ID>` with the item/role ID. An optional `[playerID]` targets someone else; leave it out to target yourself. From the hosting console, always include a player ID when targeting someone.

## 1. Find and give equipment

| Command | What it does |
| --- | --- |
| `ci list registered` | Lists available custom items and weapons with their IDs. |
| `ci info <ID>` | Shows an item's details. |
| `ci give <ID> [playerID]` | Gives an item or weapon. |
| `ci spawn <ID> <playerID>` | Drops an item at that player's feet. |

Quick examples:

```text
ci give 7101
ci give 7102 4
ci give 7113
ci give 7207
```

These give gas, cluster, impact and grenade launcher. The second example gives a cluster to player 4. Admin grants need no `--force` and ignore natural population/supply limits. The recipient still needs to be alive, unrestrained and have inventory space. Disabled items must be enabled in config before they can be given.

## 2. Check and test roles

| Command | What it does |
| --- | --- |
| `bprole` | Shows your role and stats. |
| `bprole 4` | Checks player 4. |
| `bprole all` | Checks everyone. |
| `bproles` | Lists role IDs, percentages and reasons a role is blocked. |
| `bproles 4` | Checks which roles player 4 could receive. |
| `bprole set Z04 [playerID]` | Tests Explosive Zombie. |
| `bprole set S04 [playerID]` | Tests Candy Man. |
| `bprole set G01 [playerID]` | Tests Guard Captain. |
| `bprole set D06 [playerID]` | Tests Locksmith. |

Setting a role replaces the player's current role/kit. It bypasses random chances and natural supply caps, but still requires the profile's enable switches and equipment providers. `READY` means eligible, not a guaranteed spawn. Tickler/Muncher require experimental roles; Doctor is a separate opt-in event. `bproles` shows the full shipped roster.

## 3. Run Infection

| Command | What it does |
| --- | --- |
| `bpevent list` | Lists available events. |
| `bpevent status` | Checks the active event and player counts. |
| `bpevent infection next` | Queues Infection for the next round. |
| `bpevent infection start` | Starts Infection in the current running round. |
| `bpevent stop` | Stops event rules and cancels pending conversions. |

Infection needs **four living participants** by default. Starting it replaces combat roles/kits. Skeletons start with 150 HP and no shield. Only their direct slap/strangulation kills turn humans into skeletons at the death location; hazards and other deaths do not. Stopping the event leaves current roles in place—restart the round for normal roles. Automatic events are off by default.

## 4. Change settings

Save the YAML first, then use the matching command:

| Plugin | Command |
| --- | --- |
| Roles | `bprole reload` |
| Items | `bpitems reload` |
| Weapons | `bpweapons reload` |
| Fixes / SCP-914 recipes | `pbfixes reload` |
| Round Events | `bpevent reload` while no event is active/scheduled |

Rejected settings leave the previous settings active. Existing role assignments and spent item charges are not reset. Loading a new DLL or changing registered item bodies/enabled states needs a full server restart.

Each role now has **`chance_percent`**. Inside its existing entry, set a number from 0 to 100:

```yaml
chance_percent: 10
```

That role gets a 10% roll on a matching new spawn, subject to its requirements. `0` prevents new natural rolls; admin role testing still works. Add up all percentages within each group: Class-D, Scientists, Guards, MTF, Chaos, SCP-049 or zombies. Each group must total **100 or less**, including disabled/experimental profiles. Doctor's percentage is a separate once-per-round event attempt.

For example, Athletic 10% plus Thief 5%, with every other Class-D profile at 0%, leaves 85% normal Class-D. Blocked roles keep their share as normal spawns; their chance is not handed to another role. Revived zombies keep their existing profile rather than rerolling. Old missing/null fields derive a starting percentage from the old family chance and weights; replace them with numbers to control each role directly. The old family chances/weights do not multiply an explicit percentage. Use `bproles` to see the active values.

## 5. Help players and check services

Players use these in **F1**, with the leading dot and no staff permission:

| Command | What it does |
| --- | --- |
| `.scpchat` | Toggles nearby/team SCP voice; keep holding the normal voice key to talk. |
| `.scpchat status` | Shows voice routing, loaded version and active gain. |
| `.scpchat mute all` / `.scpchat unmute all` | Mutes/enables nearby SCP audio for that listener. |
| `.scpchat mute 4` / `.scpchat unmute 4` | Mutes/unmutes player 4's nearby SCP audio. |
| `.pbxp` / `.pbxp top` | Shows personal stats / leaderboard. |
| `.pblink` | Gets a one-time Discord linking code when the bot is enabled. |

Staff can use `pbxp 4` to inspect player 4, `pbcore status` to check XP/Discord/update services, `pbcore save` to save stats, and `pbfixes status` to check Fixes integration.

If equipment will not give: check its ID, enable state, inventory space and whether the player is alive. If a role will not roll: read its `bproles` reason and percentage. If voice is silent: check `.scpchat status`, nearby reception/mutes and the player's normal voice key; spectators do not hear this channel.

## 6. Staff permissions

Add these to the appropriate staff group's **EXILED/plugin permissions in CedMod**:

| Permission | Access |
| --- | --- |
| `customitems.*` | Item/weapon lists, details, give and spawn. |
| `brainless.roles` | Role inspection, testing and reload. |
| `brainless.items.configure` | Item config reload. |
| `brainless.weapons.configure` | Weapon config reload. |
| `projectbrainless.events` | Event commands. |
| `projectbrainless.core` | Core status/save. |
| `projectbrainless.stats` | Inspect another player's stats. |
| `projectbrainless.fixes` | Fixes status/reload. |

Optional coin debugging: `bc.getserial`, `bc.coinuses.get`, `bc.coinuses.set`. Normal equipment use, role HUD, player voice controls and automatic updates require no staff permission. For additional syntax, see [COMMANDS.md](COMMANDS.md).
