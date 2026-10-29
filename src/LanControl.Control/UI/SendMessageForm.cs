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

                MessageBox.Show("Đã phát đi thông điệp tới máy trạm!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
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
    }
}
