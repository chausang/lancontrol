# HƯỚNG DẪN CÀI ĐẶT HỆ THỐNG QUẢN LÝ MÁY TÍNH LAN CONTROL (ĐA VLAN)
*(Tài liệu hướng dẫn cài đặt trực quan, chi tiết, dễ hiểu dành cho Quản trị viên IT, Giáo viên và Kỹ thuật viên)*

---

## 📌 MỤC LỤC
1. [Giới Thiệu Chung & Nguyên Lý Hoạt Động](#1-giới-thiệu-chung--nguyên-lý-hoạt-động)
2. [Gói Tệp Cài Đặt Cần Chuẩn Bị](#2-gói-tệp-cài-đặt-cần-chuẩn-bị)
3. [Phần 1: Hướng Dẫn Cài Đặt Phía Máy Chủ (Server / Gateway)](#3-phần-1-hướng-dẫn-cài-đặt-phía-máy-chủ-server--gateway)
4. [Phần 2: Hướng Dẫn Cài Đặt Phía Máy Trạm (Client PCs / Agent)](#4-phần-2-hướng-dẫn-cài-đặt-phía-máy-trạm-client-pcs--agent)
5. [Phần 3: Hướng Dẫn Gỡ Cài Đặt (Xóa Cấu Hình & Tự Khởi Động)](#5-phần-3-hướng-dẫn-gỡ-cài-đặt-xóa-cấu-hình--tự-khởi-động)
6. [Phần 4: Mẹo Đóng Gói USB Đi Cài Đặt Hàng Loạt Máy Trạm](#6-phần-4-mẹo-đóng-gói-usb-đi-cài-đặt-hàng-loạt-máy-trạm)
7. [Phần 5: Các Câu Hỏi Thường Gặp & Xử Lý Sự Cố Khi Cài Đặt](#7-phần-5-các-câu-hỏi-thường-gặp--xử-lý-sự-cố-khi-cài-đặt)
8. [Phần 6: Hướng Dẫn Cập Nhật & Nâng Cấp Tính Năng (Update Guide)](#8-phần-6-hướng-dẫn-cập-nhật--nâng-cấp-tính-năng-update-guide)

---

## 1. Giới Thiệu Chung & Nguyên Lý Hoạt Động

Hệ thống quản lý máy tính **LAN Control Multi-VLAN** hoạt động theo mô hình **Kết Nối Ngược (Reverse Connection)** qua máy chủ trung gian **Central Gateway**:

```text
 ┌────────────────────────┐                  ┌────────────────────────┐
 │   MÁY TRẠM PHÒNG BAN   │                  │   MÁY TRẠM PHÒNG BAN   │
 │   (VLAN 10 - Kỹ thuật) │                  │   (VLAN 20 - Kế toán)  │
 └───────────┬────────────┘                  └───────────┬────────────┘
             │ (Kết nối về cổng 8443)                    │ (Kết nối về cổng 8443)
             ▼                                           ▼
       ┌───────────────────────────────────────────────────────┐
       │             MÁY CHỦ TRUNG TÂM (CENTRAL GATEWAY)       │
       │           Địa chỉ IP: 192.168.1.100 | Port: 8443     │
       └───────────────────────────▲───────────────────────────┘
                                   │
                     (Điều khiển, xem màn hình, khóa máy)
                                   │
       ┌───────────────────────────┴───────────────────────────┐
       │              MÁY QUẢN TRỊ VIÊN / GIÁO VIÊN            │
       │               (LAN Control Master Control)             │
       └───────────────────────────────────────────────────────┘
```

- **Máy trạm (Agent)**: Chỉ cần biết địa chỉ IP và Cổng của máy chủ Gateway là tự động kết nối về, vượt qua mọi rào cản chia tách mạng giữa các phòng ban (VLAN).
- **Máy quản trị (Master Control)**: Kết nối vào Gateway để xem toàn bộ danh sách máy, phân loại theo từng phòng ban, xem trước màn hình thời gian thực và điều khiển từ xa.

---

## 2. Gói Tệp Cài Đặt Cần Chuẩn Bị

Toàn bộ hệ thống được xây dựng trên nền tảng **.NET Framework 4.8** (đã tích hợp sẵn trên Windows 10 và Windows 11, không cần cài thêm bất kỳ phần mềm phụ trợ nào).

Các tệp chính nằm trong thư mục `D:\lancontrol\`:

| Tệp Thực Thi / Script | Dành Cho Máy Nào? | Chức Năng |
| :--- | :--- | :--- |
| **`cai_dat.bat`** / **`LanControlSetup.exe`** | **Bộ Cài Đặt 1-Click Toàn Năng (All-In-One)** | Giao diện 1 màn hình duy nhất: chọn phiên bản cài (**Agent**, **Server**, hoặc **Cả 2**), điền nhanh IP/Port/Thư mục và bấm 1 nút **`🚀 CÀI ĐẶT & KHỞI CHẠY NGAY (1-CLICK)`** (kèm nút **`🗑️ Gỡ Cài Đặt`**). |
| **`setup\cai_dat.bat`** / **`setup\LanControlSetup.exe`** | **Gói USB Độc Lập 1-Click** | Bản đóng gói đầy đủ trong thư mục `setup\` để chép vào USB mang đi cài đặt nhanh 1-Click trên mọi máy tính. |
| **`cai_dat_server.bat`** | **Máy Chủ (Server)** | Nhấp đúp để mở bảng cài đặt & cấu hình Máy chủ Gateway tự động 1-Click. |
| **`cai_dat_agent.bat`** | **Máy Trạm (Client)** | Nhấp đúp trên máy trạm để mở bảng cài đặt & kết nối Máy trạm tự động 1-Click. |
| **`bin\LanControl.ServerSetup.exe`** | Máy Chủ | Ứng dụng đồ họa cài đặt Máy chủ. |
| **`bin\LanControl.AgentSetup.exe`** | Máy Trạm | Ứng dụng đồ họa cài đặt Máy trạm. |
| **`bin\LanControl.Gateway.exe`** | Máy Chủ | Tiến trình máy chủ Gateway trung gian chuyển tiếp dữ liệu. |
| **`bin\LanControl.Control.exe`** | Máy Quản Trị / Giáo Viên | Bàn điều khiển quản trị viên (Xem màn hình, điều khiển chuột, quét IP, nhắn tin). |
| **`bin\LanControl.Agent.exe`** | Máy Trạm | Ứng dụng máy trạm chạy ngầm khay hệ thống để truyền hình ảnh và nhận lệnh. |
| **`bin\LanControl.Core.dll`** | Dùng chung cho tất cả | Thư viện giao thức mạng, chụp màn hình và mô phỏng chuột bàn phím. |
| **`icon.ico`** | Dùng chung cho tất cả | Tệp biểu tượng logo chính thức của phần mềm (được nhúng và gắn vào lối tắt Desktop). |

---

## ⭐ Hướng Dẫn Cài Đặt 1-Click Toàn Năng (Agent / Server / Cả 2) - Khuyên Dùng

Trình cài đặt **`LanControlSetup.exe`** (khởi chạy qua **`cai_dat.bat`** tại thư mục gốc hoặc trong thư mục **`setup\`**) tích hợp toàn bộ thao tác lên **1 màn hình duy nhất**, giúp bạn cài đặt hoặc gỡ cài đặt **Agent**, **Server** hoặc **Cả 2** chỉ với 1 cú click chuột.

### Các Bước Thực Hiện (Trên 1 Màn Hình Duy Nhất):
1. Nhấp đúp vào **`cai_dat.bat`** *(hoặc `LanControlSetup.exe` / `setup\Setup.exe`)*.
2. **Mục 1 - Chọn Thành Phần Cài Đặt (Nhấp chọn 1 trong 3 thẻ):**
   - 🖥️ **Chỉ Cài Agent (Máy Trạm)**: Cài cho máy học sinh / nhân viên / khoa phòng (`LanControl.Agent.exe`).
   - 🏢 **Chỉ Cài Server (Máy Chủ)**: Cài cho máy giáo viên / quản trị viên IT (`LanControl.Gateway.exe` + `LanControl.Control.exe`).
   - 🌟 **Cài Cả 2 (Agent + Server)**: Cài đầy đủ cả máy chủ và máy trạm trên cùng 1 máy tính.
3. **Mục 2 - Nơi Lưu Trữ Cài Đặt:**
   - Mặc định lưu tại `C:\Program Files\lancontrol` (tự động tạo thư mục con `lancontrol` nếu bạn chọn thư mục khác).
4. **Mục 3 - Cấu Hình Nhanh (Tự động bật/tắt theo chế độ đã chọn):**
   - **3A. Cấu hình Máy Trạm (Agent)**: Nhập IP Máy chủ Gateway (hoặc bấm `🏠 IP Máy Này`), Cổng `8443`, Chọn phân đoạn VLAN / Phòng ban, Tick tự động khởi chạy cùng Windows & Hỗ trợ điều khiển xuyên qua UAC Admin.
   - **3B. Cấu hình Máy Chủ (Server)**: Cổng lắng nghe `8443`, Xem/Copy nhanh IP LAN của máy chủ, Tick tự động mở tường lửa Windows Firewall & tự chạy Gateway cùng Windows. Có sẵn các nút mở nhanh `🖥️ Mở Master Console`, `🛡️ Mở Gateway`, `⏹️ Dừng Dịch Vụ`.
5. **Bấm Nút Thực Thi 1-Click Ở Thanh Dưới Cùng:**
   - Nhấn **`🚀 CÀI ĐẶT & KHỞI CHẠY NGAY (1-CLICK)`**: Tự động dừng tiến trình cũ, chép tệp, lưu cấu hình JSON, đăng ký tự khởi động (Registry + Startup + Task Scheduler), mở Firewall, tạo lối tắt Desktop gắn `icon.ico` và khởi chạy ứng dụng ngay lập tức!
   - Nhấn **`💾 Chỉ Lưu Cấu Hình`**: Cập nhật nhanh IP/Port/VLAN mà không cần cài lại tệp.
   - Nhấn **`🗑️ Gỡ Cài Đặt`**: Dừng dịch vụ, xóa cấu hình, xóa tự khởi động và lối tắt của phiên bản đang chọn.

---

## 3. Phần 1: Hướng Dẫn Cài Đặt Phía Máy Chủ (Server / Gateway)

Thực hiện trên máy tính hoặc máy ảo được chọn làm **Máy Chủ Trung Tâm** (nên là máy có địa chỉ IP cố định, ví dụ: `192.168.1.100`).

### 🛠️ Các Bước Thực Hiện (Chỉ Với 1 Cú Click Chuột):

#### Bước 1: Khởi Chạy Bộ Cài Đặt Máy Chủ
- Mở thư mục chứa chương trình trên máy chủ: `D:\lancontrol\`.
- Nhấp đúp vào tệp: **`cai_dat_server.bat`** *(hoặc mở `bin\LanControl.ServerSetup.exe`)*.

#### Bước 2: Kiểm Tra & Tùy Chỉnh Thông Số Trên Giao Diện
Cửa sổ **TRÌNH CÀI ĐẶT & CẤU HÌNH MÁY CHỦ** sẽ xuất hiện với tông màu tối hiện đại:

1. **Nơi lưu trữ các tập tin máy chủ**:
   - **Tự động nhận diện**: Mặc định tự động điền thư mục hiện tại đang chạy tập tin cài đặt.
   - Nếu bạn muốn lưu vào vị trí khác, bấm nút **`📂 Duyệt...`** để chọn.
2. **Nguồn tập tin hệ thống**:
   - Chương trình tự động quét và báo màu xanh: `✅ Đã tìm thấy đầy đủ 3/3 file hệ thống nguồn`.
3. **Cổng kết nối Gateway (Port)**:
   - Mặc định là **`8443`**.
   - > [!IMPORTANT]
     > Nếu bạn lỡ tay xóa hoặc quên cổng, hãy bấm nút **`🔄 Đặt lại 8443`**.
4. **Địa chỉ IP máy chủ phát hiện**:
   - Khung sẽ hiển thị địa chỉ IP nội bộ của máy chủ (ví dụ: `192.168.1.100`).
   - Bấm nút **`📋 Copy IP`** để lưu địa chỉ IP này lại — bạn sẽ dùng IP này để điền vào các máy trạm!
5. **Các tùy chọn tự động hóa (Khuyên để nguyên dấu tick ☑)**:
   - `☑ Tự động mở cổng tường lửa Windows Defender Firewall (TCP 8443)`: Giúp tự mở luồng mạng mà không cần dùng lệnh thủ công.
   - `☑ Tự động khởi động Central Gateway cùng Windows`: Bật máy chủ là Gateway tự chạy ngầm ngay.
   - `☑ Tạo biểu tượng Lối tắt (Desktop Shortcuts)`: Tạo sẵn icon *LAN Control Master Control* và *Gateway* trên Desktop.

#### Bước 3: Hoàn Tất Cài Đặt
- Bấm nút lớn màu xanh lá: **`🚀 CÀI ĐẶT & KHỞI CHẠY GATEWAY NGAY (1-CLICK)`**.
- Chương trình sẽ tự động:
  - Sao chép toàn bộ tệp vào thư mục đích.
  - Tự động tạo luật mở cổng 8443 trên tường lửa Windows Firewall.
  - Đăng ký tự khởi động cùng Windows.
  - Tạo biểu tượng trên Desktop.
  - Khởi chạy Gateway và thông báo: `🎉 CÀI ĐẶT VÀ KHỞI CHẠY MÁY CHỦ THÀNH CÔNG!`.
- Trên giao diện, dòng trạng thái sẽ hiển thị: `🟢 Gateway đang chạy (Cổng 8443 - PID: ...)`.

> [!TIP]
> Để mở bàn điều khiển quản trị viên xem các máy trạm, bạn chỉ cần ra màn hình chính nhấp đúp vào biểu tượng **`LAN Control Master Control`** (hoặc bấm nút **`🖥️ Mở Master Control`** ngay trên bảng cài đặt).

---

## 4. Phần 2: Hướng Dẫn Cài Đặt Phía Máy Trạm (Client PCs / Agent)

Thực hiện trên từng máy tính của học viên, nhân viên hoặc các phòng ban cần quản lý.

### 🛠️ Các Bước Thực Hiện (1 Cú Click Chuột):

#### Bước 1: Khởi Chạy Bộ Cài Đặt Máy Trạm
- Cắm USB hoặc mở thư mục chứa chương trình trên máy trạm.
- Nhấp đúp vào tệp: **`cai_dat_agent.bat`** *(hoặc `LanControl.AgentSetup.exe`)*.

#### Bước 2: Nhập Thông Tin Kết Nối Đến Máy Chủ
Giao diện **CÀI ĐẶT & CẤU HÌNH LAN CONTROL CLIENT AGENT** xuất hiện:

1. **📁 Nơi lưu trữ cài đặt**:
   - **Tự động nhận diện**: Mặc định tự động điền thư mục hiện tại đang chạy tập tin cài đặt (hoặc bạn có thể bấm **`📁 Chọn Thư Mục...`** để thay đổi).
2. **🌐 Địa chỉ IP máy chủ Gateway**:
   - Nhập địa chỉ IP của máy chủ bạn đã lấy ở Phần 1 (Ví dụ: `192.168.1.100`).
3. **🔌 Cổng kết nối (Port)**:
   - Mặc định là **`8443`**.
   - > [!WARNING]
     > Giữ nguyên cổng 8443 để khớp với máy chủ. Nếu quên hoặc xóa nhầm, hãy bấm **`🔄 Đặt lại 8443`**.
4. **🏷️ Phân đoạn VLAN / Tên phòng ban**:
   - Nhấp vào menu để chọn phòng ban tương ứng, ví dụ:
     - `VLAN 10 - Phòng Kỹ Thuật`
     - `VLAN 20 - Phòng Kế Toán`
     - `Phòng Máy Tính 1`
     - Hoặc **gõ trực tiếp bất kỳ tên phòng ban nào** (ví dụ: `Phòng Giám Đốc`, `Lớp 12A1`).
5. **🚀 Tự khởi động cùng Windows**:
   - Giữ dấu tick: `☑ Tự động khởi chạy LAN Control Agent khi máy tính khởi động lại`.
   - Giúp Agent luôn hoạt động kể cả khi máy khởi động lại hoặc người dùng tắt máy bật lại.

#### Bước 3: Bấm Cài Đặt
- Bấm nút lớn màu xanh lá: **`🚀 CÀI ĐẶT & KHỞI CHẠY NGAY`**.
- Hệ thống sẽ:
  - Sao chép tệp `LanControl.Agent.exe` và `LanControl.Core.dll` vào thư mục đã chọn.
  - Lưu thông số IP/Port/VLAN vào tệp `agent_config.json`.
  - Đăng ký vào Windows Registry và thư mục Startup.
  - Khởi chạy ngay lập tức Agent trong khay hệ thống.
- **Dấu hiệu thành công**:
  - Dưới góc phải màn hình (khay hệ thống cạnh đồng hồ Windows) sẽ xuất hiện biểu tượng **chữ `N` màu xanh dương**.
  - Trên màn hình Master Control của máy chủ, máy trạm này sẽ lập tức xuất hiện trong danh sách phòng ban tương ứng!

---

## 5. Phần 3: Hướng Dẫn Gỡ Cài Đặt (Xóa Cấu Hình & Tự Khởi Động)

Khi cần di chuyển máy chủ, đổi phòng ban hoặc tháo gỡ quyền quản lý của hệ thống khỏi máy tính:

### 1. Gỡ cài đặt trên Máy Trạm (Client):
1. Mở lại **`cai_dat_agent.bat`** trên máy trạm đó.
2. Bấm nút màu đỏ: **`🗑️ Gỡ Cài Đặt`**.
3. Bấm **Yes** khi hộp thoại xác nhận xuất hiện.
4. **Kết quả**:
   - Tiến trình Agent đang chạy sẽ dừng ngay lập tức.
   - Xóa tác vụ tự khởi động cùng Windows (Registry & Startup folder).
   - Xóa tệp cấu hình `agent_config.json`.
   - **Tất cả các tệp `.exe` và `.dll` vẫn còn nguyên**, không bị xóa, sẵn sàng để cài đặt lại khi cần.

### 2. Gỡ cài đặt trên Máy Chủ (Server):
1. Mở lại **`cai_dat_server.bat`** trên máy chủ.
2. Bấm nút màu đỏ: **`🗑️ Gỡ Cài Đặt`**.
3. Bấm **Yes** khi hộp thoại hỏi xác nhận.
4. **Kết quả**:
   - Dừng tiến trình Gateway.
   - Xóa bỏ tự khởi động cùng Windows.
   - Xóa luật mở cổng tường lửa Firewall (TCP 8443).
   - Xóa các icon shortcut trên Desktop.
   - Xóa tệp cấu hình `gateway_config.json`.
   - Các tệp chương trình máy chủ vẫn giữ nguyên vẹn trên ổ đĩa.

---

## 6. Phần 4: Mẹo Đóng Gói USB Đi Cài Đặt Hàng Loạt Máy Trạm

Nếu bạn cần đi cài đặt cho 30 - 100 máy trong một phòng thực hành hoặc công ty:

1. **Chuẩn bị 1 chiếc USB** (chỉ cần dung lượng rất nhỏ, chưa tới 10 MB).
2. Copy các tệp sau vào USB:
   - `cai_dat_agent.bat`
   - `bin\LanControl.AgentSetup.exe`
   - `bin\LanControl.Agent.exe`
   - `bin\LanControl.Core.dll`
3. Cắm USB vào từng máy trạm:
   - Nhấp đúp vào **`cai_dat_agent.bat`**.
   - Điền IP máy chủ (ví dụ `192.168.1.100`), chọn phòng ban.
   - Nhấn **`🚀 CÀI ĐẶT & KHỞI CHẠY NGAY`** (mất đúng 2 giây).
   - Rút USB sang máy tiếp theo.

> [!TIP]
> Nếu bạn muốn cấu hình sẵn IP cho toàn bộ các máy trạm trước khi đi cài:
> - Mở tệp `agent_config.json` cạnh file `cai_dat_agent.bat`.
> - Sửa `"GatewayHost": "192.168.1.100"`.
> - Khi cắm vào máy trạm và mở lên, IP máy chủ sẽ tự động điền sẵn `192.168.1.100`, bạn chỉ cần bấm **Cài Đặt** là xong!

---

## 7. Phần 5: Các Câu Hỏi Thường Gặp & Xử Lý Sự Cố Khi Cài Đặt

### ❓ Câu hỏi 1: Tôi mở file `.bat` thì xuất hiện màn hình đen rồi biến mất, không thấy hiện bảng cài đặt?
- **Cách khắc phục**: Nhấp chuột phải vào file `.bat` chọn **Run as administrator** (Chạy với quyền Quản trị viên). Hoặc bạn có thể vào thẳng thư mục `bin\` và nhấp đúp trực tiếp vào `LanControl.ServerSetup.exe` hoặc `LanControl.AgentSetup.exe`.

### ❓ Câu hỏi 2: Máy trạm đã cài đặt thành công nhưng trên máy chủ không thấy xuất hiện máy trạm?
- **Nguyên nhân 1**: Máy chủ Gateway chưa được bật. Hãy kiểm tra trên máy chủ xem Gateway có đang chạy hay không (`cai_dat_server.bat` báo `🟢 Gateway đang chạy`).
- **Nguyên nhân 2**: Nhập sai IP của máy chủ trên máy trạm. Kiểm tra lại xem IP máy chủ có đúng là IP mạng LAN hay không (tránh dùng `127.0.0.1` khi cài đặt giữa 2 máy khác nhau).
- **Nguyên nhân 3**: Tường lửa của máy chủ chưa cho phép cổng 8443. Hãy mở `cai_dat_server.bat` trên máy chủ và đảm bảo đã tích chọn `☑ Tự động mở cổng tường lửa Windows Defender Firewall` rồi bấm Lưu/Cài Đặt lại.

### ❓ Câu hỏi 3: Người dùng ở máy trạm có thể vô tình tắt Agent được không?
- Biểu tượng Agent nằm thu nhỏ ở khay hệ thống (System Tray góc phải dưới). Người dùng thông thường nhấp đúp sẽ chỉ xem được thông tin và nút **"🆘 Gọi Hỗ Trợ Máy Chủ"**. Chỉ có Quản trị viên sử dụng quyền gỡ cài đặt mới có thể tắt dịch vụ hoàn toàn.

### ❓ Câu hỏi 4: Khi khởi động lại máy trạm, Agent có tự chạy không?
- **Có 100%**. Chương trình đã cài đặt cơ chế bảo vệ kép: vừa ghi khóa vào Windows Registry (`HKCU\Run`), vừa tạo file tự chạy trong thư mục Startup (`shell:startup`), đảm bảo ngay khi người dùng đăng nhập vào Windows là Agent tự khởi chạy ngầm ngay.

### ❓ Câu hỏi 5: Máy trạm phân quyền User/Admin, khi mở ứng dụng cần quyền Admin (hiện cửa sổ User Account Control - UAC như Mir4Launcher, bộ cài đặt, Task Manager...) thì có hiện khung UAC nhưng không click chuột hay bấm phím [Yes]/[No] được?

- **Nguyên nhân kỹ thuật**:
  - Hộp thoại UAC của Windows (`consent.exe`) chạy ở mức đặc quyền tối cao **`SYSTEM`** (cao hơn cả quyền Administrator thông thường).
  - Windows kích hoạt cơ chế bảo vệ **UIPI (User Interface Privilege Isolation)**: chặn tuyệt đối mọi lệnh click chuột hoặc bấm phím mô phỏng gửi từ các tiến trình bên ngoài vào cửa sổ `consent.exe`. Do đó, dù UAC đã hiển thị trên màn hình, bạn vẫn không thể click chuột trực tiếp vào nút [Yes]/[No].

- **Cách xử lý triệt để đã được tích hợp trong phiên bản mới**:
  - **Cách 1 - Sử dụng 2 phím tắt 1-click cực nhanh trên thanh công cụ Remote Desktop**:
    - Ngay khi thấy hộp thoại UAC xuất hiện trên màn hình điều khiển:
      - Bấm nút **`[✅ UAC [Yes]]`** (Màu xanh lá): Hệ thống sẽ lập tức gửi tín hiệu phần cứng `Alt + Y` và `Enter` để **tự động bấm [Yes/Đồng ý]** cho bạn mà không cần rê chuột chuẩn xác!
      - Bấm nút **`[❌ UAC [No]]`** (Màu đỏ): Hệ thống sẽ gửi tín hiệu `Escape` và lập tức kết liễu tiến trình UAC để **hủy bỏ và đóng ngay** hộp thoại, giải phóng màn hình máy trạm.
  - **Cách 2 - Bật Tự Động Nâng Quyền & Tắt chặn UIPI từ xa**:
    - Bấm nút **`[🛡️ Nâng Quyền UAC]`** trên thanh công cụ điều khiển.
    - Hệ thống sẽ kích hoạt 3 chính sách Windows mạnh mẽ nhất:
      1. `ConsentPromptBehaviorAdmin = 0`: **Tự Động Nâng Quyền** — Khi mở bất kỳ ứng dụng Admin nào, Windows sẽ tự động chạy luôn dưới quyền Quản trị viên mà KHÔNG hiển thị hộp thoại UAC làm phiền nữa!
      2. `EnableUIPI = 0`: Tắt rào cản UIPI, cho phép chuột và bàn phím từ xa click và gõ trực tiếp vào mọi cửa sổ hệ thống.
      3. `PromptOnSecureDesktop = 0`: Đưa UAC ra màn hình chính điều khiển.
  - **Cách 3 - Tự động thiết lập khi cài đặt bằng `cai_dat_agent.bat`**:
    - Khi bạn nhấp đúp vào `cai_dat_agent.bat` (chạy với quyền Administrator), tệp cài đặt đã được lập trình sẵn để tự động cấu hình cả 4 khóa bảo mật `PromptOnSecureDesktop`, `EnableUIPI`, `ConsentPromptBehaviorAdmin` và đăng ký khởi động cho toàn bộ người dùng (`HKLM\Run` & `All Users Startup`). Bạn chỉ cần bấm **Cài Đặt & Khởi Chạy Ngay** là xong!

---

## 8. Phần 6: Hướng Dẫn Cập Nhật & Nâng Cấp Tính Năng (Update Guide)

Khi mã nguồn chương trình được chỉnh sửa hoặc bổ sung tính năng mới, bạn **KHÔNG BẮT BUỘC** phải cài đặt lại từ đầu. Dưới đây là các phương thức cập nhật:

### 1. Sau khi sửa đổi code tại máy phát triển:
- Nhấp đúp chuột vào tệp **`build.bat`** tại thư mục gốc `D:\lancontrol\`.
- Hệ thống sẽ tự động biên dịch toàn bộ mã nguồn .NET 4.8 và tự động đồng bộ các file `.exe`, `.dll` mới nhất vào cả thư mục `bin\` và thư mục USB **`setup\`**.

### 2. Cập nhật trên các máy trạm / máy chủ đã cài đặt trước đó:
- **Cách 1 - Chạy lại `setup\LanControlSetup.exe` (Khuyên dùng)**: Trình cài đặt tự phát hiện tiến trình cũ, tự dừng app, chép đè file mới và **TỰ ĐỘNG GIỮ NGUYÊN 100% CẤU HÌNH CŨ** (IP, Port, VLAN). Mất chưa tới 5 giây!
- **Cách 2 - Cập nhật 1-Click bằng `setup\cap_nhat_nhanh.bat` (Siêu tốc - 2 giây)**: Nhấp đúp vào file này, script tự động tắt ứng dụng cũ, chép đè file mới vào `C:\Program Files\lancontrol\`, cập nhật khóa Registry UAC và khởi động lại Agent.
- **Cách 3 - Chép đè thủ công**: Thoát Agent ở khay hệ thống (chuột phải icon chữ N -> Thoát), sau đó copy `LanControl.Agent.exe` và `LanControl.Core.dll` dán đè vào `C:\Program Files\lancontrol\`.
- **Cách 4 - Cập nhật từ xa hàng loạt**: Ngồi tại máy quản trị Master Console, dùng tính năng **"📁 Truyền Tệp"** để gửi đè file mới xuống toàn bộ máy trạm trong mạng VLAN mà không cần đi từng máy!

> 📖 **Xem hướng dẫn chi tiết từng bước kèm hình ảnh và bảng so sánh tại tệp:** [HUONG_DAN_UPDATE.md](file:///d:/lancontrol/HUONG_DAN_UPDATE.md)

---
*Tài liệu hướng dẫn cài đặt này được thiết kế đi kèm bộ cài đặt LAN Control Multi-VLAN phát triển tại `D:\lancontrol`.*

