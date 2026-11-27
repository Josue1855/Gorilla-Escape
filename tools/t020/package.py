#!/usr/bin/env python3
"""Assemble a reproducible Phase0 Linux reference candidate; never declares clean-PC PASS."""
import argparse, hashlib, json, shutil, subprocess, sys, tempfile, zipfile
from pathlib import Path

def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def main():
 p=argparse.ArgumentParser()
 for name in ['vision','player','materials','jar','jdk','jdk-sources','output']:p.add_argument('--'+name,type=Path,required=True)
 p.add_argument('--cloudflared',type=Path,required=True)
 p.add_argument('--resume-prepared',action='store_true',help='Complete an existing finalize_distribution output; do not overwrite runtimes')
 a=p.parse_args();root=a.output.resolve();repo=Path(__file__).resolve().parents[2]
 sys.path.insert(0,str(repo/'tools/t016'));from finalize_distribution import assemble
 from refresh_manifest import refresh
 if root.exists():
  if not a.resume_prepared or not (root/'runtime-manifest.json').exists():raise ValueError('PRESERVE_EXISTING_OUTPUT')
 else:
  with tempfile.TemporaryDirectory(prefix='gorilla-t020-prepared-') as temp:
   v=Path(temp)/'Vision';shutil.copytree(a.vision,v)
   for name in ['worker.py','pipeline.py','camera_controls.py','preview.py']:shutil.copy2(repo/'tools/spikes/camera'/name,v/name)
   initial=json.loads((v/'manifest.json').read_text())
   if not any(f['path']=='preview.py' for f in initial['files']):initial['files'].append({'path':'preview.py','sha256':sha(v/'preview.py'),'bytes':(v/'preview.py').stat().st_size})
   (v/'manifest.json').write_text(json.dumps(initial,indent=2)+'\n')
   refresh(v,v/'manifest.json');summary=assemble(v,a.player,a.materials,root)
   if summary['openGaps']:raise ValueError('VISION_PROVENANCE_OPEN')
 subprocess.run([sys.executable,str(repo/'tools/t063/package-cloudflared.py'),str(a.cloudflared),str(root)],check=True)
 java=root/'Java';java.mkdir(exist_ok=True);runtime=java/'runtime'
 modules='java.se,jdk.crypto.ec,jdk.unsupported,jdk.management,jdk.security.auth,jdk.httpserver'
 if not runtime.exists():subprocess.run([str(a.jdk/'bin/jlink'),'--module-path',str(a.jdk/'jmods'),'--add-modules',modules,'--strip-debug','--no-header-files','--no-man-pages','--output',str(runtime)],check=True)
 present={line.split('@')[0] for line in subprocess.check_output([str(runtime/'bin/java'),'--list-modules'],text=True).splitlines()}
 if not set(modules.split(',')).issubset(present):raise ValueError('REQUIRED_RUNTIME_MODULE_MISSING')
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
 launcher=(repo/'tools/t062/run-qa.sh').read_text().replace('${GORILLA_QA_MENU:-1}','${GORILLA_QA_MENU:-0}')
 (root/'run-base.sh').write_text(launcher);(root/'run-base.sh').chmod(0o755)
 (root/'tools/connectivity').mkdir(parents=True,exist_ok=True)
 shutil.copy2(repo/'tools/setup-firewall.sh',root/'tools/setup-firewall.sh')
 shutil.copy2(repo/'tools/connectivity/firewall.py',root/'tools/connectivity/firewall.py')
 shutil.copy2(repo/'tools/t062/run-qa.sh',root/'run-qa.sh');(root/'run-qa.sh').chmod(0o755)
 shutil.copy2(repo/'docs/PHASE0_IPHONE15_LIVE_QA_RUNBOOK.md',root/'IPHONE-RUNBOOK.md')
 shutil.copy2(repo/'docs/PHASE0_T063_TRANSPARENT_AUTO.md',root/'TRANSPARENT_CONNECTIVITY.md')
 for name in ['PHASE0_IPHONE15_LIVE_QA_RUNBOOK.md','PHASE0_MAIN_MENU_QA_CENTER.md']:shutil.copy2(repo/'docs'/name,root/name)
 shutil.copytree(repo/'docs/evidence/t062-main-menu-2026-10-14',root/'evidence/t062-main-menu-2026-10-14',dirs_exist_ok=True)
 (root/'README-PHASE0.txt').write_text('PHASE0 TECHNICAL BASE ONLY; NO TOURNAMENT/GAMEPLAY. Linux x86_64/glibc2.39 reference candidate. Requires compatible OS graphics/OpenGL/X11 and V4L2 camera capabilities; Windows/macOS NOT VALIDATED. Execute ./run-base.sh and close its window normally. Bundled JRE21/Python/Vision/Server resources; no Unity Editor/SDK/runtime download required by launcher. No private CA key or production admission tokens included. Technical launch is not Academic MVP fallback. Clean physical PC and physical mobile gates remain NOT RUN. See project T019/T020 runbooks for acceptance. Third-party source/notice materials retained; embedded server notices indexed, legal redistribution is not certified by this packaging script.\n')
 files=[]
 for path in sorted(root.rglob('*')):
  if path.is_symlink() and not path.resolve().is_relative_to(root):raise ValueError('PACKAGE_EXTERNAL_SYMLINK_FORBIDDEN '+str(path.relative_to(root)))
  if path.is_file() and path!=root/'runtime-manifest.json' and path!=root/'Tools/cloudflared':files.append({'path':str(path.relative_to(root)),'sha256':sha(path),'bytes':path.stat().st_size})
 manifest={'version':5,'optionalPinnedBinary':'Tools/cloudflared; Java verifies the pinned SHA before execution; AUTO selects Cabin if missing/invalid','files':files,'selfExcluded':'runtime-manifest.json','base':subprocess.check_output(['git','rev-parse','HEAD'],cwd=repo,text=True).strip(),'scope':'Phase0 Linux technical candidate; clean-PC NOT RUN','serverNoticeSourceReview':'Embedded notices extracted; no legal approval asserted'}
 (root/'runtime-manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
 print(json.dumps({'files':len(files),'bytes':sum(x['bytes'] for x in files),'manifestSha256':sha(root/'runtime-manifest.json'),'cleanPC':'NOT RUN','runtimeVersion':version}))
if __name__=='__main__':main()
