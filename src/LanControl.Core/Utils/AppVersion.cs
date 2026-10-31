using System;
using System.IO;
using System.Reflection;
using System.Text;

[assembly: AssemblyTitle("LAN Control Multi-VLAN Management & Monitoring Suite")]
[assembly: AssemblyDescription("Giải pháp quản trị, điều khiển và giám sát máy trạm đa phân đoạn VLAN")]
[assembly: AssemblyCompany("Phòng CNTT - BV ĐK Châu Đốc")]
[assembly: AssemblyProduct("LAN Control Manager")]
[assembly: AssemblyCopyright("Copyright © 2026 Bùi Quang Sáng")]
[assembly: AssemblyVersion("2.2.0.0")]
[assembly: AssemblyFileVersion("2.2.0.0")]

namespace LanControl.Core.Utils
{
    public static class AppVersion
    {
        public const string Version = "2.2.0";
        public const string FullVersion = "v2.2.0";
        public const string ReleaseDate = "31/10/2026";
        public const string Edition = "Multi-VLAN Enterprise & Healthcare Edition";
        public const string Author = "Bùi Quang Sáng";
        public const string Organization = "Phòng CNTT - BV ĐK Châu Đốc";

        public static string TitleWithVersion
        {
            get { return string.Format("LAN Control Suite {0} ({1})", FullVersion, ReleaseDate); }
        }

        public static string ResolveChangelogFilePath()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] candidates = new string[]
            {
                Path.Combine(baseDir, "CHANGELOG.md"),
                Path.Combine(baseDir, "..", "CHANGELOG.md"),
                Path.Combine(baseDir, "..", "..", "CHANGELOG.md"),
                Path.Combine(Environment.CurrentDirectory, "CHANGELOG.md"),
                @"D:\Antigravity\LanControl\CHANGELOG.md",
                @"D:\lancontrol\CHANGELOG.md"
            };

            foreach (string candidate in candidates)
            {
                try
                {
                    if (File.Exists(candidate))
                    {
                        return Path.GetFullPath(candidate);
                    }
                }
                catch { }
            }

            return Path.Combine(baseDir, "CHANGELOG.md");
        }

        public static string GetChangelogMarkdown()
        {
            try
            {
                string path = ResolveChangelogFilePath();
                if (File.Exists(path))
                {
                    return File.ReadAllText(path, Encoding.UTF8);
                }
            }
            catch { }

            return DefaultChangelogMarkdown;
        }

        public static string GetShortChangelogSummary()
        {
            return
                "📌 Phiên bản hiện tại: " + FullVersion + " (Phát hành: " + ReleaseDate + ")\n\n" +
                "🔹 Điểm mới trong " + FullVersion + " (31/10/2026):\n" +
                "  • Cửa sổ nổi kiểm soát phát sóng (PiP): Tự thu gọn nổi trên cùng, kéo di chuyển và thay đổi kích thước tự do khi đang phát sóng.\n" +
                "  • Phát sóng Màn hình nâng cao: Cho phép chọn phát sóng 1 chương trình cụ thể đang mở hoặc phát sóng Toàn màn hình.\n" +
                "  • Tích hợp hệ thống Quản lý Phiên bản (Version) & Lịch sử thay đổi (CHANGELOG.md).\n" +
                "  • Khắc phục lỗi hộp thoại 'Thành công' bị che khuất sau khi gửi tin nhắn tới máy trạm.\n" +
                "  • Nâng cấp Menu chuột phải tại biểu tượng khay hệ thống (System Tray) và sửa lỗi chọn chức năng từ xa.\n\n" +
                "🔹 Phiên bản v2.1.0 (29/10/2026):\n" +
                "  • Chuyển đổi nhận diện thương hiệu đồng bộ sang LAN Control Suite.\n" +
                "  • Trình cài đặt hợp nhất 1-Click (LanControlSetup.exe) hỗ trợ 3 chế độ: Agent / Server / Full.\n" +
                "  • Hỗ trợ hiển thị hộp thoại UAC & gửi mật khẩu Admin trực tiếp từ xa.\n\n" +
                "🔹 Phiên bản v2.0.0 (20/09/2026):\n" +
                "  • Kiểm kê chuyên sâu Phần cứng, RAM từng khe, Thiết bị ngoại vi & Bản quyền Windows/Office.\n" +
                "  • Cảnh báo biến động phần cứng (tháo RAM, đổi ổ cứng) & xuất báo cáo Markdown (.md).\n" +
                "  • Truyền tệp hàng loạt đa VLAN (kéo thả trực tiếp vào màn hình điều khiển).\n\n" +
                "🔹 Phiên bản v1.0.0 (19/09/2026):\n" +
                "  • Kiến trúc Reverse Connection qua Central Gateway vượt rào cản đa VLAN.\n" +
                "  • Lưới màn hình Thumbnail Matrix, Remote Desktop, Phát sóng màn hình, Khóa máy, Chạy lệnh CMD/PS.";
        }

        public const string DefaultChangelogMarkdown = @"# 🕒 LỊCH SỬ PHIÊN BẢN & THAY ĐỔI CHỨC NĂNG (CHANGELOG)

> [!NOTE]
> Tài liệu này ghi nhận toàn bộ lịch sử nâng cấp, bổ sung tính năng và sửa lỗi của hệ thống **LAN Control Multi-VLAN Management Suite** theo từng phiên bản để thuận tiện theo dõi và quản trị.

---

## 🚀 Phiên bản v2.2.0 — *Ngày phát hành: 31/10/2026 (Bản hiện tại)*

### ✨ Tính năng mới
- **Cửa Sổ Nổi Kiểm Soát Nội Dung Phát Sóng (Floating Broadcast Monitor / Picture-in-Picture)**:
  - Khi phát sóng màn hình hoặc chương trình, hệ thống tự động thu gọn thành **Cửa sổ nổi (PiP)** luôn hiển thị trên cùng (`TopMost`) ở góc màn hình để người phát vừa thao tác trên các phần mềm khác vừa quan sát trực tiếp nội dung đang truyền tới máy trạm.
  - Hỗ trợ **điều chỉnh kích thước tự do** (kéo viền/góc cửa sổ, lăn con lăn chuột trên hình xem trước, hoặc chọn nhanh cỡ Nhỏ/Vừa/Lớn trong menu chuột phải) và **kéo di chuyển cửa sổ tới bất kỳ vị trí nào** ngay trên khung hình xem trước.
  - Tích hợp công nghệ `SetWindowDisplayAffinity (WDA_EXCLUDEFROMCAPTURE)` tự động ẩn cửa sổ nổi khỏi luồng chụp màn hình khi phát Toàn màn hình, tránh hiện tượng lặp hình trên máy trạm.
  - **Chặn người dùng máy trạm tắt cửa sổ trình chiếu (`BroadcastViewerForm` & `AgentClient`)**: Vô hiệu hóa nút đóng `[X]`, chặn phím tắt `Alt + F4`, `Esc`, chặn thu nhỏ (`Minimize` / `Win + D`), chặn thoát `Agent` từ khay hệ thống khi đang phát sóng và tự động khôi phục ngay lập tức nếu cửa sổ trình chiếu bị tác động đóng.
- **Phát sóng 1 Chương trình cụ thể hoặc Toàn màn hình (`BroadcastSenderForm` & `ScreenCapture`)**:
  - Bổ sung tùy chọn **💻 Chọn 1 chương trình** bên cạnh chế độ **🖥️ Toàn màn hình** trong công cụ Phát sóng màn hình (`Broadcast`).
  - Tự động liệt kê toàn bộ các ứng dụng/cửa sổ đang mở trên máy quản trị viên (PowerPoint, Word, Chrome, trình phát video...), cho phép chỉ phát riêng nội dung cửa sổ chương trình đã chọn tới các máy trạm (kể cả khi có cửa sổ khác đè lên nhờ công nghệ `PrintWindow` + `PW_RENDERFULLCONTENT`).
  - Hỗ trợ chuyển đổi trực tiếp (Live Switch) giữa phát Toàn màn hình và phát từng Chương trình ngay trong khi đang phát sóng.
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
  - Thiết kế trình cài đặt giao diện đồ họa 통합 3 chế độ trong 1 tệp duy nhất: **Máy Trạm (Agent)**, **Máy Chủ (Gateway + Control)**, và **Trọn Bộ (Full Suite)**.
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
";
    }
}

