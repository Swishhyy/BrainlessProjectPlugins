> Detailed owner notes. For staff, use [the quick guide](../STAFF_GUIDE.md) and [content page](../CATALOG.md). Older version notes below are historical; current spawn percentages are explained in [configuration](../CONFIGURATION.md).

# Brainless Roles

Install `Brainless.Roles.dll` in `EXILED/Plugins` on EXILED 9.15.2 / SCP: SL 14.2.8. This project builds independently and has no reference to another Brainless DLL.

The generated `brainless_roles` configuration contains 53 profiles, spawn chances, health/stamina changes, optional equipment IDs and experimental switches. Normal coin grants apply to Class-D only. Native MTF/Chaos ranks are preserved when applying a specialist.

All nonexperimental profiles are enabled by default, but caps and optional equipment determine which profiles are eligible. Enable a smaller roster for the first playtest. Child, Tickler, Muncher and Doctor behavior require explicit experimental settings. The Doctor conversion also requires `enable_doctor_event`; its profile must also be enabled, and its configurable `chance_percent` roll (default 1%) is separate from ordinary zombie selection.

Guard Captain badges use explicit native Containment 1 / Armory 2 / Administration 1 levels. The legacy `captain_gate_access` toggle is retired. Candy roles receive native SCP-330 candy. Empty `approved_candy` uses registered Rainbow, Yellow, Purple, Red, Green and Blue colors. `extreme_muncher_event_preset` adds the requested half-height and double-speed preset only when the experimental role is enabled.

Optional items and weapons are found through the framework registry. Each profile has a direct `chance_percent` on a matching new spawn. An unavailable/disabled profile keeps its share as ordinary spawns instead of increasing another role's chance. Inventory changes and stat restoration stay in this project. `bprole` (alias `myrole`) in Remote Admin shows your actual role/stats and assignment reason; `bprole <playerID>` or `bprole all` inspects others. `bproles [playerID]` audits the roster and reports why each profile is available or blocked. All require `brainless.roles`; ordinary players have only the HUD. `bprole set <roleID> [playerID]` resets a native kit and schedules an admin test assignment without natural caps; configuration switches and required providers still apply. A matching MTF/Chaos rank is kept. See [role availability](../ROLE_STATUS.md) for all profiles and gates.

The role HUD is enabled by default for living players. It shows the role name in its faction color, the custom ID when applicable, and an optional description in a compact column at the left side. The name stays on one line and the description wraps to at most four short lines, with fixed spacing. It returns after item instructions, warnings or CedMod hints expire and never clears another plugin's active hint. Role changes, death, leaving, round reset and unloading clear only HUD text this plugin owns. No HUD permission or extra plugin is required.

Configure placement under `brainless_roles`:

```yaml
show_role_hud: true
role_hud_show_description: true
role_hud_side: Left
role_hud_edge_padding_percent: 0.5
role_hud_width_percent: 30
role_hud_vertical_offset: 0
role_hud_layout_revision: 2
```

Use `Right` for the other side. Padding/width are percentages of the native hint area; width is bounded to 20-40% and edge padding to 0-12%. Vertical offset uses the role-name font size (20 pixels per unit); negative moves down. Roles 0.1.5 migrates the old stock -11 vertical offset to 0 when `role_hud_layout_revision` is missing/0. Custom offsets are otherwise preserved. Set revision 1 with deliberate placement changes. Check the result at your clients' resolutions and alongside other HUD plugins.

Build: `dotnet build Roles/Brainless.Roles.csproj -c Release` after preparing references. See the repository playtest checklist before installation on the main server.

Independent release updates are enabled under `auto_update`, checking every 60 minutes and applying after a full server restart. Paste a read-only GitHub token into `auto_update.token` in this plugin's YAML for this private repository; `BRAINLESS_GITHUB_TOKEN` remains an optional environment fallback. Only this DLL is replaced; the old file is saved as `.previous`. See `docs/UPDATES.md`.

Locksmith (D06) keeps 90 HP and uses an innate lockpick. Interact normally with a closed keycard door you cannot open and stay within reach while the progress hint runs. The highest containment, armory or administration requirement sets the duration: levels 1/2/3 take 15/30/45 seconds. The successful interaction uses the native door event path, grants access for that call only, and leaves permission/lock flags intact. Facility lockdowns still apply. A valid keycard opens its door immediately. Damage, moving out of reach/behind cover, restraint, death, role change, opening/moving doors or changed access requirements cancels progress without spending cooldown.

A successful level 1/2/3 pick earns a 120/180/240-second cooldown tied to your user ID until the round resets; changing roles or reconnecting does not reset it. Tune `lockpick_duration_seconds`, `lockpick_extra_seconds_per_level`, `lockpick_cooldown_seconds` and `lockpick_extra_cooldown_per_level` in `brainless_roles`. Retired Maintenance Pass 7110 is removed from kits. No Items DLL is required. Test with `bprole set D06 [playerID]`; the ability itself needs no admin permission.

## New human roles (0.5.0)

| ID | Role | Behavior |
| --- | --- | --- |
| C05 | Cowboy | Chaos revolver kit, full light-armor reserve ammo, two stims and painkillers |
| G07 / S08 | Rogue Guard / Scientist | Neutral, hostile to everyone, isolated Entrance spawn, early CASSIE warning |
| S03 | Head Scientist | Existing scientist profile with research-coordinator access |
| S07 | Containment Engineer | Scientist with containment engineer card; starts in the HCZ Micro room |

Rogues use the native neutral Tutorial team with their original human appearance. SCP targeting and normal cuffing remain native; Rogues can also fight each other. Their custom HUD identifies the role. SlFixes 0.3.0 lets either military faction recruit a cuffed Rogue at the normal surface escape zone. `rogue_spawn_separation` controls distance from other living players; `announce_rogue` controls the warning. Safe floor/headroom checks apply to both special spawn areas.

`role_catalog_revision: 2` appends the four new profiles once, preserves existing percentages and renames the old stock S03. A full existing chance pool adds a new role at 0%; set its percentage and adjust the same group's peers yourself. Later deliberate removals remain removed. Per-role kit, stats, spawn area and percentages remain editable.

Version 0.2.0 fixes Explosive (Z04) by capturing its custom identity before native death cleanup and arming only after a confirmed `Died` event. It spawns a native explosion with sound/VFX after a two-second warning, peaking at 80 human damage within 3m; SCP damage defaults to zero. Environmental and explosion deaths now qualify by default. Configure `explosive_zombie` for warning, damage, radius, death filters and optional per-player/concurrent limits (zero means unlimited). Canceled deaths never arm a blast.

Save YAML and run `bprole reload` with `brainless.roles` to apply settings without resetting assigned roles or lockpick cooldowns. Existing assigned stats/queued blasts keep their state; new spawns/deaths use the new settings. See the packaged `TUNING.md`.

Roles 0.2.1 fixes native explosion damage initialization for Z04: `explosive_zombie.armor_penetration` defaults to 50 (1-100). Zero penetration previously caused the native constructor to return without setting damage. The source footprint is retained and blast force is zero; world coordinates are no longer passed as a force vector.

Roles 0.2.2 fixes Candy Man (S04) to receive a real SCP-330 bag, with exactly `candy_man_candy_count: 3` candies by default (1-6). Empty `approved_candy: []` selects registered standard colors; explicit entries can opt into other registered colors. No pill fallback is used. Native seed candy is cleared before filling so the role does not accidentally receive a fourth/random special candy. Existing custom role kit additions remain operator-controlled. Test with `bprole set S04 [playerID]`; the old stock fallback hint migrates on startup/reload.

Version 0.2.3 accepts a read-only GitHub token directly in `auto_update.token`; no hosting-panel environment setup is required. A nonempty config token takes precedence over the optional environment fallback. Install this DLL manually once if the old updater cannot authenticate, then fully restart. Subsequent updates retain checksum/assembly checks and `.previous` rollback backups.

Version 0.2.4 synchronizes the separately compiled shared audio helper and updates the stock Maintenance Liaison hint to name its Door Jammer keycard. Role traits and edited hints/settings are unchanged.

Version 0.2.6 corrects captain badge levels and adds readable admin role names: `bprole set explosive_zombie` or `bprole set Z04` targets yourself; append a player ID to target another player. Permission: `brainless.roles`, Remote Admin only. Optional Core provides shared update services without becoming a hard dependency.

## Per-role percentages (0.3.0)

Set `chance_percent` on each existing role entry: `10` means a 10% roll on a matching new spawn; `0` prevents natural selection. Percentages are not multiplied by the former family chance or treated as relative weights. The complete group must total at most 100: Class-D, Scientists, Guards, MTF, Chaos, SCP-049 or zombies. Disabled/experimental entries still reserve their configured share, so enabling one does not change the other percentages. Unused or blocked shares keep the native role. Missing providers, population/supply limits and all other availability checks still apply. Admin `bprole set` ignores the percentage; revived zombies retain their existing profile.

New defaults have numeric percentages on all 35 profiles. Old missing/null values derive a starting allocation from the legacy family budget and relative weights across the configured group, including disabled/experimental entries. Values are rounded down to two decimals within the nominal budget; old eligibility-based redistribution is intentionally replaced by fixed shares. EXILED may show a missing legacy field as `chance_percent: null`; set a number to override it. Existing kits, IDs and edited traits stay intact. Once a percentage is explicit, legacy `weight` and family-chance settings do not affect it. `bproles` shows the active percentages.

Doctor (Z05) uses its percentage for the existing one-attempt-per-round conversion event, outside ordinary zombie selection. It still requires its profile, experimental and event switches, population threshold and an SCP-049.

Save edits and run `bprole reload`. Invalid percentages, non-finite numbers and group totals over 100 are rejected before applying the config. See the [staff guide](https://github.com/Swishhyy/BrainlessProjectPlugins/blob/main/docs/STAFF_GUIDE.md) for short examples.
