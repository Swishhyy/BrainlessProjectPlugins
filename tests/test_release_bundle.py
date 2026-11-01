import hashlib
import importlib
import json
import os
import pathlib
import subprocess
import sys
import tempfile
import types
import unittest
import xml.etree.ElementTree as ET
import zipfile
from unittest.mock import patch

ROOT = pathlib.Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT/'scripts'))
bundle = importlib.import_module('publish_bundle')
publisher = importlib.import_module('publish_release')
selector = importlib.import_module('select_release')

class BundleTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = pathlib.Path(self.temp.name)
        self.assets = self.root/'staged'
        self.output = self.root/'combined'
        self.repo = 'Swishhyy/BrainlessProjectPlugins'
        for plugin, (assembly, project) in bundle.PROJECTS.items():
            folder = self.assets/('release-'+plugin)
            folder.mkdir(parents=True)
            dll = folder/(assembly+'.dll')
            dll.write_bytes(('fixture-'+plugin).encode())
            manifest = {'schema_version': 1, 'repository': self.repo, 'plugins': [{
                'id': plugin, 'assembly_name': assembly, 'asset': dll.name,
                'version': ET.parse(ROOT/project).findtext('.//Version'),
                'sha256': hashlib.sha256(dll.read_bytes()).hexdigest(),
                'exiled_min': '9.15.2', 'game_version': '14.2.8', 'target_framework': 'net48'}]}
            (folder/(assembly+'.update.json')).write_text(json.dumps(manifest))

    def first_manifest(self):
        return next(self.assets.rglob('*.update.json'))

    def test_release_contains_all_pairs_and_one_complete_install_zip(self):
        files, notes = bundle.prepare_bundle(self.assets, self.output, self.repo)
        self.assertEqual(len(files), 17)
        self.assertEqual(sum(p.suffix == '.dll' for p in files), 8)
        self.assertEqual(sum(p.name.endswith('.update.json') for p in files), 8)
        with zipfile.ZipFile(files[0]) as archive:
            expected = {'Plugins/'+a+'.dll' for a, _ in bundle.PROJECTS.values()} | {'README.md'} | {'docs/'+str(p.relative_to(ROOT/'docs')) for p in (ROOT/'docs').rglob('*.md')}
            self.assertEqual(set(archive.namelist()), expected)
            self.assertEqual(archive.read('docs/STAFF_GUIDE.md'), (ROOT/'docs/STAFF_GUIDE.md').read_bytes())
            self.assertEqual(archive.read('docs/COMMANDS.md'), (ROOT/'docs/COMMANDS.md').read_bytes())
            self.assertEqual(archive.read('docs/CATALOG.md'), (ROOT/'docs/CATALOG.md').read_bytes())
            for component in bundle.collect(self.assets, self.repo):
                self.assertEqual(archive.read('Plugins/'+component['dll'].name), component['dll'].read_bytes())
                self.assertIn(component['version'], notes)
                self.assertIn(component['dll'].name, notes)
        self.assertTrue(all(p.suffix == '.dll' for p in files[1:9]))
        self.assertTrue(all(p.name.endswith('.update.json') for p in files[9:]))

    def test_archive_is_repeatable_without_timestamp_changes(self):
        files, _ = bundle.prepare_bundle(self.assets, self.output, self.repo)
        original = files[0].read_bytes()
        files, _ = bundle.prepare_bundle(self.assets, self.root/'retry', self.repo)
        self.assertEqual(original, files[0].read_bytes())

    def test_incomplete_selection_cannot_publish(self):
        self.first_manifest().unlink()
        with self.assertRaises(ValueError): bundle.prepare_bundle(self.assets, self.output, self.repo)
        self.assertFalse(self.output.exists())

    def test_duplicate_or_extra_assets_are_rejected(self):
        p = self.first_manifest()
        extra = self.assets/'duplicate'
        extra.mkdir()
        duplicate = extra/p.name
        duplicate.write_bytes(p.read_bytes())
        with self.assertRaisesRegex(ValueError, 'Duplicate'): bundle.collect(self.assets, self.repo)
        duplicate.unlink()
        (extra/'Exiled.API.dll').write_bytes(b'framework')
        with self.assertRaises(ValueError): bundle.collect(self.assets, self.repo)

    def test_wrong_identity_version_compatibility_and_checksum_are_rejected(self):
        path = self.first_manifest()
        original = json.loads(path.read_text())
        for key, value in [('id', 'Other'), ('version', '99.0.0'), ('game_version', '99.0.0'), ('sha256', '0'*64)]:
            with self.subTest(key=key):
                data = json.loads(json.dumps(original))
                data['plugins'][0][key] = value
                path.write_text(json.dumps(data))
                with self.assertRaises(ValueError): bundle.collect(self.assets, self.repo)
        path.write_text(json.dumps(original))
        with self.assertRaises(ValueError): bundle.collect(self.assets, 'Someone/Else')

    def test_tampered_dll_is_rejected(self):
        next(self.assets.rglob('*.dll')).write_bytes(b'corrupt')
        with self.assertRaises(ValueError): bundle.collect(self.assets, self.repo)

    def test_bundle_remains_draft_until_every_asset_upload_succeeds(self):
        files, notes = bundle.prepare_bundle(self.assets, self.output, self.repo)
        calls = []
        def fake(*args, check=True):
            calls.append(args)
            if args[0] == 'api': return types.SimpleNamespace(returncode=1, stdout='')
            if args[:2] == ('release', 'upload'):
                raise subprocess.CalledProcessError(1, ['gh'])
            return types.SimpleNamespace(returncode=0, stdout='')
        with patch.dict(os.environ, {'GH_REPO': self.repo, 'GITHUB_SHA': 'a'*40}), patch.object(publisher, 'gh', fake):
            with self.assertRaises(subprocess.CalledProcessError):
                publisher.publish_assets(files, 'pb-aaaaaaaaaaaa', 'All Plugins', notes, latest=True)
        self.assertIn('--draft', calls[1])
        self.assertFalse(any(c[:2] == ('release', 'edit') for c in calls))

    def test_stable_bundle_becomes_latest_and_prerelease_does_not(self):
        files, notes = bundle.prepare_bundle(self.assets, self.output, self.repo)
        for prerelease in (False, True):
            calls = []
            def fake(*args, check=True):
                calls.append(args)
                return types.SimpleNamespace(returncode=1 if args[0] == 'api' else 0, stdout='')
            with patch.dict(os.environ, {'GH_REPO': self.repo, 'GITHUB_SHA': 'a'*40}), patch.object(publisher, 'gh', fake):
                publisher.publish_assets(files, 'pb-aaaaaaaaaaaa', 'All Plugins', notes, prerelease=prerelease, latest=True)
            self.assertEqual('--latest' in calls[-1], not prerelease)
            self.assertTrue(all(str(file) in calls[-2] for file in files))

class SelectionTests(unittest.TestCase):
    sha = 'a'*40

    def test_main_push_uses_one_shared_commit_tag(self):
        result = selector.select('push', 'main', 'branch', self.sha)
        self.assertEqual(result['bundle'], 'true')
        self.assertEqual(result['tag'], 'pb-'+self.sha[:12])
        self.assertEqual(set(json.loads(result['plugins'])), set(bundle.PROJECTS))
        self.assertEqual(result, selector.select('push', 'main', 'branch', self.sha))

    def test_manual_all_and_single_component_choices(self):
        self.assertEqual(selector.select('workflow_dispatch', 'main', 'branch', self.sha)['bundle'], 'true')
        one = selector.select('workflow_dispatch', 'main', 'branch', self.sha, 'Items')
        self.assertEqual(json.loads(one['plugins']), ['Items'])
        self.assertEqual(one['bundle'], 'false')
        self.assertEqual(one['tag'], '')

    def test_component_tags_remain_supported(self):
        result = selector.select('push', 'items-v0.5.1', 'tag', self.sha)
        self.assertEqual(json.loads(result['plugins']), ['Items'])
        self.assertEqual(result['tag'], 'items-v0.5.1')

    def test_shared_published_tag_keeps_its_existing_release(self):
        result = selector.select('release', 'pb-snapshot', 'tag', self.sha)
        self.assertEqual(result['bundle'], 'true')
        self.assertEqual(result['existing_release'], 'true')
        self.assertEqual(result['tag'], 'pb-snapshot')

    def test_unknown_selections_bad_tags_and_noncommit_refs_fail(self):
        for args in [('workflow_dispatch', 'main', 'branch', self.sha, 'Unknown'),
                     ('push', 'pb-unsafe\nvalue', 'tag', self.sha),
                     ('push', 'main', 'branch', 'main')]:
            with self.assertRaises(ValueError): selector.select(*args)

if __name__ == '__main__': unittest.main()
