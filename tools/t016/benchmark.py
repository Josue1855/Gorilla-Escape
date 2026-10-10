#!/usr/bin/env python3
"""Private physical capture. Persist aggregate metrics only, never CAMERA_FRAME records."""
import argparse,json,os,subprocess,threading,time
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('--python',required=True);p.add_argument('--worker',required=True);p.add_argument('--model',required=True);p.add_argument('--output',required=True);p.add_argument('--runs',type=int,default=3);a=p.parse_args()
out=Path(a.output)
if out.exists():raise SystemExit('Preserve previous evidence')
records=[]
for run in range(1,a.runs+1):
 child=subprocess.Popen([a.python,a.worker,'--mode','physical','--model',a.model,'--device','/dev/video0','--warmup','5','--duration','35'],stdin=subprocess.PIPE,stdout=subprocess.PIPE,stderr=subprocess.PIPE,text=True)
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
 record={'run':run,'exitCode':code,'cameraInputReceived':frames,'ordered':ordered,'stderrBytes':sum(errors),'summary':summary,'physicalHumanCases':'NOT RUN','residualOwn':Path('/proc/'+str(child.pid)).exists()}
 records.append(record);out.parent.mkdir(parents=True,exist_ok=True);out.write_text(json.dumps({'runs':records,'threshold':30,'physical':True,'imagePersistence':False},indent=2)+'\n');print(json.dumps(record),flush=True)
 if code!=0:break
