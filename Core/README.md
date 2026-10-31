# Project Brainless Core 0.1.0

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
| Remote Admin | `pbxp <playerID>` | `projectbrainless.stats` |
| Remote Admin | `pbcore status` / `pbcore save` | `projectbrainless.core` |

Codes last ten minutes, can be used once, and are invalidated when another code is requested. They are not persisted. Discord links are persisted. Linking never grants Remote Admin permissions, badges or paid privileges; CedMod continues managing those.

## Host the Discord bot

1. Create your own bot/application in the [Discord Developer Portal](https://discord.com/developers/applications).
2. Invite it to your community server with the `bot` and `applications.commands` scopes. It does not need Administrator, Message Content or Server Members intents.
3. Put its **Discord bot token** in `discord.token`, put your Discord server's numeric ID in `discord.guild_id`, and set `discord.enabled: true`. This token is separate from the read-only GitHub token. Keep both out of shared screenshots/config uploads.
4. Fully restart and inspect `pbcore status`. The optional bot runs inside the SCP server process, so it is offline when that process stops.
5. In-game, run `.pblink`. In Discord use `/pb-link code:<your code>`, then `/pb-stats` or `/pb-top`.

The default `pb` command prefix is configurable. With a guild ID, commands are registered only there; an empty ID registers global commands, which can take longer to appear. Registration upserts only its three commands and does not delete other application commands. Replies are private, mention parsing is disabled, API tokens/response bodies are not logged, and reconnect/heartbeat logic runs away from the game thread. Live gateway connectivity needs your own application/token; it cannot be validated by the headless tests.

## Developer API

Reference `Brainless.Core.dll` for the public API, or use an optional reflection bridge as SlFixes does. `Brainless.Core.Api.RecordAction(userId, name, action, xp, eventId)` records a deduplicated action; use a stable per-event receipt to prevent duplicates. `GetPlayer(userId)` and `Leaderboard(count)` return snapshots. `Api.IsReady` reports stats availability. The store is thread-safe and does not retain game objects. No network listener/public HTTP API is opened.
