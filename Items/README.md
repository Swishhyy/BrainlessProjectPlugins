# Brainless Items

Install `Brainless.Items.dll` in `EXILED/Plugins` on EXILED 9.15.2 / SCP: SL 14.2.8, with EXILED's standard CustomItems module enabled. Roles and Weapons are optional.

The `brainless_items` configuration contains the 12 item definitions and their per-round caps. IDs 7101-7112 are stable integration IDs. Total supply is 6 / 10 / 14 utilities at fewer than 12 / 12-23 / 24+ players. A gas grenade reserves an additional filter. Cluster supply is 0 / 1 / 2 by the same bands.

Use native throw/consume controls. The server-specific **Use custom item or nearby device** key defaults to **V**. Hold it for a channel. Look at a device to scan/remove it, look at a closed ordinary door for a brace/pass, or use it while holding a deployable. Zombies look at a dropped specimen and hold the same key. The binding is editable in server-specific settings.

Charges survive looting. Custom coin tools are registered so BetterCoinflips ignores them. The specimen looks like a tagged SCP-500 pickup but cannot be consumed as a cure. It normalizes to a coin at SCP-914.

The jammer blocks room-level actions, not zone blackout. Echo uses synthetic spatial footsteps without creating SCP-939 perception markers. Test map geometry and audio on the live game before expanding the enabled selection.

Build: `dotnet build Items/Brainless.Items.csproj -c Release` after preparing references.
