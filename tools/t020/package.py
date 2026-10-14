#!/usr/bin/env python3
"""Assemble a reproducible Phase0 Linux reference candidate; never declares clean-PC PASS."""
import argparse, hashlib, json, shutil, subprocess, sys, tempfile, zipfile
from pathlib import Path

def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def main():
 p=argparse.ArgumentParser()
 for name in ['vision','player','materials','jar','jdk','jdk-sources','output']:p.add_argument('--'+name,type=Path,required=True)
 p.add_argument('--resume-prepared',action='store_true',help='Complete an existing finalize_distribution output; do not overwrite runtimes')
 a=p.parse_args();root=a.output.resolve();repo=Path(__file__).resolve().parents[2]
 sys.path.insert(0,str(repo/'tools/t016'));from finalize_distribution import assemble
 from refresh_manifest import refresh
 if root.exists():
  if not a.resume_prepared or not (root/'runtime-manifest.json').exists():raise ValueError('PRESERVE_EXISTING_OUTPUT')
 else:
  with tempfile.TemporaryDirectory(prefix='gorilla-t020-prepared-') as temp:
   v=Path(temp)/'Vision';shutil.copytree(a.vision,v)
   for name in ['worker.py','pipeline.py','camera_controls.py']:shutil.copy2(repo/'tools/spikes/camera'/name,v/name)
   refresh(v,v/'manifest.json');summary=assemble(v,a.player,a.materials,root)
   if summary['openGaps']:raise ValueError('VISION_PROVENANCE_OPEN')
 java=root/'Java';java.mkdir(exist_ok=True);runtime=java/'runtime'
 modules='java.se,jdk.crypto.ec,jdk.unsupported,jdk.management,jdk.security.auth'
 if not runtime.exists():subprocess.run([str(a.jdk/'bin/jlink'),'--module-path',str(a.jdk/'jmods'),'--add-modules',modules,'--strip-debug','--no-header-files','--no-man-pages','--output',str(runtime)],check=True)
 if (runtime/'bin/javac').exists() or (runtime/'jmods').exists():raise ValueError('SDK_MUST_NOT_BE_DISTRIBUTED')
 version=subprocess.run([str(runtime/'bin/java'),'-version'],capture_output=True,text=True,check=True).stderr.strip()
 if '21.0.12.1' not in version:raise ValueError('SUPPLIER_RUNTIME_VERSION_CHANGED_REVIEW_REQUIRED')
 shutil.copy2(a.jar,java/'server.jar');sources=java/'SOURCES_OR_SOURCE_INFO/openjdk21';sources.mkdir(parents=True,exist_ok=True)
 # apt-get source --download-only verifies supplier archives; retain exact source/distro patches.
 archives=list(a.jdk_sources.glob('openjdk-21*'));dscs=[f for f in archives if f.suffix=='.dsc']
 if len(dscs)!=1 or '21.0.12.1+1-1~24.04.4' not in dscs[0].read_text():raise ValueError('EXACT_OPENJDK_SOURCE_REQUIRED')
 in_sha=False;verified=[]
 for line in dscs[0].read_text().splitlines():
  if line=='Checksums-Sha256:':in_sha=True;continue
  if in_sha and not line.startswith(' '):break
  if in_sha:
   digest,size,name=line.split();f=a.jdk_sources/name
   if not f.is_file() or sha(f)!=digest or f.stat().st_size!=int(size):raise ValueError('SOURCE_CHECKSUM_INVALID')
   verified.append({'path':name,'sha256':digest,'bytes':int(size)})
 if not verified:raise ValueError('SOURCE_HASHES_REQUIRED')
 for f in archives:shutil.copy2(f,sources/f.name)
 shutil.copy2('/usr/share/doc/openjdk-21-jre-headless/copyright',sources/'copyright')
 notices=java/'dependency-notices';notices.mkdir(exist_ok=True);dependencies=[]
 with zipfile.ZipFile(java/'server.jar') as jar:
  for member in jar.namelist():
   if not member.startswith('BOOT-INF/lib/') or not member.endswith('.jar'):continue
   import io
   data=jar.read(member);found=[]
   with zipfile.ZipFile(io.BytesIO(data)) as dependency:
    for item in dependency.namelist():
     name=Path(item).name.upper()
     if item.startswith('META-INF/') and ('LICENSE' in name or 'NOTICE' in name or name=='COPYRIGHT') and not item.endswith('/'):
      destination=notices/(Path(member).name+'-'+item.replace('/','_'));destination.write_bytes(dependency.read(item));found.append(str(destination.relative_to(root)))
   dependencies.append({'jar':Path(member).name,'sha256':hashlib.sha256(data).hexdigest(),'embeddedNoticeFiles':found,'status':'Extracted actual embedded metadata; source obligations require separate review where applicable'})
 (java/'provenance.json').write_text(json.dumps({'runtime':version,'supplierPackage':'openjdk-21 21.0.12.1+1-1~24.04.4 Ubuntu','modules':modules,'gorillaJdkPatches':'NONE; jlink stock modules','sourceArchives':verified,'serverDependencies':dependencies,'source':'Installed supplier JDK/JMODs, local built JAR; exact supplier source archives/patches retained','cleanPC':'NOT RUN'},indent=2)+'\n')
 verifier='''import hashlib,json,sys
from pathlib import Path
root=Path(sys.argv[1]).resolve();data=json.loads((root/'runtime-manifest.json').read_text())
for f in data['files']:
 p=root/f['path']
 if not p.resolve().is_relative_to(root) or not p.is_file() or hashlib.sha256(p.read_bytes()).hexdigest()!=f['sha256']:raise SystemExit('PACKAGE_INTEGRITY_FAILED')
print('PACKAGE_INTEGRITY_PASS')
'''
 (root/'verify-package.py').write_text(verifier)
 launcher='''#!/bin/sh
set -eu
package_root=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
"$package_root/Vision/python" -I "$package_root/verify-package.py" "$package_root"
export GORILLA_IPC_JAVA="$package_root/Java/runtime/bin/java"
export GORILLA_IPC_JAR="$package_root/Java/server.jar"
export GORILLA_IPC_MODE=lifecycle
export GORILLA_CAMERA_ENABLED=1
export GORILLA_CAMERA_PROFILE=candidate
export GORILLA_CAMERA_BUFFERS=2
exec "$package_root/GorillaEscape.x86_64" "$@"
'''
 (root/'run-base.sh').write_text(launcher);(root/'run-base.sh').chmod(0o755)
 (root/'README-PHASE0.txt').write_text('PHASE0 TECHNICAL BASE ONLY; NO TOURNAMENT/GAMEPLAY. Linux x86_64/glibc2.39 reference candidate. Requires compatible OS graphics/OpenGL/X11 and V4L2 camera capabilities; Windows/macOS NOT VALIDATED. Execute ./run-base.sh and close its window normally. Bundled JRE21/Python/Vision/Server resources; no Unity Editor/SDK/runtime download required by launcher. No private CA key or production admission tokens included. Technical launch is not Academic MVP fallback. Clean physical PC and physical mobile gates remain NOT RUN. See project T019/T020 runbooks for acceptance. Third-party source/notice materials retained; embedded server notices indexed, legal redistribution is not certified by this packaging script.\n')
 files=[]
 for path in sorted(root.rglob('*')):
  if path.is_symlink() and not path.resolve().is_relative_to(root):raise ValueError('PACKAGE_EXTERNAL_SYMLINK_FORBIDDEN '+str(path.relative_to(root)))
  if path.is_file() and path!=root/'runtime-manifest.json':files.append({'path':str(path.relative_to(root)),'sha256':sha(path),'bytes':path.stat().st_size})
 manifest={'version':4,'files':files,'selfExcluded':'runtime-manifest.json','base':subprocess.check_output(['git','rev-parse','HEAD'],cwd=repo,text=True).strip(),'scope':'Phase0 Linux technical candidate; clean-PC NOT RUN','serverNoticeSourceReview':'Embedded notices extracted; no legal approval asserted'}
 (root/'runtime-manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
 print(json.dumps({'files':len(files),'bytes':sum(x['bytes'] for x in files),'manifestSha256':sha(root/'runtime-manifest.json'),'cleanPC':'NOT RUN','runtimeVersion':version}))
if __name__=='__main__':main()
