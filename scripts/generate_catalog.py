#!/usr/bin/env python3
import pathlib
import re
import math
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

lines = ['# Content catalog', '', 'Stable IDs map the design document into independent plugins. All values are playtest defaults; see each generated configuration for tuning.', '',
         '## Roles', '', '| ID | Name | Base role or faction | Chance % | Specialty and cost |', '| --- | --- | --- | --- | --- |']
for role_id, name, base, hint in roles:
    chance = percentage(role_id, base)
    if role_id.startswith('M'): base = 'MTF with original rank'
    elif role_id.startswith('C'): base = 'Chaos with original subtype'
    lines.append(f'| {role_id} | {name} | {base} | {chance} | {hint} |')
lines += ['', 'Percentages are direct matching-spawn shares; blocked/unused shares keep the native role. Group totals include experimental/disabled profiles. Doctor is a separate event roll. See STAFF_GUIDE.md for editing. Child, Tickler, Muncher and Doctor are experimental. Doctor is excluded from ordinary zombie draws. Candy roles use real SCP-330 candy; an empty allowlist selects registered standard colors.', '']
for folder, label in [('Items', 'Items'), ('Weapons', 'Weapons')]:
    source = (root/folder/'Definition.cs').read_text()
    if folder == 'Items': source += (root/folder/'SupCatalog.cs').read_text()
    blocks = re.findall(r'new (?:ItemSpec|WeaponSpec)\s*\{([^{}]+)\}', source)
    entries = []
    for block in blocks:
        fields = [re.search(pattern, block) for pattern in (r'\bId\s*=\s*(\d+)', r'\bName\s*=\s*"([^"]+)"', r'\bDescription\s*=\s*"([^"]+)"')]
        if all(fields): entries.append(tuple(field.group(1) for field in fields))
    entries.sort(key=lambda entry: int(entry[0]))
    assert len(entries) == (21 if folder == 'Items' else 8), f'Incomplete {label} catalog: {len(entries)} entries'
    lines += [f'## {label}', '', '| Design ID | Registry ID | Name | Behavior |', '| --- | --- | --- | --- |']
    prefix = 'I' if folder == 'Items' else 'W'
    for index, (item_id, name, description) in enumerate(entries, 1):
        design_id = int(item_id) - (7100 if folder == 'Items' else 7200)
        lines.append(f'| {prefix}{design_id:02d} | {item_id} | {name} | {description} |')
    lines.append('')
lines += ['## Initial supply', '', '| Population | Utility items including kits and loot | Custom firearms | Cluster maximum |',
          '| --- | --- | --- | --- |', '| Below 12 | 6 | 1 | 0 |', '| 12 through 23 | 10 | 1 | 1 |', '| 24 or more | 14 | 2 | 2 |', '',
          'Smoke grenades occupy one slot and create no filter. Guard badges and ordinary coins are outside these utility counts. Limits apply across a whole round, not per wave or per pickup.', '',
          'Registered item IDs can be used with EXILED CustomItems Remote Admin commands. A missing provider or spent supply budget excludes that role from the draw; another eligible profile can still be selected. Redline and Ricochet require the experimental weapon switch. Admin give/spawn ignores supply caps. The Grenade Launcher uses 16 grenades total and cannot reload from ordinary ammo.', '']
assert len(roles) == 35, f'Expected 35 roles; got {len(roles)}'
(root/'docs/CATALOG.md').write_text('\n'.join(lines))
print('Catalog written with 35 roles, 21 items and 8 weapon definitions.')

# A separate operator-facing audit records why a correctly installed profile may
# not appear. Runtime bproles remains the authority for edited server configs.
source = (root/'Roles/RoleDefinition.cs').read_text()
blocks = re.split(r'(?m)(?=^[ \t]*(?:var r = |r = )R\("[A-Z]\d\d")', source)[1:]
status = ['# Role availability audit', '',
          'All 35 profiles have an implementation and compile against the pinned EXILED/game assemblies. This is a code/default-configuration audit, not a live Unity playtest. Use `bprole` to inspect an actual assignment and `bproles [playerID]` for current runtime requirements.', '',
          '## Shipped defaults', '',
          '| ID | Role | Chance % | Default status | Required custom equipment | Minimum players |',
          '| --- | --- | --- | --- | --- | --- |']
assert len(blocks) == 35
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
previous = audit.read_text() if audit.exists() else ''
extra = previous.partition('## Additional Discord concepts not implemented')[2]
if extra: status += ['## Additional Discord concepts not implemented'+extra]
audit.write_text('\n'.join(status))
print('Role availability audit written with all 35 profiles and validated equipment IDs.')
