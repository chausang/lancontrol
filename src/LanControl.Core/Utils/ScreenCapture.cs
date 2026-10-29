using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace LanControl.Core.Utils
{
    public static class ScreenCapture
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
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

        [DllImport("user32.dll")]
        private static extern bool GetCursorInfo(out CURSORINFO pci);

        [DllImport("user32.dll")]
        private static extern bool DrawIcon(IntPtr hDC, int X, int Y, IntPtr hIcon);

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
            using (EncoderParameter param = new EncoderParameter(Encoder.Quality, quality))
            using (MemoryStream ms = new MemoryStream())
            {
                encParams.Param[0] = param;
                bmp.Save(ms, _jpegEncoder, encParams);
                return ms.ToArray();
            }
        }
    }
}
