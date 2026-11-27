"""Temporary iPhone LAN lab. No network, router, DNS or system trust changes."""
import argparse,base64,datetime,hashlib,http.client,ipaddress,json,os,secrets,selectors,signal,socket,ssl,subprocess,sys,tempfile,threading,time
from pathlib import Path
from cryptography import x509
from cryptography.hazmat.primitives import hashes,serialization
from cryptography.hazmat.primitives.asymmetric import ec
from cryptography.hazmat.primitives.serialization import pkcs12
from cryptography.x509.oid import NameOID,ExtendedKeyUsageOID
ROOT=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT))
from check import start
sys.path.insert(0,str(ROOT.parent))
from run import cert,stop
from prepare import JAVA

def pki(d,bind):
    now=datetime.datetime.now(datetime.timezone.utc)
    key=ec.generate_private_key(ec.SECP256R1())
    name=x509.Name([x509.NameAttribute(NameOID.COMMON_NAME,'Gorilla Escape TEMPORARY LAB — REMOVE AFTER TEST')])
    ca=(x509.CertificateBuilder().subject_name(name).issuer_name(name).public_key(key.public_key())
        .serial_number(x509.random_serial_number()).not_valid_before(now-datetime.timedelta(minutes=5))
        .not_valid_after(now+datetime.timedelta(hours=2)).add_extension(x509.BasicConstraints(ca=True,path_length=0),True)
        .add_extension(x509.KeyUsage(False,False,False,False,False,True,True,False,False),True).sign(key,hashes.SHA256()))
    leafkey=ec.generate_private_key(ec.SECP256R1())
    leaf=(x509.CertificateBuilder().subject_name(x509.Name([x509.NameAttribute(NameOID.COMMON_NAME,'Gorilla local lab')]))
        .issuer_name(name).public_key(leafkey.public_key()).serial_number(x509.random_serial_number())
        .not_valid_before(now-datetime.timedelta(minutes=5)).not_valid_after(now+datetime.timedelta(hours=2))
        .add_extension(x509.BasicConstraints(ca=False,path_length=None),True)
        .add_extension(x509.SubjectAlternativeName([x509.IPAddress(ipaddress.IPv4Address(bind))]),False)
        .add_extension(x509.ExtendedKeyUsage([ExtendedKeyUsageOID.SERVER_AUTH]),False).sign(key,hashes.SHA256()))
    password=secrets.token_hex(24)
    for n,b in [('gorilla-lab.cer',ca.public_bytes(serialization.Encoding.DER)),
                ('ca.pem',ca.public_bytes(serialization.Encoding.PEM)),('password',password.encode()),
                ('server.p12',pkcs12.serialize_key_and_certificates(b'lab',leafkey,leaf,[ca],serialization.BestAvailableEncryption(password.encode())))]:
        (d/n).write_bytes(b);(d/n).chmod(0o600)
    return ca.fingerprint(hashes.SHA256()).hex()

def main():
    ap=argparse.ArgumentParser();ap.add_argument('--bind',required=True);ap.add_argument('--output',type=Path,required=True)
    ap.add_argument('--minutes',type=int,default=30)
    ap.add_argument('--https-port',type=int,default=8443);ap.add_argument('--ca-port',type=int,default=8000)
    ap.add_argument('--single-wt',action='store_true');a=ap.parse_args()
    address=ipaddress.IPv4Address(a.bind)
    if not address.is_private or address.is_loopback or address.is_unspecified or address.is_multicast:raise SystemExit('explicit LAN IPv4 required')
    if not 1<=a.minutes<=30:raise SystemExit('bounded duration required')
    if not all(1024<=p<=65535 for p in (a.https_port,a.ca_port)) or a.https_port==a.ca_port:raise SystemExit('distinct unprivileged ports required')
    if a.output.exists():raise SystemExit('new output path required')
    stopped=threading.Event()
    for sig in (signal.SIGTERM,signal.SIGINT,signal.SIGHUP):signal.signal(sig,lambda *_:stopped.set())
    processes=[];host=None;result={'scope':'temporary CA laboratory on existing Wi-Fi; physical outcomes require phone evidence','physical':'NOT RUN','networkChanges':False,'systemTrustChanges':False,'cleanup':[]}
    with tempfile.TemporaryDirectory(prefix='gorilla-iphone-lan-') as t:
        d=Path(t);d.chmod(0o700)
        try:
            fingerprint=pki(d,a.bind);sid=secrets.token_hex(8)
            for label,expired in [('valid',False),('expired',True)]:
                sub=d/label;pin=cert(sub,expired,a.bind)
                if expired and a.single_wt:
                    config['otherHash']=pin;continue
                p,token=start(sub,a.bind,sid);processes.append(p)
                if not expired:config={'url':f'https://{a.bind}:{p.wt_port}/probe','hash':pin,'token':token,'sessionId':sid}
                else:config.update(expiredUrl=f'https://{a.bind}:{p.wt_port}/probe',expiredHash=pin,otherHash=pin)
            (d/'config.json').write_text(json.dumps(config));(d/'config.json').chmod(0o600)
            subprocess.run([str(JAVA/'bin/javac'),'-d',str(d),str(ROOT/'LabHttps.java')],check=True,capture_output=True)
            host=subprocess.Popen([str(JAVA/'bin/java'),'-cp',str(d),'LabHttps',str(d),str(ROOT/'public'),a.bind,str(a.https_port),str(a.ca_port)],stdin=subprocess.PIPE,stdout=subprocess.PIPE,stderr=subprocess.PIPE)
            def drain():
                while host.stderr.read(4096):pass
            threading.Thread(target=drain,daemon=True).start()
            ready=selectors.DefaultSelector();ready.register(host.stdout,selectors.EVENT_READ)
            try:
                if not ready.select(15) or not host.stdout.readline().startswith(b'READY '):raise RuntimeError('HTTPS readiness failed')
            finally:ready.close()
            context=ssl.create_default_context(cafile=str(d/'ca.pem'))
            connection=http.client.HTTPSConnection(a.bind,a.https_port,context=context,timeout=5)
            try:
                connection.request('GET','/');response=connection.getresponse();page=response.read()
                if response.status!=200 or b'cfg=' not in page:raise RuntimeError('Java HTTPS check failed')
            finally:connection.close()
            result['hostOwnTlsCheck']='PASS';result['caFingerprintSHA256']=fingerprint
            print(f'READY: Java HTTPS https://{a.bind}:{a.https_port}/ ; CA http://{a.bind}:{a.ca_port}/gorilla-lab.cer',flush=True)
            print('CA SHA256: '+fingerprint,flush=True)
            print('WT UDP: '+str(processes[0].wt_port)+' ; owner PID: '+str(os.getpid()),flush=True)
            print('No hotspot. No DNS. No WAN change. STOP or Ctrl+C closes owned processes; automatic stop after '+str(a.minutes)+' minutes.',flush=True)
            def commands():
                for line in sys.stdin:
                    if line.strip()=='STOP':stopped.set();return
                stopped.set()
            threading.Thread(target=commands,daemon=True).start()
            stopped.wait(a.minutes*60)
        except Exception as e:
            result['harnessError']=type(e).__name__;print('LAB ERROR '+type(e).__name__,flush=True)
        finally:
            if host:
                if host.stdin and not host.stdin.closed:host.stdin.close()
                forced=False
                try:host.wait(timeout=5)
                except subprocess.TimeoutExpired:forced=True;host.kill();host.wait(timeout=5)
                result['httpsCleanup']={'exitCode':host.returncode,'forced':forced,'residual':host.poll() is None}
            result['cleanup']=[stop(p) for p in processes]
    result['privateFilesRemoved']=not d.exists()
    ports={}
    for port in [a.ca_port,a.https_port]:
        with socket.socket() as s:
            s.setsockopt(socket.SOL_SOCKET,socket.SO_REUSEADDR,1)
            try:s.bind((a.bind,port));ports[str(port)]='FREE'
            except OSError:ports[str(port)]='IN USE'
    result['tcpPortsAfterCleanup']=ports
    result['iphoneProfileRemoval']='REQUIRES OWNER CONFIRMATION if installed'
    a.output.parent.mkdir(parents=True,exist_ok=True);a.output.write_text(json.dumps(result,indent=2)+'\n')
    print('STOPPED — cleanup evidence saved; remove temporary CA profile on iPhone if installed.',flush=True)

if __name__=='__main__':main()
