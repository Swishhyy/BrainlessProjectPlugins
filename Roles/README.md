# Brainless Roles

Install `Brainless.Roles.dll` in `EXILED/Plugins` on EXILED 9.15.2 / SCP: SL 14.2.8. This project builds independently and has no reference to another Brainless DLL.

The generated `brainless_roles` configuration contains 35 profiles, spawn chances, health/stamina changes, optional equipment IDs and experimental switches. Normal coin grants apply to Class-D only. Native MTF/Chaos ranks are preserved when applying a specialist.

All nonexperimental profiles are enabled by default, but caps and optional equipment determine which profiles are eligible. Enable a smaller roster for the first playtest. Child, Tickler, Muncher and Doctor behavior require explicit experimental settings. The Doctor conversion also requires `enable_doctor_event`; its profile must also be enabled, and its one-percent roll is separate from ordinary zombie selection.

Keep `captain_gate_access` false for restricted initial Guard access. Candy roles receive native SCP-330 candy. Empty `approved_candy` uses registered Rainbow, Yellow, Purple, Red, Green and Blue colors. `extreme_muncher_event_preset` adds the requested half-height and double-speed preset only when the experimental role is enabled.

Optional items and weapons are found through the framework registry. Unavailable kits are excluded before the weighted roll; another eligible profile can still win. With no eligible profile, the player remains ordinary. Inventory changes and stat restoration stay in this project. `bprole` (alias `myrole`) in Remote Admin shows your actual role/stats and assignment reason; `bprole <playerID>` or `bprole all` inspects others. `bproles [playerID]` audits the roster and reports why each profile is available or blocked. All require `brainless.roles`; ordinary players have only the HUD. `bprole set <roleID> [playerID]` resets a native kit and schedules an admin test assignment without natural caps; configuration switches and required providers still apply. A matching MTF/Chaos rank is kept. See `docs/ROLE_STATUS.md` for all 35 default profiles and gates.

The role HUD is enabled by default for living players. It shows the role name in its faction color, the custom ID when applicable, and an optional description in a compact column at the left side. The name stays on one line and the description wraps to at most four short lines, with fixed spacing. It returns after item instructions, warnings or CedMod hints expire and never clears another plugin's active hint. Role changes, death, leaving, round reset and unloading clear only HUD text this plugin owns. No HUD permission or extra plugin is required.

Configure placement under `brainless_roles`:

```yaml
show_role_hud: true
role_hud_show_description: true
role_hud_side: Left
role_hud_edge_padding_percent: 3
role_hud_width_percent: 30
role_hud_vertical_offset: 0
role_hud_layout_revision: 1
```

Use `Right` for the other side. Padding/width are percentages of the native hint area; width is bounded to 20-40% and edge padding to 0-12%. Vertical offset uses the role-name font size (20 pixels per unit); negative moves down. Roles 0.1.5 migrates the old stock -11 vertical offset to 0 when `role_hud_layout_revision` is missing/0. Custom offsets are otherwise preserved. Set revision 1 with deliberate placement changes. Check the result at your clients' resolutions and alongside other HUD plugins.

Build: `dotnet build Roles/Brainless.Roles.csproj -c Release` after preparing references. See the repository playtest checklist before installation on the main server.

Independent release updates are enabled under `auto_update`, checking every 60 minutes and applying after a full server restart. Paste a read-only GitHub token into `auto_update.token` in this plugin's YAML for this private repository; `BRAINLESS_GITHUB_TOKEN` remains an optional environment fallback. Only this DLL is replaced; the old file is saved as `.previous`. See `docs/UPDATES.md`.

Locksmith (D06) keeps 90 HP and now uses an innate lockpick instead of a Maintenance Pass. Interact normally with a closed door you cannot open: stay within reach for 15 seconds while the progress hint runs. The ability permits doors, gates and checkpoints below containment/armory level 3. The successful interaction uses the native door event path, grants access for that call only, and leaves permission/lock flags intact. A valid keycard still opens its door immediately. Taking damage, moving out of reach/behind cover, restraint, death, role change, opening/moving doors or changed access requirements cancels progress without spending cooldown.

A successful pick starts a 120-second cooldown tied to your user ID until the round resets; changing roles or reconnecting does not reset it. Tune `lockpick_duration_seconds` and `lockpick_cooldown_seconds` in `brainless_roles`. Existing role kits automatically drop references to retired item 7110, and the old stock Locksmith hint is migrated. Locksmith no longer requires the Items DLL. Test with `bprole set D06 [playerID]`; the ability itself needs no admin permission.

Version 0.2.0 fixes Explosive (Z04) by capturing its custom identity before native death cleanup and arming only after a confirmed `Died` event. It spawns a native explosion with sound/VFX after a two-second warning, peaking at 80 human damage within 3m; SCP damage defaults to zero. Environmental and explosion deaths now qualify by default. Configure `explosive_zombie` for warning, damage, radius, death filters and optional per-player/concurrent limits (zero means unlimited). Canceled deaths never arm a blast.

Save YAML and run `bprole reload` with `brainless.roles` to apply settings without resetting assigned roles or lockpick cooldowns. Existing assigned stats/queued blasts keep their state; new spawns/deaths use the new settings. See the packaged `TUNING.md`.

Roles 0.2.1 fixes native explosion damage initialization for Z04: `explosive_zombie.armor_penetration` defaults to 50 (1-100). Zero penetration previously caused the native constructor to return without setting damage. The source footprint is retained and blast force is zero; world coordinates are no longer passed as a force vector.

Roles 0.2.2 fixes Candy Man (S04) to receive a real SCP-330 bag, with exactly `candy_man_candy_count: 3` candies by default (1-6). Empty `approved_candy: []` selects registered standard colors; explicit entries can opt into other registered colors. No pill fallback is used. Native seed candy is cleared before filling so the role does not accidentally receive a fourth/random special candy. Existing custom role kit additions remain operator-controlled. Test with `bprole set S04 [playerID]`; the old stock fallback hint migrates on startup/reload.

Version 0.2.3 accepts a read-only GitHub token directly in `auto_update.token`; no hosting-panel environment setup is required. A nonempty config token takes precedence over the optional environment fallback. Install this DLL manually once if the old updater cannot authenticate, then fully restart. Subsequent updates retain checksum/assembly checks and `.previous` rollback backups.

Version 0.2.4 synchronizes the separately compiled shared audio helper and updates the stock Maintenance Liaison hint to name its Door Jammer keycard. Role traits and edited hints/settings are unchanged.
