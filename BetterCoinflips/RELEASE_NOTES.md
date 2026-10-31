Paste a read-only GitHub token directly into `auto_update.token` in this plugin's YAML. No Bisect environment-variable setup is required. The config token takes precedence; existing environment-only setups keep working.

Missing-token and permission errors now explain the appropriate fix without echoing credentials. New configs scan up to 30 releases so all five components can be discovered. Configured scan counts are preserved.

Install this DLL manually once if the old updater cannot authenticate, set the token, and fully stop/start the server. Subsequent automatic downloads retain SHA-256/assembly validation, `.previous` backups and full-restart activation.

This release also includes the previous gameplay changes:

- Synchronizes the corrected shared explosion helper with the maintained suite.
- Existing coin effects, probabilities, custom-coin exclusions and independent updater remain unchanged.

Built independently against EXILED 9.15.2 / SCP:SL 14.2.8, with gameplay/updater and native API regression checks.
