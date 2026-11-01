> Detailed owner notes. For staff, use [the quick guide](../STAFF_GUIDE.md) and [content page](../CATALOG.md). Older version notes below are historical; current spawn percentages are explained in [configuration](../CONFIGURATION.md).

# Project Brainless Core 0.2.0

Shared services for the Project Brainless EXILED plugins: persistent XP/stat storage, an optional in-process Discord bot, account linking and a reusable update service. Framework: EXILED 9.15.2 / SCP:SL 14.2.8 / net48.

Install `Brainless.Core.dll` in EXILED/Plugins and fully restart. No separate Discord package is required. The bot uses the runtime's HTTP/WebSocket support and the serializer already supplied by EXILED. Existing gameplay plugins have no hard Core assembly dependency: they use the shared updater when Core is enabled and fall back to their standalone implementation at startup when it is absent. SlFixes supplies the gameplay action tracking; its SCP-914 features also work without Core.

## Configuration

Under `project_brainless_core` (or its separated plugin file):

```yaml
is_enabled: true
debug: false
auto_update:
  enabled: true
  token: "PASTE_READ_ONLY_GITHUB_TOKEN_HERE"
  token_environment_variable: ""
  check_interval_minutes: 60
  include_prereleases: false
  releases_to_scan: 30
data_directory: ""
save_interval_seconds: 30
xp_per_level_squared: 100
max_xp_per_player_per_round: 500
discord:
  enabled: false
  token: ""
  token_environment_variable: BRAINLESS_DISCORD_TOKEN
  guild_id: ""
  command_prefix: pb
```

Empty `data_directory` saves under `EXILED/Configs/ProjectBrainless/<port>/stats.json`, with atomic writes and a `.backup`. A damaged main file is preserved and recovered from a readable backup. If both are unreadable, stats are disabled rather than silently reset; gameplay/update services remain available. Autosave defaults to 30 seconds and a normal shutdown saves again. A process crash may lose changes since the last save. Use one server process per data directory.

A Core GitHub token can supply registered gameplay updaters when their own `auto_update.token` is empty. Existing per-plugin tokens take precedence. Updates still replace only the owning DLL and activate on a full process restart. Core removal/hot reload requires a full restart to reselect updater ownership; game features remain isolated.

## XP and commands

SlFixes records confirmed enemy kills, deaths, escapes, successful custom crafting/rough teleports and round participation/survival. Its config controls each XP reward and limits repeated crafting/teleport rewards. Core caps XP per player per round while retaining action counts. Levels follow `floor(sqrt(total_xp / xp_per_level_squared))`; they provide no combat advantages.

| Where | Command | Permission |
| --- | --- | --- |
| F1 | `.pbxp` / `.pbstats` | Your own stats; everyone |
| F1 | `.pbxp top` | Leaderboard; everyone |
| F1 | `.pblink` | Private one-time linking code; everyone |
| F1 | `.pbprivacy [status\|optout\|optin]` | Your own tracking preference; everyone |
| Remote Admin | `pbxp <playerID>` | `projectbrainless.stats` |
| Remote Admin | `pbcore status` / `pbcore save` | `projectbrainless.core` |

Codes last ten minutes, can be used once, and are invalidated when another code is requested. They are not persisted. Discord links are persisted. Linking never grants Remote Admin permissions, badges or paid privileges; CedMod continues managing those.

## Host the Discord bot

1. Create your own bot/application in the [Discord Developer Portal](https://discord.com/developers/applications).
2. Invite it to your community server with the `bot` and `applications.commands` scopes. It does not need Administrator, Message Content or Server Members intents.
3. Put its **Discord bot token** in `discord.token`, put your Discord server's numeric ID in `discord.guild_id`, and set `discord.enabled: true`. This token is separate from the read-only GitHub token. Keep both out of shared screenshots/config uploads.
4. Fully restart and inspect `pbcore status`. The optional bot runs inside the SCP server process, so it is offline when that process stops.
5. In-game, run `.pblink`. In Discord use `/pb-link code:<your code>`, then `/pb-stats`, `/pb-playtime` or `/pb-top`. Add the optional `user` selector to stats/playtime to inspect another linked player. `/pb-optout` erases only your own linked game profile.

The default `pb` command prefix is configurable. With a guild ID, commands are registered only there; an empty ID registers global commands, which can take longer to appear. Registration upserts only its five commands and does not delete other application commands. The top command returns ten profiles by XP. Replies are private, mention parsing is disabled, API tokens/response bodies are not logged, and reconnect/heartbeat logic runs away from the game thread. Live gateway connectivity needs your own application/token; it cannot be validated by the headless tests.

## Playtime and privacy

Playtime counts connected time, including lobby and spectator time, using a monotonic server clock. It grants no XP and starts when Core is installed; old databases retain XP and start at zero playtime. Saves and shutdown follow the same schedule as XP. A crash may lose the unsaved interval.

Native DNT and F1 `.pbprivacy optout` suppress all Project Brainless XP, action and playtime collection. Suppression erases the profile's name, game ID, XP, action counts, playtime and Discord link from the main database, its backup and plugin-created corrupt archives. Pending link codes and in-memory round receipts are revoked. SlFixes also clears its local tracking ledgers. The bot excludes suppressed profiles from stats, playtime and leaderboard queries.

To remember the preference without retaining the raw game ID, `stats.json.privacy` stores a random HMAC key and salted suppression markers, with a matching backup. Preserve both files during normal maintenance. Unreadable privacy files disable tracking instead of resetting consent. Changes serialize with saves/linking, and saved suppression is applied before recovering old stat rows. Plugin erasure does not modify host-managed backups or other plugins' logs.

DNT suppression remains until the game authenticates that account with DNT off. A separate manual opt-out stays in effect. F1 `.pbprivacy optin` requires DNT off and starts new progress; erased stats and links are not recovered. Discord `/pb-optout` requires an existing link; an unlinked player opts out in-game. These privacy commands never target another account.

## Developer API

Reference `Brainless.Core.dll` for the public API, or use an optional reflection bridge as SlFixes does. On the game thread, `Api.CanTrack(userId)` checks live native DNT and persisted preferences; `Api.RecordAction(userId, name, action, xp, eventId)` applies that gate and records a deduplicated action. Use a stable per-event receipt to prevent duplicates. `GetPlayer(userId)` and `Leaderboard(count)` return snapshots and exclude suppressed profiles. `Api.IsReady` reports stats availability. Background bot/save work uses the thread-safe store, which retains no game objects. No network listener/public HTTP API is opened.
