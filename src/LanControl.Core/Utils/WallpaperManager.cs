using System;
using System.Runtime.InteropServices;
using System.Text;

namespace LanControl.Core.Utils
{
    public static class WallpaperManager
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SystemParametersInfo(uint uAction, uint uParam, string lpvParam, uint fuWinIni);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SystemParametersInfo(uint uAction, uint uParam, StringBuilder lpvParam, uint fuWinIni);

        private const uint SPI_SETDESKWALLPAPER = 0x0014;
        private const uint SPI_GETDESKWALLPAPER = 0x0073;
        private const uint SPIF_UPDATEINIFILE = 0x01;
        private const uint SPIF_SENDCHANGE = 0x02;

        private static readonly object _syncLock = new object();
        private static string _savedWallpaper = null;
        private static bool _isWallpaperHidden = false;

        public static bool IsWallpaperHidden
        {
            get { return _isWallpaperHidden; }
        }

        public static void HideWallpaper()
        {
            lock (_syncLock)
            {
                try
                {
                    if (!_isWallpaperHidden)
                    {
                        StringBuilder sb = new StringBuilder(512);
                        SystemParametersInfo(SPI_GETDESKWALLPAPER, 512, sb, 0);
                        _savedWallpaper = sb.ToString();

                        // Set solid blank wallpaper (empty path)
                        SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, "", SPIF_SENDCHANGE);
                        _isWallpaperHidden = true;
                    }
                }
                catch { }
            }
        }

        public static void RestoreWallpaper()
        {
            lock (_syncLock)
            {
                try
                {
                    if (_isWallpaperHidden)
                    {
                        string wp = string.IsNullOrEmpty(_savedWallpaper) ? "" : _savedWallpaper;
                        SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, wp, SPIF_SENDCHANGE);
                        _isWallpaperHidden = false;
                    }
                }
                catch { }
            }
        }
    }
}
