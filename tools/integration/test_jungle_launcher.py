"""Launcher configuration regressions; never starts Unity, Java or a tunnel."""
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest

SOURCE = Path(__file__).with_name('run-jungle-editor.sh')


class LauncherTest(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix='jungle launcher ')
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.launcher = self.root / 'tools/integration/run-jungle-editor.sh'
        self.launcher.parent.mkdir(parents=True)
        shutil.copy2(SOURCE, self.launcher)
        jar = self.root / 'Server/target/local-server-0.1.0-SNAPSHOT.jar'
        jar.parent.mkdir(parents=True)
        jar.touch()
        self.prepared = self.root / 'Unity/Builds/FoundationLinux'
        self.editor = self.root / 'fake editor'
        self.editor.write_text('#!/usr/bin/env python3\nimport os,json\nprint(json.dumps({k:os.environ.get(k) for k in ["GORILLA_PACKAGE_ROOT","GORILLA_CLOUDFLARED_PATH","GORILLA_CONNECTION_MODE"]}))\n')
        self.editor.chmod(0o755)

    def package(self):
        for relative in ('Vision/manifest.json', 'tools/setup-firewall.sh', 'Tools/cloudflared'):
            path = self.prepared / relative
            path.parent.mkdir(parents=True, exist_ok=True)
            path.touch()
            path.chmod(0o755)

    def run_launcher(self, **overrides):
        env = {k:v for k,v in os.environ.items() if not k.startswith('GORILLA_')}
        env.update(overrides)
        return json.loads(subprocess.check_output(['sh', str(self.launcher), str(self.editor)], env=env, text=True))

    def test_prepared_package_and_tunnel_selected_without_execution(self):
        self.package()
        result = self.run_launcher()
        self.assertEqual(result['GORILLA_PACKAGE_ROOT'], str(self.prepared))
        self.assertEqual(result['GORILLA_CLOUDFLARED_PATH'], str(self.prepared / 'Tools/cloudflared'))
        self.assertEqual(result['GORILLA_CONNECTION_MODE'], 'AUTO')

    def test_unprepared_checkout_keeps_optional_tunnel_absent(self):
        result = self.run_launcher()
        self.assertEqual(result['GORILLA_PACKAGE_ROOT'], str(self.root))
        self.assertIsNone(result['GORILLA_CLOUDFLARED_PATH'])

    def test_explicit_package_can_reuse_prepared_tunnel(self):
        self.package()
        result = self.run_launcher(GORILLA_PACKAGE_ROOT=str(self.root))
        self.assertEqual(result['GORILLA_PACKAGE_ROOT'], str(self.root))
        self.assertEqual(result['GORILLA_CLOUDFLARED_PATH'], str(self.prepared / 'Tools/cloudflared'))

    def test_explicit_configuration_is_preserved(self):
        self.package()
        result = self.run_launcher(GORILLA_CLOUDFLARED_PATH='/operator/reviewed-binary', GORILLA_CONNECTION_MODE='CABIN')
        self.assertEqual(result['GORILLA_CLOUDFLARED_PATH'], '/operator/reviewed-binary')
        self.assertEqual(result['GORILLA_CONNECTION_MODE'], 'CABIN')

    def test_selected_package_binary_precedes_other_packages(self):
        self.package()
        path = self.root / 'Tools/cloudflared'
        path.parent.mkdir();path.touch();path.chmod(0o755)
        result = self.run_launcher(GORILLA_PACKAGE_ROOT=str(self.root))
        self.assertEqual(result['GORILLA_CLOUDFLARED_PATH'], str(path))


if __name__ == '__main__':
    unittest.main()
