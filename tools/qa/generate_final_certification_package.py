#!/usr/bin/env python3
# -*- coding: utf-8 -*-
import os, sys, shutil, hashlib, json, time
from pathlib import Path

repo_root = Path(r"C:\Users\equipo\Documents\Repos\Gorilla Escape")
cert_id = "RELEASE-CERTIFICATION-20261103-190500"
cert_dir = repo_root / "Reports" / "QA" / cert_id

cert_dir.mkdir(parents=True, exist_ok=True)
(cert_dir / "screenshots").mkdir(exist_ok=True)
(cert_dir / "logs").mkdir(exist_ok=True)
(cert_dir / "data").mkdir(exist_ok=True)

print(f"Creating Certification Report in {cert_dir}...")

# 1. Copy Screenshots
ss_src = repo_root / "Reports" / "QA" / "P0_SPLASH_ASSETS" / "screenshots"
if ss_src.exists():
    for f in ss_src.glob("*.png"):
        shutil.copy2(f, cert_dir / "screenshots" / f.name)

# 2. Copy relevant logs
lab_log_src = repo_root / "Reports" / "QA" / "LAB-20261103-185950" / "logs"
if lab_log_src.exists():
    for f in lab_log_src.glob("*.log"):
        shutil.copy2(f, cert_dir / "logs" / f.name)

if (repo_root / "unity_build.log").exists():
    shutil.copy2(repo_root / "unity_build.log", cert_dir / "logs" / "unity_build.log")

print("Copied visual and log evidence.")
