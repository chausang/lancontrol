using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using LanControl.Core.Models;
using LanControl.Core.Utils;

namespace LanControl.Control.UI
{
    public class MainForm : Form
    {
        private readonly ControlClient _client;
        private List<AgentInfo> _agents = new List<AgentInfo>();
        private string _selectedVlan = "ALL";

        // UI Components
        private Panel _topRibbon;
        private TextBox _txtGatewayHost;
        private TextBox _txtGatewayPort;
        private Button _btnConnect;
        private Button _btnRefresh;
        private Button _btnBroadcast;
        private Button _btnLockAll;
        private Button _btnUnlockAll;
        private Button _btnMassScript;
        private Button _btnFileTransfer;
        private Button _btnHardwareAudit;
        private Button _btnScanner;
        private Button _btnSendMessage;
        private Button _btnRebootSelected;
        private Button _btnShutdownSelected;
        private Button _btnAbout;
        private Button _btnChangelog;
        private TextBox _txtSearch;

        private SplitContainer _splitMain;
        private TreeView _treeVlan;
        private TabControl _tabViews;
        private TabPage _tabMatrix;
        private TabPage _tabList;
        private ThumbnailMatrixPanel _matrixPanel;
        private DataGridView _gridAgents;

        private StatusStrip _statusStrip;
        private ToolStripStatusLabel _lblConnStatus;
        private ToolStripStatusLabel _lblTotalCount;
        private ToolStripStatusLabel _lblVlanInfo;
        private ToolStripStatusLabel _lblVersionInfo;
        private ToolStripStatusLabel _lblLog;

        public MainForm()
        {
            _client = new ControlClient();
            InitializeComponent();
            SetupEvents();
        }

        private void InitializeComponent()
        {
            this.Text = string.Format("LAN Control Manager {0} - Trạm Quản Trị Hệ Thống Đa VLAN (Multi-VLAN Master Console)", AppVersion.FullVersion);
            this.Size = new Size(1360, 850);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = ModernTheme.BackgroundDark;
            this.ForeColor = ModernTheme.TextPrimary;

            // 1. Top Ribbon Toolbar
            _topRibbon = new Panel
            {
                Dock = DockStyle.Top,
                Height = 84,
                BackColor = ModernTheme.PanelDark,
                Padding = new Padding(10, 6, 10, 6),
                AutoScroll = true
            };

            // Row 1: Connection & Search
            Label lblGw = new Label { Text = "Gateway Server:", Location = new Point(12, 12), AutoSize = true, Font = ModernTheme.RegularFont, ForeColor = ModernTheme.TextSecondary };
            _txtGatewayHost = new TextBox { Text = "127.0.0.1", Location = new Point(115, 9), Width = 110, BackColor = ModernTheme.CardDark, ForeColor = ModernTheme.TextPrimary, Font = ModernTheme.RegularFont, BorderStyle = BorderStyle.FixedSingle };
            _txtGatewayPort = new TextBox { Text = "8443", Location = new Point(230, 9), Width = 55, BackColor = ModernTheme.CardDark, ForeColor = ModernTheme.TextPrimary, Font = ModernTheme.RegularFont, BorderStyle = BorderStyle.FixedSingle };

            _btnConnect = new Button { Text = "🔌 Kết Nối", Location = new Point(292, 7), Size = new Size(95, 26) };
            ModernTheme.ApplyToButton(_btnConnect, ModernTheme.AccentGreen, Color.White);

            _btnRefresh = new Button { Text = "🔄 Làm Mới", Location = new Point(392, 7), Size = new Size(95, 26) };
            ModernTheme.ApplyToButton(_btnRefresh, ModernTheme.CardDark, ModernTheme.TextPrimary);

            Label lblSearch = new Label { Text = "🔍 Tìm máy/IP:", Location = new Point(510, 12), AutoSize = true, Font = ModernTheme.RegularFont, ForeColor = ModernTheme.TextSecondary };
            _txtSearch = new TextBox { Location = new Point(600, 9), Width = 180, BackColor = ModernTheme.CardDark, ForeColor = ModernTheme.TextPrimary, Font = ModernTheme.RegularFont, BorderStyle = BorderStyle.FixedSingle };
            _txtSearch.TextChanged += (s, e) => ApplyFilter();

            _btnAbout = new Button { Text = "ℹ️ Giới Thiệu (About)", Location = new Point(795, 7), Size = new Size(160, 26) };
            ModernTheme.ApplyToButton(_btnAbout, ModernTheme.CardDark, ModernTheme.AccentCyan);
            _btnAbout.Click += (s, e) => { using (AboutForm f = new AboutForm(false)) f.ShowDialog(this); };

            _btnChangelog = new Button { Text = string.Format("🕒 Phiên Bản {0} (Lịch Sử)", AppVersion.FullVersion), Location = new Point(961, 7), Size = new Size(195, 26) };
            ModernTheme.ApplyToButton(_btnChangelog, ModernTheme.CardDark, ModernTheme.AccentYellow);
            _btnChangelog.Click += (s, e) => { using (AboutForm f = new AboutForm(true)) f.ShowDialog(this); };

            // Row 2: Action Buttons
            _btnBroadcast = new Button { Text = "📡 Phát Sóng Màn Hình", Location = new Point(12, 42), Size = new Size(165, 32) };
            ModernTheme.ApplyToButton(_btnBroadcast, ModernTheme.AccentBlue, Color.White);

            _btnLockAll = new Button { Text = "🔒 Khóa Máy", Location = new Point(183, 42), Size = new Size(105, 32) };
            ModernTheme.ApplyToButton(_btnLockAll, ModernTheme.AccentRed, Color.White);

            _btnUnlockAll = new Button { Text = "🔓 Mở Khóa", Location = new Point(294, 42), Size = new Size(105, 32) };
            ModernTheme.ApplyToButton(_btnUnlockAll, ModernTheme.AccentGreen, Color.White);

            _btnFileTransfer = new Button { Text = "📁 Truyền Tập Tin", Location = new Point(405, 42), Size = new Size(140, 32) };
            ModernTheme.ApplyToButton(_btnFileTransfer, ModernTheme.AccentCyan, Color.FromArgb(20, 24, 32));

            _btnMassScript = new Button { Text = "⚡ Chạy Lệnh / Script", Location = new Point(551, 42), Size = new Size(150, 32) };
            ModernTheme.ApplyToButton(_btnMassScript, ModernTheme.AccentYellow, Color.FromArgb(20, 24, 32));

            _btnHardwareAudit = new Button { Text = "🔍 Cấu Hình & Bản Quyền", Location = new Point(707, 42), Size = new Size(175, 32) };
            ModernTheme.ApplyToButton(_btnHardwareAudit, ModernTheme.CardDark, ModernTheme.AccentCyan);

            _btnScanner = new Button { Text = "🔍 Quét Dãy IP", Location = new Point(888, 42), Size = new Size(125, 32) };
            ModernTheme.ApplyToButton(_btnScanner, ModernTheme.CardDark, ModernTheme.AccentCyan);

            _btnSendMessage = new Button { Text = "💬 Nhắn Tin", Location = new Point(1019, 42), Size = new Size(95, 32) };
            ModernTheme.ApplyToButton(_btnSendMessage, ModernTheme.CardDark, ModernTheme.TextPrimary);

            _btnRebootSelected = new Button { Text = "🔄 Khởi Động Lại", Location = new Point(1120, 42), Size = new Size(120, 32) };
            ModernTheme.ApplyToButton(_btnRebootSelected, ModernTheme.CardDark, ModernTheme.TextSecondary);

            _btnShutdownSelected = new Button { Text = "⛔ Tắt Máy", Location = new Point(1246, 42), Size = new Size(90, 32) };
            ModernTheme.ApplyToButton(_btnShutdownSelected, ModernTheme.CardDark, ModernTheme.AccentRed);

            _topRibbon.Controls.Add(lblGw);
            _topRibbon.Controls.Add(_txtGatewayHost);
            _topRibbon.Controls.Add(_txtGatewayPort);
            _topRibbon.Controls.Add(_btnConnect);
            _topRibbon.Controls.Add(_btnRefresh);
            _topRibbon.Controls.Add(lblSearch);
            _topRibbon.Controls.Add(_txtSearch);
            _topRibbon.Controls.Add(_btnAbout);
            _topRibbon.Controls.Add(_btnChangelog);
            _topRibbon.Controls.Add(_btnBroadcast);
            _topRibbon.Controls.Add(_btnLockAll);
            _topRibbon.Controls.Add(_btnUnlockAll);
            _topRibbon.Controls.Add(_btnFileTransfer);
            _topRibbon.Controls.Add(_btnMassScript);
            _topRibbon.Controls.Add(_btnHardwareAudit);
            _topRibbon.Controls.Add(_btnScanner);
            _topRibbon.Controls.Add(_btnSendMessage);
            _topRibbon.Controls.Add(_btnRebootSelected);
            _topRibbon.Controls.Add(_btnShutdownSelected);

            // 2. Main SplitContainer (Left TreeView, Right Views)
            _splitMain = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                BackColor = ModernTheme.BorderDark
            };

            // Left TreeView
            Panel leftPanel = new Panel { Dock = DockStyle.Fill, BackColor = ModernTheme.PanelDark, Padding = new Padding(8) };
            Label lblTreeTitle = new Label { Text = "🌐 PHÂN ĐOẠN & DANH SÁCH MÁY", Dock = DockStyle.Top, Height = 26, Font = ModernTheme.SubHeaderFont, ForeColor = ModernTheme.AccentCyan };
            _treeVlan = new TreeView
            {
                Dock = DockStyle.Fill,
                BackColor = ModernTheme.CardDark,
                ForeColor = ModernTheme.TextPrimary,
                Font = ModernTheme.RegularFont,
                BorderStyle = BorderStyle.None,
                FullRowSelect = true,
                ShowLines = true,
                ShowPlusMinus = true,
                ItemHeight = 22,
                ShowNodeToolTips = true
            };
            _treeVlan.AfterSelect += OnTreeVlanSelected;
            _treeVlan.NodeMouseDoubleClick += OnTreeVlanDoubleClick;
            _treeVlan.NodeMouseClick += OnTreeVlanMouseClick;
            leftPanel.Controls.Add(_treeVlan);
            leftPanel.Controls.Add(lblTreeTitle);
            _splitMain.Panel1.Controls.Add(leftPanel);

            // Right Tab Views
            _tabViews = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = ModernTheme.SubHeaderFont
            };

            _tabMatrix = new TabPage("🖥️ Lưới Màn Hình Xem Trước (Thumbnail Matrix)");
            _tabMatrix.BackColor = ModernTheme.BackgroundDark;
            _matrixPanel = new ThumbnailMatrixPanel { Dock = DockStyle.Fill };
            _tabMatrix.Controls.Add(_matrixPanel);

            _tabList = new TabPage("📋 Danh Sách Chi Tiết Thiết Bị (Device List)");
            _tabList.BackColor = ModernTheme.BackgroundDark;
            _gridAgents = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = ModernTheme.CardDark,
                ForeColor = ModernTheme.TextPrimary,
                GridColor = ModernTheme.BorderDark,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = ModernTheme.CardDark,
                    ForeColor = ModernTheme.TextPrimary,
                    SelectionBackColor = ModernTheme.AccentBlue,
                    SelectionForeColor = Color.White,
                    Font = ModernTheme.RegularFont
                },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = ModernTheme.PanelDark,
                    ForeColor = ModernTheme.AccentCyan,
                    Font = ModernTheme.SubHeaderFont
                },
                EnableHeadersVisualStyles = false
            };
            _gridAgents.Columns.Add("Hostname", "Tên Máy Trạm");
            _gridAgents.Columns.Add("IpAddress", "Địa Chỉ IP");
            _gridAgents.Columns.Add("VlanTag", "Phân Đoạn VLAN");
            _gridAgents.Columns.Add("Cpu", "CPU Tải (%)");
            _gridAgents.Columns.Add("Ram", "RAM Khả Dụng");
            _gridAgents.Columns.Add("User", "Người Dùng");
            _gridAgents.Columns.Add("Status", "Trạng Thái");
            _gridAgents.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.RowIndex < _gridAgents.Rows.Count)
                {
                    string host = _gridAgents.Rows[e.RowIndex].Cells[0].Value.ToString();
                    AgentInfo a = _agents.Find(ag => ag.Hostname == host);
                    if (a != null) OpenRemoteDesktop(a);
                }
            };
            _gridAgents.CellMouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Right && e.RowIndex >= 0 && e.RowIndex < _gridAgents.Rows.Count)
                {
                    _gridAgents.ClearSelection();
                    _gridAgents.Rows[e.RowIndex].Selected = true;
                    string host = _gridAgents.Rows[e.RowIndex].Cells[0].Value.ToString();
                    AgentInfo a = _agents.Find(ag => ag.Hostname == host);
                    if (a != null)
                    {
                        Point mousePos = _gridAgents.PointToClient(Cursor.Position);
                        ShowAgentContextMenu(a, _gridAgents, mousePos);
                    }
                }
            };
            _tabList.Controls.Add(_gridAgents);

            _tabViews.TabPages.Add(_tabMatrix);
            _tabViews.TabPages.Add(_tabList);
            _splitMain.Panel2.Controls.Add(_tabViews);

            // 3. Bottom Status Strip
            _statusStrip = new StatusStrip { BackColor = ModernTheme.PanelDark, ForeColor = ModernTheme.TextSecondary };
            _lblConnStatus = new ToolStripStatusLabel { Text = "🔴 Mất kết nối Gateway", ForeColor = ModernTheme.AccentRed, Font = ModernTheme.SubHeaderFont };
            _lblTotalCount = new ToolStripStatusLabel { Text = "| Máy trạm Online: 0", ForeColor = ModernTheme.TextPrimary };
            _lblVlanInfo = new ToolStripStatusLabel { Text = "| Đang lọc: Tất cả", ForeColor = ModernTheme.AccentCyan };
            _lblVersionInfo = new ToolStripStatusLabel { Text = string.Format("| 🏷️ Phiên bản: {0} ({1})", AppVersion.FullVersion, AppVersion.ReleaseDate), ForeColor = ModernTheme.AccentYellow, IsLink = false };
            _lblVersionInfo.Click += (s, e) => { using (AboutForm f = new AboutForm(true)) f.ShowDialog(this); };
            _lblLog = new ToolStripStatusLabel { Text = "| Sẵn sàng.", Spring = true, TextAlign = ContentAlignment.MiddleRight, ForeColor = ModernTheme.TextMuted };

            _statusStrip.Items.Add(_lblConnStatus);
            _statusStrip.Items.Add(_lblTotalCount);
            _statusStrip.Items.Add(_lblVlanInfo);
            _statusStrip.Items.Add(_lblVersionInfo);
            _statusStrip.Items.Add(_lblLog);

            this.Controls.Add(_splitMain);
            this.Controls.Add(_statusStrip);
            this.Controls.Add(_topRibbon);
        }

        private void SetupEvents()
        {
            this.Load += (s, e) =>
            {
                try
                {
                    if (_splitMain.Width > 500)
                        _splitMain.SplitterDistance = Math.Min(330, _splitMain.Width - 100);
                }
                catch { }
            };

            _btnConnect.Click += OnConnectClicked;
            _btnRefresh.Click += (s, e) => _client.Connect(_txtGatewayHost.Text.Trim(), int.Parse(_txtGatewayPort.Text.Trim()));
            _btnBroadcast.Click += (s, e) => OpenBroadcastSender();
            _btnLockAll.Click += OnLockSelectedClicked;
            _btnUnlockAll.Click += OnUnlockSelectedClicked;
            _btnFileTransfer.Click += OnFileTransferClicked;
            _btnMassScript.Click += OnMassScriptClicked;
            _btnHardwareAudit.Click += OnHardwareAuditClicked;
            _btnScanner.Click += (s, e) => { using (SubnetScannerForm f = new SubnetScannerForm(_client)) f.ShowDialog(this); };
            _btnSendMessage.Click += OnSendMessageClicked;
            _btnRebootSelected.Click += OnRebootSelectedClicked;
            _btnShutdownSelected.Click += OnShutdownSelectedClicked;

            // Thumbnail panel events
            _matrixPanel.RemoteDesktopRequested += OpenRemoteDesktop;
            _matrixPanel.HardwareInventoryRequested += (a) => OpenHardwareInventory(new List<AgentInfo> { a });
            _matrixPanel.FileManagerRequested += (a) => OpenRemoteFileManager(a);
            _matrixPanel.FileDistributionRequested += (a) => OpenFileDistribution(new List<AgentInfo> { a });
            _matrixPanel.LockScreenRequested += (a) => _client.SendCommand(a.AgentId, CommandType.LockScreen, "Máy tính đang được quản trị viên khóa.");
            _matrixPanel.UnlockScreenRequested += (a) => _client.SendCommand(a.AgentId, CommandType.UnlockScreen);
            _matrixPanel.RunScriptRequested += (a) => { using (RemoteScriptForm f = new RemoteScriptForm(_client, new List<AgentInfo> { a }, "", _matrixPanel.GetAllAgents())) f.ShowDialog(this); };
            _matrixPanel.ProcessManagerRequested += (a) => { using (ProcessManagerForm f = new ProcessManagerForm(_client, a)) f.ShowDialog(this); };
            _matrixPanel.SendMessageRequested += (a) => { using (SendMessageForm f = new SendMessageForm(_client, a)) f.ShowDialog(this); };
            _matrixPanel.RebootRequested += (a) =>
            {
                if (MessageBox.Show("Khởi động lại máy: " + a.Hostname + "?", "Xác nhận", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    _client.SendCommand(a.AgentId, CommandType.Reboot);
            };
            _matrixPanel.ShutdownRequested += (a) =>
            {
                if (MessageBox.Show("Tắt máy: " + a.Hostname + "?", "Xác nhận", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    _client.SendCommand(a.AgentId, CommandType.Shutdown);
            };

            // Client network events
            _client.ConnectionStatusChanged += OnClientConnectionChanged;
            _client.AgentListUpdated += OnAgentListUpdated;
            _client.HeartbeatReceived += OnHeartbeatReceived;
            _client.ScreenFrameReceived += OnScreenFrameReceived;
            _client.HelpRequestReceived += OnHelpRequestReceived;

            this.Load += (s, e) =>
            {
                // Auto-connect to local gateway on launch
                _client.Connect(_txtGatewayHost.Text.Trim(), int.Parse(_txtGatewayPort.Text.Trim()));
            };
        }

        private void OnConnectClicked(object sender, EventArgs e)
        {
            if (_client.IsConnected)
            {
                _client.Disconnect();
            }
            else
            {
                int port;
                if (int.TryParse(_txtGatewayPort.Text.Trim(), out port))
                {
                    _client.Connect(_txtGatewayHost.Text.Trim(), port);
                }
            }
        }

        private void OnClientConnectionChanged(bool connected)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action<bool>(OnClientConnectionChanged), connected);
                return;
            }

            if (connected)
            {
                _lblConnStatus.Text = "🟢 ĐÃ KẾT NỐI GATEWAY";
                _lblConnStatus.ForeColor = ModernTheme.AccentGreen;
                _btnConnect.Text = "Ngắt Kết Nối";
                ModernTheme.ApplyToButton(_btnConnect, ModernTheme.AccentRed, Color.White);
                _lblLog.Text = "| Đã kết nối Gateway " + _client.Host + ":" + _client.Port;
            }
            else
            {
                _lblConnStatus.Text = "🔴 MẤT KẾT NỐI GATEWAY";
                _lblConnStatus.ForeColor = ModernTheme.AccentRed;
                _btnConnect.Text = "🔌 Kết Nối";
                ModernTheme.ApplyToButton(_btnConnect, ModernTheme.AccentGreen, Color.White);
                _lblLog.Text = "| Chưa kết nối tới Gateway.";
            }
        }

        private void OnAgentListUpdated(List<AgentInfo> agents)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action<List<AgentInfo>>(OnAgentListUpdated), agents);
                return;
            }

            _agents = agents ?? new List<AgentInfo>();
            _lblTotalCount.Text = string.Format("| Máy trạm Online: {0}", _agents.Count);

            // Update VLAN Tree
            UpdateVlanTree();

            // Update Matrix Panel
            _matrixPanel.UpdateAgents(_agents);

            // Update Table Grid
            UpdateTableGrid();
        }

        private void OnHeartbeatReceived(AgentInfo agent)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action<AgentInfo>(OnHeartbeatReceived), agent);
                return;
            }

            _matrixPanel.UpdateHeartbeat(agent);

            // Update row in grid
            foreach (DataGridViewRow row in _gridAgents.Rows)
            {
                if (row.Cells[0].Value != null && row.Cells[0].Value.ToString() == agent.Hostname)
                {
                    row.Cells[3].Value = agent.CpuUsagePercent + "%";
                    row.Cells[4].Value = agent.AvailableMemoryMb + " MB";
                    row.Cells[6].Value = agent.IsLocked ? "Locked" : (agent.IsStreaming ? "Streaming" : "Online");
                    break;
                }
            }
        }

        private void OnScreenFrameReceived(ScreenFrameMeta meta, byte[] rawBytes)
        {
            if (meta != null && meta.IsThumbnail)
            {
                _matrixPanel.UpdateThumbnail(meta.AgentId, rawBytes);
            }
        }

        private void UpdateVlanTree()
        {
            _treeVlan.BeginUpdate();
            _treeVlan.Nodes.Clear();

            TreeNode rootNode = new TreeNode(string.Format("🌐 Tất cả máy trạm ({0})", _agents.Count)) { Tag = "ALL" };

            Dictionary<string, List<AgentInfo>> vlanGroups = new Dictionary<string, List<AgentInfo>>();
            foreach (AgentInfo a in _agents)
            {
                string vlan = string.IsNullOrEmpty(a.VlanTag) ? "Default" : a.VlanTag;
                if (!vlanGroups.ContainsKey(vlan)) vlanGroups[vlan] = new List<AgentInfo>();
                vlanGroups[vlan].Add(a);
            }

            int port = _client.Port > 0 ? _client.Port : 8443;

            foreach (KeyValuePair<string, List<AgentInfo>> kv in vlanGroups)
            {
                TreeNode vlanNode = new TreeNode(string.Format("📁 {0} ({1})", kv.Key, kv.Value.Count)) { Tag = kv.Key };
                foreach (AgentInfo a in kv.Value)
                {
                    // Format: Tên máy - Địa chỉ IP:port - Vlan
                    string label = string.Format("🖥️ {0} - {1}:{2} - {3}", a.Hostname, a.IpAddress, port, a.VlanTag);
                    TreeNode agentNode = new TreeNode(label)
                    {
                        Tag = a,
                        ToolTipText = string.Format("Tên máy: {0}\nIP: {1}:{2}\nVLAN: {3}\nNgười dùng: {4}\nTrạng thái: {5}",
                            a.Hostname, a.IpAddress, port, a.VlanTag, a.UserName,
                            a.IsLocked ? "Đang khóa" : (a.IsStreaming ? "Đang điều khiển" : "Online"))
                    };

                    if (a.IsLocked) agentNode.ForeColor = ModernTheme.AccentRed;
                    else if (a.IsStreaming) agentNode.ForeColor = ModernTheme.AccentBlue;
                    else agentNode.ForeColor = ModernTheme.AccentGreen;

                    vlanNode.Nodes.Add(agentNode);
                }
                rootNode.Nodes.Add(vlanNode);
            }

            _treeVlan.Nodes.Add(rootNode);
            rootNode.Expand();
            foreach (TreeNode vn in rootNode.Nodes)
            {
                vn.Expand();
            }
            _treeVlan.EndUpdate();
        }

        private void OnTreeVlanSelected(object sender, TreeViewEventArgs e)
        {
            if (e.Node != null && e.Node.Tag != null)
            {
                if (e.Node.Tag is AgentInfo)
                {
                    AgentInfo a = (AgentInfo)e.Node.Tag;
                    _selectedVlan = string.IsNullOrEmpty(a.VlanTag) ? "Default" : a.VlanTag;
                    _lblVlanInfo.Text = string.Format("| Máy: {0} ({1})", a.Hostname, a.VlanTag);
                }
                else
                {
                    _selectedVlan = e.Node.Tag.ToString();
                    _lblVlanInfo.Text = "| Đang lọc: " + (_selectedVlan == "ALL" ? "Tất cả máy" : _selectedVlan);
                }
                ApplyFilter();
            }
        }

        private void OnTreeVlanDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node != null && e.Node.Tag is AgentInfo)
            {
                AgentInfo a = (AgentInfo)e.Node.Tag;
                OpenRemoteDesktop(a);
            }
        }

        private void OnTreeVlanMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Right && e.Node != null)
            {
                _treeVlan.SelectedNode = e.Node;
                if (e.Node.Tag is AgentInfo)
                {
                    AgentInfo a = (AgentInfo)e.Node.Tag;
                    ShowAgentContextMenu(a, _treeVlan, e.Location);
                }
            }
        }

        private void ApplyFilter()
        {
            string search = _txtSearch.Text.Trim();
            _matrixPanel.SetFilter(_selectedVlan, search);
            UpdateTableGrid();
        }

        private void UpdateTableGrid()
        {
            _gridAgents.Rows.Clear();
            string search = _txtSearch.Text.Trim().ToLowerInvariant();

            foreach (AgentInfo a in _agents)
            {
                bool vlanMatch = (_selectedVlan == "ALL") ||
                    (!string.IsNullOrEmpty(a.VlanTag) && a.VlanTag.Equals(_selectedVlan, StringComparison.OrdinalIgnoreCase));

                bool searchMatch = string.IsNullOrEmpty(search) ||
                    (a.Hostname != null && a.Hostname.ToLowerInvariant().Contains(search)) ||
                    (a.IpAddress != null && a.IpAddress.Contains(search));

                if (vlanMatch && searchMatch)
                {
                    _gridAgents.Rows.Add(
                        a.Hostname,
                        a.IpAddress,
                        a.VlanTag,
                        a.CpuUsagePercent + "%",
                        a.AvailableMemoryMb + " MB",
                        a.UserName,
                        a.IsLocked ? "Locked 🔒" : (a.IsStreaming ? "Streaming 🖥️" : "Online 🟢")
                    );
                }
            }
        }

        private void OpenRemoteDesktop(AgentInfo agent)
        {
            RemoteDesktopForm rdp = new RemoteDesktopForm(_client, agent);
            rdp.Show();
        }

        private void ShowAgentContextMenu(AgentInfo agent, System.Windows.Forms.Control control, Point pos)
        {
            if (agent == null) return;

            ContextMenuStrip menu = new ContextMenuStrip();

            ToolStripMenuItem miRdp = new ToolStripMenuItem("🖥️ Điều Khiển Máy (Remote Desktop)", null, (s, e) => OpenRemoteDesktop(agent));
            miRdp.Font = new Font(miRdp.Font, FontStyle.Bold);
            menu.Items.Add(miRdp);

            menu.Items.Add("🔍 Xem Cấu Hình & Bản Quyền Phần Mềm...", null, (s, e) => OpenHardwareInventory(new List<AgentInfo> { agent }));

            menu.Items.Add(new ToolStripSeparator());

            menu.Items.Add("📁 Quản Lý Tệp Từ Xa (Remote File Explorer)...", null, (s, e) => OpenRemoteFileManager(agent));
            menu.Items.Add("🚀 Truyền Tập Tin Cho Máy Này (Send Files)...", null, (s, e) => OpenFileDistribution(new List<AgentInfo> { agent }));

            menu.Items.Add(new ToolStripSeparator());

            menu.Items.Add("🔒 Khóa Màn Hình (Lock Screen)", null, (s, e) => _client.SendCommand(agent.AgentId, CommandType.LockScreen, "Máy tính đang được quản trị viên khóa."));
            menu.Items.Add("🔓 Mở Khóa Màn Hình (Unlock Screen)", null, (s, e) => _client.SendCommand(agent.AgentId, CommandType.UnlockScreen));

            menu.Items.Add(new ToolStripSeparator());

            menu.Items.Add("⚡ Thực Thi Lệnh / Script (Run CMD/PS)...", null, (s, e) => { using (RemoteScriptForm f = new RemoteScriptForm(_client, new List<AgentInfo> { agent }, "", _matrixPanel.GetAllAgents())) f.ShowDialog(this); });
            menu.Items.Add("📋 Quản Lý Tác Vụ (Process Manager)...", null, (s, e) => { using (ProcessManagerForm f = new ProcessManagerForm(_client, agent)) f.ShowDialog(this); });
            menu.Items.Add("💬 Gửi Thông Báo (Send Message)...", null, (s, e) => { using (SendMessageForm f = new SendMessageForm(_client, agent)) f.ShowDialog(this); });

            menu.Items.Add(new ToolStripSeparator());

            menu.Items.Add("🖼️ Ẩn Hình Nền (Tăng tốc)", null, (s, e) => _client.SendCommand(agent.AgentId, CommandType.HideWallpaper));
            menu.Items.Add("🖼️ Khôi Phục Hình Nền", null, (s, e) => _client.SendCommand(agent.AgentId, CommandType.RestoreWallpaper));

            menu.Items.Add(new ToolStripSeparator());

            menu.Items.Add("🔄 Khởi Động Lại (Reboot)", null, (s, e) =>
            {
                if (MessageBox.Show("Khởi động lại máy: " + agent.Hostname + "?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    _client.SendCommand(agent.AgentId, CommandType.Reboot);
            });

            menu.Items.Add("⛔ Tắt Máy (Shutdown)", null, (s, e) =>
            {
                if (MessageBox.Show("Tắt máy: " + agent.Hostname + "?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    _client.SendCommand(agent.AgentId, CommandType.Shutdown);
            });

            menu.Show(control, pos);
        }

        private void OnHelpRequestReceived(HelpRequest req)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action<HelpRequest>(OnHelpRequestReceived), req);
                return;
            }

            using (HelpRequestPromptForm prompt = new HelpRequestPromptForm(req))
            {
                DialogResult res = prompt.ShowDialog(this);
                if (res == DialogResult.OK)
                {
                    // Send Accepted response
                    _client.SendHelpResponse(new HelpResponse
                    {
                        RequestId = req.RequestId,
                        AgentId = req.AgentId,
                        Accepted = true,
                        Message = "Quản trị viên đã chấp nhận yêu cầu."
                    });

                    // Open Remote Desktop immediately!
                    AgentInfo agent = _agents.Find(a => a.AgentId == req.AgentId);
                    if (agent == null)
                    {
                        agent = new AgentInfo
                        {
                            AgentId = req.AgentId,
                            Hostname = req.Hostname,
                            IpAddress = req.IpAddress,
                            VlanTag = req.VlanTag
                        };
                    }
                    OpenRemoteDesktop(agent);
                }
                else
                {
                    // Send Declined response
                    _client.SendHelpResponse(new HelpResponse
                    {
                        RequestId = req.RequestId,
                        AgentId = req.AgentId,
                        Accepted = false,
                        Message = "Quản trị viên hiện đang bận hoặc đã từ chối yêu cầu."
                    });
                }
            }
        }

        private void OpenBroadcastSender()
        {
            BroadcastSenderForm bcast = new BroadcastSenderForm(_client, _agents);
            bcast.ShowDialog(this);
        }

        private void OnLockSelectedClicked(object sender, EventArgs e)
        {
            List<AgentInfo> selected = _matrixPanel.GetSelectedAgents();
            if (selected.Count == 0)
            {
                // Fallback to locking current VLAN or prompt
                if (MessageBox.Show(string.Format("Chưa chọn máy nào. Bạn có muốn khóa TOÀN BỘ máy trong {0}?", _selectedVlan == "ALL" ? "toàn bộ hệ thống" : _selectedVlan),
                    "Khóa Màn Hình Hàng Loạt", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    _client.SendCommand("", CommandType.LockScreen, "Máy tính đang được quản trị viên khóa.", _selectedVlan);
                }
                return;
            }

            foreach (AgentInfo a in selected)
            {
                _client.SendCommand(a.AgentId, CommandType.LockScreen, "Máy tính đang được quản trị viên khóa.");
            }
        }

        private void OnUnlockSelectedClicked(object sender, EventArgs e)
        {
            List<AgentInfo> selected = _matrixPanel.GetSelectedAgents();
            if (selected.Count == 0)
            {
                _client.SendCommand("", CommandType.UnlockScreen, "", _selectedVlan);
                return;
            }

            foreach (AgentInfo a in selected)
            {
                _client.SendCommand(a.AgentId, CommandType.UnlockScreen);
            }
        }

        private void OnMassScriptClicked(object sender, EventArgs e)
        {
            List<AgentInfo> selected = _matrixPanel.GetSelectedAgents();
            List<AgentInfo> all = _matrixPanel.GetAllAgents();
            using (RemoteScriptForm f = new RemoteScriptForm(_client, selected, _selectedVlan, all))
            {
                f.ShowDialog(this);
            }
        }

        private void OnSendMessageClicked(object sender, EventArgs e)
        {
            using (SendMessageForm f = new SendMessageForm(_client, null, _selectedVlan))
            {
                f.ShowDialog(this);
            }
        }

        private void OnRebootSelectedClicked(object sender, EventArgs e)
        {
            List<AgentInfo> selected = _matrixPanel.GetSelectedAgents();
            if (selected.Count == 0)
            {
                MessageBox.Show("Vui lòng tích chọn các máy cần khởi động lại trên lưới màn hình!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show(string.Format("Bạn có chắc muốn KHỞI ĐỘNG LẠI {0} máy đã chọn?", selected.Count),
                "Xác nhận khởi động lại", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                foreach (AgentInfo a in selected)
                {
                    _client.SendCommand(a.AgentId, CommandType.Reboot);
                }
            }
        }

        private void OnShutdownSelectedClicked(object sender, EventArgs e)
        {
            List<AgentInfo> selected = _matrixPanel.GetSelectedAgents();
            if (selected.Count == 0)
            {
                MessageBox.Show("Vui lòng tích chọn các máy cần tắt trên lưới màn hình!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show(string.Format("Bạn có chắc muốn TẮT {0} máy đã chọn?", selected.Count),
                "Xác nhận tắt máy", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                foreach (AgentInfo a in selected)
                {
                    _client.SendCommand(a.AgentId, CommandType.Shutdown);
                }
            }
        }

        private void OnHardwareAuditClicked(object sender, EventArgs e)
        {
            List<AgentInfo> selected = _matrixPanel.GetSelectedAgents();
            if (selected.Count == 0)
            {
                // Check grid selection
                if (_tabViews.SelectedTab == _tabList && _gridAgents.CurrentRow != null && _gridAgents.CurrentRow.Index >= 0)
                {
                    string host = _gridAgents.CurrentRow.Cells[0].Value as string;
                    AgentInfo a = _agents.Find(x => x.Hostname == host);
                    if (a != null) selected.Add(a);
                }
                // Check tree selection
                if (selected.Count == 0 && _treeVlan.SelectedNode != null)
                {
                    AgentInfo treeAgent = _treeVlan.SelectedNode.Tag as AgentInfo;
                    if (treeAgent != null)
                    {
                        selected.Add(treeAgent);
                    }
                }
            }

            if (selected.Count == 0)
            {
                // If nothing selected, offer to query all currently filtered or all online agents
                List<AgentInfo> targetAgents = new List<AgentInfo>();
                string search = _txtSearch.Text.Trim().ToLowerInvariant();
                foreach (AgentInfo a in _agents)
                {
                    bool vlanMatch = (_selectedVlan == "ALL") ||
                        (!string.IsNullOrEmpty(a.VlanTag) && a.VlanTag.Equals(_selectedVlan, StringComparison.OrdinalIgnoreCase));
                    bool searchMatch = string.IsNullOrEmpty(search) ||
                        (a.Hostname != null && a.Hostname.ToLowerInvariant().Contains(search)) ||
                        (a.IpAddress != null && a.IpAddress.Contains(search));
                    if (vlanMatch && searchMatch) targetAgents.Add(a);
                }

                if (targetAgents.Count == 0)
                {
                    MessageBox.Show("Hiện không có máy trạm nào đang online để kiểm tra cấu hình.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (targetAgents.Count > 1)
                {
                    DialogResult dr = MessageBox.Show(string.Format("Bạn chưa tích chọn máy cụ thể. Bạn có muốn kiểm tra cấu hình toàn bộ {0} máy trạm đang hiển thị không?", targetAgents.Count),
                        "Truy xuất cấu hình hàng loạt", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (dr != DialogResult.Yes) return;
                }

                selected = targetAgents;
            }

            OpenHardwareInventory(selected);
        }

        private void OpenHardwareInventory(List<AgentInfo> agents)
        {
            if (agents == null || agents.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn ít nhất một máy trạm để kiểm tra cấu hình!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            HardwareInventoryForm form = new HardwareInventoryForm(_client, agents);
            form.Show();
        }

        private void OnFileTransferClicked(object sender, EventArgs e)
        {
            List<AgentInfo> selected = _matrixPanel.GetSelectedAgents();
            if (selected.Count == 0)
            {
                // Check grid selection
                if (_tabViews.SelectedTab == _tabList && _gridAgents.CurrentRow != null && _gridAgents.CurrentRow.Index >= 0)
                {
                    string host = _gridAgents.CurrentRow.Cells[0].Value as string;
                    AgentInfo a = _agents.Find(x => x.Hostname == host);
                    if (a != null) selected.Add(a);
                }
                // Check tree selection
                if (selected.Count == 0 && _treeVlan.SelectedNode != null)
                {
                    AgentInfo treeAgent = _treeVlan.SelectedNode.Tag as AgentInfo;
                    if (treeAgent != null)
                    {
                        selected.Add(treeAgent);
                    }
                }
            }

            if (selected.Count == 0)
            {
                // If nothing selected, offer to query all currently filtered or all online agents
                List<AgentInfo> targetAgents = new List<AgentInfo>();
                string search = _txtSearch.Text.Trim().ToLowerInvariant();
                foreach (AgentInfo a in _agents)
                {
                    bool vlanMatch = (_selectedVlan == "ALL") ||
                        (!string.IsNullOrEmpty(a.VlanTag) && a.VlanTag.Equals(_selectedVlan, StringComparison.OrdinalIgnoreCase));
                    bool searchMatch = string.IsNullOrEmpty(search) ||
                        (a.Hostname != null && a.Hostname.ToLowerInvariant().Contains(search)) ||
                        (a.IpAddress != null && a.IpAddress.Contains(search));
                    if (vlanMatch && searchMatch) targetAgents.Add(a);
                }

                if (targetAgents.Count == 0)
                {
                    MessageBox.Show("Hiện không có máy trạm nào đang online để truyền tập tin.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (targetAgents.Count > 1)
                {
                    DialogResult dr = MessageBox.Show(string.Format("Bạn chưa tích chọn máy cụ thể. Bạn có muốn mở giao diện truyền tập tin tới toàn bộ {0} máy trạm đang hiển thị không?", targetAgents.Count),
                        "Truyền tập tin hàng loạt", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (dr != DialogResult.Yes) return;
                }

                selected = targetAgents;
            }

            OpenFileDistribution(selected);
        }

        private void OpenFileDistribution(List<AgentInfo> agents)
        {
            if (agents == null || agents.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn ít nhất một máy trạm để truyền tập tin!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            FileDistributionForm form = new FileDistributionForm(_client, agents);
            form.Show();
        }

        private void OpenRemoteFileManager(AgentInfo agent)
        {
            if (agent == null) return;
            RemoteFileManagerForm form = new RemoteFileManagerForm(_client, agent);
            form.Show();
        }
    }
}
