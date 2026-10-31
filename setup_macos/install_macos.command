#!/usr/bin/env bash
# ==============================================================================
# LAN CONTROL SUITE v2.2.0 - TRÌNH CÀI ĐẶT TỰ ĐỘNG 1-CLICK CHO MACOS
# Hỗ trợ: macOS Intel (x86_64) & Apple Silicon (M1 / M2 / M3 / M4 - arm64)
# Cách chạy:
#   - Nháy đúp chuột trực tiếp vào tệp `install_macos.command` trong Finder
#   - Hoặc chạy trong Terminal:
#       chmod +x install_macos.command
#       sudo ./install_macos.command
#   - Hoặc chạy tự động qua dòng lệnh (Silent CLI):
#       sudo ./install_macos.command --agent 192.168.1.24 8443 "VLAN 10 - Phòng Kỹ Thuật"
#       sudo ./install_macos.command --gateway 8443
# ==============================================================================

set -e

INSTALL_DIR="/usr/local/lancontrol"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
DEFAULT_GW_IP="192.168.1.24"
DEFAULT_GW_PORT="8443"
DEFAULT_VLAN="VLAN 10 - Phòng Kỹ Thuật"

echo "======================================================================"
echo "   LAN CONTROL SUITE v2.2.0 - BỘ CÀI ĐẶT 1-CLICK DÀNH CHO MACOS       "
echo "======================================================================"

if [ "$EUID" -ne 0 ]; then
    echo "[*] Đang yêu cầu quyền Quản trị viên (sudo) để cài đặt dịch vụ hệ thống..."
    exec sudo "$0" "$@"
fi

ACTIVE_USER="${SUDO_USER:-$(stat -f '%Su' /dev/console 2>/dev/null || echo root)}"
USER_HOME=$(eval echo "~$ACTIVE_USER")

MODE=""
GW_IP="$DEFAULT_GW_IP"
GW_PORT="$DEFAULT_GW_PORT"
VLAN_TAG="$DEFAULT_VLAN"

if [ "$1" == "--agent" ]; then
    MODE="1"
    [ -n "$2" ] && GW_IP="$2"
    [ -n "$3" ] && GW_PORT="$3"
    [ -n "$4" ] && VLAN_TAG="$4"
elif [ "$1" == "--gateway" ]; then
    MODE="2"
    [ -n "$2" ] && GW_PORT="$2"
elif [ "$1" == "--full" ]; then
    MODE="3"
    [ -n "$2" ] && GW_IP="$2"
    [ -n "$3" ] && GW_PORT="$3"
    [ -n "$4" ] && VLAN_TAG="$4"
else
    echo ""
    echo "Chọn chế độ cài đặt LAN Control trên máy Mac này:"
    echo "  [1] Máy Trạm (LAN Control Client Agent) - Kết nối về máy chủ điều khiển"
    echo "  [2] Máy Chủ Trung Tâm (LAN Control Central Gateway Server)"
    echo "  [3] Bộ Đầy Đủ (Cả Agent + Gateway Server trên cùng máy Mac)"
    echo ""
    read -p "Nhập lựa chọn của bạn [1/2/3] (Mặc định: 1): " MODE
    MODE="${MODE:-1}"

    if [ "$MODE" == "1" ] || [ "$MODE" == "3" ]; then
        read -p "Nhập địa chỉ IP Máy Chủ Gateway (Mặc định: $DEFAULT_GW_IP): " input_ip
        GW_IP="${input_ip:-$DEFAULT_GW_IP}"
        read -p "Nhập cổng kết nối Gateway Port (Mặc định: $DEFAULT_GW_PORT): " input_port
        GW_PORT="${input_port:-$DEFAULT_GW_PORT}"
        read -p "Nhập tên phân đoạn VLAN (Mặc định: $DEFAULT_VLAN): " input_vlan
        VLAN_TAG="${input_vlan:-$DEFAULT_VLAN}"
    elif [ "$MODE" == "2" ]; then
        read -p "Nhập cổng lắng nghe cho Gateway Server (Mặc định: $DEFAULT_GW_PORT): " input_port
        GW_PORT="${input_port:-$DEFAULT_GW_PORT}"
    fi
fi

PYTHON_BIN=$(command -v python3 || echo "/usr/bin/python3")

echo ""
echo "[1/5] Đang kiểm tra Python3 và cài đặt thư viện hỗ trợ (Pillow, mss, pyautogui)..."
sudo -u "$ACTIVE_USER" "$PYTHON_BIN" -m pip install --user --upgrade pillow mss pyautogui >/dev/null 2>&1 || \
"$PYTHON_BIN" -m pip install --upgrade pillow mss pyautogui >/dev/null 2>&1 || true

echo "[2/5] Đang sao chép tệp cài đặt vào $INSTALL_DIR..."
mkdir -p "$INSTALL_DIR"
cp -f "$SCRIPT_DIR/lancontrol_agent.py" "$INSTALL_DIR/lancontrol_agent.py"
cp -f "$SCRIPT_DIR/lancontrol_gateway.py" "$INSTALL_DIR/lancontrol_gateway.py"
[ -f "$SCRIPT_DIR/run_agent.command" ] && cp -f "$SCRIPT_DIR/run_agent.command" "$INSTALL_DIR/run_agent.command"
[ -f "$SCRIPT_DIR/run_gateway.command" ] && cp -f "$SCRIPT_DIR/run_gateway.command" "$INSTALL_DIR/run_gateway.command"
[ -f "$SCRIPT_DIR/uninstall_macos.command" ] && cp -f "$SCRIPT_DIR/uninstall_macos.command" "$INSTALL_DIR/uninstall_macos.command"
chmod +x "$INSTALL_DIR"/*.py "$INSTALL_DIR"/*.command 2>/dev/null || true

echo "[3/5] Đang khởi tạo tệp cấu hình JSON..."
AGENT_UUID=$("$PYTHON_BIN" -c 'import uuid; print(uuid.uuid4().hex)')
cat > "$INSTALL_DIR/agent_config.json" <<EOF
{
  "AgentId": "$AGENT_UUID",
  "GatewayHost": "$GW_IP",
  "GatewayPort": $GW_PORT,
  "VlanTag": "$VLAN_TAG",
  "ThumbnailIntervalMs": 1500,
  "LiveStreamIntervalMs": 66
}
EOF

cat > "$INSTALL_DIR/gateway_config.json" <<EOF
{
  "Port": $GW_PORT,
  "ListenPort": $GW_PORT,
  "AutoStart": true,
  "InstallPath": "$INSTALL_DIR"
}
EOF
chmod 666 "$INSTALL_DIR/agent_config.json" "$INSTALL_DIR/gateway_config.json"

echo "[4/5] Đang đăng ký tự khởi động cùng macOS (LaunchAgents / LaunchDaemons)..."
if [ "$MODE" == "1" ] || [ "$MODE" == "3" ]; then
    PLIST_AGENT="/Library/LaunchAgents/com.lancontrol.agent.plist"
    cat > "$PLIST_AGENT" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>Label</key>
    <string>com.lancontrol.agent</string>
    <key>ProgramArguments</key>
    <array>
        <string>$PYTHON_BIN</string>
        <string>$INSTALL_DIR/lancontrol_agent.py</string>
    </array>
    <key>WorkingDirectory</key>
    <string>$INSTALL_DIR</string>
    <key>RunAtLoad</key>
    <true/>
    <key>KeepAlive</key>
    <true/>
    <key>ProcessType</key>
    <string>Interactive</string>
    <key>StandardOutPath</key>
    <string>/tmp/lancontrol_agent.log</string>
    <key>StandardErrorPath</key>
    <string>/tmp/lancontrol_agent.err</string>
</dict>
</plist>
EOF
    chown root:wheel "$PLIST_AGENT"
    chmod 644 "$PLIST_AGENT"
    sudo -u "$ACTIVE_USER" launchctl unload "$PLIST_AGENT" 2>/dev/null || true
    sudo -u "$ACTIVE_USER" launchctl load -w "$PLIST_AGENT" 2>/dev/null || true

    # Tạo lối tắt ngoài Desktop
    if [ -d "$USER_HOME/Desktop" ]; then
        cp -f "$INSTALL_DIR/run_agent.command" "$USER_HOME/Desktop/LAN Control Agent.command" 2>/dev/null || true
        chown "$ACTIVE_USER" "$USER_HOME/Desktop/LAN Control Agent.command" 2>/dev/null || true
        chmod +x "$USER_HOME/Desktop/LAN Control Agent.command" 2>/dev/null || true
    fi
fi

if [ "$MODE" == "2" ] || [ "$MODE" == "3" ]; then
    PLIST_GW="/Library/LaunchDaemons/com.lancontrol.gateway.plist"
    cat > "$PLIST_GW" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>Label</key>
    <string>com.lancontrol.gateway</string>
    <key>ProgramArguments</key>
    <array>
        <string>$PYTHON_BIN</string>
        <string>$INSTALL_DIR/lancontrol_gateway.py</string>
        <string>$GW_PORT</string>
    </array>
    <key>WorkingDirectory</key>
    <string>$INSTALL_DIR</string>
    <key>RunAtLoad</key>
    <true/>
    <key>KeepAlive</key>
    <true/>
    <key>StandardOutPath</key>
    <string>/tmp/lancontrol_gateway.log</string>
    <key>StandardErrorPath</key>
    <string>/tmp/lancontrol_gateway.err</string>
</dict>
</plist>
EOF
    chown root:wheel "$PLIST_GW"
    chmod 644 "$PLIST_GW"
    launchctl unload "$PLIST_GW" 2>/dev/null || true
    launchctl load -w "$PLIST_GW" 2>/dev/null || true
fi

echo "[5/5] Hoàn tất cài đặt và kích hoạt dịch vụ."
echo ""
echo "======================================================================"
echo " ✅ CÀI ĐẶT LAN CONTROL v2.2.0 CHO MACOS HOÀN TẤT THÀNH CÔNG!        "
echo "======================================================================"
echo "  • Thư mục cài đặt:     $INSTALL_DIR"
[ "$MODE" != "2" ] && echo "  • Kết nối Gateway:     $GW_IP:$GW_PORT ($VLAN_TAG)"
[ "$MODE" != "1" ] && echo "  • Cổng Gateway Server: 0.0.0.0:$GW_PORT"
echo "  • Lưu ý quyền macOS:   Vào System Settings -> Privacy & Security:"
echo "                         Bật quyền [Screen Recording] và [Accessibility] cho Terminal / Python"
echo "                         để hỗ trợ xem màn hình và điều khiển chuột phím từ xa."
echo "  • Gỡ cài đặt:          sudo $INSTALL_DIR/uninstall_macos.command"
echo "======================================================================"
