# Round Events 0.3.0

Automatic events are rarer and safer: 3% per round by default (untouched 10% configs migrate), never two rounds in a row, and each needs its own player count: Infection 12, Deathmatch 16, Juggernaut 10 (`automatic_minimum_players`). Staff commands are unchanged.

Built for EXILED 9.15.3 / SCP:SL 14.2.9. Fully restart after replacing DLLs. Needs a live playtest.

## Previous: 0.2.0

Adds two events next to Infection, controlled with `bpevent deathmatch next|start` and `bpevent juggernaut next|start`.

- Deathmatch: Chaos has taken the facility and spawns in Heavy Containment and Entrance; MTF raids from the surface. No SCPs. Both sides respawn in waves (15 s); first to 40 kills, or the leader after 15 minutes, wins.
- Juggernaut: one Chaos Juggernaut in heavy armor (1000 HP + 150 per hunter, heals 75 per kill) against NTF hunters, who are told its zone and room every 30 seconds. Killing the Juggernaut makes you the next one.
- Events lock the warhead and LCZ decontamination and block normal respawn waves while they run. Automatic selection picks one enabled event at random.

Built for EXILED 9.15.3 / SCP:SL 14.2.9 (the post-Halloween update). Older builds target 14.2.8, so their auto-updater refuses this release: install the DLLs manually once, then fully restart. Needs a live playtest.

## Previous: 0.1.1

Gameplay behavior is unchanged; this build includes the runtime-visibility build guard and clearer updater diagnostics.

All projects now compile against the original game assembly rather than the publicized reference, so unsupported direct private access fails during compilation. Native private patch targets are located by reflection. Updater mismatch messages report both the release target and running SCP:SL version; the exact-version compatibility guard remains.

Built for EXILED 9.15.2 / SCP:SL 14.2.8. Fully restart after replacing DLLs. If the old updater is blocked by a version mismatch, this patch needs a manual download; confirm the actual game version before selecting a differently targeted build. Native smoke/role/SCP-914 behavior still requires live verification.

## Previous: 0.1.0

Adds standalone Infection mode: a low-health SCP-3114 swarm, configurable seed counts, survivor loadout, health/shield limits and death-point conversion delay. Only confirmed human deaths from event-owned SCP-3114 slap/strangulation handlers convert; hazards, other attacks and infected deaths do not.

Adds admin list/status/start/next/stop/reload commands under projectbrainless.events. Pending conversions are cancelled by life/session changes and round resets; round endings wait for them. Normal reinforcement/revival attempts, friendly fire and disguises are restricted by default and configurable. Automatic random selection is off until enabled.

Includes the shared updater compatibility fix, verified release manifests and optional Core integration. Initial manual installation is required. Built against EXILED 9.15.2 / SCP:SL 14.2.8; live game playtesting remains necessary.
