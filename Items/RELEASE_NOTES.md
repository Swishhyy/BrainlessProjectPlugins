# Items 0.7.0

Removes Survey Beacon (7107), Trace Chalk (7108), Field Dressing (7109), Preserved Specimen (7112) and SCRAMBLE Goggles (7121), including their loot entries, recipes and tuning. Existing configs drop them on load (catalog revision 7).

- Echo Canister plays the game's own human footstep sounds (read from the character model's footstep clips) at an uneven walking pace. If a server build has no readable audio data, it logs a warning and uses generated boot steps instead.
- Signal Jammer now stops SCP-079 properly: every door touching the jammed room (not only ordinary doors), its cameras, tesla gates, pings and elevators. An SCP-079 already watching a jammed room loses its signal. The stock active time is 30 seconds instead of 6.

Built for EXILED 9.15.3 / SCP:SL 14.2.9 (the post-Halloween update). Older builds target 14.2.8, so their auto-updater refuses this release: install the DLLs manually once, then fully restart. Needs a live playtest.

## Previous: 0.6.3

Fixes smoke and Bio Grenade creation by using EXILED's public SCP-244 size setter in both spawn and maintenance. Native private SCP-914 normalization calls use cached reflection. Cluster environment damage and the 25-second Door Jammer remain.

All projects now compile against the original game assembly rather than the publicized reference, so unsupported direct private access fails during compilation. Native private patch targets are located by reflection. Updater mismatch messages report both the release target and running SCP:SL version; the exact-version compatibility guard remains.

Built for EXILED 9.15.2 / SCP:SL 14.2.8. Fully restart after replacing DLLs. If the old updater is blocked by a version mismatch, this patch needs a manual download; confirm the actual game version before selecting a differently targeted build. Native smoke/role/SCP-914 behavior still requires live verification.

## Previous: 0.6.2

Cluster parent and child blasts now preserve native door, destructible-prop and physics damage without adding a second player hit. The player target set is cleared before custom damage; original explosion cancellation and native door immunity/events remain. `tuning.cluster_grenade.damage_environment` defaults to true. Player damage, caps, count and radius are unchanged.

Door Jammer now holds for 25 seconds. Catalog revision 6 upgrades the previous stock eight-second lifetime, preserves edited values and saves the migration with a config backup. Explicit per-item overrides remain authoritative. Already deployed braces keep their captured expiry.

Built for EXILED 9.15.2 / SCP:SL 14.2.8. Fully restart after installing. Native interface/regression checks pass; door/prop destruction and blast coverage still require an in-game test.

## Previous: 0.6.1

Fixes gas, containment foam, bio and echo grenade activation at the early native fuse event. Cancelling the native flash/bang no longer prevents the custom effect. Payloads resolve once, and spent projectiles are cleaned up even if effect creation fails. Native SCP-244 stays enabled for room/fog boundaries while owned growth/decay and server exposure are controlled separately.

Foam now places its field on the floor. Untouched defaults upgrade to an eight-second field with 25% human and 15% SCP slowdown; custom values remain editable. Gas remains smoke only, without primitive fog, a flash, custom slow or damage. Startup saves pending catalog migrations with retained backups.

Natural loot percentages, supply limits, independent automatic updates and SCP-914 crafting remain. Target: EXILED 9.15.2 / SCP:SL 14.2.8. Fully restart to load the DLL; confirm native smoke rendering and field behavior in-game.
