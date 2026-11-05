"""Browser UI review with synthetic session snapshots; never claims physical sensor acceptance."""
import argparse,base64,json,os,subprocess,sys,tempfile,time,urllib.request
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
from cdp_pwa_client import MinimalCDPClient,open_pwa_tab
ROOT=Path(__file__).resolve().parents[2]
def main():
 parser=argparse.ArgumentParser();parser.add_argument('--url',default='http://127.0.0.1:5177/');parser.add_argument('--output',default=str(ROOT/'Reports/QA/UI-UX-20261104/pwa'));args=parser.parse_args()
 out=Path(args.output);out.mkdir(parents=True,exist_ok=True)
 chrome=Path(os.environ['PROGRAMFILES'])/'Google/Chrome/Application/chrome.exe'
 results=[]
 with tempfile.TemporaryDirectory(prefix='gorilla-ui-chrome-') as profile:
  proc=subprocess.Popen([str(chrome),'--headless=new','--remote-debugging-port=9335','--no-first-run','--no-default-browser-check',f'--user-data-dir={profile}','about:blank'],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
  client=None
  try:
   for _ in range(60):
    try:
     tab=open_pwa_tab(9335,'about:blank');break
    except Exception:time.sleep(.2)
   else:raise RuntimeError('Chrome did not start')
   client=MinimalCDPClient(tab['webSocketDebuggerUrl'])
   def call(method,params=None):
    reply=client.recv_cdp(client.send_cdp(method,params));
    if not reply or 'error' in reply:raise RuntimeError(str(reply))
    return reply.get('result',{})
   def evaluate(code):return client.eval(code,await_promise=True)
   def wait_for(expr):
    for _ in range(100):
     if evaluate(expr):return
     time.sleep(.05)
    raise AssertionError('Missing UI: '+expr)
   def size(w,h):call('Emulation.setDeviceMetricsOverride',dict(width=w,height=h,deviceScaleFactor=1,mobile=True))
   def shot(name):
    data=call('Page.captureScreenshot',{'format':'png','captureBeyondViewport':False})['data'];(out/(name+'.png')).write_bytes(base64.b64decode(data))
   def check(name):
    report=evaluate("""(()=>({strayZeros:[...document.querySelectorAll('.connection-panel')].some(e=>[...e.childNodes].some(n=>n.nodeType===3&&n.textContent.trim()==='0')),overflow:document.documentElement.scrollWidth>innerWidth+1,width:innerWidth,scrollWidth:document.documentElement.scrollWidth,smallTargets:[...document.querySelectorAll('button')].filter(x=>x.getBoundingClientRect().width>0&&x.getBoundingClientRect().height<44).map(x=>x.textContent.trim()),headings:[...document.querySelectorAll('h1,h2')].map(x=>x.textContent)}))()""")
    report['scenario']=name;results.append(report)
    if report['overflow'] or report['smallTargets'] or report['strayZeros']:raise AssertionError(json.dumps(report,ensure_ascii=False))
   size(390,844);call('Page.navigate',{'url':args.url});wait_for('!!window.controller');shot('01-no-qr');check('no QR')
   evaluate("""(()=>{const s=window.controller;s.setPlayerName=name=>{(window.__nameRequests??=[]).push(name);return new Promise(r=>window.__saveResolve=r)};s.network='NETWORK_READY';s.state='PERMISSION_REQUIRED';s.client.identity={playerId:1};s.client.gameContext={version:3,gameId:'lobby',screen:'connection',playerId:0,phase:'Ready',phoneRequired:true,hand:'right',contextId:'a'.repeat(32)};s.client.gameContextExpires=performance.now()+600000;s.publish()})()""")
   wait_for("!!document.querySelector('.profile-setup input')");evaluate("document.querySelector('.profile-setup input').focus()");call('Input.insertText',{'text':'Josué'});time.sleep(.15)
   assert evaluate('(window.__nameRequests||[]).length')==0,'typing a name must not submit it'
   evaluate("document.querySelector('.profile-setup button').click()");wait_for('!!window.__saveResolve')
   assert evaluate("!!document.querySelector('.profile-setup input')"),'name form disappeared before confirmation'
   shot('02-name-awaiting-confirmation');evaluate("window.__saveResolve('SAVED')");wait_for("!!document.querySelector('.player-identity-bar')")
   assert evaluate("document.querySelectorAll('.game-card').length")==0,'connection must not offer game navigation'
   shot('03-connection-preparation');check('connection gate')
   evaluate("window.controller.setPlayerName=async()=> 'SAVED';document.querySelector('.edit-name-pill').click()")
   wait_for("!!document.querySelector('.inline-name-edit input')");evaluate("document.querySelector('.inline-name-edit input').focus()");call('Input.dispatchKeyEvent',{'type':'keyDown','key':'a','code':'KeyA','windowsVirtualKeyCode':65,'modifiers':2});call('Input.dispatchKeyEvent',{'type':'keyUp','key':'a','code':'KeyA','windowsVirtualKeyCode':65,'modifiers':2});call('Input.insertText',{'text':'Jugador con nombre largo de cuarenta 1234'});evaluate("document.querySelector('.inline-name-edit button').click()");wait_for("!document.querySelector('.inline-name-edit')")
   def context(game,phase='Playing',player=1,screen='game'):
    ctx=dict(version=3,gameId=game,phase=phase,playerId=0 if game=='lobby' else player,screen=screen,phoneRequired=game not in ['lobby','coconut-slice','jungle-keeper'],hand='right',contextId='b'*32,stats=dict(hasScore=True,score=123,hasProgress=True,progress=.4,hasElapsed=True,elapsed=33,hasBalance=False,balance=0,hasDistance=False,distance=0))
    evaluate("(()=>{const s=window.controller;s.client.gameContext="+json.dumps(ctx)+";s.client.gameContextExpires=performance.now()+600000;s.network='NETWORK_READY';s.state='INPUT_READY';s.publish()})()")
    time.sleep(.08)
   games=['bowling','boxing','baseball','drums','coconut-slice','jungle-keeper','whack','log-balance','gorilla-smash','coconut-throw']
   for w,h in [(320,568),(390,844),(844,390)]:
    size(w,h);evaluate('window.controller.client.identity.playerId=1');context('lobby',screen='menu');check(f'menu {w}x{h}');shot(f'menu-{w}x{h}')
    assert evaluate("document.querySelectorAll('.game-card').length")==10
    for game in games:
     for phase in ['Ready','Playing','Paused','Finished']:
      context(game,phase);check(f'{game} {phase} {w}x{h}')
      if w==390 and phase in ['Playing','Paused']:shot(f'{game}-{phase.lower()}')
    evaluate('window.controller.client.identity.playerId=2');context('drums','Paused',player=1);check(f'spectator {w}x{h}')
    assert evaluate("document.querySelectorAll('.pause-pill-btn,.mobile-pause-overlay-card button').length")==0,'spectators must not see authority controls'
   size(320,568);evaluate("document.documentElement.style.fontSize='24px'");context('lobby',screen='menu');check('menu 150% text');shot('menu-large-text')
   evaluate("document.documentElement.style.fontSize='16px'")
   for state,network in [('DISCONNECTED','DISCONNECTED'),('RECONNECTING','RECONNECTING'),('ERROR','NOT_CONNECTED'),('FORFEITED','FORFEITED')]:
    evaluate("(()=>{const s=window.controller;s.network="+json.dumps(network)+";s.state="+json.dumps(state)+";s.publish()})()");time.sleep(.1);check(state);shot('state-'+state.lower())
   (out/'results.json').write_text(json.dumps(dict(scope='Synthetic browser presentation and interactions; no physical sensor or WAN acceptance',status='PASS',checks=results),ensure_ascii=False,indent=2),encoding='utf-8')
   print('PASS',len(results),'browser layout/state checks; screenshots:',out)
  finally:
   if client:client.close()
   if os.name=='nt':subprocess.run(['taskkill','/PID',str(proc.pid),'/T','/F'],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
   else:proc.terminate()
   try:proc.wait(timeout=10)
   except subprocess.TimeoutExpired:proc.kill();proc.wait()
   time.sleep(.5)
if __name__=='__main__':main()
