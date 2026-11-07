#!/usr/bin/env python3
"""Copy the separately built players backend into the existing Unity package; no merge/download."""
import argparse, hashlib, shutil
from pathlib import Path
parser=argparse.ArgumentParser()
parser.add_argument('--jar', type=Path, required=True)
parser.add_argument('--package',type=Path,default=Path(__file__).resolve().parents[1]/'Unity/Builds/FoundationLinux')
args=parser.parse_args()
if not args.jar.is_file() or args.jar.suffix!='.jar':parser.error('Build the backend JAR first')
target=args.package/'Java/backend.jar';target.parent.mkdir(parents=True,exist_ok=True)
temp=target.with_suffix('.jar.tmp');shutil.copyfile(args.jar,temp);temp.replace(target)
print('Players backend prepared; SHA256 '+hashlib.sha256(target.read_bytes()).hexdigest())
