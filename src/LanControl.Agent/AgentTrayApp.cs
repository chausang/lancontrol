using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using LanControl.Core.Models;
using LanControl.Core.Utils;

namespace LanControl.Agent
{
    public class AgentTrayApp : ApplicationContext
    {
        private readonly NotifyIcon _trayIcon;
        private readonly AgentClient _client;
        private readonly AgentConfig _config;

        public AgentTrayApp(AgentConfig config)
        {
            _config = config;
            _client = new AgentClient(_config);
            _client.ConnectionStateChanged += OnConnectionChanged;
            _client.HelpResponseReceived += OnHelpResponse;

            ContextMenu menu = new ContextMenu();
            menu.MenuItems.Add(new MenuItem("LAN Control Client Agent (Active)") { Enabled = false });
            menu.MenuItems.Add("-");
            menu.MenuItems.Add(new MenuItem("🙋 Gọi Hỗ Trợ (Yêu cầu kết nối)...", OnRequestHelp));
            menu.MenuItems.Add(new MenuItem("Thông tin kết nối", OnShowInfo));
            menu.MenuItems.Add(new MenuItem("Đổi VLAN...", OnChangeVlan));
            menu.MenuItems.Add(new MenuItem("Kết nối lại Gateway...", OnReconnect));
            menu.MenuItems.Add(new MenuItem("ℹ️ Giới thiệu (About)...", OnShowAbout));
            menu.MenuItems.Add("-");
            menu.MenuItems.Add(new MenuItem("Thoát Agent", OnExit));

            // Load icon.ico or create dynamic tray icon with "L" (LAN Control)
            Icon appIcon = null;
            try
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico");
                if (File.Exists(iconPath))
                {
                    appIcon = new Icon(iconPath);
                }
            }
            catch { }

            if (appIcon == null)
            {
                Bitmap iconBmp = new Bitmap(16, 16);
                using (Graphics g = Graphics.FromImage(iconBmp))
                {
                    g.Clear(Color.FromArgb(16, 150, 130));
                    using (Font font = new Font("Segoe UI", 8, FontStyle.Bold))
                    using (Brush brush = new SolidBrush(Color.White))
                    {
                        g.DrawString("L", font, brush, 2, 0);
                    }
                }
                appIcon = Icon.FromHandle(iconBmp.GetHicon());
            }

            _trayIcon = new NotifyIcon
            {
                Icon = appIcon,
                ContextMenu = menu,
                Visible = true
            };

            UpdateTrayTooltip("Đang kết nối...");
            _trayIcon.MouseMove += (s, e) => UpdateTrayTooltip();

            _client.Start();
        }

        private void UpdateTrayTooltip(string statusText = null)
        {
            if (_trayIcon == null) return;
            try
            {
                string hostname = _client != null && _client.Info != null && !string.IsNullOrEmpty(_client.Info.Hostname)
                    ? _client.Info.Hostname
                    : Environment.MachineName;

                string ip = SystemInfoCollector.GetLocalIpAddress();
                if (_client != null && _client.Info != null && !string.IsNullOrEmpty(ip))
                {
                    _client.Info.IpAddress = ip;
                }

                if (string.IsNullOrEmpty(statusText))
                {
                    statusText = (_client != null && _client.IsConnected) ? "Đã kết nối" : "Mất kết nối";
                }

                // Format:
                // Tên: [Hostname]
                // IP: [IP]
                // Trạng thái: [Trạng thái]
                string tooltip = string.Format("Tên: {0}\nIP: {1}\nTrạng thái: {2}", hostname, ip, statusText);

                // Windows NotifyIcon.Text in .NET Framework has a strict limit of 63 characters
                if (tooltip.Length > 63)
                {
                    tooltip = string.Format("Tên: {0}\nIP: {1}\nTT: {2}", hostname, ip, statusText);
                }
                if (tooltip.Length > 63)
                {
                    tooltip = string.Format("{0} ({1})\n{2}", hostname, ip, statusText);
                }
                if (tooltip.Length > 63)
                {
                    int maxHost = Math.Max(5, 63 - (ip.Length + statusText.Length + 6));
                    string shortHost = hostname.Length > maxHost ? hostname.Substring(0, maxHost) : hostname;
                    tooltip = string.Format("{0} ({1})\n{2}", shortHost, ip, statusText);
                }
                if (tooltip.Length > 63)
                {
                    tooltip = tooltip.Substring(0, 63);
                }

                _trayIcon.Text = tooltip;
            }
            catch { }
        }

        private void OnConnectionChanged(bool connected)
        {
            UpdateTrayTooltip(connected ? "Đã kết nối" : "Mất kết nối");
        }

        private void OnShowInfo(object sender, EventArgs e)
        {
            string info = string.Format(
                "--- THÔNG TIN MÁY KHÁCH LAN CONTROL ---\n\n" +
                "Tên máy trạm: {0}\n" +
                "Địa chỉ IP: {1}\n" +
                "Địa chỉ MAC: {2}\n" +
                "Phân đoạn VLAN: {3}\n" +
                "Gateway Server: {4}:{5}\n" +
                "Trạng thái: {6}\n" +
                "Bộ nhớ RAM khả dụng: {7} MB\n" +
                "CPU hiện tại: {8}%",
                _client.Info.Hostname,
                _client.Info.IpAddress,
                _client.Info.MacAddress,
                _client.Info.VlanTag,
                _config.GatewayHost,
                _config.GatewayPort,
                _client.IsConnected ? "Đã kết nối Gateway" : "Mất kết nối",
                _client.Info.AvailableMemoryMb,
                _client.Info.CpuUsagePercent);

            MessageBox.Show(info, "LAN Control Client Agent", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OnChangeVlan(object sender, EventArgs e)
        {
            using (Form prompt = new Form())
            {
                prompt.Width = 380;
                prompt.Height = 160;
                prompt.FormBorderStyle = FormBorderStyle.FixedDialog;
                prompt.Text = "Cấu hình Phân Đoạn VLAN";
                prompt.StartPosition = FormStartPosition.CenterScreen;
                prompt.MaximizeBox = false;
                prompt.MinimizeBox = false;

                Label textLabel = new Label() { Left = 20, Top = 15, Text = "Nhập nhãn VLAN (VD: VLAN 10, VLAN 20):", Width = 320 };
                TextBox textBox = new TextBox() { Left = 20, Top = 40, Width = 320, Text = _config.VlanTag };
                Button confirmation = new Button() { Text = "Xác nhận", Left = 160, Width = 85, Top = 75, DialogResult = DialogResult.OK };
                Button cancel = new Button() { Text = "Hủy", Left = 255, Width = 85, Top = 75, DialogResult = DialogResult.Cancel };

                prompt.Controls.Add(textLabel);
                prompt.Controls.Add(textBox);
                prompt.Controls.Add(confirmation);
                prompt.Controls.Add(cancel);
                prompt.AcceptButton = confirmation;

                if (prompt.ShowDialog() == DialogResult.OK)
                {
                    string newVlan = textBox.Text.Trim();
                    if (!string.IsNullOrEmpty(newVlan) && !newVlan.Equals(_config.VlanTag, StringComparison.OrdinalIgnoreCase))
                    {
                        _config.VlanTag = newVlan;
                        _client.Info.VlanTag = newVlan;
                        _config.Save("agent_config.json");
                        MessageBox.Show("Đã cập nhật phân đoạn: " + newVlan + "\nĐang đăng ký lại với Gateway...", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        _client.Stop();
                        UpdateTrayTooltip("Đang kết nối lại...");
                        _client.Start();
                    }
                }
            }
        }

        private void OnReconnect(object sender, EventArgs e)
        {
            using (Form prompt = new Form())
            {
                prompt.Width = 460;
                prompt.Height = 270;
                prompt.FormBorderStyle = FormBorderStyle.FixedDialog;
                prompt.Text = "Cấu Hình & Kết Nối Lại Gateway Server";
                prompt.StartPosition = FormStartPosition.CenterScreen;
                prompt.MaximizeBox = false;
                prompt.MinimizeBox = false;
                prompt.BackColor = Color.FromArgb(245, 247, 250);

                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico");
                if (File.Exists(iconPath))
                {
                    try { prompt.Icon = new Icon(iconPath); } catch { }
                }

                Label lblHeader = new Label
                {
                    Left = 20,
                    Top = 15,
                    Width = 405,
                    Text = "🔄 THIẾT LẬP KẾT NỐI MÁY CHỦ GATEWAY",
                    Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(41, 128, 185)
                };

                Label lblHost = new Label
                {
                    Left = 20,
                    Top = 48,
                    Width = 405,
                    Text = "Địa chỉ IP / Tên miền máy chủ Gateway (Server IP):",
                    Font = new Font("Segoe UI", 9F, FontStyle.Regular)
                };

                TextBox txtHost = new TextBox
                {
                    Left = 20,
                    Top = 72,
                    Width = 405,
                    Text = _config.GatewayHost,
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Regular)
                };

                Label lblPort = new Label
                {
                    Left = 20,
                    Top = 108,
                    Width = 140,
                    Text = "Cổng kết nối (Port):",
                    Font = new Font("Segoe UI", 9F, FontStyle.Regular)
                };

                TextBox txtPort = new TextBox
                {
                    Left = 20,
                    Top = 130,
                    Width = 100,
                    Text = _config.GatewayPort.ToString(),
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Regular)
                };

                Button btnDefaultPort = new Button
                {
                    Left = 130,
                    Top = 129,
                    Width = 120,
                    Height = 26,
                    Text = "↺ Đặt lại 8443",
                    Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                    Cursor = Cursors.Hand
                };
                btnDefaultPort.Click += (s, ev) => txtPort.Text = "8443";

                Label lblHint = new Label
                {
                    Left = 260,
                    Top = 133,
                    Width = 175,
                    Text = "💡 (Mặc định: 8443)",
                    Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
                    ForeColor = Color.FromArgb(211, 84, 0)
                };

                Button btnConnect = new Button
                {
                    Text = "🔌 Kết Nối Ngay",
                    Left = 195,
                    Width = 135,
                    Height = 34,
                    Top = 175,
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                    BackColor = Color.FromArgb(46, 204, 113),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand
                };
                btnConnect.FlatAppearance.BorderSize = 0;

                Button btnCancel = new Button
                {
                    Text = "Hủy Bỏ",
                    Left = 340,
                    Width = 85,
                    Height = 34,
                    Top = 175,
                    Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                    DialogResult = DialogResult.Cancel,
                    Cursor = Cursors.Hand
                };

                btnConnect.Click += (s, ev) =>
                {
                    string newHost = txtHost.Text.Trim();
                    if (string.IsNullOrEmpty(newHost))
                    {
                        MessageBox.Show(prompt, "Vui lòng nhập địa chỉ IP của máy chủ Gateway!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtHost.Focus();
                        return;
                    }

                    int newPort;
                    if (!int.TryParse(txtPort.Text.Trim(), out newPort) || newPort <= 0 || newPort > 65535)
                    {
                        MessageBox.Show(prompt, "Cổng kết nối không hợp lệ! Vui lòng nhập số từ 1 đến 65535 (Mặc định là 8443).", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtPort.Focus();
                        return;
                    }

                    // Save and apply new config
                    _config.GatewayHost = newHost;
                    _config.GatewayPort = newPort;

                    string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "agent_config.json");
                    _config.Save(configPath);

                    // Reconnect client
                    _client.Stop();
                    UpdateTrayTooltip("Đang kết nối lại...");
                    _client.Start();

                    if (_trayIcon != null)
                    {
                        _trayIcon.ShowBalloonTip(3000, "Đang Kết Nối Lại Gateway", 
                            string.Format("Đang thử kết nối lại tới Gateway Server tại {0}:{1}...", newHost, newPort), 
                            ToolTipIcon.Info);
                    }

                    prompt.DialogResult = DialogResult.OK;
                    prompt.Close();
                };

                prompt.Controls.Add(lblHeader);
                prompt.Controls.Add(lblHost);
                prompt.Controls.Add(txtHost);
                prompt.Controls.Add(lblPort);
                prompt.Controls.Add(txtPort);
                prompt.Controls.Add(btnDefaultPort);
                prompt.Controls.Add(lblHint);
                prompt.Controls.Add(btnConnect);
                prompt.Controls.Add(btnCancel);

                prompt.AcceptButton = btnConnect;
                prompt.CancelButton = btnCancel;

                prompt.ShowDialog();
            }
        }

        private void OnShowAbout(object sender, EventArgs e)
        {
            string info = "LAN Control Multi-VLAN Management & Monitoring Suite\n\n" +
                          "• Nền tảng: Phần mềm được vibe coding dựa trên mã nguồn https://github.com/mRemoteNG/mRemoteNG\n" +
                          "• Tác giả: Bùi Quang Sáng\n" +
                          "• Đơn vị: Phòng CNTT, BV ĐK Châu Đốc\n" +
                          "• Email: tocnttbkdkkvtinh@gmail.com";

            MessageBox.Show(info, "Thông tin chương trình (About)", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OnRequestHelp(object sender, EventArgs e)
        {
            if (!_client.IsConnected)
            {
                MessageBox.Show("Máy trạm hiện chưa kết nối với Gateway Server!\nVui lòng kiểm tra lại kết nối mạng.", "Không thể gửi yêu cầu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (Form prompt = new Form())
            {
                prompt.Width = 420;
                prompt.Height = 185;
                prompt.FormBorderStyle = FormBorderStyle.FixedDialog;
                prompt.Text = "Yêu Cầu Hỗ Trợ Kỹ Thuật (Gọi Quản Trị Viên)";
                prompt.StartPosition = FormStartPosition.CenterScreen;
                prompt.MaximizeBox = false;
                prompt.MinimizeBox = false;

                Label lblMsg = new Label() { Left = 20, Top = 15, Text = "Nhập nội dung/lời nhắn cần hỗ trợ (hoặc để mặc định):", Width = 360 };
                TextBox txtMsg = new TextBox() { Left = 20, Top = 40, Width = 360, Text = "Cần quản trị viên kết nối hỗ trợ xử lý sự cố." };
                Button btnSend = new Button() { Text = "Gửi Yêu Cầu", Left = 175, Width = 110, Top = 85, DialogResult = DialogResult.OK };
                Button btnCancel = new Button() { Text = "Hủy Bỏ", Left = 295, Width = 85, Top = 85, DialogResult = DialogResult.Cancel };

                prompt.Controls.Add(lblMsg);
                prompt.Controls.Add(txtMsg);
                prompt.Controls.Add(btnSend);
                prompt.Controls.Add(btnCancel);
                prompt.AcceptButton = btnSend;

                if (prompt.ShowDialog() == DialogResult.OK)
                {
                    string msg = txtMsg.Text.Trim();
                    bool sent = _client.SendHelpRequest(msg);
                    if (sent)
                    {
                        if (_trayIcon != null)
                        {
                            _trayIcon.ShowBalloonTip(4000, "Đã Gửi Yêu Cầu Hỗ Trợ", "Đã gửi yêu cầu kết nối tới Quản trị viên. Vui lòng chờ phản hồi.", ToolTipIcon.Info);
                        }
                        else
                        {
                            MessageBox.Show("Đã gửi yêu cầu tới Quản trị viên!\nVui lòng đợi quản trị viên chấp nhận kết nối.", "Đã Gửi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Gửi yêu cầu thất bại! Vui lòng thử lại sau.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void OnHelpResponse(HelpResponse resp)
        {
            if (resp == null) return;

            string title = resp.Accepted ? "Quản Trị Viên Đã Chấp Nhận" : "Yêu Cầu Bị Từ Chối";
            string text = resp.Accepted
                ? "Quản trị viên đã đồng ý hỗ trợ và đang kết nối tới máy tính của bạn."
                : (string.IsNullOrEmpty(resp.Message) ? "Quản trị viên hiện đang bận hoặc đã từ chối yêu cầu kết nối." : resp.Message);

            ToolTipIcon icon = resp.Accepted ? ToolTipIcon.Info : ToolTipIcon.Warning;

            try
            {
                if (_trayIcon != null)
                {
                    _trayIcon.ShowBalloonTip(5000, title, text, icon);
                }
            }
            catch { }
        }

        private void OnExit(object sender, EventArgs e)
        {
            _client.Stop();
            _trayIcon.Visible = false;
            Application.Exit();
        }
    }
}
