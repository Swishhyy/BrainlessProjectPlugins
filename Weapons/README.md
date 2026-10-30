# Brainless Weapons

Install `Brainless.Weapons.dll` in `EXILED/Plugins` on EXILED 9.15.2 / SCP: SL 14.2.8, with EXILED's standard CustomItems module enabled. Roles and Items are optional.

The `brainless_weapons` configuration registers Nullifier, Rattler, Breach Shotgun, Last Word and Grenade Launcher by default. Redline and Ricochet require `allow_experimental_weapons`. Total supply is one firearm per round below 24 players and two at 24+. World loot is disabled by default so roles can use that limited supply; enable it when testing this DLL by itself.

IDs 7201-7207 are stable integration IDs. Reserve rounds are granted once to the first recipient of a minted kit weapon. Pickup transfers never add reserves. Last Word keeps a serial-based six-shot budget. The launcher keeps a serial-based sixteen-grenade budget. SCP-914 turns either into a coin. Other guns lose custom behavior while preserving native serialized ammunition before the recipe runs.

The Grenade Launcher (7207) uses the Logicer LMG body and launches native bouncing grenades instead of dealing hitscan bullet damage. It has 16 shots total, no reserve/reload/unload, and a 0.65-second minimum interval; tap fire to aim each grenade. Default launch speed is 18m/s, fuse 1.8 seconds, blast radius 2.5m, and center damage 45 to humans / 90 to SCPs with linear falloff and normal cover/friendly-fire checks. The grenades do not break doors or apply HE burn/deafen/concussion. Tune the `launcher_*` settings in `brainless_weapons`. Drops, reacquisition, attachments and ordinary 7.62 ammo cannot refill it. Existing configs receive this new default definition once through `catalog_revision`; disable its entry to remove it from availability.

`ci give 7201 [playerID]` and `ci spawn` report confirmed results and ignore natural round supply caps using the ordinary `customitems.give`/`customitems.spawn` permissions. No `--force` flag or extra permission is required. Admin grants do not spend or reset natural supply. Disabled experimental entries still need enabling in configuration. See `docs/COMMANDS.md`.

Independent release updates are enabled under `auto_update`, checking every 60 minutes and applying after a full server restart. Set `BRAINLESS_GITHUB_TOKEN` in the server environment for this private repository. Only this DLL is replaced; the old file is saved as `.previous`. See `docs/UPDATES.md`.

Redline is a disabled heat/vent prototype at the native firing cadence. The design's proposed 20% faster cadence is not implemented pending client prediction validation. Ricochet reflects once from approved solid mesh geometry; it needs focused in-game collision checks.

Build: `dotnet build Weapons/Brainless.Weapons.csproj -c Release` after preparing references.
