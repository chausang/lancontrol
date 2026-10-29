# SÁCH HƯỚNG DẪN SỬ DỤNG HỆ THỐNG QUẢN LÝ MÁY CHỦ - MÁY KHÁCH LAN CONTROL (HỖ TRỢ ĐA VLAN)

Tài liệu này hướng dẫn chi tiết cách cài đặt, vận hành, kiểm thử và khai thác toàn bộ tính năng của hệ thống quản lý máy chủ - máy khách **LAN Control Multi-VLAN** được xây dựng tại thư mục `D:\lancontrol`.

---

## MỤC LỤC
1. [Tổng Quan Kiến Trúc Hệ Thống](#1-tổng-quan-kiến-trúc-hệ-thống)
2. [Cấu Trúc Các Thành Phần](#2-cấu-trúc-các-thành-phần)
3. [Hướng Dẫn Khởi Chạy Nhanh (Quick Start Demo)](#3-hướng-dẫn-khởi-chạy-nhanh-quick-start-demo)
4. [Hướng Dẫn Chi Tiết Các Chức Năng Trên Master Console](#4-hướng-dẫn-chi-tiết-các-chức-năng-trên-master-console)
   - [4.1. Thanh công cụ Ribbon](#41-thanh-công-cụ-ribbon)
   - [4.2. Cây phân đoạn mạng VLAN](#42-cây-phân-đoạn-mạng-vlan)
   - [4.3. Chức năng "Gọi hỗ trợ" từ máy trạm (Help Request)](#43-chức-năng-gọi-hỗ-trợ-từ-máy-trạm-help-request)
   - [4.4. Lưới màn hình xem trước (Thumbnail Matrix)](#44-lưới-màn-hình-xem-trước-thumbnail-matrix)
   - [4.5. Điều khiển máy từ xa (Remote Desktop)](#45-điều-khiển-máy-từ-xa-remote-desktop)
   - [4.6. Khóa & mở khóa màn hình (Lock/Unlock Screen)](#46-khóa--mở-khóa-màn-hình-lockunlock-screen)
   - [4.7. Phát sóng màn hình giáo viên (Screen Broadcaster)](#47-phát-sóng-màn-hình-giáo-viên-screen-broadcaster)
   - [4.8. Bộ quét dò tìm thiết bị Đa VLAN (Subnet Scanner)](#48-bộ-quét-dò-tìm-thiết-bị-đa-vlan-subnet-scanner)
   - [4.9. Thực thi lệnh & Script hàng loạt (Mass Remote Scripting)](#49-thực-thi-lệnh--script-hàng-loạt-mass-remote-scripting)
   - [4.10. Quản lý tác vụ từ xa (Remote Process Manager)](#410-quản-lý-tác-vụ-từ-xa-remote-process-manager)
   - [4.11. Gửi thông báo tới máy trạm (Send Message)](#411-gửi-thông-báo-tới-máy-trạm-send-message)
   - [4.12. Kiểm kê cấu hình & bản quyền phần mềm (Hardware & License Audit)](#412-kiểm-kê-cấu-hình-phần-cứng--rà-soát-bản-quyền-phần-mềm--phát-hiện-crack)
   - [4.13. Truyền tập tin hàng loạt (Mass File Distribution)](#413-truyền-tập-tin-hàng-loạt-tới-nhiều-máy-trạm-mass-file-distribution)
   - [4.14. Quản lý & truyền tệp từ xa 2 chiều (Remote File Explorer)](#414-quản-lý--truyền-tệp-từ-xa-2-chiều-remote-file-explorer)
5. [Hướng Dẫn Triển Khai Thực Tế Trong Doanh Nghiệp / Phòng Máy](#5-hướng-dẫn-triển-khai-thực-tế-trong-doanh-nghiệp--phòng-máy)
6. [Xử Lý Các Vấn Đề Thường Gặp (Troubleshooting)](#6-xử-lý-các-vấn-đề-thường-gặp-troubleshooting)

---

## 1. Tổng Quan Kiến Trúc Hệ Thống

Khác với các phần mềm gom cụm thụ động (như mRemoteNG thông thường) vốn bị chặn bởi Router/Firewall giữa các VLAN, hệ thống **LAN Control Multi-VLAN** hoạt động theo mô hình **Client - Server - Gateway**:

```
[Máy Trạm Khách: VLAN 10] ---> Kết nối ngược (Outbound TCP 8443) ---> [Central Gateway Server]
[Máy Trạm Khách: VLAN 20] ---> Kết nối ngược (Outbound TCP 8443) ---> [Central Gateway Server]
[Máy Trạm Khách: VLAN 30] ---> Kết nối ngược (Outbound TCP 8443) ---> [Central Gateway Server]
                                                                               ▲
                                                                               │
[Bàn Quản Trị: Master Control] <======= Kênh Điều Khiển & Live Stream =========┘
```

* **Ưu điểm cốt lõi**:
  1. Máy khách ở các phân đoạn VLAN bảo mật khác nhau chỉ cần gửi kết nối **ra ngoài (Outbound)** về máy chủ Gateway trung tâm. Không cần mở bất kỳ cổng Inbound nào tại máy trạm.
  2. Bàn quản trị viên (Master Console) chỉ cần kết nối tới Gateway là có thể quản lý, xem màn hình và điều khiển toàn bộ các máy trạm thuộc mọi VLAN.

---

## 2. Cấu Trúc Các Thành Phần

Tất cả mã nguồn và file thực thi nằm trong thư mục `D:\lancontrol`:

| Tên File | Vai Trò | Cách Sử Dụng |
| :--- | :--- | :--- |
| **`build.bat`** | File script tự động biên dịch toàn bộ dự án | Nhấp đúp để biên dịch mã nguồn C# bằng .NET 4.8 tích hợp sẵn trên Windows ra thư mục `bin\`. |
| **`run_gateway.bat`** | Khởi chạy Central Gateway Server | Chạy trên máy chủ trung tâm (Mặc định lắng nghe cổng `8443`). |
| **`run_agent.bat`** | Khởi chạy Client Agent (VLAN 10) | Chạy ngầm trên máy trạm khách (Gán nhãn: `VLAN 10 - Tech`). |
| **`run_agent_vlan20.bat`** | Khởi chạy Client Agent (VLAN 20) | Chạy trên máy trạm khách khác (Gán nhãn: `VLAN 20 - Accounting`). |
| **`run_control.bat`** | Khởi chạy Master Control Console | Chạy trên máy của Quản trị viên / Giáo viên để quản lý toàn hệ thống. |
| **`cai_dat_agent.bat`** | File cài đặt Agent tự động 1-Click | Nhấp đúp trên máy trạm để mở giao diện cài đặt & khởi chạy Agent nhanh chóng. |
| **`cai_dat_server.bat`** | File cài đặt Server tự động 1-Click | Nhấp đúp trên máy chủ để mở giao diện cài đặt Gateway, mở tường lửa & tạo shortcut. |
| **`bin\LanControl.Core.dll`** | Thư viện dùng chung | Chứa giao thức mạng nhị phân, bộ chụp màn hình GDI+ JPEG, Win32 input và bộ quét IP. |
| **`bin\LanControl.Gateway.exe`** | Ứng dụng Server Gateway | Điều phối kết nối, duy trì nhịp tim (Heartbeat), relay luồng hình ảnh và lệnh điều khiển. |
| **`bin\LanControl.Agent.exe`** | Ứng dụng Máy Khách | Chạy ở khay hệ thống, stream màn hình, nhận lệnh khóa máy, nhận phát sóng. |
| **`bin\LanControl.AgentSetup.exe`** | Trình Cài Đặt & Cấu Hình Agent | Giao diện cấu hình IP, Port 8443, VLAN, chọn nơi lưu, tự khởi động cùng Windows. |
| **`bin\LanControl.ServerSetup.exe`** | Trình Cài Đặt & Cấu Hình Server | Giao diện cấu hình Port 8443, tự mở tường lửa Firewall, IP máy chủ, shortcut. |
| **`bin\LanControl.Control.exe`** | Ứng dụng Quản Trị Viên | Giao diện đồ họa chính (Lưới màn hình, Remote Desktop, Quét IP, v.v.). |



---

## 3. Hướng Dẫn Khởi Chạy Nhanh (Quick Start Demo)

Bạn có thể chạy thử nghiệm toàn bộ hệ thống ngay trên máy tính hiện tại theo 4 bước sau:

### Bước 1: Khởi động Central Gateway
- Mở thư mục `D:\lancontrol\`.
- Nhấp đúp vào file **`run_gateway.bat`**.
- Màn hình console Gateway xuất hiện với thông báo:
  ```text
  ==================================================================
          LAN CONTROL CENTRAL GATEWAY - MULTI-VLAN RELAY HUB         
  ==================================================================
  Reverse Connection Port: 8443
  [SUCCESS] Gateway Server started on 0.0.0.0:8443
  ```
  *(Để cửa sổ này hoạt động ở chế độ nền).*

### Bước 2: Khởi động Máy Khách 1 (VLAN 10)
- Nhấp đúp vào file **`run_agent.bat`**.
- Chương trình Agent khởi chạy ngầm, tự động xuất hiện biểu tượng chữ **`N` màu xanh dương** tại khay hệ thống (System Tray góc dưới bên phải màn hình).
- Trên màn hình Gateway sẽ hiện log: `[SUCCESS] Agent Registered: [...] (VLAN: VLAN 10 - Tech)`.

### Bước 3: Khởi động Máy Khách 2 (VLAN 20)
- Nhấp đúp vào file **`run_agent_vlan20.bat`**.
- Biểu tượng Agent thứ hai được nạp đại diện cho phòng kế toán (`VLAN 20 - Accounting`).

### Bước 4: Mở Bàn Điều Khiển Master Control
- Nhấp đúp vào file **`run_control.bat`**.
- Giao diện quản trị hiện đại xuất hiện và tự động kết nối tới `127.0.0.1:8443`.
- Lúc này bạn sẽ thấy:
  1. Đèn trạng thái chuyển sang: **`🟢 ĐÃ KẾT NỐI GATEWAY`**.
  2. Cây mạng bên trái hiển thị danh mục theo VLAN:
     - `🌐 Tất cả máy trạm (2)`
     - `📁 VLAN 10 - Tech (1)`
     - `📁 VLAN 20 - Accounting (1)`
  3. Lưới màn hình hiển thị trực tiếp hình ảnh thu nhỏ của các máy trạm với thông số CPU, RAM cập nhật liên tục!

---

## 4. Hướng Dẫn Chi Tiết Các Chức Năng Trên Master Console

Giao diện `LanControl.Control` được chia thành 4 khu vực chính:
1. **Thanh công cụ Ribbon trên cùng**: Chứa các nút chức năng nhanh.
2. **Cột phân vùng mạng bên trái (VLAN Tree)**: Phân nhóm máy theo phòng ban / phân đoạn VLAN.
3. **Khu vực hiển thị trung tâm**: Gồm 2 tab:
   - Tab 1: **Lưới Màn Hình Xem Trước (Thumbnail Matrix)**
   - Tab 2: **Danh Sách Chi Tiết Thiết Bị (Device List)**
4. **Thanh trạng thái dưới cùng**: Hiển thị trạng thái kết nối, số máy online, bộ lọc đang chọn và nhật ký hoạt động.

---

### 4.1. Thanh Công Cụ Ribbon

```
[Gateway: 127.0.0.1 : 8443] [🔌 Kết Nối / Ngắt] [🔄 Làm Mới]   [🔍 Tìm máy/IP: ____________]
[📡 Phát Sóng] [🔒 Khóa Máy] [🔓 Mở Khóa] [📁 Truyền Tập Tin] [⚡ Chạy Lệnh] [🔍 Cấu Hình & Bản Quyền] [🔍 Quét IP] [💬 Nhắn Tin] [🔄 Khởi Động Lại] [⛔ Tắt Máy]
```

- **Gateway Server & Cổng**: Điền IP và Port của máy chủ Gateway (Mặc định `127.0.0.1:8443`).
- **Kết Nối / Ngắt Kết Nối**: Bấm để kết nối hoặc ngắt phiên làm việc với Gateway.
- **Tìm máy/IP**: Gõ tên máy hoặc IP để lọc tức thì các máy trên màn hình.
- **Khóa Máy Chọn / Mở Khóa Chọn**: Thực hiện khóa hoặc mở khóa đồng loạt các máy đang được tích chọn.
- **Phát Sóng Màn Hình**: Mở trình chiếu màn hình giáo viên tới học viên (1-to-Many).
- **Truyền Tập Tin**: Mở bảng phát tán và truyền tập tin hàng loạt tới nhiều máy trạm được chọn cùng lúc.
- **Chạy Lệnh / Script**: Mở bảng thực thi script PowerShell hoặc CMD từ xa.
- **Cấu Hình & Bản Quyền**: Mở bảng kiểm kê phần cứng toàn diện và rà soát bản quyền phần mềm / cảnh báo crack.
- **Quét Dãy IP Mạng**: Mở bộ dò tìm thiết bị trong mạng LAN/VLAN.
- **Nhắn Tin**: Gửi tin nhắn popup tới máy trạm.
- **Khởi Động Lại / Tắt Máy**: Gửi lệnh Reboot hoặc Shutdown hàng loạt.

---

### 4.2. Cây Phân Đoạn Mạng & Danh Sách Máy Trạm (Workstation Tree)

Nằm ở thanh điều hướng bên trái:
- Nhấp chuột vào nút **`🌐 Tất cả máy trạm`**: Hiển thị toàn bộ máy tính trong hệ thống.
- Nhấp chuột vào một nhánh phân đoạn (Ví dụ: **`📁 VLAN 10 - Tech`**): Lưới màn hình sẽ lọc và chỉ hiển thị các máy thuộc phòng ban đó.
- **Danh sách chi tiết máy trạm theo từng nhánh**:
  - Dưới mỗi nhánh phân đoạn VLAN, danh sách các máy trạm được hiển thị theo định dạng chuẩn:
    ```text
    🖥️ [Tên máy] - [Địa chỉ IP:Cổng] - [Vlan]
    Ví dụ: 🖥️ DESKTOP-TECH01 - 192.168.1.10:8443 - VLAN 10 - Tech
    ```
  - **Màu sắc đèn báo trạng thái máy**:
    - `🟢 Xanh lá`: Máy đang Online, hoạt động bình thường.
    - `🔵 Xanh dương`: Máy đang trong phiên Remote Desktop.
    - `🔴 Đỏ`: Máy đang bị khóa màn hình.
  - **Thao tác nhanh trên danh sách máy**:
    - **Double Click (Nhấp đúp chuột)** vào tên máy: Mở ngay cửa sổ **Remote Desktop** để điều khiển máy trạm.
    - **Right Click (Nhấn chuột phải)** vào tên máy: Hiển thị đầy đủ Menu ngữ cảnh giống như bên lưới màn hình xem trước (Điều khiển màn hình, Khóa máy, Mở khóa, Chạy script, Quản lý tiến trình, Nhắn tin, Ẩn/Hiện hình nền, Khởi động lại, Tắt máy).

---

### 4.3. Chức Năng "Gọi Hỗ Trợ" Từ Máy Trạm (Help Request)

1. **Từ Máy Trạm (Client Agent)**:
   - Khi người dùng máy trạm gặp sự cố và cần quản trị viên trợ giúp, nhấp chuột phải vào biểu tượng chữ **`N`** dưới khay hệ thống (System Tray).
   - Chọn **`🙋 Gọi Hỗ Trợ (Yêu cầu kết nối)...`**.
   - Xuất hiện hộp thoại cho phép nhập lời nhắn cần hỗ trợ (hoặc để mặc định: *"Cần quản trị viên kết nối hỗ trợ xử lý sự cố."*) -> Bấm **Gửi Yêu Cầu**.
   - Agent hiển thị thông báo góc màn hình: *"Đã gửi yêu cầu kết nối tới Quản trị viên. Vui lòng chờ phản hồi."*
2. **Trên Máy Chủ Quản Trị (Master Console)**:
   - Ngay lập tức, trên màn hình Quản trị viên sẽ phát chuông cảnh báo và xuất hiện hộp thoại khẩn cấp:
     ```
     🔔 CÓ MÁY TRẠM ĐANG GỌI HỖ TRỢ KỸ THUẬT!
     🖥️ Máy trạm: DESKTOP-TECH01 (192.168.1.10)
     🌐 Phân đoạn: VLAN 10 - Tech   |   ⏰ Thời gian: 14:35:10
     💬 Lời nhắn: Cần quản trị viên kết nối hỗ trợ xử lý sự cố.
     [✅ ĐỒNG Ý (KẾT NỐI NGAY)]          [❌ TỪ CHỐI]
     ```
   - **Bấm "Đồng Ý"**:
     - Master Console gửi phản hồi chấp nhận về Agent.
     - **Tự động mở ngay cửa sổ Remote Desktop** để quản trị viên điều khiển máy trạm tức thì.
     - Trên máy trạm hiển thị bóng thông báo: *"Quản trị viên đã đồng ý hỗ trợ và đang kết nối tới máy tính của bạn."*
   - **Bấm "Từ Chối"**:
     - Master Console gửi thông báo từ chối về máy trạm: *"Quản trị viên hiện đang bận hoặc đã từ chối yêu cầu kết nối."*

---

### 4.4. Lưới Màn Hình Xem Trước (Thumbnail Matrix)

Mỗi máy trạm hiển thị dưới dạng một thẻ (Card) gồm:
- **Đèn tín hiệu trạng thái**:
  - `🟢 Xanh lá`: Máy đang Online, hoạt động bình thường.
  - `🔵 Xanh dương`: Máy đang trong phiên Remote Desktop.
  - `🔴 Đỏ`: Máy đang bị khóa màn hình.
- **Ô Checkbox**: Để tích chọn một hoặc nhiều máy phục vụ các thao tác hàng loạt.
- **Tên máy & Địa chỉ IP**: Tên máy trạm và IP nội bộ của máy đó.
- **Nhãn VLAN**: Thẻ màu hiển thị phân đoạn VLAN (VD: `[VLAN 10 - Tech]`).
- **Khung ảnh màn hình**: Cập nhật ảnh chụp màn hình máy trạm theo chu kỳ 1.5 giây.
- **Thanh đo hiệu năng dưới cùng**: Mức chiếm dụng `CPU: 12%`, `RAM: 3420 MB`, và tên tài khoản người dùng đang đăng nhập.

#### Menu Chuột Phải Trên Mỗi Thẻ Máy:
Nhấp chuột phải vào bất kỳ ô máy trạm nào để mở menu ngữ cảnh:
- 🖥️ **Điều Khiển Máy (Remote Desktop)**: Mở phiên điều khiển trực tiếp.
- 🔍 **Xem Cấu Hình & Bản Quyền Phần Mềm...**: Kiểm kê toàn diện phần cứng, ngoại vi & bản quyền/crack.
- 📁 **Quản Lý Tệp Từ Xa (Remote File Explorer)...**: Trình duyệt tệp hai chiều giữa máy chủ và máy trạm.
- 🚀 **Truyền Tập Tin Cho Máy Này (Send Files)...**: Mở bảng truyền tệp nhanh tới máy này.
- 🔒 **Khóa Màn Hình (Lock Screen)**: Khóa máy trạm này.
- 🔓 **Mở Khóa Màn Hình (Unlock Screen)**: Mở khóa máy trạm này.
- ⚡ **Thực Thi Lệnh / Script**: Chạy lệnh riêng cho máy này.
- 📋 **Quản Lý Tác Vụ (Process Manager)**: Xem danh sách tiến trình đang chạy trên máy này.
- 💬 **Gửi Thông Báo (Send Message)**: Gửi tin nhắn popup riêng.
- 🖼️ **Ẩn / Hiện Hình Nền**: Tạm ẩn hình nền để tăng tốc độ truyền hình ảnh.
- 🔄 **Khởi Động Lại (Reboot)** / ⛔ **Tắt Máy (Shutdown)**.

---

### 4.5. Điều Khiển Máy Từ Xa (Remote Desktop)

> [!IMPORTANT]
> **Cơ Chế Điều Khiển Đồng Thời (Co-browsing / Remote Assistance)**:
> Khi bạn kết nối vào máy khách, **máy khách hoàn toàn không bị khóa màn hình**. Cả Quản trị viên và người dùng tại máy khách **đều thấy màn hình và thao tác chuột/phím bình thường cùng lúc**.

- **Cách mở**: Nhấp đúp chuột vào khung ảnh của bất kỳ máy nào trên Lưới màn hình (hoặc bấm chuột phải chọn *Remote Desktop*).
- **Các nút chức năng trên thanh công cụ Remote Desktop**:
  - **`🎮 Điều Khiển / 👁️ Chỉ Xem`**: Cho phép chuyển đổi nhanh giữa chế độ tương tác chuột/phím hoặc chế độ chỉ xem màn hình.
  - **`📁 Truyền Tệp ▾` (MỚI)**: Nút truyền tập tin nhanh trên máy đang điều khiển. Nhấp vào nút sẽ mở menu tiện ích gồm:
    - 🚀 *Truyền Tập Tin Nhanh (Desktop / Downloads / Tự Chạy)*: Gửi tệp có tùy chọn đích đến và tự động chạy bộ cài đặt sau khi nhận.
    - ⚡ *Chọn Tệp Gửi Ngay Ra Desktop*: Mở hộp thoại chọn tệp và gửi thẳng ra Desktop máy trạm ngay lập tức.
    - 📂 *Quản Lý & Duyệt Tệp Từ Xa 2 Chiều*: Mở trình quản lý tệp tin 2 chiều (duyệt ổ đĩa C, D, tải lên / tải về).
    - 💡 *Kéo & Thả Trực Tiếp (Drag & Drop)*: Bạn có thể kéo thả bất kỳ file nào từ máy quản trị thả thẳng vào màn hình điều khiển để gửi siêu tốc!
  - **`💬 Nhắn Tin`**: Gửi tin nhắn popup hoặc thông báo toàn màn hình đến máy khách.
  - **`🔒 Khóa` / `🔓 Mở`**: Khóa màn hình máy khách có chủ đích nếu muốn học viên tạm dừng thao tác hoặc mở khóa.
  - **`🖼️ Ẩn Nền / Hiện Nền`**: Tự động tạm ẩn hình nền máy trạm thành màu đen để giảm dung lượng nén JPEG, tối ưu tốc độ và độ mượt mà.
  - **`🛡️ Nâng Quyền`**: Kích hoạt tự động vượt quyền UAC Admin và mở chặn chuột phím UIPI trên máy trạm.
  - **`✅ UAC` / `❌ UAC`**: Bấm trực tiếp Yes hoặc No trên hộp thoại xác nhận UAC từ xa.
  - **`⛶ Toàn Màn`**: Phóng to cửa sổ điều khiển che kín toàn màn hình, bấm lại để thu nhỏ.
  - **Chỉ số FPS & Live Stream**: Hiển thị tốc độ khung hình thực tế và trạng thái truyền hình ảnh thời gian thực.

---

### 4.6. Khóa & Mở Khóa Màn Hình (Lock/Unlock Screen)

Tính năng này được dùng khi giáo viên muốn học sinh chú ý lên bảng, hoặc quản trị viên bảo trì máy trạm:
1. **Khóa máy**:
   - Tích chọn các máy cần khóa -> Nhấn nút **`🔒 Khóa Máy Chọn`** trên thanh công cụ.
   - Ngay lập tức, trên máy khách sẽ xuất hiện giao diện khóa che kín màn hình với biểu tượng ổ khóa và thông báo:
     ```text
     🔒 LAN CONTROL LOCK SCREEN
     Máy tính tạm thời bị khóa theo yêu cầu của Quản trị viên.
     ```
   - Bàn phím và chuột tại máy khách sẽ bị hạn chế, các phím nóng hệ thống (Alt+F4, Esc) bị chặn.
2. **Mở khóa máy**:
   - Nhấn nút **`🔓 Mở Khóa Chọn`** trên thanh công cụ.
   - Giao diện khóa trên máy khách sẽ tự động biến mất và trả lại quyền sử dụng bình thường.

---

### 4.7. Phát Sóng Màn Hình Giáo Viên (Screen Broadcaster)

Tính năng **1-to-Many Broadcasting** cho phép truyền trực tiếp toàn bộ nội dung màn hình của máy quản trị viên/giáo viên tới đồng loạt nhiều máy trạm:
1. Nhấn nút **`📡 Phát Sóng Màn Hình (1-N)`** trên thanh công cụ.
2. Trong hộp thoại cấu hình:
   - **Phạm vi phát sóng**: Chọn `Tất cả máy trạm (Broadcast All)` hoặc chỉ chọn một phân đoạn cụ thể (VD: `Chỉ phát VLAN: VLAN 10 - Tech`).
   - **Tốc độ (FPS)**: Chọn `5 FPS`, `10 FPS`, `15 FPS` hoặc `20 FPS`.
   - **Chất lượng**: Chọn `40% (Tiết kiệm băng thông)`, `60% (Cân bằng)` hoặc `80% (Sắc nét)`.
3. Nhấn nút màu xanh: **`📡 BẮT ĐẦU PHÁT SÓNG MÀN HÌNH`**.
4. **Hiệu ứng trên máy khách**:
   - Trên màn hình của tất cả các máy trạm trong danh sách được chọn sẽ tự động bật lên một cửa sổ toàn màn hình trình chiếu trực tiếp mọi thao tác của giáo viên với độ trễ cực thấp.
5. Khi kết thúc buổi trình chiếu, nhấn **`🛑 DỪNG PHÁT SÓNG`**, cửa sổ trên các máy khách sẽ tự động đóng lại.

---

### 4.8. Bộ Quét Dò Tìm Thiết Bị Đa VLAN (Subnet Scanner)

Tính năng cho phép dò tìm các máy tính và thiết bị đang hoạt động trong bất kỳ dải IP nào thuộc hạ tầng mạng:
1. Nhấn nút **`🔍 Quét Dãy IP Mạng`** trên thanh công cụ.
2. **Khả năng cấu hình dãy IP linh hoạt**:
   - **Cách 1 - Dãy IP bắt đầu / kết thúc**: Điền `IP Bắt Đầu` (VD: `192.168.1.1`) và `IP Kết Thúc` (VD: `192.168.1.254`).
   - **Cách 2 - Dãy IP theo CIDR**: Điền vào ô CIDR (VD: `192.168.10.0/24` hoặc `10.0.30.0/24`).
   - **Mẫu dải IP nhanh**: Chọn các preset có sẵn trong menu thả xuống (`VLAN 10`, `VLAN 20`, `VLAN 30`, `Dải mạng hiện tại`, `Localhost`).
   - **Cổng cần dò (Ports)**: Điền danh sách cổng cách nhau bởi dấu phẩy (Mặc định: `8443, 3389, 22, 80, 443`).
   - **Timeout & Luồng song song (Threads)**: Tùy chỉnh thời gian chờ (100-1000ms) và số luồng quét (50-200 luồng) để quét cực nhanh.
3. Nhấn **`🔍 BẮT ĐẦU QUÉT`**.
4. Bảng kết quả sẽ hiển thị theo thời gian thực:
   - Địa chỉ IP & Tên máy (Hostname giải mã qua DNS).
   - Trạng thái Online & Độ trễ Ping (ms).
   - Danh sách các cổng đang mở.
   - Cột **`LAN Control Agent?`**: Được đánh dấu nổi bật màu xanh lá `✅ ĐÃ CÀI ĐẶT (Port 8443)` nếu máy đó đã cài Agent.
5. **Xuất báo cáo**: Nhấn **`💾 Xuất File CSV`** để lưu danh sách thiết bị ra file Excel/CSV.

---

### 4.9. Thực Thi Lệnh & Script Hàng Loạt (Mass Remote Scripting)

Cho phép quản trị viên chạy lệnh CMD hoặc kịch bản PowerShell đồng loạt trên nhiều máy trạm mà không cần đến từng máy:
1. Tích chọn các máy trên Lưới màn hình -> Nhấn nút **`⚡ Chạy Lệnh / Script`**.
2. **Cấu hình kịch bản & Mục tiêu**:
   - Chọn mục tiêu: Tất cả máy trạm (All Online), toàn bộ phân đoạn VLAN, hoặc các máy được chọn.
   - Chọn loại script: **PowerShell (`.ps1`)** hoặc **CMD / Batch (`.bat`)**.
3. **Thanh công cụ tiện ích nhanh & Mẫu lệnh mẫu**:
   - **`🧹 Xóa Cache Trình Duyệt...` (Tính năng mới chuyên dụng)**:
     - Hỗ trợ hộp thoại trực quan cấu hình chi tiết (tương tự như cài đặt trên trình duyệt):
       - **Khoảng thời gian (Time range)**: *Từ trước đến nay (All time)* [mặc định], *24 giờ qua*, *7 ngày qua*, *4 tuần qua*.
       - **Hạng mục dữ liệu**: Tệp và hình ảnh được lưu trong bộ nhớ đệm (Cache), Lịch sử duyệt web, Lịch sử tải xuống, Cookies và dữ liệu trang web khác, Tự động đóng trình duyệt trước khi dọn dẹp để giải phóng khóa tệp.
       - **🛡️ BẢO VỆ MẬT KHẨU TUYỆT ĐỐI**: Kịch bản áp dụng bộ lọc bảo vệ nghiêm ngặt, TUYỆT ĐỐI KHÔNG XÓA tệp mật khẩu (`Login Data`, `Login Data-journal`, `logins.json`, `key4.db`), thông tin tài khoản và tự động điền của người dùng được giữ nguyên vẹn 100%.
       - **Dò tìm đa trình duyệt tự động**: Quét và xử lý tự động toàn bộ trình duyệt có trên máy trạm gồm: **Google Chrome, Microsoft Edge, Cốc Cốc, Mozilla Firefox, Brave, Opera, Opera GX, Vivaldi**.
       - Hỗ trợ nút **`⚡ Thực Thi Ngay (Clear Now)`** hoặc **`📝 Chèn Vào Script Editor`** để xem mã PowerShell.
       - **Báo cáo kết quả trực tiếp**: Trả về tổng dung lượng bộ nhớ đệm đã giải phóng (MB/GB), số tệp đã xóa và trạng thái an toàn mật khẩu của từng máy trạm.
   - **`🖨️ Khởi Động Lại Print Spooler` (Khắc phục lỗi kẹt lệnh in)**:
     - Tự động dừng dịch vụ Print Spooler (`Spooler`), cưỡng bức đóng tiến trình `spoolsv` nếu đang bị treo cứng.
     - Dọn sạch toàn bộ các tệp in bị kẹt trong thư mục hàng đợi `C:\Windows\System32\spool\PRINTERS` (`*.shd`, `*.spl`).
     - Khởi động lại dịch vụ Print Spooler, kiểm tra trạng thái hoạt động và liệt kê toàn bộ danh sách máy in, cổng in, driver hiện có trên máy trạm.
   - **`🔍 Kiểm kê toàn bộ phần cứng, màn hình, máy in, máy quét USB (Model, Serial...)` (MẶC ĐỊNH)**:
     - Tự động nạp sẵn kịch bản PowerShell tối ưu chuyên dụng để trích xuất nhanh toàn diện thông tin thiết bị:
       - **1. Thông tin phần cứng tổng quan**: Tên máy tính, Bo mạch chủ (Mainboard model & serial), Bộ vi xử lý (CPU số nhân, luồng, xung nhịp), Bộ nhớ RAM (Tổng dung lượng GB, chi tiết từng thanh, hãng sản xuất, Bus MHz, Serial), Card đồ họa (GPU model, độ phân giải), Ổ đĩa lưu trữ (Model ổ, dung lượng GB, Serial), Hệ điều hành (Phiên bản, kiến trúc 64-bit, Build number), Số Serial hệ thống (BIOS/BaseBoard), Địa chỉ MAC và IP card mạng.
       - **2. Danh sách màn hình phát hiện**: Truy vấn qua giao thức EDID / WMI (`root\wmi:WmiMonitorID`) để đọc chính xác **Hãng sản xuất (Dell, Samsung, LG, HP, Acer, AOC...)**, **Tên Model màn hình (VD: DELL P2314H, DELL U2412M...)** và **Số Serial màn hình (VD: P6MV05AH083S, M2GCR1CQ38KL...)**.
       - **3. Danh sách máy in phát hiện**: Quét qua WMI `Win32_Printer` và đối soát thiết bị `USBPRINT`, trích xuất tên máy in (VD: EPSON EP-803A, HP LaserJet, Canon LBP...), Hãng sản xuất/Driver, Cổng kết nối (`USB001`, `USB002`, `PORTPROMPT:`, `SHRFAX:...`) và **Số Serial USB** của máy in.
       - **4. Danh sách máy quét (Scanner) & Thiết bị ngoại vi USB**: Nhận diện máy quét hình ảnh chuyên dụng (Fujitsu fi-series, Canon DR-series, ScanSnap, HP ScanJet, Epson...), đọc DeviceID và số Serial phần cứng; đồng thời liệt kê các thiết bị ngoại vi USB đang cắm.
   - *Kiểm tra cấu hình IP (`ipconfig /all`)*
   - *Top 10 tiến trình tốn CPU nhất (PowerShell)*
   - *Danh sách dịch vụ Windows đang chạy (`Get-Service`)*
   - *Kiểm tra dung lượng ổ đĩa còn trống*
   - *Xóa file rác trong thư mục Temp*
   - Quản trị viên cũng có thể tự do chỉnh sửa hoặc nhập bất kỳ đoạn mã script nào vào khung soạn thảo.
4. Nhấn **`⚡ Thực Thi Lệnh (Execute)`**:
   - Lệnh được truyền tải mã hóa qua kết nối TCP thời gian thực tới tất cả máy trạm được chỉ định.
   - **Cơ chế thực thi an toàn trên Agent**: Script được ghi tạm thời ra tệp `.ps1`/`.bat` chuẩn mã hóa UTF-8 BOM, thực thi với `powershell.exe -ExecutionPolicy Bypass` và tự động thu hồi, xóa sạch tệp tạm sau khi hoàn tất, đảm bảo chạy mượt mà ngay cả với các kịch bản dài nhiều trang mã và không bị lỗi bảng mã tiếng Việt.
5. **Khung Kết quả trả về (Console Output) & Các nút Xuất File**:
   - Khung console nền tối chuyên nghiệp hiển thị chi tiết kết quả trả về của từng máy trạm.
   - Thanh công cụ xuất báo cáo tích hợp góc trên bên phải khung kết quả:
     - **`📊 Xuất File Excel`**: Hỗ trợ xuất báo cáo sang tệp bảng tính **Microsoft Excel (`.xls` / `.csv`)**. Trích xuất các trường thông tin thành từng cột rõ ràng (Máy trạm, Tên máy, Mainboard, CPU, RAM, GPU, Ổ cứng, OS, Serial, MAC, Màn hình, Máy in & Serial, Máy quét & Ngoại vi USB), mã hóa chuẩn UTF-8 BOM tiếng Việt 100% không bị lỗi font khi mở trong Excel.
     - **`📝 Xuất File .md`**: Xuất toàn bộ kết quả kiểm kê ra tệp tài liệu **Markdown (`.md`)** với định dạng bảng và mục lục rõ ràng, dễ dàng lưu trữ, đồng bộ Git hoặc mở trong Notion, VS Code.
     - **`📄 Xuất TXT`**: Lưu toàn bộ nội dung xuất ra của màn hình điều khiển ra tệp văn bản thuần `.txt`.
     - **`🧹 Xóa`**: Nút dọn sạch nội dung hiển thị trong khung kết quả console.

---

### 4.10. Quản Lý Tác Vụ Từ Xa (Remote Process Manager)

Dùng để theo dõi và tắt các phần mềm gây nặng máy hoặc game/ứng dụng không cho phép trên máy học viên:
1. Nhấp chuột phải vào máy trạm cần kiểm tra -> Chọn **`📋 Quản Lý Tác Vụ (Process Manager)`**.
2. Bảng tiến trình hiển thị: Mã tiến trình (PID), Tên tiến trình (Process Name), Tiêu đề cửa sổ (Window Title) và Dung lượng RAM đang chiếm dụng (MB).
3. Sử dụng ô **🔍 Tìm kiếm** để lọc nhanh tiến trình cần tìm (ví dụ: gõ `chrome`, `game`, `cmd`).
4. Để dừng một tiến trình: Nhấp chọn dòng tiến trình đó và nhấn nút **`⛔ Kết Thúc Tiến Trình (Kill)`**.

---

### 4.11. Gửi Thông Báo Tới Máy Trạm (Send Message)

1. Nhấn nút **`💬 Nhắn Tin`** trên thanh công cụ (hoặc chuột phải vào thẻ máy).
2. Nhập nội dung cần thông báo (Ví dụ: *"Còn 5 phút nữa là hết giờ làm bài kiểm tra!"*).
3. Nhấn **`✉️ Gửi Tin Nhắn`**.
4. Trên màn hình máy khách sẽ lập tức bật lên một hộp thoại thông báo nổi bật kèm âm thanh thông báo của Windows.

---

### 4.12. Kiểm Kê Cấu Hình Phần Cứng & Rà Soát Bản Quyền Phần Mềm / Phát Hiện Crack

Hệ thống tích hợp công cụ kiểm kê chuyên nghiệp giúp bộ phận IT / Quản trị viên nắm bắt toàn bộ thông tin phần cứng và tình trạng bản quyền của từng máy hoặc toàn bộ hệ thống mà không cần đến trực tiếp máy trạm:

1. **Cách mở:**
   - **Cách 1:** Nhấn nút **`🔍 Cấu Hình & Bản Quyền`** trên thanh công cụ Ribbon để kiểm tra đồng thời nhiều máy được chọn (hoặc toàn bộ phòng ban).
   - **Cách 2:** Nhấn chuột phải vào một máy trạm bất kỳ -> Chọn **`🔍 Xem Cấu Hình & Bản Quyền Phần Mềm...`**.
2. **Giao diện hiện đại & Khung nhìn rộng rãi:**
   - Cửa sổ tự động mở **phóng to toàn màn hình (Maximized)** giúp các bảng thông số hiển thị trực quan, thoáng đãng, không bị co nhỏ hay che khuất.
3. **Các thông tin được kiểm kê chi tiết:**
   - **Hệ Điều Hành & Bản Quyền Windows:** Trạng thái kích hoạt (Genuine / OEM / Volume KMS / Trial), kênh cấp phép và **hệ thống cảnh báo màu đỏ tự động nếu phát hiện công cụ Crack/Bẻ khóa** (*KMSpico, KMSAuto, AAct, Windows Loader, MAS, can thiệp file hosts, hoặc dịch vụ bẻ khóa ngầm AutoKMS*).
   - **CPU:** Tên vi xử lý, số nhân (Cores), số luồng (Threads), tốc độ xung nhịp, Processor ID / Serial phần cứng.
   - **Bo Mạch Chủ (Mainboard) & BIOS:** Hãng sản xuất, tên model, số serial bo mạch, phiên bản BIOS và ngày phát hành.
   - **RAM Chi Tiết Từng Khe (DIMM Slots):** Tên khe cắm, dung lượng thanh RAM (GB), Bus RAM (MHz), hãng sản xuất (Samsung, Kingston, Micron...), Part Number, Serial Number.
   - **Ổ Đĩa & Phân Vùng:** Model ổ cứng vật lý, chuẩn kết nối (NVMe, SATA, SCSI, USB), loại ổ (SSD / HDD), số Serial, danh sách phân vùng C:, D: kèm dung lượng trống và tỉ lệ %.
   - **Thiết Bị Ngoại Vi Đã Kết Nối:** Máy in (Printers), Máy quét / Scan / Camera (Imaging), Máy chấm công / Vân tay (Biometrics), thiết bị USB / PnP, Thiết bị Âm thanh.
   - **Phần Mềm Đã Cài Đặt:** Quét toàn bộ Registry 64-bit, 32-bit và HKCU; tự động phân loại thành 4 nhóm: **⚠️ Nghi Vấn Crack / Activator**, **💼 Phần Mềm Thương Mại**, **🆓 Miễn Phí / Mã Nguồn Mở**, và **ℹ️ Ứng Dụng Tiêu Chuẩn**.
     - Bổ sung nút **`📋 Xem Tất Cả`** nổi bật: Cho phép người dùng xem nhanh danh sách toàn bộ phần mềm và xóa sạch ô tìm kiếm chỉ với 1 cú click.
     - Ô tìm kiếm và nút **`✖ Xóa Tìm`** thời gian thực.
4. **Theo Dõi Lịch Sử & Phát Hiện Biến Động Cấu Hình (Tab "🕒 Lịch Sử & Biến Động"):**
   - **Tự động ghi nhận Snapshot cấu hình**: Mỗi lần quét kiểm tra máy trạm, hệ thống tự động lưu bản snapshot cấu hình tại thư mục `audit_history/`.
   - **Tự động so sánh và cảnh báo biến động**: Khi kiểm tra cấu hình mới, hệ thống sẽ đối chiếu ngay lập tức với mốc kiểm tra trước đó:
     - 🚨 *Sụt giảm dung lượng RAM / Tháo bớt thanh RAM*: Phát hiện nguy cơ bị tráo đổi hoặc tháo trộm linh kiện trong phòng máy.
     - 🚨 *Tháo gỡ / Mất ổ đĩa vật lý (HDD/SSD)*.
     - 🔌 *Thay đổi thiết bị ngoại vi*: Rút máy in, máy scan, camera, cắm thiết bị USB lạ.
     - 📦 *Phần mềm mới cài đặt*: Cảnh báo màu đỏ nổi bật nếu phát hiện phần mềm mới cài nghi vấn Crack/Bẻ khóa lậu.
     - 🗑️ *Phần mềm bị gỡ bỏ*.
     - 🔄 *Cập nhật phiên bản phần mềm hoặc bản dựng Windows Update*.
   - **Chọn mốc so sánh linh hoạt**: Menu thả xuống cho phép người quản trị so sánh cấu hình hiện tại với bất kỳ lần quét nào trong quá khứ.
   - **Xuất báo cáo lịch sử biến động ra file `.md`**: Nhấn nút **`📝 Xuất File Lịch Sử (.md)`** để xuất bảng biến động đối chiếu chi tiết ra tệp Markdown.
5. **Các Định Dạng Xuất Báo Cáo:**
   - **`📝 Xuất Chi Tiết (.md)`**: Xuất toàn bộ báo cáo cấu hình chi tiết của máy trạm ra tệp **Markdown (`.md`)** với định dạng bảng, callout alert hiện đại chuẩn GitHub.
   - **`📥 Xuất Báo Cáo HTML`**: Tạo tệp HTML đẹp mắt, tự động mở bằng trình duyệt web để in ấn hoặc nộp báo cáo.
   - **`📄 Xuất Báo Cáo CSV`**: Xuất danh sách phần mềm ra tệp bảng tính tương thích hoàn hảo với Microsoft Excel.

---

### 4.13. Truyền Tập Tin Hàng Loạt Tới Nhiều Máy Trạm (Mass File Distribution)

Cho phép giáo viên / quản trị viên phân phối đồng thời một hoặc nhiều tập tin, tài liệu học tập, đề thi, hoặc bộ cài đặt phần mềm tới hàng loạt máy trạm cùng lúc qua mạng:

1. **Cách mở:**
   - Tích chọn các máy trạm cần gửi (hoặc để trống để gửi cho toàn bộ phòng ban) -> Nhấn nút **`📁 Truyền Tập Tin`** trên thanh công cụ Ribbon.
   - Hoặc nhấn chuột phải vào máy trạm -> Chọn **`🚀 Truyền Tập Tin Cho Máy Này (Send Files)...`**.
2. **Các bước thao tác:**
   - **Bước 1 (Chọn nguồn):** Nhấn **`➕ Thêm Tệp...`** hoặc **`📁 Thêm Thư Mục...`** để chọn các file tài liệu hoặc phần mềm cần gửi đi từ máy quản trị.
   - **Bước 2 (Chọn đích đến trên máy trạm):**
     - 🖥️ *Màn hình chính (Desktop)*: Tệp sẽ xuất hiện ngay ngoài màn hình của học viên / nhân viên.
     - 📥 *Thư mục Tải về (Downloads)*: Lưu vào thư mục Downloads của người dùng hiện tại.
     - 📁 *Thư mục C:\Temp*: Thư mục tạm dùng cho các file hệ thống / bộ cài đặt.
     - 📂 *Đường dẫn tùy chỉnh*: Nhập đường dẫn bất kỳ (ví dụ `D:\BaiTap`, `C:\Setup`).
   - **Bước 3 (Thiết lập tùy chọn):**
     - ☑️ *Ghi đè tệp tin nếu đã tồn tại*: Tự động cập nhật file mới nếu trùng tên.
     - ☑️ *Tự động mở / chạy tệp sau khi truyền xong*: Máy trạm sẽ tự động mở tài liệu (PDF, Word, Excel) hoặc tự động chạy file cài đặt (`.exe`, `.msi`, `.bat`).
     - *Tham số chạy (Arguments)*: Hỗ trợ truyền tham số chạy ngầm (ví dụ `/s`, `/silent`, `/qn` để cài đặt phần mềm âm thầm không làm gián đoạn người dùng).
   - **Bước 4 (Tiến hành truyền):** Nhấn **`🚀 BẮT ĐẦU TRUYỀN HÀNG LOẠT`**.
3. **Theo dõi thời gian thực:**
   - Bảng tiến trình hiển thị rõ ràng từng máy trạm: Tên máy, IP, VLAN, tên tập tin đang gửi, thanh % hoàn thành, dung lượng đã gửi, và trạng thái (`⚡ Đang nhận (65%)...`, `✅ Hoàn tất`).

---

### 4.14. Quản Lý & Truyền Tệp Từ Xa 2 Chiều (Remote File Explorer)

Trình duyệt tập tin hai chiều toàn diện giữa máy quản trị viên và một máy trạm cụ thể theo phong cách giao diện Total Commander / Dual-Pane Explorer:

1. **Cách mở:**
   - Nhấn chuột phải vào máy trạm cần quản lý tệp -> Chọn **`📁 Quản Lý Tệp Từ Xa (Remote File Explorer)...`**.
2. **Bố cục giao diện Dual-Pane:**
   - **Khung bên trái (Local Console):** Hiển thị các ổ đĩa và cây thư mục trên máy của Quản trị viên.
   - **Khung bên phải (Remote Agent):** Hiển thị danh sách ổ đĩa (C:, D:, E:...) và tệp tin thực tế trên máy trạm khách.
3. **Các thao tác hỗ trợ:**
   - **👉 Tải Lên (Upload):** Chọn một hoặc nhiều tệp ở khung bên trái -> Nhấn nút mũi tên xanh **`👉`** (Upload) để đẩy file sang thư mục đang mở trên máy trạm.
   - **👈 Tải Về (Download):** Chọn tệp cần lấy từ máy trạm ở khung bên phải -> Nhấn nút mũi tên xanh dương **`👈`** (Download) để tải file về máy quản trị viên.
   - **▶️ Chạy / Mở tệp từ xa (Execute):** Nhấp chuột phải vào file trên máy trạm -> Chọn **`▶️ Chạy / Mở Tệp Từ Xa (Execute)`** để kích hoạt file chạy ngay trên máy trạm.
   - **📁 Tạo thư mục mới (New Folder):** Chuột phải ở khung máy trạm -> Chọn **`📁 Tạo Thư Mục Mới...`** -> Nhập tên thư mục.
   - **🗑️ Xóa tệp / thư mục từ xa (Delete):** Chuột phải vào tệp hoặc thư mục cần xóa -> Chọn **`🗑️ Xóa Tệp / Thư Mục`** -> Xác nhận xóa an toàn.
   - **⬆️ Chuyển cấp thư mục:** Nhấn nút **`⬆️`** để lùi về thư mục cha hoặc chọn nhanh ổ đĩa từ danh sách thả xuống.

---

### 4.15. Xem Thông Tin Phần Mềm (Nút About & Tệp about.md)

Hệ thống tích hợp cửa sổ thông tin chương trình hiện đại:

1. **Cách mở:**
   - **Trên Master Console (`LanControl.Control.exe`):** Nhấn nút **`ℹ️ Giới Thiệu (About)`** màu xanh ngọc ở hàng trên cùng của thanh công cụ Ribbon.
   - **Trên Client Agent (`LanControl.Agent.exe`):** Nhấp chuột phải vào biểu tượng chữ `N` màu xanh dưới khay hệ thống cạnh đồng hồ -> Chọn **`ℹ️ Giới thiệu (About)...`**.
2. **Thông tin tác giả & Đơn vị phát triển:**
   - **Tác giả:** Bùi Quang Sáng
   - **Đơn vị công tác:** Phòng CNTT - Bệnh viện Đa khoa Khu vực Châu Đốc (BV ĐK Châu Đốc)
   - **Email liên hệ:** `tocnttbkdkkvtinh@gmail.com`
   - **Nền tảng / Cảm hứng:** Phần mềm được vibe coding dựa trên mã nguồn mở [mRemoteNG](https://github.com/mRemoteNG/mRemoteNG).
3. **Nội dung & Tệp `about.md`:**
   - Nội dung hiển thị trong cửa sổ About được nạp trực tiếp từ tệp Markdown **`about.md`** nằm tại thư mục chạy chương trình.
   - **👁️ Giao diện trực quan:** Hiển thị phong cách Dark Theme sang trọng, hỗ trợ nhấp chuột mở trực tiếp liên kết website GitHub và gửi email. Có thể đóng nhanh bằng phím **Esc** hoặc nút **[X]**.

---

## 5. Hướng Dẫn Triển Khai Thực Tế Trong Doanh Nghiệp / Phòng Máy

Để triển khai hệ thống cho toàn bộ công ty hoặc trường học gồm nhiều phòng máy/phân đoạn VLAN:

### Bước 1: Chuẩn Bị Máy Chủ Central Gateway - Tự Động 1-Click

Hệ thống cung cấp công cụ đồ họa **`LanControl.ServerSetup.exe`** (khởi chạy nhanh bằng **`cai_dat_server.bat`**) giúp thực hiện toàn bộ quy trình cấu hình cổng, **tự động mở cổng tường lửa Windows Firewall**, nhận diện IP máy chủ, tạo shortcut màn hình và khởi chạy Gateway **chỉ với 1 cú click chuột**:

#### Cách 1: Cài đặt tự động bằng giao diện (Khuyên dùng - 1 Click)
1. Mở thư mục `D:\lancontrol\` trên máy chủ hoặc máy ảo quản trị.
2. Nhấp đúp vào file **`cai_dat_server.bat`** (hoặc `bin\LanControl.ServerSetup.exe`).
3. Trên giao diện cài đặt máy chủ xuất hiện:
   - **📁 Nơi lưu trữ máy chủ**: Chọn thư mục lưu trữ (Mặc định: `C:\Program Files\LAN Control Server` hoặc `D:\LAN ControlServer`, hoặc bấm **`Duyệt...`**).
   - **🔍 Nguồn file hệ thống**: Chương trình tự động nhận diện đầy đủ 3 file nguồn: `LanControl.Gateway.exe`, `LanControl.Control.exe`, `LanControl.Core.dll`.
   - **🔌 Cổng kết nối Gateway (Port)**: Mặc định là **`8443`**. Có nút **`[Đặt lại 8443]`** và **dòng ghi chú cảnh báo màu cam nổi bật** nếu lỡ xóa hoặc quên cổng.
   - **🌐 Danh sách IP máy chủ phát hiện**: Tự động lấy tất cả địa chỉ IP của máy chủ hiện tại (Ethernet, Wi-Fi,...). Bấm nút **`📋 Copy IP`** để sao chép địa chỉ IP dùng điền vào máy trạm Agent!
   - **🛡️ Tự động mở tường lửa Windows Firewall**: Tích chọn để chương trình tự động tạo quy tắc *Inbound TCP Rule* trên Windows Defender Firewall cho cổng 8443 (không cần mở CMD gõ lệnh).
   - **🚀 Tự động khởi động Gateway cùng Windows**: Tích chọn để Gateway tự bật chạy ngầm mỗi khi máy chủ khởi động lại (cơ chế kép Registry + Startup).
   - **📌 Tạo biểu tượng Lối tắt (Desktop Shortcuts)**: Tích chọn để tự động tạo 2 icon ngoài màn hình Desktop: *LAN Control Master Control* và *LAN Control Central Gateway*.
   - **Trạng thái Gateway thời gian thực**: Theo dõi xem Gateway đang chạy (`🟢 Gateway đang chạy (Cổng: 8443)`) hay chưa chạy, kèm nút **`⏹️ Dừng`**, **`🔄 Khởi Động Lại`**, **`🖥️ Mở Master Control`** và **`📂 Mở Thư Mục Server`**.
4. Nhấn nút lớn màu xanh: **`🚀 CÀI ĐẶT & KHỞI CHẠY GATEWAY NGAY (1-CLICK)`**:
   - Chương trình sẽ tự động sao chép các file vào thư mục đích.
   - Mở cổng tường lửa Windows Firewall cho cổng 8443.
   - Đăng ký tự khởi động cùng Windows và tạo biểu tượng Desktop.
   - Khởi chạy ngay `LanControl.Gateway.exe` và thông báo hoàn tất thành công!

> [!NOTE]
> **Chức năng Gỡ Cài Đặt Máy Chủ (`🗑️ Gỡ Cài Đặt`)**:
> Khi muốn tạm dừng hoạt động hoặc chuyển máy chủ sang máy khác, bạn chỉ cần bấm nút **`🗑️ Gỡ Cài Đặt`**. Chương trình sẽ tự động:
> 1. Dừng tiến trình Gateway đang chạy.
> 2. Xóa bỏ tác vụ tự khởi động cùng Windows (Registry & Startup folder).
> 3. Xóa bỏ quy tắc mở cổng TCP 8443 trên Windows Defender Firewall.
> 4. Xóa tệp cấu hình `gateway_config.json` và các lối tắt Desktop.
> *(LƯU Ý: Toàn bộ các tập tin thực thi `.exe` và `.dll` vẫn được giữ nguyên vẹn trên máy, không bị xóa).*

---

#### Cách 2: Chuẩn bị thủ công qua dòng lệnh (Dành cho Server Core / Command Line)
1. Chọn máy chủ có IP tĩnh (Ví dụ: `192.168.1.100`).
2. Mở cổng tường lửa Windows Firewall cho cổng TCP `8443`:
   - Mở CMD (Run as Administrator) và chạy lệnh:
     ```cmd
     netsh advfirewall firewall add rule name="LAN Control Gateway" dir=in action=allow protocol=TCP localport=8443
     ```
3. Chạy file `bin\LanControl.Gateway.exe 8443` (hoặc tạo một Windows Task / Service để tự khởi động cùng máy chủ).


### Bước 2: Triển Khai Trên Các Máy Trạm (Client PCs) - Tự Động 1-Click

Hệ thống cung cấp công cụ đồ họa **`LanControl.AgentSetup.exe`** (khởi chạy nhanh bằng **`cai_dat_agent.bat`**) giúp thực hiện toàn bộ quá trình cài đặt, cấu hình IP/Port/VLAN, sao chép file và bật khởi động cùng Windows **chỉ với 1 cú click chuột**:

#### Cách 1: Cài đặt tự động bằng giao diện (Khuyên dùng - 1 Click)
1. Copy thư mục cài đặt sang máy trạm (hoặc cắm USB chứa các file: `cai_dat_agent.bat`, `LanControl.AgentSetup.exe`, `LanControl.Agent.exe`, `LanControl.Core.dll`).
2. Nhấp đúp vào **`cai_dat_agent.bat`** (hoặc `LanControl.AgentSetup.exe`).
3. Trên giao diện cài đặt hiện đại xuất hiện:
   - **📁 Nơi lưu trữ cài đặt**: Chọn thư mục trên máy để lưu `LanControl.Agent.exe` (Mặc định: `C:\Program Files\LAN Control Agent` hoặc `D:\LanControlAgent` hoặc bấm **`Duyệt...`** chọn ổ đĩa bất kỳ).
   - **🔍 Nguồn file cài đặt**: Chương trình tự động quét tìm tập tin `LanControl.Agent.exe` và `LanControl.Core.dll` trên máy tính; bạn cũng có thể bấm **`Chọn File...`** nếu để ở vị trí khác.
   - **🌐 Địa chỉ IP máy chủ Gateway**: Nhập IP của máy chủ quản trị (Ví dụ: `192.168.1.100` hoặc IP tĩnh của Gateway).
   - **🔌 Cổng kết nối (Port)**: Giá trị mặc định là **`8443`**. Bên cạnh có nút **`[Đặt lại 8443]`** và **dòng ghi chú nhắc nhở màu cam nổi bật** giúp người cài khôi phục ngay lập tức nếu lỡ xóa nhầm hoặc quên cổng.
   - **🏷️ Phân đoạn VLAN / Phòng Ban**: Chọn từ danh sách mẫu (Phòng Kỹ Thuật, Kế Toán, Kinh Doanh, Đào Tạo,...) hoặc gõ trực tiếp tên phòng ban mong muốn.
   - **🚀 Tự khởi động cùng Windows**: Tích chọn ô này để chương trình thiết lập cơ chế kép (Registry `HKCU\Run` và Windows Startup Folder), đảm bảo máy trạm khi khởi động lại sẽ tự chạy Agent 100%.
   - **Trạng thái Agent**: Theo dõi thời gian thực xem Agent đang chạy ngầm (`🟢 Đang chạy ngầm`) hay chưa chạy, kèm nút **`⏹️ Dừng Agent`** và **`🔄 Khởi Động Lại`**.
4. Nhấn nút lớn màu xanh: **`🚀 CÀI ĐẶT & KHỞI CHẠY NGAY (1-CLICK)`**:
   - Chương trình sẽ tự động tạo thư mục đích, sao chép các tập tin nhị phân cần thiết.
   - Lưu cấu hình vào `agent_config.json`.
   - Đăng ký tự khởi động cùng Windows.
   - Dừng tiến trình cũ (nếu có) và khởi chạy `LanControl.Agent.exe` ngầm trong khay hệ thống (System Tray).
   - Hiển thị hộp thoại thông báo cài đặt hoàn tất thành công!

> [!TIP]
> Nếu bạn chỉ muốn thay đổi địa chỉ IP máy chủ, Port hoặc VLAN của Agent đã cài đặt trước đó mà không cần chép lại file, chỉ cần nhập thông số mới rồi bấm nút **`💾 Lưu Cấu Hình`** -> **`🔄 Khởi Động Lại`**.

> [!NOTE]
> **Chức năng Gỡ Cài Đặt Máy Trạm (`🗑️ Gỡ Cài Đặt`)**:
> Khi muốn tháo gỡ quyền quản lý của máy chủ khỏi máy trạm này, chỉ cần bấm nút **`🗑️ Gỡ Cài Đặt`**. Chương trình sẽ:
> 1. Dừng ngay tiến trình `LanControl.Agent` đang chạy.
> 2. Xóa tác vụ tự khởi chạy cùng Windows (Registry & Startup folder).
> 3. Xóa tệp cấu hình `agent_config.json`.
> *(LƯU Ý: Các tệp thực thi `.exe` và `.dll` KHÔNG bị xóa, có thể tái kích hoạt bất cứ lúc nào).*


---

#### Cách 2: Triển khai thủ công hoặc qua GPO / Batch Script
Nếu quản trị viên IT muốn phân phối qua Active Directory GPO:
1. Copy 2 file: `LanControl.Agent.exe` và `LanControl.Core.dll` vào thư mục máy trạm (ví dụ `C:\Program Files\LAN Control Agent\`).
2. Chạy Agent với tham số chỉ định IP máy chủ Gateway và nhãn phân đoạn VLAN tương ứng:
   - Ví dụ cho phòng máy Kỹ thuật (VLAN 10):
     ```cmd
     LanControl.Agent.exe 192.168.1.100 8443 "VLAN 10 - Phòng Kỹ Thuật"
     ```
   - Ví dụ cho phòng Kế toán (VLAN 20):
     ```cmd
     LanControl.Agent.exe 192.168.1.100 8443 "VLAN 20 - Phòng Kế Toán"
     ```
3. Cấu hình tự khởi động cùng Windows:
   - Thêm vào Registry Run:
     ```cmd
     reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v LanControlAgent /t REG_SZ /d "\"C:\Program Files\LAN Control Agent\LanControl.Agent.exe\"" /f
     ```

### Bước 3: Đặt Máy Quản Trị Viên (Master Console)
- Trên máy của Giáo viên hoặc IT Quản trị, mở `LanControl.Control.exe`.
- Điền IP của máy chủ Gateway (`192.168.1.100:8443`) và nhấn **Kết Nối**.
- Ngay lập tức mọi máy tính ở tất cả các VLAN đã kết nối vào Gateway sẽ hiển thị đầy đủ trên màn hình quản trị.


---

## 6. Xử Lý Các Vấn Đề Thường Gặp (Troubleshooting)

| Vấn đề | Nguyên nhân | Cách khắc phục |
| :--- | :--- | :--- |
| **Agent báo "Gateway connection failed"** | Gateway chưa chạy hoặc sai IP/Port; hoặc tường lửa máy chủ Gateway chặn cổng 8443. | 1. Kiểm tra cửa sổ Gateway đã chạy chưa.<br>2. Kiểm tra lại IP máy chủ trong file cấu hình `agent_config.json`.<br>3. Mở cổng TCP 8443 trên Windows Defender Firewall của máy chủ Gateway. |
| **Đổi IP / Cổng máy chủ Gateway trên máy trạm** | Cần chuyển máy trạm kết nối sang máy chủ Gateway mới hoặc đổi cổng kết nối. | Nhấp chuột phải vào biểu tượng chữ `N` của Agent dưới khay hệ thống -> Chọn **"Kết nối lại Gateway..."** -> Nhập IP máy chủ mới và Port mới (có nút bấm **[Đặt lại 8443]** gợi ý cổng mặc định) -> Bấm **"🔌 Kết Nối Ngay"**. Cấu hình sẽ tự động được lưu vĩnh viễn vào `agent_config.json`. |
| **Muốn đổi nhãn VLAN cho máy khách mà không cần sửa file** | Nhãn VLAN cũ được lưu trong `agent_config.json`. | Nhấp chuột phải vào biểu tượng chữ `N` của Agent dưới khay hệ thống -> Chọn **"Đổi VLAN..."** -> Nhập tên phân đoạn mới -> Bấm Xác nhận. |
| **Quét IP không thấy máy khác trong VLAN** | Tường lửa Windows mặc định chặn gói tin Ping ICMP. | Bộ quét đã tích hợp sẵn cơ chế **TCP Port Probe**. Hãy đảm bảo ô "Cổng Cần Dò" có chứa các cổng phổ biến như `8443, 3389, 80, 443`, bộ quét sẽ phát hiện máy đang hoạt động kể cả khi máy đó chặn Ping. |
| **Chống chạy trùng lặp Agent trên máy trạm** | Người dùng bấm chạy Agent nhiều lần gây lặp danh sách. | LAN Control Agent đã tích hợp cơ chế **Mutex đơn phiên (Single Instance)**: nếu Agent đã chạy trong khay hệ thống, mọi lần khởi động tiếp theo sẽ tự động thoát ngay lập tức để bảo vệ danh sách máy trên Gateway không bị trùng lặp. |
| **Bấm thu nhỏ cửa sổ máy trạm bị mất quyền điều khiển chuột** | 1. Hàm Win32 `mouse_event` cũ bị bỏ qua trên Windows 10/11 Shell & Taskbar (DirectManipulation XAML).<br>2. DPI Scaling ảo hóa tọa độ khiến click vào Taskbar bị lệch.<br>3. Gói tin `MouseMove` gửi dồn dập hàng trăm lần/giây làm trễ `MouseUp`, gây kẹt trạng thái Drag chuột. | **Đã nâng cấp triệt để:**<br>1. Chuyển sang Win32 **`SendInput` (Raw Input Thread)** mô phỏng chuột phần cứng 100%, thao tác mượt mà trên mọi cửa sổ, Desktop và thanh Taskbar.<br>2. Bổ sung **`SetProcessDPIAware()`** giúp tọa độ chuột chuẩn xác từng pixel 1:1 trên màn hình độ phân giải cao.<br>3. Bổ sung **Anti-stuck Watchdog**: Tự động giải phóng trạng thái nhấn chuột (`MOUSEEVENTF_LEFTUP`) ngay khi di chuyển nếu chuột bị kẹt sau thao tác thu nhỏ.<br>4. **Điều tiết tốc độ chuột (Throttling ~40Hz)** và **hàng đợi frame đơn (Single-dispatch rendering)** giúp UI máy chủ không bao giờ bị nghẽn gói tin hay đơ giật. |
| **Hình ảnh Remote Desktop bị trễ khi mạng yếu** | Băng thông mạng LAN bị ảnh hưởng bởi hình nền độ phân giải cao của máy trạm. | Bấm nút **`🖼️ Ẩn Nền (Tăng tốc)`** trên thanh công cụ Remote Desktop để chuyển nền máy trạm thành màu đen, giúp giảm hơn 80% dung lượng truyền tải và thao tác cực nhanh. |
| **Cần biên dịch lại toàn bộ dự án từ mã nguồn** | Muốn thay đổi mã nguồn hoặc bổ sung tính năng mới. | Chỉ cần mở thư mục `D:\lancontrol\` và chạy file `build.bat`. Toàn bộ file DLL và EXE sẽ được tự động biên dịch lại mới hoàn toàn trong vài giây. |

---

*Tài liệu được biên soạn đồng bộ với phiên bản mã nguồn LAN Control Multi-VLAN phát triển tại `D:\lancontrol`.*
