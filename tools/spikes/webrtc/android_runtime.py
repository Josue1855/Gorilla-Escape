#!/usr/bin/env python3
"""Optional Android variant; existing AVD helpers, real Player/Java, no TLS bypass/cloud."""
import asyncio,json,os,secrets,subprocess,tempfile,threading,time,sys,urllib.request,hashlib
from pathlib import Path
from types import SimpleNamespace
REPO=Path(__file__).resolve().parents[3];sys.path.insert(0,str(REPO/'tools'))
from validate_android_3a import Run,wait,Blocked
from validate_ipc_2b_player import X11
output=Path(sys.argv[1]).resolve()
if output.exists():raise SystemExit('Preserve prior evidence')
output.mkdir(parents=True)
report={'scope':'Android emulated; localhost signaling via ADB reverse, real host RTC UDP; no physical/TLS claim','gate':'FAIL','physical':'DEFERRED'}
with tempfile.TemporaryDirectory(prefix='gorilla-mobile-android-') as temporary:
 private=Path(temporary);raw=private/'raw';raw.mkdir();state=private/'state';state.mkdir()
 run=Run(SimpleNamespace(sdk=Path.home()/'Android/Sdk',java=Path('/usr/lib/jvm/java-21-openjdk-amd64/bin/java'),jar=REPO/'Server/target/local-server-0.1.0-SNAPSHOT.jar',output=raw),state)
 player=None;observations=[];events=[];ready={};cdp=None
 async def test():
  global player,cdp
  run.start_android();report['android']=run.report['android']
  # Serialize with the earlier explicit sustained regression; no lock bypass.
  marker=REPO/'Unity/Logs/MobileInputRegression2C/measurement-results.json'
  wait(lambda:marker.exists(),300)
  operator=secrets.token_urlsafe(32);env=os.environ.copy();env.update(GORILLA_IPC_JAVA=str(run.args.java),GORILLA_IPC_JAR=str(run.args.jar),GORILLA_IPC_MODE='lifecycle',GORILLA_PHONE_INPUT='1',GORILLA_MOBILE_OPERATOR=operator)
  player=subprocess.Popen([str(REPO/'Unity/Builds/FoundationLinux/GorillaEscape.x86_64'),'-screen-fullscreen','0','-screen-width','640','-screen-height','360','-logFile','-'],env=env,stdout=subprocess.PIPE,stderr=subprocess.DEVNULL,text=True)
  def consume():
   for line in player.stdout:
    if line.startswith('PHONE_LAB_HTTP_READY '):ready['port']=int(line.split()[-1])
    elif line.startswith('PHONE_INPUT_OBSERVED ') and len(observations)<1000:observations.append(json.loads(line.split(' ',1)[1]))
    elif line.startswith('{"component":"ipc-unity"') and len(events)<100:events.append(json.loads(line))
  threading.Thread(target=consume,daemon=True).start();wait(lambda:ready.get('port'),20,player)
  hp=ready['port'];base=f'http://127.0.0.1:{hp}'
  request=urllib.request.Request(base+'/mobile/admission',method='POST',headers={'X-Gorilla-Operator':operator})
  with urllib.request.urlopen(request,timeout=5) as response:admission=json.load(response)
  decoded=subprocess.run([str(run.args.java),'--class-path',str(Path.home()/'.m2/repository/com/google/zxing/core/3.5.3/core-3.5.3.jar'),str(REPO/'tools/spikes/webrtc/DecodeQr.java')],input=__import__('base64').b64decode(admission['qrPng']),capture_output=True,timeout=10)
  assert decoded.returncode==0 and decoded.stdout.decode()==admission['url'];report['qr']='PASS'
  run.adb_call('reverse',f'tcp:{hp}',f'tcp:{hp}');debugger=int(run.adb_call('forward','tcp:0','localabstract:chrome_devtools_remote').strip());cdp=await run.connect(debugger)
  await cdp.navigate(base+'/mobile-lab/index.html');await cdp.until("typeof MobileClient==='function'")
  report['features']=await cdp.evaluate("({secureContext:isSecureContext,rtc:typeof RTCPeerConnection==='function',motion:typeof DeviceMotionEvent==='function'})")
  connect=await cdp.evaluate("(async()=>{window.client=new MobileClient({source:'emulator'});try{const identity=await client.connect("+json.dumps(admission['url'])+");return {status:'CONNECTED',playerId:identity.playerId};}catch{return {status:'BLOCKED',clientState:client.state,reason:'RTC channel did not reach OPEN before deadline'};}})()")
  report['connection']=connect
  if connect['status']!='CONNECTED':report['gate']='BLOCKED';return
  await cdp.evaluate("(async()=>{window.capture=new SensorCapture();await capture.start();window.detach=attachCapture(capture,client);return capture.state;})()")
  # Virtual hardware console input, not a physical accelerometer/gyroscope.
  controls=[]
  for values in ['1:2:9.81','3:4:9.81','-2:1:9.81']:
   try:run.adb_call('emu','sensor','set','acceleration',values);controls.append('PASS')
   except Exception:controls.append('BLOCKED')
   await asyncio.sleep(.15)
  report['virtualAccelerationControls']=controls
  try:run.adb_call('emu','sensor','set','gyroscope','0.1:0.2:0.3');report['virtualGyroscopeControl']='PASS'
  except Exception:report['virtualGyroscopeControl']='BLOCKED'
  await cdp.until("client.metrics.received>=5",deadline=8)
  report['capture']=await cdp.evaluate("({state:capture.state,motionEvents:capture.snapshot().channels.motion.events,orientationEvents:capture.snapshot().channels.orientation.events,sent:client.metrics.sent,received:client.metrics.received,errors:client.metrics.errors})")
  await cdp.evaluate("detach();capture.stop();document.body.textContent='Gorilla Escape — Android Emulator LAB: sensores virtuales → WebRTC → Java → Unity';")
  await run.screenshot(cdp,'android-runtime');(output/'android-runtime.png').write_bytes((raw/'android-runtime.png').read_bytes())
  await cdp.evaluate('client.close()');wait(lambda:len(observations)>0,5)
  report['unityObservationCount']=len(observations);report['unitySources']=list(set(x['source'] for x in observations));report['gate']='PASS' if report['capture']['errors']==0 and 'emulator' in report['unitySources'] else 'FAIL'
 try:asyncio.run(test())
 except (Blocked,ImportError) as error:report['gate']='BLOCKED';report['reason']=type(error).__name__
 except Exception as error:report['gate']='FAIL';report['reason']=type(error).__name__
 finally:
  if cdp:
   try:asyncio.run(cdp.connection.close())
   except Exception:pass
  if player and player.poll() is None:
   try:X11().close(player.pid);player.wait(timeout=15)
   except Exception:player.terminate();player.wait(timeout=10);report['forcedPlayer']=True;report['gate']='FAIL'
  report['playerExit']=player.returncode if player else None
  report['javaCleanup']=[{k:e.get(k) for k in ['state','exitCode','cleanupComplete','forced']} for e in events if e['event']=='STOPPED']
  report['ownedJavaResiduals']=sum(Path('/proc',str(e['pid'])).exists() for e in events if e['event']=='PROCESS_STARTED')
  run.cleanup();report['androidCleanup']=run.report['cleanup']
  if report['ownedJavaResiduals'] or run.report['cleanup']['ownedResiduals']:report['gate']='FAIL'
report['sourceHashes']={str(p.relative_to(REPO)):hashlib.sha256(p.read_bytes()).hexdigest() for p in [Path(__file__),REPO/'tools/validate_android_3a.py',REPO/'PWA/src/input/mobileClient.js']}
(output/'results.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({'gate':report['gate'],'connection':report.get('connection'),'cleanup':report['ownedJavaResiduals']}))
raise SystemExit(0 if report['gate']=='PASS' else 2 if report['gate']=='BLOCKED' else 1)
