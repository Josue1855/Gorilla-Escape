#!/usr/bin/env python3
"""Hash actual Unity player files outside Vision; mark unverified redistribution provenance."""
import argparse,hashlib,json,pathlib,re,subprocess,sys
p=argparse.ArgumentParser();p.add_argument('--player',type=pathlib.Path,required=True);p.add_argument('--output',type=pathlib.Path,required=True);a=p.parse_args()
if a.output.exists():raise ValueError('Preserve previous inventory')
files=[];native=[]
for f in sorted(a.player.rglob('*')):
    path=f.relative_to(a.player).as_posix()
    if not f.is_file() or path.startswith('Vision/') or path.startswith('physical-'):continue
    files.append(dict(path=path,bytes=f.stat().st_size,sha256=hashlib.sha256(f.read_bytes()).hexdigest()))
    with f.open('rb') as stream:elf=stream.read(4)==b'\x7fELF'
    if elf:
        text=subprocess.run(['readelf','-d',str(f)],capture_output=True,text=True).stdout
        native.append(dict(path=path,needed=re.findall(r'\(NEEDED\).*?\[([^]]+)\]',text)))
mono=a.player/'GorillaEscape_Data/MonoBleedingEdge/x86_64/libmonobdwgc-2.0.so'
version=subprocess.run([sys.executable,'-c','import ctypes,sys; lib=ctypes.CDLL(sys.argv[1]); lib.mono_get_runtime_build_info.restype=ctypes.c_char_p; print(lib.mono_get_runtime_build_info().decode())',str(mono)],capture_output=True,text=True)
mono_version=version.stdout.strip() if version.returncode==0 else 'UNKNOWN (runtime version query failed)'
report=dict(origin='Actual freshly staged Unity6000.3.23f1 Linux Mono development build',monoVersion=mono_version,
    files=files,native=native,excluded='Vision separately inventoried; physical benchmark aggregate files are evidence, not product',
    components=[dict(name='Unity Player / Mono / managed Unity assemblies',version='6000.3.23f1; Mono '+mono_version+'; corresponding build source/notices unverified',
        files=[f['path'] for f in files if not pathlib.Path(f['path']).name.startswith('libdecor')],
        license='Unity proprietary runtime terms plus embedded component licenses; applicable notices/source inventory NOT VERIFIED',
        copyright='Not asserted from engine version; matching player/Mono notices still need exact provenance',
        redistributionRequirement='Resolve player-specific applicable terms/notices before external distribution; installed editor tool notices do not establish player notices',
        sourceRequirement='Identify embedded copyleft components and applicable source obligations; no source offer assembled',noticeRequirement='Player-specific matching notices remain OPEN'),
    dict(name='libdecor and Cairo plugin',version='Exact upstream version UNKNOWN; SONAME libdecor-0.so.0',
        files=[f['path'] for f in files if pathlib.Path(f['path']).name.startswith('libdecor')],
        origin='Copied by Unity build from LinuxStandaloneSupport; not attributed to host distro package without hash match',
        license='Exact build license/notice provenance unverified; do not infer from similarly named host library',
        copyright='UNKNOWN until matching source/notice retrieved',redistributionRequirement='Identify matching upstream revision and license requirements',
        sourceRequirement='Pending exact-license classification',noticeRequirement='Matching authentic notice/source provenance OPEN')],
    acceptance='Supplemental whole-player inventory; unresolved provenance reinforces AC2 PARTIAL')
a.output.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({'playerFiles':len(files),'nativeFiles':len(native)}))
