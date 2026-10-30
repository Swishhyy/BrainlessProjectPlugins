- Gas preserves native SCP-244 fog signalling, initializes native emitters at their world position and filters harmful cold subeffects separately. Default remains eight seconds, no damage and 20% human slowing; no gas primitives.
- New Impact Grenade 7113 detonates on surface contact after a short arming delay, with optional player impacts and a fallback fuse.
- Cluster keeps four physical children by default; child peaks are now 40 human / 100 SCP with 2.75m radius and shared 85/225 caps.
- Every item has its own `tuning` section. Change counts, fuses, spread, damage, duration, radius, slows, use/deploy times and supply. Run `bpitems reload` with `brainless.items.configure`.
- Existing charges/fields/supply retain state; registration changes require restart. Maintenance Pass remains removed. Independent updater included.

Validation: independent builds against EXILED 9.15.2 / SCP: SL 14.2.8, headless gameplay/updater checks and native API contracts. Unity client physics, smoke rendering and live audio still require a server playtest.

See [configuration](https://github.com/Swishhyy/BrainlessProjectPlugins/blob/main/docs/TUNING.md), [commands](https://github.com/Swishhyy/BrainlessProjectPlugins/blob/main/docs/COMMANDS.md) and [updates](https://github.com/Swishhyy/BrainlessProjectPlugins/blob/main/docs/UPDATES.md).
