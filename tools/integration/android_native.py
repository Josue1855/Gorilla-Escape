#!/usr/bin/env python3
"""Native Flutter → actual RTC/Java/Unity. Emulator only, private admission in memory."""
import asyncio,json,os,re,secrets,subprocess,sys,tempfile,threading,time,urllib.parse,xml.etree.ElementTree as ET
from pathlib import Path
import android_companion as qa
PACKAGE='com.gorillaescape.gorilla_companion'

def tree():
 raw=qa.adb('exec-out','uiautomator','dump','/dev/tty').decode(errors='replace')
 if '<?xml' not in raw or '</hierarchy>' not in raw:return ET.Element('hierarchy')
 return ET.fromstring(raw[raw.index('<?xml'):raw.rindex('</hierarchy>')+12])
def node(label):
 return next((n for n in tree().iter('node') if label in (n.get('text','')+' '+n.get('content-desc','')+' '+n.get('hint',''))),None)
def tap(label):
 n=node(label)
 if n is None:
  scroll=next((n for n in tree().iter('node') if n.get('scrollable')=='true'),None)
  if scroll is not None:
   b=list(map(int,re.findall(r'\d+',scroll.get('bounds',''))));x=(b[0]+b[2])//2
   qa.adb('shell','input','swipe',x,b[1]+(b[3]-b[1])*3//4,x,b[1]+(b[3]-b[1])//4,'500')
  n=node(label)
 if n is None:raise RuntimeError('MISSING_UI_TARGET_'+label)
 b=list(map(int,re.findall(r'\d+',n.get('bounds',''))));qa.adb('shell','input','tap',(b[0]+b[2])//2,(b[1]+b[3])//2);time.sleep(.8)
def waitfor(label,seconds=25):
 deadline=time.monotonic()+seconds
 while time.monotonic()<deadline:
  nodes=list(tree().iter('node'))
  n=next((n for n in nodes if label in (n.get('text','')+' '+n.get('content-desc','')+' '+n.get('hint',''))),None)
  if n is not None:return n
  if label.startswith('Conectado') and any('Este enlace no es un QR' in (n.get('text','')+n.get('content-desc','')) for n in nodes):raise RuntimeError('UI_TIMEOUT_QR_ENTRY')
  if label.startswith('Conectado') and any('Conexión interrumpida' in (n.get('text','')+n.get('content-desc','')) for n in nodes):raise RuntimeError('UI_TIMEOUT_CONNECTION_ERROR')
  time.sleep(.5)
 raise RuntimeError('UI_TIMEOUT_'+label)
async def run():
 report={'scope':'Flutter native emulator; no physical iPhone or LAN/TLS claim','gate':'FAIL','cases':{}}
 state={'port':None,'pids':[]};player=None
 with tempfile.TemporaryDirectory(prefix='gorilla-native-') as data:
  env=os.environ.copy();operator=secrets.token_urlsafe(32)
  env.update(DISPLAY=':97',GORILLA_IPC_JAVA=str(qa.PACKAGE/'Java/runtime/bin/java'),GORILLA_IPC_JAR=str(qa.PACKAGE/'Java/server.jar'),GORILLA_PACKAGE_ROOT=str(qa.PACKAGE),GORILLA_IPC_MODE='lifecycle',GORILLA_PHONE_INPUT='1',GORILLA_MOBILE_LAB='1',GORILLA_MOBILE_OPERATOR=operator,GORILLA_CONNECTION_MODE='CABIN',GORILLA_MAIN_MENU='1',GORILLA_QA_OVERLAY='1',GORILLA_QA_AUTOSTART_LOG_BALANCE='1',GORILLA_DATA_DIR=data)
  for k in ['GORILLA_MOBILE_CONFIG','GORILLA_LOCAL_TRUST_DIR','GORILLA_CAMERA_ENABLED']:env.pop(k,None)
  try:
   player=subprocess.Popen([str(qa.PACKAGE/'GorillaEscape.x86_64'),'-screen-fullscreen','0','-screen-width','1280','-screen-height','900','-logFile','-'],env=env,stdout=subprocess.PIPE,stderr=subprocess.DEVNULL,text=True)
   def consume():
    for line in player.stdout:
     if line.startswith('QA_LAB_HTTP_READY '):state['port']=int(line.split()[-1])
     if line.startswith('QA_SURFACE_SUMMARY '):state['surface']=json.loads(line.split(' ',1)[1])
     if line.startswith('{"component":"ipc-unity"'):
      e=json.loads(line)
      if e.get('event')=='PROCESS_STARTED':state['pids'].append(e['pid'])
   threading.Thread(target=consume,daemon=True).start()
   await asyncio.to_thread(qa.wait,lambda:state['port'],30,player)
   port=state['port'];base='http://127.0.0.1:'+str(port)
   qa.adb('reverse','tcp:'+str(port),'tcp:'+str(port))
   qa.adb('shell','am','force-stop',PACKAGE)
   qa.adb('shell','am','start','-n',PACKAGE+'/.MainActivity')
   await asyncio.to_thread(waitfor,'Tengo un enlace de partida')
   tap('Tengo un enlace de partida');tap('Enlace HTTPS del QR')
   admission=qa.post(base,'/mobile/admission',operator)['url'];u=urllib.parse.urlsplit(admission);admission=urllib.parse.urlunsplit((u.scheme,u.netloc,'/',u.query,u.fragment))
   import shlex
   prefix=admission.rsplit('admission=',1)[0]+'admission='
   # Pace real key events; a single long input command loses edits on this AVD.
   for chunk in [prefix[i:i+8] for i in range(0,len(prefix),8)]:
    qa.adb('shell','input text '+shlex.quote(chunk));await asyncio.sleep(.25)
   # Keep production's 30-second TTL: issue a fresh token after typing the prefix.
   fresh=qa.post(base,'/mobile/admission',operator)['url']
   token=urllib.parse.parse_qs(urllib.parse.urlsplit(fresh).fragment)['admission'][0]
   for chunk in [token[i:i+8] for i in range(0,len(token),8)]:
    qa.adb('shell','input text '+shlex.quote(chunk));await asyncio.sleep(.25)
   await asyncio.sleep(1)
   qa.adb('shell','input','keyevent','4');tap('ABRIR CONTROL')
   await asyncio.to_thread(waitfor,'Conectado · activa el control',40)
   report['cases']['nativeRtcHello']='PASS'
   tap('ACTIVAR CONTROL')
   for a in ['1:2:9.81','-2:1:9.81','0:0:9.81']:
    qa.adb('emu','sensor','set','acceleration',a);qa.adb('emu','sensor','set','gyroscope','0.1:0.2:0.3');await asyncio.sleep(.5)
   await asyncio.to_thread(waitfor,'CONTROL ACTIVO',25)
   await asyncio.to_thread(waitfor,'DESAFÍO',20)
   report['cases']['nativeSensorsUnityAutoReturnContext']='PASS'
   await asyncio.sleep(4)
   qa.adb('shell','input','keyevent','3');await asyncio.sleep(1)
   qa.adb('shell','am','start','-n',PACKAGE+'/.MainActivity')
   await asyncio.to_thread(waitfor,'CONTROL ACTIVO',20)
   report['cases']['nativeBackgroundResume']='PASS'
   await asyncio.sleep(4)
   (qa.OUT/'native-control.png').write_bytes(qa.adb('exec-out','screencap','-p'))
   tap('Ayuda de conexión')
   n=waitfor('Muestras confirmadas:')
   value=n.get('text','')+' '+n.get('content-desc','')+' '+n.get('hint','');count=re.search(r'Muestras confirmadas: (\d+)',value)
   assert count and int(count[1])>0
   report['motionAck']=int(count[1]);report['cases']['nativeMotionAck']='PASS'
   report['gate']='PASS'
  except Exception as e:
   try:
    request=qa.urllib.request.Request(base+'/mobile/diagnostics',headers={'X-Gorilla-Operator':operator})
    with qa.urllib.request.urlopen(request,timeout=5) as response:diagnostic=json.load(response)
    report['joinCodes']=[entry.get('code') for entry in diagnostic.get('joinAttempts',[]) if isinstance(entry,dict)]
   except Exception:pass
   try:tap('Ayuda de conexión')
   except Exception:pass
   report['reason']=type(e).__name__;report['detail']=str(e) if str(e).startswith(('UI_TIMEOUT','MISSING_UI')) else None
   try:
    report['ui']=[(n.get('text') or n.get('content-desc') or '')[:180] for n in tree().iter('node') if n.get('class')!='android.widget.EditText' and (n.get('text') or n.get('content-desc'))]
   except Exception:pass
  finally:
   qa.adb('shell','am','force-stop',PACKAGE)
   if player and player.poll() is None:
    player.terminate()
    try:await asyncio.to_thread(player.wait,20)
    except subprocess.TimeoutExpired:player.kill();player.wait()
   if state['port']:qa.adb('reverse','--remove','tcp:'+str(state['port']))
   await asyncio.sleep(2)
   report['surface']=state.get('surface');report['ownedJavaResiduals']=sum(Path('/proc',str(p)).exists() for p in state['pids'])
   if report['ownedJavaResiduals'] or (report.get('surface') or {}).get('phoneAccepted',0)<1:report['gate']='FAIL'
 (qa.OUT/'native-results.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report));return report['gate']
if __name__=='__main__':raise SystemExit(0 if asyncio.run(run())=='PASS' else 1)
