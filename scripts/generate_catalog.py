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

role_rarity = dict(re.findall(r'\["([A-Z]\d\d)"\] = Rarity\.(\w+)', source))
assert set(role_rarity) == {r[0] for r in roles}, 'Every role needs a default rarity.'

def item_rarities(text, constants):
    body = text[text.index('static Rarity DefaultRarity(uint id)'):]
    body = body[:body.index('\n        }\n')]
    found = {}
    for ids, tier in re.findall(r'((?:case [\w]+:\s*)+)return Rarity\.(\w+);', body):
        for key in re.findall(r'case (\w+):', ids):
            found[constants.get(key, key)] = tier
    return found

rarity_of = item_rarities((root/'Items/Definition.cs').read_text(), {})
rarity_of.update(item_rarities((root/'Weapons/Definition.cs').read_text(), {'CursedPistolId': '7210', 'CursedShotgunId': '7211'}))
def item_rarity(item_id):
    if item_id in rarity_of: return rarity_of[item_id]
    return 'Epic' if 7123 <= int(item_id) <= 7132 else 'Common'

usage = json.loads((root/'docs/content_usage.json').read_text())
assert set(usage['roles']) == {r[0] for r in roles}, 'Every role needs short usage instructions.'

# Per-role availability (chance, gate, required equipment, population) for the role table.
audit_blocks = re.split(r'(?m)(?=^[ \t]*(?:var r = |r = )R\("[A-Z]\d\d")', source)[1:]
assert len(audit_blocks) == 66
known_ids = set(re.findall(r'Id\s*=\s*(\d{4})', (root/'Items/Definition.cs').read_text()+(root/'Weapons/Definition.cs').read_text()))
availability = {}
for block in audit_blocks:
    role_id = re.search(r'R\("([A-Z]\d\d)"', block).group(1)
    equipment = re.findall(r'\b(7[12]\d\d)\b', block)
    assert set(equipment) <= known_ids, f'{role_id} references unknown equipment'
    gate = 'Enabled'
    if 'r.Experimental = true' in block: gate = 'Opt-in (allow_experimental_roles)'
    if role_id == 'Z05': gate = 'Event: 25% per round'
    minimum = re.search(r'r.MinPlayers = (\d+)', block)
    base = re.search(r'RoleTypeId\.(\w+)', block).group(1)
    availability[role_id] = (percentage(role_id, base), gate, ', '.join(equipment) or '—', minimum.group(1) if minimum else '0')

lines = ['# Project Brainless — What We Have', '',
         '[Command table](COMMANDS.md) · [Project overview and settings](../README.md)', '',
         'Current shipped content: **22 items, 5 weapons, 66 role profiles and 3 round events**. Values below are defaults; server settings can differ.', '',
         '**Tool key:** Settings → Server Specific → Use custom item or nearby device (default **V**). Hold for timed actions; tap for instant actions. Grenades use normal throw controls.', '',
         '## Rarity', '', 'Every item, weapon and role has a rarity tier. Each loot pickup or spawn rolls a tier first, then something of that tier. Defaults (items and roles have separate per-round counts; Items and Weapons share theirs):', '',
         '| Tier | Chance per roll | Max per round |', '| --- | --- | --- |',
         '| Common | 40% | no limit |', '| Uncommon | 25% | 12 |', '| Rare | 12% | 6 |', '| Epic | 6% | 4 |', '| Legendary | 3% | 2 |', '| Mythical | 1% | 1-2 (rolled each round) |', '',
         'Map loot uses the full chance (a tier with nothing for that pickup gives nothing). Starting gear rolls at 30% of it. Cursed items keep their own 5% roll but still count toward the tier limits. For roles, a tier with nothing available for that class falls to the next tier down. Players offered a reroll at spawn (35%) can use F1 `.scpreroll` once per round within 30 seconds; it always gives a different custom role, Rare or lower.', '',
         '## Items', '', '| ID | Item | Rarity | What it does | How to use |', '| --- | --- | --- | --- | --- |']
for folder, label in [('Items', 'Items'), ('Weapons', 'Weapons')]:
    content_source = (root/folder/'Definition.cs').read_text()
    if folder == 'Items': content_source += (root/folder/'SupCatalog.cs').read_text()
    entries = []
    for block in re.findall(r'new (?:ItemSpec|WeaponSpec)\s*\{([^{}]+)\}', content_source):
        fields = [re.search(pattern, block) for pattern in (r'\bId\s*=\s*(\d+)', r'\bName\s*=\s*"([^"]+)"', r'\bDescription\s*=\s*"([^"]+)"')]
        if all(fields): entries.append(tuple(field.group(1) for field in fields))
    entries.sort(key=lambda e: int(e[0]))
    assert len(entries) == (22 if folder == 'Items' else 5)
    controls = usage[folder.lower()]
    assert set(controls) == {e[0] for e in entries}, f'Usage coverage missing for {folder}'
    if folder == 'Weapons':
        lines += ['', '## Weapons', '', '| ID | Weapon | Rarity | What it does | How to use |', '| --- | --- | --- | --- | --- |']
    for item_id, name, description in entries:
        if item_id == '7206': name += ' **(opt-in)**'
        if item_id == '7207': name += ' **(staff only)**'
        lines.append(f'| {item_id} | {name} | {item_rarity(item_id)} | {description} | {controls[item_id]} |')
lines += ['', 'Ricochet requires experimental weapons to be enabled. Give equipment with `ci give <ID> [playerID]`; use `ci list registered` to see what is enabled.', '',
          '## Roles', '', 'Traits apply automatically. Use `bprole set <ID> [playerID]` to test; `bproles` shows percentages and blocked reasons.', '',
          '| ID | Role | Rarity | What it does | How to use | Status | Custom kit | Min players |', '| --- | --- | --- | --- | --- | --- | --- | --- |']
for role_id, name, base, hint in roles:
    if role_id == 'Z02': hint = 'Faster zombie with less health and weaker attacks.'
    chance, status, kit, minimum = availability[role_id]
    lines.append(f'| {role_id} | {name} | {role_rarity[role_id]} | {hint} | {usage["roles"][role_id]} | {status} | {kit} | {minimum} |')
lines += ['', 'About 87% of spawns roll a custom role by default (the tier chances added up); the rest keep the plain role. Model sizes apply when allow_model_scaling is enabled. The extreme Muncher preset is optional. SCP-106/173/939/096 profiles have editable traits under `brainless_roles.roles`. Chances above are shipped defaults; `bproles [playerID]` shows the live values and blocked reasons.', '',
          '### Why a role can be absent', '',
          '- With use_rarity (default) each spawn rolls a tier from role_rarity, then a role of that tier for the class; chance_percent only weights roles inside a tier (0 turns a role off). With use_rarity off, each profile has a direct chance_percent and groups total at most 100.',
          '- Missing/disabled Items or Weapons, spent kit supply, population thresholds, per-round profile caps, living-human specialist limits and reinforcement wave limits can exclude a profile. In rarity mode another role of the same tier (or the next tier down) is tried instead.',
          '- Doctor needs enable_doctor_event, its Z05 profile enabled, 12 players and a living SCP-049. Its separate event roll (default 25%, once per round) never enters ordinary zombie draws. It revives like SCP-049, with 35% health, no hume shield, slower movement, attacks and revives.',
          '- Rank roles only roll from their listed ranks. Custom humans are capped at custom_human_share_percent (80%) of living humans; reinforcement_specialists_per_wave (0 = no cap) limits MTF/Chaos roles per wave.',
          '- Locksmith can pick every keycard tier: 15/30/45-second attempts and 120/180/240-second cooldowns by default. Facility system locks still apply.',
          '- Remote Admin forceclass rolls a profile with the normal chances (force_class_custom_roles); escapes and plugin role changes do not reroll. `bprole set <ID> [playerID]` ignores natural chance/population/supply limits but honors disabled content and missing providers.',
          '- SCP variants use normal SCP controls. Stalker and Ambusher reduce footsteps. Poison refreshes one effect, does not stack, and medical supplies cure it. Brute, Giant, Muncher, Raisian and Alpha apply model sizes only when allow_model_scaling is true. Boom Boom uses its separate explosive_scp173 settings.', '',
          '## Round events', '', '| Event | What happens | Staff controls |', '| --- | --- | --- |',
          f'| Infection | Everyone is spread across Heavy Containment and Entrance, survivors in separate rooms away from the skeletons. Low-health SCP-3114 hunt humans; their direct kills convert victims at the death point. Other deaths stay dead. | {usage["events"]["infection"]} |',
          f'| Deathmatch | Chaos has taken the facility (spawns in Heavy Containment and Entrance); MTF raids from the surface. No SCPs; Light Containment is locked off. Both sides respawn in waves every 15s; first to 40 kills, or the leader after 15 minutes, wins. | {usage["events"]["deathmatch"]} |',
          f'| Juggernaut | One Chaos Juggernaut in heavy armor (1000 HP + 150 per hunter, heals 75 per kill) against NTF hunters. Everyone has SCP-1344 goggle vision, and hunters get a live tracker with the Juggernaut’s distance, direction, room and HP. Light Containment is locked off. Killing the Juggernaut makes you the next one (+3 points); Juggernaut kills are +1. First to 20 points, or the leader after 10 minutes, wins. | {usage["events"]["juggernaut"]} |', '',
          'Staff commands need four players. Automatic selection is off by default; when enabled it has a 3% chance per round, the lobby then pauses for up to 45 seconds while players vote with F1 `.vote` (or pick No event; anyone can sit the event out with `.vote optout`), never runs two rounds in a row, and needs 12 players for Infection, 16 for Deathmatch and 10 for Juggernaut. Arena events lock the warhead, decontamination and the Light Containment checkpoints/elevators, and block normal respawn waves. `bpevent status` checks progress; `bpevent stop` stops the rules, then restart the round for normal roles.', '',
          '## Other features', '', '| Feature | What it does | How to use |', '| --- | --- | --- |',
          '| SCP nearby voice | Lets enabled SCPs (including SCP-939) speak to nearby living players. SCP-939 is heard from its mimic point while one is placed. The role HUD shows whether it is on. | Tap N or use F1 `.scpchat`; hold the normal voice key to talk. |',
          '| Role HUD | Your role (or the spectated player\'s) and SCP proximity chat status on the side of the screen. | Fade it in/out with the HUD key (default H) or the Role HUD switch in Settings → Server Specific. |',
          '| Zombie opt-out | Players can skip being revived as SCP-049-2; a spectator takes the spot and keeps the zombie\'s custom role, or SCP-049 gets +200 HP. | Settings → Server Specific → Zombie Opt-out. Spectators type F1 `.optin` or turn on Auto-fill. |',
          '| SCP fill | If an SCP disconnects or jumps into a pit in the first 200 seconds, another player can take over its SCP, custom role and remaining health. | F1 `.fill` when the broadcast appears. |',
          '| XP and stats | Tracks completed actions and connected playtime. DNT and opt-out erase stats. | F1 `.pbxp`, `.pbxp top` or `.pbprivacy`. |',
          '| Discord stats | Shows your or another linked player’s level, XP and playtime; top 10 and opt-out. | `/pb-stats [user]`, `/pb-playtime [user]`, `/pb-top`, `/pb-optout`. Link first with `.pblink` → `/pb-link`. |',
          '| Captive recruitment | Chaos can recruit cuffed MTF/Guards; MTF can recruit cuffed Chaos. | Escort the captive to the normal surface escape area. |',
          '| Guard escape | An uncuffed Facility Guard who reaches the surface escape area becomes an NTF Sergeant. | Walk to the normal surface escape area. |',
          '| End-of-round friendly fire | When the round ends, friendly fire turns on so everyone can attack everyone until the restart. It is not punished. | Automatic; owners set `end_of_round_friendly_fire` in the Fixes config. |',
          '| BetterCoinflips | Gives configured random outcomes from ordinary coin flips. Heads can give a Brainless item or weapon or turn you into a fitting custom role; tails can give a cursed item. Coin class changes roll a custom role for the new class. | Equip a normal coin and flip it. |',
          '| SCP swap | SCPs can trade SCPs (or take an unplayed one) shortly after round start. Custom variants stay with their SCP, so you take over e.g. Boom Boom. | F1 `.scpswap list`, `.scpswap 173`, then the other SCP answers `.scpswap yes`. Staff: `bpscpswap`. |',
          '| Staff force-class roles | A Remote Admin forceclass rolls a custom role for the new class with the normal chances. | Use forceclass as usual; owners set `force_class_custom_roles`. |',
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
print('Content page written: 22 items, 5 weapons, 66 roles (with availability), 3 events and 10 recipes; all have usage instructions.')
