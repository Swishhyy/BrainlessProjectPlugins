# ScpProximityChat 0.2.2

SCP-939 is excluded from nearby voice, both as speaker and listener. Routing its voice through speakers broke proximity chat and disconnected players; it keeps native SCP team voice.

Built for EXILED 9.15.3 / SCP:SL 14.2.9 (the post-Halloween update). Older builds target 14.2.8, so their auto-updater refuses this release: install the DLLs manually once, then fully restart. Needs a live playtest.

## Previous: 0.2.1

Gameplay behavior is unchanged; this build includes the runtime-visibility build guard and clearer updater diagnostics.

All projects now compile against the original game assembly rather than the publicized reference, so unsupported direct private access fails during compilation. Native private patch targets are located by reflection. Updater mismatch messages report both the release target and running SCP:SL version; the exact-version compatibility guard remains.

Built for EXILED 9.15.2 / SCP:SL 14.2.8. Fully restart after replacing DLLs. If the old updater is blocked by a version mismatch, this patch needs a manual download; confirm the actual game version before selecting a differently targeted build. Native smoke/role/SCP-914 behavior still requires live verification.

## Previous: 0.2.0

Keeps the stronger 10x native speech gain and extends the stock full-volume area from 6m to 12m within the 16m hearing radius. Gain accepts 0-20. Stock distance migration preserves custom or explicitly pinned values and mute settings; migrated fields save with a config backup.

Adds Remote Admin/server-console commands `bpscpchat status`, `bpscpchat volume <0-20>` and `bpscpchat reload`, requiring `brainless.scpchat.configure`. Live gain changes update active speakers immediately; save the chosen volume in YAML to retain it after restart.

Toggle controls, native Opus packets, receiver/mute restrictions, independent updates and the single-DLL installation remain. Target: EXILED 9.15.2 / SCP:SL 14.2.8. Fully restart to load this build; compare the same microphone in-game because client speaker and human voice mixer settings can affect perceived loudness.
