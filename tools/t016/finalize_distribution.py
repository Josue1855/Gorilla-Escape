#!/usr/bin/env python3
"""Assemble actual local T016 materials and manifests; open provenance is never a PASS.

Prepared inputs are immutable. Source archives remain local, not Git attachments.
"""
import argparse,hashlib,json,pathlib,re,shutil,subprocess,sys

def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def write(path,value):path.parent.mkdir(parents=True,exist_ok=True);path.write_text(json.dumps(value,indent=2)+'\n')
def copy(source,target):target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(source,target)

def assemble(vision,player,materials,output):
    if output.exists():raise ValueError('Preserve existing distribution; choose a fresh output')
    shutil.copytree(vision,output/'Vision')
    for path in player.iterdir():
        if path.name=='Vision':continue
        if path.is_dir():shutil.copytree(path,output/path.name)
        else:copy(path,output/path.name)
    v=output/'Vision';info=v/'SOURCES_OR_SOURCE_INFO'
    # Regenerate only this fresh copy's generated inventory, never previous staging.
    for name in ['redistribution.json','runtime-manifest.json','source-obligations.json']:(v/name).unlink(missing_ok=True)
    for name in ['LICENSES','THIRD_PARTY_NOTICES']:
        if (v/name).exists():shutil.rmtree(v/name)
    from redistribution import inventory
    loaded=materials/'runtime-loaded-vision-only.json'
    counts=inventory(v,loaded)
    report=json.loads((v/'redistribution.json').read_text());sources=json.loads((info/'distro/source-index.json').read_text())
    indexed={s['sourcePackage']:s for s in sources if 'sourcePackage' in s};source_map={};gaps=[]
    compiler_info=info/'wheel-compiler-runtimes';compiler=json.loads((compiler_info/'wheel-correspondence.json').read_text());compiler_sources=json.loads((compiler_info/'source-rpm-index.json').read_text())
    for source in compiler_sources:
        if 'error' in source or sha(compiler_info/source['file'])!=source['sha256']:raise ValueError('COMPILER_SOURCE_MISSING_OR_CORRUPT')
    for c in report['components']:
        origin=c['origin'];src=origin.get('sourcePackage',[]);src_name=src[0] if src else None
        entry=dict(version=c['version'],origin=origin,gorillaPatches='NONE',sourceRequired=c['sourceRequirement'],provided=[],verification='Not inferred from SONAME')
        if src_name in indexed:
            source=indexed[src_name];matched=len(src)>1 and src[1]==source['sourceVersion'] and origin.get('hostHashMatches')
            for f in source['files']:
                path=info/'distro'/f['file']
                if not path.is_file() or sha(path)!=f['sha256']:raise ValueError('SOURCE_MATERIAL_HASH_INVALID '+f['file'])
                entry['provided'].append(str(path.relative_to(v)))
            entry.update(verification='Copied library hash matches actual supplier-installed file; matching source package/version and dsc SHA256 verified' if matched else 'SOURCE_VERSION_OR_BINARY_MISMATCH',sourcePackage=source['sourcePackage'],sourceVersion=source['sourceVersion'],supplierPatches='Matching debian source archive supplied')
            c['sourceFulfillment']='Matching distro source/patch/build archives supplied' if matched else 'OPEN: provenance mismatch'
            if not matched:gaps.append(c['name']+': source/binary mismatch')
        elif c['name']=='CPython-runtime':
            source=indexed['python3.12'];entry.update(sourcePackage=source['sourcePackage'],sourceVersion=source['sourceVersion'],provided=['SOURCES_OR_SOURCE_INFO/distro/python3.12/source.json'],verification='Supplier CPython source package and distro patches supplied; interpreter/stdlib copied without Gorilla code edits');c['sourceFulfillment']='Exact CPython supplier source/patch/build archives supplied'
        elif c['name']=='FFmpeg':
            entry.update(provided=['SOURCES_OR_SOURCE_INFO/ffmpeg/ffmpeg-8.0.1.tar.xz','SOURCES_OR_SOURCE_INFO/ffmpeg/build-export.json','SOURCES_OR_SOURCE_INFO/ffmpeg/vendor-Dockerfile_x86_64','SOURCES_OR_SOURCE_INFO/ffmpeg/opencv-python-92-source.tar.gz'],verification='Binary version/configuration measured; release sources and matching vendor build scripts supplied; independent bit-for-bit reproducibility not claimed',supplierPatches='No source patches in supplied FFmpeg vendor build recipe; wheel ELF repair handled by vendor build scripts')
            c['sourceFulfillment']='Sources, measured configuration and vendor build scripts supplied; no Gorilla binary/source changes'
        elif c['name']=='mediapipe':
            entry.update(gorillaPatches='SOURCES_OR_SOURCE_INFO/mediapipe-0.10.33-vision-only.diff',provided=['SOURCES_OR_SOURCE_INFO/mediapipe-modification.json'],verification='Original/modified Python module hashes and full patch supplied; upstream Apache-2.0 notices retained')
            entry['provided']+=['SOURCES_OR_SOURCE_INFO/mediapipe-static/source-index.json','SOURCES_OR_SOURCE_INFO/mediapipe-static/eigen-source.tar.gz']
            c['noticeRequirement']+=['SOURCES_OR_SOURCE_INFO/mediapipe-static'];c['license']='Apache-2.0; static Eigen MPL-2.0 and XNNPACK BSD-3-Clause notices/source retained'
            c['sourceRequirement']='Pinned Eigen headers source SHA verified against MediaPipe release WORKSPACE; source/archive and original license copies supplied'
            c['sourceFulfillment']='Pinned static dependency sources/notices plus explicit Python import modification supplied'
        elif 'GNU-compiler-runtimes' in c['name']:
            matching=[r for r in compiler if 'runtime/lib/python3.12/dist-packages/'+r['wheelFile'] in c['files']]
            if len(matching)!=len(c['files']):raise ValueError('COMPILER_BINARY_PROVENANCE_MISSING')
            for r in matching:
                if sha(v/'runtime/lib/python3.12/dist-packages'/r['wheelFile'])!=r['wheelSha256'] or not r['codeAndReadonlyDataMatch']:raise ValueError('COMPILER_BINARY_PROVENANCE_MISMATCH')
            rpms=sorted(set(r['sourceRpm'] for r in matching));entry.update(verification='Supplier binary build IDs and .text/.rodata hashes match; supplier RPM headers identify exact supplied source RPMs',supplierPatches='Exact source RPMs include distro patches and gcc.spec; wheel ELF repair documented, no Gorilla code changes',provided=['SOURCES_OR_SOURCE_INFO/wheel-compiler-runtimes/'+p for p in rpms]+['SOURCES_OR_SOURCE_INFO/wheel-compiler-runtimes/wheel-correspondence.json'])
            c['version']=' / '.join(rpms);c['license']='libgfortran: GPL-3.0-or-later WITH GCC-exception-3.1; libquadmath: LGPL-2.1-or-later';c['sourceFulfillment']='Exact source RPMs, spec/build recipes, matching code hashes and authentic licenses supplied';entry['version']=c['version']
            c['noticeRequirement']+=['SOURCES_OR_SOURCE_INFO/wheel-compiler-runtimes/notices'];c['sourceRequirement']='Matching supplier sources/patches/build recipes supplied; see component source map'
        elif c['name'].startswith('opencv-'):
            entry.update(provided=['SOURCES_OR_SOURCE_INFO/ffmpeg','SOURCES_OR_SOURCE_INFO/wheel-compiler-runtimes'],verification='Exact FFmpeg and GNU compiler runtime source/build materials supplied; Qt absent');c['sourceFulfillment']='Applicable native source materials supplied'
        else:
            entry['verification']='Authentic component notices supplied; no source requirement inferred for permissive code. Embedded compiler runtimes classified separately.'
            c['sourceRequirement']=None;c['sourceFulfillment']='No additional corresponding-source requirement established by applicable permissive notices'
        entry['sourceRequired']=c['sourceRequirement']
        source_map[c['name']]=entry
    write(info/'component-source-map.json',source_map)
    report['acceptance']='Applicable technical materials supplied; final offline validation separate; clean-PC/T020 remains outside T016'
    for c in report['components']:write(v/'THIRD_PARTY_NOTICES'/(re.sub(r'[^a-zA-Z0-9_.-]','_',c['name'])+'.json'),c)
    write(v/'redistribution.json',report)
    write(v/'source-obligations.json',[dict(component=c['name'],required=c['sourceRequirement'],fulfillment=c['sourceFulfillment'],sourceReference='SOURCES_OR_SOURCE_INFO/component-source-map.json') for c in report['components'] if c['sourceRequirement']])
    write(info/'remaining-source-gaps.json',gaps)
    # Actual Player notices for the exact platform/backend/release, plus separate libdecor references.
    licenses=output/'LICENSES';notices=output/'THIRD_PARTY_NOTICES';player_info=output/'SOURCES_OR_SOURCE_INFO'
    for name in ['Unity-Player-Linux-Mono-6000.3.23f1.pdf','Unity-Player-Linux-Mono-6000.3.23f1.txt','Mono-9d7c9afd-Third-Party-Notices.md','mono-license','libdecor-upstream-LICENSE','libdecor-upstream-copyrights.txt']:copy(materials/name,licenses/name)
    copy(materials/'libdecor-provenance.json',player_info/'libdecor-provenance.json')
    write(notices/'Player.json',dict(version='6000.3.23f1',engineCommit='09d2ecc7fb28',monoCommit='9d7c9afd266e523029fb0cab48e944e528e61e68',distributionClass='BUNDLED',license='Unity runtime terms + exact Player third-party notices; Mono MIT/BSD notices retained',noticeReference=['LICENSES/Unity-Player-Linux-Mono-6000.3.23f1.pdf','LICENSES/Mono-9d7c9afd-Third-Party-Notices.md'],sourceReference=['https://github.com/Unity-Technologies/mono/tree/9d7c9afd266e523029fb0cab48e944e528e61e68'],gorillaNativePatches='NONE'))
    write(notices/'libdecor.json',dict(version='Unity6000.3.23f1 supplied artifacts / ABI0; upstream patch release unexposed',distributionClass='BUNDLED',license='MIT; authentic upstream permission/copyright headers retained',noticeReference=['LICENSES/libdecor-upstream-LICENSE','LICENSES/libdecor-upstream-copyrights.txt'],sourceReference=['SOURCES_OR_SOURCE_INFO/libdecor-provenance.json'],gorillaPatches='NONE',sourceObligation='No corresponding-source requirement under MIT; no exact upstream revision or vendor patch list asserted',limitation='The upstream0.1.1 reference is for notices, not claimed as a byte-identical Unity source build'))
    (player_info/'README.txt').write_text('Source archives, distro patches, measured FFmpeg configuration, vendor build scripts, matched GNU runtime SRPMs and explicit MediaPipe modification are supplied. No source offer is issued. Host graphics/loader libraries are SYSTEM_PROVIDED. Libdecor MIT notices are retained; its unexposed upstream patch revision is not invented and creates no MIT corresponding-source requirement. This is technical redistribution documentation, not universal platform certification.\n')
    # Refresh Vision manifest to include reconciled materials and notices added after the first inventory.
    manifest=json.loads((v/'runtime-manifest.json').read_text());prior={f['path']:f for f in manifest['files']}
    manifest['files']=[]
    for path in sorted(v.rglob('*')):
        if not path.is_file() or path.name=='runtime-manifest.json':continue
        rel=str(path.relative_to(v));item=prior.get(rel,dict(path=rel,component='T016 redistribution materials',version='2026-10-13',origin='Materials source indexes and scripts',distributionClass='BUNDLED',license='See authentic component/source materials',noticeReference=['THIRD_PARTY_NOTICES'],sourceReference=['SOURCES_OR_SOURCE_INFO']))
        owner=next((c for c in report['components'] if rel in c['files']),None)
        if owner:item.update(sourceObligation=owner['sourceRequirement'],license=owner['license'] if len(owner['license'])<=256 else 'Full component license retained in redistribution.json and authentic noticeReference texts',noticeReference=owner['noticeRequirement'])
        if owner and 'GNU-compiler-runtimes' in owner['name']:item.update(version=owner['version'],license=owner['license'],noticeReference=owner['noticeRequirement'])
        if rel.startswith('SOURCES_OR_SOURCE_INFO/distro/'):
            source=indexed.get(rel.split('/')[2])
            if source:item.update(component='source-package-'+source['sourcePackage'],version=source['sourceVersion'],origin=source['sourcePublication'],license='Exact supplier source package licenses/copyright retained inside original and debian source archives',noticeReference=['SOURCES_OR_SOURCE_INFO/distro/'+source['sourcePackage']+'/source.json'],sourceReference=['SOURCES_OR_SOURCE_INFO/distro/'+source['sourcePackage']+'/source.json'])
        elif rel.startswith('SOURCES_OR_SOURCE_INFO/ffmpeg/'):
            item.update(component='FFmpeg corresponding sources / vendor build materials',version='8.0.1 / opencv-python92',origin='FFmpeg release archive and opencv-python commit4ddfc013fd1f13d9b9e379dbebf2cdbeb052e7f8',license='LGPL-2.1-or-later for FFmpeg; upstream wrapper licenses retained in source archive',noticeReference=['THIRD_PARTY_NOTICES/FFmpeg.json'],sourceReference=['SOURCES_OR_SOURCE_INFO/ffmpeg/build-export.json'])
        elif rel.startswith('SOURCES_OR_SOURCE_INFO/wheel-compiler-runtimes/'):
            item.update(component='GNU compiler runtime source/build/notice materials',version='gcc4.8.5-44.el7 / gcc8.5.0-28.el8_10.alma.1',origin='CentOS/AlmaLinux supplier source RPMs and matching binary license extracts',license='Supplier GNU source/runtime licenses, exceptions and notices retained in source RPMs/notice directory',noticeReference=['SOURCES_OR_SOURCE_INFO/wheel-compiler-runtimes/notices'],sourceReference=['SOURCES_OR_SOURCE_INFO/wheel-compiler-runtimes/source-rpm-index.json','SOURCES_OR_SOURCE_INFO/wheel-compiler-runtimes/wheel-correspondence.json'])
        item.update(bytes=path.stat().st_size,sha256=sha(path));manifest['files'].append(item)
    write(v/'runtime-manifest.json',manifest)
    write(output/'acceptance.json',dict(AC1='PASS — PHYSICAL REFERENCE PC (retained evidence)',AC2='MATERIALS COMPLETE; fresh final offline validation recorded separately',AC3='PASS static; final technical runtime verification separate',DEC025='ACCEPTED architecture/profile',openGaps=gaps,scope='Linux reference only; no human tests requested or certified'))
    whole=[]
    for path in sorted(output.rglob('*')):
        if not path.is_file() or path==output/'runtime-manifest.json':continue
        rel=str(path.relative_to(output))
        if rel.startswith('Vision/') and rel!='Vision/runtime-manifest.json':item=dict(next(f for f in manifest['files'] if f['path']==rel[7:]));item['path']=rel;item['noticeReference']=['Vision/'+p for p in item['noticeReference']];item['sourceReference']=['Vision/'+p for p in item['sourceReference']]
        else:
            decor=path.name.startswith('libdecor');item=dict(path=rel,component='libdecor' if decor else 'Unity Player / Gorilla content / redistribution materials',version='6000.3.23f1' if not decor else 'Unity6000.3.23f1 artifact / ABI0',origin='Unity Linux support/actual development build and local materials scripts',distributionClass='BUNDLED',license='MIT' if decor else 'Unity runtime terms/third-party notices; no third-party license asserted for Gorilla-owned content',noticeReference=['THIRD_PARTY_NOTICES/libdecor.json' if decor else 'THIRD_PARTY_NOTICES/Player.json'],sourceReference=['SOURCES_OR_SOURCE_INFO/libdecor-provenance.json' if decor else 'Vision/SOURCES_OR_SOURCE_INFO/component-source-map.json'])
        item.update(bytes=path.stat().st_size,sha256=sha(path));whole.append(item)
    write(output/'runtime-manifest.json',dict(version=3,files=whole,dependencies=manifest['dependencies'],selfExcluded='runtime-manifest.json',acceptanceReference='acceptance.json'))
    return dict(path=str(output),visionFiles=len(manifest['files']),distributionFiles=len(whole)+1,bytes=sum(f['bytes'] for f in whole),openGaps=gaps,counts=counts)

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--vision',type=pathlib.Path,required=True);p.add_argument('--player',type=pathlib.Path,required=True);p.add_argument('--materials',type=pathlib.Path,required=True);p.add_argument('--output',type=pathlib.Path,required=True);a=p.parse_args()
    print(json.dumps(assemble(a.vision,a.player,a.materials,a.output)))
