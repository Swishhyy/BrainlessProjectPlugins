# Brainless Project Plugins

Independent SCP: Secret Laboratory plugins for the Brainless Project server.

**Target: EXILED 9.15.2 and SCP: SL 14.2.8.** These are four separate C# projects, four configuration sections, and four DLLs. There are no project references or runtime dependencies between the four plugins.

| Folder | Compiled file | Purpose |
| --- | --- | --- |
| [Roles](Roles) | `Brainless.Roles.dll` | 35 configurable role profiles, spawn selection, guard badges and SCP variants |
| [Items](Items) | `Brainless.Items.dll` | 12 custom throwables, tools, devices and supplies |
| [Weapons](Weapons) | `Brainless.Weapons.dll` | Five normal weapons including a 16-shot grenade launcher, plus two disabled experimental prototypes |
| [BetterCoinflips](BetterCoinflips) | `BetterCoinflips.dll` | Imported BetterCoinflips, updated to 5.1.1 for EXILED 9.15.2 |

Read the [automatic updates guide](docs/UPDATES.md), [commands and permissions](docs/COMMANDS.md), [content catalog](docs/CATALOG.md), [architecture](docs/ARCHITECTURE.md), [role availability](docs/ROLE_STATUS.md), and [server playtest checklist](docs/PLAYTEST.md).

Roles, Items and Weapons are version **0.1.3**; BetterCoinflips is **5.1.1**. All four independently check GitHub Releases for their own updates. Admin `ci give`/`ci spawn` commands issue available content without population/supply caps or extra bypass permissions. Natural loadouts and loot keep their normal supply limits. The role HUD and reusable Maintenance Pass are included. Gas uses native SCP-244 smoke with no damage and a 20% human slow. Clusters launch four native grenades. Admins can inspect roles with `bprole`, audit availability with `bproles`, and test a profile with `bprole set <ID> [playerID]`.

## Install

1. Stop the server and install the matching [EXILED release](https://github.com/ExMod-Team/EXILED/releases/tag/v9.15.2). Keep EXILED's standard CustomItems module enabled.
2. Download the desired plugin artifacts from this repository's [Releases](https://github.com/Swishhyy/BrainlessProjectPlugins/releases), **Actions** tab, or build them below. Each artifact contains one DLL and its own instructions.
3. Put the selected DLLs in your server's `EXILED/Plugins` folder. Install any combination of the four.
4. Replace the old `BetterCoinflips.dll`; do not leave two versions installed. Keep a copy of your existing configuration before updating it.
5. Start the server. Edit the generated `brainless_roles`, `brainless_items`, `brainless_weapons`, and `better_coinflips` settings as needed. Restart to apply a new content setup.

Automatic release checks default to every 60 minutes, starting 20 seconds after each plugin enables. Verified DLLs replace their own on-disk files with a `.previous` backup and load after a full server restart. This repository is private, so configure `BRAINLESS_GITHUB_TOKEN` in the server process environment with a fine-grained token restricted to this repository and Contents: read. The token value is never stored in the plugin YAML. See [UPDATES.md](docs/UPDATES.md) for setup and release publication.

Do not install `.build/references`, the game assemblies, or the EXILED framework assemblies from a build output as if they were these plugins.

## Independence

Roles uses the standard EXILED CustomItems registry to discover optional equipment by ID. It does not reference `Brainless.Items.dll` or `Brainless.Weapons.dll`. The role draw excludes profiles whose equipment, population or supply requirements are unavailable, so another eligible profile can still be chosen. If none qualify, the ordinary role keeps its kit. Items and Weapons work without Roles. BetterCoinflips skips registered custom coin tools.

The `Shared` directory contains internal source files compiled into each DLL. It is not a fifth plugin or a required shared DLL. `Directory.Build.props` only pins compiler settings and build references. No combined gameplay project or root solution is required.

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

This checks supply limits, Last Word and launcher shot budgets, eligible role draws, shared cluster damage caps, weighted effects, blast falloff, Nullifier shield spillover, and the actual built DLLs' dependency tables. It does not simulate the Unity server. Follow `docs/PLAYTEST.md` on a test server before normal rotation.

## Prototype status

- Child model scaling, Tickler, Muncher, the Doctor event, Redline and Ricochet are opt-in. Nonexperimental catalog entries are enabled by default, subject to supply and spawn caps.
- Redline currently implements heat, venting and reduced damage at the native FSP-9 firing cadence. The design document's proposed faster cadence remains unimplemented until client prediction is validated. It is disabled by default.
- Ricochet implements one server-side reflection from solid mesh colliders and remains disabled pending in-game collision testing.
- The jammer blocks ordinary door requests, room blackout, room lockdown and speaker actions. It does not cancel a zone-wide blackout; blocking the entire zone would also jam adjacent rooms.
- Echo uses generated footstep-like audio through the game's spatial speakers. It does not create a fake player or an SCP-939 perception marker.
- Candy defaults to a painkiller fallback until an operator approves a native candy allowlist for the installed seasonal build.

## Provenance

BetterCoinflips was imported from [Swishhyy/BetterCoinflips-BP](https://github.com/Swishhyy/BetterCoinflips-BP) and retains Miki_hero's authorship and existing settings. The original fork is left unchanged. See its [import record](BetterCoinflips/IMPORT.md).
