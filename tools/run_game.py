#!/usr/bin/env python3
"""Prepare a development package or launch the current seven-game Player. Unity owns all services."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import secrets
import shutil
import sys

REPO=Path(__file__).resolve().parents[1]

def sha(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream,'sha256').hexdigest()

def source_records(repo):
    files=[]
    for folder in ['Unity/Assets','Shared/CSharp/Runtime']:
        files.extend(p for p in (repo/folder).rglob('*') if p.is_file() and p.suffix in ('.cs','.unity','.asmdef'))
    files.extend(repo/p for p in ['Unity/Packages/manifest.json','Unity/Packages/packages-lock.json','Unity/ProjectSettings/EditorBuildSettings.asset','Unity/ProjectSettings/ProjectVersion.txt'])
    return {p.relative_to(repo).as_posix():sha(p) for p in sorted(files)}

def validate_build(repo,package):
    marker=package/'game-build.json'
    if not marker.is_file():raise ValueError('Falta el build actual de los siete juegos. Usa Gorilla Escape → Build Seven Games Linux en Unity.')
    data=json.loads(marker.read_text())
    records={item['path']:item['sha256'] for item in data['sources']}
    required={'Assets/Scenes/'+name+'.unity' for name in ['Connection','MainMenu','BananaBowling','GorillaBoxing','JungleBaseball','JungleDrums','CoconutSlice','WhackAMole','LogBalance']}
    if not required.issubset(set(data.get('scenes',[]))) or len(set(data.get('scenes',[])))!=10 or data.get('schema')!=1 or not data.get('scenes') or data['scenes'][0]!='Assets/Scenes/Connection.unity' or 'Assets/Scenes/LogBalance.unity' not in data['scenes'] or records!=source_records(repo):
        raise ValueError('El ejecutable no corresponde a las fuentes actuales. Vuelve a compilar los siete juegos en Unity.')

def copy_file(source,target):
    if source.resolve()==target.resolve():return
    target.parent.mkdir(parents=True,exist_ok=True)
    staging=target.with_name(target.name+'.pending');shutil.copyfile(source,staging);staging.replace(target)

def link_folder(source,target):
    if target.exists():return
    if target.is_symlink():raise ValueError('El paquete tiene un enlace de runtime roto: '+target.name)
    target.symlink_to(source.resolve(),target_is_directory=True)

def main(argv=None):
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--prepare-only',action='store_true')
    p.add_argument('--package',type=Path,default=REPO/'Unity/Builds/JungleLinux')
    p.add_argument('--runtime',type=Path,default=Path(os.environ.get('GORILLA_RUNTIME_ROOT',str(REPO/'Unity/Builds/FoundationLinux'))))
    p.add_argument('--backend-jar',type=Path,default=None)
    args,player_args=p.parse_known_args(argv)
    package=args.package.resolve();runtime=args.runtime.resolve();player=package/'GorillaEscape.x86_64'
    jar=REPO/'Server/target/local-server-0.1.0-SNAPSHOT.jar'
    backend=(args.backend_jar or Path(os.environ.get('GORILLA_BACKEND_JAR',str(runtime/'Java/backend.jar')))).resolve()
    for item in [jar,backend,runtime/'Java/runtime/bin/java',runtime/'Vision/python']:
        if not item.is_file():raise ValueError('Falta un artefacto de desarrollo: '+str(item))
    mode=os.environ.get('GORILLA_CONNECTION_MODE','AUTO')
    if mode not in ('AUTO','CABIN','CLOUDFLARED'):raise ValueError('Modo de conexión inválido')
    binary=runtime/'Tools/cloudflared'
    # AUTO keeps its LAN fallback when the optional executable is absent/invalid; Java verifies the pin.
    if mode=='CLOUDFLARED' and not binary.is_file():raise ValueError('Falta Cloudflared para el modo forzado')
    if not args.prepare_only:
        if not player.is_file() or not os.access(player,os.X_OK):raise ValueError('Falta compilar el ejecutable de los siete juegos en Unity.')
        validate_build(REPO,package)
    package.mkdir(parents=True,exist_ok=True);(package/'Java').mkdir(exist_ok=True)
    link_folder(runtime/'Vision',package/'Vision');link_folder(runtime/'Java/runtime',package/'Java/runtime')
    if (runtime/'Tools').is_dir():link_folder(runtime/'Tools',package/'Tools')
    copy_file(jar,package/'Java/server.jar');copy_file(backend,package/'Java/backend.jar')
    copy_file(REPO/'tools/setup-firewall.sh',package/'tools/setup-firewall.sh');(package/'tools/setup-firewall.sh').chmod(0o755)
    shutil.copytree(REPO/'tools/connectivity',package/'tools/connectivity',dirs_exist_ok=True,ignore=shutil.ignore_patterns('__pycache__','*.pyc'))
    manifest={'schema':1,'scope':'developer package; not installer acceptance','serverSha256':sha(package/'Java/server.jar'),'backendSha256':sha(package/'Java/backend.jar'),'runtime':str(runtime)}
    (package/'development-package.json').write_text(json.dumps(manifest,indent=2)+'\n')
    if args.prepare_only:print('Paquete preparado. La ejecución requiere el build actual de los siete juegos.');return
    env=os.environ.copy();env.update(GORILLA_PACKAGE_ROOT=str(package),GORILLA_CONNECTION_MODE=mode,
        GORILLA_IPC_JAVA=str(package/'Java/runtime/bin/java'),GORILLA_IPC_JAR=str(package/'Java/server.jar'),
        GORILLA_PHONE_INPUT='1',GORILLA_QA_OVERLAY='1',GORILLA_MOBILE_OPERATOR=secrets.token_urlsafe(32))
    env.pop('GORILLA_MOBILE_LAB',None)
    os.execve(str(player),[str(player),*player_args],env)

if __name__=='__main__':
    try:main()
    except (ValueError,OSError,KeyError,json.JSONDecodeError) as error:sys.exit(str(error))
