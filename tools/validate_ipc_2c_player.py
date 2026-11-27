#!/usr/bin/env python3
"""Finite real Player/JVM baseline. Three individual results, then >=600s real 1Hz liveness.
Stdlib only; X11 visible normal shutdown and direct-child ownership checks reused from 2B.
Raw logs ignored; summary contains no tokens/environment dumps/payloads.
"""
import argparse
import json
import os
from pathlib import Path
import time
from validate_ipc_2b_player import Player,X11

def rss(pid):
    try:
        for line in Path('/proc',str(pid),'status').read_text().splitlines():
            if line.startswith('VmRSS:'):return int(line.split()[1])*1024
    except FileNotFoundError:pass
    return None

def summary(p):
    assert p.path.stat().st_size<8*1024*1024,'Raw log limit exceeded'
    results=[json.loads(l.split('IPC_2C_RESULT ',1)[1]) for l in p.path.read_text().splitlines() if l.startswith('IPC_2C_RESULT ')]
    assert len(results)==1,'Exactly one completed summary expected'
    result=results[0]
    assert result['errors']==0 and result['timeouts']==0,result
    assert result['cleanExit'] and result['exitCode']==0 and result['cleanupComplete'] and not result['forced'],result
    assert p.player.returncode==0 and not p.residuals()
    assert p.event('STOPPED')['cleanupComplete']
    return result

def main():
    a=argparse.ArgumentParser()
    for name in ['player','java','jar','output']:a.add_argument('--'+name,type=Path,required=True)
    args=a.parse_args();args.output.mkdir(parents=True,exist_ok=True);x=X11();players=[]
    report={'runs':[],'stability':None,'scope':'Linux development IPC only; no phone/gameplay latency or global leak claim'}
    try:
        for n in range(1,4):
            p=Player(args,'benchmark-'+str(n),smoke=True,mode='benchmark',measure=True);players.append(p)
            e=p.event('FIRST_PONG');p.verify_listeners(e)
            assert f'PPid:\t{p.player.pid}\n' in Path('/proc',str(e['pid']),'status').read_text()
            initial={'unity':rss(p.player.pid),'java':rss(e['pid'])}
            p.player.wait(timeout=45);r=summary(p)
            report['runs'].append({'run':n,'warmup':100,'N':r['samples'],'result':r,'memoryInitialRSS':initial,'ownershipVerified':True,'playerExit':p.player.returncode,'residuals':p.residuals(),'listeners':p.listenerEvidence,'events':p.events()})
            assert r['samples']==1000 and r['sent']==1100 and r['received']==1100 and r['valid']==1100,r
            assert r['p50Ms']<=5 and r['p95Ms']<=10 and r['maximumMs']<=50,r
            assert 45<=r['effectiveHz']<=51,r # effective cadence is published, never substituted with 50.
            print('RUN',n,json.dumps(r),flush=True)
        p=Player(args,'stability',mode='lifecycle',measure=True);players.append(p)
        e=p.event('FIRST_PONG');p.verify_listeners(e)
        assert f'PPid:\t{p.player.pid}\n' in Path('/proc',str(e['pid']),'status').read_text()
        start=time.monotonic();memory=[];next_sample=0
        while time.monotonic()-start<601:
            elapsed=time.monotonic()-start
            assert p.player.poll() is None and Path('/proc',str(e['pid'])).exists(),'Premature exit'
            assert not any(v['event'] in ['FAILED','STOPPED'] for v in p.events()),'Lifecycle ended early'
            if elapsed>=next_sample:
                memory.append({'seconds':round(elapsed,3),'unityRSS':rss(p.player.pid),'javaRSS':rss(e['pid'])});next_sample+=5
            time.sleep(.2) # finite measurement interval, not readiness.
        p.close(x);r=summary(p)
        assert r['durationSeconds']>=600 and r['sent']==r['received']==r['valid'] and r['samples']>=590,r
        report['stability']={'result':r,'elapsedRunningSeconds':round(time.monotonic()-start,3),'memory':memory,'managedHeap':'NOT MEASURED: portable instrumentation not available; RSS observation only','memoryInvestigationThreshold':'max(64 MiB,25% baseline); observation, not leak certification','playerExit':p.player.returncode,'residuals':p.residuals(),'listeners':p.listenerEvidence,'events':p.events()}
        for key in ['unityRSS','javaRSS']:
            values=[m[key] for m in memory if m[key] is not None]
            report['stability'][key+'Observation']={'initial':values[0],'final':values[-1],'peak':max(values),'investigate':values[-1]-values[0]>max(64*1024*1024,.25*values[0])} if values else {'status':'NOT MEASURED'}
        print('PASS: three independent N=1000 runs and >=600s real heartbeat; residual own child 0.',flush=True)
    finally:
        for p in players:p.emergency_cleanup()
        (args.output/'measurement-results.json').write_text(json.dumps(report,indent=2)+'\n')
if __name__=='__main__':main()
