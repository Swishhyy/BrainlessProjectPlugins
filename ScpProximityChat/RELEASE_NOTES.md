# ScpProximityChat 0.2.0

Keeps the stronger 10x native speech gain and extends the stock full-volume area from 6m to 12m within the 16m hearing radius. Gain accepts 0-20. Stock distance migration preserves custom or explicitly pinned values and mute settings; migrated fields save with a config backup.

Adds Remote Admin/server-console commands `bpscpchat status`, `bpscpchat volume <0-20>` and `bpscpchat reload`, requiring `brainless.scpchat.configure`. Live gain changes update active speakers immediately; save the chosen volume in YAML to retain it after restart.

Toggle controls, native Opus packets, receiver/mute restrictions, independent updates and the single-DLL installation remain. Target: EXILED 9.15.2 / SCP:SL 14.2.8. Fully restart to load this build; compare the same microphone in-game because client speaker and human voice mixer settings can affect perceived loudness.
