#!/usr/bin/env python3
"""Run only inside the caller-created private user/network namespace; never modify host network."""
import json,os,socket,subprocess,sys
from pathlib import Path
host=os.environ.get('GORILLA_HOST_NETNS')
if not host or host==os.readlink('/proc/self/ns/net'):raise SystemExit('PRIVATE_NETNS_REQUIRED')
for command in [['ip','link','set','lo','up'],['ip','link','add','gorilla-lab','type','veth','peer','name','gorilla-peer'],['ip','addr','add','192.168.77.1/24','dev','gorilla-lab'],['ip','link','set','gorilla-lab','up'],['ip','link','set','gorilla-peer','up']]:subprocess.run(command,check=True)
s=socket.socket();s.settimeout(1);result=s.connect_ex(('1.1.1.1',443));s.close();assert result==101,'WAN_MUST_BE_UNREACHABLE'
print(json.dumps({'scope':'isolated user/network namespace; dummy LAN only','wanConnectEx':result,'defaultRoutes':subprocess.check_output(['ip','-j','route','show','default'],text=True).strip()}),flush=True)
raise SystemExit(subprocess.run(sys.argv[1:]).returncode)
