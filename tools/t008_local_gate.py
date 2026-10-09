#!/usr/bin/env python3
"""DEC-016 operator gate. Private keys stay in Java's installation store, never in output.
Run from any directory. `admission` requests a new 30 s QR only when the prepared phone is ready.
`quit` closes the owned Player normally. No network/firewall/trust-store modification.
"""
import argparse, base64, hashlib, json, os, secrets, subprocess, sys, threading, time, urllib.request, ssl
from pathlib import Path
REPO=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(REPO/'tools'))
from validate_ipc_2b_player import X11

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--trust-dir',type=Path,required=True)
    parser.add_argument('--output',type=Path,required=True)
    parser.add_argument('--java',type=Path,required=True)
    parser.add_argument('--interface',required=True)
    parser.add_argument('--address',required=True)
    parser.add_argument('--https-port',type=int,default=8443)
    parser.add_argument('--bootstrap-port',type=int,default=0)
    args=parser.parse_args()
    output=args.output.resolve();output.mkdir(mode=0o700,parents=True,exist_ok=False)
    operator=secrets.token_urlsafe(32);env=os.environ.copy()
    env.pop('GORILLA_MOBILE_CONFIG',None);env.pop('GORILLA_MOBILE_LAB',None)
    env.update(GORILLA_IPC_JAVA=str(args.java.resolve()),GORILLA_IPC_JAR=str(REPO/'Server/target/local-server-0.1.0-SNAPSHOT.jar'),GORILLA_IPC_MODE='lifecycle',GORILLA_PHONE_INPUT='1',GORILLA_MOBILE_OPERATOR=operator,GORILLA_LOCAL_TRUST_DIR=str(args.trust_dir.resolve()),GORILLA_LAN_INTERFACE=args.interface,GORILLA_LAN_ADDRESS=args.address,GORILLA_HTTPS_PORT=str(args.https_port),GORILLA_TRUST_BOOTSTRAP_PORT=str(args.bootstrap_port))
    origin=f'https://{args.address}:{args.https_port}'
    player=subprocess.Popen([str(REPO/'Unity/Builds/FoundationLinux/GorillaEscape.x86_64'),'-screen-fullscreen','0','-screen-width','640','-screen-height','360','-logFile','-'],env=env,stdout=subprocess.PIPE,stderr=subprocess.DEVNULL,text=True)
    events=[];ready=threading.Event()
    def drain():
        for line in player.stdout:
            if line.startswith('PHONE_LAB_HTTP_READY '):ready.set()
            if line.startswith('{"component":"ipc-unity"') and len(events)<100:
                try:events.append(json.loads(line))
                except ValueError:pass
    threading.Thread(target=drain,daemon=True).start()
    summary={'scope':'DEC-016 physical gate; trust preparation and join measured separately','origin':origin,'physical':'NOT RUN','timings':'NOT MEASURED'}
    try:
        end=time.monotonic()+20
        while not ready.wait(.2):
            if player.poll() is not None or time.monotonic()>end:raise RuntimeError('STARTUP_NOT_READY')
        ca=args.trust_dir.resolve()/'root.cer';context=ssl.create_default_context(cafile=None)
        # Add only this installation's CA to the default validating context. Host/IP checks remain enabled.
        import ssl as ssl_module
        der=ca.read_bytes();context.load_verify_locations(cadata=ssl_module.DER_cert_to_PEM_cert(der))
        summary['caSha256']=hashlib.sha256(der).hexdigest()
        with urllib.request.urlopen(origin+'/prepare',context=context,timeout=5) as response:
            assert response.status==200
        print(json.dumps({'event':'READY','origin':origin,'caSha256':summary['caSha256'],'prepareUrl':f'http://{args.address}:{args.bootstrap_port}/prepare' if args.bootstrap_port else origin+'/prepare'}),flush=True)
        for command in sys.stdin:
            command=command.strip()
            if command=='quit':break
            if command=='admission':
                request=urllib.request.Request(origin+'/mobile/admission',method='POST',headers={'X-Gorilla-Operator':operator})
                with urllib.request.urlopen(request,context=context,timeout=5) as response:admission=json.load(response)
                for old in output.glob('join-qr-*.png'):old.unlink()
                qr=output/f'join-qr-{time.monotonic_ns()}.png'
                qr.write_bytes(base64.b64decode(admission['qrPng']))
                # URLs/admission are transient display data, not historical evidence. Do not print/save the URL.
                print(json.dumps({'event':'QR_READY','image':str(qr),'expiresInSeconds':admission['expiresInSeconds'],'issuedMonotonic':time.monotonic()}),flush=True)
            if command=='diagnostics':
                request=urllib.request.Request(origin+'/mobile/diagnostics',headers={'X-Gorilla-Operator':operator})
                with urllib.request.urlopen(request,context=context,timeout=5) as response:diagnostics=json.load(response)
                # Only server-enforced bounded metadata; never include SDP or peer identities.
                report={'joinAttempts':diagnostics['joinAttempts'],'peers':diagnostics['peers']}
                (output/'join-diagnostics.json').write_text(json.dumps(report,indent=2)+'\n')
                print(json.dumps({'event':'JOIN_DIAGNOSTICS',**report}),flush=True)
    finally:
        if player.poll() is None:
            try:
                X11().close(player.pid);player.wait(timeout=15)
            except Exception:
                summary['forcedPlayer']=True
                player.terminate()
                try:player.wait(timeout=10)
                except subprocess.TimeoutExpired:
                    player.kill();player.wait(timeout=5)
        own=[e['pid'] for e in events if e.get('event')=='PROCESS_STARTED']
        summary.update(playerExit=player.returncode,javaResiduals=sum(Path('/proc',str(pid)).exists() for pid in own),cleanup=[{k:e.get(k) for k in ('state','exitCode','cleanupComplete','forced')} for e in events if e.get('event')=='STOPPED'])
        # Remove transient QR/admission on close, retain only sanitized operational summary.
        for qr in output.glob('join-qr*.png'):qr.unlink()
        (output/'summary.json').write_text(json.dumps(summary,indent=2)+'\n')
        print(json.dumps({'event':'CLOSED','playerExit':summary['playerExit'],'javaResiduals':summary['javaResiduals']}),flush=True)
if __name__=='__main__':main()
