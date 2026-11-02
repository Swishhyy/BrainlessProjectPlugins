# Weapons 0.9.0

Adds the Stun Gun (7209, COM-18 body, 6 rounds + 12 spare). Each hit deals 1 damage and knocks the target down: a ragdoll of them drops (the death animation) while the real player is hidden, rooted and blinded for 3 seconds, then they get up where they fell. It never kills and makes no death or CASSIE announcement, including on SCPs. Stunned players cannot shoot or deal damage, and the same target cannot be stunned again for 10 seconds (`tuning.stun_gun`). It joins natural loot as a COM-18 replacement.

Built for EXILED 9.15.3 / SCP:SL 14.2.9. Fully restart after replacing DLLs. Needs a live playtest.

## Previous: 0.8.0

Removes Nullifier Rifle (7201), Breach Shotgun (7203) and Railgun (7208) with their loot entries and tuning. The Grenade Launcher and the experimental Ricochet Pistol remain.

Built for EXILED 9.15.3 / SCP:SL 14.2.9. Fully restart after replacing DLLs. Needs a live playtest.

## Previous: 0.7.0

Removes Rattler (7202), Redline SMG (7204) and Last Word (7205) with their loot entries and tuning.

- Nullifier Rifle hits harder: about 35 damage per body hit on humans (was about 28), 0.85x on SCP health, and 2x damage against hume shields (was 1.2x). Unchanged old values migrate automatically.

Built for EXILED 9.15.3 / SCP:SL 14.2.9 (the post-Halloween update). Older builds target 14.2.8, so their auto-updater refuses this release: install the DLLs manually once, then fully restart. Needs a live playtest.

## Previous: 0.6.2

Native private SCP-914 weapon normalization methods now use cached reflection. Launcher environment damage and finite-ammo behavior remain.

All projects now compile against the original game assembly rather than the publicized reference, so unsupported direct private access fails during compilation. Native private patch targets are located by reflection. Updater mismatch messages report both the release target and running SCP:SL version; the exact-version compatibility guard remains.

Built for EXILED 9.15.2 / SCP:SL 14.2.8. Fully restart after replacing DLLs. If the old updater is blocked by a version mismatch, this patch needs a manual download; confirm the actual game version before selecting a differently targeted build. Native smoke/role/SCP-914 behavior still requires live verification.

## Previous: 0.6.1

Launcher grenades now preserve native environment damage: breakable doors, destructible props and physics within the configured blast radius. `launcher_damage_environment` defaults to true. Native player targets are cleared before custom damage to prevent duplicate HE damage. Existing reduced player damage, sixteen-shot cap, fuses, radius and independent updater remain. Railgun impact blasts retain their previous behavior.

Built for EXILED 9.15.2 / SCP:SL 14.2.8. Fully restart after installing. Native interface/regression checks pass; actual door/prop destruction still requires a live playtest.

## Previous: 0.6.0

Adds direct `chance_percent` on map and starting-gear distribution rules, including optional extra-drop anchors. Matching pools cannot exceed 100%; unused or blocked shares remain ordinary loot/no grant. Global chance gates and rule weights are legacy migration inputs only. Missing/null fields derive bounded two-decimal shares; explicit values take precedence. Previously unlisted content receives editable zero-percent entries in nonempty routes, while intentionally empty routes remain empty. Natural supply, inventory, native-body, room, round and player caps remain. Role kits, admin grants and SCP-914 recipe chances are separate.

Adds `bpweapons chances` to inspect active percentages. Save edits and use the existing reload command. A full restart loads the new DLL.

Documentation is reorganized into a content/controls page, a short staff guide, a one-row-per-command reference and owner settings. Detailed plugin notes are separated under docs/owner. The ZIP and per-plugin packages include the complete Markdown docs tree. Target: EXILED 9.15.2 / SCP:SL 14.2.8. Live map/locker/spawn testing remains required.
