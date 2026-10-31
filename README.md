# Project Brainless Plugins

Independent SCP: Secret Laboratory plugins for the Project Brainless server.

**Target: EXILED 9.15.2 and SCP: SL 14.2.8.** These are five separate C# projects, configuration sections and DLLs. There are no project references or runtime dependencies between the plugins.

| Folder | Compiled file | Purpose |
| --- | --- | --- |
| [Roles](Roles) | `Brainless.Roles.dll` | 35 configurable role profiles, spawn selection, guard badges and SCP variants |
| [Items](Items) | `Brainless.Items.dll` | 12 custom throwables, tools, devices and supplies |
| [Weapons](Weapons) | `Brainless.Weapons.dll` | Five normal weapons including a 16-shot grenade launcher, plus two disabled experimental prototypes |
| [BetterCoinflips](BetterCoinflips) | `BetterCoinflips.dll` | Imported BetterCoinflips, updated to 5.1.5 for EXILED 9.15.2 |
| [ScpProximityChat](ScpProximityChat) | `Brainless.ScpProximityChat.dll` | Nearby SCP voice with native spatial audio and toggle-only controls; toggle feedback hints; no external audio/settings library |

Read the [automatic updates guide](docs/UPDATES.md), [commands and permissions](docs/COMMANDS.md), [content catalog](docs/CATALOG.md), [balance/config reference](docs/TUNING.md), [natural loot and starting gear](docs/NATURAL_LOOT.md), [architecture](docs/ARCHITECTURE.md), [role availability](docs/ROLE_STATUS.md), and [server playtest checklist](docs/PLAYTEST.md).

Roles is **0.2.4**, Items and Weapons are **0.3.2**, BetterCoinflips **5.1.5** and ScpProximityChat **0.1.4**. All five independently check GitHub Releases for their own updates. For private releases, paste a read-only GitHub token directly into each plugin's `auto_update.token` config field; the environment variable is optional. See [update setup](docs/UPDATES.md). Admin `ci give`/`ci spawn` commands issue available content without population/supply caps or extra bypass permissions. Native pickups throughout the facility and in lockers can now become custom items, and occasional human spawns receive extra/replacement gear after role kits finish. Chances, pools and map/player/room caps are configurable under `distribution`; natural loadouts and loot share their normal supply limits. The role HUD appears in a compact column at the left side by default, with optional right-side placement. Locksmith now picks eligible doors in 15 seconds with a 120-second cooldown after success; containment/armory level 3 is excluded. The Maintenance Pass has been removed. Gas uses native SCP-244 smoke with no damage and a 20% human slow. Custom blast damage now uses valid native armor penetration. Door Jammer 7105 uses a keycard. Clusters have a stronger 6m parent plus four 5.5m child blasts, faster spread and finite shared damage caps. SCP nearby voice and custom item/weapon cues have louder gain and a 6m full-volume area, with configurable settings and old-stock migrations. Impact Grenade 7113 detonates on surface contact after a short arming delay. Explosive zombies now arm a native blast after a confirmed death, including environmental/explosion deaths by default. Admins can inspect roles with `bprole`, audit availability with `bproles`, and test a profile with `bprole set <ID> [playerID]`.

## Install

1. Stop the server and install the matching [EXILED release](https://github.com/ExMod-Team/EXILED/releases/tag/v9.15.2). Keep EXILED's standard CustomItems module enabled.
2. Download the desired plugin artifacts from this repository's [Releases](https://github.com/Swishhyy/BrainlessProjectPlugins/releases), **Actions** tab, or build them below. Each artifact contains one DLL and its own instructions. SCP proximity chat needs only its own DLL on an existing EXILED installation; see [controls and mute settings](ScpProximityChat/README.md).
3. Put the selected DLLs in your server's `EXILED/Plugins` folder. Install any combination of the five.
4. Replace the old `BetterCoinflips.dll`; do not leave two versions installed. Keep a copy of your existing configuration before updating it.
5. Start the server. Edit the generated `brainless_roles`, `brainless_items`, `brainless_weapons`, `better_coinflips`, and `brainless_scp_proximity_chat` settings as needed. Use `bpitems reload`, `bpweapons reload` or `bprole reload` to apply balance edits; restart for registration/native-body changes.

Automatic release checks default to every 60 minutes, starting 20 seconds after each plugin enables. Verified DLLs replace their own on-disk files with a `.previous` backup and load after a full server restart. This repository is private, so configure `BRAINLESS_GITHUB_TOKEN` in the server process environment with a fine-grained token restricted to this repository and Contents: read. The token value is never stored in the plugin YAML. See [UPDATES.md](docs/UPDATES.md) for setup and release publication.

Do not install `.build/references`, the game assemblies, or the EXILED framework assemblies from a build output as if they were these plugins.

## Independence

Roles uses the standard EXILED CustomItems registry to discover optional equipment by ID. It does not reference `Brainless.Items.dll` or `Brainless.Weapons.dll`. The role draw excludes profiles whose equipment, population or supply requirements are unavailable, so another eligible profile can still be chosen. If none qualify, the ordinary role keeps its kit. Items and Weapons work without Roles. BetterCoinflips skips registered custom coin tools.

The `Shared` directory contains internal source files compiled into each DLL. It is not an additional plugin or a required shared DLL. `Directory.Build.props` only pins compiler settings and build references. No combined gameplay project or root solution is required.

Separate DLLs isolate builds, loading dependencies and configuration. All plugins still execute inside the same game process, so this architecture does not sandbox a plugin crash or an infinite loop.

## Build

Install Python 3 and the .NET 8 SDK. From the repository root:

```sh
python3 scripts/fetch_references.py
python3 scripts/build.py
```

Build just one plugin:

```sh
python3 scripts/build.py --plugin Roles
dotnet build Items/Brainless.Items.csproj -c Release
```

The first command downloads checksum-pinned reference assemblies into the ignored `.build/references` directory. A checksum mismatch stops the build rather than silently targeting a different game update. You can instead point `ReferencesPath` at matching local assemblies with `dotnet build ... -p:ReferencesPath=/your/references`.

The packaging script writes separate folders and ZIPs to `artifacts`. It continues building the remaining projects if one fails and returns a failing exit code afterward. The GitHub workflow also uses a separate matrix job and artifact for each plugin, with `fail-fast: false`.

## Verification

```sh
dotnet run --project tests/Brainless.Tests/Brainless.Tests.csproj -c Release -- .
```

This checks nested tuning validation, impact arming/one-shot collision policy, confirmed/canceled zombie death capture, configurable cluster caps, supply limits, Last Word and launcher shot budgets, eligible role draws, Locksmith timing/cooldowns/access policy, shared cluster damage caps, weighted effects, blast falloff, Nullifier shield spillover, SCP voice toggle input and recipient filtering, and the actual built DLLs' dependency tables. It does not simulate the Unity server. Follow `docs/PLAYTEST.md` on a test server before normal rotation.

## Prototype status

- Child model scaling, Tickler, Muncher, the Doctor event, Redline and Ricochet are opt-in. Nonexperimental catalog entries are enabled by default, subject to supply and spawn caps.
- Redline currently implements heat, venting and reduced damage at the native FSP-9 firing cadence. The design document's proposed faster cadence remains unimplemented until client prediction is validated. It is disabled by default.
- Ricochet implements one server-side reflection from solid mesh colliders and remains disabled pending in-game collision testing.
- The jammer blocks ordinary door requests, room blackout, room lockdown and speaker actions. It does not cancel a zone-wide blackout; blocking the entire zone would also jam adjacent rooms.
- Echo uses generated footstep-like audio through the game's spatial speakers. It does not create a fake player or an SCP-939 perception marker.
- Candy Man receives a real SCP-330 bag with three candies by default. The count and candy pool are configurable; an empty pool uses registered standard non-explosive colors.

## Provenance

BetterCoinflips was imported from [Swishhyy/BetterCoinflips-BP](https://github.com/Swishhyy/BetterCoinflips-BP) and retains Miki_hero's authorship and existing settings. The original fork is left unchanged. See its [import record](BetterCoinflips/IMPORT.md).
