# Roles 0.2.6

Updater compatibility patch: removes DataContract JSON initialization and machine proxy discovery, adds strict metadata parsing and sanitized failure diagnostics, and retains credential isolation, TLS/checksum/assembly validation and atomic replacement. An affected old updater needs one manual DLL replacement and a full restart; existing token YAML remains valid.

Shortens every default gameplay description and cleans old generated/config-oriented text when loaded, including per-item overrides. Descriptions explain equipment and abilities; short custom gameplay text is retained. Existing mechanics and IDs remain.

Built for EXILED 9.15.2 / SCP:SL 14.2.8. Round Events 0.1.0 adds Infection as a separate optional DLL. Full live hosting/gameplay verification is still required.
