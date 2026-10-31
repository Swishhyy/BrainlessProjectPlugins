# Plugin boundaries

Each gameplay folder builds its own assembly. EXILED and the game's assemblies are external build references with `Private=false`; they are not copied into the install artifacts. Gameplay projects contain no `ProjectReference` entries and produce no shared gameplay DLL. The headless regression project references two tiny versioned test fixtures to exercise file replacement; these are never shipped as server plugins.

## Optional equipment contracts

Roles looks up optional IDs 7101-7109, 7111-7112 and 7201-7207 in EXILED's CustomItems registry. Providers expose `CanIssueBatch(uint[])` on their registered item definitions. Roles validates the whole required batch per provider before changing a kit. Eligibility is evaluated before the weighted draw. Missing providers exclude only profiles that require them; other profiles can still be selected. No eligible profile leaves an ordinary role. Admin profile tests use the optional `GiveForRole(Player, bool)` provider method through reflection, preserving binary independence. This preflight happens on the main server thread, immediately before issuance.

Roles owns spawn selection and temporary character traits. Items owns utility supply, charges, devices, area effects and tool inputs. Weapons owns weapon supply, gun state and special hit handling. BetterCoinflips owns normal coin gambling and its legacy effects. None may restore a resource merely because an item changes owners.

Items and Weapons each compile an internal CustomItems command adapter. Each Harmony prefix recognizes only that assembly's own registered definitions; other plugins' custom items stay on their original command path. The adapter checks the ordinary `customitems.give`/`customitems.spawn` permission and automatically ignores population/supply limits for admin grants. It reports confirmed inventory/pickup results and partial bulk grants. Admin issuance creates normal serial state without charging or resetting natural supply. Disabled content and recipient eligibility are never bypassed. The old `--force` token is accepted for macro compatibility but grants no special access. Disabling either DLL removes only its own command patches.

Roles owns a cooperative native hint HUD. Percentage margins anchor a narrow left/right column, fixed line height and per-line pixel offsets keep the name/description together, and optional descriptions are wrapped to four short lines. Legacy default positioning migrates from bottom-center to the side on enable. Its short refresh checks EXILED's current hint identity and yields whenever another message is active. Cleanup clears only the exact hint instance it issued. A continuously active HUD from another plugin can keep this display paused; disable `show_role_hud` if another HUD system owns that screen area.

Each of the eight DLLs includes a standalone updater and optionally delegates checks to Core. It reads its own release manifest and asset, validates repository/schema, game/framework compatibility, SHA-256, assembly identity and version, then atomically replaces only its exact Loader-reported file path. Downloads and a bounded periodic check run off the server thread; log messages return through a small MEC queue. Plugin unload cancels outstanding requests; cancellation is checked again before the atomic replacement. A file lock coordinates server instances sharing a plugin directory. Backups end in `.previous`, staging files in `.pending`, and neither can be loaded as another DLL. Updates do not load missing plugins, rewrite configuration, replace EXILED, or reload running assemblies.

## Identity and lifetime

- Custom equipment uses native serials. Uses, shots and heat survive dropping and picking up the same item.
- Registering the same Last Word or launcher serial again does not add shots. Ordinary reserve ammo grants occur only when a weapon is minted into an initial kit.
- Round reset clears serial registries and spent supplies. Scoped timers have a round epoch and are cancelled on reset/disable.
- A delayed blast captures the thrower's footprint and faction at creation, so death or a faction change during the fuse does not change who the explosion belongs to.
- Native faction, armor and protection handling is retained for explosive and firearm damage. Smoke uses a flashbang body and full-size native SCP-244 renderer, without flash, custom slow or damage. Owned server emitters have native lifetime updates disabled and their fog exposure excluded; client particles remain native. Ordinary SCP-244 remains separate, with optional inhaler protection.
- Movement uses native synced effects with a bounded lease. Stronger existing effects take precedence; the plugin releases only an effect value it still owns.
- Maintenance Pass 7110 is retired and stripped from existing role/item lists on enable. Locksmith owns its timer/cooldown and needs no provider. A synchronous completion scope grants only its player/door/collider interaction, while native events and mechanical behavior remain active; no door permission or lock mask is rewritten. Both level-three access categories are rechecked throughout the channel. User-ID cooldowns survive individual role changes/reconnects and reset with the round.
- Guard card and item normalization at SCP-914 is explicit. Custom effects are stripped before the ordinary recipe runs. A specimen, Last Word or launcher becomes a coin, avoiding a free cure or a refillable starting pistol.

## Failure limits

Event callbacks and scoped work log errors without intentionally disabling unrelated plugins. This is not process isolation. Restart between content changes and run a controlled playtest after a game/framework update. Keep backups of prior DLLs and configuration.

Build and install only the individual plugin you are changing. The matrix jobs deliberately do not depend on one another. Changes to the source in `Shared` require rebuilding each plugin that should receive the fix, but no shared DLL upgrade is needed at runtime.

Native cluster children and launcher grenades share internal source, compiled only into Items/Weapons. Each DLL owns its own projectile registry and cleanup. Native physics, fuse and explosion VFX remain; only tracked projectiles replace native damage with reduced, covered, attributed blasts. Cluster children share a per-target damage allowance, and never carry the parent custom-item identity. Disabling/resetting a plugin destroys its outstanding projectiles before removing handlers, preventing fallback to native full-strength damage.

## SCP nearby voice

ScpProximityChat is an independent EXILED project. It uses native Server Specific Settings and spatial SpeakerToy playback. Its scoped Harmony transpiler replaces the sole final native VoiceMessage send after sender identity/rate/mute/channel checks and EXILED/LabAPI sending/receiving events. The receive adapter changes only selected SCP team packets to nearby/none per receiver. The final adapter forwards their original Opus payload as AudioMessage to the eligible recipient, so SCP client modules cannot also play a global copy. Other channels and native team mode keep the original send. Receiver radius, native visibility, alive-group mute and personal receive filters are rechecked at that final boundary. No server decoding, re-encoding or external audio/settings module is needed. Standard client scoreboard individual mutes do not identify speaker-toy audio; the plugin provides a receive switch and per-user mute commands.

Speaker IDs are allocated only when entering local mode, avoiding all existing speakers and using IDs 1–179; existing Brainless effects allocate 180–254. Speakers follow their owners and are destroyed on local-mode exit, role change, death, disconnect, round reset or disable. Native SSS definitions are appended/removed by exact identity without changing another plugin's join filter. Setting ID collisions fail enable explicitly. The plugin compiles the common updater and pure audio-preset rules; it does not compile the gameplay runtime helpers.

## Optional Core and Fixes

Core owns persisted data, XP snapshots/linking and optional Discord networking. SlFixes captures game events and owns rough teleports/914 crafting. A reflection bridge passes ordinary data to Core; there are no hard peer DLL references. Common update startup delegates to Core when available, otherwise uses its standalone engine. Crafting uses confirmed provider adapters and preserves serial/body and normal supply. Discord/save I/O runs outside the game thread; Core failure leaves gameplay plugins loadable. Each of eight projects builds/releases independently.

## Round Events

RoundEvents owns Infection participants, native SCP-3114 attack classification, death-point conversion tickets, infected health/shield limits and event-ending decisions. ForceClass setup avoids random role profiles; other gameplay plugins remain independent. Confirmed direct kills capture a death point before role loss and use an epoch plus exact dead-role identity to prevent delayed conversions from overwriting a new life. Pending conversions delay the event winner check. Stop/reset cancels callbacks and restores only owned native limits. Automatic event selection is disabled by default.

The shared updater uses a bounded internal JSON codec rather than DataContractJsonSerializer and disables system proxy discovery on its direct GitHub client. This avoids two configuration-initialization paths in hosted Unity/Mono. Exception diagnostics contain type/method identifiers, not token-bearing messages. Core uses the same codec for compatible existing stat files and the same direct-client policy for its optional bot.
