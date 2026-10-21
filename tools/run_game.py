#!/usr/bin/env python3
"""Launch the development Player; Unity owns Java, Vision and the tunnel lifecycle."""
import hashlib
import os
from pathlib import Path
import secrets
import sys

repo = Path(__file__).resolve().parents[1]
runtime = Path(os.environ.get('GORILLA_RUNTIME_ROOT', str(Path.home() / '.gorilla-escape/runtime'))).resolve()
player = repo / 'Unity/Builds/FoundationLinux/GorillaEscape.x86_64'
jar = repo / 'Server/target/local-server-0.1.0-SNAPSHOT.jar'
binary = runtime / 'Tools/cloudflared'
required = [player, jar, runtime / 'Java/runtime/bin/java', runtime / 'Vision/python']
mode = os.environ.get('GORILLA_CONNECTION_MODE', 'CLOUDFLARED')
if mode != 'CABIN':
    required.append(binary)
for item in required:
    if not item.is_file():
        sys.exit(f'Falta {item}. Prepara el build y configura GORILLA_RUNTIME_ROOT con el paquete integrado.')
if mode != 'CABIN' and hashlib.sha256(binary.read_bytes()).hexdigest() != 'd33ff2d14475178d2012c2c56beba87389ac5ded27649519f198a7d3134a99db':
    sys.exit('El binario Cloudflare no coincide con la versión verificada del proyecto.')
vision = player.parent / 'Vision'
if not vision.exists():
    vision.symlink_to(runtime / 'Vision', target_is_directory=True)
env = os.environ.copy()
env.update(
    GORILLA_PACKAGE_ROOT=str(repo), GORILLA_CONNECTION_MODE=mode,
    GORILLA_CLOUDFLARED_PATH=str(binary), GORILLA_MAIN_MENU='1',
    GORILLA_QA_MENU='1', GORILLA_QA_OVERLAY='1', GORILLA_PHONE_INPUT='1',
    GORILLA_IPC_JAVA=str(runtime / 'Java/runtime/bin/java'), GORILLA_IPC_JAR=str(jar),
    GORILLA_IPC_MODE='lifecycle', GORILLA_CAMERA_ENABLED='1',
    GORILLA_CAMERA_PROFILE='candidate', GORILLA_CAMERA_BUFFERS='2',
    GORILLA_LOCAL_TRUST_DIR=env.get('GORILLA_LOCAL_TRUST_DIR', str(Path.home() / '.gorilla-escape/live-qa-trust')),
    GORILLA_TRUST_BOOTSTRAP_PORT='8081', GORILLA_HTTPS_PORT='8443',
    GORILLA_MOBILE_OPERATOR=secrets.token_urlsafe(32),
)
env.pop('GORILLA_MOBILE_LAB', None)
os.execve(str(player), [str(player), '-screen-fullscreen', '0', '-screen-width', '1280', '-screen-height', '940', *sys.argv[1:]], env)
