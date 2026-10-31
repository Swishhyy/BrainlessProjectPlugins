# BetterCoinflips BP

This is the imported BetterCoinflips plugin, updated to **5.1.6** for **EXILED 9.15.2 / SCP: SL 14.2.8**. It builds separately as `BetterCoinflips.dll` and does not depend on any Brainless plugin DLL.

Independent release updates are enabled under `auto_update`. They check GitHub Releases every 60 minutes, verify this DLL's checksum and identity, keep a `.previous` backup, and apply the new version after a full server restart. Paste a read-only GitHub token into `auto_update.token` in this plugin's YAML for this private repository; the environment variable remains optional; see `docs/UPDATES.md`.

Stop the server, replace the prior DLL in `EXILED/Plugins`, and restart. Keep existing settings under `better_coinflips`; configuration names, translations and command permission names are retained. Do not install this and an older BetterCoinflips build at the same time.

The original good/bad effects and chances are preserved. This update fixes round cleanup, spawn-counter mutation, per-flip keycard selection, zero/negative weight handling, custom coin-tool recognition and exact-serial coin removal after inventory/role effects. Inventory and spectator swaps move the original item instances and ammo instead of recreating them from item types.

Some original coin effects intentionally change roles, teleport players, summon reinforcements, toggle the warhead or kick a player. Review your existing weights when combining this plugin with the balanced role catalog. This update does not silently disable those existing features.

Build: `dotnet build BetterCoinflips/BetterCoinflips.csproj -c Release` after preparing references.

Original author: **Miki_hero**. Original documentation is retained in [UPSTREAM_README.md](UPSTREAM_README.md). The fork source and import revision are recorded in [IMPORT.md](IMPORT.md).

Version 5.1.2 rebuilds the maintained shared helpers with the rest of the suite; coin effects and their configured probabilities are unchanged. The independent updater remains included.

Version 5.1.3 synchronizes the corrected explosion helper compiled into the suite; coin-effect weights/settings are unchanged.

Version 5.1.4 accepts a read-only GitHub token directly in `auto_update.token`; no hosting-panel environment setup is required. A nonempty config token takes precedence over the optional environment fallback. Install this DLL manually once if the old updater cannot authenticate, then fully restart. Subsequent updates retain checksum/assembly checks and `.previous` rollback backups.

Version 5.1.5 synchronizes the separately compiled shared audio helper. Coin-effect weights and settings are unchanged.

Version 5.1.6 adds optional Core update-service delegation and component release discovery. Coin gameplay remains unchanged.
