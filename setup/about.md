# LAN Control Multi-VLAN Management & Monitoring Suite

> [!NOTE]
> **LAN Control Manager** - Giải pháp quản trị, điều khiển và giám sát máy trạm tập trung cho mạng doanh nghiệp và y tế đa phân đoạn (Multi-VLAN Subnets).

---

## 📌 Thông Tin Bản Quyền & Tác Giả

- **🏷️ Phiên bản hiện tại**: **v2.2.0** *(Phát hành: 31/10/2026)*
- **👨‍💻 Tác giả**: **Bùi Quang Sáng**
- **🏥 Đơn vị công tác**: **Phòng CNTT - Bệnh viện Đa khoa Khu vực Châu Đốc (BV ĐK Châu Đốc)**
- **📧 Email liên hệ**: [tocnttbkdkkvtinh@gmail.com](mailto:tocnttbkdkkvtinh@gmail.com)
- **⚡ Nền tảng phát triển**: Phần mềm được **vibe coding** dựa trên mã nguồn mở [mRemoteNG](https://github.com/mRemoteNG/mRemoteNG) kết hợp kiến trúc LAN Control Manager.
- **📄 Giấy phép**: Mã nguồn mở phục vụ công tác quản trị mạng, ứng dụng y tế và đào tạo.

---

## 🌟 Giới Thiệu Chung & Định Hướng Phát Triển

Hệ thống được thiết kế đặc thù để giải quyết bài toán quản trị phòng máy trạm, máy tính khoa phòng và phòng thực hành trong môi trường mạng phân tầng phức tạp (Nhiều dải mạng VLAN khác nhau).

Nhờ cơ chế trung chuyển **Gateway Relay Server**, máy chủ quản trị tại bất kỳ VLAN nào cũng có thể kết nối, điều khiển, giám sát toàn diện các máy trạm mà không cần thay đổi cấu hình mạng Core Switch hay mở rộng dải broadcast.

---

## 🚀 Các Tính Năng Nổi Bật Chính

### 1. 🖥️ Điều Khiển Từ Xa (Remote Desktop)
- Truyền hình ảnh thời gian thực, độ trễ cực thấp với thuật toán nén JPEG/RGB tối ưu.
- Thao tác chuột và bàn phím mượt mà.
- **Vượt rào cản UAC**: Hỗ trợ hiển thị và bấm thao tác xác nhận quyền quản trị viên Windows User Account Control (Secure Desktop).

### 2. 📡 Phát Sóng Màn Hình Máy Chủ (Screen Broadcast)
- Giảng viên hoặc Quản trị viên chia sẻ màn hình máy chủ tới hàng loạt máy trạm cùng lúc.
- Tự động thích ứng độ phân giải màn hình máy trạm.

### 3. 📁 Truyền Tập Tin Hàng Loạt (Mass File Distribution)
- Gửi tài liệu, phần mềm cài đặt cùng lúc tới hàng chục máy trạm thuộc nhiều VLAN khác nhau.
- Tùy chọn tự động giải nén, ghi đè và kích hoạt chạy file cài đặt ngay sau khi truyền xong.

### 4. ⚡ Thực Thi Lệnh & PowerShell Script Từ Xa (Remote Scripting)
- Chạy lệnh CMD / PowerShell hàng loạt, nhận kết quả trả về tức thì.
- Tích hợp sẵn mẫu lệnh quản trị hữu ích (kiểm tra tiến trình CPU, kiểm tra IP, dọn dẹp hệ thống...).

### 5. 🔍 Kiểm Kê Cấu Hình Phần Cứng & Bản Quyền Phần Mềm (Audit & Inventory)
- **Hệ thống & Bản quyền Windows**: Phát hiện chi tiết bản quyền Windows bản quyền (Retail/OEM/KMS) hay sử dụng công cụ crack/KMSpico, partial product key.
- **Phần cứng chuyên sâu**:
  - Thông số CPU, xung nhịp, số nhân số luồng.
  - Chi tiết từng khe cắm RAM (Dung lượng, Bus, Serial, Hãng sản xuất).
  - Bo mạch chủ (Mainboard), BIOS version & serial.
  - Toàn bộ ổ đĩa vật lý (Model, Serial, Chuẩn kết nối) & Phân vùng ổ cứng.
  - Danh sách toàn bộ thiết bị ngoại vi kết nối (Máy in, Máy quét/Scan, Máy quét vân tay, Thiết bị USB...).
  - Danh mục toàn bộ phần mềm cài đặt trong máy.

### 6. 🕒 Lịch Sử & Cảnh Báo Biến Động Cấu Hình (Hardware & Software History)
- Tự động chụp snapshot cấu hình mỗi lần kiểm tra.
- Phát hiện và cảnh báo tức thì nếu:
  - Linh kiện bị tháo (RAM bị rút, ổ cứng bị thay đổi).
  - Thiết bị ngoại vi bị rút hoặc cắm mới.
  - Phần mềm mới bị cài đặt lậu hoặc phát hiện công cụ crack mới.
- Hỗ trợ xuất báo cáo biến động chi tiết ra file **Markdown (.md)**.

### 7. 🔒 Kiểm Soát & Bảo Mật Phòng Máy
- Khóa / Mở khóa màn hình máy trạm tức thì.
- Gửi tin nhắn thông báo đẩy xuống máy trạm.
- Khởi động lại hoặc tắt máy từ xa hàng loạt.
- Tùy chọn gỡ cấu hình tự khởi động linh hoạt, không xóa tệp thực thi.

---

## 🛠️ Công Nghệ & Kiến Trúc Xây Dựng

- **Ngôn ngữ**: C# / .NET Framework 4.8.
- **Giao diện**: Windows Forms với phong cách hiện đại Dark Theme & Glassmorphic Accent.
- **Mã nguồn tham khảo & cảm hứng**: [https://github.com/mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG).
- **Kiến trúc mạng**: Socket TCP / Relay Protocol đa VLAN với mã hóa dữ liệu.

---

## 🕒 Lịch Sử Phiên Bản & Thay Đổi Chức Năng (Changelog)

### 🚀 Phiên bản v2.2.0 *(31/10/2026 - Hiện tại)*
- **Quản lý Phiên bản & Lịch sử thay đổi**: Hiển thị đồng bộ số hiệu phiên bản `v2.2.0` trên toàn bộ cửa sổ, thanh trạng thái, khay hệ thống và tích hợp trình xem/chỉnh sửa `CHANGELOG.md` trực tiếp trong phần mềm.
- **Sửa lỗi Nhắn tin**: Khắc phục lỗi hộp thoại báo `Thành công` bị cửa sổ `Gửi Thông Báo / Tin Nhắn Tới Máy Trạm` che khuất gây kẹt thao tác.
- **Sửa lỗi Menu Khay hệ thống (System Tray) & Điều khiển từ xa**: Chuyển sang `ContextMenuStrip`, sửa lỗi click chọn chức năng từ xa trên biểu tượng khay hệ thống và đưa các hộp thoại chức năng lên lớp trên cùng (`TopMost`).

### 🌟 Phiên bản v2.1.0 *(29/10/2026)*
- Đồng bộ thương hiệu **LAN Control Suite** và phát hành trình cài đặt hợp nhất 1-Click (`LanControlSetup.exe`) hỗ trợ 3 chế độ (Agent / Server / Full).
- Bổ sung tính năng **Hiện UAC & Nhập mật khẩu Admin từ xa** trực tiếp trên thanh công cụ Remote Desktop.

### 🔍 Phiên bản v2.0.0 *(20/09/2026)*
- Kiểm kê chuyên sâu phần cứng (CPU, RAM từng thanh, Ổ cứng, Thiết bị ngoại vi) và bản quyền Windows/Office.
- Cảnh báo biến động cấu hình phần cứng/phần mềm và xuất báo cáo Markdown (`.md`).
- Truyền tệp hàng loạt đa VLAN, kéo thả tệp trực tiếp và tính năng máy trạm **Gọi Hỗ Trợ Kỹ Thuật**.

### 🎯 Phiên bản v1.0.0 *(19/09/2026)*
- Phát hành phiên bản đầu tiên: Kết nối ngược đa VLAN qua Central Gateway, Lưới màn hình Thumbnail Matrix, Remote Desktop, Phát sóng màn hình, Khóa màn hình, Chạy lệnh CMD/PowerShell và Quét dải IP.
