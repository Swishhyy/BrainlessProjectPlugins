# Project Brainless — Weapons

5 custom weapons, including the finite grenade launcher and railgun.

**Install:** fully stop the server, put `Brainless.Weapons.dll` in `EXILED/Plugins`, then start it. Target: EXILED 9.15.3 / SCP:SL 14.2.9. Each plugin is optional and builds separately. No peer plugin DLL is required for loading.

**Config:** `brainless_weapons`. Keep your existing YAML when updating.

**Commands:** `ci list registered` · `ci give <ID> [playerID]` · `bpweapons chances` · `bpweapons reload`

- [What we have and how to use it](../docs/CATALOG.md)
- [Staff quick guide](../docs/STAFF_GUIDE.md)
- [Commands and permissions](../docs/COMMANDS.md)
- [Owner settings](../docs/CONFIGURATION.md)
- [Detailed Weapons notes](../docs/owner/Weapons.md)

Updates load after a full server restart. Invalid reloads leave current settings active.

Natural chances are set per entry in `distribution.map_rules` and `distribution.player_rules`. Inspect with `bpweapons chances`.
