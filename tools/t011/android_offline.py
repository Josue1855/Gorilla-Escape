#!/usr/bin/env python3
"""T011 disposable AVD: real Chrome visibility, virtual sensors, Java/RTC/IPC/Unity.
WAN isolation is confined to owned Android guest iptables. ADB reverse is NOT physical LAN/TLS.
No sensor event injection, permission/TLS bypass, persistent host/network changes or dependencies.
"""
import time
import asyncio, hashlib, json, os, re, secrets, subprocess, sys, tempfile, threading, urllib.request, urllib.parse, xml.etree.ElementTree as ET
from pathlib import Path
from types import SimpleNamespace
REPO=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(REPO/'tools'))
from validate_android_3a import Run, CDP, wait, Blocked, CommandFailure
from validate_ipc_2b_player import X11
class OfflineRun(Run):
    def adb_call(self,*args,**kwargs):
        # Package manager can expose Chrome before its VIEW activity resolves on a cold boot.
        if args[:3]==('shell','am','start') and 'about:blank' in args:
            end=time.monotonic()+25
            while True:
                try:return super().adb_call(*args,**kwargs)
                except CommandFailure:
                    if time.monotonic()>=end:raise
                    time.sleep(.5)
        return super().adb_call(*args,**kwargs)
class ObservedCDP(CDP):
    def __init__(self,connection,origin):
        super().__init__(connection);self.origin=origin;self.record=False;self.requests={};self.external=0;self.pagehide=None;self.registrations=[]
    async def call(self,method,params=None):
        self.sequence+=1;seq=self.sequence
        await self.connection.send(json.dumps({'id':seq,'method':method,'params':params or {}}))
        async with asyncio.timeout(20):
            while True:
                msg=json.loads(await self.connection.recv());kind=msg.get('method');p=msg.get('params',{})
                if kind=='ServiceWorker.workerRegistrationUpdated':self.registrations=p.get('registrations',[])
                if kind=='Runtime.bindingCalled' and p.get('name')=='t011PagehideReport':
                    payload=json.loads(p['payload'])
                    if payload.get('pagehideSeen') and self.pagehide is None:self.pagehide=payload
                    if payload.get('lifecycleEvent'):
                        events=getattr(self,'lifecycleEvents',[])
                        if len(events)>=32:raise RuntimeError('bounded lifecycle observer overflow')
                        assert payload['lifecycleEvent'] in ['pagehide','beforeunload']
                        self.lifecycleEvents=events+[payload['lifecycleEvent']]
                    if payload.get('probe'):self.bindingProbe=True

                if self.record and kind=='Network.requestWillBeSent':
                    u=urllib.parse.urlsplit(p['request']['url'])
                    if u.scheme in ['http','https']:
                        origin=u.scheme+'://'+u.netloc
                        if origin!=self.origin:self.external+=1
                        key=p['request']['method']+' '+u.path
                        if len(self.requests)<128 or key in self.requests:self.requests[key]=self.requests.get(key,0)+1
                        else:raise RuntimeError('bounded request audit overflow')
                if msg.get('id')==seq:
                    if 'error' in msg:raise RuntimeError('CDP command failed: '+method)
                    return msg.get('result',{})
async def exercise(run,output,report):
    print('ANDROID_BOOT',flush=True);run.start_android();report['android']=run.report['android']
    # Google APIs disposable image permits adb root. Never use host firewall/sudo.
    run.adb_call('root');run.adb_call('wait-for-device',timeout=20)
    operator=secrets.token_urlsafe(32);env=os.environ.copy();env.update(GORILLA_IPC_JAVA=str(run.args.java),GORILLA_IPC_JAR=str(run.args.jar),GORILLA_IPC_MODE='lifecycle',GORILLA_PHONE_INPUT='1',GORILLA_MOBILE_LAB='1',GORILLA_MOBILE_OPERATOR=operator)
    player=subprocess.Popen([str(REPO/'Unity/Builds/FoundationLinux/GorillaEscape.x86_64'),'-screen-fullscreen','0','-screen-width','640','-screen-height','360','-logFile','-'],env=env,stdout=subprocess.PIPE,stderr=subprocess.DEVNULL,text=True)
    state={'port':None,'counts':{},'events':[],'pids':set()}
    def drain():
        for line in player.stdout:
            if line.startswith('PHONE_LAB_HTTP_READY '):state['port']=int(line.split()[-1])
            elif line.startswith('PHONE_INPUT_OBSERVED '):
                source=json.loads(line.split(' ',1)[1]).get('source','other');state['counts'][source]=state['counts'].get(source,0)+1
            elif line.startswith('{"component":"ipc-unity"'):
                e=json.loads(line)
                if e.get('event')=='PROCESS_STARTED':state['pids'].add(e['pid'])
                if len(state['events'])<100:state['events'].append(e)
    reader=threading.Thread(target=drain,daemon=True);reader.start();cdp=None;isolated=False
    try:
        wait(lambda:state['port'],20,player);base='http://127.0.0.1:'+str(state['port']);run.adb_call('reverse','tcp:'+str(state['port']),'tcp:'+str(state['port']))
        debugger=int(run.adb_call('forward','tcp:0','localabstract:chrome_devtools_remote').strip());original=await run.connect(debugger);cdp=ObservedCDP(original.connection,base)
        await cdp.call('Runtime.enable');await cdp.call('Runtime.addBinding',{'name':'t011PagehideReport'});await cdp.call('Network.enable');await cdp.call('ServiceWorker.enable')
        await cdp.call('Page.addScriptToEvaluateOnNewDocument',{'source':"""
          const Peer=RTCPeerConnection;window.RTCPeerConnection=class extends Peer{constructor(...args){super(...args);window.qaPeer=this;}};
          const send=RTCDataChannel.prototype.send;RTCDataChannel.prototype.send=function(data){if(typeof data==='string'){const n=JSON.parse(data);if(n.protocolVersion===1&&n.quality){n.quality.source='emulator';data=JSON.stringify(n);}}return send.call(this,data);};
          window.qaIntervals=new Set();const interval=window.setInterval.bind(window),nativeClearInterval=window.clearInterval.bind(window);window.setInterval=(...args)=>{const id=interval(...args);qaIntervals.add(id);return id;};window.clearInterval=id=>{qaIntervals.delete(id);return nativeClearInterval(id);};
          window.qaTimeouts=new Set();const nativeTimeout=window.setTimeout.bind(window),nativeClearTimeout=window.clearTimeout.bind(window);window.setTimeout=(fn,ms,...args)=>{const id=nativeTimeout(()=>{qaTimeouts.delete(id);fn(...args);},ms);qaTimeouts.add(id);return id;};window.clearTimeout=id=>{qaTimeouts.delete(id);return nativeClearTimeout(id);};
          window.qaListeners={};const add=window.addEventListener.bind(window),remove=window.removeEventListener.bind(window);
          window.addEventListener=(name,fn,...rest)=>{if(['devicemotion','deviceorientation'].includes(name)){(qaListeners[name]??=new Set()).add(fn);}return add(name,fn,...rest);};
          window.removeEventListener=(name,fn,...rest)=>{qaListeners[name]?.delete(fn);return remove(name,fn,...rest);};
          add('pagehide',()=>t011PagehideReport(JSON.stringify({lifecycleEvent:'pagehide'})));
          add('beforeunload',()=>t011PagehideReport(JSON.stringify({lifecycleEvent:'beforeunload'})));
        """})
        await cdp.call('Storage.clearDataForOrigin',{'origin':base,'storageTypes':'all'});await cdp.call('Network.clearBrowserCache')
        usage=await cdp.call('Storage.getUsageAndQuota',{'origin':base});assert usage['usage']==0
        report['cleanProfile']={'method':'new disposable AVD/Chrome; explicit origin storage clear + HTTP cache clear before first navigation','usageBefore':usage['usage'],'existingRegistrations':len([r for r in cdp.registrations if not r.get('isDeleted')]),'preloaded':False}
        assert report['cleanProfile']['existingRegistrations']==0
        host_ips=[x for x in subprocess.check_output(['hostname','-I'],text=True).split() if re.fullmatch(r'\d+\.\d+\.\d+\.\d+',x)]
        run.adb_call('shell','iptables','-N','GORILLA_T011');isolated=True
        for addr in ['127.0.0.0/8','10.0.2.2',*host_ips]:run.adb_call('shell','iptables','-A','GORILLA_T011','-d',addr,'-j','RETURN')
        run.adb_call('shell','iptables','-A','GORILLA_T011','-j','REJECT');run.adb_call('shell','iptables','-I','OUTPUT','1','-j','GORILLA_T011')
        # IPv6 WAN cannot escape an IPv4-only gate.
        run.adb_call('shell','ip6tables','-N','GORILLA_T011');run.adb_call('shell','ip6tables','-A','GORILLA_T011','-d','::1','-j','RETURN');run.adb_call('shell','ip6tables','-A','GORILLA_T011','-j','REJECT');run.adb_call('shell','ip6tables','-I','OUTPUT','1','-j','GORILLA_T011')
        run.adb_call('shell','svc','data','disable')
        async def wan():return await cdp.evaluate("(async()=>{try{await fetch('https://example.com/',{mode:'no-cors',cache:'no-store',signal:AbortSignal.timeout(3000)});return 'REACHABLE';}catch{return 'UNAVAILABLE';}})()")
        report['wanProbeBeforeFirstLoad']=await wan();assert report['wanProbeBeforeFirstLoad']=='UNAVAILABLE'
        # Direct IP TCP probe complements DNS-dependent browser probe.
        tcp=run.adb_call('shell','toybox nc -w 3 1.1.1.1 443 </dev/null >/dev/null 2>&1; echo $?',timeout=8).decode().strip();report['directPublicIpTcpExit']=tcp;assert tcp!='0', 'public IP TCP unexpectedly reachable'
        report['wanIsolation']={'method':'owned guest OUTPUT reject rules IPv4+IPv6, allow only loopback and PC endpoints; mobile data disabled','directPublicIpTcpExit':tcp,'hostNetworkModified':False}
        def admission():
            req=urllib.request.Request(base+'/mobile/admission',method='POST',headers={'X-Gorilla-Operator':operator})
            with urllib.request.urlopen(req,timeout=5) as r:u=urllib.parse.urlsplit(json.load(r)['url'])
            return urllib.parse.urlunsplit((u.scheme,u.netloc,'/',u.query,u.fragment))
        async def click(label):
            await cdp.until('Array.from(document.querySelectorAll("button,summary")).some(e=>e.textContent==='+json.dumps(label)+')',20)
            if label not in ['CONECTAR','ACTIVAR CONTROL']:
                await cdp.evaluate('Array.from(document.querySelectorAll("button,summary")).find(e=>e.textContent==='+json.dumps(label)+').click()');return
            await cdp.evaluate('Array.from(document.querySelectorAll("button")).find(e=>e.textContent==='+json.dumps(label)+').scrollIntoView({block:"center"})')
            for attempt in range(8):
                await cdp.evaluate('Array.from(document.querySelectorAll("button")).find(e=>e.textContent==='+json.dumps(label)+').scrollIntoView({block:"center"})')
                await asyncio.sleep(.2)
                raw=run.adb_call('exec-out','uiautomator','dump','/dev/tty',timeout=20).decode(errors='replace');tree=ET.fromstring(raw[raw.find('<?xml'):raw.rfind('</hierarchy>')+12])
                nodes=[n for n in tree.iter('node') if n.get('text')==label or n.get('content-desc')==label]
                for n in nodes:
                    b=list(map(int,re.findall(r'\d+',n.get('bounds',''))))
                    if len(b)==4 and b[2]>b[0] and b[3]>b[1]:run.adb_call('shell','input','tap',str((b[0]+b[2])//2),str((b[1]+b[3])//2));return
                await asyncio.sleep(.3)
            raise Blocked('UI tree control absent: '+label)
        async def diag():
            await click('Copiar diagnóstico');return await cdp.until('document.querySelector("pre") && JSON.parse(document.querySelector("pre").textContent)',5)
        async def fresh():
            for i in range(20):
                run.adb_call('emu','sensor','set','acceleration',['1:2:9.81','3:1:9.81','0:0:9.81'][i%3]);run.adb_call('emu','sensor','set','gyroscope','0.1:0.2:0.3');await asyncio.sleep(.3)
                if await cdp.evaluate('document.querySelector("h2")?.textContent==="Sensores listos"'):return
            raise RuntimeError('real virtual sensor readiness absent')
        async def begin():
            await cdp.navigate('about:blank');await cdp.navigate(admission());await click('CONECTAR');await click('ACTIVAR CONTROL');await fresh()
        report['stage']='FIRST_LOAD';print('FIRST_LOAD',flush=True);cdp.record=True;await begin();d=await diag();assert d['controller']['state']=='INPUT_READY';report['firstLoad']={'status':'PASS','secureContext':await cdp.evaluate('isSecureContext'),'network':d['controller']['network'],'source':'emulator','wan':'UNAVAILABLE','cacheBefore':0}
        sw=(REPO/'PWA/dist/sw.js').read_text();paths=json.loads(re.search(r'const URLS = (.*);',sw).group(1))
        report['assets']=await cdp.evaluate('(async()=>{const paths='+json.dumps(paths)+';return await Promise.all(paths.map(async path=>({path,status:(await fetch(path)).status})));})()');assert all(x['status']==200 for x in report['assets'])
        report['localHealth']=await cdp.evaluate("(async()=>({status:(await fetch('/api/health',{cache:'no-store'})).status}))()");assert report['localHealth']['status']==200
        await cdp.until('navigator.serviceWorker.controller!==null',20)
        cache=await cdp.evaluate("(async()=>{await navigator.serviceWorker.ready;return await Promise.all((await caches.keys()).map(async name=>({name,paths:(await (await caches.open(name)).keys()).map(r=>new URL(r.url).pathname)})));})()")
        assert len(cache)==1 and re.fullmatch(r'gorilla-shell-[a-f0-9]{16}',cache[0]['name']);assert set(cache[0]['paths'])==set(paths);report['cache']=cache;report['models']='NONE'
        # Actual pagehide from a running controller, before next independent admission.
        report['stage']='PAGEHIDE';print('PAGEHIDE',flush=True)
        await click('Medición técnica de sensores');await click('Medir movimiento suave · 25 s')
        await cdp.evaluate("t011PagehideReport(JSON.stringify({probe:true}));addEventListener('pagehide',()=>{const payload=JSON.stringify({pagehideSeen:true,motionListeners:qaListeners.devicemotion.size,orientationListeners:qaListeners.deviceorientation.size,intervals:qaIntervals.size,timeouts:qaTimeouts.size,connection:qaPeer.connectionState});sessionStorage.setItem('t011Pagehide',payload);t011PagehideReport(payload);},{once:true})")
        report['bindingProbe']=getattr(cdp,'bindingProbe',False);assert report['bindingProbe']
        # Same-origin real navigation retains the debugger target while destroying the controller document.
        await cdp.evaluate('location.assign('+json.dumps(base+'/?view=health')+')')
        await cdp.until('document.querySelector("h1")?.textContent.includes("Comprueba tu conexión")',15)
        for _ in range(10):
            if cdp.pagehide is not None:break
            await cdp.evaluate('document.readyState');await asyncio.sleep(.1)
        handoff=await cdp.evaluate("JSON.parse(sessionStorage.getItem('t011Pagehide')||'null')")
        report['lifecycleEvents']=getattr(cdp,'lifecycleEvents',[]);report['pagehideCDPBinding']=cdp.pagehide;report['pagehide']=handoff
        report['pagehideObserver']='one bounded aggregate in disposable QA sessionStorage, written during pagehide before context destruction; not product storage'
        assert report['pagehide'] and report['pagehide']['pagehideSeen'] and all(report['pagehide'][k]==0 for k in ['motionListeners','orientationListeners','intervals','timeouts']) and report['pagehide']['connection']=='closed'
        before=state['counts'].get('emulator',0);await asyncio.sleep(.5);after=state['counts'].get('emulator',0)
        report['pagehide']['externalUnityInputDeltaAfterDrain']=after-before;assert after==before
        report['pagehide']['status']='PASS'

        # Fresh one-use admission for second load; cannot reuse first admission.
        report['stage']='SECOND_LOAD';print('SECOND_LOAD',flush=True);await begin();d=await diag();report['secondLoad']={'status':'PASS','controller':d['controller']['state'],'SWControlled':await cdp.evaluate('navigator.serviceWorker.controller!==null')}
        report['stage']='SHORT_SUSPENSION';print('SHORT_SUSPENSION',flush=True);await click('Medición técnica de sensores');await click('Medir reposo · 15 s')
        run.adb_call('shell','input','keyevent','3');await cdp.until('document.visibilityState==="hidden"',5)
        hidden1=await diag();await asyncio.sleep(.8);hidden2=await diag()
        assert hidden1['controller']['state']=='CONTROL_SUSPENDED';assert hidden2['transport']['motionSubmitted']==hidden1['transport']['motionSubmitted'];assert hidden2['measurement']['status']=='INTERRUPTED'
        assert await cdp.evaluate('qaListeners.devicemotion.size===0&&qaListeners.deviceorientation.size===0')
        report['shortSuspension']={'status':'PASS','state':hidden2['controller']['state'],'hiddenMotionSubmittedDelta':hidden2['transport']['motionSubmitted']-hidden1['transport']['motionSubmitted'],'measurement':hidden2['measurement']['status'],'sensorsDetached':True}
        run.adb_call('shell','am','start','-a','android.intent.action.MAIN','-c','android.intent.category.LAUNCHER','-p','com.android.chrome')
        await cdp.until('document.visibilityState==="visible"',8);await fresh();d=await diag();assert d['controller']['state']=='INPUT_READY';report['resume']={'status':'PASS','state':d['controller']['state'],'freshVirtualCallbacks':True}
        assert await cdp.evaluate('qaListeners.devicemotion.size===1&&qaListeners.deviceorientation.size===1');report['duplicateListeners']='PASS — one per channel after real HOME/return; repeated epochs covered by unit test'
        report['stage']='LONG_SUSPENSION';print('LONG_SUSPENSION',flush=True)
        run.adb_call('shell','input','keyevent','3');await cdp.until('document.visibilityState==="hidden"',5);await asyncio.sleep(3)
        await cdp.evaluate('qaPeer.close()') # Actual owned RTC loss, no injected client state or reconnect.
        run.adb_call('shell','am','start','-a','android.intent.action.MAIN','-c','android.intent.category.LAUNCHER','-p','com.android.chrome');await cdp.until('document.querySelector("h2")?.textContent==="Se perdió la conexión"',10)
        d=await diag();assert d['controller']['state']=='DISCONNECTED';assert d['sensors']['state']=='STOPPED'
        report['longSuspension']={'status':'PASS','method':'real HOME, >=3s hidden, intentionally close owned RTCPeerConnection, return; deterministic transport-loss case, not physical OS throttling','state':d['controller']['state'],'capture':d['sensors']['state'],'recovery':'fresh QR; no automatic reconnect'}
        report['finalResources']=await cdp.evaluate('({motionListeners:qaListeners.devicemotion.size,orientationListeners:qaListeners.deviceorientation.size,intervals:qaIntervals.size,timeouts:qaTimeouts.size,connection:qaPeer.connectionState})')
        assert all(report['finalResources'][k]==0 for k in ['motionListeners','orientationListeners','intervals','timeouts']) and report['finalResources']['connection']=='closed'
        report['stage']='FINAL_PROBES'
        cdp.record=False;report['wanProbeAfterLifecycle']=await wan();assert report['wanProbeAfterLifecycle']=='UNAVAILABLE';cdp.record=True
        await cdp.navigate('about:blank')
        report['runtimeRequests']={'externalOrigins':cdp.external,'local':cdp.requests,'excludedProbes':'explicit WAN probes only; not product traffic'};assert cdp.external==0
        report['unityObservedByDeclaredSource']=state['counts'];assert state['counts'].get('emulator',0)>0 and state['counts'].get('physical',0)==0
        report['gate']='PASS'
    finally:
        if isolated:
            for command in ['iptables','ip6tables']:
                try:
                    run.adb_call('shell',command,'-D','OUTPUT','-j','GORILLA_T011');run.adb_call('shell',command,'-F','GORILLA_T011');run.adb_call('shell',command,'-X','GORILLA_T011')
                except Exception:report['isolationCleanupError']=True
            try:run.adb_call('shell','svc','data','enable')
            except Exception:report['isolationCleanupError']=True
        report['guestNetworkRestored']=not report.get('isolationCleanupError',False)
        if cdp:
            try:await cdp.navigate('about:blank');await cdp.connection.close()
            except Exception:pass
        if player.poll() is None:
            try:X11().close(player.pid);player.wait(timeout=15)
            except Exception:player.terminate();player.wait(timeout=10);report['forcedPlayer']=True
        reader.join(timeout=2);report['playerExit']=player.returncode;report['javaCleanup']=[{k:e.get(k) for k in ['state','exitCode','cleanupComplete','forced']} for e in state['events'] if e.get('event')=='STOPPED'];report['ownedJavaResiduals']=sum(Path('/proc',str(pid)).exists() for pid in state['pids'])
        if player.returncode!=0 or report['ownedJavaResiduals'] or report.get('forcedPlayer') or report.get('isolationCleanupError'):report['gate']='FAIL'
def main():
    output=Path(sys.argv[1]).resolve();output.mkdir(parents=True,exist_ok=False)
    report={'scope':'T011 LAB/EMULATOR; ADB reverse localhost != physical LAN/TLS validation','source':'emulator','physical':'NOT RUN; deferred T019/DEC-022','gate':'FAIL'}
    with tempfile.TemporaryDirectory(prefix='gorilla-t011-') as temp:
        private=Path(temp);raw=private/'raw';raw.mkdir();state=private/'state';state.mkdir();args=SimpleNamespace(sdk=Path.home()/'Android/Sdk',java=Path('/usr/lib/jvm/java-21-openjdk-amd64/bin/java'),jar=REPO/'Server/target/local-server-0.1.0-SNAPSHOT.jar',output=raw);run=OfflineRun(args,state)
        try:asyncio.run(exercise(run,output,report))
        except Blocked as e:report.update(gate='BLOCKED',reason=str(e))
        except Exception as e:
            import traceback
            last=traceback.extract_tb(e.__traceback__)[-1];report.update(gate='FAIL',reason=type(e).__name__,failureAt=Path(last.filename).name+':'+str(last.lineno),detail=str(e)[:100] if isinstance(e,(AssertionError,CommandFailure)) else None)
        finally:run.cleanup();report['androidCleanup']=run.report['cleanup']
        if report['androidCleanup']['ownedResiduals']:report['gate']='FAIL'
    report['sourceHashes']={str(p.relative_to(REPO)):hashlib.sha256(p.read_bytes()).hexdigest() for p in [Path(__file__),REPO/'PWA/src/input/sensors/capture.js',REPO/'PWA/src/controller/session.js',REPO/'PWA/src/controller/presentation.js',REPO/'PWA/dist/sw.js',args.jar]}
    (output/'results.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report),flush=True);return 0 if report['gate']=='PASS' else 2 if report['gate']=='BLOCKED' else 1
if __name__=='__main__':sys.exit(main())
