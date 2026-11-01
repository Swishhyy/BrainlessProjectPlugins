import pathlib
import unittest
import xml.etree.ElementTree as ET

ROOT = pathlib.Path(__file__).resolve().parents[1]


class RuntimeReferenceTests(unittest.TestCase):
    def test_server_projects_compile_against_original_member_visibility(self):
        props = ET.parse(ROOT / 'Directory.Build.props')
        reference, = [r for r in props.iter('Reference') if r.get('Include') == 'Assembly-CSharp']
        self.assertEqual(reference.get('HintPath'), '$(ReferencesPath)/Assembly-CSharp.dll')
        self.assertEqual(reference.get('Private'), 'false')

    def test_native_config_tests_do_not_hide_private_access(self):
        project = ET.parse(ROOT / 'tests/Brainless.Tests/Brainless.Tests.csproj')
        copy, = [c for c in project.iter('Copy') if c.get('DestinationFiles') == '$(NativeConfigReferencesPath)/Assembly-CSharp.dll']
        self.assertEqual(copy.get('SourceFiles'), '$(ReferencesPath)/Assembly-CSharp.dll')
