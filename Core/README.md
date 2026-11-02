# Project Brainless — Core

Persistent XP/stats, shared update services and a stats API for the website leaderboard and the Discord bot ([setup](../docs/STATS_API.md)).

**Install:** fully stop the server, put `Brainless.Core.dll` in `EXILED/Plugins`, then start it. Target: EXILED 9.15.3 / SCP:SL 14.2.9. Each plugin is optional and builds separately. No peer plugin DLL is required for loading.

**Config:** `project_brainless_core`. Keep your existing YAML when updating.

**Commands:** F1 `.pbxp` · `.pbxp top` · `.pblink`; Remote Admin `pbcore status` / `pbcore save`

- [What we have and how to use it](../docs/CATALOG.md)
- [Staff quick guide](../docs/STAFF_GUIDE.md)
- [Commands and permissions](../docs/COMMANDS.md)
- [Owner settings](../docs/CONFIGURATION.md)
- [Detailed Core notes](../docs/owner/Core.md)

Updates load after a full server restart. Invalid reloads leave current settings active.
