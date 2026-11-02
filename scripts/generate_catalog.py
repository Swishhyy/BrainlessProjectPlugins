#!/usr/bin/env python3
import pathlib
import re
import math
import json
from collections import defaultdict

root = pathlib.Path(__file__).resolve().parents[1]
source = (root/'Roles/RoleDefinition.cs').read_text()
roles = re.findall(r'R\(\s*"([A-Z]\d\d)",\s*"([^"]+)",\s*RoleTypeId\.(\w+),\s*"([^"]+)"\)', source)
blocks = source.split('r = R(')[1:]
weights = {}
groups = {}
totals = defaultdict(int)
for block in blocks:
    match = re.search(r'"([A-Z]\d\d)",\s*"[^"]+",\s*RoleTypeId\.(\w+)', block)
    role_id, base = match.groups()
    groups[role_id] = role_id[0] if role_id.startswith(('M', 'C')) else base
    weight = re.search(r'r.Weight = (\d+)', block)
    weights[role_id] = int(weight.group(1)) if weight else 10
    if role_id != 'Z05': totals[groups[role_id]] += weights[role_id]
explicit = {}
for block in blocks:
    match = re.search(r'"([A-Z]\d\d)"', block)
    value = re.search(r'r.ChancePercent = ([\d.]+)', block)
    if match and value: explicit[match.group(1)] = float(value.group(1))
def percentage(role_id, base):
    if role_id == 'Z05': return '25 (event)'
    if role_id in explicit: return f'{explicit[role_id]:g}'
    budget = 80
    value = math.floor(budget * weights[role_id] / totals[groups[role_id]] * 100) / 100
    return f'{value:g}'

usage = json.loads((root/'docs/content_usage.json').read_text())
assert set(usage['roles']) == {r[0] for r in roles}, 'Every role needs short usage instructions.'
lines = ['# Project Brainless — What We Have', '',
         '[Staff commands](STAFF_GUIDE.md) · [Full command table](COMMANDS.md) · [Owner settings](CONFIGURATION.md)', '',
         'Current shipped content: **12 items, 2 weapons, 66 role profiles and 3 round events**. Values below are defaults; server settings can differ.', '',
         '**Tool key:** Settings → Server Specific → Use custom item or nearby device (default **V**). Hold for timed actions; tap for instant actions. Grenades use normal throw controls.', '',
         '## Items', '', '| ID | Item | What it does | How to use |', '| --- | --- | --- | --- |']
for folder, label in [('Items', 'Items'), ('Weapons', 'Weapons')]:
    content_source = (root/folder/'Definition.cs').read_text()
    if folder == 'Items': content_source += (root/folder/'SupCatalog.cs').read_text()
    entries = []
    for block in re.findall(r'new (?:ItemSpec|WeaponSpec)\s*\{([^{}]+)\}', content_source):
        fields = [re.search(pattern, block) for pattern in (r'\bId\s*=\s*(\d+)', r'\bName\s*=\s*"([^"]+)"', r'\bDescription\s*=\s*"([^"]+)"')]
        if all(fields): entries.append(tuple(field.group(1) for field in fields))
    entries.sort(key=lambda e: int(e[0]))
    assert len(entries) == (12 if folder == 'Items' else 2)
    controls = usage[folder.lower()]
    assert set(controls) == {e[0] for e in entries}, f'Usage coverage missing for {folder}'
    if folder == 'Weapons':
        lines += ['', '## Weapons', '', '| ID | Weapon | What it does | How to use |', '| --- | --- | --- | --- |']
    for item_id, name, description in entries:
        if item_id == '7206': name += ' **(opt-in)**'
        lines.append(f'| {item_id} | {name} | {description} | {controls[item_id]} |')
lines += ['', 'Ricochet requires experimental weapons to be enabled. Give equipment with `ci give <ID> [playerID]`; use `ci list registered` to see what is enabled.', '',
          '## Roles', '', 'Traits apply automatically. Use `bprole set <ID> [playerID]` to test; `bproles` shows percentages and blocked reasons.', '',
          '| ID | Role | What it does | How to use | Default status |', '| --- | --- | --- | --- | --- |']
for role_id, name, base, hint in roles:
    if role_id == 'Z02': hint = 'Faster zombie with less health and weaker attacks.'
    status = 'Event: 25% per round' if role_id == 'Z05' else 'Enabled'
    lines.append(f'| {role_id} | {name} | {hint} | {usage["roles"][role_id]} | {status} |')
lines += ['', 'About 80% of each spawn group rolls a custom role by default; the rest keep the plain role. Model sizes apply when allow_model_scaling is enabled. The extreme Muncher preset is optional. SCP-106/173/939/096 profiles have editable traits; see [role settings](CONFIGURATION.md).', '',
          '## Round events', '', '| Event | What happens | Staff controls |', '| --- | --- | --- |',
          f'| Infection | Low-health SCP-3114 hunt humans. Their direct kills convert victims at the death point. Other deaths stay dead. | {usage["events"]["infection"]} |',
          f'| Deathmatch | Chaos has taken the facility (spawns in Heavy Containment and Entrance); MTF raids from the surface. No SCPs. Both sides respawn in waves every 15s; first to 40 kills, or the leader after 15 minutes, wins. | {usage["events"]["deathmatch"]} |',
          f'| Juggernaut | One Chaos Juggernaut in heavy armor (1000 HP + 150 per hunter, heals 75 per kill) against NTF hunters, who get its location every 30s. Killing the Juggernaut makes you the next one (+3 points); Juggernaut kills are +1. First to 20 points, or the leader after 10 minutes, wins. | {usage["events"]["juggernaut"]} |', '',
          'Staff commands need four players. Automatic selection is off by default; when enabled it has a 3% chance per round, never runs two rounds in a row, and needs 12 players for Infection, 16 for Deathmatch and 10 for Juggernaut. Arena events lock the warhead and decontamination and block normal respawn waves. `bpevent status` checks progress; `bpevent stop` stops the rules, then restart the round for normal roles.', '',
          '## Other features', '', '| Feature | What it does | How to use |', '| --- | --- | --- |',
          '| SCP nearby voice | Lets enabled SCPs speak to nearby living players. | Tap N or use F1 `.scpchat`; hold the normal voice key to talk. |',
          '| XP and stats | Tracks completed actions and connected playtime. DNT and opt-out erase stats. | F1 `.pbxp`, `.pbxp top` or `.pbprivacy`. |',
          '| Discord stats | Shows your or another linked player’s level, XP and playtime; top 10 and opt-out. | `/pb-stats [user]`, `/pb-playtime [user]`, `/pb-top`, `/pb-optout`. Link first with `.pblink` → `/pb-link`. |',
          '| Captive recruitment | Chaos can recruit cuffed MTF/Guards; MTF can recruit cuffed Chaos. Either can recruit Rogues. | Escort the captive to the normal surface escape area. |',
          '| Guard escape | An uncuffed Facility Guard who reaches the surface escape area becomes an NTF Sergeant. | Walk to the normal surface escape area. |',
          '| BetterCoinflips | Gives configured random outcomes from ordinary coin flips. | Equip a normal coin and flip it. |',
          '| SCP-914 Rough teleport | Teleports a player to a random valid position. | Stand in SCP-914 and run Rough. |',
          '| Custom loot | Replaces some map pickups and gives occasional starting gear. | Automatic; owner controls per-entry percentages. |',
          '| Automatic updates | Downloads verified updates for installed plugins. | Staff load downloaded updates with a full server restart. |', '',
          '## SCP-914 crafting', '', 'Put the input in SCP-914 and run the listed setting. Recipes are chance-based and share natural supply limits.', '',
          '| Input | Setting | Possible output |', '| --- | --- | --- |']
recipes = []
recipe_source = (root/'SlFixes/Config.cs').read_text()
for block in re.findall(r'new CraftingRecipe\s*\{([^{}]+)\}', recipe_source):
    fields = [re.search(pattern, block) for pattern in (r'Source = ItemType\.(\w+)', r'Setting = Scp914KnobSetting\.(\w+)', r'CustomId = (\d+)', r'Name = "([^"]+)"')]
    if all(fields): recipes.append(tuple(f.group(1) for f in fields))
for name, body, setting, ident in re.findall(r'R\("([^"]+)", ItemType\.(\w+), Scp914KnobSetting\.(\w+), (\d+),', recipe_source): recipes.append((body, setting, ident, name))
assert len(recipes) == 10
bodies = {'GrenadeFlash':'Flashbang','GrenadeHE':'HE grenade','SCP500':'SCP-500','KeycardJanitor':'Janitor keycard','GunE11SR':'E11-SR'}
for body, setting, ident, name in recipes:
    setting = {'OneToOne':'1:1','VeryFine':'Very Fine'}.get(setting, setting)
    lines.append(f'| {bodies.get(body, body)} | {setting} | {name} ({ident}) |')
lines += ['', 'For equipment that will not give: check its ID, enabled state, inventory space and whether the target is alive. For a role that will not roll: read its `bproles` reason.', '']
assert len(roles) == 66
(root/'docs/CATALOG.md').write_text('\n'.join(lines))
print('Content page written: 12 items, 2 weapons, 66 roles, 3 events and 10 recipes; all have usage instructions.')

# A separate operator-facing audit records why a correctly installed profile may
# not appear. Runtime bproles remains the authority for edited server configs.
source = (root/'Roles/RoleDefinition.cs').read_text()
blocks = re.split(r'(?m)(?=^[ \t]*(?:var r = |r = )R\("[A-Z]\d\d")', source)[1:]
status = ['# Role availability audit', '',
          'All 66 profiles have an implementation and compile against the pinned EXILED/game assemblies. This is a code/default-configuration audit, not a live Unity playtest. Use `bprole` to inspect an actual assignment and `bproles [playerID]` for current runtime requirements.', '',
          '## Shipped defaults', '',
          '| ID | Role | Chance % | Default status | Required custom equipment | Minimum players |',
          '| --- | --- | --- | --- | --- | --- |']
assert len(blocks) == 66
known_ids = set(re.findall(r'Id\s*=\s*(\d{4})', (root/'Items/Definition.cs').read_text()+(root/'Weapons/Definition.cs').read_text()))
for block in blocks:
    match = re.search(r'R\("([A-Z]\d\d)",\s*"([^"]+)"', block)
    role_id, name = match.groups()
    equipment = re.findall(r'\b(7[12]\d\d)\b', block)
    assert set(equipment) <= known_ids, f'{role_id} references unknown equipment'
    gate = 'Enabled; normal spawn roll'
    if 'r.Experimental = true' in block:
        gate = 'Opt-in: allow_experimental_roles'
    if role_id == 'Z05':
        gate = 'Event only: one zombie per round can become the Doctor'
    minimum = re.search(r'r.MinPlayers = (\d+)', block)
    base = re.search(r'RoleTypeId\.(\w+)', block).group(1)
    status.append(f'| {role_id} | {name} | {percentage(role_id, base)} | {gate} | {", ".join(equipment) or "None"} | {minimum.group(1) if minimum else "0"} |')
status += ['', '## Why a role can be absent', '',
           '- Each profile has a direct chance_percent. Groups total at most 100 including disabled/experimental entries; unused and blocked shares keep the native role. Defaults share an 80% total per group (two decimals, rounded down); catalog revision 3 raised existing configs to the same 80% once, keeping relative shares.',
           '- Missing/disabled Items or Weapons, spent kit supply, population thresholds, per-round profile caps, living-human specialist limits and reinforcement wave limits can exclude a profile. These restrictions do not redistribute a blocked profile\'s percentage to another role.',
           '- Head Scientist and Demolitionist require 12 players. MTF/Chaos profiles preserve their matching native rank/subtype.',
           '- Doctor needs enable_doctor_event, its Z05 profile enabled, 12 players and a living SCP-049. Its separate event roll (default 25%, once per round) never enters ordinary zombie draws. It revives like SCP-049, with 35% health, no hume shield, slower movement, attacks and revives.',
           '- Candy roles use real SCP-330 candy; approved_candy can select colors and candy_man_candy_count controls the Candy Man count. Janitor/Athletic/Pharmacist kits use their documented balanced supplies rather than the original Discord brainstorm.',
           '- Rogue Guard/Scientist stay real Guard/Scientist roles but can damage, and be damaged by, everyone including their own side. They spawn separately in Entrance and queue an early CASSIE warning. Containment Engineer starts just outside the HCZ Micro H.I.D. room; map geometry must provide a safe point. Head Scientist reuses S03 with its research-coordinator keycard.',
           '- Rank roles only roll from their listed ranks (for example Scout from MTF Private or Chaos Rifleman). Captured Insurgent rolls from a Class-D spawn and becomes an empty-handed Chaos Conscript in the cells. Custom humans are capped at custom_human_share_percent (80%) of living humans; reinforcement_specialists_per_wave (0 = no cap) limits MTF/Chaos roles per wave.',
           '- Locksmith can pick every keycard tier: 15/30/45-second attempts and 120/180/240-second cooldowns by default. Facility system locks and non-interactable doors keep their native restrictions.',
           '- Native admin force-class changes and escapes do not randomly reroll profiles. Use bprole set <ID> [playerID] for a deliberate admin test. It schedules a fresh native kit and ignores natural chance/population/supply limits, while honoring disabled content and missing providers.', '',
           'All role commands require brainless.roles and are registered only in Remote Admin. READY in bproles means eligible to roll now, not guaranteed or live-test certified. See PLAYTEST.md for traits, resurrection, client prediction and cleanup checks.', '']
audit = root/'docs/ROLE_STATUS.md'
status += ['## SCP variants', '', 'The SCP-106, SCP-173, SCP-939 and SCP-096 profiles use normal SCP controls. Stalker and Ambusher reduce footsteps; they keep native ambient sounds. Mimic has a short sound cooldown; intercom access was removed because it disconnected players. Escape\'s kills restart breakneck and clear the cooldown that follows. Poison refreshes one effect, does not stack, and medical supplies cure it. Boom Boom uses its separate explosive_scp173 settings.', '', 'Brute, Giant, Muncher, Raisian and Alpha apply their configured model sizes when allow_model_scaling is true. Child, Tickler and Muncher are no longer experimental. Profiles keep the native hume shield unless their shield multiplier changes it; the Doctor never has one.', '']
audit.write_text('\n'.join(status))
print('Role availability audit written with all 66 profiles and validated equipment IDs.')
