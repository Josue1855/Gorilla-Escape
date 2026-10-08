"""Owned desktop preparation smoke, no phone/physical offline claims."""
import argparse,hashlib,http.server,json,os,secrets,selectors,subprocess,tempfile,threading,time,sys,signal
from pathlib import Path
ROOT=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT.parent))
from run import cert,stop
from prepare import JAVA

def start(directory,bind,sid):
    token=secrets.token_hex(32);key=directory/'credential';key.write_text(token);key.chmod(0o600)
    cp=str(ROOT.parent/'target/classes')+os.pathsep+(ROOT.parent/'target/classpath.txt').read_text().strip()
    p=subprocess.Popen([str(JAVA/'bin/java'),'-cp',cp,'com.gorillaescape.spike.PhysicalServer',str(directory/'cert.pem'),str(directory/'key.pem'),bind],stdin=subprocess.PIPE,stdout=subprocess.PIPE,stderr=subprocess.PIPE,env=dict(os.environ,WT_SECRET_FILE=str(key),WT_SESSION_ID=sid))
    def drain():
        while p.stderr.read(4096):pass
    threading.Thread(target=drain,daemon=True).start()
    ready=selectors.DefaultSelector();ready.register(p.stdout,selectors.EVENT_READ)
    try:
        if not ready.select(15):raise TimeoutError('startup')
        line=p.stdout.readline().decode();assert line.startswith('READY ')
        p.wt_port=int(line.split()[1]);p.wt_bind=bind;return p,token
    except:
        p.kill();p.communicate(timeout=5);raise
    finally:ready.close()

class Quiet(http.server.SimpleHTTPRequestHandler):
    def __init__(self,*a,**kw):super().__init__(*a,directory=str(ROOT/'public'),**kw)
    def log_message(self,*args):pass

def main():
    parser=argparse.ArgumentParser();parser.add_argument('--output',type=Path,required=True);a=parser.parse_args()
    if a.output.exists():raise SystemExit('Use a new output file')
    server=http.server.ThreadingHTTPServer(('127.0.0.1',0),Quiet);thread=threading.Thread(target=server.serve_forever,daemon=True);thread.start();processes=[];result={}
    with tempfile.TemporaryDirectory(prefix='gorilla-physical-check-') as t:
        try:
            private=Path(t);sid=secrets.token_hex(8);pins=[]
            for name,expired in [('valid',False),('expired',True)]:
                d=private/name;pin=cert(d,expired,'127.0.0.1');p,token=start(d,'127.0.0.1',sid);processes.append(p);pins.append(pin)
                if name=='valid':config={'url':f'https://127.0.0.1:{p.wt_port}/probe','hash':pin,'sessionId':sid,'token':token}
                else:config.update(expiredUrl=f'https://127.0.0.1:{p.wt_port}/probe',expiredHash=pin,otherHash=pin)
            c=private/'config.json';c.write_text(json.dumps(config));c.chmod(0o600);out=private/'result.json'
            env=dict(os.environ,WT_PLAYWRIGHT_MODULE=os.environ.get('WT_PLAYWRIGHT_MODULE',str(Path.home()/'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright-core/index.mjs')))
            browser=subprocess.Popen(['node',str(ROOT/'check.mjs'),str(c),str(out),f'http://127.0.0.1:{server.server_address[1]}/'],env=env,start_new_session=True)
            try:
                code=browser.wait(timeout=150)
            finally:
                try:os.killpg(browser.pid,0);result['browserResidual']=True;os.killpg(browser.pid,signal.SIGKILL);browser.wait(timeout=5)
                except ProcessLookupError:result['browserResidual']=False
            if out.exists():result.update(json.loads(out.read_text()))
            if code!=0:result['pass']=False
        except Exception as e:result.update({'pass':False,'harnessError':type(e).__name__})
        finally:result['cleanup']=[stop(p) for p in processes];server.shutdown();server.server_close();thread.join(timeout=2)
    result['pass']=result.get('pass',False) and not result.get('browserResidual',True) and all(c['exitCode']==0 and not c['forced'] and c['udpPortReleased'] and not c['residual'] for c in result['cleanup'])
    a.output.parent.mkdir(parents=True,exist_ok=True);a.output.write_text(json.dumps(result,indent=2)+'\n');print(json.dumps({'pass':result['pass'],'cleanup':result['cleanup']}));raise SystemExit(0 if result['pass'] else 1)
if __name__=='__main__':main()
