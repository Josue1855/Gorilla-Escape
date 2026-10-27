"""Package/launch gates without starting Unity or auxiliary processes."""
import importlib.util
import json
import os
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

SPEC=importlib.util.spec_from_file_location('game_launcher',Path(__file__).resolve().parents[1]/'run_game.py')
launcher=importlib.util.module_from_spec(SPEC);SPEC.loader.exec_module(launcher)

class GameLauncherTest(unittest.TestCase):
    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory(prefix='game package ');self.addCleanup(self.tmp.cleanup)
        self.root=Path(self.tmp.name);self.runtime=self.root/'runtime';self.package=self.root/'package'
        for relative in ['Server/target/local-server-0.1.0-SNAPSHOT.jar','tools/setup-firewall.sh','tools/connectivity/firewall.py','Unity/Packages/manifest.json','Unity/Packages/packages-lock.json','Unity/ProjectSettings/EditorBuildSettings.asset','Unity/ProjectSettings/ProjectVersion.txt','Unity/Assets/test.cs','Shared/CSharp/Runtime/test.cs']:
            p=self.root/relative;p.parent.mkdir(parents=True,exist_ok=True);p.write_text('fixture')
        for relative in ['Java/runtime/bin/java','Java/backend.jar','Vision/python']:
            p=self.runtime/relative;p.parent.mkdir(parents=True,exist_ok=True);p.write_text('fixture')
        self.repo=patch.object(launcher,'REPO',self.root);self.repo.start();self.addCleanup(self.repo.stop)
        self.env=patch.dict(os.environ,{},clear=True);self.env.start();self.addCleanup(self.env.stop)
    def args(self):return ['--runtime',str(self.runtime),'--package',str(self.package)]
    def marker(self):
        self.package.mkdir(exist_ok=True);p=self.package/'GorillaEscape.x86_64';p.write_text('fixture');p.chmod(0o755)
        data={'schema':1,'scenes':['Assets/Scenes/'+name+'.unity' for name in ['Connection','MainMenu','BananaBowling','GorillaBoxing','JungleBaseball','JungleDrums','CoconutSlice','WhackAMole','LogBalance','Bootstrap']],'sources':[{'path':k,'sha256':v} for k,v in launcher.source_records(self.root).items()]}
        (self.package/'game-build.json').write_text(json.dumps(data))
    def test_auto_prepares_complete_services_without_optional_tunnel(self):
        launcher.main([*self.args(),'--prepare-only'])
        self.assertTrue((self.package/'Java/backend.jar').is_file());self.assertTrue((self.package/'Vision/python').is_file())
        self.assertTrue((self.package/'development-package.json').is_file())
    def test_missing_backend_is_reported_before_preparing_package(self):
        (self.runtime/'Java/backend.jar').unlink()
        with self.assertRaises(ValueError):launcher.main([*self.args(),'--prepare-only'])
        self.assertFalse(self.package.exists())
    def test_stale_or_unmarked_player_cannot_launch_as_seven_game_build(self):
        self.marker();(self.root/'Unity/Assets/test.cs').write_text('changed')
        with patch.object(os,'execve') as execute:
            with self.assertRaises(ValueError):launcher.main(self.args())
            execute.assert_not_called()
    def test_current_build_executes_only_the_player_with_packaged_paths(self):
        self.marker()
        with patch.object(os,'execve') as execute:
            launcher.main(self.args());execute.assert_called_once()
            binary,args,env=execute.call_args.args
            self.assertEqual(binary,str(self.package/'GorillaEscape.x86_64'));self.assertEqual(len(args),1)
            self.assertEqual(env['GORILLA_PACKAGE_ROOT'],str(self.package));self.assertEqual(len(env['GORILLA_MOBILE_OPERATOR']),43)
    def test_forced_tunnel_requires_the_optional_binary(self):
        with patch.dict(os.environ,{'GORILLA_CONNECTION_MODE':'CLOUDFLARED'}):
            with self.assertRaises(ValueError):launcher.main([*self.args(),'--prepare-only'])

if __name__=='__main__':unittest.main()
