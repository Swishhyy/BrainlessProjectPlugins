# Server playtest checklist

Build checks cannot verify client prediction, map collision or a real reinforcement wave. Use a separate test server on EXILED 9.15.2 / SCP: SL 14.2.8.

1. Install each DLL on its own. Then install all four. Verify the expected configuration section and a clean enable log. Remove Items or Weapons, restart, and confirm equipment-dependent roles skip their assignment while other roles continue.
2. Try round start, escape, death, native zombie resurrection, role changes, disconnect/reconnect and round reset. Verify base factions, cards, scale, health and stamina. Confirm a revived zombie keeps its variant without rerolling or regaining a corpse burst.
3. Use CustomItems' Remote Admin commands to issue test IDs from `CATALOG.md`; the same supply caps apply. Test the cap at 11, 12, 23 and 24 players or raise caps deliberately for a controlled mechanic test. Do not confuse a supply refusal with a failed plugin.
4. Fire all six Last Word shots. Attempt reload, unload, attachment changes, a drop, inventory swap and SCP-914 processing. No transfer should regenerate the six-shot budget, and SCP-914 should normalize it into a coin.
5. Test Nullifier against full shield, exact shield break, partial shield and no shield. Test humans with each armor type. Confirm each shot applies damage once and that armor/headshot handling remains native.
6. Shoot ordinary doors with Breach Shotgun. Count six shells across three firing cycles and verify no pellet double-counting. Test excluded gates, checkpoints, elevators, armories, locked objective doors and broken doors.
7. Throw each grenade beside a wall, closed door, another floor and an elevator. Gas/foam must stay in the origin room and respect cover. Clusters must never become native full-strength HE blasts or damage protected doors. Leave cover after the warning and verify the combined damage cap.
8. Have the thrower die or disconnect during a fuse. Verify the source faction remains the captured faction and that a new round cannot receive old delayed damage. Kill Explosive with a player, an environmental hazard and another corpse burst; only its eligible enemy combat death may arm it.
9. Test the tool key and normal item controls. Verify an interrupted channel does not consume a supply. Test door brace removal by humans and SCPs, SCP-106 traversal, maintenance pass refusal on excluded doors, beacon scans, device destruction, chalk expiry, dressing and filter behavior.
10. Use a jammer against SCP-079's ordinary door, room blackout, room lockdown and speaker actions. Existing locks must remain. Test room cooldown and the global active-device limit. Zone blackout is an explicitly unblocked action in this release.
11. Confirm Echo and Rattler sounds are spatial and readable. A beacon excludes its operator. A specimen heals only a consenting SCP-049-2 once per round; humans cannot use it as SCP-500. Test several players contesting the same specimen/device.
12. Flip normal coins with all effect weights zero, a one-use coin, a dropped coin, an inventory swap and a role-changing effect. The original coin serial must be consumed; the new held item must remain intact. Tagged chalk/brace coins must never trigger gambling effects. Repeat across two rounds to check spawn counters.
13. Test every experimental feature individually. Keep Redline's faster cadence out of scope until supported on the client. Verify Ricochet's single bounce, solid cover, armor, self-collision, headshot restriction and shooter attribution before enabling it in normal rounds.

Keep a record of server version, enabled entries, player count, cause of death, custom damage and exceptions. The initial rollout should use a smaller selection than the full catalog.
