#!/usr/bin/env python3
"""Inspect connected authorized devices only. Never scan the LAN or store device serials."""
import argparse,json,shutil,subprocess
from datetime import datetime,timezone
from pathlib import Path

def call(args):return subprocess.run(args,capture_output=True,text=True,timeout=15)
def main():
 p=argparse.ArgumentParser();p.add_argument('--adb',default=shutil.which('adb'));p.add_argument('--output',type=Path,required=True);a=p.parse_args()
 if a.output.exists():raise ValueError('PRESERVE_PREVIOUS_EVIDENCE')
 report={'utc':datetime.now(timezone.utc).isoformat(),'classification':'read-only inventory, no physical test accreditation','devices':[],'errors':[],'iPhone':'No iOS automation endpoint supplied; historical reference is not an accessible device','cleanPC':'No second clean physical PC endpoint supplied'}
 if a.adb:
  result=call([a.adb,'devices']);
  if result.returncode:report['errors'].append('ADB inventory failed; raw diagnostics retained locally only')
  else:
   for line in result.stdout.splitlines()[1:]:
    parts=line.split()
    if len(parts)<2:continue
    serial,state=parts[:2]
    if state!='device':report['devices'].append({'ordinal':len(report['devices'])+1,'state':state,'classification':'UNAVAILABLE','serialRetained':False});continue
    def prop(name):return call([a.adb,'-s',serial,'shell','getprop',name]).stdout.strip()
    emulator=prop('ro.kernel.qemu')=='1' or prop('ro.boot.qemu')=='1' or serial.startswith('emulator-')
    chrome=call([a.adb,'-s',serial,'shell','dumpsys','package','com.android.chrome']).stdout
    version=next((x.strip().split('=',1)[1] for x in chrome.splitlines() if 'versionName=' in x),'UNKNOWN')
    report['devices'].append({'ordinal':len(report['devices'])+1,'state':state,'classification':'EMULATOR' if emulator else 'PHYSICAL connected Android','manufacturer':prop('ro.product.manufacturer'),'model':prop('ro.product.model'),'Android':prop('ro.build.version.release'),'Chrome':version,'serialRetained':False})
 else:report['errors'].append('ADB executable unavailable; no conclusion about existence of unattached phones')
 report['physicalAndroidCount']=sum(x['classification']=='PHYSICAL connected Android' for x in report['devices'])
 a.output.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({'physicalAndroidCount':report['physicalAndroidCount'],'inventoryEntries':len(report['devices']),'errors':report['errors']}))
if __name__=='__main__':main()
