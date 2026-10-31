Paste a read-only GitHub token directly into `auto_update.token` in this plugin's YAML. No Bisect environment-variable setup is required. The config token takes precedence; existing environment-only setups keep working.

Missing-token and permission errors now explain the appropriate fix without echoing credentials. New configs scan up to 30 releases so all five components can be discovered. Configured scan counts are preserved.

Install this DLL manually once if the old updater cannot authenticate, set the token, and fully stop/start the server. Subsequent automatic downloads retain SHA-256/assembly validation, `.previous` backups and full-restart activation.

This release also includes the previous gameplay changes:

- Candy Man now receives a native SCP-330 bag with exactly three candies by default, replacing the empty-allowlist pill fallback.
- `candy_man_candy_count` supports 1-6. An empty allowlist uses registered standard non-explosive colors; explicit registered colors remain configurable.
- Clears any native seed candy before filling to avoid an accidental extra/random candy. Existing stock hints migrate on startup/reload.
- Existing explosive zombie fixes, role HUD, Locksmith and independent updater are retained.

Built against EXILED 9.15.2 / SCP:SL 14.2.8. Candy pool selection and compiled bag-fill path are regression checked; native bag consumption still requires a server playtest.
