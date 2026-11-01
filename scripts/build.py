#!/usr/bin/env python3
"""Build and package independent projects; a failure does not skip the remaining builds."""
import argparse
import hashlib
import json
import os
import pathlib
import shutil
import subprocess
import sys
import zipfile

ROOT = pathlib.Path(__file__).resolve().parents[1]
PROJECTS = {
    'Roles': ('Roles/Brainless.Roles.csproj', 'Brainless.Roles.dll'),
    'Items': ('Items/Brainless.Items.csproj', 'Brainless.Items.dll'),
    'Weapons': ('Weapons/Brainless.Weapons.csproj', 'Brainless.Weapons.dll'),
    'BetterCoinflips': ('BetterCoinflips/BetterCoinflips.csproj', 'BetterCoinflips.dll'),
    'ScpProximityChat': ('ScpProximityChat/Brainless.ScpProximityChat.csproj', 'Brainless.ScpProximityChat.dll'),
    'Core': ('Core/Brainless.Core.csproj', 'Brainless.Core.dll'),
    'RoundEvents': ('RoundEvents/Brainless.RoundEvents.csproj', 'Brainless.RoundEvents.dll'),
    'SlFixes': ('SlFixes/Brainless.SlFixes.csproj', 'Brainless.SlFixes.dll'),
}
parser = argparse.ArgumentParser()
parser.add_argument('--plugin', choices=list(PROJECTS))
parser.add_argument('--dotnet', default=os.environ.get('BRAINLESS_DOTNET', 'dotnet'))
parser.add_argument('--package-only', action='store_true', help='Package existing Release DLLs without invoking the compiler.')
args = parser.parse_args()
output = ROOT/'artifacts'
output.mkdir(exist_ok=True)
failed = []
manifest = {'exiled': '9.15.2', 'scp_sl': '14.2.8', 'plugins': []}
for name, (project, assembly) in PROJECTS.items():
    if args.plugin and name != args.plugin:
        continue
    if not args.package_only:
        result = subprocess.run([args.dotnet, 'build', project, '-c', 'Release', '--nologo'], cwd=ROOT)
        if result.returncode:
            failed.append(name)
            continue
    source = ROOT/name/'bin/Release'/assembly
    if not source.is_file():
        print(f'Missing compiled output: {source}', file=sys.stderr)
        failed.append(name)
        continue
    folder = output/name
    if folder.exists(): shutil.rmtree(folder)
    folder.mkdir(exist_ok=True)
    shutil.copy2(source, folder/assembly)
    (folder/'README.md').write_text((ROOT/name/'README.md').read_text().replace('../docs/', 'docs/'))
    for page in (ROOT/'docs').rglob('*.md'):
        destination = folder/'docs'/page.relative_to(ROOT/'docs')
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(page, destination)
    with zipfile.ZipFile(output/(name+'.zip'), 'w', compression=zipfile.ZIP_DEFLATED) as archive:
        for file in sorted(folder.rglob('*')):
            if file.is_file(): archive.write(file, str(file.relative_to(folder)))
    manifest['plugins'].append({'plugin': name, 'file': f'{name}/{assembly}', 'sha256': hashlib.sha256(source.read_bytes()).hexdigest()})
(output/'manifest.json').write_text(json.dumps(manifest, indent=2)+'\n')
if failed:
    print('Build failures:', ', '.join(failed), file=sys.stderr)
    sys.exit(1)
