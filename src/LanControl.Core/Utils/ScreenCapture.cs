using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace LanControl.Core.Utils
{
    public class WindowCaptureItem
    {
        public IntPtr Handle { get; set; }
        public int ProcessId { get; set; }
        public string ProcessName { get; set; }
        public string Title { get; set; }

        public override string ToString()
        {
            return string.Format("🪟 [{0}] {1}", ProcessName, Title);
        }
    }

    public static class ScreenCapture
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CURSORINFO
        {
            public int cbSize;
            public int flags;
            public IntPtr hCursor;
            public POINT ptScreenPos;
        }

        private const int CURSOR_SHOWING = 0x00000001;
        private const uint PW_RENDERFULLCONTENT = 0x00000002;
        private const int SW_RESTORE = 9;
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_APPWINDOW = 0x00040000;
        private const int DWMWA_CLOAKED = 14;

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool GetCursorInfo(out CURSORINFO pci);

        [DllImport("user32.dll")]
        private static extern bool DrawIcon(IntPtr hDC, int X, int Y, IntPtr hIcon);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint nFlags);

        [DllImport("dwmapi.dll")]
        private static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out int pvAttribute, int cbAttribute);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr OpenInputDesktop(uint dwFlags, bool fInherit, uint dwDesiredAccess);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetThreadDesktop(IntPtr hDesktop);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool CloseDesktop(IntPtr hDesktop);

        private const uint DESKTOP_ALL_ACCESS = 0x01FF;

        public static void AttachToActiveInputDesktop()
        {
            try
            {
                IntPtr hDesktop = OpenInputDesktop(0, false, DESKTOP_ALL_ACCESS);
                if (hDesktop != IntPtr.Zero)
                {
                    SetThreadDesktop(hDesktop);
                }
            }
            catch { }
        }

        private static ImageCodecInfo _jpegEncoder;

        static ScreenCapture()
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == ImageFormat.Jpeg.Guid)
                {
                    _jpegEncoder = codec;
                    break;
                }
            }
        }

        public static byte[] CaptureDesktopJpeg(long quality = 60, bool drawCursor = true, int targetWidth = 0, int targetHeight = 0)
        {
            AttachToActiveInputDesktop();

            Rectangle bounds = Screen.PrimaryScreen.Bounds;
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                bounds = new Rectangle(0, 0, 1920, 1080);
            }

            using (Bitmap bmp = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format24bppRgb))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    bool copySuccess = false;
                    try
                    {
                        g.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
                        copySuccess = true;
                    }
                    catch
                    {
                        try
                        {
                            AttachToActiveInputDesktop();
                            g.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
                            copySuccess = true;
                        }
                        catch
                        {
                            g.Clear(Color.FromArgb(20, 24, 32));
                            using (Font fTitle = new Font("Segoe UI", 15, FontStyle.Bold))
                            using (Font fSub = new Font("Segoe UI", 11, FontStyle.Regular))
                            using (Font fBtn = new Font("Segoe UI", 11, FontStyle.Bold))
                            using (SolidBrush bGold = new SolidBrush(Color.FromArgb(243, 156, 18)))
                            using (SolidBrush bCyan = new SolidBrush(Color.FromArgb(26, 188, 156)))
                            using (SolidBrush bWhite = new SolidBrush(Color.White))
                            {
                                float midX = bounds.Width / 2f;
                                float midY = bounds.Height / 2f;
                                g.DrawString("🛡️ MÁY TRẠM ĐANG HIỂN THỊ HỘP THOẠI XÁC NHẬN ADMIN / UAC TRÊN BÀN LÀM VIỆC BẢO MẬT", fTitle, bGold, new PointF(Math.Max(20, midX - 450), midY - 70));
                                g.DrawString("Khung nhập mật khẩu Admin đang bị Windows ẩn vào màn hình bảo mật riêng biệt (Secure Desktop).", fSub, bWhite, new PointF(Math.Max(20, midX - 370), midY - 30));
                                g.DrawString("👉 Bấm nút [🛡️ Hiện UAC & MK] trên thanh công cụ để đưa khung mật khẩu về màn hình điều khiển!", fBtn, bCyan, new PointF(Math.Max(20, midX - 420), midY + 10));
                                g.DrawString("👉 Hoặc bấm nút [🔑 Nhập MK Admin] để gửi mật khẩu quản trị trực tiếp vào máy trạm.", fSub, bGold, new PointF(Math.Max(20, midX - 350), midY + 45));
                            }
                        }
                    }

                    if (copySuccess && drawCursor)
                    {
                        try
                        {
                            CURSORINFO pci;
                            pci.cbSize = Marshal.SizeOf(typeof(CURSORINFO));
                            if (GetCursorInfo(out pci) && pci.flags == CURSOR_SHOWING)
                            {
                                IntPtr hdc = g.GetHdc();
                                try
                                {
                                    DrawIcon(hdc, pci.ptScreenPos.x - bounds.X, pci.ptScreenPos.y - bounds.Y, pci.hCursor);
                                }
                                finally
                                {
                                    g.ReleaseHdc(hdc);
                                }
                            }
                        }
                        catch { }
                    }
                }


                // If resizing requested (e.g. for thumbnail matrix)
                if (targetWidth > 0 && targetHeight > 0 && (targetWidth != bounds.Width || targetHeight != bounds.Height))
                {
                    using (Bitmap thumbBmp = new Bitmap(targetWidth, targetHeight, PixelFormat.Format24bppRgb))
                    {
                        using (Graphics tg = Graphics.FromImage(thumbBmp))
                        {
                            tg.InterpolationMode = InterpolationMode.Bilinear;
                            tg.CompositingQuality = CompositingQuality.HighSpeed;
                            tg.SmoothingMode = SmoothingMode.HighSpeed;
                            tg.DrawImage(bmp, 0, 0, targetWidth, targetHeight);
                        }
                        return CompressToJpegBytes(thumbBmp, quality);
                    }
                }

                return CompressToJpegBytes(bmp, quality);
            }
        }

        public static List<WindowCaptureItem> GetBroadcastableWindows(IntPtr excludeHwnd = default(IntPtr))
        {
            List<WindowCaptureItem> items = new List<WindowCaptureItem>();
            try
            {
                EnumWindows(delegate(IntPtr hWnd, IntPtr lParam)
                {
                    try
                    {
                        if (hWnd == IntPtr.Zero || hWnd == excludeHwnd) return true;
                        if (!IsWindowVisible(hWnd)) return true;

                        int len = GetWindowTextLength(hWnd);
                        if (len <= 0) return true;

                        int exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
                        if ((exStyle & WS_EX_TOOLWINDOW) != 0 && (exStyle & WS_EX_APPWINDOW) == 0)
                        {
                            return true;
                        }

                        try
                        {
                            int cloaked = 0;
                            if (DwmGetWindowAttribute(hWnd, DWMWA_CLOAKED, out cloaked, sizeof(int)) == 0 && cloaked != 0)
                            {
                                return true;
                            }
                        }
                        catch { }

                        StringBuilder sb = new StringBuilder(len + 1);
                        GetWindowText(hWnd, sb, sb.Capacity);
                        string title = sb.ToString().Trim();
                        if (string.IsNullOrEmpty(title) ||
                            title.Equals("Program Manager", StringComparison.OrdinalIgnoreCase) ||
                            title.Equals("Windows Input Experience", StringComparison.OrdinalIgnoreCase) ||
                            title.Equals("Microsoft Text Input Application", StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }

                        uint pid = 0;
                        GetWindowThreadProcessId(hWnd, out pid);
                        string procName = "App";
                        if (pid > 0)
                        {
                            try
                            {
                                using (Process p = Process.GetProcessById((int)pid))
                                {
                                    procName = p.ProcessName;
                                }
                            }
                            catch { }
                        }

                        items.Add(new WindowCaptureItem
                        {
                            Handle = hWnd,
                            ProcessId = (int)pid,
                            ProcessName = procName,
                            Title = title
                        });
                    }
                    catch { }
                    return true;
                }, IntPtr.Zero);
            }
            catch { }

            return items;
        }

        public static void BringWindowToFrontIfMinimized(IntPtr hWnd, bool activate = false)
        {
            if (hWnd == IntPtr.Zero || !IsWindow(hWnd)) return;
            try
            {
                if (IsIconic(hWnd))
                {
                    ShowWindow(hWnd, SW_RESTORE);
                }
                if (activate)
                {
                    SetForegroundWindow(hWnd);
                }
            }
            catch { }
        }

        public static byte[] CaptureWindowJpeg(IntPtr hWnd, long quality = 60, bool drawCursor = true)
        {
            AttachToActiveInputDesktop();

            if (hWnd == IntPtr.Zero || !IsWindow(hWnd))
            {
                return RenderPlaceholderFrame("⚠️ Chương trình đang phát sóng đã đóng!", "Vui lòng chọn chương trình khác hoặc chuyển sang chế độ Toàn Màn Hình.", quality);
            }

            if (IsIconic(hWnd))
            {
                try { ShowWindow(hWnd, SW_RESTORE); } catch { }
                if (IsIconic(hWnd))
                {
                    return RenderPlaceholderFrame("🪟 Cửa sổ chương trình đang thu nhỏ (Minimized)", "Vui lòng mở lại cửa sổ chương trình trên màn hình máy chủ để tiếp tục hiển thị.", quality);
                }
            }

            RECT rect;
            if (!GetWindowRect(hWnd, out rect))
            {
                return CaptureDesktopJpeg(quality, drawCursor, 0, 0);
            }

            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;
            if (width <= 10 || height <= 10)
            {
                return RenderPlaceholderFrame("🪟 Cửa sổ chương trình có kích thước quá nhỏ", "Đang chờ cửa sổ hiển thị bình thường...", quality);
            }

            using (Bitmap bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    bool captured = false;

                    // Try PrintWindow with PW_RENDERFULLCONTENT first (captures only the target application even if covered)
                    try
                    {
                        IntPtr hdc = g.GetHdc();
                        try
                        {
                            captured = PrintWindow(hWnd, hdc, PW_RENDERFULLCONTENT);
                            if (!captured)
                            {
                                captured = PrintWindow(hWnd, hdc, 0);
                            }
                        }
                        finally
                        {
                            g.ReleaseHdc(hdc);
                        }
                    }
                    catch
                    {
                        captured = false;
                    }

                    // Check if PrintWindow produced an all-black image (e.g. certain OpenGL/DirectX overlays)
                    if (captured && IsBitmapMostlyBlack(bmp))
                    {
                        captured = false;
                    }

                    // Fallback to screen region copy if PrintWindow didn't render content
                    if (!captured)
                    {
                        try
                        {
                            g.CopyFromScreen(rect.Left, rect.Top, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
                            captured = true;
                        }
                        catch { }
                    }

                    if (captured && drawCursor)
                    {
                        try
                        {
                            CURSORINFO pci;
                            pci.cbSize = Marshal.SizeOf(typeof(CURSORINFO));
                            if (GetCursorInfo(out pci) && pci.flags == CURSOR_SHOWING)
                            {
                                int cx = pci.ptScreenPos.x - rect.Left;
                                int cy = pci.ptScreenPos.y - rect.Top;
                                if (cx >= 0 && cx < width && cy >= 0 && cy < height)
                                {
                                    IntPtr hdc = g.GetHdc();
                                    try
                                    {
                                        DrawIcon(hdc, cx, cy, pci.hCursor);
                                    }
                                    finally
                                    {
                                        g.ReleaseHdc(hdc);
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                }

                return CompressToJpegBytes(bmp, quality);
            }
        }

        private static bool IsBitmapMostlyBlack(Bitmap bmp)
        {
            try
            {
                int w = bmp.Width;
                int h = bmp.Height;
                int nonBlack = 0;
                int[] xs = new int[] { w / 4, w / 2, (w * 3) / 4, w / 3, (w * 2) / 3 };
                int[] ys = new int[] { h / 4, h / 2, (h * 3) / 4, h / 3, (h * 2) / 3 };

                for (int i = 0; i < xs.Length; i++)
                {
                    for (int j = 0; j < ys.Length; j++)
                    {
                        Color c = bmp.GetPixel(Math.Max(0, Math.Min(w - 1, xs[i])), Math.Max(0, Math.Min(h - 1, ys[j])));
                        if (c.R > 4 || c.G > 4 || c.B > 4)
                        {
                            nonBlack++;
                            if (nonBlack >= 2) return false;
                        }
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static byte[] RenderPlaceholderFrame(string title, string subtitle, long quality)
        {
            using (Bitmap bmp = new Bitmap(1280, 720, PixelFormat.Format24bppRgb))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.FromArgb(20, 24, 32));
                    using (Font fTitle = new Font("Segoe UI", 18, FontStyle.Bold))
                    using (Font fSub = new Font("Segoe UI", 12, FontStyle.Regular))
                    using (SolidBrush bGold = new SolidBrush(Color.FromArgb(243, 156, 18)))
                    using (SolidBrush bWhite = new SolidBrush(Color.White))
                    {
                        g.DrawString(title, fTitle, bGold, new PointF(180, 310));
                        g.DrawString(subtitle, fSub, bWhite, new PointF(180, 360));
                    }
                }
                return CompressToJpegBytes(bmp, quality);
            }
        }

        private static byte[] CompressToJpegBytes(Bitmap bmp, long quality)
        {
            if (_jpegEncoder == null)
            {
                using (MemoryStream msFallback = new MemoryStream())
                {
                    bmp.Save(msFallback, ImageFormat.Jpeg);
                    return msFallback.ToArray();
                }
            }

            using (EncoderParameters encParams = new EncoderParameters(1))
            using (EncoderParameter param = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality))
            using (MemoryStream ms = new MemoryStream())
            {
                encParams.Param[0] = param;
                bmp.Save(ms, _jpegEncoder, encParams);
                return ms.ToArray();
            }
        }
    }
}
