#!/usr/bin/env python3
"""API36 emulator → actual PWA → RTC → Java → Unity onboarding. No physical/TLS claim.
Use an already booted dedicated emulator; this script owns only its Player and ADB mappings.
"""
import asyncio,base64,json,os,re,secrets,subprocess,sys,tempfile,threading,time,urllib.request,urllib.parse,xml.etree.ElementTree as ET
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'tools'))
from validate_android_3a import CDP,wait
import websockets
ADB=Path.home()/'Android/Sdk/platform-tools/adb'
SERIAL=os.environ.get('GORILLA_TEST_SERIAL','emulator-5554')
OUT=Path(sys.argv[1]);OUT.mkdir(parents=True,exist_ok=True)
PACKAGE=ROOT/'Unity/Builds/FoundationLinux'
report={'scope':'Android API36 virtual sensors; actual PWA/RTC/Java/Unity; localhost via ADB reverse. Not physical LAN/TLS/iPhone acceptance.','cases':{},'gate':'FAIL'}
def adb(*args):return subprocess.check_output([str(ADB),'-s',SERIAL,*map(str,args)],timeout=25)
def post(base,path,operator):
 request=urllib.request.Request(base+path,method='POST',headers={'X-Gorilla-Operator':operator})
 with urllib.request.urlopen(request,timeout=8) as response:return json.load(response)
async def tap(cdp,label):
 tap.last=label
 await cdp.call("Target.activateTarget",{"targetId":cdp.target_id})
 await cdp.call("Page.bringToFront")
 await cdp.until('Array.from(document.querySelectorAll("button,summary")).some(e=>e.textContent==='+json.dumps(label)+')',20)
 await cdp.evaluate('Array.from(document.querySelectorAll("button,summary")).find(e=>e.textContent==='+json.dumps(label)+').scrollIntoView({block:"center"})')
 for attempt in range(3):
  await asyncio.sleep(.4)
  raw=adb('exec-out','uiautomator','dump','/dev/tty').decode(errors='replace');start=raw.find('<?xml');end=raw.rfind('</hierarchy>')
  if start<0 or end<0:continue
  tree=ET.fromstring(raw[start:end+len('</hierarchy>')])
  for n in tree.iter('node'):
   if n.get('text','').casefold()==label.casefold() or n.get('content-desc','').casefold()==label.casefold():
    b=list(map(int,re.findall(r'\d+',n.get('bounds',''))))
    if len(b)==4 and b[2]>b[0] and b[3]>b[1]:adb('shell','input','tap',(b[0]+b[2])//2,(b[1]+b[3])//2);return
 # Chrome sometimes omits web descendants from the Android accessibility cache.
 # Fall back to Chrome's semantic accessibility tree, then dispatch a real touch.
 # Coordinates come from that observed node, never from a screenshot or constants.
 await cdp.call('Accessibility.enable')
 accessible=await cdp.call('Accessibility.getFullAXTree')
 node=next((n for n in accessible['nodes'] if n.get('role',{}).get('value')=='button' and str(n.get('name',{}).get('value','')).casefold()==label.casefold()),None)
 if node and node.get('backendDOMNodeId'):
  obj=await cdp.call('DOM.resolveNode',{'backendNodeId':node['backendDOMNodeId']})
  geometry=await cdp.call('Runtime.callFunctionOn',{'objectId':obj['object']['objectId'],'functionDeclaration':'function(){const r=this.getBoundingClientRect();return {x:r.x+r.width/2,y:r.y+r.height/2,width:r.width,height:r.height};}','returnByValue':True})
  rect=geometry['result']['value']
  if rect['width']>0 and rect['height']>0:
   await cdp.call('Input.dispatchTouchEvent',{'type':'touchStart','touchPoints':[{'x':rect['x'],'y':rect['y']}]})
   await cdp.call('Input.dispatchTouchEvent',{'type':'touchEnd','touchPoints':[]})
   tap.web_tree_fallback=True
   return
 tap.observed=[{'role':n.get('class'),'text':(n.get('text') or n.get('content-desc') or '')[:80],'bounds':n.get('bounds')} for n in tree.iter('node') if (n.get('class','').endswith('Button') or 'CONTROL' in (n.get('text') or n.get('content-desc') or ''))]
 raise RuntimeError('UI_TREE_TARGET_MISSING')
async def shot(cdp,name):
 # Page-only capture excludes the browser address bar/admission fragment.
 await cdp.evaluate('document.querySelectorAll("details").forEach(e=>e.open=false);window.scrollTo(0,0)')
 result=await cdp.call('Page.captureScreenshot',{'format':'png'})
 (OUT/(name+'.png')).write_bytes(base64.b64decode(result['data']))
async def run():
 cdp=None;player=None;state={'port':None,'events':[],'pids':[]};forward=None
 operator=secrets.token_urlsafe(32)
 with tempfile.TemporaryDirectory(prefix='gorilla-emulator-data-') as data:
  env=os.environ.copy();env.update(DISPLAY=':97',GORILLA_IPC_JAVA=str(PACKAGE/'Java/runtime/bin/java'),GORILLA_IPC_JAR=str(PACKAGE/'Java/server.jar'),GORILLA_PACKAGE_ROOT=str(PACKAGE),GORILLA_IPC_MODE='lifecycle',GORILLA_PHONE_INPUT='1',GORILLA_MOBILE_LAB='1',GORILLA_MOBILE_OPERATOR=operator,GORILLA_CONNECTION_MODE='CABIN',GORILLA_MAIN_MENU='1',GORILLA_QA_OVERLAY='1',GORILLA_QA_AUTOSTART_LOG_BALANCE='1',GORILLA_DATA_DIR=data)
  for key in ['GORILLA_MOBILE_CONFIG','GORILLA_LOCAL_TRUST_DIR','GORILLA_CAMERA_ENABLED','GORILLA_CAMERA_LAB','GORILLA_QA_AUTOSTART_TESTS','GORILLA_QA_AUTOSTART_PHONE']:env.pop(key,None)
  try:
   player=subprocess.Popen([str(PACKAGE/'GorillaEscape.x86_64'),'-screen-fullscreen','0','-screen-width','1280','-screen-height','900','-logFile','-'],env=env,stdout=subprocess.PIPE,stderr=subprocess.DEVNULL,text=True)
   def consume():
    for line in player.stdout:
     if line.startswith('QA_LAB_HTTP_READY '):state['port']=int(line.split()[-1])
     if line.startswith('{"component":"ipc-unity"'):
      event=json.loads(line)
      if event.get('event')=='PROCESS_STARTED':state['pids'].append(event['pid'])
      if event.get('event')=='STOPPED':state['events'].append({k:event.get(k) for k in ['event','cleanupComplete','forced','exitCode']})
     if line.startswith('QA_SURFACE_SUMMARY '):state['surface']=json.loads(line.split(' ',1)[1])
   threading.Thread(target=consume,daemon=True).start()
   await asyncio.to_thread(wait,lambda:state['port'],25,player)
   port=state['port'];base='http://127.0.0.1:'+str(port)
   adb('reverse','tcp:'+str(port),'tcp:'+str(port));forward=adb('forward','tcp:0','localabstract:chrome_devtools_remote').decode().strip()
   adb('shell','am','start','-a','android.intent.action.VIEW','-d','about:blank','-p','com.android.chrome')
   report['stage']='CHROME_DEBUGGER'
   await asyncio.sleep(1)
   with urllib.request.urlopen('http://127.0.0.1:'+forward+'/json/list') as response:target=next(t for t in json.load(response) if t['type']=='page')
   connection=await websockets.connect(target['webSocketDebuggerUrl'],max_size=4*1024*1024,ping_interval=None)
   cdp=CDP(connection);cdp.target_id=target["id"]
   # Only label provenance. Browser sensor values/timing/permissions remain unchanged.
   await cdp.call('Page.addScriptToEvaluateOnNewDocument',{'source':"const originalSend=RTCDataChannel.prototype.send;RTCDataChannel.prototype.send=function(data){if(typeof data==='string'){let n=JSON.parse(data);if(n.protocolVersion===1&&n.quality){n.quality.source='emulator';data=JSON.stringify(n);}}return originalSend.call(this,data);};"})
   report['stage']='ADMISSION'
   admission=post(base,'/mobile/admission',operator)['url'];u=urllib.parse.urlsplit(admission);url=urllib.parse.urlunsplit((u.scheme,u.netloc,'/',u.query,u.fragment))
   report['stage']='NAVIGATION'
   await cdp.navigate(url);await cdp.until('document.body.textContent.includes("CONECTAR")')
   report['stage']='CONNECT_BUTTON'
   await tap(cdp,'CONECTAR')
   await cdp.until('Boolean(document.querySelector(".profile-setup"))',15)
   await cdp.evaluate('document.querySelector(".profile-setup").open=true;document.querySelector(".profile-setup input").scrollIntoView({block:"center"});document.querySelector(".profile-setup input").focus()')
   adb('shell','input','text','Naufrago_QA')
   await cdp.until('document.querySelector(".profile-setup input").value==="Naufrago_QA"',5)
   await cdp.evaluate('document.querySelector(".profile-setup button").click()')
   await cdp.until('document.body.textContent.includes("Nombre guardado en esta PC.")',12)
   report['cases']['pwaAuthorizedPlayerPersistence']='PASS'
   await cdp.evaluate('document.activeElement?.blur();document.querySelector(".profile-setup").open=false')
   await asyncio.sleep(.5)
   report['stage']='SENSOR_BUTTON'
   await tap(cdp,'ACTIVAR CONTROL')
   for a in ['1:2:9.81','-2:1:9.81','0:0:9.81']:
    adb('emu','sensor','set','acceleration',a);adb('emu','sensor','set','gyroscope','0.1:0.2:0.3');await asyncio.sleep(.3)
   await cdp.until('Boolean(document.querySelector(".island-controller"))',20)
   report['cases']['sensorReady']='PASS'
   await cdp.until('document.querySelector(".island-controller")?.dataset.game==="log-balance"',12)
   report['cases']['unityAutoReturnAndGameContext']='PASS'
   await shot(cdp,'android-log-control')
   await asyncio.sleep(10)
   # Native app background/foreground produces real lifecycle events.
   adb('shell','input','keyevent','3');await asyncio.sleep(1)
   adb('shell','am','start','-n','com.android.chrome/com.google.android.apps.chrome.Main')
   await cdp.call('Page.bringToFront')
   for a in ['2:1:9.81','-1:3:9.81','0:1:9.81']:
    adb('emu','sensor','set','acceleration',a);adb('emu','sensor','set','gyroscope','0.2:0.1:0.3');await asyncio.sleep(.4)
   await cdp.until('Boolean(document.querySelector(".island-controller"))',15)
   report['cases']['backgroundForeground']='PASS'
   # Obtain only sanitized aggregate diagnostics, never persist admission/identity.
   await cdp.evaluate('document.querySelector(".control-help").open=true;Array.from(document.querySelectorAll("button")).find(e=>e.textContent==="Copiar diagnóstico").click()')
   diagnostic=await cdp.until('document.querySelector("pre")&&JSON.parse(document.querySelector("pre").textContent)')
   report['webAccessibilityTouchFallback']=getattr(tap,'web_tree_fallback',False)
   report['controllerState']=diagnostic['controller']['state'];report['transport']=diagnostic['transport'];report['sensors']=diagnostic['sensors'];report['gameContext']=diagnostic['controller'].get('gameContext')
   assert report['controllerState']=='INPUT_READY';assert report['transport']['motionAck']>0
   report['cases']['actualRtcMotionAck']='PASS'
   await cdp.evaluate('document.querySelectorAll("details").forEach(e=>e.open=false)')
   await cdp.navigate(base+'/');await cdp.until('document.body.textContent.includes("Escanea el QR")')
   await shot(cdp,'android-no-admission');report['cases']['noAdmission']='PASS'
   report['gate']='PASS';report['stage']='COMPLETE'
  except Exception as error:
   report['reason']=type(error).__name__;report['httpStatus']=getattr(error,'code',None)
   report['uiTarget']=getattr(tap,'last',None);report['uiObserved']=getattr(tap,'observed',None)
   if str(error).startswith('CDP command failed: '):report['detail']=str(error)
   if str(error) in ['UI_TREE_TARGET_MISSING','Browser evaluation failed','CDP command failed: Page.navigate']:report['detail']=str(error)
   if cdp:
    try:
     report['page']=await cdp.evaluate('({heading:document.querySelector("h2")?.textContent,buttons:Array.from(document.querySelectorAll("button")).map(e=>e.textContent)})')
     await shot(cdp,'android-failure')
    except Exception:pass
   report['stageState']={k:v for k,v in state.items() if k not in ['port','pids']}
  finally:
   if cdp:
    try:await cdp.navigate('about:blank');await cdp.connection.close()
    except Exception:pass
   if player and player.poll() is None:
    player.terminate()
    try:await asyncio.to_thread(player.wait,20)
    except subprocess.TimeoutExpired:player.kill();player.wait();report['forcedPlayer']=True
   await asyncio.sleep(2)
   report['ownedJavaResiduals']=sum(Path('/proc',str(p)).exists() for p in state['pids']);report['cleanup']=state['events'];report['surface']=state.get('surface')
   if state['port']:
    try:adb('reverse','--remove','tcp:'+str(state['port']))
    except Exception:pass
   if forward:
    try:adb('forward','--remove','tcp:'+forward)
    except Exception:pass
   if report['ownedJavaResiduals']:report['gate']='FAIL'
   if report['gate']=='PASS' and (state.get('surface') or {}).get('phoneAccepted',0)<1:report['gate']='FAIL'
 (OUT/'android-results.json').write_text(json.dumps(report,indent=2)+'\n')
 print(json.dumps({'gate':report['gate'],'cases':report['cases'],'reason':report.get('reason')}))
 return report['gate']
if __name__ == '__main__':
 raise SystemExit(0 if asyncio.run(run())=='PASS' else 1)
