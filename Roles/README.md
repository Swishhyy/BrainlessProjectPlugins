# Brainless Roles

Install `Brainless.Roles.dll` in `EXILED/Plugins` on EXILED 9.15.2 / SCP: SL 14.2.8. This project builds independently and has no reference to another Brainless DLL.

The generated `brainless_roles` configuration contains 35 profiles, spawn chances, health/stamina changes, optional equipment IDs and experimental switches. Normal coin grants apply to Class-D only. Native MTF/Chaos ranks are preserved when applying a specialist.

All nonexperimental profiles are enabled by default, but caps and optional equipment can skip a selection. Enable a smaller roster for the first playtest. Child, Tickler, Muncher and Doctor behavior require explicit experimental settings. The Doctor conversion also requires `enable_doctor_event`; its one-percent roll is separate from ordinary zombie selection.

Keep `captain_gate_access` false for restricted initial Guard access. Candy uses a painkiller fallback while `approved_candy` is empty. `extreme_muncher_event_preset` adds the requested half-height and double-speed preset only when the experimental role is enabled.

Optional items and weapons are found through the framework registry. If an entire kit cannot be issued, the player remains ordinary. Inventory changes and stat restoration stay in this project. `bproles` in Remote Admin lists the configured roster; it requires `brainless.roles` permission.

The role HUD is enabled by default for living players, showing their custom role name and specialty, or their ordinary role if no custom profile was assigned. It appears near the bottom center and returns after item instructions, warnings or CedMod hints expire. It never clears another plugin's active hint. In `brainless_roles`, use `show_role_hud`, `role_hud_show_description` and `role_hud_vertical_offset` to configure it; the default offset is -11 em. Role changes, death, leaving, round reset and unloading clear only HUD text this plugin owns. No HUD permission or extra plugin is required.

Build: `dotnet build Roles/Brainless.Roles.csproj -c Release` after preparing references. See the repository playtest checklist before installation on the main server.
