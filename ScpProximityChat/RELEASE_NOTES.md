# ScpProximityChat 0.1.7

Raises nearby SCP speech gain from 3.5 to 10 (about +9.1 dB) to address quiet playback. Both old stock gains (1 and 3.5) migrate at audio revision 2, including configs previously marked revision 1. Old 2m full-volume distance upgrades to 6m. Explicit mute, custom levels and revision-2 pinned values are preserved. Item/weapon sound levels do not change.

The F1 `.scpchat status` command now reports plugin version, configured/active speaker gain and distances, so staff can confirm the loaded preset. Toggle controls, native Opus packets, spatial playback, receiver/mute restrictions and independent automatic updates remain.

Target: EXILED 9.15.2 / SCP:SL 14.2.8. Load by fully restarting after the DLL update; keep existing YAML. Exact perceived parity with normal human voice needs a live A/B test because native speakers and human voice use different playback/settings paths.
