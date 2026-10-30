#!/usr/bin/env python3
"""Prepare one independent DLL and its updater manifest; never bundle game references."""
import argparse
import hashlib
import json
import os
import pathlib
import re
import shutil
import xml.etree.ElementTree as ET

ROOT = pathlib.Path(__file__).resolve().parents[1]
PROJECTS = {
    'Roles': ('Brainless.Roles', 'Roles/Brainless.Roles.csproj'),
    'Items': ('Brainless.Items', 'Items/Brainless.Items.csproj'),
    'Weapons': ('Brainless.Weapons', 'Weapons/Brainless.Weapons.csproj'),
    'BetterCoinflips': ('BetterCoinflips', 'BetterCoinflips/BetterCoinflips.csproj'),
}

def prepare(plugin, output):
    assembly, project = PROJECTS[plugin]
    version = ET.parse(ROOT/project).findtext('.//Version')
    if not re.fullmatch(r'\d+\.\d+\.\d+(\.\d+)?', version or ''):
        raise ValueError('Project needs an explicit stable numeric version.')
    exiled = ET.parse(ROOT/'Directory.Build.props').findtext('.//ExiledVersion')
    source = ROOT/plugin/'bin/Release'/(assembly+'.dll')
    if not source.is_file():
        raise FileNotFoundError(f'Build {plugin} first: {source}')
    output.mkdir(parents=True, exist_ok=True)
    destination = output/source.name
    shutil.copy2(source, destination)
    manifest = {
        'schema_version': 1,
        'repository': 'Swishhyy/BrainlessProjectPlugins',
        'plugins': [{
            'id': plugin, 'assembly_name': assembly, 'asset': source.name,
            'version': version, 'sha256': hashlib.sha256(source.read_bytes()).hexdigest(),
            'exiled_min': exiled, 'game_version': '14.2.8', 'target_framework': 'net48',
        }],
    }
    manifest_path = output/(assembly+'.update.json')
    manifest_path.write_text(json.dumps(manifest, indent=2)+'\n')
    return {'plugin': plugin, 'assembly': assembly, 'version': version,
            'tag': plugin.lower()+'-v'+version, 'dll': str(destination), 'manifest': str(manifest_path)}

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--plugin', required=True, choices=list(PROJECTS))
    parser.add_argument('--output', type=pathlib.Path)
    args = parser.parse_args()
    values = prepare(args.plugin, args.output or ROOT/'artifacts/release'/args.plugin)
    print(json.dumps(values))
    if os.environ.get('GITHUB_OUTPUT'):
        with open(os.environ['GITHUB_OUTPUT'], 'a') as file:
            file.writelines(f'{key}={value}\n' for key, value in values.items())
