#!/usr/bin/python3
"""Closed UFW operations. No shell, caller commands, passwords or environment configuration.

A root-owned receipt permits non-elevated checks of protected UFW files on later runs.
If those files change, their rules must be inspected again, never assumed to still match.
"""
import argparse
import fcntl
import hashlib
import ipaddress
import json
import os
from pathlib import Path
import re
import select
import stat
import subprocess
import sys
import tempfile

UFW = '/usr/sbin/ufw'
IP = '/usr/sbin/ip'
STATE = Path('/var/lib/gorilla-escape-firewall')
CONFIG = Path('/etc/ufw/ufw.conf')
RULE_FILES = (Path('/etc/ufw/user.rules'), Path('/etc/ufw/user6.rules'))
# Explicit RTC allocator policy, configured identically in MobileRuntime (32 bound ports,
# four peers plus reconnection headroom); validated with real host ICE candidates.
RTC_MIN, RTC_MAX = 50000, 50031
ENV = {'PATH': '/usr/sbin:/usr/bin:/sbin:/bin', 'LANG': 'C', 'LC_ALL': 'C'}

class FirewallError(Exception):
    pass

def run(args):
    result = subprocess.run(args, capture_output=True, text=True, timeout=5, env=ENV)
    if result.returncode:
        raise FirewallError('COMMAND_FAILED')
    return result.stdout

def network(address, runner=run):
    ip = ipaddress.IPv4Address(address)
    if not any(ip in ipaddress.ip_network(n) for n in ('10.0.0.0/8', '172.16.0.0/12', '192.168.0.0/16')):
        raise FirewallError('LAN_ADDRESS_INVALID')
    for link in json.loads(runner([IP, '-j', '-4', 'address', 'show', 'up'])):
        iface = link['ifname']
        if not re.fullmatch(r'[A-Za-z0-9_.:-]{1,15}', iface) or iface == 'lo':
            continue
        for info in link.get('addr_info', []):
            if info.get('local') == str(ip):
                subnet = ipaddress.ip_network(str(ip) + '/' + str(info['prefixlen']), strict=False)
                if subnet.prefixlen < 8 or subnet.prefixlen > 30:
                    raise FirewallError('LAN_SUBNET_INVALID')
                return {'interface': iface, 'address': str(ip), 'subnet': str(subnet)}
    raise FirewallError('LAN_CHANGED')

def rules(net, recovery=False):
    def rule(proto, port, kind):
        return ['allow', 'in', 'on', net['interface'], 'proto', proto, 'from', net['subnet'],
                'to', net['address'], 'port', port, 'comment', 'GorillaEscape-v1-' + kind]
    result = [rule('tcp', '8443', 'https'), rule('udp', f'{RTC_MIN}:{RTC_MAX}', 'rtc')]
    if recovery:
        result.append(rule('tcp', '8081', 'recovery'))
    return result

def signature(paths=RULE_FILES):
    return [[str(p), p.stat().st_dev, p.stat().st_ino, p.stat().st_size, p.stat().st_mtime_ns, p.stat().st_ctime_ns] for p in paths]

def safe_receipt(path):
    try:
        s = path.lstat()
        if not stat.S_ISREG(s.st_mode) or s.st_uid != 0 or s.st_mode & 0o022:
            return None
        return json.loads(path.read_text())
    except (OSError, ValueError):
        return None

def save(path, data):
    fd, temp = tempfile.mkstemp(prefix='.receipt-', dir=path.parent)
    try:
        with os.fdopen(fd, 'w') as out:
            json.dump(data, out, sort_keys=True)
            out.flush()
            os.fsync(out.fileno())
        os.chmod(temp, 0o644)
        os.replace(temp, path)
    finally:
        if os.path.exists(temp):
            os.unlink(temp)

def tuple_rule(line):
    # UFW's actual user.rules schema, not localized `ufw status` output.
    parts = line.split()
    if parts[:3] != ['###', 'tuple', '###'] or len(parts) < 10:
        return None
    # action protocol destination-port destination source-port source direction
    return tuple(parts[3:10])

def present(rule, text):
    # interface is stored as "in_<iface>" in UFW's tuple; comment is separate.
    proto, subnet, address, port, iface = rule[5], rule[7], rule[9], rule[11], rule[3]
    expected = ('allow', proto, port, address, 'any', subnet, 'in_' + iface)
    return any(tuple_rule(line) == expected for line in text.splitlines())

def owned_present(rule, text):
    # Ownership belongs to this exact tuple AND comment on the same UFW line.
    comment = 'comment=' + rule[-1].encode().hex()
    return any(present(rule, line) and line.split()[-1:] == [comment] for line in text.splitlines())


def execute(operation, net, recovery=False, state=STATE, runner=run, files=RULE_FILES):
    key = hashlib.sha256(json.dumps(net, sort_keys=True).encode()).hexdigest()[:24]
    receipt_path = state / (key + '.json')
    receipt = safe_receipt(receipt_path)
    wanted = rules(net, recovery)
    current_signature = signature(files)
    if operation == '--check' and receipt and receipt.get('network') == net and receipt.get('signature') == current_signature and all(r in receipt.get('verified', []) for r in wanted):
        return 'READY'
    try:
        text = files[0].read_text()
    except PermissionError:
        return 'INSPECTION_REQUIRED'
    missing = [r for r in wanted if not present(r, text)]
    if operation == '--check':
        return 'NEEDS_PERMISSION' if missing else 'READY'
    if os.geteuid() != 0:
        raise FirewallError('ROOT_HELPER_ONLY')
    if operation == '--remove':
        # Never claim an equivalent pre-existing rule. Verify our exact comment too.
        retained = []
        for r in (receipt or {}).get('owned', []):
            if recovery and r[-1] != 'GorillaEscape-v1-recovery':
                retained.append(r)
                continue
            if r not in rules(net, True):
                raise FirewallError('RECEIPT_INVALID')
            if owned_present(r, text):
                runner([UFW, '--force', 'delete'] + r)
        after = files[0].read_text()
        verified = [r for r in rules(net) if present(r, after)]
        save(receipt_path, {'network': net, 'owned': retained, 'verified': verified, 'signature': signature(files)})
        return 'REMOVED'
    owned = list((receipt or {}).get('owned', []))
    added = []
    try:
        for r in missing:
            # Persist intent before modification so interrupted installs are recoverable.
            save(receipt_path, {'network': net, 'owned': owned + added + [r], 'verified': [], 'signature': []})
            added.append(r)
            runner([UFW] + r)
        updated = files[0].read_text()
        if any(not present(r, updated) for r in wanted):
            raise FirewallError('VERIFY_FAILED')
        save(receipt_path, {'network': net, 'owned': owned + added, 'verified': wanted, 'signature': signature(files)})
        return 'READY'
    except Exception:
        remaining = []
        for r in reversed(added):
            try:
                current = files[0].read_text()
                if owned_present(r, current):
                    runner([UFW, '--force', 'delete'] + r)
            except Exception:
                remaining.append(r)
        save(receipt_path, {'network': net, 'owned': owned + remaining, 'verified': [], 'signature': []})
        raise

def main():
    # Treat the closed operation as positional while retaining the requested CLI spelling.
    argv = sys.argv[1:]
    if not argv or argv[0] not in ('--check', '--install', '--remove'):
        raise FirewallError('OPERATION_INVALID')
    argv[0] = argv[0][2:]
    p = argparse.ArgumentParser()
    p.add_argument('operation', choices=['check', 'install', 'remove'])
    p.add_argument('--address')
    p.add_argument('--recovery', action='store_true')
    args = p.parse_args(argv)
    if not Path(UFW).exists():
        print(json.dumps({'state': 'NOT_INSTALLED'})); return
    enabled = re.search(r'^ENABLED=yes$', CONFIG.read_text(), re.M) is not None
    if not enabled and args.operation != 'remove':
        print(json.dumps({'state': 'DISABLED'})); return
    if args.operation == 'remove':
        if os.geteuid() != 0:
            raise FirewallError('ROOT_HELPER_ONLY')
        if STATE.exists():
            with (STATE / 'lock').open('a') as lock:
                fcntl.flock(lock, fcntl.LOCK_EX)
                for path in STATE.glob('*.json'):
                    receipt = safe_receipt(path)
                    if receipt:
                        execute('--remove', receipt['network'])
        print(json.dumps({'state': 'REMOVED'})); return
    if args.address is None:
        raise FirewallError('LAN_ADDRESS_REQUIRED')
    net = network(args.address)
    if args.operation != 'check':
        if os.geteuid() != 0:
            raise FirewallError('ROOT_HELPER_ONLY')
        STATE.mkdir(mode=0o755, parents=True, exist_ok=True)
        s = STATE.lstat()
        if not stat.S_ISDIR(s.st_mode) or s.st_uid != 0 or s.st_mode & 0o022:
            raise FirewallError('STATE_DIRECTORY_INVALID')
        # Lock is opened only in a root-owned directory, never under user input.
        with (STATE / 'lock').open('a') as lock:
            fcntl.flock(lock, fcntl.LOCK_EX)
            result = execute('--' + args.operation, net, args.recovery)
    else:
        result = execute('--check', net, args.recovery)
    print(json.dumps({'state': result, **net, 'recovery': args.recovery}), flush=True)
    if args.operation == 'install' and args.recovery and result == 'READY':
        # Root helper receives no commands/passwords. EOF or 180s ends this exception.
        select.select([sys.stdin], [], [], 180)
        with (STATE / 'lock').open('a') as lock:
            fcntl.flock(lock, fcntl.LOCK_EX)
            execute('--remove', net, True)


if __name__ == '__main__':
    try:
        main()
    except (FirewallError, OSError, ValueError, subprocess.TimeoutExpired) as error:
        print(json.dumps({'state': 'FAILED', 'reason': str(error) if isinstance(error, FirewallError) else type(error).__name__}))
        sys.exit(1)
