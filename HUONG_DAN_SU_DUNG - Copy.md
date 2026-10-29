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
   - [4.3. Lưới màn hình xem trước (Thumbnail Matrix)](#43-lưới-màn-hình-xem-trước-thumbnail-matrix)
   - [4.4. Điều khiển máy từ xa (Remote Desktop)](#44-điều-khiển-máy-từ-xa-remote-desktop)
   - [4.5. Khóa & mở khóa màn hình (Lock/Unlock Screen)](#45-khóa--mở-khóa-màn-hình-lockunlock-screen)
   - [4.6. Phát sóng màn hình giáo viên (Screen Broadcaster)](#46-phát-sóng-màn-hình-giáo-viên-screen-broadcaster)
   - [4.7. Bộ quét dò tìm thiết bị Đa VLAN (Subnet Scanner)](#47-bộ-quét-dò-tìm-thiết-bị-đa-vlan-subnet-scanner)
   - [4.8. Thực thi lệnh & Script hàng loạt (Mass Remote Scripting)](#48-thực-thi-lệnh--script-hàng-loạt-mass-remote-scripting)
   - [4.9. Quản lý tác vụ từ xa (Remote Process Manager)](#49-quản-lý-tác-vụ-từ-xa-remote-process-manager)
   - [4.10. Gửi thông báo tới máy trạm (Send Message)](#410-gửi-thông-báo-tới-máy-trạm-send-message)
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
[📡 Phát Sóng 1-N] [🔒 Khóa Máy Chọn] [🔓 Mở Khóa Chọn] [⚡ Chạy Lệnh] [🔍 Quét IP] [💬 Nhắn Tin] [🔄 Khởi Động Lại] [⛔ Tắt Máy]
```

- **Gateway Server & Cổng**: Điền IP và Port của máy chủ Gateway (Mặc định `127.0.0.1:8443`).
- **Kết Nối / Ngắt Kết Nối**: Bấm để kết nối hoặc ngắt phiên làm việc với Gateway.
- **Tìm máy/IP**: Gõ tên máy hoặc IP để lọc tức thì các máy trên màn hình.
- **Khóa Máy Chọn / Mở Khóa Chọn**: Thực hiện khóa hoặc mở khóa đồng loạt các máy đang được tích chọn.
- **Phát Sóng Màn Hình (1-N)**: Mở trình chiếu màn hình giáo viên tới học viên.
- **Chạy Lệnh / Script**: Mở bảng thực thi script PowerShell hoặc CMD từ xa.
- **Quét Dãy IP Mạng**: Mở bộ dò tìm thiết bị trong mạng LAN/VLAN.
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
- 🔒 **Khóa Màn Hình (Lock Screen)**: Khóa máy trạm này.
- 🔓 **Mở Khóa Màn Hình (Unlock Screen)**: Mở khóa máy trạm này.
- ⚡ **Thực Thi Lệnh / Script**: Chạy lệnh riêng cho máy này.
- 📋 **Quản Lý Tác Vụ (Process Manager)**: Xem danh sách tiến trình đang chạy trên máy này.
- 💬 **Gửi Thông Báo (Send Message)**: Gửi tin nhắn popup riêng.
- 🔄 **Khởi Động Lại (Reboot)** / ⛔ **Tắt Máy (Shutdown)**.

---

### 4.4. Điều Khiển Máy Từ Xa (Remote Desktop)

> [!IMPORTANT]
> **Cơ Chế Điều Khiển Đồng Thời (Co-browsing / Remote Assistance)**:
> Khi bạn kết nối vào máy khách, **máy khách hoàn toàn không bị khóa màn hình**. Cả Quản trị viên và người dùng tại máy khách **đều thấy màn hình và thao tác chuột/phím bình thường cùng lúc**.

- **Cách mở**: Nhấp đúp chuột vào khung ảnh của bất kỳ máy nào trên Lưới màn hình (hoặc bấm chuột phải chọn *Remote Desktop*).
- **Các nút chức năng trên cửa sổ Remote Desktop**:
  - **`🎮 Điều Khiển: BẬT / 👁️ Chỉ Xem: TẮT`**: Cho phép chuyển đổi nhanh giữa chế độ tương tác chuột/phím hoặc chế độ chỉ xem màn hình.
  - **`🖼️ Ẩn Nền (Tăng tốc) / Hiện Nền`**: Tự động tạm ẩn hình nền máy trạm thành màu đơn sắc đen để giảm tới 80% dung lượng nén JPEG, giúp giảm tối đa độ trễ (latency) và thao tác cực kỳ mượt mà. Khi tắt cửa sổ Remote Desktop, hình nền gốc của máy trạm sẽ được khôi phục tự động.
  - **`🔒 Khóa Máy` / `🔓 Mở Khóa`**: Khóa màn hình máy khách có chủ đích nếu muốn học viên tạm dừng thao tác.
  - **`💬 Nhắn Tin`**: Gửi tin nhắn trao đổi nhanh với người đang ngồi tại máy khách.
  - **`⛶ Toàn Màn Hình`**: Phóng to cửa sổ điều khiển che kín toàn màn hình, bấm lại để thu nhỏ.
  - **Chỉ số FPS & Độ phân giải**: Hiển thị tốc độ khung hình thực tế (khoảng 15-20 FPS) và độ phân giải màn hình máy khách.

---

### 4.5. Khóa & Mở Khóa Màn Hình (Lock/Unlock Screen)

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

### 4.6. Phát Sóng Màn Hình Giáo Viên (Screen Broadcaster)

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

### 4.7. Bộ Quét Dò Tìm Thiết Bị Đa VLAN (Subnet Scanner)

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

### 4.8. Thực Thi Lệnh & Script Hàng Loạt (Mass Remote Scripting)

Cho phép quản trị viên chạy lệnh CMD hoặc kịch bản PowerShell đồng loạt trên nhiều máy trạm mà không cần đến từng máy:
1. Tích chọn các máy trên Lưới màn hình -> Nhấn nút **`⚡ Chạy Lệnh / Script`**.
2. **Cấu hình kịch bản**:
   - Chọn mục tiêu: Các máy được chọn, toàn bộ VLAN, hoặc tất cả các máy.
   - Chọn loại script: **PowerShell (`.ps1`)** hoặc **CMD / Batch (`.bat`)**.
   - Chọn lệnh mẫu nhanh:
     - *Kiểm tra cấu hình IP (`ipconfig /all`)*
     - *Top 10 tiến trình tốn CPU nhất (PowerShell)*
     - *Danh sách dịch vụ Windows đang chạy (`Get-Service`)*
     - *Kiểm tra dung lượng ổ đĩa còn trống*
     - *Xóa file rác trong thư mục Temp*
   - Hoặc tự soạn bất kỳ câu lệnh nào vào khung soạn thảo.
3. Nhấn **`⚡ Thực Thi Lệnh (Execute)`**.
4. Khung **Kết quả trả về (Console Output)** bên dưới sẽ hiển thị toàn bộ nội dung xuất ra (stdout/stderr) của từng máy trạm theo thời gian thực.

---

### 4.9. Quản Lý Tác Vụ Từ Xa (Remote Process Manager)

Dùng để theo dõi và tắt các phần mềm gây nặng máy hoặc game/ứng dụng không cho phép trên máy học viên:
1. Nhấp chuột phải vào máy trạm cần kiểm tra -> Chọn **`📋 Quản Lý Tác Vụ (Process Manager)`**.
2. Bảng tiến trình hiển thị: Mã tiến trình (PID), Tên tiến trình (Process Name), Tiêu đề cửa sổ (Window Title) và Dung lượng RAM đang chiếm dụng (MB).
3. Sử dụng ô **🔍 Tìm kiếm** để lọc nhanh tiến trình cần tìm (ví dụ: gõ `chrome`, `game`, `cmd`).
4. Để dừng một tiến trình: Nhấp chọn dòng tiến trình đó và nhấn nút **`⛔ Kết Thúc Tiến Trình (Kill)`**.

---

### 4.10. Gửi Thông Báo Tới Máy Trạm (Send Message)

1. Nhấn nút **`💬 Nhắn Tin`** trên thanh công cụ (hoặc chuột phải vào thẻ máy).
2. Nhập nội dung cần thông báo (Ví dụ: *"Còn 5 phút nữa là hết giờ làm bài kiểm tra!"*).
3. Nhấn **`✉️ Gửi Tin Nhắn`**.
4. Trên màn hình máy khách sẽ lập tức bật lên một hộp thoại thông báo nổi bật kèm âm thanh thông báo của Windows.

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
| **Muốn đổi nhãn VLAN cho máy khách mà không cần sửa file** | Nhãn VLAN cũ được lưu trong `agent_config.json`. | Nhấp chuột phải vào biểu tượng chữ `N` của Agent dưới khay hệ thống -> Chọn **"Đổi VLAN..."** -> Nhập tên phân đoạn mới -> Bấm Xác nhận. |
| **Quét IP không thấy máy khác trong VLAN** | Tường lửa Windows mặc định chặn gói tin Ping ICMP. | Bộ quét đã tích hợp sẵn cơ chế **TCP Port Probe**. Hãy đảm bảo ô "Cổng Cần Dò" có chứa các cổng phổ biến như `8443, 3389, 80, 443`, bộ quét sẽ phát hiện máy đang hoạt động kể cả khi máy đó chặn Ping. |
| **Chống chạy trùng lặp Agent trên máy trạm** | Người dùng bấm chạy Agent nhiều lần gây lặp danh sách. | LAN Control Agent đã tích hợp cơ chế **Mutex đơn phiên (Single Instance)**: nếu Agent đã chạy trong khay hệ thống, mọi lần khởi động tiếp theo sẽ tự động thoát ngay lập tức để bảo vệ danh sách máy trên Gateway không bị trùng lặp. |
| **Bấm thu nhỏ cửa sổ máy trạm bị mất quyền điều khiển chuột** | 1. Hàm Win32 `mouse_event` cũ bị bỏ qua trên Windows 10/11 Shell & Taskbar (DirectManipulation XAML).<br>2. DPI Scaling ảo hóa tọa độ khiến click vào Taskbar bị lệch.<br>3. Gói tin `MouseMove` gửi dồn dập hàng trăm lần/giây làm trễ `MouseUp`, gây kẹt trạng thái Drag chuột. | **Đã nâng cấp triệt để:**<br>1. Chuyển sang Win32 **`SendInput` (Raw Input Thread)** mô phỏng chuột phần cứng 100%, thao tác mượt mà trên mọi cửa sổ, Desktop và thanh Taskbar.<br>2. Bổ sung **`SetProcessDPIAware()`** giúp tọa độ chuột chuẩn xác từng pixel 1:1 trên màn hình độ phân giải cao.<br>3. Bổ sung **Anti-stuck Watchdog**: Tự động giải phóng trạng thái nhấn chuột (`MOUSEEVENTF_LEFTUP`) ngay khi di chuyển nếu chuột bị kẹt sau thao tác thu nhỏ.<br>4. **Điều tiết tốc độ chuột (Throttling ~40Hz)** và **hàng đợi frame đơn (Single-dispatch rendering)** giúp UI máy chủ không bao giờ bị nghẽn gói tin hay đơ giật. |
| **Hình ảnh Remote Desktop bị trễ khi mạng yếu** | Băng thông mạng LAN bị ảnh hưởng bởi hình nền độ phân giải cao của máy trạm. | Bấm nút **`🖼️ Ẩn Nền (Tăng tốc)`** trên thanh công cụ Remote Desktop để chuyển nền máy trạm thành màu đen, giúp giảm hơn 80% dung lượng truyền tải và thao tác cực nhanh. |
| **Cần biên dịch lại toàn bộ dự án từ mã nguồn** | Muốn thay đổi mã nguồn hoặc bổ sung tính năng mới. | Chỉ cần mở thư mục `D:\lancontrol\` và chạy file `build.bat`. Toàn bộ file DLL và EXE sẽ được tự động biên dịch lại mới hoàn toàn trong vài giây. |

---

*Tài liệu được biên soạn đồng bộ với phiên bản mã nguồn LAN Control Multi-VLAN phát triển tại `D:\lancontrol`.*
