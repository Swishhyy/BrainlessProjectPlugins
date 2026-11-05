#!/usr/bin/env python3
"""Collect independently verified component pairs into one complete release."""
import argparse
import hashlib
import json
import os
import pathlib
import shutil
import subprocess
import xml.etree.ElementTree as ET
import zipfile
from prepare_release import PROJECTS, ROOT
from publish_release import publish_assets

def collect(assets, repository):
    files = {}
    for file in sorted(assets.rglob('*')):
        if not file.is_file(): continue
        if file.name in files:
            raise ValueError('Duplicate release asset: '+file.name)
        files[file.name] = file
    expected = {assembly+suffix for assembly, _ in PROJECTS.values() for suffix in ('.dll', '.update.json')}
    if set(files) != expected:
        raise ValueError('Combined release requires exactly one DLL and manifest for every plugin.')
    components = []
    for plugin, (assembly, project) in PROJECTS.items():
        dll, manifest = files[assembly+'.dll'], files[assembly+'.update.json']
        data = json.loads(manifest.read_text())
        entries = data.get('plugins', [])
        if data.get('schema_version') != 1 or data.get('repository') != repository or len(entries) != 1:
            raise ValueError('Invalid component manifest: '+plugin)
        entry = entries[0]
        version = ET.parse(ROOT/project).findtext('.//Version')
        expected_entry = {'id': plugin, 'assembly_name': assembly, 'asset': dll.name, 'version': version,
                          'sha256': hashlib.sha256(dll.read_bytes()).hexdigest(), 'exiled_min': '9.15.3',
                          'game_version': '14.2.9', 'target_framework': 'net48'}
        if any(entry.get(key) != value for key, value in expected_entry.items()):
            raise ValueError('Component identity, version, compatibility or checksum mismatch: '+plugin)
        components.append({'plugin': plugin, 'version': version, 'dll': dll, 'manifest': manifest})
    return components

def prepare_bundle(assets, output, repository):
    components = collect(assets, repository)
    output.mkdir(parents=True, exist_ok=True)
    install = ('# Project Brainless Plugins\n\n'
               'Eight separate plugins for EXILED 9.15.3 / SCP:SL 14.2.9.\n\n'
               '1. Fully stop the server.\n'
               '2. Copy the DLLs you want from Plugins/ into EXILED/Plugins, replacing prior copies.\n'
               '3. Keep existing configuration and start the server. New settings are generated.\n\n'
               'Each plugin is optional and keeps its own version/configuration. The manifests on the release '
               'page are for automatic updates; they do not go in the Plugins directory.\n'
               'Staff: open docs/CATALOG.md for all content/controls, or docs/COMMANDS.md for commands and permissions.\n'
               'Affected old updaters require this manual replacement once, including Core if installed. '
               'Future updates load after a full server restart.\n\n'
               f'Instructions: https://github.com/{repository}/blob/main/README.md\n')
    archive_path = output/'ProjectBrainless-Plugins.zip'
    with zipfile.ZipFile(archive_path, 'w', compression=zipfile.ZIP_DEFLATED) as archive:
        entries = {'README.md': install.encode('utf-8')}
        entries.update({'docs/'+str(p.relative_to(ROOT/'docs')): p.read_bytes() for p in (ROOT/'docs').rglob('*.md')})
        entries.update({'Plugins/'+c['dll'].name: c['dll'].read_bytes() for c in components})
        for name, content in sorted(entries.items()):
            info = zipfile.ZipInfo(name, date_time=(1980, 1, 1, 0, 0, 0))
            info.compress_type = zipfile.ZIP_DEFLATED
            info.external_attr = 0o100644 << 16
            archive.writestr(info, content)
    # Data before manifests, also safe when attaching to an existing release.
    files = [archive_path]
    for key in ('dll', 'manifest'):
        for component in components:
            source = component[key]
            destination = output/source.name
            shutil.copy2(source, destination)
            files.append(destination)
    notes = ('All Project Brainless plugins in one release. Projects, DLLs, configurations and versions remain separate.\n\n'
             'Download **ProjectBrainless-Plugins.zip** for everything, or choose individual DLLs below. '
             'The `.update.json` files are used by the updater; they are not server plugins. '
             'Stop the server, replace your selected DLLs, keep existing configs, then start it. '
             'If the old updater fails, replace every installed component once, including Core if installed.\n\n'
             '**Target:** EXILED 9.15.3 / SCP:SL 14.2.9. Automatic updates still replace only each installed component.\n\n'
             f'[All content and controls](https://github.com/{repository}/blob/main/docs/CATALOG.md) · [Commands](https://github.com/{repository}/blob/main/docs/COMMANDS.md). Also included under docs/ in the ZIP.\n\n'
             '| Plugin | Version | DLL |\n| --- | --- | --- |\n')
    notes += ''.join(f'| {c["plugin"]} | {c["version"]} | `{c["dll"].name}` |\n' for c in components)
    return files, notes

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--assets', required=True, type=pathlib.Path)
    parser.add_argument('--output', type=pathlib.Path, default=ROOT/'artifacts/combined')
    parser.add_argument('--tag', required=True)
    parser.add_argument('--existing-release', action='store_true')
    parser.add_argument('--prerelease', action='store_true')
    args = parser.parse_args()
    files, notes = prepare_bundle(args.assets, args.output, os.environ['GH_REPO'])
    try:
        publish_assets(files, args.tag, 'Project Brainless — All Plugins', notes,
                       args.existing_release, args.prerelease, latest=True)
    except subprocess.CalledProcessError as error:
        raise SystemExit(f'GitHub release command failed with exit code {error.returncode}; published assets were not clobbered.')
