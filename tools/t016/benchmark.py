#!/usr/bin/env python3
"""Private physical capture. Persist aggregate metrics only, never CAMERA_FRAME records."""
import argparse,hashlib,json,os,subprocess,threading,time
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('--python',required=True);p.add_argument('--worker',required=True);p.add_argument('--model',required=True);p.add_argument('--output',required=True);p.add_argument('--runs',type=int,default=3);p.add_argument('--profile',choices=['original','candidate'],default='original');p.add_argument('--buffers',type=int,choices=[1,2,3]);p.add_argument('--dynamic-framerate',type=int,choices=[0,1]);p.add_argument('--duration',type=float,default=61);p.add_argument('--warmup',type=float,default=5);a=p.parse_args()
out=Path(a.output)
if out.exists():raise SystemExit('Preserve previous evidence')
records=[]
for run in range(1,a.runs+1):
 hashes={name:hashlib.sha256((Path(a.worker).parent/name).read_bytes()).hexdigest() for name in ['worker.py','pipeline.py','camera_controls.py']}
 command=[a.python,a.worker,'--mode','physical','--model',a.model,'--device','/dev/video0','--warmup',str(a.warmup),'--duration',str(a.duration),'--profile',a.profile]
 if a.buffers is not None:command+=['--buffers',str(a.buffers)]
 if a.dynamic_framerate is not None:command+=['--dynamic-framerate',str(a.dynamic_framerate)]
 child=subprocess.Popen(command,stdin=subprocess.PIPE,stdout=subprocess.PIPE,stderr=subprocess.PIPE,text=True)
 errors=[]
 def drain():
  # Count stderr only. Do not persist native logs / raw data.
  n=0
  for line in child.stderr:n+=len(line)
  errors.append(n)
 reader=threading.Thread(target=drain);reader.start();summary=None;last=0;frames=0;ordered=True
 for line in child.stdout:
  if line.startswith('CAMERA_FRAME '):
   frame=json.loads(line[13:]);seq=frame['frameSequence'];ordered=ordered and seq>last;last=seq;frames+=1
  elif line.startswith('CAMERA_SUMMARY '):summary=json.loads(line[15:])
 child.stdin.close();code=child.wait(timeout=10);reader.join()
 record={'run':run,'exitCode':code,'cameraInputReceived':frames,'ordered':ordered,'stderrBytes':sum(errors),'summary':summary,'physicalHumanCases':'NOT RUN','residualOwn':Path('/proc/'+str(child.pid)).exists(),'sourceHashes':hashes}
 records.append(record);out.parent.mkdir(parents=True,exist_ok=True);out.write_text(json.dumps({'runs':records,'threshold':30,'physical':True,'imagePersistence':False,'profile':a.profile,'requestedBuffers':a.buffers,'requestedDurationSeconds':a.duration,'lighting':'current ambient; no human participation/controlled lighting qualification'},indent=2)+'\n');print(json.dumps(record),flush=True)
 if code!=0:break
