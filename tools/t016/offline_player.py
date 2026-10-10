#!/usr/bin/env python3
"""Linux build -> packaged physical vision -> Unity CameraInput, isolated WAN/cache.
Only aggregated privacy/cleanup/runtime metrics are retained; never images or pose traces.
"""
import argparse,json,os,pathlib,re,subprocess,sys,tempfile,threading,time
p=argparse.ArgumentParser();p.add_argument('--build',required=True);p.add_argument('--output',required=True);a=p.parse_args()
root=pathlib.Path(a.build).resolve();output=pathlib.Path(a.output)
if output.exists():raise SystemExit('Preserve previous evidence')
sys.path.insert(0,str(pathlib.Path(__file__).resolve().parents[1]));from validate_ipc_2b_player import X11
home=tempfile.mkdtemp(prefix='gorilla-t016-home-');trace=pathlib.Path(home)/'native-open.trace'
env={k:v for k,v in os.environ.items() if not k.startswith(('GORILLA_CAMERA_','GORILLA_IPC_','GORILLA_PHONE_','GORILLA_MOBILE_'))}
env.update(HOME=home,XDG_CACHE_HOME=home+'/cache',GORILLA_CAMERA_ENABLED='1')
namespace=['bwrap','--unshare-net','--dev-bind','/','/','--tmpfs',str(pathlib.Path.home()/'.cache'),'--proc','/proc','--']
probe=subprocess.run(namespace+[str(root/'Vision/python'),'-c','import socket; s=socket.socket(); s.settimeout(1); print(s.connect_ex(("1.1.1.1",443)))'],env=env,capture_output=True,text=True,timeout=10)
result={'scope':'Built Linux Player/relative Vision; physical webcam; private pipes/Unity store','wanProbeExit':probe.returncode,'wanUnavailable':probe.stdout.strip()=='101','developerCacheHidden':True,'imagePersistence':False,'gate':'FAIL','humanTracking':'NOT RUN'}
child=subprocess.Popen(namespace+['strace','-f','-e','trace=openat,creat,rename,unlink,connect','-o',str(trace),str(root/'GorillaEscape.x86_64'),'-screen-fullscreen','0','-screen-width','640','-screen-height','360','-logFile','-'],env=env,stdout=subprocess.PIPE,stderr=subprocess.PIPE,text=True)
ready=threading.Event();rows=[];counts={'stderrBytes':0,'forbiddenPhysicalObservationLogs':0}
def read():
 for line in child.stdout:
  if line.startswith('CAMERA_LAB_READY'):ready.set()
  if line.startswith('CAMERA_LAB_CLEANUP '):result['cleanup']=json.loads(line[19:])
  if line.startswith('CAMERA_LAB_METRICS '):result['metrics']=json.loads(line[19:])
  if line.startswith(('CAMERA_INPUT_OBSERVED ','FUSION_OBSERVED ')):counts['forbiddenPhysicalObservationLogs']+=1

def errors():
 for line in child.stderr:counts['stderrBytes']+=len(line)
def close_player():
 queue=[child.pid]
 for _ in range(16):
  if not queue:break
  pid=queue.pop(0)
  try:
   command=pathlib.Path('/proc/'+str(pid)+'/cmdline').read_bytes().split(b'\0')[0]
   if pathlib.Path(os.fsdecode(command)).name=='GorillaEscape.x86_64':X11().close(pid);return
   queue.extend(int(v) for v in pathlib.Path('/proc/'+str(pid)+'/task/'+str(pid)+'/children').read_text().split())
  except FileNotFoundError:pass
 raise RuntimeError('OWN_PLAYER_PID_INVALID')
t=threading.Thread(target=read);e=threading.Thread(target=errors);t.start();e.start()
try:
 if not ready.wait(25):raise RuntimeError('PACKAGED_CAMERA_STARTUP_FAILED')
 # Camera aggregate metrics only; no human admission/lock testing.
 time.sleep(35)
 close_player()
 code=child.wait(timeout=15);t.join(3);e.join(3);result['playerExitCode']=code
 text=trace.read_text();writes=[line for line in text.splitlines() if ('O_WRONLY' in line or 'O_RDWR' in line or 'creat(' in line) and not '= -1' in line]
 result['privacy']={'successfulImageVideoWrites':sum(bool(re.search(r'\.(jpg|jpeg|png|bmp|webp|mp4|avi|mkv)(?:[" ])',line,re.I)) for line in writes),'physicalObservationLogs':counts['forbiddenPhysicalObservationLogs'],'aggregateOnlyEvidence':True}
 cleanup=result.get('cleanup',{});result['residualOwnVision']=pathlib.Path('/proc/'+str(cleanup.get('pid',-1))).exists()
 result['gate']='PASS' if code==0 and result['wanUnavailable'] and cleanup.get('complete') and not cleanup.get('forced') and cleanup.get('exitCode')==0 and cleanup.get('accepted',0)>20 and cleanup.get('source')=='physical-webcam' and not result['residualOwnVision'] and result['privacy']['successfulImageVideoWrites']==0 and counts['forbiddenPhysicalObservationLogs']==0 else 'FAIL'
except Exception as error:
 result['error']=str(error)
finally:
 if child.poll() is None:
  close_player();child.wait(timeout=15);t.join(3);e.join(3);result['errorCleanupComplete']=True
 result['stderrBytes']=counts['stderrBytes'];trace.unlink(missing_ok=True)
 output.parent.mkdir(parents=True,exist_ok=True);output.write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result))
if result['gate']!='PASS':sys.exit(1)
