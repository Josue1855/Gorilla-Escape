#!/usr/bin/env python3
"""T010 auxiliary QA: disposable Android AVD, virtual sensors, actual PWA/Java/Unity.
No TLS bypass: localhost via ADB reverse is explicitly not physical LAN/public trust.
No product changes or injected sensor events. Wire quality.source is labeled emulator
by test-only CDP instrumentation; sensor payloads and timing are untouched.
"""
import asyncio, base64, hashlib, json, os, re, secrets, subprocess, sys, tempfile, threading, time
import urllib.request, urllib.parse, xml.etree.ElementTree as ET
from pathlib import Path
from types import SimpleNamespace
REPO = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(REPO / 'tools'))
from validate_android_3a import Run, wait, Blocked, CommandFailure
from validate_ipc_2b_player import X11

async def exercise(run, output, report):
    report['stage']='ANDROID_BOOT'; print(json.dumps({'stage':report['stage']}),flush=True)
    run.start_android()
    report['android'] = run.report['android']
    operator = secrets.token_urlsafe(32)
    env = os.environ.copy()
    env.update(GORILLA_IPC_JAVA=str(run.args.java), GORILLA_IPC_JAR=str(run.args.jar),
               GORILLA_IPC_MODE='lifecycle', GORILLA_PHONE_INPUT='1',
               GORILLA_MOBILE_LAB='1', GORILLA_MOBILE_OPERATOR=operator)
    process = subprocess.Popen([str(REPO/'Unity/Builds/FoundationLinux/GorillaEscape.x86_64'),
                                '-screen-fullscreen','0','-screen-width','640','-screen-height','360','-logFile','-'],
                               env=env, stdout=subprocess.PIPE, stderr=subprocess.DEVNULL, text=True)
    state = {'port':None, 'counts':{}, 'events':[], 'pids':set()}
    def consume():
        for line in process.stdout:
            if line.startswith('PHONE_LAB_HTTP_READY '): state['port'] = int(line.split()[-1])
            elif line.startswith('PHONE_INPUT_OBSERVED '):
                data = json.loads(line.split(' ',1)[1]); source=data.get('source','other')
                state['counts'][source] = state['counts'].get(source,0)+1
            elif line.startswith('{"component":"ipc-unity"'):
                event=json.loads(line)
                if event.get('event')=='PROCESS_STARTED': state['pids'].add(event['pid'])
                if len(state['events'])<100: state['events'].append(event)
    reader=threading.Thread(target=consume,daemon=True);reader.start()
    cdp=None
    try:
        report['stage']='JAVA_READY'; print(json.dumps({'stage':report['stage']}),flush=True)
        wait(lambda:state['port'],20,process)
        base='http://127.0.0.1:'+str(state['port'])
        run.adb_call('reverse','tcp:'+str(state['port']),'tcp:'+str(state['port']))
        debugger=int(run.adb_call('forward','tcp:0','localabstract:chrome_devtools_remote').strip())
        cdp=await run.connect(debugger)
        await cdp.call('Page.addScriptToEvaluateOnNewDocument', {'source':"""
          const send=RTCDataChannel.prototype.send;
          RTCDataChannel.prototype.send=function(data){
            if(typeof data==='string'){
              const message=JSON.parse(data);
              if(message.protocolVersion===1 && message.quality){
                message.quality.source='emulator'; data=JSON.stringify(message);
              }
            }
            return send.call(this,data);
          };
        """})
        def admission():
            req=urllib.request.Request(base+'/mobile/admission',method='POST',headers={'X-Gorilla-Operator':operator})
            with urllib.request.urlopen(req,timeout=5) as response: raw=json.load(response)['url']
            parsed=urllib.parse.urlsplit(raw)
            # Lab admission defaults to the old harness path; use actual React root.
            return urllib.parse.urlunsplit((parsed.scheme,parsed.netloc,'/',parsed.query,parsed.fragment))
        async def click(label):
            # Android taps derive solely from native UI-tree bounds.
            await cdp.until('Array.from(document.querySelectorAll("button,summary")).some(e=>e.textContent==='+json.dumps(label)+')',20)
            if label not in ['CONECTAR','ACTIVAR CONTROL']:
                # Browser automation for diagnostic/measurement controls; no permission bypass.
                await cdp.evaluate('Array.from(document.querySelectorAll("button,summary")).find(e=>e.textContent==='+json.dumps(label)+').click()')
                report.setdefault('uiActions',[]).append({'label':label,'method':'DOM click; diagnostic/measurement only'})
                return
            await cdp.evaluate('Array.from(document.querySelectorAll("button,summary")).find(e=>e.textContent==='+json.dumps(label)+').scrollIntoView({block:"center"})')
            await asyncio.sleep(.3)
            raw=run.adb_call('exec-out','uiautomator','dump','/dev/tty',timeout=20).decode(errors='replace')
            start,end=raw.find('<?xml'),raw.rfind('</hierarchy>')
            tree=ET.fromstring(raw[start:end+len('</hierarchy>')])
            def visible(n):
                b=list(map(int,re.findall(r'\d+',n.get('bounds',''))))
                return len(b)==4 and b[2]>b[0] and b[3]>b[1]
            node=next((n for n in tree.iter('node') if (n.get('text')==label or n.get('content-desc')==label) and visible(n)),None)
            if node is None:
                await cdp.evaluate('Array.from(document.querySelectorAll("button,summary")).find(e=>e.textContent==='+json.dumps(label)+').scrollIntoView({block:"center"})')
                raw=run.adb_call('exec-out','uiautomator','dump','/dev/tty',timeout=20).decode(errors='replace')
                tree=ET.fromstring(raw[raw.find('<?xml'):raw.rfind('</hierarchy>')+len('</hierarchy>')])
                node=next((n for n in tree.iter('node') if (n.get('text')==label or n.get('content-desc')==label) and visible(n)),None)
            if node is None: raise Blocked('UI target absent: '+label)
            x1,y1,x2,y2=map(int,re.findall(r'\d+',node.get('bounds','')))
            report.setdefault('uiActions',[]).append({'label':label,'bounds':[x1,y1,x2,y2]})
            run.adb_call('shell','input','tap',str((x1+x2)//2),str((y1+y2)//2))
        async def diagnostic():
            await click('Copiar diagnóstico')
            await asyncio.sleep(.1)
            return await cdp.until('document.querySelector("pre") && JSON.parse(document.querySelector("pre").textContent)',5)
        report['stage']='PWA_NAVIGATION'; print(json.dumps({'stage':report['stage']}),flush=True)
        await cdp.navigate(admission())
        report['features']=await cdp.evaluate('({secureContext:isSecureContext,motion:typeof DeviceMotionEvent==="function",orientation:typeof DeviceOrientationEvent==="function",rtc:typeof RTCPeerConnection==="function"})')
        report['stage']='CONNECT'; print(json.dumps({'stage':report['stage']}),flush=True)
        await click('CONECTAR')
        report['stage']='ACTIVATE'; print(json.dumps({'stage':report['stage']}),flush=True)
        await click('ACTIVAR CONTROL')
        await cdp.evaluate('''window.qaSignal={motion:0,orientation:0,trustedMotion:0,trustedOrientation:0,accelerationChanged:false,rotationChanged:false,orientationChanged:false,orientationRangeRejected:{alpha:0,beta:0,gamma:0},alphaViolationCount:0,alphaViolationMin:null,alphaViolationMax:null,firstAlphaViolations:[]};
          let firstAcceleration,firstRotation,firstOrientation;
          addEventListener('devicemotion',e=>{qaSignal.motion++;if(e.isTrusted)qaSignal.trustedMotion++;
            const a=JSON.stringify([e.acceleration?.x,e.acceleration?.y,e.acceleration?.z]),r=JSON.stringify([e.rotationRate?.alpha,e.rotationRate?.beta,e.rotationRate?.gamma]);
            if(firstAcceleration===undefined)firstAcceleration=a;else if(a!==firstAcceleration)qaSignal.accelerationChanged=true;
            if(firstRotation===undefined)firstRotation=r;else if(r!==firstRotation)qaSignal.rotationChanged=true;});
          addEventListener('deviceorientation',e=>{qaSignal.orientation++;if(e.isTrusted)qaSignal.trustedOrientation++;
            const o=JSON.stringify([e.alpha,e.beta,e.gamma]);
            for(const [axis,low,high] of [['alpha',0,360],['beta',-180,180],['gamma',-90,90]]){
              const v=e[axis];if(Number.isFinite(v)&&(v<low||v>=high)){
                qaSignal.orientationRangeRejected[axis]++;
                if(axis==='alpha'){
                  qaSignal.alphaViolationCount++;
                  qaSignal.alphaViolationMin=qaSignal.alphaViolationMin===null?v:Math.min(qaSignal.alphaViolationMin,v);
                  qaSignal.alphaViolationMax=qaSignal.alphaViolationMax===null?v:Math.max(qaSignal.alphaViolationMax,v);
                  if(qaSignal.firstAlphaViolations.length<8)qaSignal.firstAlphaViolations.push(v);
                }
              }}
            if(firstOrientation===undefined)firstOrientation=o;else if(o!==firstOrientation)qaSignal.orientationChanged=true;});''')
        report['virtualSensors']=run.adb_call('emu','sensor','status').decode().splitlines()[:-1]
        run.adb_call('emu','sensor','set','acceleration','0:0:9.81')
        run.adb_call('emu','sensor','set','gyroscope','0:0:0')
        report['stage']='INPUT_READY'; print(json.dumps({'stage':report['stage']}),flush=True)
        await cdp.until('document.querySelector("h2")?.textContent==="Sensores listos"',15)
        initial=await diagnostic()
        assert initial['controller']['state']=='INPUT_READY'
        assert initial['sensors']['captureRevision']=='t010-2-coarsened-clock'
        assert initial['emissionPolicy']['maximumHz']==50
        report['preflight']='PASS'
        await click('Medición técnica de sensores')
        modes=[('rest','Medir reposo · 15 s',15.5),('gentle','Medir movimiento suave · 25 s',25)]
        if '--gentle-only' in sys.argv: modes=modes[1:]
        for mode,label,seconds in modes:
            report['stage']='MEASURE_'+mode.upper(); print(json.dumps({'stage':report['stage']}),flush=True)
            await cdp.until('document.querySelector("h2")?.textContent==="Sensores listos"',10)
            await click(label)
            await cdp.until('document.body.textContent.includes("Medición en curso")',5)
            begin=time.monotonic(); controls=[]
            while time.monotonic()-begin<seconds+0.5:
                if mode=='gentle':
                    index=len(controls)%3
                    acceleration=['1:2:9.81','3:4:9.81','-2:1:9.81'][index]
                    for sensor,value in [('acceleration',acceleration),('gyroscope',['0.1:0.2:0.3','-0.2:0.3:0.1','0:0:0'][index])]:
                        run.adb_call('emu','sensor','set',sensor,value)
                    controls.append(index)
                await asyncio.sleep(.5)
            await cdp.until('document.body.textContent.includes("Medición terminada")',8)
            diag=await diagnostic(); measurement=diag['measurement']
            report['virtualSignalVariation']=await cdp.evaluate('qaSignal')
            # Persist bounded diagnostics before optional renderer screenshot work.
            (output/'alpha-diagnostic.json').write_text(json.dumps({
                'scope':'Android virtual sensor observer; cumulative since activation, no physical claim',
                'maximumStoredViolations':8,
                **{key:report['virtualSignalVariation'][key] for key in
                   ['alphaViolationCount','alphaViolationMin','alphaViolationMax','firstAlphaViolations','orientationRangeRejected']}
            },indent=2)+'\n')
            assert measurement['mode']==mode and measurement['status']=='COMPLETE'
            assert measurement['capture']['captureRevision']=='t010-2-coarsened-clock'
            assert measurement['emissionPolicy']['maximumHz']==50
            # Preserve rest before starting gentle. No token, SDP, IP, raw vectors or identity.
            safe={'scope':'Android emulator virtual sensors — NOT physical QA', 'mode':mode,
                  'controller':diag['controller'],'measurement':measurement,
                  'virtualControlUpdates':len(controls)}
            channels=measurement['capture']['channels']
            success=all(channels[c]['validSamples']>3 and channels[c]['invalidFields']==0 for c in ['motion','orientation'])
            success=success and measurement['emission']['actualMeasuredHz']<=51 and measurement['inputState']=='INPUT_READY'
            success=success and measurement['transportDelta']['motionErrors']==0 and measurement['transportDelta']['errors']==0
            safe['gate']='PASS' if success else 'FAIL'
            (output/('android-'+mode+'.json')).write_text(json.dumps(safe,indent=2)+'\n')
            report['runs'].append({'mode':mode,'gate':safe['gate'],'file':'android-'+mode+'.json'})
            # Page screenshot omits Chrome URL bar; diagnostics contain only sanitized fields.
            await cdp.evaluate('document.querySelectorAll("details").forEach(e=>e.open=false);window.scrollTo(0,0)')
            if '--skip-screenshot' not in sys.argv:
                await run.screenshot(cdp,'android-'+mode)
                (output/('android-'+mode+'.png')).write_bytes((run.args.output/('android-'+mode+'.png')).read_bytes())
            if mode=='rest': await click('Medición técnica de sensores')
        report['virtualSignalVariation']=await cdp.evaluate('qaSignal')
        report['unityObservedByDeclaredSource']=dict(state['counts'])
        assert state['counts'].get('emulator',0)>0 and state['counts'].get('physical',0)==0
        report['gate']='PASS' if all(x['gate']=='PASS' for x in report['runs']) else 'FAIL'
    except Exception:
        if cdp:
            try:
                report['failurePage']=await cdp.evaluate('({heading:document.querySelector("h2")?.textContent,buttons:Array.from(document.querySelectorAll("button")).map(e=>e.textContent)})')
                await run.screenshot(cdp,'failure-page')
                (output/'failure-page.png').write_bytes((run.args.output/'failure-page.png').read_bytes())
                await click('Copiar diagnóstico')
                diag=await cdp.until('document.querySelector("pre") && JSON.parse(document.querySelector("pre").textContent)',5)
                diag.pop('candidates',None)
                (output/'failure-diagnostic.json').write_text(json.dumps(diag,indent=2)+'\n')
                await cdp.evaluate('document.querySelectorAll("details").forEach(e=>e.open=false);window.scrollTo(0,0)')
                await run.screenshot(cdp,'failure-page')
                (output/'failure-page.png').write_bytes((run.args.output/'failure-page.png').read_bytes())
            except Exception: pass
        raise
    finally:
        if cdp:
            try: await cdp.navigate('about:blank');await cdp.connection.close()
            except Exception: pass
        if process.poll() is None:
            try: X11().close(process.pid);process.wait(timeout=15)
            except Exception:
                process.terminate();process.wait(timeout=10);report['forcedPlayer']=True
        reader.join(timeout=2)
        report['playerExit']=process.returncode
        report['javaCleanup']=[{k:e.get(k) for k in ['state','exitCode','cleanupComplete','forced']} for e in state['events'] if e.get('event')=='STOPPED']
        report['ownedJavaResiduals']=sum(Path('/proc',str(pid)).exists() for pid in state['pids'])
        if process.returncode!=0 or report['ownedJavaResiduals'] or report.get('forcedPlayer'): report['gate']='FAIL'

def main():
    output=Path(sys.argv[1]).resolve();output.mkdir(parents=True,exist_ok=False)
    report={'scope':'T010 actual React shell → Android Chrome virtual sensor events → RTC → Java child → loopback IPC → Unity',
            'physical':'NOT RUN / pending; emulator does not replace iPhone rest/gentle or Android physical QA',
            'tls':'NOT VALIDATED: localhost secure context through ADB reverse; no TLS bypass or CA installation',
            'instrumentation':'CDP labels wire quality.source emulator only; passive observers read explicit axes; no sensor API/event/payload/time substitution',
            'runs':[],'gate':'FAIL'}
    with tempfile.TemporaryDirectory(prefix='gorilla-t010-android-') as temp:
        private=Path(temp);raw=private/'raw';raw.mkdir();state=private/'state';state.mkdir()
        args=SimpleNamespace(sdk=Path.home()/'Android/Sdk',java=Path('/usr/lib/jvm/java-21-openjdk-amd64/bin/java'),jar=REPO/'Server/target/local-server-0.1.0-SNAPSHOT.jar',output=raw)
        run=Run(args,state)
        try: asyncio.run(exercise(run,output,report))
        except Blocked as error: report.update(gate='BLOCKED',reason=str(error))
        except CommandFailure as error:
            report.update(gate='FAIL',reason='CommandFailure')
            if report.get('stage')=='ANDROID_BOOT': report['bootError']=str(error)
        except Exception as error: report.update(gate='FAIL',reason=type(error).__name__)
        finally:
            run.cleanup();report['androidCleanup']=run.report['cleanup']
            if run.report['cleanup']['ownedResiduals']: report['gate']='FAIL'
    paths=[Path(__file__),REPO/'PWA/src/input/sensors/capture.js',REPO/'PWA/src/controller/session.js',REPO/'PWA/src/input/mobileClient.js',args.jar]
    report['sourceHashes']={str(p.relative_to(REPO)):hashlib.sha256(p.read_bytes()).hexdigest() for p in paths}
    (output/'results.json').write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps({'gate':report['gate'],'runs':report['runs'],'reason':report.get('reason'),'ownedJavaResiduals':report.get('ownedJavaResiduals'),'androidResiduals':report['androidCleanup']['ownedResiduals']}))
    return 0 if report['gate']=='PASS' else 2 if report['gate']=='BLOCKED' else 1
if __name__=='__main__': sys.exit(main())
