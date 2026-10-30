# Automatic plugin updates

Roles 0.1.4, Items 0.1.4, Weapons 0.1.3 and BetterCoinflips 5.1.1 include their own updater. Install these builds once. Each then independently checks published releases in [Swishhyy/BrainlessProjectPlugins](https://github.com/Swishhyy/BrainlessProjectPlugins/releases) for its own DLL.

## Server setup

Release checks are enabled by default. The first check starts about 20 seconds after a plugin enables; later checks run every 60 minutes. Each plugin has this block under its existing configuration section:

```yaml
auto_update:
  enabled: true
  check_interval_minutes: 60
  include_prereleases: false
  token_environment_variable: BRAINLESS_GITHUB_TOKEN
  releases_to_scan: 10
```

The repository is currently private. Create a fine-grained GitHub personal access token restricted to **Swishhyy/BrainlessProjectPlugins**, with **Contents: read-only**, and place its value in the **server process environment** as `BRAINLESS_GITHUB_TOKEN`. Configure this in your host panel, container, service or startup environment, then restart the server. Put only the environment variable's name in YAML. Never commit or paste the token value into configuration or release notes. The updater reads the same variable independently in all four DLLs. Public repositories can be read without a token; this code does not change the repository's visibility.

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
3. The workflow creates a draft, uploads that plugin's DLL plus its own checksum manifest, then publishes. Component tags look like `roles-v0.1.4`, `items-v0.1.4`, `weapons-v0.1.3` and `bettercoinflips-v5.1.1`. Already-published component versions are skipped, preserving their original assets. Changed code needs a version bump, even if the rebuild produces different bytes due to commit/build metadata.

You can also push one of those component tags matching its project version. Alternatively, publishing a release through GitHub's Releases UI triggers attachment builds. A component tag selects that plugin; a general tag selects all four. Each uses its actual project version in its manifest, so a release title/tag cannot manufacture a higher DLL version. Per-plugin manifests prevent upload races on a shared release. The DLL is uploaded before its manifest, and identical existing assets permit safe retries. If immutable releases are enabled, use the draft-first workflow rather than trying to attach files after publication.

Release publishing uses the workflow's temporary `GITHUB_TOKEN` with repository Contents: write. Downloading servers only need a separate read-only token. The workflow becomes available for manual runs after it is on the default branch. Regular pull-request builds prepare and verify the same release assets without publishing anything.

## Release asset contract

| Plugin | DLL | Updater manifest |
| --- | --- | --- |
| Roles | `Brainless.Roles.dll` | `Brainless.Roles.update.json` |
| Items | `Brainless.Items.dll` | `Brainless.Items.update.json` |
| Weapons | `Brainless.Weapons.dll` | `Brainless.Weapons.update.json` |
| BetterCoinflips | `BetterCoinflips.dll` | `BetterCoinflips.update.json` |

Each manifest has schema version 1, repository identity, and a `plugins` array containing the component ID, assembly name, DLL asset name, numeric version, SHA-256, minimum EXILED version, exact SCP: SL version and `net48` target. `scripts/prepare_release.py` generates this from the built project; CI verifies the actual DLL identity/version before publishing. Updating game support also requires reviewing build references and the manifest's compatibility values.

Recent release lists use ETags, manifests are cached, and an already-downloaded pending version is not fetched again. `releases_to_scan` is bounded to 1-30; increase it if many other component releases push an older component beyond the default ten entries. The minimum check interval is five minutes.

For the Locksmith rework, update both Roles and Items to 0.1.4 and fully restart. Roles removes retired item 7110 from configured kits and supplies the innate ability; Items removes the retired definition even from old generated lists. Weapons and BetterCoinflips retain their existing versions.
