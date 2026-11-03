# Core 0.3.1

New public stats API endpoint: `GET /api/v1/status` returns `{"online":true,"players":8,"maxPlayers":25,"roundStarted":true,"roundSeconds":125,"updated":"…"}`. The Discord bot uses it for its "Watching … 8/25" status and the population alerts, and the website can use it too. The count is captured on the game thread every second; a snapshot older than 30 seconds reports `{"online":false}`. Player counts are public in the server list, so the endpoint needs no secret.

Built for EXILED 9.15.3 / SCP:SL 14.2.9. Fully restart after replacing DLLs.

## Previous: 0.3.0

The Discord bot no longer runs inside the game server. Core now serves a small **stats API** instead, and the bot is a separate free **Cloudflare Worker** ([BrainlessDiscordBot](https://github.com/Swishhyy/BrainlessDiscordBot)).

- Public leaderboard for your website: `GET /api/v1/leaderboard?sort=xp|playtime&limit=1-100`. It lists names, levels, XP and playtime, never Steam IDs, and is cached and rate-limited.
- Private endpoints for the bot (stats, `/pb-link`, `/pb-optout`) are signed with a shared secret (HMAC-SHA256, timestamp and one-time nonce). The secret is never sent, and replayed or altered requests are rejected.
- Put the API behind Cloudflare for HTTPS; [the setup guide](../docs/STATS_API.md) covers the config, DNS and the port rule.
- New `stats_api` config section (off by default). The old `discord:` section is ignored; remove the bot token from backups. Player commands are unchanged.

Built for EXILED 9.15.3 / SCP:SL 14.2.9. Fully restart after replacing DLLs. Needs a live check of the API behind Cloudflare and the Worker bot.

## Previous: 0.2.2

Rebuilt for EXILED 9.15.3 / SCP:SL 14.2.9 (the post-Halloween update). Gameplay is unchanged. Older builds target 14.2.8, so their auto-updater refuses this release: install the DLL manually once, then fully restart.

## Previous: 0.2.1

Gameplay behavior is unchanged; this build includes the runtime-visibility build guard and clearer updater diagnostics.

All projects now compile against the original game assembly rather than the publicized reference, so unsupported direct private access fails during compilation. Native private patch targets are located by reflection. Updater mismatch messages report both the release target and running SCP:SL version; the exact-version compatibility guard remains.

Built for EXILED 9.15.2 / SCP:SL 14.2.8. Fully restart after replacing DLLs. If the old updater is blocked by a version mismatch, this patch needs a manual download; confirm the actual game version before selecting a differently targeted build. Native smoke/role/SCP-914 behavior still requires live verification.

## Previous: 0.2.0

Adds saved server playtime and Discord commands for self/other-player level, XP and playtime, the top 10 by XP, and self opt-out. Existing XP databases migrate without resetting progress. Discord queries require linked accounts; replies stay private.

Adds F1 `.pbprivacy status`, `.pbprivacy optout` and `.pbprivacy optin`. Native game DNT also suppresses collection. Opt-out/DNT remove the stored name, game ID, XP, playtime and Discord link from the database, its backups and plugin-created corrupt archives, and revoke pending link codes. Salted HMAC suppression markers retain the preference across restarts without storing the raw identity. Manual opt-out stays active when DNT is disabled. Explicit opt-in starts fresh and requires DNT off.

Privacy changes serialize with saves and Discord linking, preventing a concurrent save from restoring erased profiles. Playtime includes connected lobby/spectator time and grants no XP. Failure to read privacy preferences disables tracking instead of resetting consent. Gameplay and standalone updates remain independent.

Built for EXILED 9.15.2 / SCP:SL 14.2.8. Fully restart after installing. Storage/concurrency checks pass; the Discord gateway and authenticated DNT behavior need a live test.

## Previous: 0.1.1

Updater compatibility patch: removes DataContract JSON initialization and machine proxy discovery, adds strict metadata parsing and sanitized failure diagnostics, and retains credential isolation, TLS/checksum/assembly validation and atomic replacement. An affected old updater needs one manual DLL replacement and a full restart; existing token YAML remains valid.

Saved XP uses the same Unity-safe JSON codec, reads the previous database JSON format and retains backup/atomic-write protection. Optional Discord HTTP also avoids machine proxy discovery.

Built for EXILED 9.15.2 / SCP:SL 14.2.8. Round Events 0.1.0 adds Infection as a separate optional DLL. Full live hosting/gameplay verification is still required.
