# Brainless SCP Proximity Chat 0.1.5

Standalone **EXILED 9.15.2 / SCP: SL 14.2.8** plugin. Install **one DLL**, `Brainless.ScpProximityChat.dll`, in `EXILED/Plugins`. No SecretAPI, SecretLabNAudio, AudioPlayer, or separate settings/HUD library is required. EXILED already supplies the framework, Harmony and MEC; the game supplies speakers, Opus playback and Server Specific Settings.

Stop the server, remove any other SCP proximity-chat plugin, install this DLL, and start the server. The configuration section is `brainless_scp_proximity_chat`. This plugin is independent of Roles, Items, Weapons and BetterCoinflips. Do not install framework/game DLLs from the build reference directory. Automatic updates use this repository's own release assets and `auto_update.token` directly in YAML (the environment variable remains an optional fallback); see [UPDATES.md](../docs/UPDATES.md).

## Controls

Open **Settings → Server Specific → Project Brainless SCP voice**. Bind **Toggle SCP proximity chat** (suggested default **N**). Tap once to enable nearby chat, release it, and use your ordinary voice key (normally Q) to speak. Tap N again to restore team chat. The route key is always a toggle; the normal microphone control stays unchanged.

- **Toggle:** press N to choose nearby chat; press again to return to global SCP team chat.
- Nearby mode **replaces** team chat for that speech. Only eligible nearby living receivers hear the spatial audio, including nearby SCP teammates. Distant SCPs and spectators do not hear it. Team mode keeps normal native SCP voice.
- Modes reset to team on death, role changes and round resets. SCP-079 and SCP-3114 keep their native voice behavior. SCP-939's mimicry channel is not rerouted.

Enabled defaults: SCP-049, SCP-049-2, SCP-096, SCP-106, SCP-173 and SCP-939, including custom profiles using these native roles.

## Player commands

Type these in the **F1 player console**, including the dot. Everyone can use them; no CedMod/admin permission is required.

| Command | Action |
| --- | --- |
| `.scpchat` / `.scpvc` | Toggle nearby/team while an enabled living SCP. |
| `.scpchat local` | Select nearby voice. |
| `.scpchat team` | Select ordinary SCP team voice. |
| `.scpchat status` | Inspect route, reception and individual mute count. |
| `.scpchat mute all` / `.scpchat unmute all` | Disable/enable receipt of all custom nearby SCP voice. |
| `.scpchat mute <playerID>` / `.scpchat unmute <playerID>` | Mute/unmute one connected player's custom nearby voice. |

You can also turn **Hear nearby SCP voice** off in Server Specific Settings. Personal mutes follow user identity for your connection and survive role/round changes; reconnecting resets them. `unmute all` enables reception without clearing individual mutes. Console receive changes last until you change the matching SSS setting or disconnect. Explicit console routes remain selected until another toggle/command or a normal role/round reset.

Server administrative/global voice mutes and native sender identity/rate validation remain upstream. Voice packets pass through EXILED/LabAPI sending and per-recipient receiving events before the final send. Local audio uses a native speaker, so the ordinary scoreboard's individual voice mute and voice-volume slider are not sufficient controls for this custom stream: use the provided SSS receive switch or `.scpchat mute`. The game's speaker/master audio settings apply. The plugin's personal mute commands affect only its nearby audio, never team chat, human voice or other audio plugins.

## Configuration

```yaml
brainless_scp_proximity_chat:
  is_enabled: true
  debug: false
  settings_id_base: 58000
  suggested_key: N
  show_hints: true
  hint_duration_seconds: 2
  enabled_hint: Proximity chat enabled
  disabled_hint: Proximity chat disabled
  voice_range: 16
  full_volume_range: 6
  volume: 3.5
  audio_balance_revision: 0
  allowed_roles:
  - Scp049
  - Scp0492
  - Scp096
  - Scp106
  - Scp173
  - Scp939
  auto_update:
    enabled: true
    check_interval_minutes: 60
    include_prereleases: false
    token: ""
    token_environment_variable: BRAINLESS_GITHUB_TOKEN
    releases_to_scan: 30
```

`voice_range` is bounded to 5–30 metres; native receiver visibility checks still apply. Full-volume distance is bounded to the voice radius; volume to 0–10. Four native SSS IDs, base through base+3, are reserved. An existing collision stops only this plugin's enable with an explanatory error; change the base and restart. Other plugins' settings and join filters are not replaced. Disabling this plugin removes only its own settings, speakers and Harmony patch.

Build with `python3 scripts/build.py --plugin ScpProximityChat`. The release tag is `scpproximitychat-v0.1.5`. This is a new Brainless implementation using the game/EXILED APIs, not copied from the unlicensed BoltonDev repository. It has been compiled against the pinned real assemblies and checked headlessly; live microphone playback, CedMod interaction and client rendering still require the [playtest](../docs/PLAYTEST.md).

Version 0.1.1 shows **Proximity chat enabled** after nearby mode successfully acquires a native speaker, and **Proximity chat disabled** when returning to team voice. Key toggles and F1 route commands use this feedback. It briefly replaces the current hint so a persistent role HUD cannot suppress it; that HUD can resume afterward. A failed speaker allocation displays its failure instead of an enabled message. Configure the text/duration above, or disable these messages with `show_hints: false`.

Version 0.1.2 removes Hold mode completely. Old `default_hold_mode` YAML and reports for the retired base+2 setting cannot re-enable it or reset the route. Releasing N never turns nearby chat off. Speaker allocation completes before the enabled state is committed; failures stay on team chat and show an error. `.scpchat status` includes route-check and nearby-audio-send counts for troubleshooting.

Test with two connected living players within the configured range. You do not hear your own stream, and spectators do not receive nearby chat. After enabling, speak using the normal voice key, then check `.scpchat status`: zero route checks means no local voice messages reached the receiving hook; checks with zero audio sends indicate no permitted nearby recipient; a positive send count confirms server transmission but does not confirm client playback. Verify the receiver has nearby audio enabled and no personal/server mute. Live microphone/client playback still requires an in-game check.

Version 0.1.3 accepts a read-only GitHub token directly in `auto_update.token`; no hosting-panel environment setup is required. A nonempty config token takes precedence over the optional environment fallback. Install this DLL manually once if the old updater cannot authenticate, then fully restart. Subsequent updates retain checksum/assembly checks and `.previous` rollback backups.

Version 0.1.4 raises nearby microphone gain from 1 to 3.5 and full-volume distance from 2m to 6m. Native speaker gain supports amplification above 1; no re-encoding or new dependency is needed. Untouched old YAML values migrate unless `audio_balance_revision: 1`; custom settings survive. Gain is configurable from 0 to 10. Live listening levels still need a two-player check.

Version 0.1.5 adds optional Core update-service delegation and robust component release discovery. Voice routing, controls and louder audio defaults are unchanged.
