#!/usr/bin/env python3
"""Recipient-owned LGPL library replacement: refresh hashes, never weaken verification."""
import argparse,hashlib,json,pathlib,os
def digest(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def refresh(root,path):
    value=json.loads(path.read_text())
    for record in value['files']:
        target=root/record['path']
        if not target.resolve().is_relative_to(root.resolve()) or not target.is_file():raise ValueError('MANIFEST_PATH_INVALID')
        record['sha256']=digest(target);record['bytes']=target.stat().st_size
    if 'sourceManifestSha256' in value:value['sourceManifestSha256']=digest(root/'manifest.json')
    temporary=path.with_name(path.name+'.new');temporary.write_text(json.dumps(value,indent=2)+'\n');os.replace(temporary,path)
if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--recipient-owned-root',type=pathlib.Path,required=True);a=p.parse_args();root=a.recipient_owned_root.resolve()
    for directory,name in [(root/'Vision','manifest.json'),(root/'Vision','runtime-manifest.json'),(root,'runtime-manifest.json')]:refresh(directory,directory/name)
