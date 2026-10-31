# Project Brainless Core 0.1.0

Shared services for the Project Brainless EXILED plugins: persistent XP/stat storage, an optional in-process Discord bot, account linking and a reusable update service. Framework: EXILED 9.15.2 / SCP:SL 14.2.8 / net48.

Install `Brainless.Core.dll` in EXILED/Plugins and fully restart. No separate Discord package is required. The bot uses the runtime's HTTP/WebSocket support and the serializer already supplied by EXILED. Existing gameplay plugins have no hard Core assembly dependency: they use the shared updater when Core is enabled and fall back to their standalone implementation at startup when it is absent. SlFixes supplies the gameplay action tracking; its SCP-914 features also work without Core.


See the project README for configuration, permissions and setup. Independent EXILED DLL with verified release updates. Discord is optional and disabled until configured; live gameplay/network checks remain necessary.
