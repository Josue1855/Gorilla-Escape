#!/usr/bin/env python3
"""Build-time only: bundle a reviewed platform binary. No network/runtime installer."""
import hashlib,json,shutil,sys
from pathlib import Path
ARTIFACTS={
    'linux-amd64':('cloudflared','d33ff2d14475178d2012c2c56beba87389ac5ded27649519f198a7d3134a99db'),
    'windows-amd64':('cloudflared.exe','86aee4017b26625cee8484c113558f48effa4cd47f7aa05fcf425604e5d2b23c'),
}
def bundle(source,root,platform='linux-amd64'):
    filename,pin=ARTIFACTS[platform]
    if hashlib.sha256(source.read_bytes()).hexdigest()!=pin:raise ValueError('CLOUDFLARED_SHA_INVALID')
    target=root/'Tools';target.mkdir(exist_ok=True)
    shutil.copy2(source,target/filename)
    if platform.startswith('linux-'):(target/filename).chmod(0o755)
    for name in ['LICENSE','NOTICE']:shutil.copy2(Path(__file__).parent/'cloudflared'/name,target/('cloudflared-'+name))
    shutil.copy2(Path(__file__).parent/'diagnose-cloudflared.py',target/'diagnose-cloudflared.py')
    manifest={'version':'2026.10.0','platform':platform,'sha256':pin,'license':'Apache-2.0',
        'source':f'https://github.com/cloudflare/cloudflared/releases/download/2026.10.0/{"cloudflared-windows-amd64.exe" if platform == "windows-amd64" else "cloudflared-linux-amd64"}',
        'licenseSource':'https://github.com/cloudflare/cloudflared/blob/2026.10.0/LICENSE',
        'runtimeDownload':False,'integrityPolicy':'Java verifies SHA256 before any execution; missing/invalid optional binary selects Cabin in AUTO'}
    (target/'cloudflared-manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
    return manifest
if __name__=='__main__':print(json.dumps(bundle(Path(sys.argv[1]),Path(sys.argv[2]),sys.argv[3] if len(sys.argv)>3 else 'linux-amd64')))
