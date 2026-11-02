# Project Brainless Plugins

Eight separate SCP: Secret Laboratory plugins for **EXILED 9.15.3 / SCP:SL 14.2.9**.

## Staff documentation

| Page | What it covers |
| --- | --- |
| [What We Have](docs/CATALOG.md) | Every item, weapon, role and event: short description + how to use it |
| [Staff Quick Guide](docs/STAFF_GUIDE.md) | Giving equipment, testing roles, running events and common fixes |
| [Command Table](docs/COMMANDS.md) | Exact commands and permission nodes |
| [Owner Settings](docs/CONFIGURATION.md) | Percentages, reloads, updates and detailed references |

The combined ZIP includes these pages. Start with `CATALOG.md` for content or `STAFF_GUIDE.md` for commands.

## Plugins

| Folder | DLL | Purpose |
| --- | --- | --- |
| [Roles](Roles) | `Brainless.Roles.dll` | 66 role profiles, per-role percentages and role HUD |
| [Items](Items) | `Brainless.Items.dll` | 12 custom grenades, tools and consumables |
| [Weapons](Weapons) | `Brainless.Weapons.dll` | 2 weapons: a grenade launcher and the experimental Ricochet Pistol |
| [BetterCoinflips](BetterCoinflips) | `BetterCoinflips.dll` | Ordinary coin-flip outcomes |
| [ScpProximityChat](ScpProximityChat) | `Brainless.ScpProximityChat.dll` | Nearby SCP voice and receive controls |
| [Core](Core) | `Brainless.Core.dll` | XP, playtime, privacy controls, optional Discord bot and update services |
| [SlFixes](SlFixes) | `Brainless.SlFixes.dll` | Captive recruitment, guard escape, SCP-914 additions and action tracking |
| [RoundEvents](RoundEvents) | `Brainless.RoundEvents.dll` | Infection, Deathmatch and Juggernaut events |

Current component versions: Roles **0.7.0**, Items **0.8.1**, Weapons **0.8.0**, BetterCoinflips **5.1.9**, SCP voice **0.2.2**, Core **0.2.2**, Fixes **0.4.1**, Events **0.3.0**.

## Install / update

1. Download [the latest release](https://github.com/Swishhyy/BrainlessProjectPlugins/releases/latest): use **ProjectBrainless-Plugins.zip** or individual DLLs.
2. Fully stop the server. Copy your selected DLLs into `EXILED/Plugins`, replacing old copies. Keep existing configs.
3. Start the server. For private automatic updates, set a read-only repository token in `auto_update.token`; see [Owner Settings](docs/CONFIGURATION.md).

Each DLL keeps its own project, version and config. Plugins can run independently; Core integrations are optional. They share the same game process, so separate DLLs do not sandbox crashes. New DLLs need an initial manual install. Downloaded updates load after a full process restart.

## Build / developer notes

```sh
python3 scripts/fetch_references.py
python3 scripts/build.py
```

Build one plugin: `python3 scripts/build.py --plugin Items`. Artifacts contain only the plugin DLL and docs; do not install the build references as plugins. CI builds every project separately and publishes one combined release after validation.

[Architecture](docs/ARCHITECTURE.md) · [Playtest checklist](docs/PLAYTEST.md) · [Update details](docs/UPDATES.md)

BetterCoinflips was imported from [Swishhyy/BetterCoinflips-BP](https://github.com/Swishhyy/BetterCoinflips-BP) and retains its original attribution. See [the import record](BetterCoinflips/IMPORT.md).
