# Roles 0.5.1

Fixes private access in SCP-173 breakneck refresh and SCP-096 rage timing, and uses the public SCP-106 vigor API. Native CustomInfo validation now checks role-name-only overhead labels; IDs remain in the HUD/admin output. Guard-card SCP-914 normalization uses cached reflection for native private upgrade methods.

All projects now compile against the original game assembly rather than the publicized reference, so unsupported direct private access fails during compilation. Native private patch targets are located by reflection. Updater mismatch messages report both the release target and running SCP:SL version; the exact-version compatibility guard remains.

Built for EXILED 9.15.2 / SCP:SL 14.2.8. Fully restart after replacing DLLs. If the old updater is blocked by a version mismatch, this patch needs a manual download; confirm the actual game version before selecting a differently targeted build. Native smoke/role/SCP-914 behavior still requires live verification.

## Previous: 0.5.0

Adds Cowboy (C05), Rogue Guard (G07), Containment Engineer (S07) and Rogue Scientist (S08), bringing the catalog to 53 profiles. S03 is now Head Scientist with the native research-coordinator card. Cowboy replaces the Chaos kit with a loaded revolver, full light-armor reserve ammo, two adrenaline shots and painkillers.

Rogues are neutral, hostile to all factions and retain their Guard/Scientist appearance. They spawn separately in Entrance and trigger one early CASSIE warning. Both can be detained and recruited at the surface escape zone through SlFixes 0.3.0. The Engineer starts in the HCZ Micro room. Spawns require a clear, supported location.

Locksmith now picks level-three keycard doors too. Default level 1/2/3 times are 15/30/45 seconds, with 120/180/240-second cooldowns; facility locks and normal cancellation still apply. All four timing fields are configurable. Revision 2 appends the new roles once while preserving existing percentage pools and explicit later removals. Replacement kits correctly handle full inventories.

Built for EXILED 9.15.2 / SCP:SL 14.2.8. Fully restart after installing. Headless/native-contract checks pass; faction damage, cuffing, CASSIE and safe spawn geometry still require a live playtest.

## Previous: 0.4.0

Adds the fourteen SCP-106, SCP-173, SCP-939 and SCP-096 variants: 49 profiles total. Each profile has editable traits for shield recovery, vigor, quiet footsteps, poison, rage timing, calm movement, mimic sounds, intercom access and breakneck refresh. Poison refreshes one bounded effect and medical supplies cure it. Brute, Giant and Muncher now apply their advertised model sizes; experimental gates remain.

Moves the default HUD inset from 3% to 0.5%. Adds separate Boom Boom death-burst tuning and preserves direct per-role percentages. New profiles append once; custom SCP chance pools remain unchanged. Startup saves migrated fields with retained backups and preserves peer config sections verbatim.

The staff catalog and command tables cover all profiles with short descriptions. Target: EXILED 9.15.2 / SCP:SL 14.2.8. Load the DLL with a full server restart; native movement, HUD and ability behavior still need an in-game playtest.
