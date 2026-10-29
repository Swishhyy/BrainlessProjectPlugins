#!/usr/bin/env python3
import pathlib
import re

root = pathlib.Path(__file__).resolve().parents[1]
source = (root/'Roles/RoleDefinition.cs').read_text()
roles = re.findall(r'R\(\s*"([A-Z]\d\d)",\s*"([^"]+)",\s*RoleTypeId\.(\w+),\s*"([^"]+)"\)', source)
lines = ['# Content catalog', '', 'Stable IDs map the design document into independent plugins. All values are playtest defaults; see each generated configuration for tuning.', '',
         '## Roles', '', '| ID | Name | Base role or faction | Specialty and cost |', '| --- | --- | --- | --- |']
for role_id, name, base, hint in roles:
    if role_id.startswith('M'): base = 'MTF with original rank'
    elif role_id.startswith('C'): base = 'Chaos with original subtype'
    lines.append(f'| {role_id} | {name} | {base} | {hint} |')
lines += ['', 'Child, Tickler, Muncher and Doctor are experimental. Doctor is excluded from ordinary zombie draws. Candy falls back to painkillers until a seasonal allowlist is approved.', '']
for folder, label in [('Items', 'Items'), ('Weapons', 'Weapons')]:
    source = (root/folder/'Definition.cs').read_text()
    entries = re.findall(r'new (?:ItemSpec|WeaponSpec)\s*\{\s*Id\s*=\s*(\d+),\s*Name\s*=\s*"([^"]+)",\s*Description\s*=\s*"([^"]+)"', source)
    assert len(entries) == (12 if folder == 'Items' else 6), f'Incomplete {label} catalog: {len(entries)} entries'
    lines += [f'## {label}', '', '| Design ID | Registry ID | Name | Behavior |', '| --- | --- | --- | --- |']
    prefix = 'I' if folder == 'Items' else 'W'
    for index, (item_id, name, description) in enumerate(entries, 1):
        lines.append(f'| {prefix}{index:02d} | {item_id} | {name} | {description} |')
    lines.append('')
lines += ['## Initial supply', '', '| Population | Utility items including kits and loot | Custom firearms | Cluster maximum |',
          '| --- | --- | --- | --- |', '| Below 12 | 6 | 1 | 0 |', '| 12 through 23 | 10 | 1 | 1 |', '| 24 or more | 14 | 2 | 2 |', '',
          'A gas grenade also creates a filter and consumes two utility slots. Guard badges and ordinary coins are outside these utility counts. Limits apply across a whole round, not per wave or per pickup.', '',
          'Registered item IDs can be used with EXILED CustomItems Remote Admin commands. A missing provider or spent supply budget causes a role to keep its ordinary kit. Redline and Ricochet require the experimental weapon switch.', '']
assert len(roles) == 35, f'Expected 35 roles; got {len(roles)}'
(root/'docs/CATALOG.md').write_text('\n'.join(lines))
print('Catalog written with 35 roles, 12 items and 6 weapon definitions.')
