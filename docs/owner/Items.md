> Detailed owner notes. For staff, use [the quick guide](../STAFF_GUIDE.md) and [content page](../CATALOG.md). Older version notes below are historical; current spawn percentages are explained in [configuration](../CONFIGURATION.md).

# Brainless Items

Install `Brainless.Items.dll` in `EXILED/Plugins` on EXILED 9.15.2 / SCP: SL 14.2.8, with EXILED's standard CustomItems module enabled. Roles and Weapons are optional.

The `brainless_items` configuration contains the 16 item definitions and their per-round caps. IDs 7101-7106, 7111, 7113-7120 and 7122 are stable integration IDs; 7107-7110, 7112 and 7121 are retired (Survey Beacon, Trace Chalk, Field Dressing, Preserved Specimen and SCRAMBLE Goggles were removed in 0.7.0). Total supply is 6 / 10 / 14 utilities at fewer than 12 / 12-23 / 24+ players. Smoke occupies one utility slot and creates no filter. Cluster supply is 0 / 1 / 2 by the same bands.

Smoke (7101) uses a flashbang body and full-size native SCP-244 fog. It applies no flash, blast damage or custom slow. One emitter lasts 20 seconds by default; duration and emitter count are configurable. It needs no paired filter or extra inventory slot. Native client fog remains; server cold exposure from owned clouds is excluded. Ordinary SCP-244 keeps its behavior except while a player deliberately uses a Filter Inhaler.

Cluster (7102) has a parent blast (180 human / 500 SCP peak, 6m radius) and launches four real HE children outward/upward. Children peak at 120 human / 300 SCP over 5.5m, with 0.9-1.08s fuses. Parent and children share a per-target raw damage cap of 300 human / 1200 SCP. They never recursively split. Parent and children now preserve native door, destructible-prop and physics damage within their own radius; `tuning.cluster_grenade.damage_environment: false` disables that path. Player damage and shared caps stay separate.

Use native throw/consume controls. The server-specific **Use custom item or nearby device** key defaults to **V**. Hold it for a channel. Look at a device to scan/remove it, look at a closed ordinary door for a brace, or use it while holding a deployable. Zombies look at a dropped specimen and hold the same key. The binding is editable in server-specific settings.

Maintenance Pass (7110) has been removed. Existing generated item lists are filtered on enable, so an old YAML entry cannot register or grant it. The remaining items keep their IDs. Locksmith's replacement door-picking ability is entirely in the Roles DLL and consumes no item or utility supply.

`ci give 7102 [playerID]` gives a cluster even below 12 players with the ordinary `customitems.give` permission. `ci spawn` uses `customitems.spawn`. Admin commands ignore natural supply caps and never spend that supply; smoke needs only one inventory slot. No `--force` flag or extra permission is required. Results report actual deliveries and inventory/recipient failures. Natural role kits and world loot retain population and supply caps. See `docs/COMMANDS.md`.

Independent release updates are enabled under `auto_update`, checking every 60 minutes and applying after a full server restart. Paste a read-only GitHub token into `auto_update.token` in this plugin's YAML for this private repository; `BRAINLESS_GITHUB_TOKEN` remains an optional environment fallback. Only this DLL is replaced; the old file is saved as `.previous`. See `docs/UPDATES.md`.

Charges survive looting. Custom coin tools are registered so BetterCoinflips ignores them. The specimen looks like a tagged SCP-500 pickup but cannot be consumed as a cure. It normalizes to a coin at SCP-914.

The jammer blocks room-level actions, not zone blackout. Echo uses synthetic spatial footsteps without creating SCP-939 perception markers. Test map geometry and audio on the live game before expanding the enabled selection.

Build: `dotnet build Items/Brainless.Items.csproj -c Release` after preparing references.

Impact Grenade (7113) uses a native HE body, detonates on surface contact after 0.1 seconds, and has a 10-second fallback fuse. Its default 5m blast peaks at 140 human / 350 SCP damage before native mitigation. Thrower collisions are ignored; player impacts are optional. Existing lists receive 7113 when `catalog_revision` is 0/missing. Set `catalog_revision: 1` when deliberately removing it.

Every item has its own section under `tuning`, plus list fields for name/description, charges, weight and round cap. Counts, damage, slows, radius, duration, fuses, deploy/remove/use times and supply settings are editable. Run `bpitems reload` with `brainless.items.configure` after saving. Registration changes require a restart. Existing fields/projectiles/charges keep their state. See the packaged `TUNING.md` for the complete reference and native smoke size limits.

Version 0.3.0 enables map/locker replacements and occasional starting gear through `distribution`. Defaults: 25% per eligible pickup, at most 3 replacements, one bundle per room, and a 30% player roll with at most 3 successful grants per round / 1 per user. Class-D can receive chalk, a brace or dressing; other human kits can have native medicine/grenades converted. Optional `world_loot` extra bundles now scatter at real loose-loot locations instead of a single armory origin. All routes share existing supply caps; configured counts are maxima, not guarantees. See packaged `NATURAL_LOOT.md`.

Custom explosions now pass valid native armor penetration (50 by default), fixing a zero-damage setup bug. Stock grenade values from older configs migrate to the stronger preset when `grenade_balance_revision` is 0/missing; edited values are retained. Set that marker to 1 before deliberately restoring old numbers. All parent/child damage, caps, radius, spread, fuse and penetration settings remain configurable.

Version 0.3.1 accepts a read-only GitHub token directly in `auto_update.token`; no hosting-panel environment setup is required. A nonempty config token takes precedence over the optional environment fallback. Install this DLL manually once if the old updater cannot authenticate, then fully restart. Subsequent updates retain checksum/assembly checks and `.previous` rollback backups.

Version 0.3.2 turns Door Jammer 7105 into a keycard, migrates old stock coin definitions/distribution, strengthens the cluster parent/children while retaining four children and finite caps, and raises custom audio gain/full-volume distance. General cues use `tuning.audio`; Echo keeps its separate audio fields. Old stock settings migrate without resetting edited values. See `TUNING.md`.

Version 0.5.1 simplifies gas to native smoke only, migrates the old HE body and matching loot rules to flashbangs, and exposes confirmed in-place SCP-914 crafting adapters for SlFixes. Optional Core can host this DLL's update checks; standalone operation remains supported.

SUP content 7114–7122 is documented in [SUP_MIGRATION.md](https://github.com/Swishhyy/BrainlessProjectPlugins/blob/main/docs/SUP_MIGRATION.md). New items join the existing supply and distribution ledgers. `sci itemlist`, `sci give <ID> [playerID]` and `sci help` provide compatibility without webhook logging. Optional `individual_item_files: true` stores catalog/tuning overrides in `EXILED/Configs/ProjectBrainless/<port>/Items/<ID>.yml`; existing files are never overwritten. `bpitems reload` validates these overrides on a detached configuration before applying them. Enabled/ID/body registration changes still require a full restart.

Items 0.6.2 raises Door Jammer's stock hold time to 25 seconds. Catalog revision 6 migrates an untouched eight-second main-config value and preserves custom lifetimes. Explicit per-item file overrides remain authoritative. Deployment/removal remain two/three seconds.
