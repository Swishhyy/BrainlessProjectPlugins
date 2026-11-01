# SlFixes 0.3.0

Adds escorted military recruitment at the normal surface escape zone. A living Chaos captor can convert cuffed MTF/Guards to Chaos Conscript; a living Guard/MTF captor can convert cuffed Chaos to MTF Private. Roles 0.5.0 Rogues can be recruited by either side. Default minimum time alive is ten seconds. Enable state, delay and destination military roles are configurable; ordinary Class-D/Scientist escape rules remain native. Later escape-event handlers can still cancel a conversion.

XP collection now respects Core opt-out and native DNT. Suppressed players are removed from local round-stat ledgers; gameplay/crafting continue without Core. Death receipts use anonymous round-life IDs instead of embedding another player's account ID.

Built for EXILED 9.15.2 / SCP:SL 14.2.8. Fully restart after installing. Capture-policy and privacy checks pass; escorting/cuffing and actual escape-zone behavior still need a live playtest.

## Previous: 0.2.1

Updater compatibility patch: removes DataContract JSON initialization and machine proxy discovery, adds strict metadata parsing and sanitized failure diagnostics, and retains credential isolation, TLS/checksum/assembly validation and atomic replacement. An affected old updater needs one manual DLL replacement and a full restart; existing token YAML remains valid.

Built for EXILED 9.15.2 / SCP:SL 14.2.8. Round Events 0.1.0 adds Infection as a separate optional DLL. Full live hosting/gameplay verification is still required.
