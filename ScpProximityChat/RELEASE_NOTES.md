- SCP proximity chat is toggle-only: tap N once to enable nearby, release it and use the normal voice key to speak; tap N again for team chat.
- Removes the Hold setting and legacy hold-mode configuration. Old mode reports cannot switch it back or reset an active route.
- Speaker allocation succeeds before enabling the route; failures remain on team and show an error. Duplicate key-down reports do not retry a failed allocation.
- Enabled/disabled hints remain visible over the role HUD. `.scpchat status` adds route-check/audio-send counters for live troubleshooting.
- Existing native voice events, spatial audio, mute controls and independent updater are retained.

Built against EXILED 9.15.2 / SCP:SL 14.2.8, with input/recipient/native API checks. Live microphone and client playback require a two-player server test; server send counters do not prove audible client playback.
