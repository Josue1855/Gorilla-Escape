#!/usr/bin/env python3
"""ABBA camera/Unity telemetry comparison; reuse the no-video offline T016 harness.
No physical-phone, human, or imperceptibility PASS is produced by this tool.
"""
import argparse,hashlib,json,platform,subprocess,sys
from datetime import datetime,timezone
from pathlib import Path

def delta(a,b):
    return {'minimal':a,'enabled':b,'absolute':b-a,'percent':100*(b-a)/a if a else None}

def compare(reports):
    rows=[]
    for left,right in [(0,1),(3,2)]:
        a,b=reports[left],reports[right]
        if a['telemetryMode']!='minimal' or b['telemetryMode']!='enabled':raise ValueError('ABBA_MODES_INVALID')
        rows.append({'pair':len(rows)+1,'unity':{k:delta(a['unityFrameTiming'][k],b['unityFrameTiming'][k]) for k in ['fpsP50','fpsP95','frameTimeP50Ms','frameTimeP95Ms','effectiveFps']},'camera':{k:delta(a['metrics'][k],b['metrics'][k]) for k in ['captureFps','inferenceFps','outputFps']}})
    return rows

def main():
    p=argparse.ArgumentParser();p.add_argument('--build',type=Path,required=True);p.add_argument('--output',type=Path,required=True);p.add_argument('--duration',type=float,default=61);p.add_argument('--warmup',type=float,default=5);a=p.parse_args()
    if a.output.exists():raise SystemExit('Preserve previous evidence; use a new directory')
    if a.duration<=0 or a.warmup<0:raise SystemExit('Invalid duration/warmup')
    a.output.mkdir(parents=True);repo=Path(__file__).resolve().parents[2];reports=[]
    files=['tools/spikes/camera/worker.py','tools/spikes/camera/pipeline.py','tools/t016/offline_player.py','tools/t018/compare.py','Unity/Assets/_Project/Scripts/CameraTracking/CameraFusionLabRunner.cs','Unity/Assets/_Project/Scripts/CameraTracking/VisionSupervisor.cs','Unity/Assets/_Project/Scripts/CameraTracking/FrameTimingWindow.cs']
    provenance={'base':subprocess.check_output(['git','rev-parse','HEAD'],cwd=repo,text=True).strip(),'sourceHashes':{f:hashlib.sha256((repo/f).read_bytes()).hexdigest() for f in files},'playerSha256':hashlib.sha256((a.build/'GorillaEscape.x86_64').read_bytes()).hexdigest(),'assemblySha256':hashlib.sha256((a.build/'GorillaEscape_Data/Managed/GorillaEscape.Runtime.dll').read_bytes()).hexdigest(),'platform':platform.platform(),'utcStarted':datetime.now(timezone.utc).isoformat(),'order':['minimal','enabled','enabled','minimal'],'requestedDurationSeconds':a.duration,'warmupSeconds':a.warmup,'conditions':'same built Player/Vision; MJPG 640x480/2 poses/candidate buffers2/manual10ms/priority1; 640x360 window; WAN off/developer cache hidden; no participants or lighting changes; live host load uncontrolled','cpu':'NOT RUN: no existing CPU sampler; RSS reuses validate_ipc_2c_player.rss at 1 Hz in both arms','physicalPhones':'NOT RUN','humanTests':'NOT RUN','motionToResultEndToEnd':'NOT RUN','imperceptibility':'UNDETERMINED: specification has no numerical threshold'}
    (a.output/'provenance.json').write_text(json.dumps(provenance,indent=2)+'\n')
    for index,mode in enumerate(provenance['order'],1):
        target=a.output/f'{index}-{mode}.json'
        cmd=[sys.executable,str(repo/'tools/t016/offline_player.py'),'--build',str(a.build.resolve()),'--output',str(target.resolve()),'--profile','candidate','--buffers','2','--warmup',str(a.warmup),'--duration',str(a.duration),'--telemetry',mode,'--measure-frame-times']
        print(f'Run {index}/4: {mode}',flush=True)
        run=subprocess.run(cmd,cwd=repo,capture_output=True,text=True)
        if not target.exists():raise RuntimeError('Harness did not produce evidence')
        report=json.loads(target.read_text());reports.append(report)
        print(json.dumps({'run':index,'gate':report['gate'],'unity':report.get('unityFrameTiming'),'captureFps':report.get('metrics',{}).get('captureFps')}),flush=True)
        if run.returncode:raise SystemExit('Comparison incomplete; failed run preserved')
    result={'pairs':compare(reports),'imperceptibility':provenance['imperceptibility'],'scope':'local webcam technical workload; no physical sensors/motion-result ground truth'}
    (a.output/'comparison.json').write_text(json.dumps(result,indent=2)+'\n')
if __name__=='__main__':main()
