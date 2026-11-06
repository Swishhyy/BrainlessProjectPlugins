# Project Brainless Plugins

Ten separate SCP: Secret Laboratory plugins for **EXILED 9.15.3 / SCP:SL 14.2.9** (`net48`).

- **[docs/CATALOG.md](docs/CATALOG.md)** — every item, weapon, role (with default chances), event and SCP-914 recipe, and how to use it. Generated; do not edit by hand.
- **[docs/COMMANDS.md](docs/COMMANDS.md)** — every Remote Admin, F1 and Discord command with its permission node.

This README is the single overview of the project: what each plugin does, where things live, how to configure, build and release.

## Plugins

| Folder | DLL | Version | Config section | Purpose |
| --- | --- | --- | --- | --- |
| `Roles` | `Brainless.Roles.dll` | 0.10.0 | `brainless_roles` | 121 role profiles (7+ per class including SCP-079, a Mythical in each) with rarity tiers, `.scpreroll`, SCP variants, lockpicking, role HUD, `RolesApi` for other plugins |
| `Items` | `Brainless.Items.dll` | 0.11.0 | `brainless_items` | 28 custom grenades, tools, consumables and cursed items (IDs 71xx), floor loot |
| `Weapons` | `Brainless.Weapons.dll` | 0.11.0 | `brainless_weapons` | Grenade Launcher (staff only), Stun Gun (rare, 5 shots), Cursed Pistol, Cursed Shotgun, opt-in Ricochet Pistol (IDs 72xx) |
| `BetterCoinflips` | `BetterCoinflips.dll` | 5.3.0 | `better_coinflips` | Random outcomes from ordinary coin flips, including Brainless items/roles |
| `ScpProximityChat` | `Brainless.ScpProximityChat.dll` | 0.3.1 | `brainless_scp_proximity_chat` | Nearby SCP voice (SCP-939 from its mimic point), receive controls, HUD status |
| `Core` | `Brainless.Core.dll` | 0.3.2 | `project_brainless_core` | XP/playtime storage, privacy, stats API (leaderboard + Discord bot), shared update service |
| `SlFixes` | `Brainless.SlFixes.dll` | 0.5.1 | `project_brainless_sl_fixes` | Captive recruitment, guard escape, SCP-914 rough teleport/crafting, XP action tracking, end-of-round FF |
| `RoundEvents` | `Brainless.RoundEvents.dll` | 0.4.0 | `project_brainless_round_events` | Infection, Deathmatch and Juggernaut events, lobby event vote and sit-out |
| `ScpSwap` | `Brainless.ScpSwap.dll` | 0.1.1 | `brainless_scp_swap` | Round-start SCP swaps that keep Brainless SCP variants |
| `ZombieOptOut` | `Brainless.ZombieOptOut.dll` | 0.1.1 | `brainless_zombie_opt_out` | Zombie opt-out/auto-fill and early-round SCP replacement (`.fill`), keeping Brainless roles |

Each DLL has its own project, version, config and updater. None has a hard reference to another; optional integrations (Roles ↔ Items/Weapons equipment, BetterCoinflips/ScpSwap/ZombieOptOut → Roles via `Shared/RolesBridge.cs` → `Brainless.Roles.RolesApi`, Roles HUD → SCP voice via `Roles/VoiceBridge.cs` → `Brainless.ScpProximityChat.VoiceApi`, everything → Core) go through reflection or EXILED's CustomItems registry, so any subset can be installed. They share one game process, so a crash is not sandboxed.

## Repository layout

| Path | Contents |
| --- | --- |
| `<Plugin>/` | One `.csproj` per plugin: `Plugin.cs` (entry, version), `Config.cs`, `Commands.cs`, feature files |
| `Roles/RolesApi.cs` | Public API other plugins call by reflection (profile ID/name, assign, roll, grant) and the forceclass hook |
| `Roles/RoleDefinition.cs` | All 121 role profiles (`R(...)`/`N(...)`), default rarity tiers, kits, gates; `Roles/RoleAbilities.cs` holds the revision 12-13 abilities and `Roles/Scp079Roles.cs` the SCP-079 traits |
| `Items/Definition.cs`, `Items/SupCatalog.cs`, `Weapons/Definition.cs` | Item/weapon specs (ID, name, description) |
| `SlFixes/Config.cs` | SCP-914 crafting recipes |
| `Shared/` | Source compiled into each plugin that needs it (not a shared DLL): config migration/reload, distribution rules, explosions, audio, `Updates/` updater |
| `docs/content_usage.json` | "How to use" text for every item/weapon/role/event, consumed by the catalog generator |
| `scripts/` | `fetch_references.py`, `build.py`, `generate_catalog.py`, release scripts (`prepare_release`, `select_release`, `publish_release`, `publish_bundle`) |
| `tests/Brainless.Tests/` | Headless C# regression checks (`dotnet run`) |
| `tests/test_release_*.py` | Release packaging/publishing policy tests |
| `.github/workflows/` | `build.yml` (PR/branch builds + checks), `release.yml` (publishing) |

## Install / update

1. Download [the latest release](https://github.com/Swishhyy/BrainlessProjectPlugins/releases/latest): **ProjectBrainless-Plugins.zip** (all DLLs + docs) or individual DLLs.
2. Fully stop the server. Copy the DLLs you want into `EXILED/Plugins`, replacing old copies. Keep existing configs.
3. Start the server. New settings are generated; old ones are migrated (with a `.before-brainless-migration` backup).

### Automatic updates

Every plugin has an `auto_update` block in its config section:

```yaml
auto_update:
  enabled: true
  check_interval_minutes: 60
  include_prereleases: false
  token: "READ_ONLY_GITHUB_TOKEN"   # fine-grained, this repo only, Contents: read
  token_environment_variable: BRAINLESS_GITHUB_TOKEN
  releases_to_scan: 30
```

The repo is private, so set a token. The updater finds its own DLL + `.update.json` in recent releases, checks repository, game/EXILED version, SHA-256 and assembly identity, then atomically replaces only its own file (old copy kept as `.previous`). **New DLLs load on the next full server restart.** Rollback: stop, restore `X.dll.previous` over `X.dll`, set `auto_update.enabled: false`, start. If Core is installed, other plugins delegate update checks to it and can use its token.

## Configuration

Edit `EXILED/Configs/<port>-config.yml` (or `EXILED/Configs/Plugins/<prefix>/<port>.yml` in separated mode), then run the plugin's reload command (`bprole reload`, `bpitems reload`, `bpweapons reload`, `bpscpchat reload`, `bpevent reload`, `pbfixes reload`). Invalid edits are rejected and the active config stays in place. Changing enabled states, IDs or native bodies needs a full restart.

**Rarity (default).** Every item, weapon and role has a `rarity` tier: Common, Uncommon, Rare, Epic, Legendary or Mythical; a higher tier is always stronger. A spawn or loot spot rolls a tier (40/25/12/6/3/1%), then picks something of that tier. Common and Uncommon are unlimited; Rare/Epic/Legendary are limited per round and only one Mythical can exist. Roles: `brainless_roles.guarantee_roles` (every spawn, escapes included, gets a custom role; lifts the human share and wave caps), `human_rarity` (Rare 8, Epic 4, Legendary 2, Mythical 1) and `scp_rarity` (Rare 2, Epic 1, Legendary 1, Mythical 1) are counted separately, `mythical_scp_removal_chance_percent` (40: other SCPs may turn human when an SCP is Mythical), and `chance_percent` only weights roles inside a tier (0 = off). Items/Weapons: `distribution.use_rarity`, `distribution.rarity` (Rare 6, Epic 4, Legendary 2, Mythical 1), `starting_gear_rarity_scale` (0.3) and `floor_loot_small/medium/large` (6/10/14 items on random room floors; Weapons 0/1/2); Items and Weapons share their per-round counts. Cursed loot keeps its own roll but obeys the tier limits. `.scpreroll`: `brainless_roles.reroll` (offered to 35% of custom spawns, 30 s, once per round, `max_rarity: Rare`). BetterCoinflips: coins no longer replace SCP-500 spawns (`item_to_replace` is empty) and `scp914_doubles_coins` doubles coins on Very Fine. Turn `use_rarity` off for the per-entry percentages below.

**Chances (use_rarity off).** Roles use `chance_percent` per profile under `brainless_roles.roles` (inspect with `bproles`). Item/weapon natural loot uses `chance_percent` per rule under `distribution.map_rules` (map pickups, keyed by source type + zone) and `distribution.player_rules` (starting gear per human role). Every matching pool must total ≤ 100; unused or blocked shares keep the vanilla role/item and are never redistributed. Inspect with `bpitems chances` / `bpweapons chances`. Admin `ci give` ignores natural supply/population caps but not disabled content.

```yaml
map_rules:                # 10% Cluster + 20% Impact, 70% stays a normal HE grenade
- custom_id: 7102
  chance_percent: 10
  source_types: [GrenadeHE]
  zones: [LightContainment, HeavyContainment, Entrance]
- custom_id: 7113
  chance_percent: 20
  source_types: [GrenadeHE]
  zones: [LightContainment, HeavyContainment, Entrance]
```

**Other notable settings.** Each role has editable `traits`, `add`/`remove`/`replace_inventory`, `spawn_area` and stats; `allow_model_scaling`, `allow_experimental_roles`, `explosive_scp173`, `enable_doctor_event`, `force_class_custom_roles` (staff forceclass rolls a custom role with normal chances), per-role `traits.health_regen_per_second`/`health_regen_delay_seconds`/`health_regen_max` (out-of-combat healing; Anger Issue heals 20 HP/s up to 900), lockpick durations/cooldowns, `show_role_hud`. SCP voice: `volume` (10), `full_volume_range` (12), `voice_range` (16). Fixes: `capture_conversions.*`, `end_of_round_friendly_fire`, `rough_teleport_rooms` (rooms SCP-914 Rough may teleport players to; open corridors by default so nobody gets stuck). Round events: automatic selection is off by default; when its roll hits, `vote` (on, 45 s, rolled with 5 s left on the lobby countdown) pauses the lobby for a `.vote`, and `.vote optout` lets players spectate the event.

**BetterCoinflips ↔ Brainless.** `custom_item_chance` + `custom_items` (heads gives a listed Brainless item/weapon), `cursed_custom_item_chance` + `cursed_custom_items` (tails), `custom_items_ignore_supply`, `custom_role_chance` + `custom_roles` (heads grants a fitting custom role in place; empty list = any), `roll_custom_role_on_class_change` (Turn into SCP / UNO reverse roll a role for the new class). Chances are proportional like the other coin effects and drop to 0 automatically when the needed DLL is missing. Coins lean toward goodies and pranks with a little gambling: `lucky_tails_percent` (30) turns some tails into a heads outcome, nothing sends players to spectator by default (kick, spectator swap, instant explosion and Tesla teleport are 0; damage effects leave 1 HP), and `bad_effects` has no lethal effects. `balance_revision: 2` marks the migration; untouched old values update automatically.

**Cursed items.** Obvious trade-offs: Cursed Keycard 7123, Cursed SCP-500 7124, Cursed Adrenaline 7125, Monkey's Paw 7126, Cursed Medkit 7127, Cursed Grenade 7128, Cursed Flashbang 7129, Cursed Hat 7130, Cursed Cola 7131, Cursed Lantern 7132 (Items), Cursed Pistol 7210 and Cursed Shotgun 7211 (Weapons). They come from coin tails (`cursed_custom_items`) and rare natural loot (`brainless_items.cursed.loot_chance_percent` 5, `loot_max_per_item` 2; `brainless_weapons.cursed_loot_chance_percent`). Strengths live in `brainless_items.cursed` and `cursed_pistol_jam_percent`. The Monkey's Paw is a coin whose flip BetterCoinflips turns into one twisted wish (`*_chance` weights for the 11 wishes, durations in `timings`). Consumable custom items (pills, cursed SCP-500/adrenaline/medkit/hat/cola) use the native use animation; the native heal/boost is cancelled and the custom effect applies.

**HUD and voice.** Roles registers Server Specific settings at `hud_settings_id_base` (58100): a fade keybind (`hud_toggle_key`, default H) and a Role HUD shown/hidden switch; `hud_fade_seconds` (0.6). The HUD's proximity chat line comes from `show_hud_status` in the voice config; `scp939_voice_from_mimic_point` plays SCP-939's nearby voice from its active mimic point. Setting ID ranges: voice 58000–58003, HUD 58100–58102, zombie opt-out 58200–58202.

**Zombie opt-out.** `zombie_fill_duration` (10 s), `health_compensation` (200), `afk_replacement` + `afk_replacement_valid_time` (200 s), `scp_fill_duration` (15 s), `fill_chance_multiplier_for_no_custom_role` (4), `zombie_fill_xp`/`scp_fill_xp` (Core XP). Ported from [KadavasKingdom/ZombieOptOut](https://github.com/KadavasKingdom/ZombieOptOut) (MIT, `ZombieOptOut/LICENSE`).

**SCP swap.** `swap_window_seconds` (60), `request_timeout_seconds` (20), `max_swaps_per_player` (1, 0 = unlimited), `allow_unplayed_scps`, `swappable_scps`, `custom_role_mode`: `FollowScp` (default: the variant stays with its SCP; an unplayed SCP gets a normal roll), `Reroll` or `None`. Only SCPs from the round-start/late-join spawn can swap, so event and staff assignments stay put.

## Stats API and Discord bot

Core can serve XP/playtime over HTTP for the website leaderboard ([BrainlessWebsite](https://github.com/Swishhyy/BrainlessWebsite)) and the Discord bot ([BrainlessDiscordBot](https://github.com/Swishhyy/BrainlessDiscordBot), a Cloudflare Worker).

```yaml
project_brainless_core:
  stats_api:
    enabled: true
    port: 9025                 # a TCP port your host forwards
    bind_host: '*'
    shared_secret: '...'       # ≥32 chars (openssl rand -hex 32), or env BRAINLESS_API_SECRET
    allowed_origin: 'https://yourwebsite.com'
    trust_cloudflare_headers: true
    requests_per_minute: 60
    leaderboard_cache_seconds: 30
    discord_command_prefix: pb
```

Restart, check `pbcore status` and `http://IP:PORT/api/v1/health`. Put it behind Cloudflare: proxied `A` record `api` → server IP, an Origin Rule rewriting the port, and a Configuration Rule setting SSL to **Flexible** for that hostname.

| Method | Path | Access |
| --- | --- | --- |
| GET | `/api/v1/health`, `/api/v1/status`, `/api/v1/leaderboard?sort=xp\|playtime&limit=1-100` | Public |
| GET | `/api/v1/discord/{discordId}` | Signed |
| POST | `/api/v1/discord/{discordId}/link?code=CODE`, `/api/v1/discord/{discordId}/optout` | Signed |

Signed requests send `X-PB-Timestamp` (unix seconds, ±5 min), `X-PB-Nonce` (16–64 alphanumerics, single use) and `X-PB-Signature` = hex `HMAC-SHA256(secret, timestamp+"\n"+nonce+"\n"+METHOD+"\n"+pathAndQuery)`. `tests/Brainless.Tests/StatsApiTests.cs` and the bot's tests check the same fixed signature.

**Privacy.** Native DNT or `.pbprivacy optout` / `/pb-optout` erases a player's stats and link and suppresses tracking; keep `stats.json.privacy` and its backup during maintenance.

## Build, test, release

```sh
python3 scripts/fetch_references.py              # pinned EXILED/game reference DLLs
python3 scripts/build.py                         # all plugins → artifacts/
python3 scripts/build.py --plugin Items          # one plugin
dotnet run --project tests/Brainless.Tests/Brainless.Tests.csproj -c Release
python3 -m unittest discover -s tests -p 'test_release_*.py'
python3 scripts/generate_catalog.py              # regenerate docs/CATALOG.md (CI fails if stale)
```

To release: bump the changed plugin's `.csproj` `<Version>` and `Plugin.Version` (BetterCoinflips also `Properties/AssemblyInfo.cs`), update the version column above, and merge to `main`. CI builds each plugin in its own job, then publishes one combined draft release tagged `pb-<12-char commit>` with every DLL, every `.update.json` manifest and the install ZIP, and only marks it published/Latest after all uploads succeed. Manual runs (Actions → Release plugins) can pick one plugin or a prerelease; tags like `items-v0.5.1` still publish a single component.

## Design notes

- Custom equipment uses native serials; uses/shots/heat follow the item. Round reset clears serial registries, supply and scoped timers.
- Items and Weapons each patch the CustomItems `give`/`spawn` commands only for their own definitions.
- Explosions keep native faction/armor handling and door/prop damage; tracked projectiles replace player damage with attributed, cover-aware blasts.
- Roles validates a profile's whole equipment batch with its provider before changing a kit; a missing provider leaves the role native.
- ScpProximityChat forwards the original Opus voice payload to nearby receivers via a scoped Harmony transpiler; no re-encoding.
- Server projects compile against the original (non-publicized) `Assembly-CSharp.dll`; private members are reached via cached reflection so access errors surface at build/test time.

BetterCoinflips was imported from [Swishhyy/BetterCoinflips-BP](https://github.com/Swishhyy/BetterCoinflips-BP); original author Miki_hero.
