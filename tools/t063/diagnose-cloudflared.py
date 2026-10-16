#!/usr/bin/env python3
"""Explicit QA only. Collect cloudflared's official local diagnostic with a bounded command."""
import hashlib,os,subprocess,sys
from pathlib import Path
root=Path(sys.argv[1]).resolve();binary=root/'Tools/cloudflared'
if not binary.is_file() or hashlib.sha256(binary.read_bytes()).hexdigest()!='d33ff2d14475178d2012c2c56beba87389ac5ded27649519f198a7d3134a99db':raise SystemExit('DIAGNOSTIC_BINARY_UNAVAILABLE')
output=Path.home()/'GorillaEscape-QA-Summaries';output.mkdir(exist_ok=True)
env={k:v for k,v in os.environ.items() if not k.startswith(('GORILLA_','TUNNEL_','CLOUDFLARE_','CF_'))}
try:raise SystemExit(subprocess.run([str(binary),'tunnel','diag'],cwd=output,env=env,timeout=15).returncode)
except subprocess.TimeoutExpired:raise SystemExit('DIAGNOSTIC_TIMEOUT')
