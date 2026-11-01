# Items 0.6.2

Cluster parent and child blasts now preserve native door, destructible-prop and physics damage without adding a second player hit. The player target set is cleared before custom damage; original explosion cancellation and native door immunity/events remain. `tuning.cluster_grenade.damage_environment` defaults to true. Player damage, caps, count and radius are unchanged.

Door Jammer now holds for 25 seconds. Catalog revision 6 upgrades the previous stock eight-second lifetime, preserves edited values and saves the migration with a config backup. Explicit per-item overrides remain authoritative. Already deployed braces keep their captured expiry.

Built for EXILED 9.15.2 / SCP:SL 14.2.8. Fully restart after installing. Native interface/regression checks pass; door/prop destruction and blast coverage still require an in-game test.

## Previous: 0.6.1

Fixes gas, containment foam, bio and echo grenade activation at the early native fuse event. Cancelling the native flash/bang no longer prevents the custom effect. Payloads resolve once, and spent projectiles are cleaned up even if effect creation fails. Native SCP-244 stays enabled for room/fog boundaries while owned growth/decay and server exposure are controlled separately.

Foam now places its field on the floor. Untouched defaults upgrade to an eight-second field with 25% human and 15% SCP slowdown; custom values remain editable. Gas remains smoke only, without primitive fog, a flash, custom slow or damage. Startup saves pending catalog migrations with retained backups.

Natural loot percentages, supply limits, independent automatic updates and SCP-914 crafting remain. Target: EXILED 9.15.2 / SCP:SL 14.2.8. Fully restart to load the DLL; confirm native smoke rendering and field behavior in-game.
