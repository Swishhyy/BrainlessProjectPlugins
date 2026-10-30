- Configurable Nullifier, Rattler, Breach, Redline and Ricochet mechanics under `tuning`; launcher fields retain their existing names and accept validated operator values without old balance clamps.
- Launcher and Last Word finite shot counts use their definition's magazine. Defaults remain 16 and 6; existing issued guns retain their original remaining shots after tuning.
- `bpweapons reload` with `brainless.weapons.configure` applies tuning without resetting supply or refilling finite guns.
- Independent updater included; disabled experimental weapons remain opt-in.

Validation: independent builds against EXILED 9.15.2 / SCP: SL 14.2.8, headless gameplay/updater checks and native API contracts. Unity client physics, smoke rendering and live audio still require a server playtest.

See [configuration](https://github.com/Swishhyy/BrainlessProjectPlugins/blob/main/docs/TUNING.md), [commands](https://github.com/Swishhyy/BrainlessProjectPlugins/blob/main/docs/COMMANDS.md) and [updates](https://github.com/Swishhyy/BrainlessProjectPlugins/blob/main/docs/UPDATES.md).
