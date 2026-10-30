#!/usr/bin/env python3
"""Publish one plugin's immutable assets using GitHub Actions' scoped GH_TOKEN."""
import argparse
import hashlib
import json
import os
import pathlib
import subprocess
import tempfile
from urllib.parse import quote

def gh(*args, check=True):
    return subprocess.run(['gh', *args], check=check, capture_output=True, text=True)

def publish(plugin, version, dll, manifest, tag, existing_release=False, prerelease=False):
    repository = os.environ['GH_REPO']
    current = gh('api', f'repos/{repository}/releases/tags/{quote(tag, safe="")}', check=False)
    if existing_release:
        if current.returncode:
            raise RuntimeError('The published release could not be read.')
        state = json.loads(current.stdout)
        if state['draft']:
            raise RuntimeError('The release-published job expected a published release.')
        # No clobber: a published asset is never silently replaced at the same version.
        # Upload manifest last so an updater never sees an incomplete pair.
        for file in (dll, manifest):
            match = next((asset for asset in state.get('assets', []) if asset['name'] == file.name), None)
            if match is None:
                gh('release', 'upload', tag, str(file))
            elif (match.get('digest') or '').lower() != 'sha256:'+hashlib.sha256(file.read_bytes()).hexdigest():
                raise RuntimeError('A published asset already exists with different content; use a new version.')
        return
    if not current.returncode:
        state = json.loads(current.stdout)
        if not state['draft']:
            expected = {file.name: 'sha256:'+hashlib.sha256(file.read_bytes()).hexdigest() for file in (dll, manifest)}
            assets = {a['name']: (a.get('digest') or '').lower() for a in state.get('assets', [])}
            if all(assets.get(name) == digest for name, digest in expected.items()):
                print(f'{tag} already contains these verified assets; skipped.')
                return
            print(f'{tag} is already published; keeping its original assets. Bump the project version to release changed code.')
            return
        if state['target_commitish'] != os.environ['GITHUB_SHA']:
            raise RuntimeError('An existing draft targets another commit; refusing to replace its assets.')
    else:
        notes = (f'{plugin} {version} for EXILED 9.15.2 / SCP: SL 14.2.8.\n\n'
                 'Download the DLL for manual installation. Existing installations with the updater '
                 'will verify and download this plugin independently; the new version loads on the next full server restart.\n')
        detail = pathlib.Path(__file__).resolve().parents[1]/plugin/'RELEASE_NOTES.md'
        if detail.is_file():
            notes += '\n'+detail.read_text()
        with tempfile.TemporaryDirectory() as directory:
            body = pathlib.Path(directory)/'release-notes.md'
            body.write_text(notes)
            args = ['release', 'create', tag, '--draft', '--target', os.environ['GITHUB_SHA'],
                    '--title', f'{plugin} {version}', '--notes-file', str(body)]
            if prerelease: args.append('--prerelease')
            gh(*args)
    gh('release', 'upload', tag, str(dll), str(manifest), '--clobber')
    gh('release', 'edit', tag, '--draft=false', '--prerelease='+str(prerelease).lower())

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--plugin', required=True)
    parser.add_argument('--version', required=True)
    parser.add_argument('--dll', required=True, type=pathlib.Path)
    parser.add_argument('--manifest', required=True, type=pathlib.Path)
    parser.add_argument('--tag', required=True)
    parser.add_argument('--existing-release', action='store_true')
    parser.add_argument('--prerelease', action='store_true')
    args = parser.parse_args()
    try:
        publish(args.plugin, args.version, args.dll, args.manifest, args.tag, args.existing_release, args.prerelease)
    except subprocess.CalledProcessError as error:
        # gh's stderr can contain URLs; don't dump auth headers or environment data.
        raise SystemExit(f'GitHub release command failed with exit code {error.returncode}; published assets were not clobbered.')
