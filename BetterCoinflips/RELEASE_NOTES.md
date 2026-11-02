# BetterCoinflips 5.1.9

Rebuilt for EXILED 9.15.3 / SCP:SL 14.2.9 (the post-Halloween update). Gameplay is unchanged. Older builds target 14.2.8, so their auto-updater refuses this release: install the DLL manually once, then fully restart.

## Previous: 5.1.8

Gameplay behavior is unchanged; this build includes the runtime-visibility build guard and clearer updater diagnostics.

All projects now compile against the original game assembly rather than the publicized reference, so unsupported direct private access fails during compilation. Native private patch targets are located by reflection. Updater mismatch messages report both the release target and running SCP:SL version; the exact-version compatibility guard remains.

Built for EXILED 9.15.2 / SCP:SL 14.2.8. Fully restart after replacing DLLs. If the old updater is blocked by a version mismatch, this patch needs a manual download; confirm the actual game version before selecting a differently targeted build. Native smoke/role/SCP-914 behavior still requires live verification.

## Previous: 5.1.7

Updater compatibility patch: removes DataContract JSON initialization and machine proxy discovery, adds strict metadata parsing and sanitized failure diagnostics, and retains credential isolation, TLS/checksum/assembly validation and atomic replacement. An affected old updater needs one manual DLL replacement and a full restart; existing token YAML remains valid.

Built for EXILED 9.15.2 / SCP:SL 14.2.8. Round Events 0.1.0 adds Infection as a separate optional DLL. Full live hosting/gameplay verification is still required.
