# Brainless Items

Install `Brainless.Items.dll` in `EXILED/Plugins` on EXILED 9.15.2 / SCP: SL 14.2.8, with EXILED's standard CustomItems module enabled. Roles and Weapons are optional.

The `brainless_items` configuration contains the 11 item definitions and their per-round caps. IDs 7101-7109 and 7111-7112 are stable integration IDs; 7110 is retired. Total supply is 6 / 10 / 14 utilities at fewer than 12 / 12-23 / 24+ players. A gas grenade reserves an additional filter. Cluster supply is 0 / 1 / 2 by the same bands.

Gas (7101) uses the native SCP-244 fog/particles for eight seconds, with a four-meter radius, no damage ticks and a 20% human slow. Its native server cold exposure is suppressed only for these custom emitters; ordinary SCP-244 pickups keep their behavior. A filter blocks the custom slow, while the world smoke still obstructs sight. No primitive renders the cloud.

Cluster (7102) releases four real HE projectiles at the first detonation. They launch outward/upward, bounce and explode after 1.5-1.74 seconds. There are no marker spheres and no recursive child clusters. Their smaller 2.5-meter blasts share a per-target ceiling of 75 human / 200 SCP damage across all four children; the parent releases them without native HE damage.

Use native throw/consume controls. The server-specific **Use custom item or nearby device** key defaults to **V**. Hold it for a channel. Look at a device to scan/remove it, look at a closed ordinary door for a brace, or use it while holding a deployable. Zombies look at a dropped specimen and hold the same key. The binding is editable in server-specific settings.

Maintenance Pass (7110) has been removed. Existing generated item lists are filtered on enable, so an old YAML entry cannot register or grant it. The remaining items keep their IDs. Locksmith's replacement door-picking ability is entirely in the Roles DLL and consumes no item or utility supply.

`ci give 7102 [playerID]` gives a cluster even below 12 players with the ordinary `customitems.give` permission. `ci spawn` uses `customitems.spawn`. Admin commands ignore natural supply caps and never spend that supply; gas includes its paired filter. No `--force` flag or extra permission is required. Results report actual deliveries and inventory/recipient failures. Natural role kits and world loot retain population and supply caps. See `docs/COMMANDS.md`.

Independent release updates are enabled under `auto_update`, checking every 60 minutes and applying after a full server restart. Set `BRAINLESS_GITHUB_TOKEN` in the server environment for this private repository. Only this DLL is replaced; the old file is saved as `.previous`. See `docs/UPDATES.md`.

Charges survive looting. Custom coin tools are registered so BetterCoinflips ignores them. The specimen looks like a tagged SCP-500 pickup but cannot be consumed as a cure. It normalizes to a coin at SCP-914.

The jammer blocks room-level actions, not zone blackout. Echo uses synthetic spatial footsteps without creating SCP-939 perception markers. Test map geometry and audio on the live game before expanding the enabled selection.

Build: `dotnet build Items/Brainless.Items.csproj -c Release` after preparing references.
