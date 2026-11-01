> Detailed owner notes. For staff, use [the quick guide](../STAFF_GUIDE.md) and [content page](../CATALOG.md). Older version notes below are historical; current spawn percentages are explained in [configuration](../CONFIGURATION.md).

# Brainless Weapons

Install `Brainless.Weapons.dll` in `EXILED/Plugins` on EXILED 9.15.2 / SCP: SL 14.2.8, with EXILED's standard CustomItems module enabled. Roles and Items are optional.

The `brainless_weapons` configuration registers Nullifier, Rattler, Breach Shotgun, Last Word and Grenade Launcher by default. Redline and Ricochet require `allow_experimental_weapons`. Total supply is one firearm per round below 24 players and two at 24+. Additional world-loot bundles are disabled by default; configurable native firearm replacements and occasional starting-gun conversions are enabled. All share the limited firearm supply.

IDs 7201-7208 are stable integration IDs. Reserve rounds are granted once to the first recipient of a minted kit weapon. Pickup transfers never add reserves. Last Word keeps a serial-based six-shot budget. The launcher keeps a serial-based sixteen-grenade budget. The railgun has three finite charges. SCP-914 turns these finite weapons into a coin. Other guns lose custom behavior while preserving native serialized ammunition before the recipe runs.

The Grenade Launcher (7207) uses the Logicer LMG body and launches native bouncing grenades instead of dealing hitscan bullet damage. It has 16 shots total, no reserve/reload/unload, and one grenade per approved native bullet with no extra cooldown. Default launch speed is 18m/s, fuse 1.8 seconds, blast radius 3.5m, and center damage 70 to humans / 150 to SCPs with linear falloff and normal cover/friendly-fire checks. The grenades preserve native door, destructible-prop and physics damage within the blast radius; `launcher_damage_environment: false` disables it. Native HE burn/deafen/concussion is not added to the custom player hit. Tune the `launcher_*` settings in `brainless_weapons`. Drops, reacquisition, attachments and ordinary 7.62 ammo cannot refill it. Existing configs receive this new default definition once through `catalog_revision`; disable its entry to remove it from availability.

`ci give 7201 [playerID]` and `ci spawn` report confirmed results and ignore natural round supply caps using the ordinary `customitems.give`/`customitems.spawn` permissions. No `--force` flag or extra permission is required. Admin grants do not spend or reset natural supply. Disabled experimental entries still need enabling in configuration. See `docs/COMMANDS.md`.

Independent release updates are enabled under `auto_update`, checking every 60 minutes and applying after a full server restart. Paste a read-only GitHub token into `auto_update.token` in this plugin's YAML for this private repository; `BRAINLESS_GITHUB_TOKEN` remains an optional environment fallback. Only this DLL is replaced; the old file is saved as `.previous`. See `docs/UPDATES.md`.

Redline is a disabled heat/vent prototype at the native firing cadence. The design's proposed 20% faster cadence is not implemented pending client prediction validation. Ricochet reflects once from approved solid mesh geometry; it needs focused in-game collision checks.

Build: `dotnet build Weapons/Brainless.Weapons.csproj -c Release` after preparing references.

Version 0.2.0 exposes Nullifier, Rattler, Breach Shotgun, Redline and Ricochet mechanics under `tuning`. Existing `launcher_*` fields remain compatible and now use validated operator values without the former balance clamps. Set the launcher definition's `magazine` to change the total grenade count (16 by default); Last Word uses its own configured magazine as its finite shot budget. Run `bpweapons reload` with `brainless.weapons.configure`. New grants use the new count; already-issued finite weapons keep their original remaining shots. See the packaged `TUNING.md`.

Version 0.3.0 adds `distribution`: default Nullifier/Rattler/Breach replacements, 20% per eligible map gun (max 1), and 15% per eligible human spawn (max 1 successful grant per round). Existing custom gear is skipped by default. Replacements keep the native body/serial and do not grant extra reserve ammo. Last Word, the launcher and experimental prototypes are absent from the default random pools. See packaged `NATURAL_LOOT.md`.

Custom grenade damage now uses valid native armor penetration (50 by default), fixing a zero-damage setup bug. Launcher blast damage/radius defaults are stronger; unchanged old stock fields migrate with `grenade_balance_revision: 0`. Set it to 1 when deliberately keeping older values. Configure `launcher_armor_penetration` from 1-100; use zero damage to disable damage.

Version 0.3.1 accepts a read-only GitHub token directly in `auto_update.token`; no hosting-panel environment setup is required. A nonempty config token takes precedence over the optional environment fallback. Install this DLL manually once if the old updater cannot authenticate, then fully restart. Subsequent updates retain checksum/assembly checks and `.previous` rollback backups.

Version 0.3.2 raises custom cue and Rattler gain to 2.5, with full volume out to 6m. Tune general vent/ricochet sounds under `tuning.audio` and Rattler under its own audio fields. Untouched old gain migrates; custom settings and native gunshot assets remain unchanged.

Version 0.5.1 makes every approved launcher bullet launch one grenade, using the native Logicer firing cadence. The extra launcher cooldown is retired. Total capacity is hard-capped at 16, smaller configured capacities remain valid, reserve is zero, reload/unload/refills remain blocked, and duplicate/empty callbacks cannot launch extra grenades. Optional Core provides shared update services; the gameplay DLL remains standalone.

Railgun 7208 and disposable launcher settings are described in [SUP_MIGRATION.md](https://github.com/Swishhyy/BrainlessProjectPlugins/blob/main/docs/SUP_MIGRATION.md). The launcher keeps 16 shots and discards its exact empty serial after the final shot by default; `launcher_discard_when_empty: false` retains it empty. Railgun has separate human/SCP/impact damage, stops at walls, excludes targets behind the shooter and cancels charging on a role/item change without consuming a charge. Optional `individual_item_files: true` stores each weapon and its tuning in `EXILED/Configs/ProjectBrainless/<port>/Weapons/<ID>.yml`. Existing files are preserved; `bpweapons reload` validates edits, and enabled/ID/body changes require restart.
