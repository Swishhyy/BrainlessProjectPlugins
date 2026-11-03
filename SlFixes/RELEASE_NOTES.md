# SlFixes 0.5.0

**End-of-round friendly fire:** when a round ends, friendly fire turns on so everyone can attack everyone until the restart, with a short broadcast. The game already pauses its friendly-fire auto-punish detector at round end, so nobody is kicked or banned for it. When the next round starts, friendly fire goes back to the server's own setting. If friendly fire is already on, nothing changes. Custom grenades and weapons hit teammates too while it's on. Configure it with `end_of_round_friendly_fire` (`enabled`, `broadcast_seconds`, `message`).

Built for EXILED 9.15.3 / SCP:SL 14.2.9. Fully restart after replacing DLLs. Needs a live playtest.

## Previous: 0.4.1

Drops SCP-914 recipes for removed items (Bio Grenade, Railgun).

Built for EXILED 9.15.3 / SCP:SL 14.2.9. Fully restart after replacing DLLs. Needs a live playtest.

## Previous: 0.4.0

Facility Guards can escape: an uncuffed guard who reaches the surface escape zone becomes an NTF Sergeant (`guard_escape`). Field Dressing and SCRAMBLE Goggles recipes were removed with the items.

Built for EXILED 9.15.3 / SCP:SL 14.2.9 (the post-Halloween update). Older builds target 14.2.8, so their auto-updater refuses this release: install the DLLs manually once, then fully restart. Needs a live playtest.

## Previous: 0.3.1

Gameplay behavior is unchanged; this build includes the runtime-visibility build guard and clearer updater diagnostics.

All projects now compile against the original game assembly rather than the publicized reference, so unsupported direct private access fails during compilation. Native private patch targets are located by reflection. Updater mismatch messages report both the release target and running SCP:SL version; the exact-version compatibility guard remains.

Built for EXILED 9.15.2 / SCP:SL 14.2.8. Fully restart after replacing DLLs. If the old updater is blocked by a version mismatch, this patch needs a manual download; confirm the actual game version before selecting a differently targeted build. Native smoke/role/SCP-914 behavior still requires live verification.

## Previous: 0.3.0

Adds escorted military recruitment at the normal surface escape zone. A living Chaos captor can convert cuffed MTF/Guards to Chaos Conscript; a living Guard/MTF captor can convert cuffed Chaos to MTF Private. Roles 0.5.0 Rogues can be recruited by either side. Default minimum time alive is ten seconds. Enable state, delay and destination military roles are configurable; ordinary Class-D/Scientist escape rules remain native. Later escape-event handlers can still cancel a conversion.

XP collection now respects Core opt-out and native DNT. Suppressed players are removed from local round-stat ledgers; gameplay/crafting continue without Core. Death receipts use anonymous round-life IDs instead of embedding another player's account ID.

Built for EXILED 9.15.2 / SCP:SL 14.2.8. Fully restart after installing. Capture-policy and privacy checks pass; escorting/cuffing and actual escape-zone behavior still need a live playtest.

## Previous: 0.2.1

Updater compatibility patch: removes DataContract JSON initialization and machine proxy discovery, adds strict metadata parsing and sanitized failure diagnostics, and retains credential isolation, TLS/checksum/assembly validation and atomic replacement. An affected old updater needs one manual DLL replacement and a full restart; existing token YAML remains valid.

Built for EXILED 9.15.2 / SCP:SL 14.2.8. Round Events 0.1.0 adds Infection as a separate optional DLL. Full live hosting/gameplay verification is still required.
