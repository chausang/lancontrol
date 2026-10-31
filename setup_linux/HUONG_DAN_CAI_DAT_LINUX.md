# 🐧 HƯỚNG DẪN CÀI ĐẶT & SỬ DỤNG LAN CONTROL v2.2.0 TRÊN LINUX

Bộ cài đặt trong thư mục `setup_linux` hỗ trợ đầy đủ các bản phân phối Linux phổ biến (**Ubuntu, Debian, Linux Mint, CentOS, RHEL, AlmaLinux, Rocky Linux, Fedora, Arch Linux**) và tương thích **100% giao thức nhị phân** với Máy Chủ / Máy Điều Khiển **LAN Control** trên Windows.

---

## 📦 1. Các thành phần trong thư mục `setup_linux`

| Tệp tin | Chức năng |
| :--- | :--- |
| `install_linux.sh` | **Trình cài đặt 1-Click** (Tự động cài thư viện, tạo cấu hình JSON, đăng ký dịch vụ `systemd` & `XDG Autostart`, mở tường lửa) |
| `uninstall_linux.sh` | **Trình gỡ cài đặt 1-Click** (Dừng dịch vụ, xóa tự khởi động và gỡ sạch `/opt/lancontrol`) |
| `lancontrol_agent.py` | **LAN Control Client Agent** cho máy trạm Linux (Streaming màn hình, nhận điều khiển chuột/phím, nhận phát sóng màn hình, nhận thông báo, xóa cache trình duyệt, kiểm kê phần cứng, truyền tệp 2 chiều) |
| `lancontrol_gateway.py` | **LAN Control Central Gateway Server** cho máy chủ Linux (Cho phép dùng máy chủ Linux làm trạm trung chuyển Đa VLAN trên cổng `8443`) |
| `run_agent.sh` | Chạy nhanh Agent trực tiếp không cần cài đặt |
| `run_gateway.sh` | Chạy nhanh Gateway Server trực tiếp không cần cài đặt |

---

## 🚀 2. Cách cài đặt nhanh (1-Click Interactive)

Mở Terminal tại thư mục `setup_linux` và chạy:

```bash
sudo chmod +x *.sh *.py
sudo ./install_linux.sh
```

Trình cài đặt sẽ hiển thị menu lựa chọn:
- **`[1]` Máy Trạm (Client Agent)**: Nhập IP Máy chủ Gateway (ví dụ `192.168.1.24`), Cổng (`8443`) và Tên phân đoạn VLAN (`VLAN 10 - Phòng Kỹ Thuật`).
- **`[2]` Máy Chủ Trung Tâm (Central Gateway Server)**: Lắng nghe kết nối từ các máy trạm trên cổng `8443`.
- **`[3]` Bộ Đầy Đủ (Cả Agent + Gateway Server)**.

---

## ⚡ 3. Cài đặt tự động hàng loạt qua dòng lệnh (Silent CLI)

Dùng khi triển khai hàng loạt qua SSH / Ansible:

```bash
# Cài đặt Máy trạm (Agent) kết nối về Gateway 192.168.1.24:8443
sudo ./install_linux.sh --agent 192.168.1.24 8443 "VLAN 10 - Phòng Kỹ Thuật"

# Cài đặt Máy chủ trung tâm (Gateway Server) trên cổng 8443
sudo ./install_linux.sh --gateway 8443
```

---

## 🛠️ 4. Các lệnh quản lý dịch vụ trên Linux

```bash
# Kiểm tra trạng thái Agent
sudo systemctl status lancontrol-agent

# Khởi động lại Agent
sudo systemctl restart lancontrol-agent

# Xem nhật ký kết nối của Agent
sudo journalctl -u lancontrol-agent -f

# Gỡ cài đặt hoàn toàn
sudo /opt/lancontrol/uninstall_linux.sh
```
