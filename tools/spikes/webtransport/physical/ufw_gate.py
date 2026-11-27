"""Privileged lab guard: exact IPv4 rules, bounded lifetime, own-comment rollback.
Java/PWA remain unprivileged. No reset, disable, NAT, forwarding or router edits.
"""
import argparse,ipaddress,json,os,re,signal,subprocess,threading,time
from pathlib import Path

UFW='/usr/sbin/ufw'
def ufw(*args):
    r=subprocess.run([UFW,*args],capture_output=True,text=True,timeout=30,env={**os.environ,'LC_ALL':'C'})
    if r.returncode:raise RuntimeError('UFW command failed: '+str(r.returncode))
    return r.stdout

def own_numbers(text,tag):
    return sorted([int(m.group(1)) for line in text.splitlines()
                   if (m:=re.match(r'^\[\s*(\d+)\].*#\s*'+re.escape(tag)+r'\s*$',line))],reverse=True)

def main():
    ap=argparse.ArgumentParser();ap.add_argument('--interface',required=True);ap.add_argument('--source',required=True)
    ap.add_argument('--destination',required=True);ap.add_argument('--https',type=int,required=True)
    ap.add_argument('--ca',type=int,required=True);ap.add_argument('--wt',type=int,required=True)
    ap.add_argument('--owner-pid',type=int,required=True);ap.add_argument('--owner-output',required=True)
    ap.add_argument('--audit',type=Path,required=True);a=ap.parse_args()
    if os.geteuid()!=0:raise SystemExit('Use local administrative authentication; do not run Java as root')
    if not re.fullmatch(r'[A-Za-z0-9_.-]{1,32}',a.interface):raise SystemExit('invalid interface')
    src=ipaddress.IPv4Address(a.source);dst=ipaddress.IPv4Address(a.destination)
    if any(not x.is_private or x.is_loopback or x.is_unspecified or x.is_multicast for x in (src,dst)) or src==dst:raise SystemExit('explicit separate private IPv4 required')
    if not all(1024<=p<=65535 for p in (a.https,a.ca,a.wt)) or a.https==a.ca:raise SystemExit('invalid ports')
    if not a.audit.is_dir():raise SystemExit('existing private audit directory required')
    def owner_alive():
        try:
            args=Path('/proc',str(a.owner_pid),'cmdline').read_bytes().decode().split('\0')
            return 'tools/spikes/webtransport/physical/lan.py' in args and a.owner_output in args
        except (OSError,UnicodeError):return False
    if not owner_alive():raise SystemExit('owned lab runner not active')
    tag='gorilla-phase0-iphone-temp-'+str(a.owner_pid)
    stopped=threading.Event()
    for sig in (signal.SIGTERM,signal.SIGINT,signal.SIGHUP):signal.signal(sig,lambda *_:stopped.set())
    before=ufw('status','numbered');before_verbose=ufw('status','verbose')
    if not before.startswith('Status: active'):raise SystemExit('UFW must remain active')
    if tag in before:raise SystemExit('unexpected prior own tag')
    # Never update an existing identical rule/comment. Reject any ambiguity instead.
    for line in before.splitlines():
        if str(src) in line and str(dst) in line and any(str(p) in line for p in (a.https,a.ca,a.wt)):
            raise SystemExit('potential existing-rule overlap; read-only review required')
    audit={'tag':tag,'temporaryRules':[],'rollback':'PENDING','activeBefore':True,'ownerRuntime':'unprivileged Java lab'}
    try:
        for proto,port in [('tcp',a.https),('tcp',a.ca),('udp',a.wt)]:
            args=['allow','in','on',a.interface,'proto',proto,'from',str(src)+'/32','to',str(dst),'port',str(port),'comment',tag]
            out=ufw(*args)
            if 'Skipping' in out:raise RuntimeError('existing rule detected; no further mutations')
            audit['temporaryRules'].append({'protocol':proto,'port':port,'source':'confirmed iPhone /32','destination':'current laptop','interface':'current Wi-Fi'})
        effective=ufw('status','numbered')
        if not effective.startswith('Status: active') or len(own_numbers(effective,tag))!=3:raise RuntimeError('effective rules did not match')
        (a.audit/'ufw-during-numbered.txt').write_text(effective)
        print('READY UFW: active; three scoped temporary rules; rollback on owner exit or 30-minute deadline.',flush=True)
        def commands():
            for line in __import__('sys').stdin:
                if line.strip()=='STOP':stopped.set();return
            stopped.set()
        threading.Thread(target=commands,daemon=True).start()
        end=time.monotonic()+30*60
        while not stopped.wait(.5) and owner_alive() and time.monotonic()<end:pass
    except Exception as e:audit['error']=type(e).__name__
    finally:
        try:
            for number in own_numbers(ufw('status','numbered'),tag):
                current=ufw('status','numbered')
                if number not in own_numbers(current,tag):raise RuntimeError('rule numbering changed; refuse unrelated deletion')
                ufw('--force','delete',str(number))
            after=ufw('status','numbered');after_verbose=ufw('status','verbose')
            (a.audit/'ufw-after-numbered.txt').write_text(after)
            audit.update(activeAfter=after.startswith('Status: active'),temporaryRulesRemaining=len(own_numbers(after,tag)),
                         originalNumberedRulesIdentical=before==after,originalVerboseStateIdentical=before_verbose==after_verbose)
            audit['rollback']='PASS' if audit['activeAfter'] and audit['temporaryRulesRemaining']==0 and before==after and before_verbose==after_verbose else 'FAIL'
        except Exception as e:audit.update(rollback='FAIL',rollbackError=type(e).__name__)
        path=a.audit/'ufw-gate-result.json';path.write_text(json.dumps(audit,indent=2)+'\n');path.chmod(0o600)
        uid=int(os.environ.get('PKEXEC_UID','0'));os.chown(path,uid,-1)
        print('STOPPED UFW: '+audit['rollback'],flush=True)

if __name__=='__main__':main()
