#!/usr/bin/env bash
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
if [ -f "/usr/local/lancontrol/lancontrol_gateway.py" ] && [ ! -f "$SCRIPT_DIR/lancontrol_gateway.py" ]; then
    SCRIPT_DIR="/usr/local/lancontrol"
fi
cd "$SCRIPT_DIR"
exec python3 "$SCRIPT_DIR/lancontrol_gateway.py" "$@"
