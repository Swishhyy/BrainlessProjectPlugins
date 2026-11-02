# Project Brainless — Items

16 custom grenades, tools and consumables.

**Install:** fully stop the server, put `Brainless.Items.dll` in `EXILED/Plugins`, then start it. Target: EXILED 9.15.3 / SCP:SL 14.2.9. Each plugin is optional and builds separately. No peer plugin DLL is required for loading.

**Config:** `brainless_items`. Keep your existing YAML when updating.

**Commands:** `ci list registered` · `ci give <ID> [playerID]` · `bpitems chances` · `bpitems reload`

- [What we have and how to use it](../docs/CATALOG.md)
- [Staff quick guide](../docs/STAFF_GUIDE.md)
- [Commands and permissions](../docs/COMMANDS.md)
- [Owner settings](../docs/CONFIGURATION.md)
- [Detailed Items notes](../docs/owner/Items.md)

Updates load after a full server restart. Invalid reloads leave current settings active.

Natural chances are set per entry in `distribution.map_rules` and `distribution.player_rules`. Inspect with `bpitems chances`.
