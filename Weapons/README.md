# Brainless Weapons

Install `Brainless.Weapons.dll` in `EXILED/Plugins` on EXILED 9.15.2 / SCP: SL 14.2.8, with EXILED's standard CustomItems module enabled. Roles and Items are optional.

The `brainless_weapons` configuration registers Nullifier, Rattler, Breach Shotgun and Last Word by default. Redline and Ricochet require `allow_experimental_weapons`. Total supply is one firearm per round below 24 players and two at 24+. World loot is disabled by default so roles can use that limited supply; enable it when testing this DLL by itself.

IDs 7201-7206 are stable integration IDs. Reserve rounds are granted once to the first recipient of a minted kit weapon. Pickup transfers never add reserves. Last Word keeps a serial-based six-shot budget, and SCP-914 turns it into a coin. Other guns lose custom behavior while preserving native serialized ammunition before the recipe runs.

`ci give` and `ci spawn` report actual confirmed results for Brainless weapons. For deliberate supply-limit testing, use `ci give 7201 [playerID] --force` with **both** `customitems.give` and `brainless.weapons.bypasslimits`. Spawn needs `customitems.spawn` plus the bypass permission. Forced weapons do not spend, reset or refill the natural round supply, and do not unlock disabled experimental entries. See `docs/COMMANDS.md`.

Redline is a disabled heat/vent prototype at the native firing cadence. The design's proposed 20% faster cadence is not implemented pending client prediction validation. Ricochet reflects once from approved solid mesh geometry; it needs focused in-game collision checks.

Build: `dotnet build Weapons/Brainless.Weapons.csproj -c Release` after preparing references.
