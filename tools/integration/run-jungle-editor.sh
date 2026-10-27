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
export GORILLA_PACKAGE_ROOT="${GORILLA_PACKAGE_ROOT:-$repo_root}"
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
