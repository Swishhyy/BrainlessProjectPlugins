# Core 0.2.2

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
