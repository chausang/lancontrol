# 🍎 HƯỚNG DẪN CÀI ĐẶT & SỬ DỤNG LAN CONTROL v2.2.0 TRÊN MACOS

Bộ cài đặt trong thư mục `setup_macos` hỗ trợ đầy đủ cả **macOS Intel (`x86_64`)** và **macOS Apple Silicon (`M1 / M2 / M3 / M4 - arm64`)**, tương thích **100% giao thức nhị phân** với Máy Chủ / Máy Điều Khiển **LAN Control** trên Windows.

---

## 📦 1. Các thành phần trong thư mục `setup_macos`

| Tệp tin | Chức năng |
| :--- | :--- |
| `install_macos.command` | **Trình cài đặt 1-Click cho macOS** (Nháy đúp chuột trong Finder hoặc chạy qua Terminal; tự động cài đặt vào `/usr/local/lancontrol`, tạo `LaunchAgents` / `LaunchDaemons` và lối tắt ngoài Desktop) |
| `uninstall_macos.command` | **Trình gỡ cài đặt 1-Click cho macOS** (Gỡ sạch dịch vụ `launchd`, lối tắt và thư mục cài đặt) |
| `lancontrol_agent.py` | **LAN Control Client Agent** cho máy trạm Mac (Streaming màn hình Retina, điều khiển chuột/phím, nhận phát sóng màn hình, nhận thông báo, xóa dữ liệu duyệt web Chrome/Safari/Edge/Firefox/Brave/Cốc Cốc bảo vệ mật khẩu, kiểm kê phần cứng Mac, truyền tệp 2 chiều) |
| `lancontrol_gateway.py` | **LAN Control Central Gateway Server** cho máy Mac làm Server trung tâm Đa VLAN (cổng `8443`) |
| `run_agent.command` | Nháy đúp chuột để chạy nhanh Agent trực tiếp |
| `run_gateway.command` | Nháy đúp chuột để chạy nhanh Gateway Server trực tiếp |

---

## 🚀 2. Cách cài đặt nhanh (1-Click trên Finder hoặc Terminal)

### Cách 1: Nháy đúp chuột trên Finder
1. Mở thư mục `setup_macos` trong **Finder**.
2. Nháy đúp chuột vào **`install_macos.command`** (Nếu macOS hỏi xác nhận mở tệp từ nhà phát triển, giữ phím `Control` + Click chuột phải -> chọn **Open**).
3. Nhập mật khẩu quản trị viên máy Mac và chọn chế độ cài đặt (`[1]` Máy Trạm Agent, `[2]` Máy Chủ Gateway, hoặc `[3]` Cả hai).

### Cách 2: Chạy qua Terminal (Hoặc triển khai hàng loạt qua SSH / MDM)
```bash
cd setup_macos
chmod +x *.command *.py
sudo ./install_macos.command
```

Hoặc chạy tự động không cần nhập tay (Silent CLI):
```bash
# Cài đặt Máy trạm Mac kết nối về Gateway 192.168.1.24:8443
sudo ./install_macos.command --agent 192.168.1.24 8443 "VLAN 10 - Phòng Kỹ Thuật"

# Cài đặt Máy chủ trung tâm Gateway trên cổng 8443
sudo ./install_macos.command --gateway 8443
```

---

## 🔐 3. Cấp quyền Ghi Màn Hình & Điều Khiển Chuột Phím trên macOS

Để máy chủ Windows có thể **Xem màn hình trực tiếp (Remote Desktop)** và **Điều khiển chuột/phím** trên máy Mac, lần đầu chạy macOS sẽ hiển thị hộp thoại xin quyền hoặc bạn bật trong:
1. Mở **System Settings (Cài đặt hệ thống)** -> **Privacy & Security (Quyền riêng tư & Bảo mật)**.
2. Mục **Screen & System Audio Recording (Ghi màn hình)**: Bật công tắc cho **Terminal** / **Python**.
3. Mục **Accessibility (Trợ năng)**: Bật công tắc cho **Terminal** / **Python**.

---

## 🛠️ 4. Quản lý dịch vụ & Gỡ cài đặt trên macOS

```bash
# Xem log hoạt động của Agent trên macOS
tail -f /tmp/lancontrol_agent.log

# Gỡ cài đặt hoàn toàn
sudo /usr/local/lancontrol/uninstall_macos.command
```
