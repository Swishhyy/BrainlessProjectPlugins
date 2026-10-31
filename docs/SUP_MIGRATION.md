# SUP gameplay migration

Items 0.5.0, Weapons 0.5.0 and SCP SL Fixes 0.2.0 rebuild the gameplay ideas from [Swishhyy/SUP](https://github.com/Swishhyy/SUP) for EXILED 9.15.2 / SCP:SL 14.2.8. No old SUP DLL or new dependency is needed. The webhook is excluded. SUP's empty roles/events/module projects contain no gameplay to import. The former wearable plan is implemented as the SCRAMBLE utility below.

Existing Project Brainless roles, smoke, impact and cluster behavior remain. The launcher stays at 16 rounds; the old eight-shot limit is not imported. Items remain a separate DLL from Weapons and Fixes. Existing configs receive new catalog/loot rows once (Items catalog revision 4, Weapons revision 2, Fixes recipe revision 1); later deliberate removals are respected. Explicit empty loot/recipe pools stay empty. Ordinary supply caps remain 6/10/14 utilities, 1/2 custom firearms and the configured cluster population gates; admin grants bypass issue limits only.

## New content and defaults

| Current ID | Old SUP ID | Name | Behavior |
| --- | --- | --- | --- |
| 7114 | 101 | SCP500-A Adrenaline Pills | 35% movement boost for 25s, restore 100% stamina, then 5s exhaustion. Uses a scoped movement layer rather than misusing SCP-207 intensity as a speed multiplier. |
| 7115 | 103 | SCP500-B Anti-096 Pills | Removes all current SCP-096 targets for the user. Retained if untargeted. Optional 15s retarget immunity is off by default. |
| 7116 | 102 | SCP500-C Expired Pills | One weighted effect: positive 50 / negative 30 / SCP 20; default duration 10s. Each of 24 effects has its own weight, min/max intensity and optional duration. Empty/zero-weight pools heal 15–35 HP instead. Some configured SCP effects can be lethal. |
| 7117 | 104 | SCP500-D Invisibility Pills | 7s of refreshed native invisibility, hides the correct target from SCP-939 vision/footstep indicators, once per user per round. Attacks break stealth by default. Existing SCP-096 targets are unaffected. |
| 7118 | 107 | Bio Grenade | Flashbang body, 3s fuse, 30s native visual SCP-244 fog. Ten seconds of sustained exposure within 5m applies 3s Corroding once per cloud/player. Leaving or filtering resets exposure; cover, room, owner and SCP checks are configurable. |
| 7119 | 105 | Hacking Chip | Janitor keycard body. Hold the tool key or interact with the aimed door for 8s; consumes a charge to open a door requiring at most level 2 in every access category. Hard locks, gates and elevators are protected. Checkpoints default off. |
| 7120 | 106 | Reinforcement Radio | Radio control/tool key, 3s channel, 15s arrival, up to three eligible connected spectators become native NTF Privates or Chaos Riflemen. No full wave is forced. One call per faction per round, 300s user cooldown, earliest 60s into a round. Class-D/SCPs cannot call. |
| 7121 | Planned wearable | SCRAMBLE Goggles | Flashlight body acting as a wearable utility. Toggle with tool key/flashlight control, then other equipment can be held. Blocks visual SCP-096 target acquisition while active for 60 battery seconds. Does not remove existing targets or stop damage-triggered targeting. Dropping/role changes disable it; remaining battery follows the serial. |
| 7122 | Removed pre-3.1 item | SCP500-E Volatile Pills | 0.5s blast after consumption, usually kills user; 5% survival roll leaves 5 HP. Nearby damage peaks at 70 human / 200 SCP within 6m, respects cover, armor and friendly-fire policy. The user's survival roll is isolated from its own custom blast. |
| 7208 | 112 | Railgun | E11 body; three finite charges. 5s charging + 0.75s discharge, 50m range, 0.5m beam radius, 85 human / 250 SCP beam damage. Stops at walls and hits only targets ahead within the visible segment. Optional 2m impact blast adds up to 20 human / 50 SCP damage. |

Pills default to a 2s use channel and 5s cooldown. Native use controls or holding the custom tool key work; moving, changing item/role or taking damage interrupts the existing channel policy. Most new items have a round cap of two; the radio, Volatile Pills and Railgun have a cap of one. These are additional limits inside the shared provider supply budget.

SCRAMBLE's configurable failure chance defaults to 0%. A nonzero setting rolls once per device/SCP-096 encounter per round, so repeated native face checks cannot reroll every frame. Battery drains while active, including when holding another item. This is a server utility with a flashlight inventory model, not a custom client-side goggle model or screen shader.

Bio Grenade uses the existing filter consumable when `filter_protects` is on. `ally_heal_amount` and `ally_ahp_amount` default to zero; setting them enables actual healing/AHP support, with `continuous_healing` and `healing_interval_seconds` controlling repeat pulses. Native fog size is independent of the configurable exposure radius. Corroding uses the game's native status-effect damage, so it does not report an attributed firearm kill to XP.

Reinforcement eligibility is checked again at arrival. Overwatch, disconnected and already-living players are excluded, and fewer than three available spectators means fewer recruits. A failed arrival does not refund a consumed radio or call slot. Calls cancel on round reset/plugin shutdown. `minimum_players`, `earliest_round_seconds`, `players_per_call`, `calls_per_faction_per_round`, cooldown and arrival can all be tuned.

The railgun can move/aim during charging. Changing away, dropping, dying or changing role cancels before ammunition is consumed. Its charging visuals use three primitive rings; `ring_segments`, `show_charging_effects`, `show_charge_progress` and `max_concurrent_charges` bound the effect work. Beam visuals fade out. Impact damage is separate and intentional; there is no extra vanilla E11 hit damage. Reload/unload/attachments and pickup transfers cannot refill the finite charge ledger.

The launcher still launches once per approved native LMG bullet. `launcher_discard_when_empty: true` removes the exact empty launcher serial after its final shot; false retains it empty. Dropping/reacquiring cannot refill it. Existing cluster child count, physical launch, shared damage caps and impact collision handling remain configurable in their existing sections.

## Configuration locations

Main `brainless_items.sup` contains `adrenaline_pills`, `anti096_pills`, `expired_pills`, `invisibility_pills`, `bio_grenade`, `hacking_chip`, `reinforcement_radio`, `scramble_goggles`, and `volatile_pills`. The matching catalog entries in `items` control enabled state, charges, weight and round cap. `brainless_weapons.tuning.railgun` owns charge/beam/impact settings; the 7208 entry controls its finite magazine and enabled state. Existing `launcher_*` fields continue working.

Optional `individual_item_files: true` in Items and/or Weapons creates:

```
EXILED/Configs/ProjectBrainless/<port>/Items/7114.yml
EXILED/Configs/ProjectBrainless/<port>/Weapons/7208.yml
```

Every file has `item` (catalog entry) and `tuning` blocks. Files are created only when absent; saved edits are never regenerated or overwritten at startup/reload. When enabled, per-item files override the corresponding main catalog/tuning values. Global supply/distribution remain in the main YAML. Disable the switch to return to main YAML. The Last Word file has an empty tuning block because its finite capacity is in `item.magazine`.

Use `bpitems reload` (`brainless.items.configure`) or `bpweapons reload` (`brainless.weapons.configure`) for tuning edits. Files are read into a detached config, validated, then applied; invalid YAML/values retain active settings. Enabled/ID/body/registration changes require a full restart. Existing finite weapons keep their minted shot capacity; new grants use edited capacities. Existing effect instances may retain captured tuning until they expire.

## Commands

`ci list registered`, `ci give <currentID> [playerID]` and existing role commands remain. `sci itemlist`/`sci items`, `sci help`/`sci h`, and `sci give <currentID/oldID> [playerID]`/`sci gitem` provide old command compatibility. Granting requires `customitems.give` or `sci.admin`, confirms actual receipt and bypasses natural issuance caps. It does not bypass use restrictions such as radio eligibility or used stealth. `sci` is only in Items; its optional weapon grants discover Weapons through the registry, with no DLL dependency.

Old ID 103 now means Anti-096 as in SUP 3.1, not the removed explosive pills; use 7122 for Volatile Pills. Retired Maintenance Pass 7110 remains removed. No webhook configuration or command audit HTTP request is added.

## Added SCP-914 recipes

| Input | Setting | Output | Chance | Round recipe cap |
| --- | --- | --- | --- | --- |
| SCP-500 | Fine | 7114 Adrenaline | 20% | 2 |
| SCP-500 | Very Fine | 7115 Anti-096 | 15% | 2 |
| SCP-500 | Coarse | 7116 Expired | 40% | 2 |
| SCP-500 | Very Fine | 7117 Invisibility | 10% | 2 |
| Flashbang | Very Fine | 7118 Bio | 20% | 2 |
| Janitor keycard | Very Fine | 7119 Hacking Chip | 15% | 2 |
| Radio | Very Fine | 7120 Reinforcement Radio | 10% | 1 |
| Flashlight | Fine | 7121 SCRAMBLE | 20% | 2 |
| SCP-500 | Rough | 7122 Volatile | 10% | 1 |
| E11-SR | Very Fine | 7208 Railgun | 5% | 1 |

The four existing smoke/cluster/impact/dressing recipes remain. Matching recipes roll in config order: a later recipe on the same input/setting rolls only if earlier recipes did not match. Recipe chances, settings and caps are configurable. Native bodies/serials are retained; failed/unavailable conversions leave the ordinary recipe intact. Natural loot/player loadouts still have one shared distribution roller in their provider DLL, not a second SUP roller.

## Validation and playtesting

All affected assemblies compile against pinned real game/framework references. Automated checks cover weighted effects, beam direction/cover boundaries, access policy, finite-ammo behavior, native enum validity, recipe/distribution migrations and per-item file preservation/identity rejection. These do not simulate Unity clients. Test fog appearance, door aiming, target events, battery transfer, spectator arrival and charging visuals on the actual server before setting final balance values.
