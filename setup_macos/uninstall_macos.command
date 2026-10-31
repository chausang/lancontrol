#!/usr/bin/env bash
# ==============================================================================
# LAN CONTROL SUITE v2.2.0 - TRÌNH GỠ CÀI ĐẶT 1-CLICK CHO MACOS
# ==============================================================================

set -e

if [ "$EUID" -ne 0 ]; then
    echo "[*] Đang yêu cầu quyền Quản trị viên (sudo) để gỡ cài đặt..."
    exec sudo "$0" "$@"
fi

ACTIVE_USER="${SUDO_USER:-$(stat -f '%Su' /dev/console 2>/dev/null || echo root)}"
USER_HOME=$(eval echo "~$ACTIVE_USER")

echo "======================================================================"
echo "   ĐANG GỠ CÀI ĐẶT LAN CONTROL TRÊN MACOS...                          "
echo "======================================================================"

if [ -f "/Library/LaunchAgents/com.lancontrol.agent.plist" ]; then
    sudo -u "$ACTIVE_USER" launchctl unload "/Library/LaunchAgents/com.lancontrol.agent.plist" 2>/dev/null || true
    rm -f "/Library/LaunchAgents/com.lancontrol.agent.plist"
fi

if [ -f "/Library/LaunchDaemons/com.lancontrol.gateway.plist" ]; then
    launchctl unload "/Library/LaunchDaemons/com.lancontrol.gateway.plist" 2>/dev/null || true
    rm -f "/Library/LaunchDaemons/com.lancontrol.gateway.plist"
fi

pkill -f lancontrol_agent.py 2>/dev/null || true
pkill -f lancontrol_gateway.py 2>/dev/null || true

rm -rf "/usr/local/lancontrol"
rm -f "$USER_HOME/Desktop/LAN Control Agent.command" 2>/dev/null || true
rm -f "$USER_HOME/Desktop/LAN Control Gateway.command" 2>/dev/null || true

echo "✅ Đã gỡ cài đặt hoàn toàn LAN Control khỏi hệ thống macOS!"
