#!/usr/bin/env python3
"""Inventory actual staging files, preserve authentic notices; never declare source fulfillment.

Reads wheel RECORD/METADATA, ELF DT_NEEDED, dpkg provenance and measured /proc/self/maps.
Adds an independent runtime-manifest; leaves original candidate manifest unchanged.
"""
import argparse,csv,email,hashlib,json,pathlib,re,shutil,subprocess

def digest(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def run(*args):return subprocess.run(args,capture_output=True,text=True).stdout
def relative(p,root):return p.relative_to(root).as_posix()

def inventory(root,loaded):
    root=root.resolve();site=root/'runtime/lib/python3.12/dist-packages'
    if (root/'redistribution.json').exists():raise ValueError('Preserve prior inventory; use another staging copy')
    original=json.loads((root/'manifest.json').read_text())
    loaded=json.loads(loaded.read_text())['loadedFiles']
    licenses=root/'LICENSES';notices=root/'THIRD_PARTY_NOTICES'
    licenses.mkdir();notices.mkdir();owners={};components=[]

    def component(name,version,origin,files,license_evidence,kind,source_requirement):
        ident=re.sub(r'[^a-zA-Z0-9_.-]','_',name)
        copied=[];copyrights=[]
        for p in sorted(set(license_evidence)):
            if not p.is_file():continue
            dest=licenses/ident/relative(p,root) if p.is_relative_to(root) else licenses/ident/p.name
            dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,dest)
            copied.append(relative(dest,root))
            copyrights.extend(line.strip() for line in p.read_text(errors='replace').splitlines()
                              if re.search(r'copyright|©',line,re.I))
        data=dict(name=name,version=version,origin=origin,files=sorted(files),kind=kind,
                  license=origin.get('declaredLicense') or 'See authentic per-component texts; embedded multi-license code is not collapsed to package license',
                  licenseFiles=copied,copyright=sorted(set(copyrights)),
                  redistributionRequirement='Retain applicable licenses, copyright, disclaimers and attribution; unresolved source obligations block external distribution',
                  sourceRequirement=source_requirement,noticeRequirement=copied,
                  sourceFulfillment='NOT VERIFIED' if source_requirement else 'No additional source requirement established by this inventory; inspect embedded notices',
                  runtimeLoadedFiles=[p for p in files if p in loaded])
        (notices/(ident+'.json')).write_text(json.dumps(data,indent=2)+'\n');components.append(data)
        for p in files:owners[p]=name

    for dist in sorted(site.glob('*.dist-info')):
        meta=email.message_from_string((dist/'METADATA').read_text())
        files=[];omitted=[]
        with (dist/'RECORD').open(newline='') as f:
            for row in csv.reader(f):
                p=(site/row[0]).resolve()
                if p.is_relative_to(root) and p.is_file():files.append(relative(p,root))
                else:omitted.append(row[0])
        texts=[root/p for p in files if re.search(r'license|copying|copyright|notice',pathlib.Path(p).name,re.I)]
        if meta['Name'].lower()=='flatbuffers' and (root/'licenses/flatbuffers-25.12.19-LICENSE').is_file():
            texts.append(root/'licenses/flatbuffers-25.12.19-LICENSE')
        requirement=('Exact corresponding source/build patches/options for embedded Qt/FFmpeg remain uncollected; see source-obligations.json'
            if meta['Name'].lower().startswith('opencv') else None)
        component(meta['Name'],meta['Version'],dict(metadata=relative(dist/'METADATA',root),
            urls=meta.get_all('Project-URL') or [meta.get('Home-page')],recordOmittedFromBundle=omitted,
            declaredLicense=meta.get('License-Expression') or meta.get('License')),files,texts,'bundled wheel',
            requirement)

    cvnotice=site/'cv2/LICENSE-3RD-PARTY.txt'
    for name,version,license,pattern,upstream in [('Qt','5.15.18','LGPL-3.0',r'libQt5','https://www.qt.io/'),
        ('FFmpeg','ABI avcodec62.11.100/avformat62.3.100/avutil60.8.100; exact upstream source revision unresolved',
         'LGPL-2.1 per wheel notice; build-option verification pending',r'lib(avcodec|avformat|avutil|avdevice|swscale|swresample)', 'https://ffmpeg.org/')]:
        files=[relative(p,root) for p in site.rglob('*') if p.is_file() and re.match(pattern,p.name)]
        component(name,version,dict(upstream=upstream,wheel='opencv-contrib-python 4.13.0.92',declaredLicense=license),
            files,[cvnotice],'bundled native subcomponent','See explicit source-obligations.json; exact sources/patches/build configuration and applicable linking conditions remain OPEN')

    for p in sorted((root/'runtime/native').glob('*')):
        if not p.is_file():continue
        matches=[line.split('=>')[-1].strip() for line in run('ldconfig','-p').splitlines() if line.strip().startswith(p.name+' ')]
        host=pathlib.Path(matches[0]) if matches else pathlib.Path('/nonexistent')
        candidates=[str(host),str(host.resolve()),str(host).replace('/lib/','/usr/lib/',1)]
        found=''
        for candidate in candidates:
            out=run('dpkg-query','-S',candidate)
            records=[line for line in out.splitlines() if ': ' in line and not line.startswith(('diversion ','local diversion '))]
            if records:found=records[0].rsplit(': ',1)[0];break
        pkg=found or 'UNRESOLVED-'+p.name
        info=run('dpkg-query','-W','-f=${Version}\n${source:Package}\n${source:Version}\n',pkg).splitlines() if found else []
        doc=pathlib.Path('/usr/share/doc')/pkg.split(':')[0]/'copyright'
        texts=[doc] if doc.is_file() else []
        if doc.is_file():
            for name in re.findall(r'/usr/share/common-licenses/([A-Za-z0-9_.+-]+)',doc.read_text(errors='replace')):
                common=pathlib.Path('/usr/share/common-licenses')/name
                if common.is_file():texts.append(common)
        component('system-copy-'+p.name,info[0] if info else 'UNKNOWN',dict(package=pkg,
            sourcePackage=info[1:] if info else [],copiedFrom=str(host),hostHashMatches=host.is_file() and digest(host)==digest(p)),
            [relative(p,root)],texts,'bundled copied system library',
            'For copyleft-covered files provide exact package corresponding source, distro patches and build materials; not downloaded or offered yet. Full copyright file controls per-file exceptions.')

    python_files=[relative(p,root) for p in (root/'runtime').rglob('*') if p.is_file() and relative(p,root) not in owners]
    component('CPython-runtime','3.12.3',dict(origin='/usr/bin/python3.12 and /usr/lib/python3.12',
        package=run('dpkg-query','-W','-f=${Version}', 'python3.12-minimal').strip()),python_files,
        [root/'licenses/Python-copyright.txt',root/'runtime/lib/python3.12/LICENSE.txt'],
        'bundled interpreter/stdlib', 'Inspect retained Python and embedded-extension licenses; GNU readline and other copied system libraries have separate source requirements.')
    component('Pose-Landmarker-lite','float16/v1',original['model'],['pose_landmarker_lite-v1.task'],[],
        'bundled model','Family model card reports Apache-2.0; artifact-specific notice/license provenance and any NOTICE applicability still require completion. No invented model notice.')

    native=[]
    for p in sorted((root/'runtime').rglob('*')):
        if not p.is_file():continue
        with p.open('rb') as f:elf=f.read(4)==b'\x7fELF'
        if not elf:continue
        dynamic=run('readelf','-d',str(p));needed=re.findall(r'\(NEEDED\).*?\[([^]]+)\]',dynamic)
        path=relative(p,root)
        native.append(dict(path=path,component=owners.get(path,'UNRESOLVED'),sha256=digest(p),needed=needed,
            runtimeLoaded=path in loaded,versionEvidence=p.name,upstreamRelease='Not inferred from SONAME; consult component metadata/notices'))
    host_loaded=[]
    for p in loaded:
        if not p.startswith('/'):continue
        host_loaded.append(dict(path=p,classification='system-provided / not redistributed by Vision',
            package=run('dpkg-query','-S',p).strip(),runtimeLoaded=True))
    obligations=[dict(component=name,license=license,required=required,fulfillment='OPEN; no matching source archive or valid source offer assembled; external distribution blocked') for name,license,required in [
        ('Qt 5.15.18','LGPL-3.0 (actual wheel notice)','Exact Qt library source including wheel patches/build materials, notices and applicable LGPL combined-work/relinking conditions; preserve library replacement capability'),
        ('FFmpeg bundled avcodec62/avformat62/avutil60/swscale9/avdevice62','LGPL-2.1 (actual wheel notice; build options not fully verified)','Exact corresponding library source and modifications/build configuration, applicable LGPL linking/replacement materials and notices; verify no GPL/nonfree options'),
        ('Copied GPL/LGPL system libraries, including readline8 and libdb5.3','Per-package copyright files, including exceptions','Matching distro source version, patches and build materials or license-compliant offer/access mechanism; retain full per-file copyright and license notices')]]
    (root/'source-obligations.json').write_text(json.dumps(obligations,indent=2)+'\n')
    report=dict(scope='Actual Vision staging only; Unity proprietary player licensing remains outside this third-party runtime inventory',
        acceptance='AC2 PARTIAL; license/source and clean-host closure pending',components=components,native=native,
        systemAbi=original['systemAbi'],runtimeLoadedHostFiles=host_loaded,
        qtAndFfmpeg='Both bundled, DT_NEEDED by cv2.abi3.so and runtime-loaded. Cannot delete with current OpenCV binary. No dependency swap performed.',
        unusedBundledNative=[p['path'] for p in native if not p['runtimeLoaded']],
        unusedMeaning='Not loaded in import+PoseLandmarker creation probe; not proof removable for all paths')
    (root/'redistribution.json').write_text(json.dumps(report,indent=2)+'\n')
    files=[dict(path=relative(p,root),bytes=p.stat().st_size,sha256=digest(p),
        component=owners.get(relative(p,root),'Gorilla support / generated notices / inventory'))
        for p in sorted(root.rglob('*')) if p.is_file() and p.name!='runtime-manifest.json']
    (root/'runtime-manifest.json').write_text(json.dumps(dict(version=2,files=files,
        selfExcluded='runtime-manifest.json (avoids self-referential hash)',sourceManifestSha256=digest(root/'manifest.json')),indent=2)+'\n')
    return dict(files=len(files),nativeFiles=len(native),components=len(components),bytes=sum(p['bytes'] for p in files))

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--root',type=pathlib.Path,required=True);p.add_argument('--loaded',type=pathlib.Path,required=True);a=p.parse_args()
    print(json.dumps(inventory(a.root,a.loaded)))
