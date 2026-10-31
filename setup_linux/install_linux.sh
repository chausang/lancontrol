#!/usr/bin/env bash
# ==============================================================================
# LAN CONTROL SUITE v2.2.0 - TRÌNH CÀI ĐẶT TỰ ĐỘNG 1-CLICK CHO LINUX
# Hỗ trợ: Ubuntu, Debian, Linux Mint, CentOS, RHEL, AlmaLinux, Rocky, Fedora, Arch
# Cách chạy:
#   sudo chmod +x install_linux.sh
#   sudo ./install_linux.sh
# Hoặc chạy tự động không cần hỏi (Silent CLI):
#   sudo ./install_linux.sh --agent 192.168.1.24 8443 "VLAN 10 - Phòng Kỹ Thuật"
#   sudo ./install_linux.sh --gateway 8443
#   sudo ./install_linux.sh --full 192.168.1.24 8443 "VLAN 10 - Phòng Kỹ Thuật"
# ==============================================================================

set -e

INSTALL_DIR="/opt/lancontrol"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DEFAULT_GW_IP="192.168.1.24"
DEFAULT_GW_PORT="8443"
DEFAULT_VLAN="VLAN 10 - Phòng Kỹ Thuật"

echo "======================================================================"
echo "   LAN CONTROL SUITE v2.2.0 - BỘ CÀI ĐẶT 1-CLICK DÀNH CHO LINUX       "
echo "======================================================================"

if [ "$EUID" -ne 0 ]; then
    echo "[!] Vui lòng chạy trình cài đặt với quyền root (sudo ./install_linux.sh)"
    exit 1
fi

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
    echo "Chọn chế độ cài đặt LAN Control trên máy Linux này:"
    echo "  [1] Máy Trạm (LAN Control Client Agent) - Kết nối về máy chủ điều khiển"
    echo "  [2] Máy Chủ Trung Tâm (LAN Control Central Gateway Server)"
    echo "  [3] Bộ Đầy Đủ (Cả Agent + Gateway Server trên cùng máy)"
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

echo ""
echo "[1/5] Đang kiểm tra và cài đặt các gói phụ thuộc hệ thống (Python3, Tkinter, Pillow, Scrot)..."
if command -v apt-get >/dev/null 2>&1; then
    apt-get update -y >/dev/null 2>&1 || true
    apt-get install -y python3 python3-pip python3-tk python3-pil python3-pil.imagetk scrot xdotool zenity libnotify-bin >/dev/null 2>&1 || true
elif command -v dnf >/dev/null 2>&1; then
    dnf install -y python3 python3-pip python3-tkinter python3-pillow scrot xdotool zenity >/dev/null 2>&1 || true
elif command -v yum >/dev/null 2>&1; then
    yum install -y python3 python3-pip python3-tkinter python3-pillow scrot xdotool zenity >/dev/null 2>&1 || true
elif command -v pacman >/dev/null 2>&1; then
    pacman -Sy --noconfirm python python-pip tk python-pillow scrot xdotool zenity >/dev/null 2>&1 || true
fi

python3 -m pip install --upgrade mss pillow pyautogui >/dev/null 2>&1 || \
python3 -m pip install --break-system-packages mss pillow pyautogui >/dev/null 2>&1 || true

echo "[2/5] Đang sao chép tệp cài đặt vào $INSTALL_DIR..."
mkdir -p "$INSTALL_DIR"
cp -f "$SCRIPT_DIR/lancontrol_agent.py" "$INSTALL_DIR/lancontrol_agent.py"
cp -f "$SCRIPT_DIR/lancontrol_gateway.py" "$INSTALL_DIR/lancontrol_gateway.py"
[ -f "$SCRIPT_DIR/run_agent.sh" ] && cp -f "$SCRIPT_DIR/run_agent.sh" "$INSTALL_DIR/run_agent.sh"
[ -f "$SCRIPT_DIR/run_gateway.sh" ] && cp -f "$SCRIPT_DIR/run_gateway.sh" "$INSTALL_DIR/run_gateway.sh"
[ -f "$SCRIPT_DIR/uninstall_linux.sh" ] && cp -f "$SCRIPT_DIR/uninstall_linux.sh" "$INSTALL_DIR/uninstall_linux.sh"
chmod +x "$INSTALL_DIR"/*.py "$INSTALL_DIR"/*.sh 2>/dev/null || true

echo "[3/5] Đang khởi tạo tệp cấu hình JSON..."
AGENT_UUID=$(python3 -c 'import uuid; print(uuid.uuid4().hex)')
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

echo "[4/5] Đang thiết lập dịch vụ tự khởi động (systemd & XDG Desktop Autostart)..."
ACTIVE_USER="${SUDO_USER:-$(logname 2>/dev/null || echo root)}"

if [ "$MODE" == "1" ] || [ "$MODE" == "3" ]; then
    # Tạo XDG Desktop Autostart để Agent có quyền truy cập màn hình X11/Wayland của User
    mkdir -p /etc/xdg/autostart
    cat > /etc/xdg/autostart/lancontrol-agent.desktop <<EOF
[Desktop Entry]
Type=Application
Name=LAN Control Client Agent
Comment=LAN Control Multi-VLAN Workstation Agent (v2.2.0)
Exec=/usr/bin/python3 $INSTALL_DIR/lancontrol_agent.py
WorkingDirectory=$INSTALL_DIR
Terminal=false
Hidden=false
NoDisplay=false
X-GNOME-Autostart-enabled=true
EOF

    # Tạo dịch vụ systemd lancontrol-agent.service
    cat > /etc/systemd/system/lancontrol-agent.service <<EOF
[Unit]
Description=LAN Control Multi-VLAN Workstation Agent (v2.2.0)
After=network-online.target graphical.target
Wants=network-online.target

[Service]
Type=simple
User=$ACTIVE_USER
Environment=DISPLAY=:0
Environment=XAUTHORITY=/home/$ACTIVE_USER/.Xauthority
WorkingDirectory=$INSTALL_DIR
ExecStart=/usr/bin/python3 $INSTALL_DIR/lancontrol_agent.py
Restart=always
RestartSec=5

[Install]
WantedBy=multi-user.target
EOF
    systemctl daemon-reload >/dev/null 2>&1 || true
    systemctl enable lancontrol-agent.service >/dev/null 2>&1 || true
    systemctl restart lancontrol-agent.service >/dev/null 2>&1 || true
fi

if [ "$MODE" == "2" ] || [ "$MODE" == "3" ]; then
    cat > /etc/systemd/system/lancontrol-gateway.service <<EOF
[Unit]
Description=LAN Control Central Multi-VLAN Relay Gateway Server (v2.2.0)
After=network-online.target
Wants=network-online.target

[Service]
Type=simple
WorkingDirectory=$INSTALL_DIR
ExecStart=/usr/bin/python3 $INSTALL_DIR/lancontrol_gateway.py $GW_PORT
Restart=always
RestartSec=3

[Install]
WantedBy=multi-user.target
EOF
    systemctl daemon-reload >/dev/null 2>&1 || true
    systemctl enable lancontrol-gateway.service >/dev/null 2>&1 || true
    systemctl restart lancontrol-gateway.service >/dev/null 2>&1 || true

    echo "[5/5] Đang mở cổng tường lửa $GW_PORT/TCP cho Gateway Server..."
    if command -v ufw >/dev/null 2>&1; then
        ufw allow "$GW_PORT"/tcp >/dev/null 2>&1 || true
    elif command -v firewall-cmd >/dev/null 2>&1; then
        firewall-cmd --permanent --add-port="$GW_PORT"/tcp >/dev/null 2>&1 || true
        firewall-cmd --reload >/dev/null 2>&1 || true
    fi
else
    echo "[5/5] Hoàn tất cấu hình dịch vụ."
fi

echo ""
echo "======================================================================"
echo " ✅ CÀI ĐẶT LAN CONTROL v2.2.0 CHO LINUX HOÀN TẤT THÀNH CÔNG!        "
echo "======================================================================"
echo "  • Thư mục cài đặt:     $INSTALL_DIR"
[ "$MODE" != "2" ] && echo "  • Kết nối Gateway:     $GW_IP:$GW_PORT ($VLAN_TAG)"
[ "$MODE" != "1" ] && echo "  • Cổng Gateway Server: 0.0.0.0:$GW_PORT"
echo "  • Kiểm tra trạng thái: sudo systemctl status lancontrol-agent"
echo "  • Gỡ cài đặt:          sudo $INSTALL_DIR/uninstall_linux.sh"
echo "======================================================================"
