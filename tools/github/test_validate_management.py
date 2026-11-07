"""Regression tests for tools/github/validate_management.py.

Guards the fix that prunes generated/build directories (Flutter `build/`,
`node_modules`, etc.) from Markdown traversal: before the fix, `rglob` crashed
with WinError 3 on deep Windows paths under `Companion/build/.../.transforms/`.
"""
import json, tempfile, unittest, importlib.util
from pathlib import Path

def _load_module():
    path = Path(__file__).resolve().parent / 'validate_management.py'
    spec = importlib.util.spec_from_file_location('validate_management', path)
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod

class ValidateManagementRegressionTest(unittest.TestCase):
    def setUp(self):
        self.vm = _load_module()
        self.tmp = tempfile.TemporaryDirectory()
        self.root = Path(self.tmp.name)
        (self.root / 'docs' / 'github').mkdir(parents=True)
        self.root.joinpath('docs', 'github', 'backlog.json').write_text(
            json.dumps({'items': []}), encoding='utf-8')
        self.old_root = self.vm.ROOT
        self.vm.ROOT = self.root

    def tearDown(self):
        self.vm.ROOT = self.old_root
        self.tmp.cleanup()

    def test_generated_build_dirs_are_pruned(self):
        # A broken link inside a generated build directory must be ignored.
        build = self.root / 'Companion' / 'build' / 'native_device_orientation' / '.transforms'
        build.mkdir(parents=True)
        (build / 'IGNORED.md').write_text('[broken](not-found-target.md)', encoding='utf-8')
        (self.root / 'README.md').write_text('[ok](docs/github/backlog.json)', encoding='utf-8')
        self.vm.validate()  # must not raise

    def test_broken_link_in_tracked_docs_still_raises(self):
        (self.root / 'README.md').write_text('[broken](not-found-target.md)', encoding='utf-8')
        with self.assertRaises(AssertionError):
            self.vm.validate()

    def test_empty_markdown_raises(self):
        (self.root / 'README.md').write_text('', encoding='utf-8')
        with self.assertRaises(AssertionError):
            self.vm.validate()

if __name__ == '__main__':
    unittest.main()
