#!/usr/bin/env python3
"""Linux build -> packaged physical vision -> Unity CameraInput, isolated WAN/cache.
Only aggregated privacy/cleanup/runtime metrics are retained; never images or pose traces.
"""
import argparse,hashlib,json,os,pathlib,re,signal,subprocess,sys,tempfile,threading,time
from collections import deque
from math import ceil
p=argparse.ArgumentParser();p.add_argument('--build',required=True);p.add_argument('--output',required=True);p.add_argument('--profile',choices=['original','candidate'],default='original');p.add_argument('--buffers',type=int,choices=[1,2,3],default=1);p.add_argument('--warmup',type=float,default=5);p.add_argument('--duration',type=float,default=61);p.add_argument('--telemetry',choices=['minimal','enabled'],default='enabled');p.add_argument('--measure-frame-times',action='store_true');p.add_argument('--qa-overlay',action='store_true');p.add_argument('--qa-menu-only',action='store_true');p.add_argument('--java');p.add_argument('--jar');a=p.parse_args()
if bool(a.java)!=bool(a.jar):p.error('--java and --jar must be supplied together')
root=pathlib.Path(a.build).resolve();output=pathlib.Path(a.output)
if output.exists():raise SystemExit('Preserve previous evidence')
sys.path.insert(0,str(pathlib.Path(__file__).resolve().parents[1]));from validate_ipc_2b_player import X11
home=tempfile.mkdtemp(prefix='gorilla-t016-home-');trace=pathlib.Path(home)/'native-open.trace'
env={k:v for k,v in os.environ.items() if not k.startswith(('GORILLA_CAMERA_','GORILLA_IPC_','GORILLA_PHONE_','GORILLA_MOBILE_','GORILLA_T018_','GORILLA_TELEMETRY_','GORILLA_QA_','GORILLA_MAIN_MENU'))}
env.update(HOME=home,XDG_CACHE_HOME=home+'/cache',GORILLA_CAMERA_ENABLED='1')
env['GORILLA_TELEMETRY_MODE']=a.telemetry
if a.qa_overlay:
 if not a.java:p.error('--qa-overlay requires Java/JAR')
 import secrets
 env.update(GORILLA_QA_OVERLAY='1',GORILLA_PHONE_INPUT='1',GORILLA_MOBILE_LAB='1',GORILLA_MOBILE_OPERATOR=secrets.token_urlsafe(32))
if a.qa_overlay:
 env['GORILLA_QA_MENU']='0' if a.qa_menu_only else '1'
 env['GORILLA_QA_AUTOSTART_TESTS']='0' if a.qa_menu_only else '1'
if a.java:env.update(GORILLA_IPC_JAVA=str(pathlib.Path(a.java).resolve()),GORILLA_IPC_JAR=str(pathlib.Path(a.jar).resolve()),GORILLA_IPC_MODE='lifecycle')
if a.measure_frame_times:env.update(GORILLA_T018_MEASURE='1',GORILLA_T018_DURATION=str(a.duration))
if a.measure_frame_times and a.qa_overlay:env['GORILLA_QA_AUTOMATED_MEASUREMENT']='1'
env.update(GORILLA_CAMERA_PROFILE=a.profile,GORILLA_CAMERA_BUFFERS=str(a.buffers),GORILLA_CAMERA_WARMUP=str(a.warmup))
namespace=['bwrap','--unshare-net','--dev-bind','/','/','--tmpfs',str(pathlib.Path.home()/'.cache'),'--proc','/proc','--']
probe=subprocess.run(namespace+[str(root/'Vision/python'),'-c','import socket; s=socket.socket(); s.settimeout(1); print(s.connect_ex(("1.1.1.1",443)))'],env=env,capture_output=True,text=True,timeout=10)
result={'scope':'Built Linux Player/relative Vision; physical webcam; private pipes/Unity store','wanProbeExit':probe.returncode,'wanUnavailable':probe.stdout.strip()=='101','developerCacheHidden':True,'imagePersistence':False,'gate':'FAIL','humanTracking':'NOT RUN','javaLifecycleEnabled':bool(a.java),'ipcLifecycle':{'started':0,'stopped':0,'errors':0},'cameraRuntimeErrors':0}
result['qaNavigation']={'opened':0,'closed':0}
result['requestedPreview']='OFF' if a.qa_menu_only else 'ON' if a.qa_overlay else 'DISABLED'
result['candidateHashes']={str(p.relative_to(root)):hashlib.sha256(p.read_bytes()).hexdigest() for p in [root/'GorillaEscape_Data/Managed/GorillaEscape.Runtime.dll',root/'Vision/worker.py',root/'Vision/preview.py',root/'Java/server.jar'] if p.is_file()}
child=subprocess.Popen(namespace+['strace','-f','-e','trace=openat,creat,rename,unlink,connect','-o',str(trace),str(root/'GorillaEscape.x86_64'),'-screen-fullscreen','0','-screen-width','640','-screen-height','360','-logFile','-'],env=env,stdout=subprocess.PIPE,stderr=subprocess.PIPE,text=True,start_new_session=True)
ready=threading.Event();timing_done=threading.Event();rows=[];counts={'stderrBytes':0,'forbiddenPhysicalObservationLogs':0}
def read():
 for line in child.stdout:
  if line.startswith('QA_CENTER_OPEN'):result['qaNavigation']['opened']+=1
  if line.startswith('QA_CENTER_CLOSED'):result['qaNavigation']['closed']+=1
  if line.startswith('IPC_DIAGNOSTIC '):result['ipcDiagnosticObserved']=True
  if line.startswith('CAMERA_LAB_ERROR '):result['cameraRuntimeErrors']+=1
  if line.startswith('{"component":"ipc-unity"'):
   try:
    event=json.loads(line);name=event.get('event','');result['ipcLifecycle']['started']+=name=='PROCESS_STARTED';result['ipcLifecycle']['stopped']+=name=='SHUTDOWN_CLEAN';result['ipcLifecycle']['errors']+=bool(event.get('errorCode')) or bool(event.get('cleanupError'));result['ipcLifecycle']['last']={k:event.get(k) for k in ['event','state','exitCode','errorCode','cleanupError','cleanupComplete','forced']}
   except (ValueError,TypeError):result['ipcLifecycle']['errors']+=1
  if line.startswith('QA_SURFACE_SUMMARY '):result['qaSurface']=json.loads(line[19:])
  if line.startswith('CAMERA_LAB_READY'):ready.set()
  if line.startswith('T018_FRAME_TIMING '):result['unityFrameTiming']=json.loads(line[18:]);result['qaNavigationAtMeasurementEnd']=dict(result['qaNavigation']);timing_done.set()
  if line.startswith('CAMERA_LAB_CLEANUP '):result['cleanup']=json.loads(line[19:])
  if line.startswith('CAMERA_LAB_METRICS '):result['metrics']=json.loads(line[19:])
  if line.startswith('CAMERA_LAB_TRANSIT '):result['transit']=json.loads(line[19:])
  if line.startswith(('CAMERA_INPUT_OBSERVED ','FUSION_OBSERVED ')):counts['forbiddenPhysicalObservationLogs']+=1

def errors():
 for line in child.stderr:counts['stderrBytes']+=len(line)
def own_processes():
 found={};queue=[child.pid]
 for _ in range(32):
  if not queue:break
  pid=queue.pop(0)
  try:
   command=pathlib.Path('/proc/'+str(pid)+'/cmdline').read_bytes().split(b'\0')
   name=pathlib.Path(os.fsdecode(command[0])).name
   if name=='GorillaEscape.x86_64':found['unity']=pid
   if name=='java':found['java']=pid
   if name=='python3.12' and any(v.endswith(b'/worker.py') for v in command):found['vision']=pid

   for task_children in pathlib.Path('/proc/'+str(pid)+'/task').glob('*/children'):
    try:queue.extend(int(v) for v in task_children.read_text().split())
    except FileNotFoundError:pass
  except FileNotFoundError:pass
 return found

owned_runtime_pids=set()
memory_stop=threading.Event();memory={name:deque(maxlen=4096) for name in ['unity','vision','java']};memory_counts={name:0 for name in memory};memory_edges={name:[] for name in memory};cpu={name:deque(maxlen=4096) for name in memory};cpu_previous={};clock_offsets=deque(maxlen=4096);clock_brackets=deque(maxlen=4096)
def sample_memory():
 from validate_ipc_2c_player import rss
 while not memory_stop.is_set():
  before=time.monotonic_ns();utc=time.time_ns();after=time.monotonic_ns()
  clock_offsets.append((utc-(before+after)/2)/1e6);clock_brackets.append((after-before)/2e6)
  for name,pid in own_processes().items():
   owned_runtime_pids.add((name,pid))
   value=rss(pid)
   if value is not None:
    memory[name].append(value);memory_counts[name]+=1
    if len(memory_edges[name])<2:memory_edges[name].append(value)
    else:memory_edges[name][1]=value
   try:
    fields=pathlib.Path('/proc/'+str(pid)+'/stat').read_text().rsplit(')',1)[1].split();ticks=int(fields[11])+int(fields[12]);stamp=time.monotonic();previous=cpu_previous.get((name,pid))
    if previous and stamp>previous[0]:cpu[name].append(100*(ticks-previous[1])/os.sysconf('SC_CLK_TCK')/(stamp-previous[0]))
    cpu_previous[(name,pid)]=(stamp,ticks)
   except (FileNotFoundError,IndexError,ValueError):pass
  memory_stop.wait(1)

def close_player():
 queue=[child.pid]
 for _ in range(16):
  if not queue:break
  pid=queue.pop(0)
  try:
   command=pathlib.Path('/proc/'+str(pid)+'/cmdline').read_bytes().split(b'\0')[0]
   if pathlib.Path(os.fsdecode(command)).name=='GorillaEscape.x86_64':X11().close(pid);return

   for task_children in pathlib.Path('/proc/'+str(pid)+'/task').glob('*/children'):
    try:queue.extend(int(v) for v in task_children.read_text().split())
    except FileNotFoundError:pass
  except FileNotFoundError:pass
 raise RuntimeError('OWN_PLAYER_PID_INVALID')
t=threading.Thread(target=read,daemon=True);e=threading.Thread(target=errors,daemon=True);t.start();e.start()
try:
 if not ready.wait(25):raise RuntimeError('PACKAGED_CAMERA_STARTUP_FAILED')
 # Camera aggregate metrics only; no human admission/lock testing.
 print('CAMERA_VALIDATION_WARMUP',flush=True)
 time.sleep(a.warmup)
 if a.measure_frame_times:
  memory_reader=threading.Thread(target=sample_memory,daemon=True);memory_reader.start()
 print('CAMERA_VALIDATION_MEASURING',flush=True)
 if a.measure_frame_times:
  if not timing_done.wait(a.duration+10):raise RuntimeError('T018_TIMING_TIMEOUT')
 else:time.sleep(a.duration)
 memory_stop.set()
 if a.measure_frame_times:
  memory_reader.join(3)
  result['hostClockBridge']={'N':len(clock_offsets),'offsetUtcMinusMonotonicMsMin':min(clock_offsets) if clock_offsets else None,'offsetUtcMinusMonotonicMsMax':max(clock_offsets) if clock_offsets else None,'maxHalfReadBracketMs':max(clock_brackets) if clock_brackets else None,'method':'monotonic_ns before/time_ns/monotonic_ns after, midpoint offset; descriptive observed range, not bound on future slew or event timestamp error','cameraUnityUtcQuantizationUncertaintyMs':'<2 ms conservative combined integer-ms quantization only; total error UNKNOWN','phoneSynchronization':'NOT RUN; no physical-device offset/drift/error measurement'}
  result['rssBytes']={}
  for name,values in memory.items():
   values=sorted(values);result['rssBytes'][name]={'N':memory_counts[name],'retained':len(values),'P50':values[ceil(len(values)*.5)-1] if values else None,'P95':values[ceil(len(values)*.95)-1] if values else None,'peak':max(values) if values else None,'first':memory_edges[name][0] if memory_edges[name] else None,'last':memory_edges[name][-1] if memory_edges[name] else None,'delta':memory_edges[name][-1]-memory_edges[name][0] if memory_edges[name] else None,'source':'/proc VmRSS at 1 Hz; startup included; descriptive first/last drift, not leak diagnosis'}
  result['cpuPercent']={name:{'N':len(values),'P50':sorted(values)[ceil(len(values)*.5)-1] if values else None,'P95':sorted(values)[ceil(len(values)*.95)-1] if values else None,'definition':'/proc utime+stime over monotonic elapsed; 100%=one CPU core'} for name,values in cpu.items()}
 close_player()
 code=child.wait(timeout=20);t.join(3);e.join(3);result['playerExitCode']=code
 text=trace.read_text();writes=[line for line in text.splitlines() if ('O_WRONLY' in line or 'O_RDWR' in line or 'creat(' in line) and not '= -1' in line]
 result['privacy']={'successfulImageVideoWrites':sum(bool(re.search(r'\.(jpg|jpeg|png|bmp|webp|mp4|avi|mkv)(?:[" ])',line,re.I)) for line in writes),'physicalObservationLogs':counts['forbiddenPhysicalObservationLogs'],'aggregateOnlyEvidence':True}
 cleanup=result.get('cleanup',{});result['residualOwnVision']=pathlib.Path('/proc/'+str(cleanup.get('pid',-1))).exists()
 result['gate']='PASS' if code==0 and result['wanUnavailable'] and cleanup.get('complete') and cleanup.get('controlsRestored') and not cleanup.get('forced') and cleanup.get('exitCode')==0 and cleanup.get('accepted',0)>20 and cleanup.get('source')=='physical-webcam' and not result['residualOwnVision'] and result['privacy']['successfulImageVideoWrites']==0 and counts['forbiddenPhysicalObservationLogs']==0 else 'FAIL'
 if a.java and (memory_counts['java']==0 or result['ipcLifecycle']['stopped']!=1):result['gate']='FAIL'
 result['residualOwnJava']=any(name=='java' and pathlib.Path('/proc/'+str(pid)).exists() for name,pid in owned_runtime_pids)
 result['residualOwnedRuntime']={name:sum(pathlib.Path('/proc/'+str(pid)).exists() for kind,pid in owned_runtime_pids if kind==name) for name in memory}
 if result['cameraRuntimeErrors'] or result['ipcLifecycle']['errors'] or any(result['residualOwnedRuntime'].values()):result['gate']='FAIL'
 result['telemetryMode']=a.telemetry;result['requestedDurationSeconds']=a.duration;result['requestedWarmupSeconds']=a.warmup
 if a.measure_frame_times and (result.get('unityFrameTiming',{}).get('N',0)==0 or result['metrics'].get('telemetryMode')!=a.telemetry):result['gate']='FAIL'
 result['profile']=a.profile;result['buffers']=a.buffers;result['captureThresholdPass']=result.get('metrics',{}).get('captureFps',0)>=30;result['qualityAcceptance']='Outside T016 acceptance; no human tests authorized'
 if not result['captureThresholdPass'] and not a.measure_frame_times:result['gate']='FAIL'
 # This harness is now technical-only under the PO decision: no human QA or quality gate.
 for key in list(result.get('metrics',{})):
  if any(word in key.lower() for word in ['tracking','quality','subject','confidence','landmark','pixel','ambiguous','eligible','productthreshold','detection']):del result['metrics'][key]
 if a.qa_overlay:
  expected=(0,0) if a.qa_menu_only else (1,0)
  if (result.get('qaNavigationAtMeasurementEnd',result['qaNavigation'])['opened'],result.get('qaNavigationAtMeasurementEnd',result['qaNavigation'])['closed'])!=expected:result['gate']='FAIL';result['previewModeError']='Requested preview mode changed or was not active'
 result['humanTracking']='NOT RUN — technical camera/runtime test only';result['qualityAcceptance']='Outside T016 acceptance; no human tests authorized'
except Exception as error:
 result['error']=str(error)
finally:
 memory_stop.set()
 if child.poll() is None:
  try:
   close_player();child.wait(timeout=20);result['errorCleanupComplete']=True
  except Exception as error:
   result['cleanupError']=str(error);result['forcedHarnessCleanup']=True;result['gate']='FAIL'
   # Own session only; never signal unrelated camera owners.
   os.killpg(child.pid,signal.SIGTERM)
   try:child.wait(timeout=3)
   except subprocess.TimeoutExpired:
    os.killpg(child.pid,signal.SIGKILL);child.wait(timeout=3)
 t.join(3);e.join(3)
 result['stderrBytes']=counts['stderrBytes'];trace.unlink(missing_ok=True)
 output.parent.mkdir(parents=True,exist_ok=True);output.write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result))
if result['gate']!='PASS':sys.exit(1)
