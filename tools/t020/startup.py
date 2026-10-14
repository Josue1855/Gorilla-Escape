#!/usr/bin/env python3
"""Repeated owned Player/JRE lifecycle and optional idle IPC soak; no physical QA claim."""
import argparse,json,os,sys,tempfile,time,hashlib,subprocess
from collections import deque
from pathlib import Path
from types import SimpleNamespace
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
from validate_ipc_2b_player import Player,X11
from validate_ipc_2c_player import rss

def main():
 p=argparse.ArgumentParser()
 for name in ['player','java','jar','output']:p.add_argument('--'+name,type=Path,required=True)
 p.add_argument('--repetitions',type=int,default=20);p.add_argument('--soak-seconds',type=float,default=0);a=p.parse_args()
 if a.output.exists():raise ValueError('PRESERVE_EVIDENCE')
 if not 1<=a.repetitions<=100 or not 0<=a.soak_seconds<=3600:p.error('Bounded run count/duration required')
 report={'base':subprocess.check_output(['git','rev-parse','HEAD'],text=True).strip(),'classification':'LAB reference developer PC; physical cleanPC NOT RUN','camera':'NOT RUN in this IPC-only harness','Vision':'NOT RUN','gate':'FAIL','runs':[],'playerSha256':hashlib.sha256(a.player.read_bytes()).hexdigest(),'jarSha256':hashlib.sha256(a.jar.read_bytes()).hexdigest()}
 # Prevent ambient opt-in physical/sensor diagnostic modes from contaminating this scope.
 for k in list(os.environ):
  if k.startswith(('GORILLA_CAMERA_','GORILLA_PHONE_','GORILLA_MOBILE_','GORILLA_IPC_','GORILLA_FUSION_')):del os.environ[k]
 with tempfile.TemporaryDirectory(prefix='gorilla-t020-lifecycle-') as temp:
  args=SimpleNamespace(player=a.player.resolve(),java=a.java.resolve(),jar=a.jar.resolve(),output=Path(temp));x=X11()
  for i in range(a.repetitions):
   player=None;row={'run':i+1,'gate':'FAIL'};report['runs'].append(row)
   try:
    player=Player(args,'run'+str(i+1),mode='lifecycle',measure=True);ready=player.event('FIRST_PONG');row['startupSeconds']=time.monotonic()-player.started
    duration=a.soak_seconds if i==0 else 0;started=time.monotonic();values={name:deque(maxlen=4096) for name in ['unity','java']};edges={};counts={name:0 for name in values}
    while time.monotonic()-started<duration:
     if player.player.poll() is not None or not Path('/proc',str(ready['pid'])).exists():raise ValueError('PROCESS_DIED_DURING_SOAK')
     for name,pid in [('unity',player.player.pid),('java',ready['pid'])]:
      value=rss(pid)
      if value is not None:
       values[name].append(value);counts[name]+=1;edges.setdefault(name,[value,value])[1]=value
     time.sleep(min(1,max(0,duration-(time.monotonic()-started))))
    row['measuredHoldSeconds']=time.monotonic()-started;row['rssBytes']={name:{'N':counts[name],'retained':len(data),'first':edges[name][0] if name in edges else None,'last':edges[name][1] if name in edges else None,'delta':edges[name][1]-edges[name][0] if name in edges else None,'peak':max(data) if data else None,'scope':'1Hz VmRSS; startup/GC/load included; not a leak diagnosis'} for name,data in values.items()}
    player.close(x);events=player.events();final=events[-1];row['cleanup']={k:final.get(k) for k in ['state','exitCode','cleanupComplete','forced','errorCode','cleanupError']};row['playerExit']=player.player.returncode;row['residuals']=len(player.residuals());assert final['cleanupComplete'] and not final['forced'] and not final['errorCode'] and not final['cleanupError'];row['gate']='PASS'
   except Exception as e:row['error']=str(e)
   finally:
    if player and player.player.poll() is None:player.emergency_cleanup();row['emergencyCleanup']=True
   print(json.dumps({'run':i+1,'gate':row['gate'],'heldSeconds':row.get('measuredHoldSeconds')}),flush=True)
   if row['gate']!='PASS':break
 report['successfulRuns']=sum(r['gate']=='PASS' for r in report['runs']);report['gate']='PASS' if report['successfulRuns']==a.repetitions else 'FAIL';a.output.write_text(json.dumps(report,indent=2)+'\n');return 0 if report['gate']=='PASS' else 1
if __name__=='__main__':sys.exit(main())
