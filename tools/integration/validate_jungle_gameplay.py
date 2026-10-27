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
    print('PASS: 6 saved scenes reference fused controllers; typed frame/action gates; no raw or mouse scoring route; pure gameplay rules.')


if __name__ == '__main__':
    validate()
