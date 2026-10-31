# Hệ Thống Quản Lý Máy Chủ - Máy Khách (LAN Control v2.2.0) Hỗ Trợ Đa VLAN

> **Phiên bản hiện tại**: `v2.2.0` *(Cập nhật: 31/10/2026)* — Xem chi tiết tại [CHANGELOG.md](CHANGELOG.md)

Hệ thống quản trị tập trung Client-Server-Gateway mô phỏng giải pháp **LAN Control Manager & LAN Control School** kết hợp giao diện hiện đại phong cách **mRemoteNG**. Hệ thống giải quyết triệt để rào cản mạng Đa VLAN (Multi-VLAN) nhờ cơ chế kết nối ngược (**Outbound Reverse Connection**) qua máy chủ trung gian (**Central Gateway**).

---

## 1. Cấu Trúc Dự Án

```text
D:\lancontrol\
├── build.bat                    # Script tự động biên dịch toàn bộ dự án bằng .NET Framework 4.8
├── run_gateway.bat              # Khởi chạy Central Gateway Server (Cổng 8443)
├── run_agent.bat                # Khởi chạy Client Agent (Mặc định: VLAN 10 - Tech)
├── run_agent_vlan20.bat         # Khởi chạy Client Agent mô phỏng (VLAN 20 - Accounting)
├── run_control.bat              # Khởi chạy Master Control Console (Giao diện Quản trị)
├── LanControl.sln               # File Solution Visual Studio
├── bin\                         # Thư mục chứa các file thực thi đã biên dịch
│   ├── LanControl.Core.dll      # Thư viện giao thức nhị phân, chụp màn hình, Win32 input
│   ├── LanControl.Gateway.exe   # Máy chủ định tuyến & chuyển tiếp đa VLAN
│   ├── LanControl.Agent.exe     # Dịch vụ/ứng dụng máy khách kết nối ngược
│   └── LanControl.Control.exe   # Bàn điều khiển quản trị viên (Master Console)
└── src\
    ├── LanControl.Core\         # Models, Network Framing, System & Input Utilities
    ├── LanControl.Gateway\      # Async Socket Server, Client Registry, Stream Relay
    ├── LanControl.Agent\        # Reverse Client, Screen Streamer, Locker, Broadcaster
    └── LanControl.Control\      # UI Form, Thumbnail Matrix, Remote Desktop, Subnet Scanner
```

---

## 2. Các Tính Năng Nổi Bật

### 📡 1. Kết Nối Ngược Đa VLAN (Multi-VLAN Reverse Connection)
- Các máy khách (Agent) trong các VLAN khác nhau (`VLAN 10`, `VLAN 20`, `VLAN 30`, v.v.) tự động thiết lập kết nối **Outbound** tới IP của **Central Gateway** (Cổng `8443`).
- **Không cần mở port Inbound** tại từng máy trạm.
- Tự động vượt qua NAT, Firewall giữa các phân đoạn mạng doanh nghiệp.

### 🖥️ 2. Lưới Màn Hình Xem Trước Thời Gian Thực (Thumbnail Matrix Grid)
- Trạm quản trị hiển thị lưới thu nhỏ toàn bộ màn hình của tất cả các máy trạm cùng lúc.
- Hiển thị đầy đủ nhãn VLAN, địa chỉ IP, thước đo mức tải CPU (%), dung lượng RAM trống và tài khoản đăng nhập.
- Lọc tức thì theo phân đoạn VLAN hoặc tìm kiếm theo tên máy / IP.

### 🎮 3. Điều Khiển Máy Từ Xa Tương Tác Đồng Thời (Interactive Remote Desktop)
> [!NOTE]
> Khi người quản trị kết nối vào máy khách, **máy khách không bị khóa màn hình**, cả hai bên đều thấy màn hình và thao tác chuột/phím bình thường (chế độ Remote Assistance / Co-browsing giống LAN Control & TeamViewer).

- Nhấp đúp chuột vào bất kỳ ô nào trên Lưới màn hình để mở ngay cửa sổ **Remote Desktop**.
- Hỗ trợ đầy đủ tương tác chuột (di chuyển, click trái/phải, cuộn con trỏ) và bàn phím.
- Nút chuyển chế độ **Toàn màn hình (Full Screen)**, phím tắt linh hoạt.

### 🔒 4. Khóa & Mở Khóa Màn Hình Chủ Động (Intentional Screen Locking)
- Khi giáo viên hoặc quản trị viên cần tập trung sự chú ý của học viên/nhân viên, có thể nhấn nút **"Khóa Máy Chọn"** hoặc **"Khóa Tất Cả"**.
- Màn hình máy khách sẽ bị che kín bởi giao diện khóa bảo vệ toàn màn hình và chặn các phím nóng.
- Nhấn **"Mở Khóa"** để đưa máy khách trở lại hoạt động bình thường ngay lập tức.

### 📢 5. Phát Sóng Màn Hình Giáo Viên (1-to-Many Screen Broadcaster)
- Cho phép truyền trực tiếp màn hình của máy Quản trị viên (Master/Teacher) tới **đồng loạt hàng trăm máy khách** cùng một lúc.
- Tùy chọn phạm vi phát sóng: Toàn bộ hệ thống hoặc chỉ một phân đoạn VLAN được chọn.
- Tùy chỉnh tốc độ khung hình (5 - 20 FPS) và chất lượng nén ảnh.

### 🔍 6. Bộ Quét Dò Tìm Thiết Bị Đa VLAN (Configurable Subnet Scanner)
- **Cho phép cấu hình linh hoạt dãy IP quét**:
  - Dãy IP bắt đầu - kết thúc (Ví dụ: `192.168.1.1` đến `192.168.1.254`).
  - Hoặc định dạng CIDR (Ví dụ: `192.168.10.0/24`, `10.0.30.0/24`).
  - Danh sách cổng TCP cần dò (Mặc định: `8443, 3389, 22, 80, 443`).
  - Tùy chỉnh thời gian chờ (Timeout ms) và số luồng song song (Threads) lên tới 200 luồng.
  - Các cấu hình mẫu nhanh: Mạng LAN hiện tại, VLAN 10, VLAN 20, VLAN 30, Localhost.
  - Nhận diện máy đã cài đặt LAN Control Agent (Cổng 8443 mở).
  - Hỗ trợ xuất kết quả ra file **CSV** để báo cáo.

### ⚡ 7. Thực Thi Lệnh & Script Hàng Loạt (Mass Remote Scripting)
- Hỗ trợ thực thi lệnh dòng lệnh Windows (`cmd.exe`) hoặc kịch bản **PowerShell** (`.ps1`) đồng loạt.
- Tích hợp sẵn các mẫu script quản trị nhanh (`ipconfig`, `Get-Process Top 10 CPU`, `Get-Service`, kiểm tra dung lượng ổ đĩa, xóa file tạm).
- Trả về toàn bộ kết quả stdout/stderr hiển thị theo thời gian thực.

### 📋 8. Quản Lý Tác Vụ Từ Xa (Remote Process Manager)
- Xem bảng tiến trình đang chạy theo thời gian thực của máy khách (PID, Tên tiến trình, Tiêu đề cửa sổ, RAM sử dụng).
- Hỗ trợ tìm kiếm, lọc nhanh và kết thúc tiến trình bất kỳ (Kill Process).

---

## 3. Hướng Dẫn Biên Dịch & Chạy Thử Nghiệm

### Bước 1: Biên Dịch
Mở thư mục `D:\lancontrol\` và nhấp đúp vào:
```cmd
build.bat
```
*(Hệ thống sử dụng trình biên dịch `csc.exe` đi kèm .NET Framework 4.8 tích hợp sẵn trên Windows, không cần cài đặt thêm bất kỳ công cụ nào).*

### Bước 2: Khởi Chạy Hệ Thống Thử Nghiệm Trên Cùng Một Máy Hoặc Trong Mạng LAN

1. **Khởi chạy Gateway Server**:
   - Nhấp đúp vào `run_gateway.bat` (Lắng nghe cổng `8443`).
2. **Khởi chạy Máy Khách 1 (Giả lập VLAN 10 - Phòng Kỹ Thuật)**:
   - Nhấp đúp vào `run_agent.bat`. Biểu tượng chữ `N` màu xanh sẽ xuất hiện ở khay hệ thống (System Tray).
3. **Khởi chạy Máy Khách 2 (Giả lập VLAN 20 - Phòng Kế Toán)**:
   - Nhấp đúp vào `run_agent_vlan20.bat`.
4. **Khởi chạy Master Control Console (Trạm Quản Trị)**:
   - Nhấp đúp vào `run_control.bat`.
   - Bàn điều khiển sẽ tự động kết nối tới Gateway `127.0.0.1:8443`.
   - Cây mạng bên trái sẽ hiển thị danh mục phân loại theo VLAN:
     - `📁 VLAN 10 - Tech (1)`
     - `📁 VLAN 20 - Accounting (1)`
   - Tab **Lưới Màn Hình** sẽ hiển thị màn hình xem trước thời gian thực của các máy trạm!

---

## 4. Triển Khai Trong Mạng Doanh Nghiệp Thực Tế

1. **Máy Chủ Trung Tâm (Central Server)**:
   - Chạy `LanControl.Gateway.exe 8443` trên máy chủ có IP tĩnh hoặc FQDN (ví dụ: `192.168.1.100` hoặc `gateway.company.lan`).
2. **Các Máy Khách Trạm (Client PCs trên các VLAN)**:
   - Chạy lệnh khởi động cùng Windows:
     ```cmd
     LanControl.Agent.exe 192.168.1.100 8443 "VLAN 10 - Phòng Kỹ Thuật"
     ```
   - Agent sẽ tự động chạy ngầm, duy trì kết nối ngược về Gateway và tự động kết nối lại khi có sự cố mạng.
3. **Máy Quản Trị Viên (IT Admin / Giảng Viên)**:
   - Mở `LanControl.Control.exe`, điền IP của Gateway (`192.168.1.100:8443`) và nhấn **Kết Nối** để bắt đầu quản trị toàn bộ hệ sinh thái mạng.

---

## 5. Lịch Sử Phiên Bản & Thay Đổi Chức Năng (Changelog)

Chi tiết đầy đủ được lưu trữ trong tệp [`CHANGELOG.md`](CHANGELOG.md) và hiển thị trực tiếp trong mục **ℹ️ Giới Thiệu & Phiên Bản** của phần mềm:

| Phiên bản | Ngày phát hành | Nội dung thay đổi chính |
| :--- | :---: | :--- |
| **v2.2.0** | `31/10/2026` | • Thêm hệ thống quản lý **Phiên bản (Version)** & **Lịch sử thay đổi (`CHANGELOG.md`)** trên toàn bộ giao diện.<br>• Sửa lỗi hộp thoại `Thành công` bị cửa sổ `Gửi Tin Nhắn` che khuất.<br>• Nâng cấp menu chuột phải tại icon khay hệ thống (System Tray) và sửa lỗi chọn chức năng từ xa qua Remote Desktop. |
| **v2.1.0** | `29/10/2026` | • Đồng bộ thương hiệu **LAN Control Suite**.<br>• Trình cài đặt hợp nhất 1-Click (`LanControlSetup.exe`) 3 chế độ (Agent / Server / Full).<br>• Hỗ trợ hiển thị hộp thoại UAC & gửi mật khẩu Admin từ xa. |
| **v2.0.0** | `20/09/2026` | • Kiểm kê chuyên sâu cấu hình phần cứng (RAM từng khe, ổ cứng, ngoại vi) & bản quyền Windows.<br>• Cảnh báo biến động phần cứng/phần mềm & xuất báo cáo `.md`.<br>• Truyền tệp hàng loạt đa VLAN (hỗ trợ kéo thả) & nút Gọi Hỗ Trợ từ máy trạm. |
| **v1.0.0** | `19/09/2026` | • Khởi tạo kiến trúc Reverse Connection qua Central Gateway vượt rào cản đa VLAN.<br>• Lưới màn hình Thumbnail Matrix, Remote Desktop, Phát sóng màn hình, Khóa máy, Chạy lệnh CMD/PS, Quét IP. |
