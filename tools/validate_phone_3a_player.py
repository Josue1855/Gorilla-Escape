#!/usr/bin/env python3
"""Real Linux Player + JAR TLS smoke. Temporary test PKI is outside the repository.
This is PC evidence, not a phone or WAN-off acceptance. No trust-all or firewall changes.
"""
import argparse
import json
import http.client
import os
from pathlib import Path
import socket
import ssl
import subprocess
import tempfile
from validate_ipc_2b_player import Player, X11


def main():
    parser = argparse.ArgumentParser()
    for field in ['player', 'java', 'jar', 'output']:
        parser.add_argument('--' + field, type=Path, required=True)
    parser.add_argument('--address', required=True)
    parser.add_argument('--interface', required=True)
    parser.add_argument('--port', type=int, default=8443, help='Internal HTTPS port; 0 selects an ephemeral test port')
    args = parser.parse_args()
    if args.port != 0 and not 1024 <= args.port <= 65535:
        parser.error('port must be 0 for test allocation or 1024..65535')
    args.output.mkdir(parents=True, exist_ok=True)
    x = X11()
    hostname = 'probe.gorilla.test'
    result = {'scope': 'PC automated smoke; physical phones and WAN-off NOT RUN'}
    with tempfile.TemporaryDirectory(prefix='gorilla-3a-test-pki-') as temp:
        directory = Path(temp)
        def command(*parts):
            subprocess.run(parts, cwd=directory, check=True, timeout=15, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        command('openssl', 'req', '-x509', '-newkey', 'rsa:2048', '-nodes', '-keyout', 'ca.key', '-out', 'ca.crt', '-days', '1', '-subj', '/CN=Temporary Player test CA')
        command('openssl', 'req', '-new', '-newkey', 'rsa:2048', '-nodes', '-keyout', 'leaf.key', '-out', 'leaf.csr', '-subj', '/CN=Temporary Player test leaf')
        (directory/'san').write_text('subjectAltName=DNS:' + hostname + '\nbasicConstraints=CA:FALSE\nkeyUsage=digitalSignature,keyEncipherment\nextendedKeyUsage=serverAuth\n')
        command('openssl', 'x509', '-req', '-in', 'leaf.csr', '-CA', 'ca.crt', '-CAkey', 'ca.key', '-CAcreateserial', '-out', 'leaf.crt', '-days', '1', '-extfile', 'san')
        # Fixed password protects ephemeral TEST material only; never use for a demo.
        command('openssl', 'pkcs12', '-export', '-out', 'server.p12', '-inkey', 'leaf.key', '-in', 'leaf.crt', '-certfile', 'ca.crt', '-passout', 'pass:temporary-test-only')
        (directory/'password').write_text('temporary-test-only\n')
        for path in directory.iterdir():
            path.chmod(0o600)
        with socket.socket() as reserve:
            reserve.bind((args.address, args.port)); port = reserve.getsockname()[1]
        config = directory/'mobile.properties'
        config.write_text(f'address={args.address}\ninterface={args.interface}\noperatorConfirmed=true\nport={port}\nhostname={hostname}\npublicOrigin=https://{hostname}\nkeyStore={directory}/server.p12\npasswordFile={directory}/password\n')
        config.chmod(0o600)
        previous = os.environ.get('GORILLA_MOBILE_CONFIG')
        os.environ['GORILLA_MOBILE_CONFIG'] = str(config)
        player = None
        try:
            player = Player(args, 'mobile-https', mode='lifecycle')
            running = player.event('FIRST_PONG')
            pid = running['pid']
            status = Path('/proc', str(pid), 'status').read_text()
            assert f'PPid:\t{player.player.pid}\n' in status
            fields = dict(line.split(':', 1) for line in status.splitlines() if ':' in line)
            assert os.getuid() != 0 and all(int(uid) == os.getuid() for uid in fields['Uid'].split())
            assert int(fields['CapEff'].strip(), 16) == 0
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
            # Connect directly to the explicit LAN IPv4, preserving DNS SNI and strict
            # hostname verification. Test-only routing, no /etc/hosts or DNS mutation.
            def get(path):
                connection = http.client.HTTPSConnection(hostname, port, context=context, timeout=5)
                raw = socket.create_connection((args.address, port), timeout=5)
                connection.sock = context.wrap_socket(raw, server_hostname=hostname)
                try:
                    connection.request('GET', path)
                    response = connection.getresponse()
                    assert response.status == 200
                    return response.headers, response.read()
                finally:
                    connection.close()
            headers, body = get('/api/health')
            assert headers['Cache-Control'] == 'no-store'
            health = json.loads(body)
            assert health['secure'] and health['service'] == 'gorilla-escape-local'
            assert 'id="root"' in get('/')[1].decode()
            with socket.socket() as probe:
                probe.settimeout(1)
                assert probe.connect_ex((args.address, private[0]['port'])) != 0
            # Healthy liveness remains active while HTTP is used; then normal WM close -> EOF.
            player.close(x)
            stopped = player.event('STOPPED')
            assert stopped['cleanupComplete'] and not stopped['forced']
            result.update(hostname=hostname, publicOrigin="https://" + hostname, public443="NOT RUN", testRouting="direct IPv4 with DNS SNI; no system DNS changes", status='PASS', listeners=listeners, httpsHealth=True, packagedShell=True,
                          ipcHandshake=True, unprivilegedJava=True, effectiveCapabilities=0, lanToIpcFromPc='REJECTED', secondPhysicalDevice='NOT RUN',
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
