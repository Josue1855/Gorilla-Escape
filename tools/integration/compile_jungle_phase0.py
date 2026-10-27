"""Optional C# type check against installed Unity references; never starts the Editor.

This does not import assets, build a Player, execute engine tests or validate glTF/game scenes.
"""
import argparse
from pathlib import Path
import shutil
import subprocess
import tempfile


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--editor-data', type=Path, required=True)
    parser.add_argument('--output', type=Path)
    parser.add_argument('--player', action='store_true', help='Check player preprocessor paths instead of Editor paths')
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[2]
    output = args.output or Path(tempfile.mkdtemp(prefix='gorilla-jungle-compile-'))
    output.mkdir(parents=True, exist_ok=True)
    data = args.editor_data.resolve()
    dotnet = shutil.which('dotnet')
    if not dotnet:
        parser.error('.NET SDK is required for this developer-only check')
    sdk = subprocess.check_output([dotnet, '--list-sdks'], text=True).splitlines()[-1]
    version, folder = sdk.split(' ', 1)
    compiler = Path(folder.strip('[]')) / version / 'Roslyn/bincore/csc.dll'
    refs = list((data / 'NetStandard/ref/2.1.0').glob('*.dll'))
    refs += list((data / 'Managed/UnityEngine').glob('*.dll'))
    if not refs or not compiler.exists():
        parser.error('Missing installed Unity reference assemblies or C# compiler')

    def compile_assembly(name, files, extra=()):
        target = output / (name + '.dll')
        options = ['-nologo', '-target:library', '-langversion:9', '-nostdlib+',
                   ('-define:UNITY_STANDALONE_LINUX' if args.player else '-define:UNITY_EDITOR,UNITY_EDITOR_LINUX'), '-out:' + str(target)]
        options += ['-r:' + str(p) for p in refs + list(extra)]
        options += [str(p) for p in files]
        response = output / (name + '.rsp')
        response.write_text('\n'.join('"' + option + '"' for option in options))
        result = subprocess.run([dotnet, str(compiler), '@' + str(response)],
                                text=True, capture_output=True)
        (output / (name + '.log')).write_text(result.stdout + result.stderr)
        if result.returncode:
            print(result.stdout + result.stderr)
            raise SystemExit(result.returncode)
        print(f'PASS {name}: {len(files)} source files (warnings retained in log)')
        return target

    contracts = compile_assembly('GorillaEscape.Contracts', list((root / 'Shared/CSharp/Runtime').glob('*.cs')))
    runtime = compile_assembly('GorillaEscape.Runtime', list((root / 'Unity/Assets/_Project/Scripts').rglob('*.cs')), [contracts])
    compile_assembly('JungleParty.Phase0', [root / 'Unity/Assets' / path for path in (
        'PortableModule/PortableModuleContracts.cs', 'PortableModule/PortableServiceHost.cs',
        'Scripts/MotionSession.cs', 'Scripts/SceneNavigator.cs', 'Scripts/ConnectionScreen.cs')], [contracts, runtime])
    print('Source type check only; full Unity compilation and physical acceptance remain separate.')
    print('Logs:', output)


if __name__ == '__main__':
    main()
