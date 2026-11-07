"""ADB UI review of the installed Companion. Taps are derived only from the UI tree."""
import argparse,json,re,shlex,subprocess,time,xml.etree.ElementTree as ET
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
PKG='com.gorillaescape.gorilla_companion'
def main():
 ap=argparse.ArgumentParser();ap.add_argument('--serial',default='emulator-5554');ap.add_argument('--output',default=str(ROOT/'Reports/QA/UI-UX-20261104/android'));args=ap.parse_args()
 out=Path(args.output);out.mkdir(parents=True,exist_ok=True);checks=[]
 def adb(*cmd,raw=False):
  r=subprocess.run(['adb','-s',args.serial,*cmd],stdout=subprocess.PIPE,stderr=subprocess.PIPE,timeout=180 if cmd[0]=='install' else 45,check=True)
  return r.stdout if raw else r.stdout.decode('utf-8',errors='replace')
 def tree():
  raw=adb('exec-out','uiautomator','dump','/dev/tty');start=raw.find('<?xml');end=raw.rfind('</hierarchy>')
  if start<0:return ET.Element('hierarchy')
  return ET.fromstring(raw[start:end+len('</hierarchy>')])
 def bounds(node):return tuple(map(int,re.findall(r'\d+',node.get('bounds',''))))
 def find(label,root=None):
  nodes=list((root or tree()).iter('node'))
  exact=[n for n in nodes if n.get('text')==label or n.get('content-desc')==label]
  found=exact or [n for n in nodes if label.casefold() in (n.get('text','')+' '+n.get('content-desc','')).casefold()]
  found=[n for n in found if len(bounds(n))==4 and bounds(n)[2]>bounds(n)[0]]
  return min(found,key=lambda n:(bounds(n)[2]-bounds(n)[0])*(bounds(n)[3]-bounds(n)[1])) if found else None
 def tap(label):
  node=find(label)
  if node is None:raise AssertionError('Missing tap target: '+label)
  x1,y1,x2,y2=bounds(node);adb('shell','input','tap',str((x1+x2)//2),str((y1+y2)//2));time.sleep(.35)
 def scroll_to(label):
  for _ in range(6):
   root=tree()
   if find(label,root) is not None:return
   nodes=[n for n in root.iter('node') if n.get('scrollable')=='true']
   if not nodes:raise AssertionError('No scrollable surface for '+label)
   x1,y1,x2,y2=bounds(max(nodes,key=lambda n:(bounds(n)[2]-bounds(n)[0])*(bounds(n)[3]-bounds(n)[1])))
   adb('shell','input','swipe',str((x1+x2)//2),str(y1+int((y2-y1)*.78)),str((x1+x2)//2),str(y1+int((y2-y1)*.25)),'350');time.sleep(.2)
  raise AssertionError('Missing after scroll: '+label)
 def capture(name):
  (out/(name+'.png')).write_bytes(adb('exec-out','screencap','-p',raw=True))
  raw=adb('exec-out','uiautomator','dump','/dev/tty');(out/(name+'.xml')).write_text(raw,encoding='utf-8')
 def wait(label):
  until=time.monotonic()+35
  while time.monotonic()<until:
   if find(label) is not None:return
   time.sleep(.25)
  raise AssertionError('Missing state: '+label)
 apk=ROOT/'Companion/build/app/outputs/flutter-apk/app-debug.apk'
 adb('install','-r',str(apk));adb('logcat','-c');adb('shell','am','force-stop',PKG);adb('shell','am','start','-n',PKG+'/.MainActivity')
 wait('GORILLA ESCAPE');capture('01-home');checks.append('Home and brand visible on Android emulator')
 scroll_to('Tengo un enlace de partida');tap('Tengo un enlace de partida');time.sleep(.2)
 nodes=[n for n in tree().iter('node') if n.get('class')=='android.widget.EditText']
 assert nodes,'Manual link field is missing'
 x1,y1,x2,y2=bounds(nodes[0]);adb('shell','input','tap',str((x1+x2)//2),str((y1+y2)//2));adb('shell','input','text','no-es-un-qr');capture('02-manual-link-keyboard')
 adb('shell','input','keyevent','4');scroll_to('ABRIR CONTROL');tap('ABRIR CONTROL');wait('Este enlace no es un QR');capture('03-invalid-link');checks.append('Invalid link stays on home with readable recovery text')
 def link(token):return 'https://example.invalid/#v=1&sessionId=00000000-0000-0000-0000-000000000001&admission='+token*43
 adb('shell','am','start','-n',PKG+'/.MainActivity','-a','android.intent.action.VIEW','-d',shlex.quote(link('a')))
 wait('Conexión interrumpida');wait('RECUPERA TU CONEXIÓN');assert find('Control preparado') is None;capture('04-disconnected');checks.append('Unreachable room produces recoverable connection screen')
 adb('shell','am','start','-n',PKG+'/.MainActivity','-a','android.intent.action.VIEW','-d',shlex.quote(link('b')))
 wait('¿Usar otra sala?');capture('05-switch-room-confirmation');tap('ABRIR QR NUEVO');wait('Conexión interrumpida');checks.append('A new QR can replace an existing control after confirmation')
 adb('shell','input','keyevent','4');wait('GORILLA ESCAPE');checks.append('Back from an unreachable room returns to home')
 logs=adb('logcat','-d');(out/'app-log-review.txt').write_text('\n'.join(l for l in logs.splitlines() if any(s in l for s in ['RenderFlex','FATAL EXCEPTION','E/flutter','gorilla_companion'])),encoding='utf-8')
 app_pid=adb('shell','pidof',PKG).strip()
 assert app_pid,'Companion process exited'
 app_logs='\n'.join(line for line in logs.splitlines() if re.match(r'^\S+\s+\S+\s+'+re.escape(app_pid)+r'\s',line))
 assert not re.search(r'A RenderFlex overflowed|FATAL EXCEPTION',app_logs),'Companion rendered an overflow or crashed'
 system_failures=len(re.findall(r'FATAL EXCEPTION IN SYSTEM PROCESS',logs))
 (out/'results.json').write_text(json.dumps(dict(status='PASS',scope='Android emulator; invalid room fixture, no physical sensors or WAN acceptance',system_tool_failures=system_failures,checks=checks),ensure_ascii=False,indent=2),encoding='utf-8')
 print('PASS',len(checks),'Android emulator flows;',out)
if __name__=='__main__':main()
