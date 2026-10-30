- Explosive zombie Z04 now captures its identity before role cleanup and produces one native blast after a confirmed death, including environmental/explosion deaths by default.
- Configure warning, radius, human/SCP damage, death filters, optional limits and cues under `explosive_zombie`.
- `bprole reload` applies validated YAML without resetting active assignments or lockpick cooldowns. Existing blasts retain their captured settings.
- Existing side HUD, Locksmith rework and independent release updater are included.

Validation: independent builds against EXILED 9.15.2 / SCP: SL 14.2.8, headless gameplay/updater checks and native API contracts. Unity client physics, smoke rendering and live audio still require a server playtest.

See [configuration](https://github.com/Swishhyy/BrainlessProjectPlugins/blob/main/docs/TUNING.md), [commands](https://github.com/Swishhyy/BrainlessProjectPlugins/blob/main/docs/COMMANDS.md) and [updates](https://github.com/Swishhyy/BrainlessProjectPlugins/blob/main/docs/UPDATES.md).
