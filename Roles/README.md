# Project Brainless — Roles

53 custom profiles with configurable percentages and a side HUD.

**Install:** fully stop the server, put `Brainless.Roles.dll` in `EXILED/Plugins`, then start it. Target: EXILED 9.15.2 / SCP:SL 14.2.8. Each plugin is optional and builds separately. No peer plugin DLL is required for loading.

**Config:** `brainless_roles`. Keep your existing YAML when updating.

**Commands:** `bproles` · `bprole [playerID]` · `bprole set <ID> [playerID]` · `bprole reload`

- [What we have and how to use it](../docs/CATALOG.md)
- [Staff quick guide](../docs/STAFF_GUIDE.md)
- [Commands and permissions](../docs/COMMANDS.md)
- [Owner settings](../docs/CONFIGURATION.md)
- [Detailed Roles notes](../docs/owner/Roles.md)

Updates load after a full server restart. Invalid reloads leave current settings active.

Set each profile’s `chance_percent`; inspect active values with `bproles`.
