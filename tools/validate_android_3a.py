#!/usr/bin/env python3
"""Isolated Android/Chrome component smoke. No LAN/public-TLS acceptance.
Uses installed SDK/image and websockets; never installs dependencies or modifies product code.
"""
import argparse
import asyncio
import base64
import hashlib
import json
import os
from pathlib import Path
import queue
import re
import secrets
import signal
import socket
import subprocess
import tempfile
import threading
import time
import urllib.request
import uuid
import xml.etree.ElementTree as ET


class CommandFailure(RuntimeError):
    pass


class Blocked(Exception):
    pass


def port():
    with socket.socket() as sock:
        sock.bind(('127.0.0.1', 0))
        return sock.getsockname()[1]


def command(args, env=None, timeout=15, input=None):
    result = subprocess.run([str(a) for a in args], env=env, input=input,
                            capture_output=True, timeout=timeout)
    if result.returncode:
        # Raw diagnostics stay in ignored output. Do not surface arbitrary environment/log data.
        raise CommandFailure('Command failed: ' + Path(str(args[0])).name + ': ' + (result.stderr.decode(errors='replace').splitlines() or ['exit '+str(result.returncode)])[0][:100])
    return result.stdout


def wait(check, deadline, process=None):
    end = time.monotonic() + deadline
    while time.monotonic() < end:
        if process is not None and process.poll() is not None:
            raise RuntimeError('Owned process exited before readiness')
        try:
            result = check()
            if result:
                return result
        except (OSError, subprocess.SubprocessError, CommandFailure):
            pass
        time.sleep(.25)  # Poll interval only; readiness is checked, never assumed.
    raise TimeoutError('Readiness deadline exceeded')


class CDP:
    def __init__(self, connection):
        self.connection = connection
        self.sequence = 0

    async def call(self, method, params=None):
        self.sequence += 1
        sequence = self.sequence
        await self.connection.send(json.dumps(dict(id=sequence, method=method, params=params or {})))
        async with asyncio.timeout(15):
            while True:
                message = json.loads(await self.connection.recv())
                if message.get('id') == sequence:
                    if 'error' in message:
                        raise RuntimeError('CDP command failed: ' + method)
                    return message.get('result', {})

    async def evaluate(self, expression):
        result = await self.call('Runtime.evaluate', dict(expression=expression, returnByValue=True, awaitPromise=True))
        if 'exceptionDetails' in result:
            raise RuntimeError('Browser evaluation failed')
        return result.get('result', {}).get('value')

    async def until(self, expression, deadline=10):
        end = time.monotonic() + deadline
        while time.monotonic() < end:
            value = await self.evaluate(expression)
            if value:
                return value
            await asyncio.sleep(.2)
        raise TimeoutError('Browser assertion deadline exceeded')

    async def reload(self):
        before = (await self.call('Page.getFrameTree'))['frameTree']['frame'].get('loaderId')
        await self.call('Page.reload', dict(ignoreCache=True))
        end = time.monotonic()+15
        while time.monotonic()<end:
            current = (await self.call('Page.getFrameTree'))['frameTree']['frame'].get('loaderId')
            if current and current != before:
                break
            await asyncio.sleep(.2)
        else:
            raise TimeoutError('Reload loader did not change')
        await self.until("document.readyState === 'complete'", deadline=15)

    async def navigate(self, url):
        await self.call('Page.enable')
        await self.call('Page.bringToFront')
        result = await self.call('Page.navigate', dict(url=url))
        if result.get('errorText'):
            return result
        loader = result.get('loaderId')
        end = time.monotonic()+15
        while time.monotonic()<end:
            frame = await self.call('Page.getFrameTree')
            if not loader or frame['frameTree']['frame'].get('loaderId')==loader:
                break
            await asyncio.sleep(.2)
        else:
            raise TimeoutError('Navigation loader did not commit')
        await self.until("document.readyState === 'complete'", deadline=15)
        return result


class Run:
    def __init__(self, args, directory):
        self.args, self.directory = args, directory
        self.report = dict(scope='Android emulator component/browser tests; not physical LAN or public trust', gate3A='IN PROGRESS', cases=[], cleanup={})
        self.processes = []
        self.java = None
        self.env = os.environ.copy()
        self.env.update(ANDROID_HOME=str(args.sdk), ANDROID_SDK_ROOT=str(args.sdk),
                        ANDROID_USER_HOME=str(directory/'android'), ANDROID_AVD_HOME=str(directory/'avds'),
                        ANDROID_EMULATOR_HOME=str(directory/'emulator'), JAVA_HOME=str(args.java.parent.parent))
        self.adb = args.sdk/'platform-tools/adb'
        self.adb_port = port()
        self.env.update(ANDROID_ADB_SERVER_PORT=str(self.adb_port), ADB_SERVER_SOCKET=f'tcp:localhost:{self.adb_port}')
        self.serial = None
        self.logs = []
        self.descendants = {}

    def result(self, name, status, detail=''):
        self.report['cases'].append(dict(name=name, status=status, detail=detail))

    def spawn(self, args, name, env=None, stdin=None):
        log = open(self.args.output/(name+'.log'), 'wb')
        self.logs.append(log)
        process = subprocess.Popen([str(a) for a in args], env=env or self.env, stdin=stdin,
                                   stdout=log, stderr=subprocess.STDOUT)
        self.processes.append((name, process))
        return process

    def adb_call(self, *args, serial=True, timeout=15):
        prefix = [self.adb, '-P', str(self.adb_port)]
        if serial and self.serial:
            prefix += ['-s', self.serial]
        return command(prefix + list(args), self.env, timeout=timeout)

    def start_android(self):
        for name in ['android', 'avds', 'emulator']:
            (self.directory/name).mkdir()
        adb_server = self.spawn([self.adb, '-L', f'tcp:localhost:{self.adb_port}', 'server', 'nodaemon'], 'adb')
        wait(lambda: socket.create_connection(('127.0.0.1', self.adb_port), .3).close() or True, 5, adb_server)
        avd = self.directory/'avds/Gorilla3A.avd'
        command([self.args.sdk/'cmdline-tools/latest/bin/avdmanager', 'create', 'avd', '-n', 'Gorilla3A',
                 '-p', avd, '-k', 'system-images;android-36;google_apis;x86_64', '-d', 'pixel_5'],
                self.env, timeout=45, input=b'no\n')
        config = avd/'config.ini'
        values = dict(line.split('=',1) for line in config.read_text().splitlines() if '=' in line)
        values.update({'hw.lcd.width':'720','hw.lcd.height':'1280','hw.lcd.density':'320'})
        config.write_text(''.join(key+'='+value+'\n' for key,value in values.items()))
        console = None
        for candidate in range(5554, 5682, 2):
            try:
                with socket.socket() as first, socket.socket() as second:
                    first.bind(('127.0.0.1', candidate)); second.bind(('127.0.0.1', candidate+1))
                    console = candidate
                    break
            except OSError:
                continue
        if console is None:
            raise Blocked('No free emulator console/ADB port pair')
        self.serial = f'emulator-{console}'
        emulator = self.spawn([self.args.sdk/'emulator/emulator', '-avd', 'Gorilla3A', '-port', str(console),
                               '-no-window', '-no-snapshot', '-no-audio', '-no-boot-anim',
                               '-gpu', 'software', '-memory', '2048', '-cores', '2', '-accel', 'on'], 'emulator')
        wait(lambda: self.adb_call('shell', 'getprop', 'sys.boot_completed', timeout=3).strip() == b'1', 180, emulator)
        self.result('android_boot', 'PASS', 'API36, KVM, headless software rendering; isolated temporary AVD')
        chrome = self.adb_call('shell', 'pm', 'path', 'com.android.chrome').decode().strip()
        if not chrome.startswith('package:'):
            raise Blocked('Chrome absent in installed image; installation requires approval')
        self.report['android'] = dict(api=self.adb_call('shell', 'getprop', 'ro.build.version.sdk').decode().strip(),
                                     chrome=self.adb_call('shell', 'dumpsys', 'package', 'com.android.chrome').decode().split('versionName=')[1].splitlines()[0].strip())
        self.adb_call('shell', 'input', 'keyevent', '82')
        self.adb_call('shell', 'am', 'start', '-a', 'android.intent.action.VIEW', '-d', 'about:blank', '-p', 'com.android.chrome')
        # First-run dialogs only in our disposable AVD. Tap exclusively from UI-tree bounds.
        first_run_end = time.monotonic()+90
        step = 0
        scrolled = False
        while time.monotonic() < first_run_end:
            step += 1
            raw = self.adb_call('exec-out', 'uiautomator', 'dump', '/dev/tty', timeout=20).decode(errors='replace')
            start, finish = raw.find('<?xml'), raw.rfind('</hierarchy>')
            if start < 0 or finish < 0:
                continue
            tree = ET.fromstring(raw[start:finish+len('</hierarchy>')])
            (self.args.output/f'first-run-{step}.xml').write_text(ET.tostring(tree, encoding='unicode'))
            labels = ['Use without an account', 'Accept & continue', 'No thanks', 'Got it', 'Not now']
            node = next((node for label in labels for node in tree.iter('node')
                         if node.get('text') == label and node.get('enabled') == 'true'), None)
            if node is None:
                if any(node.get('resource-id','').endswith(('/url_bar','/search_box_text','/toolbar')) for node in tree.iter('node')):
                    break
                scroll = next((node for node in tree.iter('node') if node.get('scrollable')=='true'), None)
                if scroll is not None and not scrolled:
                    bounds = list(map(int, re.findall(r'\d+', scroll.get('bounds',''))))
                    if len(bounds)==4:
                        x1,y1,x2,y2=bounds
                        inset=max(1,(y2-y1)//4)
                        self.adb_call('shell','input','swipe',str((x1+x2)//2),str(y2-inset),str((x1+x2)//2),str(y1+inset),'300')
                        scrolled=True
                time.sleep(.25)
                continue
            coordinates = list(map(int, re.findall(r'\d+', node.get('bounds', ''))))
            if len(coordinates) != 4:
                raise RuntimeError('Missing UI-tree bounds')
            x1,y1,x2,y2 = coordinates
            self.adb_call('shell', 'input', 'tap', str((x1+x2)//2), str((y1+y2)//2))
        else:
            raise Blocked('Chrome first-run UI not resolved; see temporary UI trees')
        self.result('chrome_launch', 'PASS', 'Native browser UI available; initial dialogs handled from UI tree')

    def start_java(self, tls=False):
        env = self.env.copy()
        env.pop('GORILLA_MOBILE_CONFIG', None)
        env.update(GORILLA_IPC_INSTANCE=str(uuid.uuid4()), GORILLA_IPC_TOKEN=secrets.token_urlsafe(32),
                   GORILLA_IPC_LOCK_DIR=str(self.directory/'java-lock'))
        args = [self.args.java, '-Djava.net.preferIPv4Stack=true', '-jar', self.args.jar,
                '--gorilla.ipc.managed=true', '--server.address=127.0.0.1', '--server.port=0',
                '--logging.config=classpath:ipc-logback.xml']
        if tls:
            password = self.directory/'password'
            password.write_text(secrets.token_urlsafe(24)); password.chmod(0o600)
            command(['openssl','req','-x509','-newkey','rsa:2048','-nodes','-keyout',self.directory/'leaf.key',
                     '-out',self.directory/'leaf.crt','-days','1','-subj','/CN=Disposable QA certificate',
                     '-addext','subjectAltName=IP:127.0.0.1'], timeout=20)
            command(['openssl','pkcs12','-export','-out',self.directory/'server.p12','-inkey',self.directory/'leaf.key',
                     '-in',self.directory/'leaf.crt','-passout','file:'+str(password)])
            config = self.directory/'tls.properties'
            config.write_text('server.ssl.enabled=true\nserver.ssl.key-store='+ (self.directory/'server.p12').as_uri()
                              +'\nserver.ssl.key-store-type=PKCS12\nserver.ssl.key-store-password='+password.read_text()+'\n')
            config.chmod(0o600)
            args.append('--spring.config.additional-location='+config.as_uri())
        log = open(self.args.output/('java-tls.log' if tls else 'java-http.log'), 'wb'); self.logs.append(log)
        process = subprocess.Popen([str(a) for a in args], env=env, stdin=subprocess.PIPE,
                                   stdout=subprocess.PIPE, stderr=log)
        self.java = process; self.processes.append(('java-tls' if tls else 'java-http', process))
        ready = queue.Queue(maxsize=1)
        def drain():
            for line in iter(process.stdout.readline, b''):
                log.write(line); log.flush()
                if line.startswith(b'GORILLA_IPC_READY '):
                    try: ready.put_nowait(json.loads(line[18:]))
                    except queue.Full: pass
        reader = threading.Thread(target=drain, daemon=True); reader.start()
        end = time.monotonic()+20
        while time.monotonic()<end:
            if process.poll() is not None:
                raise RuntimeError('Java exited before READY')
            try:
                message = ready.get(timeout=.25)
                assert message['httpPort'] > 0
                return message['httpPort']
            except queue.Empty: pass
        raise TimeoutError('Java READY deadline')

    def stop_java(self):
        if self.java:
            if self.java.poll() is None:
                self.java.stdin.close()
                self.java.wait(timeout=12)
            if self.java.returncode != 0:
                raise RuntimeError('Java EOF did not exit0')
        self.java = None

    def targets(self, debugger_port):
        with urllib.request.urlopen(f'http://127.0.0.1:{debugger_port}/json', timeout=2) as response:
            return json.load(response)

    async def connect(self, debugger_port):
        import websockets
        target = wait(lambda: next((page for page in self.targets(debugger_port) if page.get('type')=='page'), None), 15)
        connection = await websockets.connect(target['webSocketDebuggerUrl'], open_timeout=5, close_timeout=2,
                                             max_size=4*1024*1024, max_queue=16, ping_interval=None)
        return CDP(connection)

    async def screenshot(self, cdp, name):
        result = await cdp.call('Page.captureScreenshot', dict(format='png'))
        (self.args.output/(name+'.png')).write_bytes(base64.b64decode(result['data']))

    async def browser(self):
        self.start_android()
        http_port = self.start_java()
        self.adb_call('reverse', f'tcp:{http_port}', f'tcp:{http_port}')
        url = f'http://127.0.0.1:{http_port}/'
        self.adb_call('shell','am','start','-a','android.intent.action.VIEW','-d',url,'-p','com.android.chrome')
        debugger = int(self.adb_call('forward','tcp:0','localabstract:chrome_devtools_remote').strip())
        cdp = await self.connect(debugger)
        try:
            await cdp.call('Page.enable')
            await cdp.navigate(url)
            await cdp.until("document.querySelector('h1')?.textContent.includes('Comprueba tu conexión')")
            self.result('react_load', 'PASS', 'HTTP localhost via isolated adb reverse: component test, not product HTTPS')
            await cdp.evaluate("document.querySelector('button').click()")
            await cdp.until("document.body.innerText.includes('Petición recibida:')")
            await self.screenshot(cdp, 'react-health')
            health = await cdp.evaluate("fetch('/api/health',{cache:'no-store'}).then(async r=>({status:r.status,cache:r.headers.get('cache-control'),body:await r.json()}))")
            assert health['status']==200 and health['cache']=='no-store'
            assert health['body']['service']=='gorilla-escape-local' and health['body']['secure'] is False
            self.report['httpComponent'] = dict(status=health['status'], noStore=True, javaSecure=False, requestNumber=health['body']['requestNumber'])
            self.result('health_and_interaction', 'PASS', 'Actual Java response; HTTP explicitly recorded, no TLS/public-trust claim')
            (self.args.output/'native-react-health.png').write_bytes(self.adb_call('exec-out','screencap','-p'))
            await cdp.reload()
            await cdp.until("document.querySelector('h1')?.textContent.includes('Comprueba tu conexión')")
            self.result('page_reload', 'PASS')
        finally:
            await cdp.connection.close()
        self.adb_call('shell','am','force-stop','com.android.chrome')
        self.adb_call('shell','am','start','-a','android.intent.action.VIEW','-d',url,'-p','com.android.chrome')
        cdp = await self.connect(debugger)
        try:
            await cdp.navigate(url)
            await cdp.until("document.querySelector('button') !== null")
            self.result('browser_restart', 'PASS')
            self.stop_java()
            await cdp.evaluate("document.querySelector('button').click()")
            await cdp.until("document.body.innerText.includes('No podemos contactar con la PC')", deadline=8)
            await self.screenshot(cdp, 'connection-error')
            self.result('connection_failure', 'PASS', 'Real Java stopped by stdin EOF; no mock or fake API success')
            tls_port = self.start_java(tls=True)
            self.adb_call('reverse', f'tcp:{tls_port}', f'tcp:{tls_port}')
            navigation = await cdp.navigate(f'https://127.0.0.1:{tls_port}/')
            assert navigation.get('errorText')=='net::ERR_CERT_AUTHORITY_INVALID', navigation.get('errorText')
            await cdp.until("document.body.innerText.includes('private') || document.body.innerText.includes('certificado')")
            await self.screenshot(cdp, 'tls-rejected')
            self.result('untrusted_tls_rejected', 'PASS', 'Ephemeral certificate rejected by stock Chrome; no bypass or CA installation')
            self.stop_java()
        finally:
            await cdp.connection.close()
        self.result('public_android_trust', 'BLOCKED', 'Controlled FQDN/public certificate pending; test PKI cannot validate this')
        self.result('physical_lan_dns_443_android_iphone', 'BLOCKED', 'Original physical gates remain; emulator NAT/adb reverse not router LAN')
        self.result('sensors_qr_sessions_wss_flutter', 'SKIP', 'Outside3A; Chrome DevTools WS is QA-only local debugging, not mobile protocol')

    def track_descendants(self, pid):
        try:
            children = Path(f'/proc/{pid}/task/{pid}/children').read_text().split()
            for child in children:
                number = int(child)
                # starttime prevents PID reuse from being reported as our residual.
                signature = Path(f'/proc/{number}/stat').read_text().rsplit(')',1)[1].split()[19]
                self.descendants[number] = signature
                self.track_descendants(number)
        except (FileNotFoundError, ProcessLookupError):
            pass

    def cleanup(self):
        for _, process in self.processes:
            if process.poll() is None: self.track_descendants(process.pid)
        if self.serial:
            try:
                pid = self.adb_call('shell','pidof','-s','com.android.chrome',timeout=3).decode().strip()
                if pid.isdigit():
                    (self.args.output/'chrome-logcat.txt').write_bytes(self.adb_call('logcat','-d','-t','200','--pid='+pid,timeout=5))
            except Exception: pass
        if self.serial:
            try: self.adb_call('emu','kill', timeout=5)
            except Exception: pass
        graceful = True
        try: self.stop_java()
        except Exception: graceful = False
        # Only our isolated server, never the shared default5037 server.
        if any(name=='adb' and process.poll() is None for name,process in self.processes):
            try: self.adb_call('kill-server',serial=False,timeout=5)
            except Exception: pass
        outcomes = []
        for name, process in reversed(self.processes):
            if process.poll() is None:
                try: process.wait(timeout=8)
                except subprocess.TimeoutExpired:
                    if name not in ['adb','emulator']: graceful = False
                    process.terminate()
                    try: process.wait(timeout=3)
                    except subprocess.TimeoutExpired: process.kill(); process.wait(timeout=3)
            outcomes.append(dict(component=name, exitCode=process.returncode, residual=process.poll() is None))
        for log in self.logs: log.close()
        residual_helpers = []
        for pid, signature in self.descendants.items():
            try:
                current = Path(f'/proc/{pid}/stat').read_text().rsplit(')',1)[1].split()
                if current[19] == signature and current[0] != 'Z': residual_helpers.append(pid)
            except FileNotFoundError: pass
        self.report['cleanup'] = dict(processes=outcomes, ownedResiduals=sum(p.poll() is None for _,p in self.processes)+len(residual_helpers),
                                      ownedHelperResiduals=len(residual_helpers), javaGraceful=graceful)


def main():
    def cancel(signum, frame):
        raise KeyboardInterrupt()
    signal.signal(signal.SIGTERM, cancel)
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--sdk', type=Path, default=Path.home()/'Android/Sdk')
    parser.add_argument('--java', type=Path, default=Path('/usr/lib/jvm/java-21-openjdk-amd64/bin/java'))
    parser.add_argument('--jar', type=Path, default=Path('Server/target/local-server-0.1.0-SNAPSHOT.jar'))
    parser.add_argument('--output', type=Path, default=Path('Unity/Logs/Android3AValidation'))
    args = parser.parse_args()
    args.sdk=args.sdk.resolve(); args.jar=args.jar.resolve(); args.java=args.java.resolve(); args.output=args.output.resolve()
    # Do not overwrite evidence. Output goes under ignored Unity/Logs by default.
    args.output.mkdir(parents=True, exist_ok=True)
    args.output=args.output/('run-'+str(time.time_ns())); args.output.mkdir()
    with tempfile.TemporaryDirectory(prefix='gorilla-android-3a-') as directory:
        run=Run(args, Path(directory)); code=0
        try:
            import websockets
            required = [args.java,args.jar,args.sdk/'platform-tools/adb',args.sdk/'emulator/emulator',args.sdk/'cmdline-tools/latest/bin/avdmanager',
                        args.sdk/'system-images/android-36/google_apis/x86_64/package.xml']
            if not all(path.is_file() for path in required): raise Blocked('Installed SDK/image/Java/JAR missing; no automatic installation')
            if not os.access('/dev/kvm',os.R_OK|os.W_OK): raise Blocked('KVM inaccessible; no privilege changes')
            run.report['jarSha256']=hashlib.sha256(args.jar.read_bytes()).hexdigest()
            asyncio.run(run.browser())
        except KeyboardInterrupt:
            run.result('cancelled','SKIP','Cancelled by operator; cleanup attempted'); code=130
        except (Blocked,ImportError) as failure:
            run.result('environment','BLOCKED',str(failure)); code=2
        except Exception as failure:
            run.result('automation','FAIL',type(failure).__name__+': '+str(failure)[:160]); code=1
        finally:
            run.cleanup()
            if run.report['cleanup']['ownedResiduals'] or not run.report['cleanup']['javaGraceful']:
                run.result('cleanup','FAIL'); code=1
            else: run.result('cleanup','PASS','Owned process handles closed; no shared ADB server/device/profile killed')
            run.result('android_logcat','PASS' if (args.output/'chrome-logcat.txt').exists() else 'SKIP', 'Bounded Chrome PID log capture in disposable AVD; raw logs ignored')
            (args.output/'results.json').write_text(json.dumps(run.report,indent=2)+'\n')
    print('Report:', args.output/'results.json')
    print('Component run:', 'PASS' if code==0 else ('BLOCKED' if code==2 else 'FAIL'), '; physical3A remains IN PROGRESS')
    raise SystemExit(code)


if __name__=='__main__': main()
