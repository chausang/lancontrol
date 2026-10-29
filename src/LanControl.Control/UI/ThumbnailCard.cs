using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using LanControl.Core.Models;

namespace LanControl.Control.UI
{
    public class ThumbnailCard : UserControl
    {
        private AgentInfo _agent;
        private PictureBox _picScreen;
        private Label _lblHost;
        private Label _lblIp;
        private Label _lblVlan;
        private Label _lblStats;
        private Panel _statusDot;
        private CheckBox _chkSelect;
        private ContextMenuStrip _contextMenu;

        public AgentInfo Agent { get { return _agent; } }
        public bool IsSelected { get { return _chkSelect.Checked; } set { _chkSelect.Checked = value; } }

        public event Action<AgentInfo> RemoteDesktopRequested;
        public event Action<AgentInfo> LockScreenRequested;
        public event Action<AgentInfo> UnlockScreenRequested;
        public event Action<AgentInfo> RunScriptRequested;
        public event Action<AgentInfo> ProcessManagerRequested;
        public event Action<AgentInfo> FileManagerRequested;
        public event Action<AgentInfo> FileDistributionRequested;
        public event Action<AgentInfo> HardwareInventoryRequested;
        public event Action<AgentInfo> SendMessageRequested;
        public event Action<AgentInfo> RebootRequested;
        public event Action<AgentInfo> ShutdownRequested;

        public ThumbnailCard(AgentInfo agent)
        {
            _agent = agent;
            InitializeComponent();
            UpdateData(agent);
        }

        private void InitializeComponent()
        {
            this.Size = new Size(240, 205);
            this.BackColor = ModernTheme.CardDark;
            this.Margin = new Padding(8);
            this.Padding = new Padding(4);
            this.BorderStyle = BorderStyle.FixedSingle;

            // Context Menu
            _contextMenu = new ContextMenuStrip();
            _contextMenu.Items.Add("🖥️ Điều Khiển Máy (Remote Desktop)", null, (s, e) => { if (RemoteDesktopRequested != null) RemoteDesktopRequested(_agent); });
            _contextMenu.Items.Add(new ToolStripSeparator());
            _contextMenu.Items.Add("🔍 Xem Cấu Hình & Bản Quyền Phần Mềm...", null, (s, e) => { if (HardwareInventoryRequested != null) HardwareInventoryRequested(_agent); });
            _contextMenu.Items.Add(new ToolStripSeparator());
            _contextMenu.Items.Add("📁 Quản Lý Tệp Từ Xa (Remote File Explorer)...", null, (s, e) => { if (FileManagerRequested != null) FileManagerRequested(_agent); });
            _contextMenu.Items.Add("🚀 Truyền Tập Tin Cho Máy Này (Send Files)...", null, (s, e) => { if (FileDistributionRequested != null) FileDistributionRequested(_agent); });
            _contextMenu.Items.Add(new ToolStripSeparator());
            _contextMenu.Items.Add("🔒 Khóa Màn Hình (Lock Screen)", null, (s, e) => { if (LockScreenRequested != null) LockScreenRequested(_agent); });
            _contextMenu.Items.Add("🔓 Mở Khóa Màn Hình (Unlock Screen)", null, (s, e) => { if (UnlockScreenRequested != null) UnlockScreenRequested(_agent); });
            _contextMenu.Items.Add(new ToolStripSeparator());
            _contextMenu.Items.Add("⚡ Thực Thi Lệnh / Script (Run CMD/PS)", null, (s, e) => { if (RunScriptRequested != null) RunScriptRequested(_agent); });
            _contextMenu.Items.Add("📋 Quản Lý Tác Vụ (Process Manager)", null, (s, e) => { if (ProcessManagerRequested != null) ProcessManagerRequested(_agent); });
            _contextMenu.Items.Add("💬 Gửi Thông Báo (Send Message)", null, (s, e) => { if (SendMessageRequested != null) SendMessageRequested(_agent); });
            _contextMenu.Items.Add(new ToolStripSeparator());
            _contextMenu.Items.Add("🔄 Khởi Động Lại (Reboot)", null, (s, e) => { if (RebootRequested != null) RebootRequested(_agent); });
            _contextMenu.Items.Add("⛔ Tắt Máy (Shutdown)", null, (s, e) => { if (ShutdownRequested != null) ShutdownRequested(_agent); });

            this.ContextMenuStrip = _contextMenu;

            // Top Header Panel
            Panel topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 38,
                BackColor = Color.FromArgb(30, 36, 48)
            };

            _statusDot = new Panel
            {
                Size = new Size(10, 10),
                Location = new Point(6, 14),
                BackColor = ModernTheme.AccentGreen
            };

            _chkSelect = new CheckBox
            {
                Location = new Point(20, 10),
                Size = new Size(16, 16),
                BackColor = Color.Transparent
            };

            _lblHost = new Label
            {
                Text = _agent.Hostname,
                Font = ModernTheme.SubHeaderFont,
                ForeColor = ModernTheme.TextPrimary,
                Location = new Point(40, 2),
                Size = new Size(115, 18),
                AutoEllipsis = true
            };

            _lblIp = new Label
            {
                Text = _agent.IpAddress,
                Font = ModernTheme.SmallFont,
                ForeColor = ModernTheme.TextSecondary,
                Location = new Point(40, 20),
                Size = new Size(115, 16),
                AutoEllipsis = true
            };

            _lblVlan = new Label
            {
                Text = _agent.VlanTag,
                Font = ModernTheme.SmallFont,
                ForeColor = Color.FromArgb(41, 128, 185),
                BackColor = Color.FromArgb(20, 30, 45),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(158, 6),
                Size = new Size(74, 24),
                BorderStyle = BorderStyle.FixedSingle,
                AutoEllipsis = true
            };

            topPanel.Controls.Add(_statusDot);
            topPanel.Controls.Add(_chkSelect);
            topPanel.Controls.Add(_lblHost);
            topPanel.Controls.Add(_lblIp);
            topPanel.Controls.Add(_lblVlan);

            // Thumbnail Image Box
            _picScreen = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black,
                Cursor = Cursors.Hand
            };
            _picScreen.DoubleClick += (s, e) => { if (RemoteDesktopRequested != null) RemoteDesktopRequested(_agent); };
            _picScreen.ContextMenuStrip = _contextMenu;

            // Bottom Stats Panel
            Panel bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 24,
                BackColor = Color.FromArgb(26, 32, 42)
            };

            _lblStats = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = ModernTheme.SmallFont,
                ForeColor = ModernTheme.TextSecondary,
                Padding = new Padding(6, 0, 0, 0),
                Text = string.Format("CPU: {0}% | RAM: {1}MB | {2}", _agent.CpuUsagePercent, _agent.AvailableMemoryMb, _agent.UserName)
            };
            bottomPanel.Controls.Add(_lblStats);

            this.Controls.Add(_picScreen);
            this.Controls.Add(topPanel);
            this.Controls.Add(bottomPanel);
        }

        public void UpdateData(AgentInfo agent)
        {
            _agent = agent;
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<AgentInfo>(UpdateData), agent);
                return;
            }

            _lblHost.Text = agent.Hostname;
            _lblIp.Text = agent.IpAddress;
            _lblVlan.Text = string.IsNullOrEmpty(agent.VlanTag) ? "Default" : agent.VlanTag;
            _lblStats.Text = string.Format("CPU: {0}% | RAM: {1}MB | {2}", agent.CpuUsagePercent, agent.AvailableMemoryMb, agent.UserName);

            if (agent.IsLocked)
            {
                _statusDot.BackColor = ModernTheme.AccentRed;
            }
            else if (agent.IsStreaming)
            {
                _statusDot.BackColor = ModernTheme.AccentBlue;
            }
            else
            {
                _statusDot.BackColor = ModernTheme.AccentGreen;
            }
        }

        public void UpdateThumbnail(byte[] imageBytes)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action<byte[]>(UpdateThumbnail), (object)imageBytes);
                return;
            }

            if (imageBytes == null || imageBytes.Length == 0) return;

            try
            {
                using (MemoryStream ms = new MemoryStream(imageBytes))
                {
                    Image newImg = Image.FromStream(ms);
                    Image oldImg = _picScreen.Image;
                    _picScreen.Image = newImg;
                    if (oldImg != null) oldImg.Dispose();
                }
            }
            catch { }
        }
    }
}
