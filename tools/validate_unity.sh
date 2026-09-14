#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# Python 3.10+ and an existing licensed Unity editor are required; no auto-install.
ARGS=()
if [[ $# -gt 0 && "$1" != --* ]]; then ARGS+=(--unity "$1"); shift; fi
exec python3 "$ROOT/tools/run_unity_validation.py" "${ARGS[@]}" "$@"
