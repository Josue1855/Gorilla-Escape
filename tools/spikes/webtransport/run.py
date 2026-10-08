"""Private ephemeral PKI + owned Java/Chrome processes; sanitized results only."""
import argparse, datetime, hashlib, ipaddress, json, os, secrets, selectors, socket, subprocess, signal, tempfile, time, threading
from pathlib import Path
from cryptography import x509
from cryptography.hazmat.primitives import hashes, serialization
from cryptography.hazmat.primitives.asymmetric import ec
from cryptography.x509.oid import NameOID, ExtendedKeyUsageOID
from prepare import JAVA, CACHE, SHA
ROOT=Path(__file__).resolve().parent

def cert(directory,expired,bind):
    key=ec.generate_private_key(ec.SECP256R1()); now=datetime.datetime.now(datetime.timezone.utc)
    start=now-datetime.timedelta(days=3 if expired else 1)
    end=now-datetime.timedelta(days=1) if expired else now+datetime.timedelta(days=1)
    name=x509.Name([x509.NameAttribute(NameOID.COMMON_NAME,'isolated-wt-lab')])
    c=x509.CertificateBuilder().subject_name(name).issuer_name(name).public_key(key.public_key()).serial_number(x509.random_serial_number()).not_valid_before(start).not_valid_after(end).add_extension(x509.BasicConstraints(ca=False,path_length=None),critical=True).add_extension(x509.SubjectAlternativeName([x509.IPAddress(ipaddress.IPv4Address(bind)),x509.DNSName('localhost')]),critical=False).add_extension(x509.ExtendedKeyUsage([ExtendedKeyUsageOID.SERVER_AUTH]),critical=False).sign(key,hashes.SHA256())
    directory.mkdir(mode=0o700)
    for file,data in [('cert.pem',c.public_bytes(serialization.Encoding.PEM)),('key.pem',key.private_bytes(serialization.Encoding.PEM,serialization.PrivateFormat.PKCS8,serialization.NoEncryption()))]:
        (directory/file).write_bytes(data);(directory/file).chmod(0o600)
    import base64
    return base64.b64encode(c.fingerprint(hashes.SHA256())).decode()

def start_java(directory,bind):
    token=secrets.token_hex(32);secret=directory/'credential';secret.write_text(token);secret.chmod(0o600)
    cp=str(ROOT/'target/classes')+os.pathsep+(ROOT/'target/classpath.txt').read_text().strip()
    command=[str(JAVA/'bin/java'),'-cp',cp,'com.gorillaescape.spike.ProbeServer',str(directory/'cert.pem'),str(directory/'key.pem'),bind]
    process=subprocess.Popen(command,stdin=subprocess.PIPE,stdout=subprocess.PIPE,stderr=subprocess.PIPE,env=dict(os.environ,WT_SECRET_FILE=str(secret)))
    # Drain stderr without retaining potentially sensitive native diagnostic logs.
    def drain():
        while process.stderr.read(4096): pass
    threading.Thread(target=drain,daemon=True).start()
    selector=selectors.DefaultSelector();selector.register(process.stdout,selectors.EVENT_READ)
    deadline=time.monotonic()+15
    try:
        while time.monotonic()<deadline:
            if selector.select(max(0,deadline-time.monotonic())):
                line=process.stdout.readline().decode()
                if line.startswith('READY '):
                    process.wt_port=int(line.split()[1]);process.wt_bind=bind
                    return process,process.wt_port,token
                if process.poll() is not None:raise RuntimeError('Java startup exit '+str(process.returncode))
        raise TimeoutError('Java readiness')
    except:
        if process.poll() is None:process.kill()
        process.communicate(timeout=5);raise
    finally:selector.close()

def stop(process):
    started=time.monotonic();forced=False
    if process.stdin and not process.stdin.closed:process.stdin.close()
    try:process.wait(timeout=5)
    except subprocess.TimeoutExpired: forced=True;process.kill();process.wait(timeout=5)
    lines=process.stdout.read().decode()
    socket_free=False
    with socket.socket(socket.AF_INET,socket.SOCK_DGRAM) as check:
        try:check.bind((process.wt_bind,process.wt_port));socket_free=True
        except OSError:pass
    return {'exitCode':process.returncode,'forced':forced,'shutdownMs':(time.monotonic()-started)*1000,'residual':process.poll() is None,'udpPortReleased':socket_free,'serverSummary':next((l for l in lines.splitlines() if l.startswith('STOPPED ')),None)}

def main():
    parser=argparse.ArgumentParser();parser.add_argument('--bind',default='127.0.0.1');parser.add_argument('--output',type=Path,required=True);args=parser.parse_args()
    address=ipaddress.IPv4Address(args.bind)
    if not (address.is_private and not address.is_unspecified and not address.is_multicast):raise ValueError('explicit local IPv4 only')
    result={'date':datetime.datetime.now(datetime.timezone.utc).date().isoformat(),'scope':'real desktop Chrome + Java21; local endpoint on same host','bindKind':'loopback' if address.is_loopback else 'LAN-address-on-same-host','dependencyCommit':SHA,'java':subprocess.check_output([str(JAVA/'bin/java'),'-version'],stderr=subprocess.STDOUT,text=True).splitlines()[0], 'android':'NOT RUN','iphone':'NOT RUN','offlinePwa':'NOT RUN','windows':'NOT RUN'}
    with tempfile.TemporaryDirectory(prefix='gorilla-wt-private-') as temp:
        private=Path(temp);processes=[]
        try:
            cases=[]
            for label,expired in [('valid',False),('expired',True)]:
                directory=private/label;pin=cert(directory,expired,args.bind);t=time.monotonic();p,port,token=start_java(directory,args.bind);processes.append(p)
                result[label+'StartupMs']=(time.monotonic()-t)*1000
                url='https://'+args.bind+':'+str(port)+'/probe'
                if expired:cases.append({'name':'expired-certificate','expected':'REJECTED','url':url,'hash':pin,'token':token})
                else:
                    for name,patch,expected in [('correct-certificate',{},'PASS'),('wrong-fingerprint',{'badPin':True},'REJECTED'),('unauthorized-session',{'token':'0'*64},'REJECTED'),('oversize',{'oversize':True},'REJECTED'),('manual-recovery',{},'PASS')]:
                        cases.append({'name':name,'expected':expected,'url':url,'hash':pin,'token':token,**patch})
            config=private/'config.json';config.write_text(json.dumps({'cases':cases}));config.chmod(0o600)
            browserResult=private/'browser.json'
            env=dict(os.environ)
            if 'WT_PLAYWRIGHT_MODULE' not in env:
                env['WT_PLAYWRIGHT_MODULE']=str(Path.home()/'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright-core/index.mjs')
            browserProcess=subprocess.Popen(['node',str(ROOT/'browser.mjs'),str(config),str(browserResult)],env=env,start_new_session=True)
            try:
                if browserProcess.wait(timeout=100)!=0:raise RuntimeError('browser harness failed')
            finally:
                # Only the process group created by this harness; never unrelated browsers.
                try:os.killpg(browserProcess.pid,0); remaining=True
                except ProcessLookupError:remaining=False
                result['browserProcessGroupResidual']=remaining
                if remaining:
                    os.killpg(browserProcess.pid,signal.SIGKILL)
                    browserProcess.wait(timeout=5)
            result.update(json.loads(browserResult.read_text()))
        except Exception as error:
            result['harnessError']=type(error).__name__ # no raw diagnostics or credentials
        finally:result['cleanup']=[stop(p) for p in processes]
    expectedStages={'wrong-fingerprint':'CONNECTING','expired-certificate':'CONNECTING','unauthorized-session':'AUTHENTICATING','oversize':'OVERSIZE'}
    for case in result.get('results',[]):
        case['casePass']=case['status']==case['expected'] and (
            case.get('invalidRejected') is True if case['expected']=='PASS' else
            case.get('stage')==expectedStages[case['name']] and not case.get('deadline',True))
    result['pass']=not result.get('browserProcessGroupResidual',True) and len(result.get('results',[]))==6 and all(x['casePass'] for x in result['results']) and all(c['exitCode']==0 and not c['forced'] and not c['residual'] and c['udpPortReleased'] for c in result['cleanup'])
    args.output.parent.mkdir(parents=True,exist_ok=True);args.output.write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps({'pass':result['pass'],'cases':[(r['name'],r['status']) for r in result.get('results',[])],'cleanup':result['cleanup']}))
    if not result['pass']:raise SystemExit(1)
if __name__=='__main__':main()
