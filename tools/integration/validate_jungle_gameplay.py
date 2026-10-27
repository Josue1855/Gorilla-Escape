"""Architectural wiring checks for the saved Jungle scenes; not a Unity or physical test."""
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[2]
CONTROLLERS = {
    'BananaBowling': 'PhoneMotionController',
    'GorillaBoxing': 'PhoneMotionController',
    'JungleBaseball': 'BaseballController',
    'JungleDrums': 'RhythmDrumsController',
    'CoconutSlice': 'CoconutSliceController',
    'WhackAMole': 'WhackAMoleController',
    'LogBalance': 'LogBalanceGameController',
}


def validate():
    for scene, controller in CONTROLLERS.items():
        path = ROOT / 'Unity/Assets/Scripts' / (controller + '.cs')
        guid = re.search(r'^guid: ([a-f0-9]{32})$', Path(str(path) + '.meta').read_text(), re.M)[1]
        assert guid in (ROOT / 'Unity/Assets/Scenes' / (scene + '.unity')).read_text(), scene
        text = path.read_text()
        for required in ('TryConsumeFrame', 'TryAuthorize', 'IsInputReady'):
            assert required in text, f'{controller}: missing {required}'
        for forbidden in ('ConsumeMotion', 'ConsumePose', 'JsonUtility.FromJson', 'GetMouseButton', 'HttpClient', 'UnityWebRequest', 'Time.timeScale ='):
            assert forbidden not in text, f'{controller}: unexpected input/authority path {forbidden}'
    gameplay = ROOT / 'Unity/Assets/_Project/Scripts/Gameplay/Input'
    for path in gameplay.glob('*.cs'):
        assert 'using UnityEngine' not in path.read_text(), f'{path}: engine-independent gameplay rules required'
    host = (ROOT / 'Unity/Assets/PortableModule/PortableServiceHost.cs').read_text()
    assert 'fusion.EvaluateLatest(' in host
    assert 'gameplay.Authorize(' in host
    # Resolve Resources paths and imported prefab/material GUIDs without invoking Unity.
    catalog = (ROOT / 'Unity/Assets/_Project/Scripts/Minigames/LogBalance/LogBalanceArtCatalog.cs').read_text()
    resources = [p for p in (ROOT / 'Unity/Assets').rglob('Resources') if p.is_dir()]
    for name, asset in re.findall(r'public const string (\w+) = "([^"]+)";', catalog):
        suffix = '.prefab' if name in ('Human', 'Trunk') else '.png' if name.endswith('Texture') or name in ('Background', 'Ground') else '.fbx'
        matches = [base / (asset + suffix) for base in resources if (base / (asset + suffix)).is_file()]
        assert len(matches) == 1, f'LogBalance resource missing or ambiguous: {asset}'
    known = set()
    for meta in (ROOT / 'Unity/Assets').rglob('*.meta'):
        match = re.search(r'^guid: ([a-f0-9]{32})$', meta.read_text(), re.M)
        if match:
            known.add(match[1])
    imported = ROOT / 'Unity/Assets/_Project/Resources/LogBalance'
    for path in imported.rglob('*'):
        if path.suffix in ('.prefab', '.mat'):
            for guid in re.findall(r'guid: ([a-f0-9]{32})', path.read_text()):
                assert guid in known or guid.startswith('0000000000000000'), f'{path}: unresolved {guid}'
    assert 'isReadable: 1' in (imported / 'Models/trunk.fbx.meta').read_text(), 'trunk surface sampling requires readable mesh'
    assert (imported / 'CREDITS.txt').is_file(), 'runtime attribution missing'
    print('PASS: 7 saved scenes reference fused controllers; typed frame/action gates; no raw or mouse scoring route; pure gameplay rules; LogBalance resources and dependencies resolve.')


if __name__ == '__main__':
    validate()
