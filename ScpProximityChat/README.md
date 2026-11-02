# Project Brainless — ScpProximityChat

Nearby SCP voice with tap-to-toggle controls.

**Install:** fully stop the server, put `Brainless.ScpProximityChat.dll` in `EXILED/Plugins`, then start it. Target: EXILED 9.15.3 / SCP:SL 14.2.9. Each plugin is optional and builds separately. No peer plugin DLL is required for loading.

**Config:** `brainless_scp_proximity_chat`. Keep your existing YAML when updating.

**Staff:** `bpscpchat status`, `bpscpchat volume <0-20>` or `bpscpchat reload` — permission `brainless.scpchat.configure`. Live changes need no restart; save `volume` in YAML to keep a chosen level.

**Commands:** F1 `.scpchat` · `.scpchat status` · `.scpchat mute all` / `.scpchat unmute all`

- [What we have and how to use it](../docs/CATALOG.md)
- [Staff quick guide](../docs/STAFF_GUIDE.md)
- [Commands and permissions](../docs/COMMANDS.md)
- [Owner settings](../docs/CONFIGURATION.md)
- [Detailed ScpProximityChat notes](../docs/owner/ScpProximityChat.md)

Updates load after a full server restart. Invalid reloads leave current settings active.
