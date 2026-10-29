using System;
using System.Drawing;
using System.Media;
using System.Windows.Forms;
using LanControl.Core.Models;

namespace LanControl.Control.UI
{
    public class HelpRequestPromptForm : Form
    {
        private readonly HelpRequest _request;

        public HelpRequest Request { get { return _request; } }

        public HelpRequestPromptForm(HelpRequest request)
        {
            _request = request;
            InitializeComponent();
            try
            {
                SystemSounds.Exclamation.Play();
            }
            catch { }
        }

        private void InitializeComponent()
        {
            this.Text = "🚨 Yêu Cầu Hỗ Trợ Từ Máy Trạm";
            this.Size = new Size(480, 310);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.TopMost = true;
            this.BackColor = ModernTheme.BackgroundDark;
            this.ForeColor = ModernTheme.TextPrimary;

            // Top Header Banner
            Panel banner = new Panel
            {
                Dock = DockStyle.Top,
                Height = 55,
                BackColor = Color.FromArgb(192, 57, 43), // Vivid Alert Red
                Padding = new Padding(12)
            };

            Label lblTitle = new Label
            {
                Text = "🔔 CÓ MÁY TRẠM ĐANG GỌI HỖ TRỢ KỸ THUẬT!",
                ForeColor = Color.White,
                Font = ModernTheme.HeaderFont,
                AutoSize = true,
                Location = new Point(14, 15)
            };
            banner.Controls.Add(lblTitle);

            // Center Info Panel
            Panel center = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 15, 20, 15)
            };

            Label lblHost = new Label
            {
                Text = string.Format("🖥️ Máy trạm: {0} ({1})", _request.Hostname, _request.IpAddress),
                Font = ModernTheme.SubHeaderFont,
                ForeColor = ModernTheme.AccentCyan,
                Location = new Point(20, 15),
                Size = new Size(420, 24)
            };

            Label lblVlan = new Label
            {
                Text = string.Format("🌐 Phân đoạn: {0}   |   ⏰ Thời gian: {1:HH:mm:ss dd/MM/yyyy}", _request.VlanTag, _request.Timestamp),
                Font = ModernTheme.RegularFont,
                ForeColor = ModernTheme.TextSecondary,
                Location = new Point(20, 42),
                Size = new Size(420, 20)
            };

            Label lblMsgHeader = new Label
            {
                Text = "💬 Lời nhắn từ người dùng máy trạm:",
                Font = ModernTheme.SmallFont,
                ForeColor = ModernTheme.TextMuted,
                Location = new Point(20, 70),
                Size = new Size(420, 18)
            };

            TextBox txtMessage = new TextBox
            {
                Text = string.IsNullOrEmpty(_request.Message) ? "(Không có lời nhắn kèm theo)" : _request.Message,
                ReadOnly = true,
                Multiline = true,
                BackColor = ModernTheme.CardDark,
                ForeColor = ModernTheme.TextPrimary,
                Font = ModernTheme.RegularFont,
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(20, 90),
                Size = new Size(420, 55)
            };

            // Buttons
            Button btnAccept = new Button
            {
                Text = "✅ ĐỒNG Ý (KẾT NỐI NGAY)",
                Size = new Size(205, 40),
                Location = new Point(20, 160),
                DialogResult = DialogResult.OK,
                Cursor = Cursors.Hand
            };
            ModernTheme.ApplyToButton(btnAccept, ModernTheme.AccentGreen, Color.White);

            Button btnDecline = new Button
            {
                Text = "❌ TỪ CHỐI",
                Size = new Size(130, 40),
                Location = new Point(235, 160),
                DialogResult = DialogResult.Cancel,
                Cursor = Cursors.Hand
            };
            ModernTheme.ApplyToButton(btnDecline, ModernTheme.AccentRed, Color.White);

            center.Controls.Add(lblHost);
            center.Controls.Add(lblVlan);
            center.Controls.Add(lblMsgHeader);
            center.Controls.Add(txtMessage);
            center.Controls.Add(btnAccept);
            center.Controls.Add(btnDecline);

            this.Controls.Add(center);
            this.Controls.Add(banner);
            this.AcceptButton = btnAccept;
            this.CancelButton = btnDecline;
        }
    }
}
