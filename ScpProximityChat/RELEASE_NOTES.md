- Nearby chat now shows “Proximity chat enabled” after successful activation, and “Proximity chat disabled” when switching back to team voice.
- Feedback works for the key toggle, Hold press/release, route commands and mode changes that leave nearby voice.
- The short hint can appear while the role HUD or another hint is present; the HUD can resume after it expires. Failed speaker allocation shows its failure instead of confirming activation.
- Configure `show_hints`, `hint_duration_seconds`, `enabled_hint` and `disabled_hint`. Existing native voice routing, player mute controls and independent updater remain included.

Validation: independent builds against EXILED 9.15.2 / SCP: SL 14.2.8, headless gameplay/updater regression checks and native API contracts. Live loot/locker behavior and client hints/audio still require a server playtest.

See [natural loot](https://github.com/Swishhyy/BrainlessProjectPlugins/blob/main/docs/NATURAL_LOOT.md), [configuration](https://github.com/Swishhyy/BrainlessProjectPlugins/blob/main/docs/TUNING.md) and [updates](https://github.com/Swishhyy/BrainlessProjectPlugins/blob/main/docs/UPDATES.md).
