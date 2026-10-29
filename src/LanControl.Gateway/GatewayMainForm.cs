using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using LanControl.Core.Models;
using LanControl.Core.Utils;

namespace LanControl.Gateway
{
    public class GatewayMainForm : Form
    {
        private readonly int _port;
        private readonly GatewayServer _server;
        private readonly DateTime _startTime;
        private readonly NotifyIcon _trayIcon;

        // UI Components
        private Label _lblStatusBadge;
        private Label _lblMetricPort;
        private Label _lblMetricAgents;
        private Label _lblMetricVlans;
        private Label _lblMetricUptime;

        private Button _btnMinimizeToTray;
        private Button _btnRestart;
        private Button _btnCopyIp;
        private Button _btnLaunchControl;

        private TabControl _tabControl;
        private DataGridView _gridAgents;
        private TextBox _txtSearch;
        private DataGridView _gridVlans;
        private RichTextBox _rtbLogs;
        private CheckBox _chkAutoScroll;

        private Timer _refreshTimer;
        private bool _isExplicitExit = false;

        public GatewayMainForm(int port = 8443, bool startMinimized = false)
        {
            _port = port;
            _startTime = DateTime.Now;
            _server = new GatewayServer(_port);

            // Icon loading
            Icon appIcon = LoadAppIcon();
            this.Icon = appIcon;

            InitializeComponent();

            // Tray Icon Setup
            _trayIcon = new NotifyIcon
            {
                Icon = appIcon,
                Text = "LAN Control Central Gateway - Đang chạy",
                Visible = true
            };
            SetupTrayContextMenu();
            _trayIcon.DoubleClick += (s, e) => RestoreFromTray();
            _trayIcon.MouseMove += (s, e) => UpdateTrayTooltip();

            // Hook Server & Logger Events
            _server.AgentsUpdated += () =>
            {
                if (this.IsHandleCreated)
                {
                    try { this.BeginInvoke(new Action(UpdateAgentData)); } catch { }
                }
            };

            Logger.OnLog += OnServerLog;

            // Start Server
            try
            {
                _server.Start();
                UpdateStatusBadge(true);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to start Gateway on port {0}: {1}", _port, ex.Message);
                UpdateStatusBadge(false);
                MessageBox.Show("Không thể khởi động Gateway trên cổng " + _port + "!\nChi tiết lỗi: " + ex.Message,
                                "Lỗi Khởi Động Server", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // Refresh timer
            _refreshTimer = new Timer { Interval = 1000 };
            _refreshTimer.Tick += (s, e) => OnTimerTick();
            _refreshTimer.Start();

            UpdateTrayTooltip();
            UpdateAgentData();

            if (startMinimized)
            {
                this.WindowState = FormWindowState.Minimized;
                this.ShowInTaskbar = false;
                this.Opacity = 0;
            }
        }

        private Icon LoadAppIcon()
        {
            try
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico");
                if (File.Exists(iconPath))
                {
                    return new Icon(iconPath);
                }
                return Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch
            {
                // Fallback generated icon
                Bitmap bmp = new Bitmap(16, 16);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(ModernTheme.AccentBlue);
                    using (Font font = new Font("Segoe UI", 8, FontStyle.Bold))
                    using (Brush brush = new SolidBrush(Color.White))
                    {
                        g.DrawString("L", font, brush, 2, 0);
                    }
                }
                return Icon.FromHandle(bmp.GetHicon());
            }
        }

        private void InitializeComponent()
        {
            this.Text = string.Format("LAN Control Central Gateway - Máy Chủ Điều Phối Đa VLAN (Port: {0})", _port);
            this.Size = new Size(1000, 720);
            this.MinimumSize = new Size(880, 580);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = ModernTheme.BackgroundDark;
            this.ForeColor = ModernTheme.TextPrimary;

            // 1. Top Header Banner
            Panel topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 72,
                BackColor = ModernTheme.PanelDark,
                Padding = new Padding(18, 10, 18, 10)
            };

            Label lblTitle = new Label
            {
                Text = "🛡️ LAN CONTROL CENTRAL GATEWAY - MULTI-VLAN HUB",
                Font = ModernTheme.HeaderFont,
                ForeColor = ModernTheme.AccentCyan,
                AutoSize = true,
                Location = new Point(14, 12)
            };

            Label lblSubTitle = new Label
            {
                Text = "Máy chủ trung tâm định tuyến & chuyển tiếp kết nối ngược (Reverse Connection) giữa các phân đoạn mạng",
                Font = ModernTheme.RegularFont,
                ForeColor = ModernTheme.TextSecondary,
                AutoSize = true,
                Location = new Point(16, 38)
            };

            _lblStatusBadge = new Label
            {
                Text = string.Format("🟢 ĐANG HOẠT ĐỘNG (PORT {0})", _port),
                Font = ModernTheme.SubHeaderFont,
                ForeColor = ModernTheme.AccentGreen,
                BackColor = Color.FromArgb(20, 45, 30),
                Padding = new Padding(10, 6, 10, 6),
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(topPanel.Width - 280, 18)
            };

            topPanel.Controls.Add(lblTitle);
            topPanel.Controls.Add(lblSubTitle);
            topPanel.Controls.Add(_lblStatusBadge);
            this.Controls.Add(topPanel);

            // 2. Action Buttons Toolbar
            Panel toolBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = ModernTheme.CardDark,
                Padding = new Padding(14, 8, 14, 8)
            };

            _btnMinimizeToTray = new Button
            {
                Text = "📥 Thu Nhỏ Xuống Khay Hệ Thống",
                Location = new Point(14, 8),
                Size = new Size(240, 32)
            };
            ModernTheme.ApplyToButton(_btnMinimizeToTray, ModernTheme.AccentBlue, Color.White);
            _btnMinimizeToTray.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            _btnMinimizeToTray.Click += (s, e) => MinimizeToTray();
            toolBar.Controls.Add(_btnMinimizeToTray);

            _btnRestart = new Button
            {
                Text = "🔄 Khởi Động Lại Server",
                Location = new Point(262, 8),
                Size = new Size(170, 32)
            };
            ModernTheme.ApplyToButton(_btnRestart, ModernTheme.AccentYellow, Color.Black);
            _btnRestart.Click += (s, e) => RestartServer();
            toolBar.Controls.Add(_btnRestart);

            _btnCopyIp = new Button
            {
                Text = "📋 Sao Chép IP Máy Chủ",
                Location = new Point(440, 8),
                Size = new Size(170, 32)
            };
            ModernTheme.ApplyToButton(_btnCopyIp, ModernTheme.AccentCyan, Color.Black);
            _btnCopyIp.Click += (s, e) => CopyServerIp();
            toolBar.Controls.Add(_btnCopyIp);

            _btnLaunchControl = new Button
            {
                Text = "🖥️ Mở Master Control",
                Location = new Point(618, 8),
                Size = new Size(170, 32)
            };
            ModernTheme.ApplyToButton(_btnLaunchControl, ModernTheme.AccentGreen, Color.White);
            _btnLaunchControl.Click += (s, e) => LaunchMasterControl();
            toolBar.Controls.Add(_btnLaunchControl);

            this.Controls.Add(toolBar);

            // 3. Metrics Cards Panel
            Panel metricsPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 85,
                BackColor = ModernTheme.BackgroundDark,
                Padding = new Padding(14, 8, 14, 8)
            };

            int cardWidth = 230;
            int cardSpacing = 12;

            Panel card1 = CreateMetricCard("CỔNG LẮNG NGHE (PORT)", _port.ToString() + " TCP", ModernTheme.AccentCyan, 14 + 0 * (cardWidth + cardSpacing));
            _lblMetricPort = (Label)card1.Tag;
            metricsPanel.Controls.Add(card1);

            Panel card2 = CreateMetricCard("MÁY TRẠM ONLINE", "0 máy", ModernTheme.AccentGreen, 14 + 1 * (cardWidth + cardSpacing));
            _lblMetricAgents = (Label)card2.Tag;
            metricsPanel.Controls.Add(card2);

            Panel card3 = CreateMetricCard("PHÂN ĐOẠN VLAN", "0 VLAN", ModernTheme.AccentYellow, 14 + 2 * (cardWidth + cardSpacing));
            _lblMetricVlans = (Label)card3.Tag;
            metricsPanel.Controls.Add(card3);

            Panel card4 = CreateMetricCard("THỜI GIAN HOẠT ĐỘNG", "00:00:00", ModernTheme.TextPrimary, 14 + 3 * (cardWidth + cardSpacing));
            _lblMetricUptime = (Label)card4.Tag;
            metricsPanel.Controls.Add(card4);

            this.Controls.Add(metricsPanel);

            // 4. Tab Control for Main Content
            _tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = ModernTheme.SubHeaderFont
            };

            TabPage tabAgents = new TabPage("🖥️ Máy Trạm Trực Tuyến (Online Agents)");
            tabAgents.BackColor = ModernTheme.BackgroundDark;
            SetupAgentsTab(tabAgents);
            _tabControl.TabPages.Add(tabAgents);

            TabPage tabVlans = new TabPage("📊 Phân Nhóm Phân Đoạn (VLAN Stats)");
            tabVlans.BackColor = ModernTheme.BackgroundDark;
            SetupVlansTab(tabVlans);
            _tabControl.TabPages.Add(tabVlans);

            TabPage tabLogs = new TabPage("📜 Nhật Ký Hoạt Động (Live Server Logs)");
            tabLogs.BackColor = ModernTheme.BackgroundDark;
            SetupLogsTab(tabLogs);
            _tabControl.TabPages.Add(tabLogs);

            this.Controls.Add(_tabControl);

            // Bring panels to top z-order
            topPanel.BringToFront();
            toolBar.BringToFront();
            metricsPanel.BringToFront();
            _tabControl.BringToFront();
        }

        private Panel CreateMetricCard(string title, string initialValue, Color accentColor, int left)
        {
            Panel card = new Panel
            {
                Location = new Point(left, 8),
                Size = new Size(230, 68),
                BackColor = ModernTheme.CardDark,
                Padding = new Padding(12, 8, 12, 8)
            };

            Label lblTitle = new Label
            {
                Text = title,
                Font = ModernTheme.SmallFont,
                ForeColor = ModernTheme.TextSecondary,
                AutoSize = true,
                Location = new Point(10, 8)
            };

            Label lblValue = new Label
            {
                Text = initialValue,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = accentColor,
                AutoSize = true,
                Location = new Point(10, 28)
            };

            card.Controls.Add(lblTitle);
            card.Controls.Add(lblValue);
            card.Tag = lblValue;
            return card;
        }

        private void SetupAgentsTab(TabPage tab)
        {
            Panel searchBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = ModernTheme.PanelDark,
                Padding = new Padding(10, 7, 10, 7)
            };

            Label lblSearch = new Label
            {
                Text = "🔍 Tìm kiếm máy trạm:",
                Font = ModernTheme.RegularFont,
                ForeColor = ModernTheme.TextSecondary,
                Location = new Point(10, 12),
                AutoSize = true
            };
            searchBar.Controls.Add(lblSearch);

            _txtSearch = new TextBox
            {
                Location = new Point(155, 9),
                Width = 320,
                Font = ModernTheme.RegularFont,
                BackColor = ModernTheme.CardDark,
                ForeColor = ModernTheme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle
            };
            _txtSearch.TextChanged += (s, e) => FilterAgentGrid();
            searchBar.Controls.Add(_txtSearch);

            Button btnRefresh = new Button
            {
                Text = "🔄 Làm Mới",
                Location = new Point(485, 8),
                Size = new Size(100, 26)
            };
            ModernTheme.ApplyToButton(btnRefresh, ModernTheme.BorderDark, ModernTheme.TextPrimary);
            btnRefresh.Font = ModernTheme.RegularFont;
            btnRefresh.Click += (s, e) => UpdateAgentData();
            searchBar.Controls.Add(btnRefresh);

            tab.Controls.Add(searchBar);

            _gridAgents = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = ModernTheme.BackgroundDark,
                GridColor = ModernTheme.BorderDark,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EnableHeadersVisualStyles = false
            };

            _gridAgents.ColumnHeadersDefaultCellStyle.BackColor = ModernTheme.CardDark;
            _gridAgents.ColumnHeadersDefaultCellStyle.ForeColor = ModernTheme.TextPrimary;
            _gridAgents.ColumnHeadersDefaultCellStyle.Font = ModernTheme.SubHeaderFont;
            _gridAgents.ColumnHeadersHeight = 32;

            _gridAgents.DefaultCellStyle.BackColor = ModernTheme.PanelDark;
            _gridAgents.DefaultCellStyle.ForeColor = ModernTheme.TextPrimary;
            _gridAgents.DefaultCellStyle.Font = ModernTheme.RegularFont;
            _gridAgents.DefaultCellStyle.SelectionBackColor = ModernTheme.AccentBlue;
            _gridAgents.DefaultCellStyle.SelectionForeColor = Color.White;
            _gridAgents.RowTemplate.Height = 28;

            _gridAgents.Columns.Add("AgentId", "ID Máy");
            _gridAgents.Columns.Add("Hostname", "Tên Máy Trạm");
            _gridAgents.Columns.Add("IpAddress", "Địa Chỉ IP");
            _gridAgents.Columns.Add("VlanTag", "Phân Đoạn VLAN");
            _gridAgents.Columns.Add("Cpu", "CPU Hiện Tại");
            _gridAgents.Columns.Add("Ram", "RAM Khả Dụng");
            _gridAgents.Columns.Add("Status", "Trạng Thái");

            _gridAgents.Columns["AgentId"].FillWeight = 50;
            _gridAgents.Columns["Hostname"].FillWeight = 90;
            _gridAgents.Columns["IpAddress"].FillWeight = 80;
            _gridAgents.Columns["VlanTag"].FillWeight = 70;
            _gridAgents.Columns["Cpu"].FillWeight = 50;
            _gridAgents.Columns["Ram"].FillWeight = 60;
            _gridAgents.Columns["Status"].FillWeight = 65;

            tab.Controls.Add(_gridAgents);
            _gridAgents.BringToFront();
        }

        private void SetupVlansTab(TabPage tab)
        {
            _gridVlans = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = ModernTheme.BackgroundDark,
                GridColor = ModernTheme.BorderDark,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EnableHeadersVisualStyles = false
            };

            _gridVlans.ColumnHeadersDefaultCellStyle.BackColor = ModernTheme.CardDark;
            _gridVlans.ColumnHeadersDefaultCellStyle.ForeColor = ModernTheme.TextPrimary;
            _gridVlans.ColumnHeadersDefaultCellStyle.Font = ModernTheme.SubHeaderFont;
            _gridVlans.ColumnHeadersHeight = 32;

            _gridVlans.DefaultCellStyle.BackColor = ModernTheme.PanelDark;
            _gridVlans.DefaultCellStyle.ForeColor = ModernTheme.TextPrimary;
            _gridVlans.DefaultCellStyle.Font = ModernTheme.RegularFont;
            _gridVlans.DefaultCellStyle.SelectionBackColor = ModernTheme.AccentBlue;
            _gridVlans.RowTemplate.Height = 30;

            _gridVlans.Columns.Add("Vlan", "Nhãn Phân Đoạn (VLAN Tag)");
            _gridVlans.Columns.Add("Count", "Số Lượng Máy Trạm");
            _gridVlans.Columns.Add("Percent", "Tỷ Lệ %");

            tab.Controls.Add(_gridVlans);
        }

        private void SetupLogsTab(TabPage tab)
        {
            Panel logToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 38,
                BackColor = ModernTheme.PanelDark,
                Padding = new Padding(10, 5, 10, 5)
            };

            _chkAutoScroll = new CheckBox
            {
                Text = "Tự động cuộn theo log mới",
                Checked = true,
                ForeColor = ModernTheme.TextPrimary,
                Font = ModernTheme.RegularFont,
                Location = new Point(12, 8),
                AutoSize = true
            };
            logToolbar.Controls.Add(_chkAutoScroll);

            Button btnClear = new Button
            {
                Text = "🗑️ Xóa Nhật Ký",
                Location = new Point(230, 5),
                Size = new Size(115, 26)
            };
            ModernTheme.ApplyToButton(btnClear, ModernTheme.BorderDark, ModernTheme.TextPrimary);
            btnClear.Font = ModernTheme.RegularFont;
            btnClear.Click += (s, e) => _rtbLogs.Clear();
            logToolbar.Controls.Add(btnClear);

            tab.Controls.Add(logToolbar);

            _rtbLogs = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(14, 17, 22),
                ForeColor = ModernTheme.TextPrimary,
                Font = ModernTheme.CodeFont,
                BorderStyle = BorderStyle.None,
                ReadOnly = true
            };
            tab.Controls.Add(_rtbLogs);
            _rtbLogs.BringToFront();
        }

        private void SetupTrayContextMenu()
        {
            ContextMenu menu = new ContextMenu();
            MenuItem mTitle = new MenuItem(string.Format("🛡️ LAN Control Central Gateway (Cổng {0})", _port)) { Enabled = false };
            menu.MenuItems.Add(mTitle);
            menu.MenuItems.Add("-");

            menu.MenuItems.Add(new MenuItem("🖥️ Mở Cửa Sổ Gateway", (s, e) => RestoreFromTray()));
            menu.MenuItems.Add(new MenuItem("📥 Thu Nhỏ Xuống Khay", (s, e) => MinimizeToTray()));
            menu.MenuItems.Add(new MenuItem("🔄 Khởi Động Lại Server", (s, e) => RestartServer()));
            menu.MenuItems.Add(new MenuItem("ℹ️ Thông Tin Máy Chủ...", (s, e) => ShowServerInfo()));
            menu.MenuItems.Add("-");
            menu.MenuItems.Add(new MenuItem("❌ Dừng & Thoát Hoàn Toàn", (s, e) => ExitServerExplicitly()));

            _trayIcon.ContextMenu = menu;
        }

        public void MinimizeToTray()
        {
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Hide();

            if (_trayIcon != null)
            {
                _trayIcon.ShowBalloonTip(3000, "LAN Control Central Gateway",
                    string.Format("Máy chủ Central Gateway đang chạy ngầm trên cổng {0}.\nNhấp đúp chuột vào biểu tượng này để mở lại giao diện.", _port),
                    ToolTipIcon.Info);
            }
        }

        public void RestoreFromTray()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
            this.Opacity = 1;
            this.BringToFront();
            this.Activate();
        }

        private void UpdateTrayTooltip()
        {
            if (_trayIcon == null) return;
            try
            {
                int agentCount = _server != null ? _server.GetActiveAgents().Count : 0;
                string text = string.Format("LAN Control Gateway (Cổng {0})\nĐang chạy - {1} Agent", _port, agentCount);
                if (text.Length > 63)
                {
                    text = string.Format("LAN Control Gateway ({0})\n{1} Agents", _port, agentCount);
                }
                if (text.Length > 63)
                {
                    text = text.Substring(0, 63);
                }
                _trayIcon.Text = text;
            }
            catch { }
        }

        private void UpdateStatusBadge(bool running)
        {
            if (_lblStatusBadge == null) return;
            if (running)
            {
                _lblStatusBadge.Text = string.Format("🟢 ĐANG HOẠT ĐỘNG (PORT {0})", _port);
                _lblStatusBadge.ForeColor = ModernTheme.AccentGreen;
                _lblStatusBadge.BackColor = Color.FromArgb(20, 45, 30);
            }
            else
            {
                _lblStatusBadge.Text = "🔴 ĐÃ DỪNG HOẠT ĐỘNG";
                _lblStatusBadge.ForeColor = ModernTheme.AccentRed;
                _lblStatusBadge.BackColor = Color.FromArgb(50, 20, 20);
            }
        }

        private void UpdateAgentData()
        {
            if (_server == null) return;
            List<AgentInfo> agents = _server.GetActiveAgents();
            Dictionary<string, int> vlans = _server.GetVlanStatistics();

            // Update Metrics Cards
            if (_lblMetricAgents != null) _lblMetricAgents.Text = agents.Count + " máy";
            if (_lblMetricVlans != null) _lblMetricVlans.Text = vlans.Count + " VLAN";

            // Update Agent Grid
            string search = _txtSearch != null ? _txtSearch.Text.Trim().ToLowerInvariant() : "";
            _gridAgents.Rows.Clear();

            foreach (AgentInfo a in agents)
            {
                if (!string.IsNullOrEmpty(search))
                {
                    bool match = (a.Hostname != null && a.Hostname.ToLowerInvariant().Contains(search)) ||
                                 (a.IpAddress != null && a.IpAddress.ToLowerInvariant().Contains(search)) ||
                                 (a.VlanTag != null && a.VlanTag.ToLowerInvariant().Contains(search));
                    if (!match) continue;
                }

                string shortId = a.AgentId != null && a.AgentId.Length > 8 ? a.AgentId.Substring(0, 8) : (a.AgentId ?? "");
                string status = a.IsLocked ? "🔒 Đang khóa" : (a.IsStreaming ? "📺 Đang truyền" : "🟢 Hoạt động");

                _gridAgents.Rows.Add(
                    shortId,
                    a.Hostname ?? "N/A",
                    a.IpAddress ?? "N/A",
                    string.IsNullOrEmpty(a.VlanTag) ? "Default" : a.VlanTag,
                    a.CpuUsagePercent + "%",
                    a.AvailableMemoryMb + " MB",
                    status
                );
            }

            // Update VLAN Grid
            _gridVlans.Rows.Clear();
            int total = agents.Count;
            foreach (KeyValuePair<string, int> kv in vlans)
            {
                double pct = total > 0 ? (kv.Value * 100.0 / total) : 0;
                _gridVlans.Rows.Add(kv.Key, kv.Value + " máy", string.Format("{0:F1}%", pct));
            }

            UpdateTrayTooltip();
        }

        private void FilterAgentGrid()
        {
            UpdateAgentData();
        }

        private void OnTimerTick()
        {
            TimeSpan uptime = DateTime.Now - _startTime;
            if (_lblMetricUptime != null)
            {
                _lblMetricUptime.Text = string.Format("{0:D2}:{1:D2}:{2:D2}",
                    (int)uptime.TotalHours, uptime.Minutes, uptime.Seconds);
            }
        }

        private void OnServerLog(string logLine)
        {
            if (_rtbLogs == null || _rtbLogs.IsDisposed) return;

            if (_rtbLogs.InvokeRequired)
            {
                try { _rtbLogs.BeginInvoke(new Action<string>(OnServerLog), logLine); } catch { }
                return;
            }

            Color color = ModernTheme.TextPrimary;
            if (logLine.Contains("[INFO]")) color = ModernTheme.AccentCyan;
            else if (logLine.Contains("[SUCCESS]")) color = ModernTheme.AccentGreen;
            else if (logLine.Contains("[WARN]")) color = ModernTheme.AccentYellow;
            else if (logLine.Contains("[ERROR]")) color = ModernTheme.AccentRed;

            _rtbLogs.SelectionStart = _rtbLogs.TextLength;
            _rtbLogs.SelectionLength = 0;
            _rtbLogs.SelectionColor = color;
            _rtbLogs.AppendText(logLine + "\n");
            _rtbLogs.SelectionColor = _rtbLogs.ForeColor;

            if (_chkAutoScroll != null && _chkAutoScroll.Checked)
            {
                _rtbLogs.ScrollToCaret();
            }
        }

        private void RestartServer()
        {
            DialogResult dr = MessageBox.Show(
                "Bạn có chắc chắn muốn khởi động lại máy chủ Central Gateway không?\nCác kết nối máy trạm hiện tại sẽ tạm thời ngắt và tự động kết nối lại sau vài giây.",
                "Xác Nhận Khởi Động Lại", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (dr == DialogResult.Yes)
            {
                try
                {
                    _server.Stop();
                    System.Threading.Thread.Sleep(500);
                    _server.Start();
                    UpdateStatusBadge(true);
                    UpdateAgentData();
                    MessageBox.Show("Đã khởi động lại Central Gateway trên cổng " + _port + " thành công!",
                                    "Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi khởi động lại: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void CopyServerIp()
        {
            string ip = SystemInfoCollector.GetLocalIpAddress();
            try
            {
                Clipboard.SetText(ip);
                MessageBox.Show(string.Format("Đã sao chép địa chỉ IP máy chủ: {0}\nCổng Gateway: {1}\n\nBạn có thể dán IP này vào cấu hình máy trạm Agent.", ip, _port),
                                "Đã Sao Chép IP", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch
            {
                MessageBox.Show("Địa chỉ IP máy chủ Gateway là: " + ip, "Thông Tin IP", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void LaunchMasterControl()
        {
            string dir = AppDomain.CurrentDomain.BaseDirectory;
            string controlExe = Path.Combine(dir, "LanControl.Control.exe");
            if (!File.Exists(controlExe))
            {
                controlExe = Path.Combine(dir, "bin", "LanControl.Control.exe");
            }

            if (File.Exists(controlExe))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = controlExe,
                        WorkingDirectory = Path.GetDirectoryName(controlExe),
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Không thể khởi chạy Master Control: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Không tìm thấy LanControl.Control.exe trong cùng thư mục!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ShowServerInfo()
        {
            int agentCount = _server != null ? _server.GetActiveAgents().Count : 0;
            TimeSpan uptime = DateTime.Now - _startTime;

            string info = string.Format(
                "--- THÔNG TIN LAN CONTROL CENTRAL GATEWAY ---\n\n" +
                "• Trạng thái: Đang hoạt động\n" +
                "• Cổng lắng nghe (Port): {0} (TCP)\n" +
                "• Địa chỉ IP máy chủ: {1}\n" +
                "• Tổng số Agent online: {2} máy\n" +
                "• Thời gian hoạt động: {3:D2}:{4:D2}:{5:D2}\n" +
                "• Tác giả: Bùi Quang Sáng\n" +
                "• Đơn vị: Phòng CNTT, BV ĐK Châu Đốc",
                _port,
                SystemInfoCollector.GetLocalIpAddress(),
                agentCount,
                (int)uptime.TotalHours, uptime.Minutes, uptime.Seconds);

            MessageBox.Show(info, "Thông Tin Central Gateway", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ExitServerExplicitly()
        {
            DialogResult dr = MessageBox.Show(
                "Bạn có thực sự muốn DỪNG HOÀN TOÀN máy chủ Central Gateway không?\n\n" +
                "⚠️ Lưu ý: Khi dừng Central Gateway, tất cả máy trạm (Agent) và bàn quản trị (Master Control) sẽ mất kết nối!",
                "Cảnh Báo Dừng Máy Chủ", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (dr == DialogResult.Yes)
            {
                _isExplicitExit = true;
                _server.Stop();
                if (_trayIcon != null)
                {
                    _trayIcon.Visible = false;
                    _trayIcon.Dispose();
                }
                Application.Exit();
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (this.WindowState == FormWindowState.Minimized)
            {
                MinimizeToTray();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!_isExplicitExit && e.CloseReason == CloseReason.UserClosing)
            {
                DialogResult res = MessageBox.Show(
                    "Bạn có muốn thu nhỏ Central Gateway xuống khay hệ thống để tiếp tục phục vụ kết nối cho các máy trạm không?\n\n" +
                    "• Chọn [Yes / Có]: Thu nhỏ xuống khay hệ thống (Khuyên dùng - máy chủ tiếp tục chạy ngầm).\n" +
                    "• Chọn [No / Không]: Dừng máy chủ và thoát hoàn toàn chương trình.\n" +
                    "• Chọn [Cancel / Hủy]: Tiếp tục giữ cửa sổ mở.",
                    "Xác Nhận Đóng Cửa Sổ Central Gateway",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (res == DialogResult.Yes)
                {
                    e.Cancel = true;
                    MinimizeToTray();
                    return;
                }
                else if (res == DialogResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
                else
                {
                    _isExplicitExit = true;
                }
            }

            if (_refreshTimer != null)
            {
                _refreshTimer.Stop();
                _refreshTimer.Dispose();
            }

            if (_server != null)
            {
                _server.Stop();
            }

            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
            }

            base.OnFormClosing(e);
        }
    }
}
