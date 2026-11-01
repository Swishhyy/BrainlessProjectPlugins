# Roles 0.3.0

Adds `chance_percent` to every role. Percentages are direct shares of matching new spawns, with no extra family gate or automatic normalization. Unused/blocked shares remain native roles; group totals over 100 or invalid values are rejected. Admin assignments bypass percentages, existing availability limits remain, and zombie revivals retain their profile. Doctor uses its configured percentage for the separate opt-in event attempt.

New defaults contain numeric values for all 35 profiles. Missing/null legacy fields derive an allocation from the old nominal family budget and weights; explicit percentages take precedence. Existing kits and traits remain. `bproles` shows active percentages and blocked reasons. Save YAML and use `bprole reload` to apply changes.

Adds a simple staff guide covering equipment, role testing, Infection, reloads, player support and CedMod permission nodes. The guide is included in the combined download ZIP and per-plugin packages. Target: EXILED 9.15.2 / SCP:SL 14.2.8. Live spawn/playtesting is still required.
