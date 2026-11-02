> Detailed owner notes. For staff, use [the quick guide](../STAFF_GUIDE.md) and [content page](../CATALOG.md). Older version notes below are historical; current spawn percentages are explained in [configuration](../CONFIGURATION.md).

# Project Brainless Core 0.3.0

Shared services for the Project Brainless EXILED plugins: persistent XP/stat storage, a stats API (website leaderboard and Discord bot), account linking and a reusable update service. Framework: EXILED 9.15.3 / SCP:SL 14.2.9 / net48.

Install `Brainless.Core.dll` in EXILED/Plugins and fully restart. The stats API uses the runtime's built-in HTTP listener; no extra package is required. Existing gameplay plugins have no hard Core assembly dependency: they use the shared updater when Core is enabled and fall back to their standalone implementation at startup when it is absent. SlFixes supplies the gameplay action tracking; its SCP-914 features also work without Core.

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
stats_api:
  enabled: false
  port: 0
  bind_host: '*'
  shared_secret: ""
  shared_secret_environment_variable: BRAINLESS_API_SECRET
  allowed_origin: '*'
  trust_cloudflare_headers: true
  requests_per_minute: 60
  leaderboard_cache_seconds: 30
  discord_command_prefix: pb
```

The old `discord:` section (0.2.x in-process bot) is ignored and dropped from the file on the next save; remove the bot token from any backups.

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

## Stats API and Discord bot

Core 0.3.0 no longer runs a Discord bot inside the game server. It serves a small HTTP API instead: a public leaderboard for your website, plus private endpoints the Discord bot uses for stats, linking and opt-out. The bot is a free Cloudflare Worker in the [`discord-bot`](../../discord-bot) folder, and Cloudflare also provides HTTPS for the API. Follow [its README](../../discord-bot/README.md) for the full setup (game server → Cloudflare → Discord).

- Public: `GET /api/v1/health`, `GET /api/v1/leaderboard?sort=xp|playtime&limit=1-100` (names, levels, XP and playtime only; never Steam IDs).
- Signed (bot only): `GET /api/v1/discord/{id}`, `POST /api/v1/discord/{id}/link?code=`, `POST /api/v1/discord/{id}/optout`. Requests are HMAC-SHA256 signed with `shared_secret` plus a timestamp and one-time nonce. The secret is never transmitted, and old or repeated requests are rejected.
- Without a `shared_secret` of at least 32 characters, only the public endpoints run and `.pblink` reports that linking is not set up.
- Per-IP rate limiting (`requests_per_minute`, using Cloudflare's `CF-Connecting-IP` when `trust_cloudflare_headers` is on), a 32-request concurrency cap, and leaderboard caching keep load off the game thread. All requests are handled on background threads.

In Discord, `/pb-link code:<code>` links the account from `.pblink`; `/pb-stats`, `/pb-playtime` (optionally for another linked `user`), `/pb-top` and `/pb-optout` work as before. Replies are private and mentions are disabled.

## Playtime and privacy

Playtime counts connected time, including lobby and spectator time, using a monotonic server clock. It grants no XP and starts when Core is installed; old databases retain XP and start at zero playtime. Saves and shutdown follow the same schedule as XP. A crash may lose the unsaved interval.

Native DNT and F1 `.pbprivacy optout` suppress all Project Brainless XP, action and playtime collection. Suppression erases the profile's name, game ID, XP, action counts, playtime and Discord link from the main database, its backup and plugin-created corrupt archives. Pending link codes and in-memory round receipts are revoked. SlFixes also clears its local tracking ledgers. The bot excludes suppressed profiles from stats, playtime and leaderboard queries.

To remember the preference without retaining the raw game ID, `stats.json.privacy` stores a random HMAC key and salted suppression markers, with a matching backup. Preserve both files during normal maintenance. Unreadable privacy files disable tracking instead of resetting consent. Changes serialize with saves/linking, and saved suppression is applied before recovering old stat rows. Plugin erasure does not modify host-managed backups or other plugins' logs.

DNT suppression remains until the game authenticates that account with DNT off. A separate manual opt-out stays in effect. F1 `.pbprivacy optin` requires DNT off and starts new progress; erased stats and links are not recovered. Discord `/pb-optout` requires an existing link; an unlinked player opts out in-game. These privacy commands never target another account.

## Developer API

Reference `Brainless.Core.dll` for the public API, or use an optional reflection bridge as SlFixes does. On the game thread, `Api.CanTrack(userId)` checks live native DNT and persisted preferences; `Api.RecordAction(userId, name, action, xp, eventId)` applies that gate and records a deduplicated action. Use a stable per-event receipt to prevent duplicates. `GetPlayer(userId)` and `Leaderboard(count)` return snapshots and exclude suppressed profiles. `Api.IsReady` reports stats availability. Background bot/save work uses the thread-safe store, which retains no game objects. No network listener/public HTTP API is opened.
