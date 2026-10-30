import hashlib
import importlib.util
import json
import os
import pathlib
import tempfile
import types
import unittest
from unittest.mock import patch

ROOT = pathlib.Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('publish_release', ROOT/'scripts/publish_release.py')
publisher = importlib.util.module_from_spec(spec)
spec.loader.exec_module(publisher)

class ReleasePublishTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.dll = pathlib.Path(self.temp.name)/'Brainless.Items.dll'
        self.manifest = pathlib.Path(self.temp.name)/'Brainless.Items.update.json'
        self.dll.write_bytes(b'fixture-dll')
        self.manifest.write_text('{}')
        self.calls = []
        self.env = patch.dict(os.environ, {'GH_REPO': 'Swishhyy/BrainlessProjectPlugins', 'GITHUB_SHA': 'abc123'})
        self.env.start()
        self.addCleanup(self.env.stop)

    def run_publish(self, state=None, existing=False):
        def fake(*args, check=True):
            self.calls.append(args)
            if args[0] == 'api':
                return types.SimpleNamespace(returncode=0 if state else 1, stdout=json.dumps(state) if state else '')
            return types.SimpleNamespace(returncode=0, stdout='')
        with patch.object(publisher, 'gh', fake):
            publisher.publish('Items', '0.1.2', self.dll, self.manifest, 'items-v0.1.2', existing)

    def assets(self):
        return [{'name': p.name, 'digest': 'sha256:'+hashlib.sha256(p.read_bytes()).hexdigest()} for p in (self.dll, self.manifest)]

    def test_new_release_stays_draft_until_upload_finishes(self):
        self.run_publish()
        create, upload, publish = self.calls[1:]
        self.assertIn('--draft', create)
        self.assertEqual(upload[:2], ('release', 'upload'))
        self.assertIn('--draft=false', publish)

    def test_published_assets_are_uploaded_manifest_last(self):
        self.run_publish({'draft': False, 'assets': []}, existing=True)
        self.assertEqual(self.calls[1][-1], str(self.dll))
        self.assertEqual(self.calls[2][-1], str(self.manifest))
        self.assertTrue(all('--clobber' not in call for call in self.calls))

    def test_retry_can_finish_manifest_after_dll_upload(self):
        self.run_publish({'draft': False, 'assets': self.assets()[:1]}, existing=True)
        self.assertEqual(self.calls[1][-1], str(self.manifest))
        self.assertEqual(len(self.calls), 2)

    def test_same_published_version_is_idempotent(self):
        self.run_publish({'draft': False, 'assets': self.assets()})
        self.assertEqual(len(self.calls), 1)

    def test_published_version_cannot_be_replaced_with_different_bytes(self):
        state = {'draft': False, 'assets': self.assets()}
        self.dll.write_bytes(b'different')
        self.run_publish(state)
        self.assertEqual(len(self.calls), 1)

    def test_published_upload_refuses_a_conflicting_asset(self):
        state = {'draft': False, 'assets': self.assets()}
        self.dll.write_bytes(b'different')
        with self.assertRaises(RuntimeError): self.run_publish(state, existing=True)
        self.assertEqual(len(self.calls), 1)

    def test_retry_draft_must_target_same_commit(self):
        with self.assertRaises(RuntimeError): self.run_publish({'draft': True, 'target_commitish': 'other'})
        self.assertEqual(len(self.calls), 1)

    def test_same_draft_can_resume(self):
        self.run_publish({'draft': True, 'target_commitish': 'abc123'})
        self.assertEqual(self.calls[1][:2], ('release', 'upload'))
        self.assertIn('--draft=false', self.calls[2])

if __name__ == '__main__': unittest.main()
