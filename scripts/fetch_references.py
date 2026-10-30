#!/usr/bin/env python3
"""Fetch the exact EXILED 9.15.2 build inputs; never package them as plugin output."""
import argparse
import hashlib
import io
import pathlib
import shutil
import tarfile
import urllib.request
import zipfile

ROOT = pathlib.Path(__file__).resolve().parents[1]
SOURCES = [
    ('https://exmod-team.github.io/SL-References/Master.zip',
     '769477523d740752b929a46194cacfa7fd399a4f016ef4c6226b669a27c66a7f', 'zip'),
    ('https://github.com/ExMod-Team/EXILED/releases/download/v9.15.2/Exiled.tar.gz',
     '6b0537f92f911657e2187b079905781c9c0606fc8e6cef0ce8f737f4d8308ec8', 'tar'),
]

def fetch(destination):
    destination.mkdir(parents=True, exist_ok=True)
    for url, expected, kind in SOURCES:
        print('Fetching', url, flush=True)
        request = urllib.request.Request(url, headers={'User-Agent': 'BrainlessProjectPlugins-build'})
        with urllib.request.urlopen(request, timeout=120) as response:
            data = response.read()
        actual = hashlib.sha256(data).hexdigest()
        if actual != expected:
            raise RuntimeError(f'Reference checksum changed for {url}. Expected {expected}; got {actual}. Review the upstream update before changing the pin.')
        if kind == 'zip':
            with zipfile.ZipFile(io.BytesIO(data)) as archive:
                for name in archive.namelist():
                    if name.endswith('.dll'):
                        (destination/pathlib.PurePosixPath(name).name).write_bytes(archive.read(name))
        else:
            with tarfile.open(fileobj=io.BytesIO(data), mode='r:gz') as archive:
                for member in archive.getmembers():
                    if member.isfile() and member.name.endswith('.dll'):
                        stream = archive.extractfile(member)
                        (destination/pathlib.PurePosixPath(member.name).name).write_bytes(stream.read())
    for required in ['Exiled.API.dll', 'Exiled.Events.dll', 'Exiled.CustomItems.dll', 'Assembly-CSharp-Publicized.dll']:
        if not (destination/required).is_file():
            raise RuntimeError(f'Missing build input: {required}')
    (destination/'versions.txt').write_text('EXILED 9.15.2\nSCP SL 14.2.8 reference snapshot\n')
    print('References prepared at', destination)

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--destination', type=pathlib.Path, default=ROOT/'.build/references')
    args = parser.parse_args()
    fetch(args.destination.resolve())
