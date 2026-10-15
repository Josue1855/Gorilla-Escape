#!/bin/sh
set -eu
package_root=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
"$package_root/Vision/python" -I "$package_root/verify-package.py" "$package_root"
export GORILLA_MAIN_MENU=1
export GORILLA_QA_MENU="${GORILLA_QA_MENU:-1}"
export GORILLA_QA_OVERLAY=1
export GORILLA_PHONE_INPUT=1
export GORILLA_IPC_JAVA="$package_root/Java/runtime/bin/java"
export GORILLA_IPC_JAR="$package_root/Java/server.jar"
export GORILLA_IPC_MODE=lifecycle
export GORILLA_CAMERA_ENABLED=1
export GORILLA_CAMERA_PROFILE=candidate
export GORILLA_CAMERA_BUFFERS=2
export GORILLA_LOCAL_TRUST_DIR="${GORILLA_LOCAL_TRUST_DIR:-$HOME/.gorilla-escape/live-qa-trust}"
export GORILLA_TRUST_BOOTSTRAP_PORT="${GORILLA_TRUST_BOOTSTRAP_PORT:-8081}"
export GORILLA_HTTPS_PORT="${GORILLA_HTTPS_PORT:-8443}"
# Launch-only operator credential. Never a file, argument, printed value or admission token.
export GORILLA_MOBILE_OPERATOR=$("$package_root/Vision/python" -I -c 'import secrets; print(secrets.token_urlsafe(32))')
unset GORILLA_MOBILE_LAB
exec "$package_root/GorillaEscape.x86_64" -screen-fullscreen 0 -screen-width 1280 -screen-height 940 "$@"
