#!/usr/bin/env python3
"""Real Linux Player + JAR TLS smoke. Temporary test PKI is outside the repository.
This is PC evidence, not a phone or WAN-off acceptance. No trust-all or firewall changes.
"""
import argparse
import json
import os
from pathlib import Path
import socket
import ssl
import subprocess
import tempfile
import urllib.request
from validate_ipc_2b_player import Player, X11


def main():
    parser = argparse.ArgumentParser()
    for field in ['player', 'java', 'jar', 'output']:
        parser.add_argument('--' + field, type=Path, required=True)
    parser.add_argument('--address', required=True)
    parser.add_argument('--interface', required=True)
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)
    x = X11()
    result = {'scope': 'PC automated smoke; physical phones and WAN-off NOT RUN'}
    with tempfile.TemporaryDirectory(prefix='gorilla-3a-test-pki-') as temp:
        directory = Path(temp)
        def command(*parts):
            subprocess.run(parts, cwd=directory, check=True, timeout=15, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        command('openssl', 'req', '-x509', '-newkey', 'rsa:2048', '-nodes', '-keyout', 'ca.key', '-out', 'ca.crt', '-days', '1', '-subj', '/CN=Temporary Player test CA')
        command('openssl', 'req', '-new', '-newkey', 'rsa:2048', '-nodes', '-keyout', 'leaf.key', '-out', 'leaf.csr', '-subj', '/CN=Temporary Player test leaf')
        (directory/'san').write_text('subjectAltName=IP:' + args.address + '\nbasicConstraints=CA:FALSE\nkeyUsage=digitalSignature,keyEncipherment\nextendedKeyUsage=serverAuth\n')
        command('openssl', 'x509', '-req', '-in', 'leaf.csr', '-CA', 'ca.crt', '-CAkey', 'ca.key', '-CAcreateserial', '-out', 'leaf.crt', '-days', '1', '-extfile', 'san')
        # Fixed password protects ephemeral TEST material only; never use for a demo.
        command('openssl', 'pkcs12', '-export', '-out', 'server.p12', '-inkey', 'leaf.key', '-in', 'leaf.crt', '-certfile', 'ca.crt', '-passout', 'pass:temporary-test-only')
        (directory/'password').write_text('temporary-test-only\n')
        for path in directory.iterdir():
            path.chmod(0o600)
        with socket.socket() as reserve:
            reserve.bind((args.address, 0)); port = reserve.getsockname()[1]
        config = directory/'mobile.properties'
        config.write_text(f'address={args.address}\ninterface={args.interface}\noperatorConfirmed=true\nport={port}\nkeyStore={directory}/server.p12\npasswordFile={directory}/password\n')
        config.chmod(0o600)
        previous = os.environ.get('GORILLA_MOBILE_CONFIG')
        os.environ['GORILLA_MOBILE_CONFIG'] = str(config)
        player = None
        try:
            player = Player(args, 'mobile-https', mode='lifecycle')
            running = player.event('FIRST_PONG')
            pid = running['pid']
            assert f'PPid:\t{player.player.pid}\n' in Path('/proc', str(pid), 'status').read_text()
            inodes = set()
            for fd in Path('/proc', str(pid), 'fd').iterdir():
                try:
                    value = os.readlink(fd)
                    if value.startswith('socket:['): inodes.add(value[8:-1])
                except FileNotFoundError:
                    pass
            listeners = []
            for family in ['tcp', 'tcp6']:
                for line in Path('/proc', str(pid), 'net', family).read_text().splitlines()[1:]:
                    parts = line.split()
                    if parts[3] != '0A' or parts[9] not in inodes: continue
                    assert family == 'tcp', 'Unexpected IPv6 listener'
                    host, p = parts[1].split(':')
                    host = socket.inet_ntoa(bytes.fromhex(host)[::-1])
                    listeners.append({'address': host, 'port': int(p, 16)})
            assert len(listeners) == 2, listeners
            assert {'address': args.address, 'port': port} in listeners
            private = [item for item in listeners if item['address'] == '127.0.0.1']
            assert len(private) == 1, listeners
            context = ssl.create_default_context(cafile=str(directory/'ca.crt'))
            # Explicitly avoid proxy env variables; traffic must go directly to the LAN address.
            opener = urllib.request.build_opener(urllib.request.ProxyHandler({}), urllib.request.HTTPSHandler(context=context))
            base = f'https://{args.address}:{port}'
            with opener.open(base + '/api/health', timeout=5) as response:
                assert response.headers['Cache-Control'] == 'no-store'
                health = json.load(response)
                assert health['secure'] and health['service'] == 'gorilla-escape-local'
            with opener.open(base + '/', timeout=5) as response:
                assert 'id="root"' in response.read().decode()
            with socket.socket() as probe:
                probe.settimeout(1)
                assert probe.connect_ex((args.address, private[0]['port'])) != 0
            # Healthy liveness remains active while HTTP is used; then normal WM close -> EOF.
            player.close(x)
            stopped = player.event('STOPPED')
            assert stopped['cleanupComplete'] and not stopped['forced']
            result.update(status='PASS', listeners=listeners, httpsHealth=True, packagedShell=True,
                          ipcHandshake=True, lanToIpcFromPc='REJECTED', secondPhysicalDevice='NOT RUN',
                          unityExit=player.player.returncode, javaExit=stopped['exitCode'], cleanup=stopped['cleanupComplete'],
                          ownedJavaResidual=len(player.residuals()))
            assert result['javaExit'] == 0
        finally:
            if player: player.emergency_cleanup()
            if previous is None: os.environ.pop('GORILLA_MOBILE_CONFIG', None)
            else: os.environ['GORILLA_MOBILE_CONFIG'] = previous
    (args.output/'mobile-player-result.json').write_text(json.dumps(result, indent=2) + '\n')
    print('PASS: Player-owned Java HTTPS + private IPC + normal EOF cleanup; phones NOT RUN.')

if __name__ == '__main__':
    main()
