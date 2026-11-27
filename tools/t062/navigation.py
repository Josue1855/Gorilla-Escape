#!/usr/bin/env python3
"""Exercise real built MainMenu buttons without capturing or saving any screen image."""
import ctypes as C,http.client,json,os,secrets,ssl,subprocess,sys,tempfile,threading,time
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
from validate_ipc_2b_player import X11,wait
root=Path(sys.argv[1]).resolve();output=Path(sys.argv[2]);operator=secrets.token_urlsafe(32);physical='--physical-backend' in sys.argv[3:]
if output.exists():raise SystemExit('PRESERVE_EVIDENCE')
report={'classification':'LAB / built UI + physical capture transport; physical phone NOT RUN','screenshots':0,'gate':'FAIL','opened':0,'closed':0,'secretLogs':0}
events=[];summary={};owned=set();stderr=0;render_size=None

def descendants(pid):
 result=set();queue=[pid]
 while queue:
  pid=queue.pop()
  if pid in result:continue
  result.add(pid)
  try:
   for p in Path('/proc/'+str(pid)+'/task').glob('*/children'):queue.extend(map(int,p.read_text().split()))
  except FileNotFoundError:pass
 return result

def vision_pid():
 for pid in descendants(player.pid):
  try:
   if b'/worker.py' in Path('/proc/'+str(pid)+'/cmdline').read_bytes():return pid
  except FileNotFoundError:pass

def listeners(pid):
 if not pid:return []
 inodes=set()
 for fd in Path('/proc/'+str(pid)+'/fd').glob('*'):
  try:
   target=os.readlink(fd)
   if target.startswith('socket:['):inodes.add(target[8:-1])
  except FileNotFoundError:pass
 found=[]
 for row in Path('/proc/net/tcp').read_text().splitlines()[1:]:
  f=row.split()
  if f[3]=='0A' and f[9] in inodes:found.append(f[1])
 return found

class Driver(X11):
 def activate(self,window):
  class Data(C.Union):_fields_=[('b',C.c_char*20),('s',C.c_short*10),('l',C.c_long*5)]
  class Message(C.Structure):_fields_=[('type',C.c_int),('serial',C.c_ulong),('send_event',C.c_int),('display',C.c_void_p),('window',C.c_ulong),('message_type',C.c_ulong),('format',C.c_int),('data',Data)]
  message=Message();message.type=33;message.display=self.display;message.window=window
  message.message_type=self.x.XInternAtom(self.display,b'_NET_ACTIVE_WINDOW',0);message.format=32;message.data.l[0]=2
  self.x.XSendEvent.argtypes=[C.c_void_p,C.c_ulong,C.c_int,C.c_long,C.c_void_p]
  self.x.XSendEvent(self.display,self.root,0,(1<<20)|(1<<19),C.byref(message));self.x.XFlush(self.display)
 def geometry(self,pid):
  window=self.window(pid);root=C.c_ulong();x=C.c_int();y=C.c_int();w=C.c_uint();h=C.c_uint();border=C.c_uint();depth=C.c_uint()
  self.x.XGetGeometry.argtypes=[C.c_void_p,C.c_ulong,C.POINTER(C.c_ulong),C.POINTER(C.c_int),C.POINTER(C.c_int),C.POINTER(C.c_uint),C.POINTER(C.c_uint),C.POINTER(C.c_uint),C.POINTER(C.c_uint)]
  self.x.XGetGeometry(self.display,window,C.byref(root),C.byref(x),C.byref(y),C.byref(w),C.byref(h),C.byref(border),C.byref(depth));return w.value,h.value
 def action(self,pid,x,y,condition):
  for _ in range(4):
   self.clickxy(pid,x,y)
   try:return wait(condition,1)
   except TimeoutError:pass
  raise TimeoutError('UI action was not acknowledged')
 def clickxy(self,pid,x,y):
  width,height=self.geometry(pid);rw,rh=render_size or (width,height);scale=max(.75,min(rw/1280,rh/940));x=round(x*scale*width/rw);y=round(y*scale*height/rh)
  window=self.window(pid);self.activate(window);xx=C.c_int();yy=C.c_int();child=C.c_ulong()
  self.x.XRaiseWindow(self.display,window);self.x.XSetInputFocus(self.display,window,1,0);self.x.XFlush(self.display)
  self.x.XTranslateCoordinates(self.display,window,self.root,x,y,C.byref(xx),C.byref(yy),C.byref(child))
  self.xt.XTestFakeMotionEvent(self.display,-1,xx.value,yy.value,0);self.xt.XTestFakeButtonEvent(self.display,1,1,0);self.x.XFlush(self.display)
  time.sleep(.08);self.xt.XTestFakeButtonEvent(self.display,1,0,0);self.x.XFlush(self.display)

with tempfile.TemporaryDirectory(prefix='gorilla-main-menu-ui-') as home:
 env={**os.environ,'HOME':home,'GORILLA_MAIN_MENU':'1','GORILLA_QA_MENU':'1','GORILLA_QA_UI_PROBE':'1','GORILLA_QA_OVERLAY':'1','GORILLA_PHONE_INPUT':'1','GORILLA_MOBILE_LAB':'1','GORILLA_MOBILE_OPERATOR':operator,'GORILLA_IPC_JAVA':str(root/'Java/runtime/bin/java'),'GORILLA_IPC_JAR':str(root/'Java/server.jar'),'GORILLA_CAMERA_ENABLED':'1','GORILLA_CAMERA_PROFILE':'candidate','GORILLA_CAMERA_BUFFERS':'2'}
 env.pop('GORILLA_QA_AUTOSTART_TESTS',None)
 if physical:
  env.pop('GORILLA_MOBILE_LAB',None);env['GORILLA_LOCAL_TRUST_DIR']=home+'/test-trust'
  report['backendMode']='PHYSICAL HTTPS LAN / no physical phone'

 player=subprocess.Popen(([str(root/'run-qa.sh')] if physical else [str(root/'GorillaEscape.x86_64')])+['-screen-fullscreen','0','-screen-width','1280','-screen-height','940','-logFile','-'],env=env,stdout=subprocess.PIPE,stderr=subprocess.PIPE,text=True)
 def read():
  global render_size
  for line in player.stdout:
   if operator in line:report['secretLogs']+=1
   if line.startswith('MAIN_MENU_POINTER '):report['mouseDownEvents']=report.get('mouseDownEvents',0)+1
   if line.startswith('MAIN_MENU_LAYOUT '):render_size=tuple(map(int,line.split()[1:]));report['renderSize']=render_size
   if line.startswith(('NullReferenceException','ArgumentException','InvalidOperationException')):report['uiErrors']=report.get('uiErrors',0)+1
   if line.startswith('QA_CENTER_OPEN'):report['opened']+=1
   if line.startswith('QA_CENTER_CLOSED'):report['closed']+=1
   if line.startswith('CAMERA_LAB_READY'):events.append('CAMERA');report['cameraStarted']=True
   if line.startswith('{"component":"ipc-unity"'):
    item=json.loads(line)
    if item.get('errorCode'):report.setdefault('backendErrorCodes',[]).append(item['errorCode'])
    if item['event']=='FIRST_PONG':events.append('BACKEND')
   if line.startswith('QA_SURFACE_SUMMARY '):summary.update(json.loads(line[19:]))
 def drain():
  global stderr
  for line in player.stderr:stderr+=len(line)
 a=threading.Thread(target=read,daemon=True);b=threading.Thread(target=drain,daemon=True);a.start();b.start();driver=Driver()
 try:
  wait(lambda:'CAMERA' in events and 'BACKEND' in events,30);pid=vision_pid();assert pid
  if physical:
   addresses=json.loads(subprocess.check_output(['ip','-4','-json','addr','show','scope','global'],text=True))
   address=env.get('GORILLA_LAN_ADDRESS') or next(a['local'] for item in addresses for a in item['addr_info'] if a['family']=='inet')
   ca=Path(home,'test-trust/root.cer');wait(ca.is_file,5)
   context=ssl.create_default_context(cadata=ssl.DER_cert_to_PEM_cert(ca.read_bytes()))
   connection=http.client.HTTPSConnection(address,int(env.get('GORILLA_HTTPS_PORT','8443')),context=context,timeout=5)
   connection.request('GET','/');response=connection.getresponse();response.read();report['httpsLanStatus']=response.status;connection.close();assert response.status==200

  owned.update(descendants(player.pid));report["windowSize"]=driver.geometry(player.pid);assert not listeners(pid)
  driver.clickxy(player.pid,180,485);time.sleep(.3);assert report['opened']==0
  report['playDisabled']=True
  driver.action(player.pid,180,568,lambda:report['opened']==1)
  endpoints=wait(lambda:listeners(pid),5);assert all(e.startswith('0100007F:') for e in endpoints);report['loopbackOnly']=True
  driver.clickxy(player.pid,825,824);driver.clickxy(player.pid,1000,824)
  files=wait(lambda:list(Path(home,'GorillaEscape-QA-Summaries').glob('qa-*.json')),5)
  def read_export():
   try:return json.loads(files[-1].read_text())
   except (ValueError,OSError):return None
  exported=wait(read_export,5);assert exported['measurementStart']>0 and exported['measurementEnd']>=exported['measurementStart']
  for key in ['token','resumeToken','modules','sdp','acceleration','orientation']:
   assert key not in exported
  report['summaryExport']='PASS / aggregate allowlist'
  if physical:assert exported['qrVisible']>0;report['realQrRendered']=True
  driver.action(player.pid,120,920,lambda:report['closed']==1 and not listeners(pid))
  driver.action(player.pid,180,568,lambda:report['opened']==2 and listeners(pid))
  driver.action(player.pid,120,920,lambda:report['closed']==2 and not listeners(pid))
  driver.clickxy(player.pid,180,649);report['exitCode']=player.wait(timeout=20);a.join(2);b.join(2)
  report['cleanup']=summary;report['residualOwnedProcesses']=sum(Path('/proc/'+str(p)).exists() for p in owned)
  if report['exitCode']==0 and not report['residualOwnedProcesses'] and summary.get('cameraCleanup') and summary.get('javaCleanup') and not report['secretLogs']:report['gate']='PASS'
 except Exception as error:report['error']=str(error)
 finally:
  if player.poll() is None:
   driver.close(player.pid)
   try:player.wait(20)
   except subprocess.TimeoutExpired:player.kill();player.wait(5);report['forced']=True
  report['stderrBytes']=stderr
output.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report));sys.exit(0 if report['gate']=='PASS' else 1)
