#!/bin/sh
# Only the fixed firewall helper can receive elevation; the game stays unprivileged.
set -eu
exec /usr/bin/python3 -I "$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)/connectivity/firewall.py" "$@"
