#!/bin/sh
# Developer launcher only. Product packaging remains the existing Phase 0 responsibility.
set -eu
[ "$#" -ge 1 ] || { echo 'Uso: run-jungle-editor.sh /ruta/al/Editor/Unity [opciones]' >&2; exit 2; }
unity_editor=$1
shift
repo_root=$(CDPATH= cd -- "$(dirname -- "$0")/../.." && pwd)
[ -x "$unity_editor" ] || { echo 'No se encontró el Editor indicado.' >&2; exit 2; }
export GORILLA_IPC_JAVA="${GORILLA_IPC_JAVA:-$(command -v java)}"
export GORILLA_IPC_JAR="${GORILLA_IPC_JAR:-$repo_root/Server/target/local-server-0.1.0-SNAPSHOT.jar}"
[ -f "$GORILLA_IPC_JAR" ] || { echo 'Primero compila el servidor y la PWA.' >&2; exit 2; }
# Match the Editor menu: reuse the prepared Phase 0 package when available.
prepared_package="$repo_root/Unity/Builds/FoundationLinux"
if [ -z "${GORILLA_PACKAGE_ROOT:-}" ]; then
    if [ -f "$prepared_package/Vision/manifest.json" ] && [ -f "$prepared_package/tools/setup-firewall.sh" ]; then
        GORILLA_PACKAGE_ROOT=$prepared_package
    else
        GORILLA_PACKAGE_ROOT=$repo_root
    fi
fi
export GORILLA_PACKAGE_ROOT
# Explicit configuration wins. Java verifies the pinned hash before executing any candidate.
if [ -z "${GORILLA_CLOUDFLARED_PATH:-}" ]; then
    for candidate in "$GORILLA_PACKAGE_ROOT/Tools/cloudflared" "$repo_root/Tools/cloudflared" "$prepared_package/Tools/cloudflared"; do
        if [ -f "$candidate" ] && [ -x "$candidate" ]; then
            export GORILLA_CLOUDFLARED_PATH="$candidate"
            break
        fi
    done
fi
export GORILLA_EDITOR_PHYSICAL_QA=1
export GORILLA_MAIN_MENU=1
export GORILLA_QA_OVERLAY=1
export GORILLA_PHONE_INPUT=1
export GORILLA_CONNECTION_MODE="${GORILLA_CONNECTION_MODE:-AUTO}"
export GORILLA_LOCAL_TRUST_DIR="${GORILLA_LOCAL_TRUST_DIR:-$HOME/.gorilla-escape/live-qa-trust}"
export GORILLA_TRUST_BOOTSTRAP_PORT="${GORILLA_TRUST_BOOTSTRAP_PORT:-8081}"
export GORILLA_HTTPS_PORT="${GORILLA_HTTPS_PORT:-8443}"
export GORILLA_MOBILE_OPERATOR=$(python3 -c 'import secrets; print(secrets.token_urlsafe(32))')
unset GORILLA_MOBILE_LAB
exec "$unity_editor" -projectPath "$repo_root/Unity" "$@"
