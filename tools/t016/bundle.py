#!/usr/bin/env python3
"""Reproducible Linux reference staging, copies pinned prepared inputs. No runtime install/download.
No freezer dependency. Host ABI glibc2.39/Linux x86_64 remains an explicit platform requirement.
"""
import argparse,difflib,hashlib,importlib.metadata,json,os,pathlib,re,shutil,subprocess,sys
MODEL_SHA='59929e1d1ee95287735ddd833b19cf4ac46d29bc7afddbbf6753c459690d574a'
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def verify(root):
    manifest=json.loads((root/'manifest.json').read_text())
    for entry in manifest['files']:
        p=root/entry['path']
        if not p.resolve().is_relative_to(root.resolve()) or not p.is_file() or sha(p)!=entry['sha256']:raise ValueError('VISION_MANIFEST_INVALID')
    return manifest

def build(venv,model,output):
    root=pathlib.Path(output);venv=pathlib.Path(venv);model=pathlib.Path(model)
    if root.exists():raise ValueError('Preserve existing staging; choose a new output')
    if sha(model)!=MODEL_SHA:raise ValueError('VISION_MODEL_HASH_INVALID')
    site=venv/'lib/python3.12/site-packages';repo=pathlib.Path(__file__).resolve().parents[2]
    pinned=dict(line.strip().split('==') for line in (repo/'tools/spikes/camera/requirements-linux-lab.lock').read_text().splitlines() if line.strip())
    packages=[]
    for dist in importlib.metadata.distributions(path=[str(site)]):
        name=dist.metadata['Name'].lower().replace('_','-')
        if name=='pip':continue
        if pinned.get(name)!=dist.version:raise ValueError('VISION_PIN_MISMATCH '+name)
        packages.append({'name':name,'version':dist.version,'license':dist.metadata.get('License-Expression') or dist.metadata.get('License'),'source':dist.metadata.get_all('Project-URL') or dist.metadata.get('Home-page')})
    if len(packages)!=len(pinned):raise ValueError('VISION_MISSING_DEPENDENCY')
    runtime=root/'runtime';(runtime/'bin').mkdir(parents=True);(runtime/'lib').mkdir()
    shutil.copy2('/usr/bin/python3.12',runtime/'bin/python3.12')
    ignore=shutil.ignore_patterns('__pycache__','*.pyc','test','tests','idlelib','ensurepip','tkinter')
    shutil.copytree('/usr/lib/python3.12',runtime/'lib/python3.12',ignore=ignore)
    # Do not copy activation, pyvenv.cfg, pip or paths back to developer caches.
    shutil.copytree(site,runtime/'lib/python3.12/dist-packages',ignore=shutil.ignore_patterns('__pycache__','*.pyc','tests','test','pip','pip-*.dist-info'))
    # Vision never uses Audio Tasks. Its eager import initializes host audio drivers at startup.
    # Keep original upstream notices and supply this explicit Apache-2.0 source modification.
    mp_init=runtime/'lib/python3.12/dist-packages/mediapipe/tasks/python/__init__.py'
    original_init=mp_init.read_text()
    if original_init.count('from . import audio\n')!=1:raise ValueError('VISION_MEDIAPIPE_PATCH_INPUT_CHANGED')
    patched_init=original_init.replace('from . import audio\n','# Modified by Gorilla Escape, 2026-10-09: Vision-only runtime omits eager Audio Tasks import.\n')
    mp_init.write_text(patched_init)
    source_info=root/'SOURCES_OR_SOURCE_INFO';source_info.mkdir()
    patch_path=source_info/'mediapipe-0.10.33-vision-only.diff'
    patch_path.write_text(''.join(difflib.unified_diff(original_init.splitlines(True),patched_init.splitlines(True),fromfile='original/mediapipe/tasks/python/__init__.py',tofile='modified/mediapipe/tasks/python/__init__.py')))
    (source_info/'mediapipe-modification.json').write_text(json.dumps(dict(component='mediapipe',version='0.10.33',path=str(mp_init.relative_to(root)),originalSha256=hashlib.sha256(original_init.encode()).hexdigest(),modifiedSha256=sha(mp_init),patch=str(patch_path.relative_to(root)),license='Apache-2.0',scope='No Audio Tasks eager initialization/API export in this Vision-only runtime; PoseLandmarker API unchanged'),indent=2)+'\n')
    (root/'licenses').mkdir();shutil.copy2('/usr/share/doc/python3.12/copyright',root/'licenses/Python-copyright.txt')
    shutil.copy2(model,root/'pose_landmarker_lite-v1.task')
    for name in ['worker.py','pipeline.py','camera_controls.py']:shutil.copy2(repo/'tools/spikes/camera'/name,root/name)
    shutil.copy2(repo/'Shared/Protocol/camera/corpus-v1.json',root/'corpus-v1.json')
    shutil.copy2(__file__,root/'bundle.py')
    native=[];external=set()
    search_dirs=sorted({str(p.parent) for p in runtime.rglob('*.so*')})
    audit_env={**os.environ,'LD_LIBRARY_PATH':':'.join(search_dirs)}
    # Native dependency inventory for every copied ELF. Leave system ABI explicit, never hide ldd missing libs.
    for p in sorted(runtime.rglob('*')):
        if not p.is_file() or p.open('rb').read(4)!=b'\x7fELF':continue
        result=subprocess.run(['ldd',str(p)],capture_output=True,text=True,env=audit_env)
        if 'not found' in result.stdout:raise ValueError('VISION_NATIVE_MISSING '+p.name)
        links=[]
        for line in result.stdout.splitlines():
            match=re.search(r'=> (/[^ ]+)',line)
            if match:
                dependency=pathlib.Path(match[1]);links.append(dependency.name)
                if not dependency.resolve().is_relative_to(runtime.resolve()):external.add(str(dependency))
        native.append({'path':str(p.relative_to(root)),'dependencies':links})
    # Reference distribution includes non-glibc dynamic dependencies; dynamic loader/glibc remain OS ABI.
    system=[]
    abi={'libc.so.6','libm.so.6','libpthread.so.0','libdl.so.2','librt.so.1','ld-linux-x86-64.so.2'}
    for name in sorted(external):
        source=pathlib.Path(name)
        if source.name in abi:system.append(name);continue
        dest=runtime/'native'/source.name;dest.parent.mkdir(exist_ok=True);shutil.copy2(source,dest)
    launcher='''#!/bin/sh
set -eu
vision_root=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
export PYTHONHOME="$vision_root/runtime"
unset PYTHONPATH PYTHONUSERBASE VIRTUAL_ENV
export PYTHONNOUSERSITE=1 PYTHONDONTWRITEBYTECODE=1
export LD_LIBRARY_PATH="$vision_root/runtime/native"
"$vision_root/runtime/bin/python3.12" -I "$vision_root/bundle.py" --verify "$vision_root"
exec "$vision_root/runtime/bin/python3.12" -s -B "$@"
'''
    (root/'python').write_text(launcher);(root/'python').chmod(0o755)
    manifest={'version':1,'platform':'Linux x86_64 / Pop!_OS24.04 reference glibc2.39','python':'3.12.3','model':{'file':'pose_landmarker_lite-v1.task','sha256':MODEL_SHA,'bytes':model.stat().st_size,'source':'https://storage.googleapis.com/mediapipe-models/pose_landmarker/pose_landmarker_lite/float16/1/pose_landmarker_lite.task','familyModelCard':'https://storage.googleapis.com/mediapipe-assets/Model%20Card%20BlazePose%20GHUM%203D.pdf','familyLicense':'Apache-2.0'},'packages':sorted(packages,key=lambda x:x['name']),'native':native,'systemAbi':system,'redistributionReview':'LOCAL STAGING ONLY. Full corresponding-source fulfillment for Qt/FFmpeg/system libraries remains required before external binary distribution. Notices retained from pinned packages.','files':[]}
    for p in sorted(root.rglob('*')):
        if p.is_file():manifest['files'].append({'path':str(p.relative_to(root)),'bytes':p.stat().st_size,'sha256':sha(p)})
    (root/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n');verify(root)
    return {'bytes':sum(e['bytes'] for e in manifest['files']),'files':len(manifest['files']),'nativeFiles':len(native),'manifestSha256':sha(root/'manifest.json'),'redistributionReview':manifest['redistributionReview']}
if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--venv');p.add_argument('--model');p.add_argument('--output');p.add_argument('--verify');a=p.parse_args()
    try:
        if a.verify:verify(pathlib.Path(a.verify))
        else:print(json.dumps(build(a.venv,a.model,a.output)))
    except Exception as error:print(str(error),file=sys.stderr);sys.exit(2)
