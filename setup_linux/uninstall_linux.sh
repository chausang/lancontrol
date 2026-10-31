#!/usr/bin/env bash
# ==============================================================================
# LAN CONTROL SUITE v2.2.0 - TRÌNH GỠ CÀI ĐẶT 1-CLICK CHO LINUX
# ==============================================================================

set -e

if [ "$EUID" -ne 0 ]; then
    echo "[!] Vui lòng chạy với quyền root: sudo ./uninstall_linux.sh"
    exit 1
fi

echo "======================================================================"
echo "   ĐANG GỠ CÀI ĐẶT LAN CONTROL TRÊN LINUX...                          "
echo "======================================================================"

systemctl stop lancontrol-agent.service 2>/dev/null || true
systemctl disable lancontrol-agent.service 2>/dev/null || true
rm -f /etc/systemd/system/lancontrol-agent.service

systemctl stop lancontrol-gateway.service 2>/dev/null || true
systemctl disable lancontrol-gateway.service 2>/dev/null || true
rm -f /etc/systemd/system/lancontrol-gateway.service

systemctl daemon-reload 2>/dev/null || true

rm -f /etc/xdg/autostart/lancontrol-agent.desktop
pkill -f lancontrol_agent.py 2>/dev/null || true
pkill -f lancontrol_gateway.py 2>/dev/null || true

rm -rf /opt/lancontrol

echo "✅ Đã gỡ cài đặt hoàn toàn LAN Control khỏi hệ thống Linux!"
