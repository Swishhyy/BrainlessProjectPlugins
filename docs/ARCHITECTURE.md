# Plugin boundaries

Each folder builds its own assembly. EXILED and the game's assemblies are external build references with `Private=false`; they are not copied into the install artifacts. The suite contains no `ProjectReference` entries and produces no shared gameplay DLL.

## Optional equipment contracts

Roles looks up IDs 7101-7112 and 7201-7206 in EXILED's CustomItems registry. Providers expose `CanIssueBatch(uint[])` on their registered item definitions. Roles validates the whole required batch per provider before changing a kit. Missing providers produce an ordinary role, not a runtime assembly-load error. This preflight happens on the main server thread, immediately before issuance.

Roles owns spawn selection and temporary character traits. Items owns utility supply, charges, devices, area effects and tool inputs. Weapons owns weapon supply, gun state and special hit handling. BetterCoinflips owns normal coin gambling and its legacy effects. None may restore a resource merely because an item changes owners.

Items and Weapons each compile an internal CustomItems command adapter. Each Harmony prefix recognizes only that assembly's own registered definitions; other plugins' custom items stay on their original command path. The adapter preserves `customitems.give`/`customitems.spawn` checks and requires an additional per-plugin permission for the explicit `--force` flag. It reports confirmed inventory/pickup results and partial bulk grants. Forced issuance creates normal serial state without charging or resetting natural supply. Disabled content and recipient eligibility are never bypassed. Disabling either DLL removes only its own command patches.

Roles owns a cooperative native hint HUD. Its short refresh checks EXILED's current hint identity and yields whenever another message is active. Cleanup clears only the exact hint instance it issued. A continuously active HUD from another plugin can keep this display paused; disable `show_role_hud` if another HUD system owns that screen area.

## Identity and lifetime

- Custom equipment uses native serials. Uses, shots and heat survive dropping and picking up the same item.
- Registering the same Last Word serial again does not add shots. Ordinary reserve ammo grants occur only when a weapon is minted into an initial kit.
- Round reset clears serial registries and spent supplies. Scoped timers have a round epoch and are cancelled on reset/disable.
- A delayed blast captures the thrower's footprint and faction at creation, so death or a faction change during the fuse does not change who the explosion belongs to.
- Native faction, armor and protection handling is retained for explosive and firearm damage. Gas uses an attributed custom damage handler and only affects humans.
- Movement uses native synced effects with a bounded lease. Stronger existing effects take precedence; the plugin releases only an effect value it still owns.
- Guard card and item normalization at SCP-914 is explicit. Custom effects are stripped before the ordinary recipe runs. A specimen or Last Word becomes a coin, avoiding a free cure or a refillable starting pistol.

## Failure limits

Event callbacks and scoped work log errors without intentionally disabling unrelated plugins. This is not process isolation. Restart between content changes and run a controlled playtest after a game/framework update. Keep backups of prior DLLs and configuration.

Build and install only the individual plugin you are changing. The matrix jobs deliberately do not depend on one another. Changes to the source in `Shared` require rebuilding each plugin that should receive the fix, but no shared DLL upgrade is needed at runtime.
