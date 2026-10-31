# Automatic plugin updates

Roles 0.2.5, Items/Weapons 0.4.0, BetterCoinflips 5.1.6, ScpProximityChat 0.1.5, Core 0.1.0 and SlFixes 0.1.0 support verified component release updates with direct token configuration. Install each desired DLL once. Existing updaters never install another plugin automatically.

## Server setup

Release checks are enabled by default. The first check starts about 20 seconds after a plugin enables; later checks run every 60 minutes. Each plugin has this block under its existing configuration section:

```yaml
auto_update:
  enabled: true
  check_interval_minutes: 60
  include_prereleases: false
  token: "PASTE_YOUR_READ_ONLY_GITHUB_TOKEN_HERE"
  token_environment_variable: BRAINLESS_GITHUB_TOKEN
  releases_to_scan: 30
```

The repository is currently private. Create a fine-grained GitHub personal access token restricted to **Swishhyy/BrainlessProjectPlugins**, with **Contents: read-only**, and paste its value into **`auto_update.token` in each plugin's YAML**. Replace the placeholder above with the actual token. The same token can be used in all installed plugin sections; no Bisect environment-variable setup is required. The updater does not write tokens, headers or response bodies to its logs. Keep a config containing a token out of public uploads and commits.

If an older DLL cannot authenticate, download and install the desired versions listed above manually once, then fully stop/start the server with the updated config. Those versions understand `token`; older versions only read an environment variable. If a token was previously pasted into `token_environment_variable`, move it to `token` and restore the environment-variable name to `BRAINLESS_GITHUB_TOKEN` (or leave that optional field empty). Replace any token that has been exposed.

The environment variable remains optional for existing installations: leave `token: ""` and set `BRAINLESS_GITHUB_TOKEN` in the server process environment. A nonempty `token` in YAML always takes precedence, including if an unrelated or stale environment value exists. Surrounding whitespace is trimmed. If both sources are empty, requests are anonymous; public repositories can be read without a token. This code does not change repository visibility.

The server account needs write access to the installed plugin directory. The updater uses EXILED Loader's exact path for its own assembly, so it also handles installations under a port-specific directory or a renamed DLL file.

## Applying an update

The updater selects its own component from recent eligible releases and checks the manifest's runtime, game version and minimum EXILED version. Stable releases are the default; prereleases require `include_prereleases: true`. It downloads through GitHub's authenticated asset API, checks SHA-256 and actual assembly name/version, then replaces only its own on-disk DLL atomically. The previous file is saved beside it with `.previous` appended.

**The new DLL loads on the next full server restart.** The current round continues using the already-loaded assembly. A normal round restart may not restart the process, depending on your host setup. There is no automatic mid-round reload or forced server restart. Configuration stays in place.

Network, authentication, compatibility or validation failures leave the existing plugin running and produce a console status. Invalid temporary downloads are removed. Same-version/downgrade releases are skipped. A file lock prevents two server instances sharing one path from replacing it concurrently. Draft releases are ignored, and backups/temp files do not have a `.dll` extension.

For rollback, stop the server, restore the appropriate `YourPlugin.dll.previous` over `YourPlugin.dll`, set that plugin's `auto_update.enabled` to false, and start the server. Keep the backup outside the plugin DLL list. Otherwise the same newer published version would be downloaded again.

## Publishing releases

Once these workflows are on `main`, a push/merge to `main` automatically builds and publishes component versions that do not already have a release. A push to a `release/**` branch supports a controlled bootstrap release before merging. The **Release plugins** workflow also offers a manual **All** or single-plugin choice. Each plugin builds, validates and publishes independently; one failed project does not cancel another project's release.

1. Bump the changed project's `.csproj` version and its `Plugin.Version`. BetterCoinflips also keeps its version in `Properties/AssemblyInfo.cs`.
2. Commit and merge/push to `main`, or open **Actions → Release plugins → Run workflow** and select the desired plugin or All. Prerelease is optional for manual runs.
3. The workflow creates a draft, uploads that plugin's DLL plus its own checksum manifest, then publishes. Component tags look like `roles-v0.2.5`, `items-v0.4.0`, `weapons-v0.4.0`, `bettercoinflips-v5.1.6` and `scpproximitychat-v0.1.5`. Already-published component versions are skipped, preserving their original assets. Changed code needs a version bump, even if the rebuild produces different bytes due to commit/build metadata.

You can also push one of those component tags matching its project version. Alternatively, publishing a release through GitHub's Releases UI triggers attachment builds. A component tag selects that plugin; a general tag selects all seven. Each uses its actual project version in its manifest, so a release title/tag cannot manufacture a higher DLL version. Per-plugin manifests prevent upload races on a shared release. The DLL is uploaded before its manifest, and identical existing assets permit safe retries. If immutable releases are enabled, use the draft-first workflow rather than trying to attach files after publication.

Release publishing uses the workflow's temporary `GITHUB_TOKEN` with repository Contents: write. Downloading servers only need a separate read-only token. The workflow becomes available for manual runs after it is on the default branch. Regular pull-request builds prepare and verify the same release assets without publishing anything.

## Release asset contract

| Plugin | DLL | Updater manifest |
| --- | --- | --- |
| Roles | `Brainless.Roles.dll` | `Brainless.Roles.update.json` |
| Items | `Brainless.Items.dll` | `Brainless.Items.update.json` |
| Weapons | `Brainless.Weapons.dll` | `Brainless.Weapons.update.json` |
| BetterCoinflips | `BetterCoinflips.dll` | `BetterCoinflips.update.json` |
| ScpProximityChat | `Brainless.ScpProximityChat.dll` | `Brainless.ScpProximityChat.update.json` |
| Core | `Brainless.Core.dll` | `Brainless.Core.update.json` |
| SlFixes | `Brainless.SlFixes.dll` | `Brainless.SlFixes.update.json` |

Each manifest has schema version 1, repository identity, and a `plugins` array containing the component ID, assembly name, DLL asset name, numeric version, SHA-256, minimum EXILED version, exact SCP: SL version and `net48` target. `scripts/prepare_release.py` generates this from the built project; CI verifies the actual DLL identity/version before publishing. Updating game support also requires reviewing build references and the manifest's compatibility values.

Recent release lists use ETags, manifests are cached, and an already-downloaded pending version is not fetched again. `releases_to_scan` is bounded to 1-30 and defaults to 30 in new configs; existing configured counts are preserved. Use 30 when other component releases push a plugin beyond a shorter scan. The minimum check interval is five minutes.

For the Locksmith rework, update both Roles and Items to 0.1.4 and fully restart. Roles removes retired item 7110 from configured kits and supplies the innate ability; Items removes the retired definition even from old generated lists. Weapons and BetterCoinflips retain their existing versions.

Roles 0.1.5 changes only the role HUD layout: left-side placement by default, optional Right placement, fixed line spacing, a single-line name and wrapped description. The former stock -11 offset is migrated automatically. Update only Roles for this layout change and fully restart after download.

Roles/Items/Weapons 0.2.0 add gameplay tuning and admin reload commands, confirmed-death zombie blasts, native gas initialization/cold filtering, Impact Grenade 7113 and a modest cluster buff. See [TUNING.md](TUNING.md). BetterCoinflips 5.1.2 synchronizes the rebuilt shared helper source; its coin effect settings and probabilities are unchanged. ScpProximityChat remains 0.1.0 with its independent updater.

Items/Weapons 0.3.0 add configurable natural map/locker replacements, scattered optional extra loot and occasional human starting gear. Existing YAML gains the new `distribution` defaults. SCP proximity chat 0.1.1 adds explicit enabled/disabled hints that remain visible while another HUD hint is active. See [NATURAL_LOOT.md](NATURAL_LOOT.md).

The current builds fix zero armor penetration in the shared explosion handler: this native constructor otherwise returns before setting damage/attacker. Explosions use valid penetration (default 50) and zero force instead of a world position as force. Items/Weapons also ship stronger explosive defaults and migrate unchanged old stock values when `grenade_balance_revision` is 0/missing. Set it to 1 when deliberately keeping old numerical values. Roles 0.2.1 includes the same fix for zombie blasts; BetterCoinflips 5.1.3 synchronizes shared helpers without changing coin-effect weights.

Roles 0.2.2 gives Candy Man real SCP-330 candy, with configurable count and registered palette, including when the old allowlist is empty. SCP proximity chat 0.1.2 removes Hold mode, ignores old mode settings and commits the route only after successful speaker allocation. Both retain independent update checks and require a full process restart after downloading their DLLs.

Items 0.3.2 changes Door Jammer 7105 to a keycard and adds the stronger revision-2 cluster preset. Items/Weapons 0.3.2 and chat 0.1.4 raise audio gain/full-volume distance; only untouched old stock values migrate. Roles 0.2.4 and BetterCoinflips 5.1.5 synchronize separately compiled shared helpers without changing role/coin gameplay. All retain direct `auto_update.token` configuration and independent restart-based updates.

## Shared Core service

Core/SlFixes 0.1.0 are new components (`core-v0.1.0`, `slfixes-v0.1.0`) and require a one-time manual install. New builds of the five existing plugins optionally register their update worker with Core through a primitive-data API, with no hard Core DLL reference. Core can supply its configured GitHub token when a component token is empty. Without Core they use the existing standalone engine. One failed component check cannot replace another DLL or stop gameplay. Full process restart is required after downloads or Core removal/hot reload.

Release discovery fetches a full API page (up to 100 records), orders by publication time and then applies releases_to_scan (1-30). This avoids component/tag ordering hiding newer releases on a multi-plugin repo. Stable/prerelease and manifest compatibility checks remain. Build/release workflows now cover all seven independent projects.

## Unity/Mono configuration errors

A token belongs in `auto_update.token`; `token_environment_variable` is an optional fallback name used only when token is empty. Correct token placement does not prevent an unrelated runtime `ConfigurationErrorsException`. The updated client replaces DataContract JSON initialization with a strict, self-contained JSON codec and avoids machine proxy discovery. TLS certificate checks, GitHub-only redirects, token stripping on CDN redirects, size/checksum/assembly checks and atomic replacement remain. Runtime failure logs include sanitized exception types and method identifiers, never raw credential-bearing messages.

Old affected updaters may be unable to fetch this fix: stop the server, manually replace the plugin DLLs with their current releases (including Core if installed), and restart. Existing YAML stays valid. If a token was shown in a screenshot, revoke/regenerate it, update `token`, and keep the replacement private. A later HTTP 401/403/404 is a separate token/access/release issue; report the sanitized status if checks still fail after manual replacement. This patch is built/tested against pinned game references; the exact hosting runtime must still be verified live.

RoundEvents has its own `roundevents-v*` release tags, `Brainless.RoundEvents.dll` and manifest, and supports optional Core update delegation like the other seven components. New DLLs require an initial manual installation.
