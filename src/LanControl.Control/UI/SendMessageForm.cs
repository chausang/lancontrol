using System;
using System.Drawing;
using System.Windows.Forms;
using LanControl.Core.Models;

namespace LanControl.Control.UI
{
    public class SendMessageForm : Form
    {
        private readonly ControlClient _client;
        private readonly AgentInfo _singleAgent;
        private readonly string _targetVlan;
        private TextBox _txtMessage;
        private Button _btnSend;
        private Button _btnCancel;

        public SendMessageForm(ControlClient client, AgentInfo singleAgent = null, string targetVlan = "")
        {
            _client = client;
            _singleAgent = singleAgent;
            _targetVlan = targetVlan;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Gửi Thông Báo / Tin Nhắn Tới Máy Trạm";
            this.Size = new Size(480, 260);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = ModernTheme.PanelDark;
            this.ForeColor = ModernTheme.TextPrimary;

            string recipient = _singleAgent != null
                ? string.Format("{0} ({1})", _singleAgent.Hostname, _singleAgent.IpAddress)
                : (!string.IsNullOrEmpty(_targetVlan) ? "Phân đoạn: " + _targetVlan : "Tất cả các máy trạm (Broadcast)");

            Label lblTo = new Label
            {
                Text = "Người nhận: " + recipient,
                Font = ModernTheme.SubHeaderFont,
                ForeColor = ModernTheme.AccentCyan,
                Location = new Point(20, 15),
                AutoSize = true
            };

            Label lblPrompt = new Label
            {
                Text = "Nội dung thông điệp hiển thị trên màn hình máy khách:",
                Font = ModernTheme.RegularFont,
                ForeColor = ModernTheme.TextSecondary,
                Location = new Point(20, 42),
                AutoSize = true
            };

            _txtMessage = new TextBox
            {
                Location = new Point(20, 68),
                Size = new Size(424, 95),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = ModernTheme.BackgroundDark,
                ForeColor = ModernTheme.TextPrimary,
                Font = ModernTheme.RegularFont,
                BorderStyle = BorderStyle.FixedSingle,
                Text = "Xin chào! Quản trị viên hệ thống đang gửi thông báo bảo trì định kỳ."
            };

            _btnSend = new Button
            {
                Text = "✉️ Gửi Tin Nhắn",
                Location = new Point(220, 175),
                Size = new Size(130, 32)
            };
            ModernTheme.ApplyToButton(_btnSend, ModernTheme.AccentBlue, Color.White);
            _btnSend.Click += (s, e) =>
            {
                string msg = _txtMessage.Text.Trim();
                if (string.IsNullOrEmpty(msg)) return;

                if (_singleAgent != null)
                {
                    _client.SendCommand(_singleAgent.AgentId, CommandType.SendMessage, msg);
                }
                else
                {
                    _client.SendCommand("", CommandType.SendMessage, msg, _targetVlan);
                }

                this.DialogResult = DialogResult.OK;
                this.Close();

                ShowNonBlockingSuccessToast(recipient);
            };

            _btnCancel = new Button
            {
                Text = "Hủy",
                Location = new Point(360, 175),
                Size = new Size(84, 32)
            };
            ModernTheme.ApplyToButton(_btnCancel, ModernTheme.CardDark, ModernTheme.TextPrimary);
            _btnCancel.Click += (s, e) => this.Close();

            this.Controls.Add(lblTo);
            this.Controls.Add(lblPrompt);
            this.Controls.Add(_txtMessage);
            this.Controls.Add(_btnSend);
            this.Controls.Add(_btnCancel);
            this.AcceptButton = _btnSend;
            this.CancelButton = _btnCancel;
        }

        private static void ShowNonBlockingSuccessToast(string recipient)
        {
            try
            {
                ToastNotificationForm toast = new ToastNotificationForm("Đã phát đi thông điệp tới: " + recipient);
                toast.Show();
            }
            catch { }
        }

        private class ToastNotificationForm : Form
        {
            private System.Windows.Forms.Timer _closeTimer;

            protected override bool ShowWithoutActivation
            {
                get { return true; }
            }

            protected override CreateParams CreateParams
            {
                get
                {
                    CreateParams cp = base.CreateParams;
                    cp.ExStyle |= 0x00000008; // WS_EX_TOPMOST
                    cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW
                    cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
                    return cp;
                }
            }

            public ToastNotificationForm(string detailText)
            {
                this.FormBorderStyle = FormBorderStyle.None;
                this.ShowInTaskbar = false;
                this.TopMost = true;
                this.StartPosition = FormStartPosition.Manual;
                this.Size = new Size(360, 68);
                this.BackColor = Color.FromArgb(22, 36, 54);
                this.ForeColor = Color.White;

                Rectangle workArea = Screen.PrimaryScreen.WorkingArea;
                this.Location = new Point(workArea.Right - this.Width - 20, workArea.Bottom - this.Height - 20);

                Panel accentBar = new Panel
                {
                    Dock = DockStyle.Left,
                    Width = 6,
                    BackColor = ModernTheme.AccentGreen
                };

                Label lblTitle = new Label
                {
                    Text = "✅ Đã gửi thông điệp tới máy trạm!",
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    ForeColor = ModernTheme.AccentGreen,
                    Location = new Point(16, 10),
                    AutoSize = true
                };

                Label lblDetail = new Label
                {
                    Text = detailText,
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                    ForeColor = ModernTheme.TextSecondary,
                    Location = new Point(16, 34),
                    Size = new Size(310, 22),
                    AutoEllipsis = true
                };

                Label lblClose = new Label
                {
                    Text = "✕",
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                    ForeColor = ModernTheme.TextSecondary,
                    Location = new Point(332, 8),
                    Size = new Size(20, 20),
                    Cursor = Cursors.Hand,
                    TextAlign = ContentAlignment.MiddleCenter
                };

                EventHandler dismiss = (s, e) =>
                {
                    try { this.Close(); } catch { }
                };

                this.Click += dismiss;
                lblTitle.Click += dismiss;
                lblDetail.Click += dismiss;
                lblClose.Click += dismiss;

                this.Controls.Add(lblClose);
                this.Controls.Add(lblTitle);
                this.Controls.Add(lblDetail);
                this.Controls.Add(accentBar);

                this.Paint += (s, e) =>
                {
                    using (Pen pen = new Pen(ModernTheme.AccentGreen, 1))
                    {
                        e.Graphics.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
                    }
                };

                _closeTimer = new System.Windows.Forms.Timer { Interval = 2200 };
                _closeTimer.Tick += (s, e) =>
                {
                    _closeTimer.Stop();
                    try { this.Close(); } catch { }
                };
                this.Shown += (s, e) => _closeTimer.Start();
                this.FormClosed += (s, e) =>
                {
                    try { if (_closeTimer != null) _closeTimer.Dispose(); } catch { }
                };
            }
        }
    }
}
