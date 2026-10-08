"""Linux emulator QA: existing components, isolated WT, and virtual sensor observation."""
import argparse, asyncio, datetime, hashlib, http.server, json, os, signal, socket, sys, tempfile, threading, time
from pathlib import Path
from types import SimpleNamespace
ROOT=Path(__file__).resolve().parent
REPO=ROOT.parents[2]
sys.path.insert(0,str(REPO/'tools'))
from validate_android_3a import Run, Blocked
from run import cert, start_java, stop
from prepare import JAVA, SHA

class Fixture(http.server.BaseHTTPRequestHandler):
    def do_GET(self):
        routes={'/sensors/':REPO/'PWA/spikes/sensors/index.html','/sensors/capture.js':REPO/'PWA/spikes/sensors/capture.js'}
        if self.path=='/':
            data=b'<!doctype html><meta name="viewport" content="width=device-width"><title>WT Android lab</title><h1>WebTransport Android</h1><p>Laboratorio aislado. Sin gameplay.</p><pre id="results"></pre>';mime='text/html'
        elif self.path in routes:data=routes[self.path].read_bytes();mime='text/javascript' if self.path.endswith('.js') else 'text/html'
        else:self.send_error(404);return
        self.send_response(200);self.send_header('Content-Type',mime+'; charset=utf-8');self.send_header('Cache-Control','no-store');self.end_headers();self.wfile.write(data)
    def log_message(self,*args):pass

def record(report,name,status,detail=''):
    report['cases'].append(dict(name=name,status=status,detail=detail))

async def browser(run,report,private,output):
    run.start_android();report['android']=run.report['android']
    fixture=http.server.ThreadingHTTPServer(('127.0.0.1',0),Fixture)
    thread=threading.Thread(target=fixture.serve_forever,daemon=True);thread.start()
    labport=fixture.server_address[1];processes=[];cdp=None
    try:
        run.adb_call('reverse',f'tcp:{labport}',f'tcp:{labport}')
        debugger=int(run.adb_call('forward','tcp:0','localabstract:chrome_devtools_remote').strip())
        cdp=await run.connect(debugger)
        # Existing React/Java component: no new product code or altered trust.
        hp=run.start_java();run.adb_call('reverse',f'tcp:{hp}',f'tcp:{hp}')
        url=f'http://127.0.0.1:{hp}/';await cdp.navigate(url)
        await cdp.until("document.querySelector('h1')?.textContent.includes('Comprueba tu conexión')")
        record(report,'react_load','PASS','Java-hosted React via adb reverse; HTTP component, not LAN HTTPS')
        await cdp.evaluate("document.querySelector('button').click()")
        await cdp.until("document.body.innerText.includes('Petición recibida:')")
        health=await cdp.evaluate("fetch('/api/health',{cache:'no-store'}).then(async r=>({status:r.status,cache:r.headers.get('cache-control'),service:(await r.json()).service}))")
        assert health=={'status':200,'cache':'no-store','service':'gorilla-escape-local'}
        report['health']=health;record(report,'health_and_interaction','PASS')
        await run.screenshot(cdp,'react-health');await cdp.reload()
        await cdp.until("document.querySelector('button') !== null");record(report,'react_reload','PASS')
        await cdp.connection.close();cdp=None
        run.adb_call('shell','am','force-stop','com.android.chrome')
        run.adb_call('shell','am','start','-a','android.intent.action.VIEW','-d',url,'-p','com.android.chrome')
        cdp=await run.connect(debugger);await cdp.navigate(url)
        await cdp.until("document.querySelector('button') !== null");record(report,'chrome_restart','PASS')
        run.stop_java();await cdp.evaluate("document.querySelector('button').click()")
        await cdp.until("document.body.innerText.includes('No podemos contactar con la PC')",deadline=8)
        record(report,'java_connection_failure','PASS');await run.screenshot(cdp,'connection-error')
        cases=[]
        for label,expired in [('valid',False),('expired',True)]:
            directory=private/label;pin=cert(directory,expired,'127.0.0.1');t=time.monotonic()
            p,port,token=start_java(directory,'127.0.0.1');processes.append(p)
            report[label+'StartupMs']=(time.monotonic()-t)*1000
            wturl=f'https://10.0.2.2:{port}/probe'
            if expired:cases.append({'name':'expired-certificate','expected':'REJECTED','url':wturl,'hash':pin,'token':token})
            else:
                for name,patch,expected in [('correct-certificate',{},'PASS'),('wrong-fingerprint',{'badPin':True},'REJECTED'),('unauthorized-session',{'token':'0'*64},'REJECTED'),('oversize',{'oversize':True},'REJECTED'),('manual-recovery',{},'PASS')]:
                    cases.append({'name':name,'expected':expected,'url':wturl,'hash':pin,'token':token,**patch})
        laburl=f'http://127.0.0.1:{labport}/'
        async def prepare_page():
            await cdp.navigate(laburl)
            features=await cdp.evaluate("({secureContext:isSecureContext,webtransport:typeof WebTransport==='function'})")
            assert features['secureContext'] and features['webtransport'];report['features']=features
            await cdp.evaluate((ROOT/'client.js').read_text().replace('export async function runProbe','window.runProbe = async function'))
        await prepare_page();report['wtResults']=[]
        stages={'wrong-fingerprint':'CONNECTING','expired-certificate':'CONNECTING','unauthorized-session':'AUTHENTICATING','oversize':'OVERSIZE'}
        baseline=False
        for case in cases:
            result=await cdp.evaluate('window.runProbe('+json.dumps(case)+')')
            item={'name':case['name'],'expected':case['expected'],**result};report['wtResults'].append(item)
            passed=result['status']==case['expected'] and (result.get('invalidRejected') is True if case['expected']=='PASS' else result.get('stage')==stages[case['name']] and not result.get('deadline',True))
            if case['name']=='correct-certificate':baseline=passed
            status='PASS' if passed else 'FAIL'
            if case['expected']=='REJECTED' and not baseline:status='BLOCKED'
            record(report,'wt_'+case['name'],status,'Expected result and exact failure stage; no TLS bypass')
            await cdp.evaluate("document.querySelector('#results').textContent="+json.dumps(json.dumps([{'name':x['name'],'status':x['status']} for x in report['wtResults']],indent=2)))
        await run.screenshot(cdp,'webtransport-results')
        await cdp.reload();await prepare_page()
        retry=await cdp.evaluate('window.runProbe('+json.dumps(cases[0])+')');report['wtAfterReload']=retry
        record(report,'wt_after_reload','PASS' if retry['status']=='PASS' else 'FAIL')
        # Existing capture page. Observed values are emulator-generated, never physical evidence.
        await cdp.navigate(f'http://127.0.0.1:{labport}/sensors/')
        await cdp.until("document.querySelector('#output')?.textContent.includes('motion')")
        await cdp.evaluate("document.querySelector('#start').click()")
        await cdp.until("JSON.parse(document.querySelector('#output').textContent).state === 'RUNNING'",deadline=10)
        await cdp.until("JSON.parse(document.querySelector('#output').textContent).channels.motion.events >= 10",deadline=8)
        record(report,'virtual_motion_events','PASS','Emulated Android events observed; not physical motion or sensor accuracy')
        report['sensorObservation']=await cdp.evaluate("JSON.parse(document.querySelector('#output').textContent)")
        record(report,'sensor_page','PASS','Existing UI and capture only; samples are virtual/possibly absent, not hardware validation')
        await cdp.evaluate("document.querySelector('#stop').click()")
        report['sensorStopped']=await cdp.evaluate("JSON.parse(document.querySelector('#output').textContent)")
        assert report['sensorStopped']['state']=='STOPPED'
        record(report,'sensor_stop','PASS');await run.screenshot(cdp,'sensors-virtual-stopped')
    finally:
        if cdp:await cdp.connection.close()
        report['wtCleanup']=[stop(p) for p in processes]
        fixture.shutdown();fixture.server_close();thread.join(timeout=2)
        report['fixtureStopped']=not thread.is_alive()

def main():
    parser=argparse.ArgumentParser();parser.add_argument('--sdk',type=Path,default=Path.home()/'Android/Sdk');parser.add_argument('--output',type=Path,required=True);args=parser.parse_args()
    output=args.output.resolve()
    if output.exists():raise SystemExit('Use a new evidence directory; historical results are never overwritten')
    output.mkdir(parents=True)
    report={'date':datetime.datetime.now(datetime.timezone.utc).date().isoformat(),'scope':'Android emulator; secure loopback fixture via ADB reverse; QUIC UDP via emulator host alias 10.0.2.2','cases':[],'dependencyCommit':SHA,'physicalAndroid':'NOT RUN','iphone':'NOT RUN','physicalLan':'NOT RUN','offlinePwa':'NOT RUN','gate3A':'IN PROGRESS'}
    with tempfile.TemporaryDirectory(prefix='gorilla-wt-android-') as temporary:
        private=Path(temporary);raw=private/'raw';raw.mkdir();android=private/'android-state';android.mkdir()
        run=Run(SimpleNamespace(sdk=args.sdk.resolve(),java=JAVA/'bin/java',jar=REPO/'Server/target/local-server-0.1.0-SNAPSHOT.jar',output=raw),android)
        try:
            if not os.access('/dev/kvm',os.R_OK|os.W_OK):raise Blocked('KVM unavailable')
            asyncio.run(browser(run,report,private,output))
        except (Blocked,ImportError) as e:record(report,'environment','BLOCKED',type(e).__name__)
        except KeyboardInterrupt:record(report,'cancelled','SKIP','Cancelled; cleanup attempted')
        except Exception as e:record(report,'automation','FAIL',type(e).__name__)
        finally:
            run.cleanup();report['androidCleanup']=run.report['cleanup']
            for p in raw.glob('*.png'):(output/p.name).write_bytes(p.read_bytes())
            # No raw logcat/UI dumps, credentials or addresses in published diagnostic logs.
            log=raw/'chrome-logcat.txt'
            report['diagnostic']={'boundedChromeLogCaptured':log.exists(),'rawLogsPublished':False}
            clean=report['androidCleanup']['ownedResiduals']==0 and report['androidCleanup']['javaGraceful'] and report.get('fixtureStopped',False) and all(c['exitCode']==0 and not c['forced'] and not c['residual'] and c['udpPortReleased'] for c in report.get('wtCleanup',[]))
            record(report,'cleanup','PASS' if clean else 'FAIL')
    report['counts']={s:sum(c['status']==s for c in report['cases']) for s in ['PASS','FAIL','SKIP','BLOCKED']}
    report['pass']=report['counts']['FAIL']==0 and report['counts']['BLOCKED']==0 and len(report.get('wtResults',[]))==6
    report['javaHostJarSha256']=hashlib.sha256((REPO/'Server/target/local-server-0.1.0-SNAPSHOT.jar').read_bytes()).hexdigest()
    report['sourceHashes']={str(p.relative_to(REPO)):hashlib.sha256(p.read_bytes()).hexdigest() for p in [Path(__file__),ROOT/'client.js',ROOT/'run.py',REPO/'tools/validate_android_3a.py',REPO/'PWA/spikes/sensors/capture.js']}
    (output/'results.json').write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps({'pass':report['pass'],'counts':report['counts'],'report':str(output/'results.json')}))
    raise SystemExit(0 if report['pass'] else 1)
if __name__=='__main__':main()
