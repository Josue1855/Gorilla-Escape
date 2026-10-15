#!/usr/bin/env python3
"""Create an isolated LAB network with a virtual LAN and no default route; host stays unchanged."""
import os,subprocess,sys
from pathlib import Path
repo=Path(__file__).resolve().parents[2]
command=['bwrap','--unshare-user','--unshare-net','--uid','0','--gid','0','--cap-add','CAP_NET_ADMIN','--dev-bind','/','/','--proc','/proc','--setenv','GORILLA_HOST_NETNS',os.readlink('/proc/self/ns/net'),'--',sys.executable,str(repo/'tools/t063/no-wan.py'),*sys.argv[1:]]
raise SystemExit(subprocess.run(command,cwd=repo).returncode)
