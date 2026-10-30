#!/bin/sh
# Launch the current development Player with its supervised phone services.
set -eu
repo_root=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
exec python3 "$repo_root/tools/run_game.py" "$@"
