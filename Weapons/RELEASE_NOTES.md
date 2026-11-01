# Weapons 0.6.0

Adds direct `chance_percent` on map and starting-gear distribution rules, including optional extra-drop anchors. Matching pools cannot exceed 100%; unused or blocked shares remain ordinary loot/no grant. Global chance gates and rule weights are legacy migration inputs only. Missing/null fields derive bounded two-decimal shares; explicit values take precedence. Previously unlisted content receives editable zero-percent entries in nonempty routes, while intentionally empty routes remain empty. Natural supply, inventory, native-body, room, round and player caps remain. Role kits, admin grants and SCP-914 recipe chances are separate.

Adds `bpweapons chances` to inspect active percentages. Save edits and use the existing reload command. A full restart loads the new DLL.

Documentation is reorganized into a content/controls page, a short staff guide, a one-row-per-command reference and owner settings. Detailed plugin notes are separated under docs/owner. The ZIP and per-plugin packages include the complete Markdown docs tree. Target: EXILED 9.15.2 / SCP:SL 14.2.8. Live map/locker/spawn testing remains required.
