# 🕒 LỊCH SỬ PHIÊN BẢN & THAY ĐỔI CHỨC NĂNG (CHANGELOG)

> [!NOTE]
> Tài liệu này ghi nhận toàn bộ lịch sử nâng cấp, bổ sung tính năng và sửa lỗi của hệ thống **LAN Control Multi-VLAN Management Suite** theo từng phiên bản để thuận tiện theo dõi và quản trị.

---

## 🚀 Phiên bản v2.2.0 — *Ngày phát hành: 31/10/2026 (Bản hiện tại)*

### ✨ Tính năng mới
- **Quản lý Phiên bản (Versioning) & Lịch sử thay đổi (Changelog)**:
  - Hiển thị đồng bộ số hiệu phiên bản `v2.2.0` trên thanh tiêu đề, thanh trạng thái (Status Bar), khay hệ thống (System Tray) của cả 4 phân hệ: `Master Control`, `Central Gateway`, `Client Agent`, và `Setup Wizard`.
  - Bổ sung tab **🕒 Lịch Sử Phiên Bản (Changelog)** ngay trong cửa sổ **Giới Thiệu (About)** của Master Control.
  - Hỗ trợ nút mở và chỉnh sửa trực tiếp tệp `CHANGELOG.md` để quản trị viên dễ dàng ghi chú, cập nhật lịch sử nâng cấp về sau.

### 🐛 Sửa lỗi & Cải tiến
- **Tính năng Nhắn tin (`SendMessageForm` & `CommandHandler`)**:
  - Khắc phục triệt để lỗi hộp thoại báo `Thành công` bị cửa sổ `Gửi Thông Báo / Tin Nhắn Tới Máy Trạm` che khuất làm khóa thao tác và phải nhấn `Alt + Tab`.
  - Đóng cửa sổ nhập tin nhắn ngay khi gửi và gắn quyền ưu tiên hiển thị (`TopMost` / `Owner`) cho cả thông báo xác nhận lẫn hộp thoại tin nhắn trên màn hình máy trạm.
- **Biểu tượng Khay hệ thống (System Tray / Status Bar Icon) & Điều khiển Chuột từ xa (`InputSimulator`)**:
  - Chuyển đổi menu chuột phải của `Agent` và `Gateway` sang `ContextMenuStrip` hiện đại, xử lý bất đồng bộ trên luồng giao diện chính và luôn đưa hộp thoại chức năng lên lớp trên cùng (`TopMost`).
  - Loại bỏ xung đột cập nhật Tooltip liên tục khi di chuột (`MouseMove`) trên khay hệ thống.
  - Sửa lỗi trong `InputSimulator` tự động gọi `SetForegroundWindow` trước sự kiện click chuột trái làm hủy menu chuột phải khi điều khiển qua `Remote Desktop`.

---

## 🌟 Phiên bản v2.1.0 — *Ngày phát hành: 29/10/2026*

### ✨ Tính năng mới
- **Đồng bộ Thương hiệu & Kiến trúc `LAN Control`**:
  - Chuẩn hóa toàn bộ tên gọi giải pháp, không gian tên (`LanControl.*`), tệp thực thi (`LanControl.Control.exe`, `LanControl.Gateway.exe`, `LanControl.Agent.exe`) và biểu tượng nhận diện.
- **Trình Cài Đặt Hợp Nhất 1-Click (`LanControlSetup.exe`)**:
  - Thiết kế trình cài đặt giao diện đồ họa tích hợp 3 chế độ trong 1 tệp duy nhất: **Máy Trạm (Agent)**, **Máy Chủ (Gateway + Control)**, và **Trọn Bộ (Full Suite)**.
  - Tự động hóa toàn bộ quy trình: chép tệp vào `C:\Program Files\lancontrol`, tạo cấu hình JSON, mở cổng tường lửa Windows Firewall, đăng ký tự khởi động đa tầng và tạo lối tắt Desktop.
- **Điều khiển xuyên qua UAC & Gửi Mật khẩu Admin (`RemoteDesktopForm`)**:
  - Thêm nút **🛡️ Hiện UAC & MK**: Tự động đưa hộp thoại UAC từ Secure Desktop về màn hình thường (`PromptOnSecureDesktop = 0`) và mở khóa tương tác chuột phím (`EnableUIPI = 0`).
  - Thêm nút **🔑 Nhập MK Admin**, **✅ Đồng Ý UAC**, **❌ Hủy UAC** hỗ trợ truyền mật khẩu Administrator bằng mã phím Unicode chính xác.

---

## 🔍 Phiên bản v2.0.0 — *Ngày phát hành: 20/09/2026*

### ✨ Tính năng mới
- **Kiểm Kê Cấu Hình Phần Cứng & Bản Quyền Chuyên Sâu (`HardwareInventoryForm`)**:
  - Thu thập chi tiết CPU, Mainboard, BIOS, từng thanh RAM vật lý (Dung lượng, Bus, Hãng, Serial), ổ cứng vật lý & phân vùng.
  - Liệt kê thiết bị ngoại vi (Máy in, Máy Scan, USB, Đầu đọc thẻ/vân tay) và danh sách phần mềm đã cài đặt.
  - Kiểm tra trạng thái bản quyền Windows & Office (phát hiện bản quyền chuẩn Retail/OEM/Volume hay công cụ kích hoạt KMS/Crack).
- **Lịch Sử & Cảnh Báo Biến Động Cấu Hình (`audit_history`)**:
  - Tự động lưu bản chụp (Snapshot) cấu hình máy trạm và đối chiếu phát hiện thay đổi phần cứng (tháo RAM, đổi ổ cứng, rút thiết bị ngoại vi) hoặc phần mềm mới cài đặt.
  - Hỗ trợ xuất báo cáo kiểm kê và lịch sử biến động ra định dạng **Markdown (`.md`)** và **CSV**.
- **Truyền Tập Tin Hàng Loạt & Quản Lý Tệp 2 Chiều (`FileDistributionForm` & `RemoteFileManagerForm`)**:
  - Hỗ trợ kéo-thả tệp trực tiếp vào cửa sổ Remote Desktop để gửi nhanh ra Desktop máy trạm.
  - Duyệt cây thư mục máy trạm từ xa, tải lên/tải xuống, xóa hoặc chạy tệp thực thi từ xa.
- **Tính năng Gọi Hỗ Trợ Kỹ Thuật (`HelpRequest`)**:
  - Cho phép người dùng máy trạm gửi yêu cầu hỗ trợ từ khay hệ thống tới bàn điều khiển Quản trị viên; Quản trị viên bấm chấp nhận sẽ mở ngay phiên điều khiển từ xa.

---

## 🎯 Phiên bản v1.0.0 — *Ngày phát hành: 19/09/2026*

### ✨ Tính năng cốt lõi ban đầu
- **Kiến trúc Kết Nối Ngược Đa VLAN (`Reverse TCP Connection`)**:
  - Máy trạm `Agent` chủ động kết nối ngược về `Central Gateway` (mặc định cổng `8443`), vượt qua mọi rào cản định tuyến khác lớp mạng VLAN và tường lửa máy trạm.
- **Lưới Màn Hình Thời Gian Thực (`Thumbnail Matrix`) & Danh Sách Thiết Bị**:
  - Giám sát trực quan toàn bộ máy trạm theo phân đoạn VLAN, theo dõi % CPU, RAM khả dụng, địa chỉ IP/MAC và người dùng đang đăng nhập.
- **Điều Khiển Từ Xa (`Remote Desktop`) & Phát Sóng Màn Hình (`Screen Broadcast`)**:
  - Điều khiển chuột phím thời gian thực (Co-browsing không khóa màn hình người dùng) và phát màn hình máy chủ tới nhiều máy khách.
- **Công Cụ Quản Trị Phòng Máy**:
  - Khóa/Mở khóa màn hình máy trạm, Gửi thông báo toàn màn hình, Thực thi lệnh CMD/PowerShell hàng loạt, Quản lý tiến trình (`Process Manager`), Quét dải IP đa luồng (`Subnet Scanner`), Tắt/Khởi động lại máy từ xa.
