using System;
using System.Drawing;
using System.Windows.Forms;

namespace LanControl.SetupWizard
{
    public static class ModernTheme
    {
        public static readonly Color BackgroundDark = Color.FromArgb(20, 24, 32);
        public static readonly Color PanelDark = Color.FromArgb(28, 34, 46);
        public static readonly Color CardDark = Color.FromArgb(36, 43, 58);
        public static readonly Color CardSelected = Color.FromArgb(24, 60, 80);
        public static readonly Color BorderDark = Color.FromArgb(50, 60, 80);
        public static readonly Color BorderSelected = Color.FromArgb(26, 188, 156);

        public static readonly Color TextPrimary = Color.FromArgb(240, 244, 250);
        public static readonly Color TextSecondary = Color.FromArgb(160, 175, 195);
        public static readonly Color TextMuted = Color.FromArgb(110, 125, 145);

        public static readonly Color AccentBlue = Color.FromArgb(41, 128, 185);
        public static readonly Color AccentCyan = Color.FromArgb(26, 188, 156);
        public static readonly Color AccentGreen = Color.FromArgb(46, 204, 113);
        public static readonly Color AccentYellow = Color.FromArgb(243, 156, 18);
        public static readonly Color AccentRed = Color.FromArgb(231, 76, 60);

        public static readonly Font HeaderFont = new Font("Segoe UI", 12.5F, FontStyle.Bold);
        public static readonly Font SubHeaderFont = new Font("Segoe UI", 10F, FontStyle.Bold);
        public static readonly Font RegularFont = new Font("Segoe UI", 9.5F, FontStyle.Regular);
        public static readonly Font SmallFont = new Font("Segoe UI", 8.5F, FontStyle.Regular);
        public static readonly Font CodeFont = new Font("Consolas", 9F, FontStyle.Regular);

        public static void ApplyToButton(Button btn, Color bg, Color fg)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = bg;
            btn.ForeColor = fg;
            btn.Font = SubHeaderFont;
            btn.Cursor = Cursors.Hand;
        }
    }
}
