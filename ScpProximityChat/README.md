# Brainless SCP Proximity Chat 0.1.0

Standalone **EXILED 9.15.2 / SCP: SL 14.2.8** plugin. Install **one DLL**, `Brainless.ScpProximityChat.dll`, in `EXILED/Plugins`. No SecretAPI, SecretLabNAudio, AudioPlayer, or separate settings/HUD library is required. EXILED already supplies the framework, Harmony and MEC; the game supplies speakers, Opus playback and Server Specific Settings.

Stop the server, remove any other SCP proximity-chat plugin, install this DLL, and start the server. The configuration section is `brainless_scp_proximity_chat`. This plugin is independent of Roles, Items, Weapons and BetterCoinflips. Do not install framework/game DLLs from the build reference directory. Automatic updates use this repository's own release assets and the existing `BRAINLESS_GITHUB_TOKEN` setup; see [UPDATES.md](../docs/UPDATES.md).

## Controls

Open **Settings → Server Specific → Brainless SCP voice**. Bind **Nearby SCP voice** (suggested default **N**). Select **Toggle** or **Hold** mode. Your ordinary voice key (normally Q) must also be held to transmit; N selects the route, not the microphone.

- **Toggle:** press N to choose nearby chat; press again to return to global SCP team chat.
- **Hold:** N + your normal voice key speaks nearby. Release N to return to team chat.
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

You can also turn **Hear nearby SCP voice** off in Server Specific Settings. Personal mutes follow user identity for your connection and survive role/round changes; reconnecting resets them. `unmute all` enables reception without clearing individual mutes. Console receive changes last until you change the matching SSS setting or disconnect. In Hold mode, an explicit console route remains selected until the next key update or reset.

Server administrative/global voice mutes and native sender identity/rate validation remain upstream. Voice packets pass through EXILED/LabAPI sending and per-recipient receiving events before the final send. Local audio uses a native speaker, so the ordinary scoreboard's individual voice mute and voice-volume slider are not sufficient controls for this custom stream: use the provided SSS receive switch or `.scpchat mute`. The game's speaker/master audio settings apply. The plugin's personal mute commands affect only its nearby audio, never team chat, human voice or other audio plugins.

## Configuration

```yaml
brainless_scp_proximity_chat:
  is_enabled: true
  debug: false
  settings_id_base: 58000
  suggested_key: N
  default_hold_mode: false
  show_hints: true
  voice_range: 16
  full_volume_range: 2
  volume: 1
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
    token_environment_variable: BRAINLESS_GITHUB_TOKEN
    releases_to_scan: 10
```

`voice_range` is bounded to 5–30 metres; native receiver visibility checks still apply. Full-volume distance is bounded to the voice radius; volume to 0–2. Four native SSS IDs, base through base+3, are reserved. An existing collision stops only this plugin's enable with an explanatory error; change the base and restart. Other plugins' settings and join filters are not replaced. Disabling this plugin removes only its own settings, speakers and Harmony patch.

Build with `python3 scripts/build.py --plugin ScpProximityChat`. The release tag is `scpproximitychat-v0.1.0`. This is a new Brainless implementation using the game/EXILED APIs, not copied from the unlicensed BoltonDev repository. It has been compiled against the pinned real assemblies and checked headlessly; live microphone playback, CedMod interaction and client rendering still require the [playtest](../docs/PLAYTEST.md).
