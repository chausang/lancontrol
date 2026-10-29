# HƯỚNG DẪN CẬP NHẬT & NÂNG CẤP TÍNH NĂNG (UPDATE GUIDE)
### HỆ THỐNG QUẢN LÝ MÁY TÍNH LAN CONTROL MULTI-VLAN

---

## 📌 CÂU HỎI THƯỜNG GẶP: CÓ PHẢI CHẠY LẠI FILE CÀI ĐẶT TỪ ĐẦU KHÔNG?

> **TRẢ LỜI NGẮN GỌN:**
> 
> **KHÔNG BẮT BUỘC phải cài đặt lại từ đầu!** 
> 
> Bạn có thể chọn 1 trong 4 cách cập nhật cực kỳ nhanh chóng:
> 1. **Cách 1 (Khuyên dùng - Đơn giản nhất)**: Chạy lại file cài đặt `setup\LanControlSetup.exe` (hoặc `cai_dat.bat`). Trình cài đặt sẽ tự động tắt app cũ, chép đè file mới và **TỰ ĐỘNG GIỮ NGUYÊN 100% CẤU HÌNH CŨ** (IP Gateway, Port, Tên phân đoạn VLAN). Mất chưa tới 5 giây!
> 2. **Cách 2 (Siêu tốc - 2 giây)**: Nhấp đúp vào file `setup\cap_nhat_nhanh.bat` để tự động chép đè và khởi động lại dịch vụ tức thì.
> 3. **Cách 3 (Thủ công)**: Thoát ứng dụng dưới khay hệ thống, chép đè file `.exe` và `LanControl.Core.dll` mới vào thư mục `C:\Program Files\lancontrol\`.
> 4. **Cách 4 (Cập nhật từ xa hàng loạt)**: Ngồi tại máy giáo viên/quản trị, dùng tính năng **"📁 Truyền Tệp"** trên Master Console để đẩy file mới xuống toàn bộ máy học sinh cùng lúc mà **không cần đi đến từng máy**!

---

## 🔄 QUY TRÌNH CẬP NHẬT TỔNG THỂ (2 GIAI ĐOẠN)

```text
 ┌─────────────────────────────────────────────────────────────┐
 │ GIAI ĐOẠN 1: MÁY LẬP TRÌNH / ADMIN (KHI SỬA CODE/TÍNH NĂNG) │
 └──────────────────────────────┬──────────────────────────────┘
                                │ Chạy build.bat
                                ▼
 ┌─────────────────────────────────────────────────────────────┐
 │       HỆ THỐNG TỰ BIÊN DỊCH VÀ ĐỒNG BỘ VÀO GÓI setup\       │
 │   - setup\LanControl.Agent.exe    - setup\LanControl.Core.dll│
 │   - setup\LanControl.Control.exe  - setup\LanControlSetup.exe│
 └──────────────────────────────┬──────────────────────────────┘
                                │ Copy thư mục setup\ sang USB
                                ▼
 ┌─────────────────────────────────────────────────────────────┐
 │  GIAI ĐOẠN 2: CẬP NHẬT TRÊN CÁC MÁY ĐÃ CÀI ĐẶT TRƯỚC ĐÓ    │
 ├──────────────────────────────┼──────────────────────────────┤
 │ Cách 1: Chạy LAN ControlSetup │ Tự động giữ nguyên IP/VLAN   │
 │ Cách 2: Chạy cap_nhat_nhanh  │ Cập nhật 1-Click trong 2 giây│
 │ Cách 3: Chép đè thủ công     │ Dán đè vào C:\Program Files  │
 │ Cách 4: Đẩy file từ xa       │ Master Console gửi qua mạng  │
 └──────────────────────────────┴──────────────────────────────┘
```

---

## 🛠️ GIAI ĐOẠN 1: KHI CÓ CHỈNH SỬA MÃ NGUỒN HOẶC TÍNH NĂNG

Mỗi khi bạn sửa đổi mã nguồn (trong thư mục `src\`), hãy thực hiện các bước sau để cập nhật bộ cài và các tệp nhị phân:

1. Mở thư mục gốc dự án: `D:\lancontrol\`
2. Nhấp đúp chuột vào tệp: **`build.bat`**
3. Script sẽ tự động:
   - Biên dịch lại mã nguồn C# bằng trình biên dịch .NET 4.8 Framework.
   - Tạo ra các tệp mới nhất trong thư mục `bin\`.
   - **Tự động sao chép đồng bộ** toàn bộ các file mới vào thư mục triển khai **`setup\`**.
4. Lúc này thư mục `setup\` đã chứa trọn vẹn phiên bản mới nhất, sẵn sàng copy vào USB hoặc chia sẻ qua mạng nội bộ.

---

## 💻 GIAI ĐOẠN 2: CÁC CÁCH CẬP NHẬT TRÊN MÁY TÍNH ĐÃ CÀI ĐẶT

### CÁCH 1: Chạy Trình Cài Đặt `setup\LanControlSetup.exe` (Khuyên Dùng)

Trình cài đặt Wizard đã được lập trình sẵn cơ chế nhận diện nâng cấp thông minh:
- **Tự giải phóng tệp khóa**: Nếu `LanControl.Agent.exe` hoặc `LanControl.Gateway.exe` đang chạy ngầm, trình cài đặt sẽ tự động dừng tiến trình cũ để chép đè tệp mới mà không báo lỗi `Access Denied`.
- **Bảo lưu cấu hình**: Tệp cấu hình `agent_config.json` và `gateway_config.json` luôn được bảo vệ nguyên vẹn các thông số IP, Cổng kết nối và nhãn VLAN đã cài trước đó.
- **Cập nhật chính sách hệ thống**: Tự động kích hoạt lại các khóa Registry tối ưu cho quyền Admin và UAC.

**Các bước thực hiện:**
1. Cắm USB chứa thư mục `setup\` vào máy tính cần cập nhật.
2. Mở thư mục `setup\` -> Chạy **`LanControlSetup.exe`** *(hoặc `cai_dat.bat`)*.
3. Chọn phiên bản tương ứng -> Bấm **`Tiếp tục`** -> Bấm **`🚀 Cài Đặt Ngay`**.
4. Quá trình cập nhật hoàn thành sau 3-5 giây!

---

### CÁCH 2: Cập Nhật Siêu Tốc 1-Click Với `setup\cap_nhat_nhanh.bat` (2 Giây)

Nếu bạn không muốn phải bấm qua các bước của giao diện Wizard, hãy dùng tệp lệnh cập nhật nhanh:

1. Mở thư mục `setup\` trên USB hoặc máy tính.
2. Nhấp đúp chuột vào: **`cap_nhat_nhanh.bat`**
3. Script tự động thực hiện:
   - Tự yêu cầu quyền Administrator (`RunAs`).
   - Tắt ứng dụng cũ đang chạy ngầm.
   - Sao chép đè `LanControl.Agent.exe`, `LanControl.Core.dll` mới vào `C:\Program Files\lancontrol\`.
   - Cập nhật chính sách mở khóa UAC và rào cản UIPI.
   - Tự động bật lại Agent phiên bản mới dưới khay hệ thống.
4. Xong ngay lập tức mà không cần bất kỳ thao tác bấm chuột nào khác!

---

### CÁCH 3: Chép Đè Thủ Công Bằng Tay (Manual Hot-Fix)

Cách này phù hợp khi bạn chỉ muốn cập nhật riêng 1 tệp cụ thể (ví dụ chỉ sửa file DLL hoặc file Agent):

1. **Thoát chương trình đang chạy**:
   - Với máy trạm (Agent): Click chuột phải vào biểu tượng chữ **N** màu xanh dưới khay hệ thống (góc phải màn hình bên cạnh đồng hồ) -> Chọn **`Thoát Agent`**.
   - Với máy chủ (Gateway/Console): Tắt các cửa sổ Console hoặc dừng tiến trình Gateway.
2. **Sao chép tệp mới**:
   - Vào thư mục `setup\` hoặc `bin\`.
   - Copy tệp bạn vừa cập nhật (ví dụ: `LanControl.Agent.exe` và `LanControl.Core.dll`).
   - Dán đè (Paste & Replace) vào thư mục cài đặt:
     ```text
     C:\Program Files\lancontrol\
     ```
3. **Mở lại chương trình**:
   - Nhấp đúp vào biểu tượng **LAN Control Agent** ngoài Desktop để chạy lại.
   - Cấu hình kết nối IP/VLAN trong `agent_config.json` vẫn giữ nguyên 100%.

---

### CÁCH 4: Cập Nhật Từ Xa Hàng Loạt Qua Master Console (Không Cần Đến Từng Máy)

Khi hệ thống có từ 20 đến hàng trăm máy trạm phòng ban, bạn không cần phải cắm USB đi từng máy! Hãy ngồi tại máy Giáo viên / IT để cập nhật từ xa:

1. Mở **LAN Control Master Console** (`LanControl.Control.exe`).
2. Chọn danh sách các máy trạm cần cập nhật (hoặc chọn cả phòng ban / VLAN).
3. Bấm nút **`📁 Truyền Tệp ▾`** trên thanh công cụ -> Chọn **`Truyền Tập Tin Nhanh (Desktop / Downloads / Tự Chạy)...`**.
4. Chọn tệp nguồn: `setup\LanControl.Agent.exe` và `setup\LanControl.Core.dll`.
5. Đặt thư mục đích: `C:\Program Files\lancontrol\`.
6. Bấm **`🚀 Bắt Đầu Phân Phối Hàng Loạt`**:
   - Hệ thống sẽ truyền song song tệp cập nhật xuống tất cả các máy trạm cùng lúc qua mạng LAN đa VLAN.
7. Khi cần khởi động lại Agent từ xa:
   - Dùng tính năng Chạy Lệnh Từ Xa (Remote Script/Command) gửi lệnh:
     ```cmd
     taskkill /f /im LanControl.Agent.exe & start "" "C:\Program Files\lancontrol\LanControl.Agent.exe"
     ```
   - Toàn bộ máy trạm sẽ tự động nạp phiên bản mới nhất ngay lập tức!

---

## 📊 BẢNG SO SÁNH CÁC PHƯƠNG PHÁP CẬP NHẬT

| Tiêu Chí | Cách 1: LanControlSetup.exe | Cách 2: cap_nhat_nhanh.bat | Cách 3: Chép Đè Bằng Tay | Cách 4: Cập Nhật Từ Xa |
| :--- | :---: | :---: | :---: | :---: |
| **Thời gian thực hiện** | ~5 giây | **~2 giây** | ~10 giây | **Đồng thời tất cả máy** |
| **Bảo lưu cấu hình IP/VLAN** | ✅ Có (100%) | ✅ Có (100%) | ✅ Có (100%) | ✅ Có (100%) |
| **Tự giải phóng tiến trình cũ** | ✅ Tự động | ✅ Tự động | ⚠️ Cần thoát tay | ✅ Gửi lệnh từ xa |
| **Cập nhật chính sách UAC** | ✅ Tự động | ✅ Tự động | ❌ Không | ✅ Hỗ trợ qua script |
| **Phù hợp cho** | Cập nhật định kỳ | Cập nhật nhanh qua USB | Cập nhật thử nghiệm | **Phòng máy số lượng lớn** |

---

## ❓ CÁC CÂU HỎI THƯỜNG GẶP KHI UPDATE (FAQ)

### Q1: Cập nhật xong có cần khởi động lại máy tính (Reboot) không?
> **Trả lời**: **KHÔNG CẦN**. LAN Control hoạt động ở tầng ứng dụng và giao thức mạng độc lập. Chỉ cần tắt phiên bản cũ và bật phiên bản mới là toàn bộ tính năng mới đã có hiệu lực ngay lập tức.

### Q2: Tệp cấu hình nằm ở đâu? Làm sao để sao lưu phòng khi cần?
> **Trả lời**: Tệp cấu hình nằm tại `C:\Program Files\lancontrol\agent_config.json` (phía máy trạm) và `gateway_config.json` (phía máy chủ). Bạn có thể mở tệp này bằng Notepad để xem hoặc sao lưu. Các phương pháp cập nhật trên đều không bao giờ làm mất nội dung tệp cấu hình này.

### Q3: Nếu muốn đổi phân đoạn VLAN hoặc IP Gateway trong lúc update thì làm thế nào?
> **Trả lời**: Hãy chọn **Cách 1** (chạy `LanControlSetup.exe`). Ở Bước 3, bạn chỉ cần sửa lại địa chỉ IP hoặc chọn VLAN mới trong danh sách, sau đó bấm `🚀 Cài Đặt Ngay`.

### Q4: Máy trạm đang bị khóa màn hình hoặc đang được điều khiển thì có update được không?
> **Trả lời**: Có thể update bình thường. Trình cập nhật sẽ tự đóng phiên điều khiển và khởi động lại Agent mới chỉ trong 1-2 giây.
