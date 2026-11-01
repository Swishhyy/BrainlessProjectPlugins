# Project Brainless — RoundEvents

Infection rounds with low-health SCP-3114 and direct-kill conversion.

**Install:** fully stop the server, put `Brainless.RoundEvents.dll` in `EXILED/Plugins`, then start it. Target: EXILED 9.15.2 / SCP:SL 14.2.8. Each plugin is optional and builds separately. No peer plugin DLL is required for loading.

**Config:** `project_brainless_round_events`. Keep your existing YAML when updating.

**Commands:** `bpevent list` · `bpevent infection next` · `bpevent infection start` · `bpevent stop`

- [What we have and how to use it](../docs/CATALOG.md)
- [Staff quick guide](../docs/STAFF_GUIDE.md)
- [Commands and permissions](../docs/COMMANDS.md)
- [Owner settings](../docs/CONFIGURATION.md)
- [Detailed RoundEvents notes](../docs/owner/RoundEvents.md)

Updates load after a full server restart. Invalid reloads leave current settings active.
