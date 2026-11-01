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
def percentage(role_id, base):
    if role_id == 'Z05': return '1 (event)'
    budget = 45 if base in ('ClassD', 'Scientist', 'Scp0492') else 50 if base in ('FacilityGuard', 'Scp049') else 30
    value = math.floor(budget * weights[role_id] / totals[groups[role_id]] * 100) / 100
    return f'{value:g}'

usage = json.loads((root/'docs/content_usage.json').read_text())
assert set(usage['roles']) == {r[0] for r in roles}, 'Every role needs short usage instructions.'
lines = ['# Project Brainless — What We Have', '',
         '[Staff commands](STAFF_GUIDE.md) · [Full command table](COMMANDS.md) · [Owner settings](CONFIGURATION.md)', '',
         'Current shipped content: **21 items, 8 weapons, 49 role profiles and 1 round event**. Values below are defaults; server settings can differ.', '',
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
    assert len(entries) == (21 if folder == 'Items' else 8)
    controls = usage[folder.lower()]
    assert set(controls) == {e[0] for e in entries}, f'Usage coverage missing for {folder}'
    if folder == 'Weapons':
        lines += ['', '## Weapons', '', '| ID | Weapon | What it does | How to use |', '| --- | --- | --- | --- |']
    for item_id, name, description in entries:
        if item_id in ('7204', '7206'): name += ' **(opt-in)**'
        if item_id == '7121': description = 'Blocks visual SCP-096 targeting while powered; can fail.'
        lines.append(f'| {item_id} | {name} | {description} | {controls[item_id]} |')
lines += ['', 'Redline and Ricochet require experimental weapons to be enabled. Give equipment with `ci give <ID> [playerID]`; use `ci list registered` to see what is enabled.', '',
          '## Roles', '', 'Traits apply automatically. Use `bprole set <ID> [playerID]` to test; `bproles` shows percentages and blocked reasons.', '',
          '| ID | Role | What it does | How to use | Default status |', '| --- | --- | --- | --- | --- |']
for role_id, name, base, hint in roles:
    if role_id == 'Z02': hint = 'Faster zombie with less health and weaker attacks.'
    status = 'Off / event only' if role_id == 'Z05' else 'Opt-in' if role_id in ('D03', 'P03', 'Z02') else 'Enabled'
    lines.append(f'| {role_id} | {name} | {hint} | {usage["roles"][role_id]} | {status} |')
lines += ['', 'Model sizes apply when allow_model_scaling is enabled. The extreme Muncher preset is optional. New SCP-106/173/939/096 profiles have editable traits; see [role settings](CONFIGURATION.md).', '',
          '## Round events', '', '| Event | What happens | Staff controls |', '| --- | --- | --- |',
          '| Infection | Low-health SCP-3114 hunt humans. Their direct kills convert victims at the death point. Other deaths stay dead. | `bpevent infection next` or `bpevent infection start` |', '',
          'Infection defaults: 150 HP, zero shield, four living players minimum, automatic selection off. `bpevent status` checks it; `bpevent stop` stops rules, then restart the round for normal roles.', '',
          '## Other features', '', '| Feature | What it does | How to use |', '| --- | --- | --- |',
          '| SCP nearby voice | Lets enabled SCPs speak to nearby living players. | Tap N or use F1 `.scpchat`; hold the normal voice key to talk. |',
          '| XP and stats | Tracks completed actions and survival. | F1 `.pbxp` or `.pbxp top`. |',
          '| Discord stats | Links in-game stats to Discord when the bot is enabled. | F1 `.pblink`, then Discord `/pb-link`; use `/pb-stats` or `/pb-top`. |',
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
assert len(recipes) == 14
bodies = {'GrenadeFlash':'Flashbang','GrenadeHE':'HE grenade','SCP500':'SCP-500','KeycardJanitor':'Janitor keycard','GunE11SR':'E11-SR'}
for body, setting, ident, name in recipes:
    setting = {'OneToOne':'1:1','VeryFine':'Very Fine'}.get(setting, setting)
    lines.append(f'| {bodies.get(body, body)} | {setting} | {name} ({ident}) |')
lines += ['', 'For equipment that will not give: check its ID, enabled state, inventory space and whether the target is alive. For a role that will not roll: read its `bproles` reason.', '']
assert len(roles) == 49
(root/'docs/CATALOG.md').write_text('\n'.join(lines))
print('Content page written: 21 items, 8 weapons, 49 roles, 1 event and 14 recipes; all have usage instructions.')

# A separate operator-facing audit records why a correctly installed profile may
# not appear. Runtime bproles remains the authority for edited server configs.
source = (root/'Roles/RoleDefinition.cs').read_text()
blocks = re.split(r'(?m)(?=^[ \t]*(?:var r = |r = )R\("[A-Z]\d\d")', source)[1:]
status = ['# Role availability audit', '',
          'All 49 profiles have an implementation and compile against the pinned EXILED/game assemblies. This is a code/default-configuration audit, not a live Unity playtest. Use `bprole` to inspect an actual assignment and `bproles [playerID]` for current runtime requirements.', '',
          '## Shipped defaults', '',
          '| ID | Role | Chance % | Default status | Required custom equipment | Minimum players |',
          '| --- | --- | --- | --- | --- | --- |']
assert len(blocks) == 49
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
        gate = 'Disabled; event only; enable profile + both event/experimental switches'
    minimum = re.search(r'r.MinPlayers = (\d+)', block)
    base = re.search(r'RoleTypeId\.(\w+)', block).group(1)
    status.append(f'| {role_id} | {name} | {percentage(role_id, base)} | {gate} | {", ".join(equipment) or "None"} | {minimum.group(1) if minimum else "0"} |')
status += ['', '## Why a role can be absent', '',
           '- Each profile has a direct chance_percent. Groups total at most 100 including disabled/experimental entries; unused and blocked shares keep the native role. Defaults above are two-decimal allocations of the old nominal family budgets.',
           '- Missing/disabled Items or Weapons, spent kit supply, population thresholds, per-round profile caps, living-human specialist limits and reinforcement wave limits can exclude a profile. These restrictions do not redistribute a blocked profile\'s percentage to another role.',
           '- Chief Scientist and Demolitionist require 12 players. Interceptor needs a Chaos subtype with an AK. MTF/Chaos profiles preserve their matching native rank/subtype.',
           '- Child, Tickler and Muncher require the experimental role switch. Doctor also needs its disabled profile enabled and enable_doctor_event; its separate configurable event roll (default 1%) never enters ordinary zombie draws. Redline and Ricochet are weapon prototypes and do not gate the normal role roster.',
           '- Candy roles use real SCP-330 candy; approved_candy can select colors and candy_man_candy_count controls the Candy Man count. Janitor/Athletic/Pharmacist kits use their documented balanced supplies rather than the original Discord brainstorm.',
           '- Native admin force-class changes and escapes do not randomly reroll profiles. Use bprole set <ID> [playerID] for a deliberate admin test. It schedules a fresh native kit and ignores natural chance/population/supply limits, while honoring disabled content and missing providers.', '',
           'All role commands require brainless.roles and are registered only in Remote Admin. READY in bproles means eligible to roll now, not guaranteed or live-test certified. See PLAYTEST.md for traits, resurrection, client prediction and cleanup checks.', '']
audit = root/'docs/ROLE_STATUS.md'
status += ['## SCP variants', '', 'The SCP-106, SCP-173, SCP-939 and SCP-096 profiles are now included. Their skills use normal SCP controls. Stalker and Ambusher reduce footsteps; they keep native ambient sounds. Mimic has a short sound cooldown and intercom access; normal intercom distance, mute and timing restrictions apply. Poison refreshes one effect, does not stack, and medical supplies cure it. Boom Boom uses its separate explosive_scp173 settings.', '', 'Brute, Giant, Muncher, Raisian and Alpha apply their configured model sizes when allow_model_scaling is true. Child, Tickler and Muncher remain opt-in; Doctor remains disabled/event-only.', '']
audit.write_text('\n'.join(status))
print('Role availability audit written with all 49 profiles and validated equipment IDs.')
