using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using LanControl.Core.Models;
using LanControl.Core.Network;

namespace LanControl.Control.UI
{
    public class HardwareInventoryForm : Form
    {
        private readonly ControlClient _client;
        private readonly List<AgentInfo> _targetAgents;
        private AgentInfo _currentSelectedAgent;

        private readonly Dictionary<string, SystemAuditReport> _reports = new Dictionary<string, SystemAuditReport>();
        private readonly Dictionary<string, string> _statusMap = new Dictionary<string, string>();

        // UI Controls
        private SplitContainer _splitMain;
        private ListView _lvAgents;
        private Label _lblAgentTitle;
        private Button _btnRefresh;
        private Button _btnExportMd;
        private Button _btnExportHtml;
        private Button _btnExportCsv;

        private TabControl _tabAudit;
        private TabPage _tabOverview;
        private TabPage _tabHardware;
        private TabPage _tabDisks;
        private TabPage _tabPeripherals;
        private TabPage _tabSoftware;
        private TabPage _tabHistory;

        // Tab 6: History & Changes
        private Panel _pnlHistoryBadge;
        private Label _lblHistoryBadgeTitle;
        private Label _lblHistoryBadgeDesc;
        private ComboBox _cboSnapshotHistory;
        private Button _btnExportHistoryMd;
        private DataGridView _gridHistoryChanges;
        private Label _lblHistorySummaryStats;
        private List<AuditChangeRecord> _currentChangeRecords = new List<AuditChangeRecord>();
        private AuditSnapshotItem _currentComparedSnapshot;

        // Tab 1: Overview & OS
        private Panel _pnlActivationBadge;
        private Label _lblActivationTitle;
        private Label _lblActivationDesc;
        private Label _lblActivationChannel;
        private Label _lblActivationKey;
        private Label _lblCrackDetails;
        private ListView _lvOsProps;
        private ListView _lvHardwareSummary;

        // Tab 2: Detail Hardware
        private ListView _lvCpuProps;
        private ListView _lvMotherboard;
        private ListView _lvRamSticks;
        private ListView _lvGpu;
        private ListView _lvNetwork;

        // Tab 3: Disks
        private DataGridView _gridPhysicalDisks;
        private DataGridView _gridLogicalDrives;

        // Tab 4: Peripherals
        private ComboBox _cboPeripheralFilter;
        private DataGridView _gridPeripherals;
        private List<PeripheralDevice> _currentPeripherals = new List<PeripheralDevice>();

        // Tab 5: Software & License Audit
        private Label _lblSoftwareStats;
        private TextBox _txtSearchSoftware;
        private Button _btnFilterAll;
        private Button _btnFilterCrack;
        private Button _btnFilterCommercial;
        private Button _btnFilterFreeware;
        private DataGridView _gridSoftware;
        private List<InstalledSoftwareInfo> _currentSoftwareList = new List<InstalledSoftwareInfo>();
        private string _activeSoftwareFilter = "ALL";

        public HardwareInventoryForm(ControlClient client, List<AgentInfo> targetAgents)
        {
            _client = client;
            _targetAgents = targetAgents != null && targetAgents.Count > 0 ? targetAgents : new List<AgentInfo>();
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Kiểm Kê Cấu Hình Phần Cứng, Thiết Bị Ngoại Vi & Bản Quyền Phần Mềm";
            this.Size = new Size(1400, 880);
            this.MinimumSize = new Size(1100, 700);
            this.WindowState = FormWindowState.Maximized;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = ModernTheme.BackgroundDark;
            this.ForeColor = ModernTheme.TextPrimary;

            // Main Splitter
            _splitMain = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                BackColor = ModernTheme.BorderDark
            };

            // LEFT PANEL: Agent List
            Panel leftPanel = new Panel { Dock = DockStyle.Fill, BackColor = ModernTheme.PanelDark, Padding = new Padding(8) };
            Label lblLeftHeader = new Label
            {
                Text = "🖥️ DANH SÁCH MÁY KIỂM TRA",
                Dock = DockStyle.Top,
                Height = 28,
                Font = ModernTheme.SubHeaderFont,
                ForeColor = ModernTheme.AccentCyan
            };

            _lvAgents = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                BackColor = ModernTheme.CardDark,
                ForeColor = ModernTheme.TextPrimary,
                Font = ModernTheme.RegularFont,
                BorderStyle = BorderStyle.None
            };
            _lvAgents.Columns.Add("Máy Trạm / IP", 170);
            _lvAgents.Columns.Add("Trạng Thái", 95);
            _lvAgents.SelectedIndexChanged += OnAgentSelectionChanged;

            leftPanel.Controls.Add(_lvAgents);
            leftPanel.Controls.Add(lblLeftHeader);
            _splitMain.Panel1.Controls.Add(leftPanel);

            // RIGHT PANEL: Top Action Bar & Tabs
            Panel rightPanel = new Panel { Dock = DockStyle.Fill, BackColor = ModernTheme.BackgroundDark };

            // Top Header & Action Bar
            Panel topBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = ModernTheme.PanelDark,
                Padding = new Padding(12, 10, 12, 10)
            };

            _lblAgentTitle = new Label
            {
                Text = "Đang chọn: Chưa chọn máy trạm",
                Font = ModernTheme.HeaderFont,
                ForeColor = ModernTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(12, 14)
            };

            FlowLayoutPanel actionPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                WrapContents = false,
                Padding = new Padding(0, 8, 8, 8),
                BackColor = Color.Transparent
            };

            _btnRefresh = new Button
            {
                Text = "🔄 Làm Mới Dữ Liệu",
                Size = new Size(140, 32),
                Margin = new Padding(3, 0, 3, 0)
            };
            ModernTheme.ApplyToButton(_btnRefresh, ModernTheme.AccentBlue, Color.White);
            _btnRefresh.Click += (s, e) => QueryCurrentAgent();

            _btnExportMd = new Button
            {
                Text = "📝 Xuất Chi Tiết (.md)",
                Size = new Size(155, 32),
                Margin = new Padding(3, 0, 3, 0)
            };
            ModernTheme.ApplyToButton(_btnExportMd, Color.FromArgb(130, 60, 210), Color.White);
            _btnExportMd.Click += (s, e) => ExportReportToMarkdown();

            _btnExportHtml = new Button
            {
                Text = "📥 Xuất HTML",
                Size = new Size(125, 32),
                Margin = new Padding(3, 0, 3, 0)
            };
            ModernTheme.ApplyToButton(_btnExportHtml, ModernTheme.AccentGreen, Color.White);
            _btnExportHtml.Click += (s, e) => ExportReportToHtml();

            _btnExportCsv = new Button
            {
                Text = "📄 Xuất CSV",
                Size = new Size(115, 32),
                Margin = new Padding(3, 0, 3, 0)
            };
            ModernTheme.ApplyToButton(_btnExportCsv, ModernTheme.CardDark, ModernTheme.TextPrimary);
            _btnExportCsv.Click += (s, e) => ExportReportToCsv();

            actionPanel.Controls.Add(_btnRefresh);
            actionPanel.Controls.Add(_btnExportMd);
            actionPanel.Controls.Add(_btnExportHtml);
            actionPanel.Controls.Add(_btnExportCsv);

            topBar.Controls.Add(actionPanel);
            topBar.Controls.Add(_lblAgentTitle);

            // TabControl
            _tabAudit = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = ModernTheme.SubHeaderFont
            };

            InitializeTabOverview();
            InitializeTabHardware();
            InitializeTabDisks();
            InitializeTabPeripherals();
            InitializeTabSoftware();
            InitializeTabHistory();

            _tabAudit.TabPages.Add(_tabOverview);
            _tabAudit.TabPages.Add(_tabHardware);
            _tabAudit.TabPages.Add(_tabDisks);
            _tabAudit.TabPages.Add(_tabPeripherals);
            _tabAudit.TabPages.Add(_tabSoftware);
            _tabAudit.TabPages.Add(_tabHistory);

            rightPanel.Controls.Add(_tabAudit);
            rightPanel.Controls.Add(topBar);
            _splitMain.Panel2.Controls.Add(rightPanel);

            this.Controls.Add(_splitMain);

            this.Load += (s, e) =>
            {
                try
                {
                    if (_splitMain.Width > 500)
                    {
                        _splitMain.SplitterDistance = 280;
                    }
                }
                catch { }

                PopulateAgentList();
                _client.CommandResponseReceived += OnCommandResponseReceived;
                QueryAllAgents();
            };

            this.FormClosing += (s, e) =>
            {
                _client.CommandResponseReceived -= OnCommandResponseReceived;
            };
        }

        #region Initialization of Tabs

        private void InitializeTabOverview()
        {
            _tabOverview = new TabPage("🖥️ Tổng Quan & Bản Quyền");
            _tabOverview.BackColor = ModernTheme.BackgroundDark;
            _tabOverview.Padding = new Padding(12);

            // Activation Badge Panel
            _pnlActivationBadge = new Panel
            {
                Dock = DockStyle.Top,
                Height = 110,
                BackColor = Color.FromArgb(28, 38, 48),
                Padding = new Padding(14)
            };

            _lblActivationTitle = new Label
            {
                Text = "🛡️ TRẠNG THÁI BẢN QUYỀN WINDOWS: Đang kiểm tra...",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = ModernTheme.AccentCyan,
                AutoSize = true,
                Location = new Point(14, 12)
            };

            _lblActivationDesc = new Label
            {
                Text = "Kênh cấp phép: Đang tải...",
                Font = ModernTheme.RegularFont,
                ForeColor = ModernTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(16, 42)
            };

            _lblActivationChannel = new Label
            {
                Text = "Khóa sản phẩm (Partial Key): --",
                Font = ModernTheme.RegularFont,
                ForeColor = ModernTheme.TextSecondary,
                AutoSize = true,
                Location = new Point(16, 64)
            };

            _lblActivationKey = new Label
            {
                Text = "",
                Font = ModernTheme.RegularFont,
                ForeColor = ModernTheme.TextSecondary,
                AutoSize = true,
                Location = new Point(350, 64)
            };

            _lblCrackDetails = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = ModernTheme.AccentRed,
                AutoSize = true,
                Location = new Point(16, 86)
            };

            _pnlActivationBadge.Controls.Add(_lblActivationTitle);
            _pnlActivationBadge.Controls.Add(_lblActivationDesc);
            _pnlActivationBadge.Controls.Add(_lblActivationChannel);
            _pnlActivationBadge.Controls.Add(_lblActivationKey);
            _pnlActivationBadge.Controls.Add(_lblCrackDetails);

            // Responsive Layout for OS Properties and Summary Hardware
            TableLayoutPanel tblOverview = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                BackColor = ModernTheme.BackgroundDark
            };
            tblOverview.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tblOverview.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            // OS GroupBox
            GroupBox grpOs = new GroupBox
            {
                Text = "📋 Thông Tin Hệ Điều Hành & Hệ Thống",
                Dock = DockStyle.Fill,
                Font = ModernTheme.SubHeaderFont,
                ForeColor = ModernTheme.AccentCyan
            };
            _lvOsProps = CreateSimpleListView(new string[] { "Thuộc Tính", "Chi Tiết" }, new int[] { 220, 600 });
            grpOs.Controls.Add(_lvOsProps);
            tblOverview.Controls.Add(grpOs, 0, 0);

            // Hardware Summary GroupBox
            GroupBox grpHw = new GroupBox
            {
                Text = "⚙️ Cấu Hình Phần Cứng Tóm Tắt",
                Dock = DockStyle.Fill,
                Font = ModernTheme.SubHeaderFont,
                ForeColor = ModernTheme.AccentYellow
            };
            _lvHardwareSummary = CreateSimpleListView(new string[] { "Linh Kiện / Thiết Bị", "Thông Số Kỹ Thuật" }, new int[] { 220, 600 });
            grpHw.Controls.Add(_lvHardwareSummary);
            tblOverview.Controls.Add(grpHw, 0, 1);

            _tabOverview.Controls.Add(tblOverview);
            _tabOverview.Controls.Add(_pnlActivationBadge);
        }

        private void InitializeTabHardware()
        {
            _tabHardware = new TabPage("⚙️ Chi Tiết Phần Cứng");
            _tabHardware.BackColor = ModernTheme.BackgroundDark;
            _tabHardware.Padding = new Padding(8);

            TableLayoutPanel tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3,
                BackColor = ModernTheme.BackgroundDark
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 35F));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 35F));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 30F));

            // 1. CPU
            GroupBox grpCpu = new GroupBox { Text = "🧠 Bộ Vi Xử Lý (CPU)", Dock = DockStyle.Fill, Font = ModernTheme.SubHeaderFont, ForeColor = ModernTheme.AccentCyan };
            _lvCpuProps = CreateSimpleListView(new string[] { "Thuộc Tính", "Giá Trị" }, new int[] { 160, 320 });
            grpCpu.Controls.Add(_lvCpuProps);
            tbl.Controls.Add(grpCpu, 0, 0);

            // 2. Motherboard & BIOS
            GroupBox grpMb = new GroupBox { Text = "🖲️ Bo Mạch Chủ (Mainboard) & BIOS", Dock = DockStyle.Fill, Font = ModernTheme.SubHeaderFont, ForeColor = ModernTheme.AccentGreen };
            _lvMotherboard = CreateSimpleListView(new string[] { "Mục", "Thông Tin" }, new int[] { 160, 320 });
            grpMb.Controls.Add(_lvMotherboard);
            tbl.Controls.Add(grpMb, 1, 0);

            // 3. RAM Sticks
            GroupBox grpRam = new GroupBox { Text = "⚡ Chi Tiết Từng Khe Cắm RAM (Slots & Modules)", Dock = DockStyle.Fill, Font = ModernTheme.SubHeaderFont, ForeColor = ModernTheme.AccentYellow };
            _lvRamSticks = CreateSimpleListView(new string[] { "Khe Cắm", "Dung Lượng", "Tốc Độ Bus", "Hãng Sản Xuất", "Số Serial", "Mã Part Number" }, new int[] { 90, 95, 90, 110, 110, 130 });
            grpRam.Controls.Add(_lvRamSticks);
            tbl.Controls.Add(grpRam, 0, 1);

            // 4. GPU & Display
            GroupBox grpGpu = new GroupBox { Text = "🎮 Card Đồ Họa (GPU) & Màn Hình", Dock = DockStyle.Fill, Font = ModernTheme.SubHeaderFont, ForeColor = Color.FromArgb(255, 120, 120) };
            _lvGpu = CreateSimpleListView(new string[] { "Thông Số", "Chi Tiết" }, new int[] { 160, 320 });
            grpGpu.Controls.Add(_lvGpu);
            tbl.Controls.Add(grpGpu, 1, 1);

            // 5. Network Adapters (Spans 2 columns)
            GroupBox grpNet = new GroupBox { Text = "🌐 Card Mạng & Kết Nối (Network Adapters)", Dock = DockStyle.Fill, Font = ModernTheme.SubHeaderFont, ForeColor = ModernTheme.TextPrimary };
            _lvNetwork = CreateSimpleListView(new string[] { "Tên Card Mạng", "Địa Chỉ MAC", "Địa Chỉ IP", "Default Gateway", "Tốc Độ", "Trạng Thái" }, new int[] { 220, 140, 160, 130, 100, 90 });
            grpNet.Controls.Add(_lvNetwork);
            tbl.Controls.Add(grpNet, 0, 2);
            tbl.SetColumnSpan(grpNet, 2);

            _tabHardware.Controls.Add(tbl);
        }

        private void InitializeTabDisks()
        {
            _tabDisks = new TabPage("💾 Ổ Đĩa & Lưu Trữ");
            _tabDisks.BackColor = ModernTheme.BackgroundDark;
            _tabDisks.Padding = new Padding(10);

            TableLayoutPanel tblDisks = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                BackColor = ModernTheme.BackgroundDark
            };
            tblDisks.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tblDisks.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            // Physical Disks
            GroupBox grpPhys = new GroupBox { Text = "💽 Ổ Đĩa Vật Lý (Physical Disks / NVMe / SSD / HDD)", Dock = DockStyle.Fill, Font = ModernTheme.SubHeaderFont, ForeColor = ModernTheme.AccentCyan };
            _gridPhysicalDisks = CreateStyledGrid();
            _gridPhysicalDisks.Columns.Add("Index", "Thứ Tự");
            _gridPhysicalDisks.Columns.Add("Model", "Model Ổ Đĩa");
            _gridPhysicalDisks.Columns.Add("InterfaceType", "Chuẩn Giao Tiếp");
            _gridPhysicalDisks.Columns.Add("SizeGb", "Dung Lượng (GB)");
            _gridPhysicalDisks.Columns.Add("SerialNumber", "Số Serial Number");
            _gridPhysicalDisks.Columns.Add("MediaType", "Loại Ổ / Media");
            grpPhys.Controls.Add(_gridPhysicalDisks);
            tblDisks.Controls.Add(grpPhys, 0, 0);

            // Logical Partitions
            GroupBox grpLog = new GroupBox { Text = "📁 Phân Vùng Ổ Đĩa (Logical Partitions C:, D:...)", Dock = DockStyle.Fill, Font = ModernTheme.SubHeaderFont, ForeColor = ModernTheme.AccentGreen };
            _gridLogicalDrives = CreateStyledGrid();
            _gridLogicalDrives.Columns.Add("DriveLetter", "Ổ Đĩa");
            _gridLogicalDrives.Columns.Add("VolumeName", "Tên Nhãn");
            _gridLogicalDrives.Columns.Add("FileSystem", "Định Dạng");
            _gridLogicalDrives.Columns.Add("TotalSizeGb", "Tổng Dung Lượng (GB)");
            _gridLogicalDrives.Columns.Add("FreeSpaceGb", "Dung Lượng Trống (GB)");
            _gridLogicalDrives.Columns.Add("FreePercent", "Tỉ Lệ Trống (%)");
            grpLog.Controls.Add(_gridLogicalDrives);
            tblDisks.Controls.Add(grpLog, 0, 1);

            _tabDisks.Controls.Add(tblDisks);
        }

        private void InitializeTabPeripherals()
        {
            _tabPeripherals = new TabPage("🖨️ Thiết Bị Ngoại Vi");
            _tabPeripherals.BackColor = ModernTheme.BackgroundDark;
            _tabPeripherals.Padding = new Padding(10);

            Panel topPer = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = ModernTheme.PanelDark, Padding = new Padding(8, 6, 8, 6) };
            Label lblFilter = new Label { Text = "🏷️ Lọc Thiết Bị:", AutoSize = true, Location = new Point(12, 12), Font = ModernTheme.SubHeaderFont, ForeColor = ModernTheme.TextPrimary };
            _cboPeripheralFilter = new ComboBox
            {
                Location = new Point(140, 9),
                Width = 260,
                Font = ModernTheme.RegularFont,
                BackColor = ModernTheme.CardDark,
                ForeColor = ModernTheme.TextPrimary,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cboPeripheralFilter.Items.AddRange(new object[]
            {
                "Tất Cả Thiết Bị Ngoại Vi",
                "🖨️ Máy In (Printers)",
                "📷 Máy Quét / Scan / Camera (Imaging)",
                "👆 Máy Chấm Công / Vân Tay (Biometrics)",
                "🔌 Thiết Bị Ngoại Vi USB / PnP",
                "🎧 Thiết Bị Âm Thanh (Audio)"
            });
            _cboPeripheralFilter.SelectedIndex = 0;
            _cboPeripheralFilter.SelectedIndexChanged += (s, e) => ApplyPeripheralFilter();

            topPer.Controls.Add(lblFilter);
            topPer.Controls.Add(_cboPeripheralFilter);

            _gridPeripherals = CreateStyledGrid();
            _gridPeripherals.Columns.Add("Name", "Tên Thiết Bị / Model");
            _gridPeripherals.Columns.Add("Category", "Phân Loại");
            _gridPeripherals.Columns.Add("Manufacturer", "Hãng Sản Xuất");
            _gridPeripherals.Columns.Add("DeviceID", "Mã Phần Cứng / Device ID / Cổng");
            _gridPeripherals.Columns.Add("Status", "Trạng Thái");
            _gridPeripherals.Columns.Add("Description", "Mô Tả Chi Tiết");

            _tabPeripherals.Controls.Add(_gridPeripherals);
            _tabPeripherals.Controls.Add(topPer);
        }

        private void InitializeTabSoftware()
        {
            _tabSoftware = new TabPage("📦 Phần Mềm & Bản Quyền");
            _tabSoftware.BackColor = ModernTheme.BackgroundDark;
            _tabSoftware.Padding = new Padding(10);

            // Filter and Stats Ribbon (Height 84 for two clean rows)
            Panel topSoft = new Panel { Dock = DockStyle.Top, Height = 84, BackColor = ModernTheme.PanelDark, Padding = new Padding(10, 8, 10, 8) };

            _lblSoftwareStats = new Label
            {
                Text = "📊 Thống kê: Tổng 0 | ⚠️ Nghi vấn Crack: 0 | 💼 Thương mại: 0 | 🆓 Miễn phí: 0",
                Font = ModernTheme.SubHeaderFont,
                ForeColor = ModernTheme.AccentCyan,
                AutoSize = true,
                Location = new Point(12, 10)
            };

            FlowLayoutPanel filterFlow = new FlowLayoutPanel
            {
                Location = new Point(10, 38),
                Size = new Size(1300, 38),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent
            };

            _btnFilterAll = CreateFilterButton("📋 Xem Tất Cả (0)", ModernTheme.AccentBlue, () => {
                _txtSearchSoftware.Text = "";
                SetSoftwareFilter("ALL");
            });

            _btnFilterCrack = CreateFilterButton("⚠️ Nghi Vấn Crack (0)", ModernTheme.AccentRed, () => SetSoftwareFilter("CRACK"));
            _btnFilterCommercial = CreateFilterButton("💼 Thương Mại (0)", Color.FromArgb(220, 150, 40), () => SetSoftwareFilter("COMMERCIAL"));
            _btnFilterFreeware = CreateFilterButton("🆓 Miễn Phí (0)", ModernTheme.AccentGreen, () => SetSoftwareFilter("FREEWARE"));

            Label lblSearch = new Label
            {
                Text = "🔍 Tìm kiếm:",
                AutoSize = true,
                Font = ModernTheme.RegularFont,
                ForeColor = ModernTheme.TextSecondary,
                Margin = new Padding(15, 7, 3, 0)
            };

            _txtSearchSoftware = new TextBox
            {
                Width = 190,
                Font = ModernTheme.RegularFont,
                BackColor = ModernTheme.CardDark,
                ForeColor = ModernTheme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 4, 3, 0)
            };
            _txtSearchSoftware.TextChanged += (s, e) => ApplySoftwareFilter();

            Button btnClearSearch = new Button
            {
                Text = "✖ Xóa Tìm",
                Size = new Size(85, 28),
                Font = ModernTheme.RegularFont,
                Margin = new Padding(3, 3, 0, 0)
            };
            ModernTheme.ApplyToButton(btnClearSearch, ModernTheme.CardDark, ModernTheme.TextSecondary);
            btnClearSearch.Click += (s, e) => {
                _txtSearchSoftware.Text = "";
                SetSoftwareFilter("ALL");
            };

            filterFlow.Controls.Add(_btnFilterAll);
            filterFlow.Controls.Add(_btnFilterCrack);
            filterFlow.Controls.Add(_btnFilterCommercial);
            filterFlow.Controls.Add(_btnFilterFreeware);
            filterFlow.Controls.Add(lblSearch);
            filterFlow.Controls.Add(_txtSearchSoftware);
            filterFlow.Controls.Add(btnClearSearch);

            topSoft.Controls.Add(_lblSoftwareStats);
            topSoft.Controls.Add(filterFlow);

            _gridSoftware = CreateStyledGrid();
            _gridSoftware.Columns.Add("DisplayName", "Tên Phần Mềm Cài Đặt");
            _gridSoftware.Columns.Add("DisplayVersion", "Phiên Bản");
            _gridSoftware.Columns.Add("Publisher", "Nhà Phát Hành");
            _gridSoftware.Columns.Add("InstallDate", "Ngày Cài Đặt");
            _gridSoftware.Columns.Add("LicenseStatus", "Tình Trạng Bản Quyền / Crack");
            _gridSoftware.Columns.Add("CrackNotes", "Ghi Chú Đánh Giá");

            _tabSoftware.Controls.Add(_gridSoftware);
            _tabSoftware.Controls.Add(topSoft);
        }

        #endregion

        #region Helper Controls

        private Button CreateFilterButton(string text, Color accentColor, Action onClick)
        {
            Button btn = new Button
            {
                Text = text,
                Size = new Size(140, 28),
                Font = ModernTheme.RegularFont
            };
            ModernTheme.ApplyToButton(btn, ModernTheme.CardDark, accentColor);
            btn.Click += (s, e) => onClick();
            return btn;
        }

        private ListView CreateSimpleListView(string[] headers, int[] widths)
        {
            ListView lv = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                BackColor = ModernTheme.CardDark,
                ForeColor = ModernTheme.TextPrimary,
                Font = ModernTheme.RegularFont,
                BorderStyle = BorderStyle.None
            };
            for (int i = 0; i < headers.Length; i++)
            {
                lv.Columns.Add(headers[i], widths != null && i < widths.Length ? widths[i] : 150);
            }
            return lv;
        }

        private DataGridView CreateStyledGrid()
        {
            DataGridView grid = new DataGridView
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
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            grid.DefaultCellStyle.BackColor = ModernTheme.CardDark;
            grid.DefaultCellStyle.ForeColor = ModernTheme.TextPrimary;
            grid.DefaultCellStyle.SelectionBackColor = ModernTheme.AccentBlue;
            grid.DefaultCellStyle.SelectionForeColor = Color.White;
            grid.DefaultCellStyle.Font = ModernTheme.RegularFont;
            grid.ColumnHeadersDefaultCellStyle.BackColor = ModernTheme.PanelDark;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = ModernTheme.TextPrimary;
            grid.ColumnHeadersDefaultCellStyle.Font = ModernTheme.SubHeaderFont;
            grid.EnableHeadersVisualStyles = false;
            return grid;
        }

        #endregion

        #region Data Retrieval and Event Handling

        private void PopulateAgentList()
        {
            _lvAgents.Items.Clear();
            foreach (var a in _targetAgents)
            {
                ListViewItem lvi = new ListViewItem(string.Format("{0} ({1})", a.Hostname, a.IpAddress));
                lvi.SubItems.Add("⏳ Đang tải...");
                lvi.Tag = a;
                _lvAgents.Items.Add(lvi);
            }

            if (_lvAgents.Items.Count > 0)
            {
                _lvAgents.Items[0].Selected = true;
            }
        }

        private void QueryAllAgents()
        {
            foreach (var a in _targetAgents)
            {
                _statusMap[a.AgentId] = "⏳ Đang tải...";
                _client.SendCommand(a.AgentId, CommandType.GetHardwareInventory);
            }
            UpdateAgentListStatus();
        }

        private void QueryCurrentAgent()
        {
            if (_currentSelectedAgent != null)
            {
                _statusMap[_currentSelectedAgent.AgentId] = "⏳ Đang tải...";
                UpdateAgentListStatus();
                _client.SendCommand(_currentSelectedAgent.AgentId, CommandType.GetHardwareInventory);
            }
        }

        private void OnCommandResponseReceived(CommandResponse resp)
        {
            if (resp == null || string.IsNullOrEmpty(resp.Output)) return;

            // Check if response is SystemAuditReport JSON
            if (resp.Output.IndexOf("\"CPU\"", StringComparison.OrdinalIgnoreCase) < 0 &&
                resp.Output.IndexOf("\"OS\"", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return;
            }

            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action<CommandResponse>(OnCommandResponseReceived), resp);
                return;
            }

            try
            {
                SystemAuditReport report = PacketSerializer.Deserialize<SystemAuditReport>(resp.Output);
                if (report != null)
                {
                    _reports[resp.AgentId] = report;
                    _statusMap[resp.AgentId] = "🟢 Sẵn sàng";
                    UpdateAgentListStatus();

                    if (_currentSelectedAgent != null && _currentSelectedAgent.AgentId == resp.AgentId)
                    {
                        DisplayAuditReport(report);
                    }
                }
            }
            catch { }
        }

        private void UpdateAgentListStatus()
        {
            foreach (ListViewItem item in _lvAgents.Items)
            {
                AgentInfo a = item.Tag as AgentInfo;
                if (a != null && _statusMap.ContainsKey(a.AgentId))
                {
                    item.SubItems[1].Text = _statusMap[a.AgentId];
                }
            }
        }

        private void OnAgentSelectionChanged(object sender, EventArgs e)
        {
            if (_lvAgents.SelectedItems.Count == 0) return;

            AgentInfo a = _lvAgents.SelectedItems[0].Tag as AgentInfo;
            if (a == null) return;

            _currentSelectedAgent = a;
            _lblAgentTitle.Text = string.Format("🖥️ MÁY TRẠM: {0} | IP: {1} | VLAN: {2}", a.Hostname, a.IpAddress, a.VlanTag);

            if (_reports.ContainsKey(a.AgentId))
            {
                DisplayAuditReport(_reports[a.AgentId]);
            }
            else
            {
                ClearAllDisplays("Đang tải dữ liệu kiểm kê từ máy trạm...");
                _client.SendCommand(a.AgentId, CommandType.GetHardwareInventory);
            }
        }

        #endregion

        #region Display Audit Report

        private void ClearAllDisplays(string message)
        {
            _lblActivationTitle.Text = "🛡️ " + message;
            _lblActivationDesc.Text = "";
            _lblActivationChannel.Text = "";
            _lblActivationKey.Text = "";
            _lblCrackDetails.Text = "";
            _pnlActivationBadge.BackColor = Color.FromArgb(28, 38, 48);

            _lvOsProps.Items.Clear();
            _lvHardwareSummary.Items.Clear();
            _lvCpuProps.Items.Clear();
            _lvMotherboard.Items.Clear();
            _lvRamSticks.Items.Clear();
            _lvGpu.Items.Clear();
            _lvNetwork.Items.Clear();
            _gridPhysicalDisks.Rows.Clear();
            _gridLogicalDrives.Rows.Clear();
            _gridPeripherals.Rows.Clear();
            _gridSoftware.Rows.Clear();
            if (_gridHistoryChanges != null) _gridHistoryChanges.Rows.Clear();
            if (_cboSnapshotHistory != null) _cboSnapshotHistory.Items.Clear();
        }

        private void DisplayAuditReport(SystemAuditReport report)
        {
            if (report == null) return;

            // 1. Overview Tab & Windows Activation Badge
            if (report.OS.IsSuspectedCrack)
            {
                _pnlActivationBadge.BackColor = Color.FromArgb(64, 25, 25);
                _lblActivationTitle.Text = "⚠️ CẢNH BÁO BẢN QUYỀN: PHÁT HIỆN DẤU HIỆU CRACK / GIẢ LẬP KMS!";
                _lblActivationTitle.ForeColor = ModernTheme.AccentRed;
                _lblCrackDetails.Text = "🚨 Chi tiết phát hiện: " + report.OS.CrackWarningDetails;
            }
            else if (report.OS.WindowsActivationStatus.IndexOf("Licensed", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _pnlActivationBadge.BackColor = Color.FromArgb(20, 50, 35);
                _lblActivationTitle.Text = "✅ BẢN QUYỀN WINDOWS CHÍNH HÃNG HỢP LỆ (ACTIVATED)";
                _lblActivationTitle.ForeColor = ModernTheme.AccentGreen;
                _lblCrackDetails.Text = "";
            }
            else
            {
                _pnlActivationBadge.BackColor = Color.FromArgb(50, 40, 20);
                _lblActivationTitle.Text = "ℹ️ WINDOWS CHƯA KÍCH HOẠT / DÙNG THỬ (NOT GENUINE)";
                _lblActivationTitle.ForeColor = ModernTheme.AccentYellow;
                _lblCrackDetails.Text = "";
            }

            _lblActivationDesc.Text = "Trạng thái: " + report.OS.WindowsActivationStatus;
            _lblActivationChannel.Text = "Kênh cấp phép: " + report.OS.WindowsLicenseChannel;
            _lblActivationKey.Text = string.IsNullOrEmpty(report.OS.PartialProductKey) ? "" : "Mã Khóa (Partial Key): *****-*****-" + report.OS.PartialProductKey;

            // OS Props
            _lvOsProps.BeginUpdate();
            _lvOsProps.Items.Clear();
            AddProp(_lvOsProps, "Tên Hệ Điều Hành (OS Name)", report.OS.OsName);
            AddProp(_lvOsProps, "Phiên Bản (Version)", report.OS.Version);
            AddProp(_lvOsProps, "Số Bản Dựng (Build Number)", report.OS.BuildNumber);
            AddProp(_lvOsProps, "Kiến Trúc Hệ Thống (Architecture)", report.OS.Architecture);
            AddProp(_lvOsProps, "Ngày Cài Đặt Windows", report.OS.InstallDate);
            AddProp(_lvOsProps, "Tên Máy Tính (Computer Name)", report.OS.ComputerName);
            AddProp(_lvOsProps, "Tài Khoản Đăng Nhập", report.OS.RegisteredUser);
            AddProp(_lvOsProps, "Mạng Miền (Domain/Workgroup)", report.OS.DomainOrWorkgroup);
            _lvOsProps.EndUpdate();

            // Hardware Summary
            _lvHardwareSummary.BeginUpdate();
            _lvHardwareSummary.Items.Clear();
            AddProp(_lvHardwareSummary, "Bộ Vi Xử Lý (CPU)", string.Format("{0} ({1} Nhân, {2} Luồng)", report.CPU.Name, report.CPU.Cores, report.CPU.LogicalProcessors));
            AddProp(_lvHardwareSummary, "Bộ Nhớ Trong (RAM)", string.Format("{0:N0} MB (~{1} GB) [{2} thanh RAM]", report.TotalRamMb, Math.Round(report.TotalRamMb / 1024.0, 1), report.RamSticks.Count));
            AddProp(_lvHardwareSummary, "Bo Mạch Chủ (Mainboard)", string.Format("{0} - Model: {1} (S/N: {2})", report.Motherboard.Manufacturer, report.Motherboard.Product, report.Motherboard.SerialNumber));
            AddProp(_lvHardwareSummary, "BIOS Hệ Thống", string.Format("{0} ver {1} ({2})", report.Motherboard.BiosVendor, report.Motherboard.BiosVersion, report.Motherboard.BiosReleaseDate));
            AddProp(_lvHardwareSummary, "Ổ Đĩa Vật Lý", string.Format("{0} ổ đĩa kết nối", report.PhysicalDisks.Count));
            AddProp(_lvHardwareSummary, "Card Đồ Họa (GPU)", report.Gpus.Count > 0 ? report.Gpus[0].Name : "Standard Graphics Adapter");
            _lvHardwareSummary.EndUpdate();

            // 2. Hardware Detail Tab
            _lvCpuProps.BeginUpdate();
            _lvCpuProps.Items.Clear();
            AddProp(_lvCpuProps, "Model Vi Xử Lý", report.CPU.Name);
            AddProp(_lvCpuProps, "Hãng Sản Xuất", report.CPU.Manufacturer);
            AddProp(_lvCpuProps, "Số Nhân Vật Lý", report.CPU.Cores.ToString());
            AddProp(_lvCpuProps, "Số Luồng Logic", report.CPU.LogicalProcessors.ToString());
            AddProp(_lvCpuProps, "Xung Nhịp Tối Đa", report.CPU.MaxClockSpeedMhz + " MHz");
            AddProp(_lvCpuProps, "Kiến Trúc Vi Kiến Trúc", report.CPU.Architecture);
            AddProp(_lvCpuProps, "Mã Processor ID", report.CPU.ProcessorId);
            _lvCpuProps.EndUpdate();

            _lvMotherboard.BeginUpdate();
            _lvMotherboard.Items.Clear();
            AddProp(_lvMotherboard, "Hãng Sản Xuất Mainboard", report.Motherboard.Manufacturer);
            AddProp(_lvMotherboard, "Model Sản Phẩm Mainboard", report.Motherboard.Product);
            AddProp(_lvMotherboard, "Số Serial Number Mainboard", report.Motherboard.SerialNumber);
            AddProp(_lvMotherboard, "Nhà Cung Cấp BIOS", report.Motherboard.BiosVendor);
            AddProp(_lvMotherboard, "Phiên Bản BIOS", report.Motherboard.BiosVersion);
            AddProp(_lvMotherboard, "Ngày Phát Hành BIOS", report.Motherboard.BiosReleaseDate);
            _lvMotherboard.EndUpdate();

            _lvRamSticks.BeginUpdate();
            _lvRamSticks.Items.Clear();
            foreach (var r in report.RamSticks)
            {
                ListViewItem lvi = new ListViewItem(r.DeviceLocator);
                lvi.SubItems.Add(r.CapacityMb + " MB");
                lvi.SubItems.Add(r.SpeedMhz > 0 ? r.SpeedMhz + " MHz" : "N/A");
                lvi.SubItems.Add(r.Manufacturer);
                lvi.SubItems.Add(r.SerialNumber);
                lvi.SubItems.Add(r.PartNumber);
                _lvRamSticks.Items.Add(lvi);
            }
            _lvRamSticks.EndUpdate();

            _lvGpu.BeginUpdate();
            _lvGpu.Items.Clear();
            foreach (var g in report.Gpus)
            {
                AddProp(_lvGpu, "Card Đồ Họa (GPU)", g.Name);
                AddProp(_lvGpu, "Bộ Nhớ VRAM", g.AdapterRamMb > 0 ? g.AdapterRamMb + " MB" : "Chia sẻ hệ thống");
                AddProp(_lvGpu, "Phiên Bản Driver", g.DriverVersion);
                AddProp(_lvGpu, "Độ Phân Giải Màn Hình", g.CurrentResolution);
            }
            _lvGpu.EndUpdate();

            _lvNetwork.BeginUpdate();
            _lvNetwork.Items.Clear();
            foreach (var n in report.NetworkAdapters)
            {
                ListViewItem lvi = new ListViewItem(n.Name);
                lvi.SubItems.Add(n.MacAddress);
                lvi.SubItems.Add(n.IpAddresses);
                lvi.SubItems.Add(n.DefaultGateway);
                lvi.SubItems.Add(n.SpeedMbps > 0 ? n.SpeedMbps + " Mbps" : "--");
                lvi.SubItems.Add(n.Status);
                _lvNetwork.Items.Add(lvi);
            }
            _lvNetwork.EndUpdate();

            // 3. Disks Tab
            _gridPhysicalDisks.Rows.Clear();
            foreach (var d in report.PhysicalDisks)
            {
                _gridPhysicalDisks.Rows.Add(d.Index, d.Model, d.InterfaceType, d.SizeGb + " GB", d.SerialNumber, d.MediaType);
            }

            _gridLogicalDrives.Rows.Clear();
            foreach (var l in report.LogicalDrives)
            {
                _gridLogicalDrives.Rows.Add(l.DriveLetter, l.VolumeName, l.FileSystem, l.TotalSizeGb + " GB", l.FreeSpaceGb + " GB", l.FreePercent + "%");
            }

            // 4. Peripherals Tab
            _currentPeripherals = report.Peripherals ?? new List<PeripheralDevice>();
            ApplyPeripheralFilter();

            // 5. Software & License Tab
            _currentSoftwareList = report.InstalledSoftware ?? new List<InstalledSoftwareInfo>();
            _lblSoftwareStats.Text = string.Format("Tổng: {0} phần mềm | ⚠️ Nghi vấn Crack: {1} | 💼 Thương mại: {2} | 🆓 Miễn phí: {3}",
                report.TotalSoftwareCount, report.CrackCount, report.CommercialCount, report.FreewareCount);

            _btnFilterAll.Text = string.Format("📋 Xem Tất Cả ({0})", report.TotalSoftwareCount);
            _btnFilterCrack.Text = string.Format("⚠️ Nghi Vấn Crack ({0})", report.CrackCount);
            _btnFilterCommercial.Text = string.Format("💼 Thương Mại ({0})", report.CommercialCount);
            _btnFilterFreeware.Text = string.Format("🆓 Miễn Phí ({0})", report.FreewareCount);

            ApplySoftwareFilter();

            // 6. History & Change Tracking
            UpdateHistoryTab(report);
        }

        private void AddProp(ListView lv, string name, string value)
        {
            ListViewItem lvi = new ListViewItem(name);
            lvi.SubItems.Add(string.IsNullOrEmpty(value) ? "--" : value);
            lv.Items.Add(lvi);
        }

        #endregion

        #region Filters & Search

        private void ApplyPeripheralFilter()
        {
            int sel = _cboPeripheralFilter.SelectedIndex;
            _gridPeripherals.Rows.Clear();

            foreach (var p in _currentPeripherals)
            {
                bool match = false;
                if (sel == 0) match = true; // All
                else if (sel == 1 && p.Category.Contains("Máy In")) match = true;
                else if (sel == 2 && p.Category.Contains("Máy Quét")) match = true;
                else if (sel == 3 && p.Category.Contains("Vân Tay")) match = true;
                else if (sel == 4 && p.Category.Contains("USB")) match = true;
                else if (sel == 5 && p.Category.Contains("Âm Thanh")) match = true;

                if (match)
                {
                    _gridPeripherals.Rows.Add(p.Name, p.Category, p.Manufacturer, p.DeviceID, p.Status, p.Description);
                }
            }
        }

        private void SetSoftwareFilter(string category)
        {
            _activeSoftwareFilter = category;
            ApplySoftwareFilter();
        }

        private void ApplySoftwareFilter()
        {
            string search = _txtSearchSoftware.Text.Trim().ToLowerInvariant();
            _gridSoftware.Rows.Clear();

            foreach (var sw in _currentSoftwareList)
            {
                // Category Filter
                bool catMatch = false;
                if (_activeSoftwareFilter == "ALL") catMatch = true;
                else if (_activeSoftwareFilter == "CRACK" && sw.LicenseCategory == SoftwareLicenseCategory.SuspectedCrack) catMatch = true;
                else if (_activeSoftwareFilter == "COMMERCIAL" && sw.LicenseCategory == SoftwareLicenseCategory.Commercial) catMatch = true;
                else if (_activeSoftwareFilter == "FREEWARE" && sw.LicenseCategory == SoftwareLicenseCategory.Freeware) catMatch = true;

                if (!catMatch) continue;

                // Search Filter
                if (!string.IsNullOrEmpty(search))
                {
                    if (!sw.DisplayName.ToLowerInvariant().Contains(search) &&
                        !(sw.Publisher ?? "").ToLowerInvariant().Contains(search) &&
                        !(sw.DisplayVersion ?? "").ToLowerInvariant().Contains(search))
                    {
                        continue;
                    }
                }

                int rowIdx = _gridSoftware.Rows.Add(sw.DisplayName, sw.DisplayVersion, sw.Publisher, sw.InstallDate, GetLicenseCategoryBadge(sw.LicenseCategory), sw.CrackNotes);

                // Highlight Row Color
                if (sw.LicenseCategory == SoftwareLicenseCategory.SuspectedCrack)
                {
                    _gridSoftware.Rows[rowIdx].DefaultCellStyle.ForeColor = Color.FromArgb(255, 100, 100);
                    _gridSoftware.Rows[rowIdx].DefaultCellStyle.BackColor = Color.FromArgb(45, 20, 20);
                }
                else if (sw.LicenseCategory == SoftwareLicenseCategory.Commercial)
                {
                    _gridSoftware.Rows[rowIdx].DefaultCellStyle.ForeColor = Color.FromArgb(255, 190, 80);
                }
                else if (sw.LicenseCategory == SoftwareLicenseCategory.Freeware)
                {
                    _gridSoftware.Rows[rowIdx].DefaultCellStyle.ForeColor = Color.FromArgb(120, 240, 150);
                }
            }
        }

        private string GetLicenseCategoryBadge(SoftwareLicenseCategory cat)
        {
            switch (cat)
            {
                case SoftwareLicenseCategory.SuspectedCrack:
                    return "⚠️ Nghi Vấn Crack / Activator";
                case SoftwareLicenseCategory.Commercial:
                    return "💼 Phần Mềm Thương Mại";
                case SoftwareLicenseCategory.Freeware:
                    return "🆓 Miễn Phí / Mã Nguồn Mở";
                default:
                    return "ℹ️ Ứng Dụng Tiêu Chuẩn";
            }
        }

        #endregion

        #region Export HTML / CSV

        private void ExportReportToHtml()
        {
            if (_currentSelectedAgent == null || !_reports.ContainsKey(_currentSelectedAgent.AgentId))
            {
                MessageBox.Show("Chưa có dữ liệu kiểm kê để xuất báo cáo!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SystemAuditReport rep = _reports[_currentSelectedAgent.AgentId];

            using (SaveFileDialog dlg = new SaveFileDialog())
            {
                dlg.Title = "Xuất Báo Cáo Cấu Hình & Bản Quyền Phần Mềm (HTML)";
                dlg.Filter = "HTML Report (*.html)|*.html|All Files (*.*)|*.*";
                dlg.FileName = string.Format("BaoCao_KiemKe_{0}_{1}.html", rep.Hostname, DateTime.Now.ToString("yyyyMMdd_HHmm"));

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine("<!DOCTYPE html>");
                        sb.AppendLine("<html lang='vi'>");
                        sb.AppendLine("<head>");
                        sb.AppendLine("<meta charset='UTF-8'>");
                        sb.AppendLine("<title>Báo Cáo Kiểm Kê Cấu Hình & Bản Quyền - " + rep.Hostname + "</title>");
                        sb.AppendLine("<style>");
                        sb.AppendLine("body { font-family: 'Segoe UI', Tahoma, sans-serif; background: #0f141c; color: #e2e8f0; margin: 20px; line-height: 1.5; }");
                        sb.AppendLine("h1, h2, h3 { color: #38bdf8; }");
                        sb.AppendLine(".badge-box { padding: 15px; border-radius: 8px; margin-bottom: 20px; }");
                        sb.AppendLine(".badge-crack { background: #451a1a; border: 2px solid #ef4444; color: #fca5a5; }");
                        sb.AppendLine(".badge-genuine { background: #143224; border: 2px solid #10b981; color: #a7f3d0; }");
                        sb.AppendLine("table { width: 100%; border-collapse: collapse; margin-bottom: 25px; background: #1a2332; border-radius: 6px; overflow: hidden; }");
                        sb.AppendLine("th { background: #263345; color: #38bdf8; text-align: left; padding: 10px; font-weight: 600; border-bottom: 1px solid #334155; }");
                        sb.AppendLine("td { padding: 8px 10px; border-bottom: 1px solid #202b3a; }");
                        sb.AppendLine(".crack-row { background: #3b1515; color: #fca5a5; font-weight: bold; }");
                        sb.AppendLine(".comm-row { color: #fbbf24; }");
                        sb.AppendLine(".free-row { color: #4ade80; }");
                        sb.AppendLine("</style>");
                        sb.AppendLine("</head><body>");

                        sb.AppendLine("<h1>🖥️ BÁO CÁO KIỂM KÊ CẤU HÌNH & BẢN QUYỀN PHẦN MỀM</h1>");
                        sb.AppendLine(string.Format("<p><strong>Máy trạm:</strong> {0} | <strong>IP:</strong> {1} | <strong>VLAN:</strong> {2} | <strong>Thời gian:</strong> {3}</p>",
                            rep.Hostname, rep.IpAddress, rep.VlanTag, rep.CollectedTime));

                        // Activation Badge
                        string badgeClass = rep.OS.IsSuspectedCrack ? "badge-crack" : "badge-genuine";
                        sb.AppendLine("<div class='badge-box " + badgeClass + "'>");
                        sb.AppendLine("<h2>" + (rep.OS.IsSuspectedCrack ? "⚠️ PHÁT HIỆN DẤU HIỆU CRACK / KÍCH HOẠT LẬU" : "✅ BẢN QUYỀN WINDOWS HỢP LỆ") + "</h2>");
                        sb.AppendLine("<p><strong>Trạng thái:</strong> " + rep.OS.WindowsActivationStatus + "</p>");
                        sb.AppendLine("<p><strong>Kênh cấp phép:</strong> " + rep.OS.WindowsLicenseChannel + "</p>");
                        if (rep.OS.IsSuspectedCrack)
                        {
                            sb.AppendLine("<p><strong>Chi tiết cảnh báo:</strong> " + rep.OS.CrackWarningDetails + "</p>");
                        }
                        sb.AppendLine("</div>");

                        // OS Table
                        sb.AppendLine("<h2>📋 1. Thông Tin Hệ Điều Hành</h2>");
                        sb.AppendLine("<table>");
                        sb.AppendLine("<tr><th>Thuộc Tính</th><th>Chi Tiết</th></tr>");
                        sb.AppendLine("<tr><td>Tên OS</td><td>" + rep.OS.OsName + "</td></tr>");
                        sb.AppendLine("<tr><td>Phiên bản / Build</td><td>" + rep.OS.Version + " (Build " + rep.OS.BuildNumber + ")</td></tr>");
                        sb.AppendLine("<tr><td>Kiến trúc</td><td>" + rep.OS.Architecture + "</td></tr>");
                        sb.AppendLine("<tr><td>Ngày cài đặt</td><td>" + rep.OS.InstallDate + "</td></tr>");
                        sb.AppendLine("<tr><td>Domain / Workgroup</td><td>" + rep.OS.DomainOrWorkgroup + "</td></tr>");
                        sb.AppendLine("</table>");

                        // Hardware Table
                        sb.AppendLine("<h2>⚙️ 2. Cấu Hình Phần Cứng Chi Tiết</h2>");
                        sb.AppendLine("<table>");
                        sb.AppendLine("<tr><th>Linh Kiện</th><th>Thông Số</th></tr>");
                        sb.AppendLine(string.Format("<tr><td>CPU</td><td>{0} ({1} Nhân, {2} Luồng @ {3} MHz)</td></tr>", rep.CPU.Name, rep.CPU.Cores, rep.CPU.LogicalProcessors, rep.CPU.MaxClockSpeedMhz));
                        sb.AppendLine(string.Format("<tr><td>Mainboard</td><td>{0} {1} (S/N: {2})</td></tr>", rep.Motherboard.Manufacturer, rep.Motherboard.Product, rep.Motherboard.SerialNumber));
                        sb.AppendLine(string.Format("<tr><td>BIOS</td><td>{0} ver {1} ({2})</td></tr>", rep.Motherboard.BiosVendor, rep.Motherboard.BiosVersion, rep.Motherboard.BiosReleaseDate));
                        sb.AppendLine(string.Format("<tr><td>RAM Tổng</td><td>{0:N0} MB (~{1} GB)</td></tr>", rep.TotalRamMb, Math.Round(rep.TotalRamMb / 1024.0, 1)));
                        sb.AppendLine("</table>");

                        // Peripherals Table
                        sb.AppendLine("<h2>🖨️ 3. Thiết Bị Ngoại Vi (Máy In, Scan, Vân Tay, USB)</h2>");
                        sb.AppendLine("<table>");
                        sb.AppendLine("<tr><th>Tên Thiết Bị</th><th>Phân Loại</th><th>Hãng Sản Xuất</th><th>ID Thiết Bị / Cổng</th><th>Trạng Thái</th></tr>");
                        foreach (var p in rep.Peripherals)
                        {
                            sb.AppendLine(string.Format("<tr><td>{0}</td><td>{1}</td><td>{2}</td><td>{3}</td><td>{4}</td></tr>",
                                p.Name, p.Category, p.Manufacturer, p.DeviceID, p.Status));
                        }
                        sb.AppendLine("</table>");

                        // Software Audit Table
                        sb.AppendLine("<h2>📦 4. Danh Sách Phần Mềm & Đánh Giá Bản Quyền</h2>");
                        sb.AppendLine(string.Format("<p>Tổng: <strong>{0}</strong> | Nghi vấn Crack: <strong style='color:#ef4444;'>{1}</strong> | Thương mại: <strong style='color:#fbbf24;'>{2}</strong> | Miễn phí: <strong style='color:#4ade80;'>{3}</strong></p>",
                            rep.TotalSoftwareCount, rep.CrackCount, rep.CommercialCount, rep.FreewareCount));
                        sb.AppendLine("<table>");
                        sb.AppendLine("<tr><th>Tên Phần Mềm</th><th>Phiên Bản</th><th>Nhà Phát Hành</th><th>Ngày Cài</th><th>Tình Trạng Bản Quyền</th><th>Ghi Chú</th></tr>");
                        foreach (var sw in rep.InstalledSoftware)
                        {
                            string rowClass = sw.LicenseCategory == SoftwareLicenseCategory.SuspectedCrack ? "crack-row" :
                                             sw.LicenseCategory == SoftwareLicenseCategory.Commercial ? "comm-row" :
                                             sw.LicenseCategory == SoftwareLicenseCategory.Freeware ? "free-row" : "";
                            sb.AppendLine(string.Format("<tr class='{0}'><td>{1}</td><td>{2}</td><td>{3}</td><td>{4}</td><td>{5}</td><td>{6}</td></tr>",
                                rowClass, sw.DisplayName, sw.DisplayVersion, sw.Publisher, sw.InstallDate, GetLicenseCategoryBadge(sw.LicenseCategory), sw.CrackNotes));
                        }
                        sb.AppendLine("</table>");

                        sb.AppendLine("<hr><p style='color:#64748b;font-size:12px;'>Báo cáo được tạo tự động bởi Hệ Thống LAN Control Multi-VLAN Management.</p>");
                        sb.AppendLine("</body></html>");

                        File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
                        MessageBox.Show("Đã xuất báo cáo HTML thành công:\n" + dlg.FileName, "Xuất Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        Process.Start(dlg.FileName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Lỗi khi xuất file: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void ExportReportToCsv()
        {
            if (_currentSelectedAgent == null || !_reports.ContainsKey(_currentSelectedAgent.AgentId))
            {
                MessageBox.Show("Chưa có dữ liệu kiểm kê để xuất CSV!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SystemAuditReport rep = _reports[_currentSelectedAgent.AgentId];

            using (SaveFileDialog dlg = new SaveFileDialog())
            {
                dlg.Title = "Xuất Danh Sách Phần Mềm & Bản Quyền (CSV)";
                dlg.Filter = "CSV File (*.csv)|*.csv|All Files (*.*)|*.*";
                dlg.FileName = string.Format("PhanMem_{0}_{1}.csv", rep.Hostname, DateTime.Now.ToString("yyyyMMdd_HHmm"));

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine("Máy Trạm,Địa Chỉ IP,VLAN,Tên Phần Mềm,Phiên Bản,Nhà Phát Hành,Ngày Cài Đặt,Trạng Thái Bản Quyền,Ghi Chú Đánh Giá");

                        foreach (var sw in rep.InstalledSoftware)
                        {
                            sb.AppendLine(string.Format("\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\",\"{6}\",\"{7}\",\"{8}\"",
                                rep.Hostname, rep.IpAddress, rep.VlanTag,
                                (sw.DisplayName ?? "").Replace("\"", "\"\""),
                                (sw.DisplayVersion ?? "").Replace("\"", "\"\""),
                                (sw.Publisher ?? "").Replace("\"", "\"\""),
                                sw.InstallDate,
                                GetLicenseCategoryBadge(sw.LicenseCategory),
                                (sw.CrackNotes ?? "").Replace("\"", "\"\"")));
                        }

                        File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
                        MessageBox.Show("Đã xuất danh sách phần mềm ra tệp CSV thành công:\n" + dlg.FileName, "Xuất Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Lỗi khi xuất file: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        #endregion

        #region Tab 6: History & Change Tracking

        private void InitializeTabHistory()
        {
            _tabHistory = new TabPage("🕒 Lịch Sử & Biến Động Cấu Hình");
            _tabHistory.BackColor = ModernTheme.BackgroundDark;
            _tabHistory.Padding = new Padding(10);

            // 1. Status Banner Panel
            _pnlHistoryBadge = new Panel
            {
                Dock = DockStyle.Top,
                Height = 85,
                BackColor = Color.FromArgb(28, 38, 48),
                Padding = new Padding(14, 10, 14, 10)
            };

            _lblHistoryBadgeTitle = new Label
            {
                Text = "🕒 ĐANG KIỂM TRA LỊCH SỬ THAY ĐỔI CẤU HÌNH...",
                Font = new Font("Segoe UI", 11.5F, FontStyle.Bold),
                ForeColor = ModernTheme.AccentCyan,
                AutoSize = true,
                Location = new Point(14, 10)
            };

            _lblHistoryBadgeDesc = new Label
            {
                Text = "Hệ thống tự động ghi nhận snapshot cấu hình để đối chiếu, phát hiện tháo trộm linh kiện hoặc cài phần mềm lạ.",
                Font = ModernTheme.RegularFont,
                ForeColor = ModernTheme.TextSecondary,
                AutoSize = true,
                Location = new Point(14, 38)
            };

            _pnlHistoryBadge.Controls.Add(_lblHistoryBadgeTitle);
            _pnlHistoryBadge.Controls.Add(_lblHistoryBadgeDesc);

            // 2. Control Toolbar
            Panel toolHistory = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = ModernTheme.PanelDark,
                Padding = new Padding(10, 8, 10, 8)
            };

            Label lblSelect = new Label
            {
                Text = "📅 Đối chiếu với lần quét:",
                AutoSize = true,
                Font = ModernTheme.SubHeaderFont,
                ForeColor = ModernTheme.TextPrimary,
                Location = new Point(10, 15)
            };

            _cboSnapshotHistory = new ComboBox
            {
                Location = new Point(220, 12),
                Width = 330,
                Font = ModernTheme.RegularFont,
                BackColor = ModernTheme.CardDark,
                ForeColor = ModernTheme.TextPrimary,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cboSnapshotHistory.SelectedIndexChanged += OnSnapshotHistorySelectionChanged;

            _lblHistorySummaryStats = new Label
            {
                Text = "Số biến động: 0",
                AutoSize = true,
                Font = ModernTheme.SubHeaderFont,
                ForeColor = ModernTheme.AccentYellow,
                Location = new Point(565, 15)
            };

            _btnExportHistoryMd = new Button
            {
                Text = "📝 Xuất File Lịch Sử (.md)",
                Size = new Size(230, 32),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(toolHistory.Width - 245, 9)
            };
            ModernTheme.ApplyToButton(_btnExportHistoryMd, Color.FromArgb(180, 80, 50), Color.White);
            _btnExportHistoryMd.Click += (s, e) => ExportHistoryToMarkdown();

            toolHistory.Controls.Add(lblSelect);
            toolHistory.Controls.Add(_cboSnapshotHistory);
            toolHistory.Controls.Add(_lblHistorySummaryStats);
            toolHistory.Controls.Add(_btnExportHistoryMd);

            toolHistory.Resize += (s, e) =>
            {
                _btnExportHistoryMd.Location = new Point(toolHistory.Width - _btnExportHistoryMd.Width - 12, 9);
            };

            // 3. DataGridView for Changes
            _gridHistoryChanges = CreateStyledGrid();
            _gridHistoryChanges.Columns.Add("No", "STT");
            _gridHistoryChanges.Columns["No"].Width = 50;
            _gridHistoryChanges.Columns.Add("Category", "Phân Loại");
            _gridHistoryChanges.Columns["Category"].Width = 150;
            _gridHistoryChanges.Columns.Add("ItemName", "Linh Kiện / Phần Mềm");
            _gridHistoryChanges.Columns["ItemName"].Width = 240;
            _gridHistoryChanges.Columns.Add("Action", "Hành Động Biến Động");
            _gridHistoryChanges.Columns["Action"].Width = 200;
            _gridHistoryChanges.Columns.Add("OldVal", "Cấu Hình Trước Đây");
            _gridHistoryChanges.Columns["OldVal"].Width = 260;
            _gridHistoryChanges.Columns.Add("NewVal", "Cấu Hình Hiện Tại");
            _gridHistoryChanges.Columns["NewVal"].Width = 260;
            _gridHistoryChanges.Columns.Add("Severity", "Mức Độ Đánh Giá");
            _gridHistoryChanges.Columns["Severity"].Width = 150;

            _tabHistory.Controls.Add(_gridHistoryChanges);
            _tabHistory.Controls.Add(toolHistory);
            _tabHistory.Controls.Add(_pnlHistoryBadge);
        }

        private static string GetHistoryDirectory()
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "audit_history");
            if (!Directory.Exists(dir))
            {
                try { Directory.CreateDirectory(dir); } catch { }
            }
            return dir;
        }

        private static string GetHistoryFilePath(string agentId)
        {
            string safeId = string.IsNullOrEmpty(agentId) ? "unknown" : agentId;
            foreach (char c in Path.GetInvalidFileNameChars()) safeId = safeId.Replace(c, '_');
            return Path.Combine(GetHistoryDirectory(), safeId + ".json");
        }

        private List<AuditSnapshotItem> LoadSnapshotHistory(string agentId)
        {
            string filePath = GetHistoryFilePath(agentId);
            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath, Encoding.UTF8);
                    List<AuditSnapshotItem> list = PacketSerializer.Deserialize<List<AuditSnapshotItem>>(json);
                    if (list != null) return list;
                }
                catch { }
            }
            return new List<AuditSnapshotItem>();
        }

        private void SaveSnapshotHistory(string agentId, List<AuditSnapshotItem> history)
        {
            try
            {
                string filePath = GetHistoryFilePath(agentId);
                string json = PacketSerializer.Serialize(history);
                File.WriteAllText(filePath, json, Encoding.UTF8);
            }
            catch { }
        }

        private void UpdateHistoryTab(SystemAuditReport currentReport)
        {
            if (currentReport == null || string.IsNullOrEmpty(currentReport.AgentId)) return;

            string agentId = currentReport.AgentId;
            List<AuditSnapshotItem> history = LoadSnapshotHistory(agentId);

            AuditSnapshotItem previousSnap = history.Count > 0 ? history[history.Count - 1] : null;

            bool shouldRecordNew = false;
            if (previousSnap == null)
            {
                shouldRecordNew = true;
            }
            else
            {
                TimeSpan diff = DateTime.Now - previousSnap.ScanTime;
                if (diff.TotalSeconds > 20)
                {
                    shouldRecordNew = true;
                }
            }

            if (shouldRecordNew)
            {
                AuditSnapshotItem newSnap = new AuditSnapshotItem
                {
                    SnapshotId = Guid.NewGuid().ToString("N"),
                    ScanTime = DateTime.Now,
                    Hostname = currentReport.Hostname,
                    IpAddress = currentReport.IpAddress,
                    VlanTag = currentReport.VlanTag,
                    Report = currentReport
                };
                history.Add(newSnap);
                if (history.Count > 60) history.RemoveAt(0);
                SaveSnapshotHistory(agentId, history);
            }

            // Populate combo box
            _cboSnapshotHistory.Items.Clear();
            if (history.Count <= 1)
            {
                _pnlHistoryBadge.BackColor = Color.FromArgb(20, 40, 60);
                _lblHistoryBadgeTitle.Text = "ℹ️ BẢN GHI ĐẦU TIÊN (BASELINE SNAPSHOT)";
                _lblHistoryBadgeTitle.ForeColor = ModernTheme.AccentCyan;
                _lblHistoryBadgeDesc.Text = string.Format("Đã lưu bản ghi cấu hình gốc cho máy trạm '{0}' ({1}). Ở các lần quét tiếp theo, hệ thống sẽ tự động so sánh và cảnh báo nếu có linh kiện hoặc phần mềm bị thay đổi.", currentReport.Hostname, DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
                _lblHistorySummaryStats.Text = "Lần đầu kiểm tra | Chưa có mốc đối chiếu";
                _gridHistoryChanges.Rows.Clear();
                _currentChangeRecords.Clear();
                _currentComparedSnapshot = null;
                _tabHistory.Text = "🕒 Lịch Sử & Biến Động";
                return;
            }

            // Populate all previous snapshots (excluding the latest one which is current)
            for (int i = history.Count - 2; i >= 0; i--)
            {
                _cboSnapshotHistory.Items.Add(history[i]);
            }

            if (_cboSnapshotHistory.Items.Count > 0)
            {
                _cboSnapshotHistory.SelectedIndex = 0;
            }
        }

        private void OnSnapshotHistorySelectionChanged(object sender, EventArgs e)
        {
            AuditSnapshotItem selected = _cboSnapshotHistory.SelectedItem as AuditSnapshotItem;
            if (selected == null || _currentSelectedAgent == null || !_reports.ContainsKey(_currentSelectedAgent.AgentId)) return;

            SystemAuditReport currentReport = _reports[_currentSelectedAgent.AgentId];
            _currentComparedSnapshot = selected;

            List<AuditChangeRecord> changes = CompareAuditReports(selected.Report, currentReport, selected.ScanTime, DateTime.Now);
            _currentChangeRecords = changes;

            _gridHistoryChanges.Rows.Clear();
            int stt = 1;
            int criticalCount = 0;
            int hwChanges = 0;
            int swChanges = 0;

            foreach (var c in changes)
            {
                int rowIdx = _gridHistoryChanges.Rows.Add(
                    stt++,
                    c.Category,
                    c.ItemName,
                    c.ChangeAction,
                    c.OldValue,
                    c.NewValue,
                    GetSeverityText(c.Severity)
                );

                DataGridViewRow row = _gridHistoryChanges.Rows[rowIdx];
                if (c.Severity == AuditChangeSeverity.Critical)
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(50, 20, 20);
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(255, 120, 120);
                    criticalCount++;
                }
                else if (c.Severity == AuditChangeSeverity.Warning)
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(45, 35, 15);
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(255, 190, 80);
                }
                else
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(18, 35, 25);
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(130, 230, 160);
                }

                if (c.Category.Contains("RAM") || c.Category.Contains("CPU") || c.Category.Contains("Ổ Đĩa") || c.Category.Contains("Mainboard") || c.Category.Contains("GPU"))
                {
                    hwChanges++;
                }
                else if (c.Category.Contains("Phần Mềm"))
                {
                    swChanges++;
                }
            }

            if (changes.Count > 0)
            {
                if (criticalCount > 0)
                {
                    _pnlHistoryBadge.BackColor = Color.FromArgb(64, 20, 20);
                    _lblHistoryBadgeTitle.Text = string.Format("🚨 CẢNH BÁO NGUY CƠ: PHÁT HIỆN {0} BIẾN ĐỘNG CẤU HÌNH (CÓ {1} MỤC NGUY CƠ CAO)!", changes.Count, criticalCount);
                    _lblHistoryBadgeTitle.ForeColor = ModernTheme.AccentRed;
                }
                else
                {
                    _pnlHistoryBadge.BackColor = Color.FromArgb(55, 40, 18);
                    _lblHistoryBadgeTitle.Text = string.Format("⚠️ CẢNH BÁO: PHÁT HIỆN CÓ {0} BIẾN ĐỘNG CẤU HÌNH SO VỚI LẦN QUÉT TRƯỚC!", changes.Count);
                    _lblHistoryBadgeTitle.ForeColor = Color.FromArgb(255, 190, 80);
                }

                _lblHistoryBadgeDesc.Text = string.Format("So sánh mốc trước ({0:dd/MM/yyyy HH:mm:ss}) với hiện tại ({1:dd/MM/yyyy HH:mm:ss}). Phần cứng biến động: {2} mục | Phần mềm biến động: {3} mục.",
                    selected.ScanTime, DateTime.Now, hwChanges, swChanges);

                _lblHistorySummaryStats.Text = string.Format("Số biến động: {0} (Phần cứng: {1} | Phần mềm: {2})", changes.Count, hwChanges, swChanges);
                _tabHistory.Text = string.Format("🕒 Lịch Sử ({0} Biến Động)", changes.Count);
            }
            else
            {
                _pnlHistoryBadge.BackColor = Color.FromArgb(18, 48, 30);
                _lblHistoryBadgeTitle.Text = "✅ CẤU HÌNH NGUYÊN VẸN: KHÔNG CÓ BẤT KỲ THAY ĐỔI NÀO!";
                _lblHistoryBadgeTitle.ForeColor = ModernTheme.AccentGreen;
                _lblHistoryBadgeDesc.Text = string.Format("Cấu hình phần cứng và phần mềm của máy trạm hoàn toàn trùng khớp 100% với lần kiểm tra trước ({0:dd/MM/yyyy HH:mm:ss}).", selected.ScanTime);
                _lblHistorySummaryStats.Text = "Cấu hình 100% nguyên vẹn";
                _tabHistory.Text = "🕒 Lịch Sử & Biến Động";
            }
        }

        private static string GetSeverityText(AuditChangeSeverity sev)
        {
            switch (sev)
            {
                case AuditChangeSeverity.Critical: return "🚨 Nghiêm trọng";
                case AuditChangeSeverity.Warning: return "⚠️ Cảnh báo";
                default: return "ℹ️ Thông tin";
            }
        }

        private List<AuditChangeRecord> CompareAuditReports(SystemAuditReport oldRep, SystemAuditReport newRep, DateTime oldTime, DateTime newTime)
        {
            List<AuditChangeRecord> diffs = new List<AuditChangeRecord>();
            if (oldRep == null || newRep == null) return diffs;

            // 1. Operating System
            if (!string.Equals(oldRep.OS.OsName, newRep.OS.OsName, StringComparison.OrdinalIgnoreCase))
            {
                diffs.Add(new AuditChangeRecord
                {
                    ChangeDetectedTime = newTime,
                    Category = "Hệ Điều Hành",
                    ItemName = "Hệ Điều Hành (OS)",
                    ChangeAction = "Thay đổi hệ điều hành",
                    OldValue = oldRep.OS.OsName,
                    NewValue = newRep.OS.OsName,
                    Severity = AuditChangeSeverity.Critical
                });
            }

            if (!string.Equals(oldRep.OS.BuildNumber, newRep.OS.BuildNumber, StringComparison.OrdinalIgnoreCase))
            {
                diffs.Add(new AuditChangeRecord
                {
                    ChangeDetectedTime = newTime,
                    Category = "Hệ Điều Hành",
                    ItemName = "Số Bản Dựng (Build)",
                    ChangeAction = "Cập nhật Windows Update",
                    OldValue = "Build " + oldRep.OS.BuildNumber,
                    NewValue = "Build " + newRep.OS.BuildNumber,
                    Severity = AuditChangeSeverity.Info
                });
            }

            // 2. Windows License
            if (!string.Equals(oldRep.OS.WindowsActivationStatus, newRep.OS.WindowsActivationStatus, StringComparison.OrdinalIgnoreCase))
            {
                diffs.Add(new AuditChangeRecord
                {
                    ChangeDetectedTime = newTime,
                    Category = "Bản Quyền Windows",
                    ItemName = "Trạng Thái Kích Hoạt",
                    ChangeAction = "Thay đổi kích hoạt bản quyền",
                    OldValue = oldRep.OS.WindowsActivationStatus,
                    NewValue = newRep.OS.WindowsActivationStatus,
                    Severity = AuditChangeSeverity.Warning
                });
            }

            if (!oldRep.OS.IsSuspectedCrack && newRep.OS.IsSuspectedCrack)
            {
                diffs.Add(new AuditChangeRecord
                {
                    ChangeDetectedTime = newTime,
                    Category = "Bản Quyền Windows",
                    ItemName = "Cảnh Báo Kích Hoạt Lậu",
                    ChangeAction = "🚨 Phát hiện nghi vấn bẻ khóa / Crack KMS!",
                    OldValue = "Bản quyền chuẩn",
                    NewValue = newRep.OS.CrackWarningDetails,
                    Severity = AuditChangeSeverity.Critical
                });
            }
            else if (oldRep.OS.IsSuspectedCrack && !newRep.OS.IsSuspectedCrack)
            {
                diffs.Add(new AuditChangeRecord
                {
                    ChangeDetectedTime = newTime,
                    Category = "Bản Quyền Windows",
                    ItemName = "Cảnh Báo Kích Hoạt Lậu",
                    ChangeAction = "✅ Đã gỡ bỏ bẻ khóa / Trở về bản quyền hợp lệ",
                    OldValue = oldRep.OS.CrackWarningDetails,
                    NewValue = "Bản quyền hợp lệ",
                    Severity = AuditChangeSeverity.Info
                });
            }

            // 3. CPU
            if (!string.Equals(oldRep.CPU.Name, newRep.CPU.Name, StringComparison.OrdinalIgnoreCase))
            {
                diffs.Add(new AuditChangeRecord
                {
                    ChangeDetectedTime = newTime,
                    Category = "Bộ Vi Xử Lý (CPU)",
                    ItemName = "Chip CPU",
                    ChangeAction = "Thay thế chip xử lý CPU",
                    OldValue = oldRep.CPU.Name,
                    NewValue = newRep.CPU.Name,
                    Severity = AuditChangeSeverity.Critical
                });
            }

            // 4. Motherboard & BIOS
            if (!string.Equals(oldRep.Motherboard.Product, newRep.Motherboard.Product, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(oldRep.Motherboard.SerialNumber, newRep.Motherboard.SerialNumber, StringComparison.OrdinalIgnoreCase))
            {
                diffs.Add(new AuditChangeRecord
                {
                    ChangeDetectedTime = newTime,
                    Category = "Bo Mạch Chủ",
                    ItemName = "Mainboard",
                    ChangeAction = "Thay thế Mainboard",
                    OldValue = string.Format("{0} {1} (S/N: {2})", oldRep.Motherboard.Manufacturer, oldRep.Motherboard.Product, oldRep.Motherboard.SerialNumber),
                    NewValue = string.Format("{0} {1} (S/N: {2})", newRep.Motherboard.Manufacturer, newRep.Motherboard.Product, newRep.Motherboard.SerialNumber),
                    Severity = AuditChangeSeverity.Critical
                });
            }

            if (!string.Equals(oldRep.Motherboard.BiosVersion, newRep.Motherboard.BiosVersion, StringComparison.OrdinalIgnoreCase))
            {
                diffs.Add(new AuditChangeRecord
                {
                    ChangeDetectedTime = newTime,
                    Category = "Bo Mạch Chủ",
                    ItemName = "Phiên Bản BIOS",
                    ChangeAction = "Cập nhật / Nâng cấp BIOS",
                    OldValue = oldRep.Motherboard.BiosVersion,
                    NewValue = newRep.Motherboard.BiosVersion,
                    Severity = AuditChangeSeverity.Info
                });
            }

            // 5. RAM (Total & Sticks)
            if (oldRep.TotalRamMb != newRep.TotalRamMb)
            {
                if (newRep.TotalRamMb < oldRep.TotalRamMb)
                {
                    diffs.Add(new AuditChangeRecord
                    {
                        ChangeDetectedTime = newTime,
                        Category = "Bộ Nhớ RAM",
                        ItemName = "Tổng Dung Lượng RAM",
                        ChangeAction = "🚨 SỤT GIẢM DUNG LƯỢNG RAM (Nghi vấn tháo trộm!)",
                        OldValue = string.Format("{0:N0} MB (~{1:0.0} GB)", oldRep.TotalRamMb, oldRep.TotalRamMb / 1024.0),
                        NewValue = string.Format("{0:N0} MB (~{1:0.0} GB)", newRep.TotalRamMb, newRep.TotalRamMb / 1024.0),
                        Severity = AuditChangeSeverity.Critical
                    });
                }
                else
                {
                    diffs.Add(new AuditChangeRecord
                    {
                        ChangeDetectedTime = newTime,
                        Category = "Bộ Nhớ RAM",
                        ItemName = "Tổng Dung Lượng RAM",
                        ChangeAction = "⚡ Nâng cấp dung lượng RAM",
                        OldValue = string.Format("{0:N0} MB (~{1:0.0} GB)", oldRep.TotalRamMb, oldRep.TotalRamMb / 1024.0),
                        NewValue = string.Format("{0:N0} MB (~{1:0.0} GB)", newRep.TotalRamMb, newRep.TotalRamMb / 1024.0),
                        Severity = AuditChangeSeverity.Info
                    });
                }
            }

            // RAM Sticks Diff
            var oldSticks = oldRep.RamSticks ?? new List<RamStickInfo>();
            var newSticks = newRep.RamSticks ?? new List<RamStickInfo>();

            foreach (var os in oldSticks)
            {
                string key = (os.DeviceLocator ?? "").Trim() + "_" + (os.SerialNumber ?? "").Trim();
                bool found = false;
                foreach (var ns in newSticks)
                {
                    string nKey = (ns.DeviceLocator ?? "").Trim() + "_" + (ns.SerialNumber ?? "").Trim();
                    if (string.Equals(key, nKey, StringComparison.OrdinalIgnoreCase)) { found = true; break; }
                }
                if (!found)
                {
                    diffs.Add(new AuditChangeRecord
                    {
                        ChangeDetectedTime = newTime,
                        Category = "Bộ Nhớ RAM",
                        ItemName = string.Format("Thanh RAM ({0})", os.DeviceLocator),
                        ChangeAction = "🚨 THÁO BỎ THANH RAM (Mất linh kiện!)",
                        OldValue = string.Format("{0}MB {1} (S/N: {2}, Part: {3})", os.CapacityMb, os.Manufacturer, os.SerialNumber, os.PartNumber),
                        NewValue = "[ĐÃ BỊ THÁO GỠ / THIẾU]",
                        Severity = AuditChangeSeverity.Critical
                    });
                }
            }

            foreach (var ns in newSticks)
            {
                string key = (ns.DeviceLocator ?? "").Trim() + "_" + (ns.SerialNumber ?? "").Trim();
                bool found = false;
                foreach (var os in oldSticks)
                {
                    string oKey = (os.DeviceLocator ?? "").Trim() + "_" + (os.SerialNumber ?? "").Trim();
                    if (string.Equals(key, oKey, StringComparison.OrdinalIgnoreCase)) { found = true; break; }
                }
                if (!found)
                {
                    diffs.Add(new AuditChangeRecord
                    {
                        ChangeDetectedTime = newTime,
                        Category = "Bộ Nhớ RAM",
                        ItemName = string.Format("Thanh RAM ({0})", ns.DeviceLocator),
                        ChangeAction = "⚡ Gắn thêm thanh RAM",
                        OldValue = "[Chưa gắn]",
                        NewValue = string.Format("{0}MB {1} (S/N: {2}, Part: {3})", ns.CapacityMb, ns.Manufacturer, ns.SerialNumber, ns.PartNumber),
                        Severity = AuditChangeSeverity.Info
                    });
                }
            }

            // 6. Physical Disks
            var oldDisks = oldRep.PhysicalDisks ?? new List<PhysicalDiskInfo>();
            var newDisks = newRep.PhysicalDisks ?? new List<PhysicalDiskInfo>();

            foreach (var od in oldDisks)
            {
                string key = (od.SerialNumber ?? "").Trim();
                if (string.IsNullOrEmpty(key)) key = (od.Model ?? "").Trim() + "_" + od.Index;
                bool found = false;
                foreach (var nd in newDisks)
                {
                    string nKey = (nd.SerialNumber ?? "").Trim();
                    if (string.IsNullOrEmpty(nKey)) nKey = (nd.Model ?? "").Trim() + "_" + nd.Index;
                    if (string.Equals(key, nKey, StringComparison.OrdinalIgnoreCase)) { found = true; break; }
                }
                if (!found)
                {
                    diffs.Add(new AuditChangeRecord
                    {
                        ChangeDetectedTime = newTime,
                        Category = "Ổ Đĩa Lưu Trữ",
                        ItemName = od.Model,
                        ChangeAction = "🚨 THÁO GỠ / MẤT Ổ ĐĨA VẬT LÝ",
                        OldValue = string.Format("{0:0.0} GB {1} (S/N: {2})", od.SizeGb, od.MediaType, od.SerialNumber),
                        NewValue = "[ĐÃ BỊ THÁO GỠ / MẤT KẾT NỐI]",
                        Severity = AuditChangeSeverity.Critical
                    });
                }
            }

            foreach (var nd in newDisks)
            {
                string key = (nd.SerialNumber ?? "").Trim();
                if (string.IsNullOrEmpty(key)) key = (nd.Model ?? "").Trim() + "_" + nd.Index;
                bool found = false;
                foreach (var od in oldDisks)
                {
                    string oKey = (od.SerialNumber ?? "").Trim();
                    if (string.IsNullOrEmpty(oKey)) oKey = (od.Model ?? "").Trim() + "_" + od.Index;
                    if (string.Equals(key, oKey, StringComparison.OrdinalIgnoreCase)) { found = true; break; }
                }
                if (!found)
                {
                    diffs.Add(new AuditChangeRecord
                    {
                        ChangeDetectedTime = newTime,
                        Category = "Ổ Đĩa Lưu Trữ",
                        ItemName = nd.Model,
                        ChangeAction = "💽 Gắn thêm ổ đĩa vật lý",
                        OldValue = "[Chưa có]",
                        NewValue = string.Format("{0:0.0} GB {1} (S/N: {2})", nd.SizeGb, nd.MediaType, nd.SerialNumber),
                        Severity = AuditChangeSeverity.Info
                    });
                }
            }

            // 7. Peripherals
            var oldPers = oldRep.Peripherals ?? new List<PeripheralDevice>();
            var newPers = newRep.Peripherals ?? new List<PeripheralDevice>();

            foreach (var op in oldPers)
            {
                string key = !string.IsNullOrEmpty(op.DeviceID) ? op.DeviceID.Trim() : op.Category + "_" + op.Name;
                bool found = false;
                foreach (var np in newPers)
                {
                    string nKey = !string.IsNullOrEmpty(np.DeviceID) ? np.DeviceID.Trim() : np.Category + "_" + np.Name;
                    if (string.Equals(key, nKey, StringComparison.OrdinalIgnoreCase)) { found = true; break; }
                }
                if (!found)
                {
                    diffs.Add(new AuditChangeRecord
                    {
                        ChangeDetectedTime = newTime,
                        Category = "Thiết Bị Ngoại Vi",
                        ItemName = op.Name,
                        ChangeAction = "🔌 Rút ra / Ngắt kết nối thiết bị",
                        OldValue = string.Format("{0} ({1})", op.Category, op.Manufacturer),
                        NewValue = "[ĐÃ NGẮT KẾT NỐI]",
                        Severity = AuditChangeSeverity.Warning
                    });
                }
            }

            foreach (var np in newPers)
            {
                string key = !string.IsNullOrEmpty(np.DeviceID) ? np.DeviceID.Trim() : np.Category + "_" + np.Name;
                bool found = false;
                foreach (var op in oldPers)
                {
                    string oKey = !string.IsNullOrEmpty(op.DeviceID) ? op.DeviceID.Trim() : op.Category + "_" + op.Name;
                    if (string.Equals(key, oKey, StringComparison.OrdinalIgnoreCase)) { found = true; break; }
                }
                if (!found)
                {
                    diffs.Add(new AuditChangeRecord
                    {
                        ChangeDetectedTime = newTime,
                        Category = "Thiết Bị Ngoại Vi",
                        ItemName = np.Name,
                        ChangeAction = "🔌 Cắm mới / Kết nối thiết bị",
                        OldValue = "[Chưa kết nối]",
                        NewValue = string.Format("{0} ({1})", np.Category, np.Manufacturer),
                        Severity = AuditChangeSeverity.Info
                    });
                }
            }

            // 8. Installed Software
            var oldSwList = oldRep.InstalledSoftware ?? new List<InstalledSoftwareInfo>();
            var newSwList = newRep.InstalledSoftware ?? new List<InstalledSoftwareInfo>();

            Dictionary<string, InstalledSoftwareInfo> oldSwMap = new Dictionary<string, InstalledSoftwareInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in oldSwList)
            {
                string k = (s.DisplayName ?? "").Trim();
                if (!string.IsNullOrEmpty(k) && !oldSwMap.ContainsKey(k)) oldSwMap[k] = s;
            }

            Dictionary<string, InstalledSoftwareInfo> newSwMap = new Dictionary<string, InstalledSoftwareInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in newSwList)
            {
                string k = (s.DisplayName ?? "").Trim();
                if (!string.IsNullOrEmpty(k) && !newSwMap.ContainsKey(k)) newSwMap[k] = s;
            }

            // New software installed
            foreach (var kvp in newSwMap)
            {
                if (!oldSwMap.ContainsKey(kvp.Key))
                {
                    var sw = kvp.Value;
                    bool isCrack = sw.LicenseCategory == SoftwareLicenseCategory.SuspectedCrack;
                    diffs.Add(new AuditChangeRecord
                    {
                        ChangeDetectedTime = newTime,
                        Category = "Phần Mềm Đã Cài",
                        ItemName = sw.DisplayName,
                        ChangeAction = isCrack ? "🚨 CÀI ĐẶT PHẦN MỀM NGHI CRACK" : "📦 Cài đặt phần mềm mới",
                        OldValue = "[Chưa cài đặt]",
                        NewValue = string.Format("v{0} ({1}) - {2}", sw.DisplayVersion, sw.Publisher, isCrack ? sw.CrackNotes : GetLicenseCategoryBadge(sw.LicenseCategory)),
                        Severity = isCrack ? AuditChangeSeverity.Critical : AuditChangeSeverity.Info
                    });
                }
                else
                {
                    // Check version update or license category change
                    var oldSw = oldSwMap[kvp.Key];
                    var newSw = kvp.Value;
                    if (!string.Equals(oldSw.DisplayVersion, newSw.DisplayVersion, StringComparison.OrdinalIgnoreCase))
                    {
                        diffs.Add(new AuditChangeRecord
                        {
                            ChangeDetectedTime = newTime,
                            Category = "Phần Mềm Đã Cài",
                            ItemName = newSw.DisplayName,
                            ChangeAction = "🔄 Cập nhật phiên bản phần mềm",
                            OldValue = "v" + oldSw.DisplayVersion,
                            NewValue = "v" + newSw.DisplayVersion,
                            Severity = AuditChangeSeverity.Info
                        });
                    }
                    if (oldSw.LicenseCategory != newSw.LicenseCategory)
                    {
                        bool turnedCrack = newSw.LicenseCategory == SoftwareLicenseCategory.SuspectedCrack;
                        diffs.Add(new AuditChangeRecord
                        {
                            ChangeDetectedTime = newTime,
                            Category = "Phần Mềm Đã Cài",
                            ItemName = newSw.DisplayName,
                            ChangeAction = turnedCrack ? "🚨 Biến đổi thành phần mềm CRACK!" : "Thay đổi phân loại bản quyền",
                            OldValue = GetLicenseCategoryBadge(oldSw.LicenseCategory),
                            NewValue = GetLicenseCategoryBadge(newSw.LicenseCategory),
                            Severity = turnedCrack ? AuditChangeSeverity.Critical : AuditChangeSeverity.Warning
                        });
                    }
                }
            }

            // Uninstalled software
            foreach (var kvp in oldSwMap)
            {
                if (!newSwMap.ContainsKey(kvp.Key))
                {
                    var sw = kvp.Value;
                    diffs.Add(new AuditChangeRecord
                    {
                        ChangeDetectedTime = newTime,
                        Category = "Phần Mềm Đã Cài",
                        ItemName = sw.DisplayName,
                        ChangeAction = "🗑️ Đã gỡ cài đặt phần mềm",
                        OldValue = string.Format("v{0} ({1})", sw.DisplayVersion, sw.Publisher),
                        NewValue = "[ĐÃ GỠ CÀI ĐẶT]",
                        Severity = AuditChangeSeverity.Warning
                    });
                }
            }

            return diffs;
        }

        #endregion

        #region Export Markdown (.md) Reports

        private static string EscapeMd(string val)
        {
            if (string.IsNullOrEmpty(val)) return "--";
            return val.Replace("\r\n", " ").Replace("\n", " ").Replace("|", "\\|").Trim();
        }

        private void ExportReportToMarkdown()
        {
            if (_currentSelectedAgent == null || !_reports.ContainsKey(_currentSelectedAgent.AgentId))
            {
                MessageBox.Show("Chưa có dữ liệu kiểm kê để xuất báo cáo Markdown!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SystemAuditReport rep = _reports[_currentSelectedAgent.AgentId];

            using (SaveFileDialog dlg = new SaveFileDialog())
            {
                dlg.Title = "Xuất Báo Cáo Cấu Hình & Bản Quyền Hệ Thống (Markdown)";
                dlg.Filter = "Markdown Document (*.md)|*.md|Text Document (*.txt)|*.txt|All Files (*.*)|*.*";
                dlg.FileName = string.Format("BaoCao_CauHinh_{0}_{1}.md", rep.Hostname, DateTime.Now.ToString("yyyyMMdd_HHmm"));

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine("# 🖥️ BÁO CÁO KIỂM KÊ CẤU HÌNH & BẢN QUYỀN HỆ THỐNG");
                        sb.AppendLine();
                        sb.AppendLine(string.Format("- **Tên Máy Trạm:** `{0}`", rep.Hostname));
                        sb.AppendLine(string.Format("- **Địa Chỉ IP:** `{0}`", rep.IpAddress));
                        sb.AppendLine(string.Format("- **Phân Đoạn VLAN:** `{0}`", rep.VlanTag));
                        sb.AppendLine(string.Format("- **Thời Gian Kiểm Kê:** `{0}`", rep.CollectedTime));
                        sb.AppendLine(string.Format("- **Mã Định Danh Agent:** `{0}`", rep.AgentId));
                        sb.AppendLine();
                        sb.AppendLine("---");
                        sb.AppendLine();

                        // Windows License Status
                        sb.AppendLine("## 🛡️ Tình Trạng Bản Quyền Windows");
                        sb.AppendLine();
                        if (rep.OS.IsSuspectedCrack)
                        {
                            sb.AppendLine("> [!WARNING]");
                            sb.AppendLine("> **CẢNH BÁO BẢN QUYỀN**: PHÁT HIỆN DẤU HIỆU BẺ KHÓA / GIẢ LẬP KMS!");
                            sb.AppendLine(string.Format("> - **Trạng thái:** {0}", rep.OS.WindowsActivationStatus));
                            sb.AppendLine(string.Format("> - **Kênh cấp phép:** {0}", rep.OS.WindowsLicenseChannel));
                            sb.AppendLine(string.Format("> - **Chi tiết cảnh báo:** {0}", rep.OS.CrackWarningDetails));
                        }
                        else if (rep.OS.WindowsActivationStatus != null && rep.OS.WindowsActivationStatus.IndexOf("Licensed", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            sb.AppendLine("> [!NOTE]");
                            sb.AppendLine("> **BẢN QUYỀN WINDOWS CHÍNH HÃNG HỢP LỆ (ACTIVATED)**");
                            sb.AppendLine(string.Format("> - **Trạng thái:** {0}", rep.OS.WindowsActivationStatus));
                            sb.AppendLine(string.Format("> - **Kênh cấp phép:** {0}", rep.OS.WindowsLicenseChannel));
                            if (!string.IsNullOrEmpty(rep.OS.PartialProductKey))
                                sb.AppendLine(string.Format("> - **Mã Khóa (Partial Key):** `***** - ***** - {0}`", rep.OS.PartialProductKey));
                        }
                        else
                        {
                            sb.AppendLine("> [!TIP]");
                            sb.AppendLine("> **WINDOWS CHƯA KÍCH HOẠT / DÙNG THỬ (NOT GENUINE)**");
                            sb.AppendLine(string.Format("> - **Trạng thái:** {0}", rep.OS.WindowsActivationStatus));
                            sb.AppendLine(string.Format("> - **Kênh cấp phép:** {0}", rep.OS.WindowsLicenseChannel));
                        }
                        sb.AppendLine();

                        // 1. OS Info
                        sb.AppendLine("## 📋 1. Thông Tin Hệ Điều Hành & Hệ Thống");
                        sb.AppendLine();
                        sb.AppendLine("| Thuộc Tính | Chi Tiết |");
                        sb.AppendLine("| :--- | :--- |");
                        sb.AppendLine(string.Format("| Tên Hệ Điều Hành (OS Name) | {0} |", EscapeMd(rep.OS.OsName)));
                        sb.AppendLine(string.Format("| Phiên Bản (Version) | {0} |", EscapeMd(rep.OS.Version)));
                        sb.AppendLine(string.Format("| Số Bản Dựng (Build Number) | {0} |", EscapeMd(rep.OS.BuildNumber)));
                        sb.AppendLine(string.Format("| Kiến Trúc Hệ Thống (Architecture) | {0} |", EscapeMd(rep.OS.Architecture)));
                        sb.AppendLine(string.Format("| Ngày Cài Đặt Windows | {0} |", EscapeMd(rep.OS.InstallDate)));
                        sb.AppendLine(string.Format("| Tên Máy Tính (Computer Name) | {0} |", EscapeMd(rep.OS.ComputerName)));
                        sb.AppendLine(string.Format("| Tài Khoản Đăng Nhập | {0} |", EscapeMd(rep.OS.RegisteredUser)));
                        sb.AppendLine(string.Format("| Mạng Miền (Domain/Workgroup) | {0} |", EscapeMd(rep.OS.DomainOrWorkgroup)));
                        sb.AppendLine();

                        // 2. Hardware Detail
                        sb.AppendLine("## ⚙️ 2. Cấu Hình Phần Cứng Chi Tiết");
                        sb.AppendLine();
                        sb.AppendLine("### 🧠 Bộ Vi Xử Lý (CPU)");
                        sb.AppendLine("| Thuộc Tính | Thông Số Kỹ Thuật |");
                        sb.AppendLine("| :--- | :--- |");
                        sb.AppendLine(string.Format("| Model Vi Xử Lý | {0} |", EscapeMd(rep.CPU.Name)));
                        sb.AppendLine(string.Format("| Hãng Sản Xuất | {0} |", EscapeMd(rep.CPU.Manufacturer)));
                        sb.AppendLine(string.Format("| Số Nhân / Số Luồng | {0} Nhân, {1} Luồng |", rep.CPU.Cores, rep.CPU.LogicalProcessors));
                        sb.AppendLine(string.Format("| Xung Nhịp Tối Đa | {0} MHz |", rep.CPU.MaxClockSpeedMhz));
                        sb.AppendLine(string.Format("| Kiến Trúc | {0} |", EscapeMd(rep.CPU.Architecture)));
                        sb.AppendLine(string.Format("| Processor ID | {0} |", EscapeMd(rep.CPU.ProcessorId)));
                        sb.AppendLine();

                        sb.AppendLine("### 🖲️ Bo Mạch Chủ (Mainboard) & BIOS");
                        sb.AppendLine("| Mục | Thông Tin |");
                        sb.AppendLine("| :--- | :--- |");
                        sb.AppendLine(string.Format("| Hãng Sản Xuất Mainboard | {0} |", EscapeMd(rep.Motherboard.Manufacturer)));
                        sb.AppendLine(string.Format("| Model Sản Phẩm | {0} |", EscapeMd(rep.Motherboard.Product)));
                        sb.AppendLine(string.Format("| Số Serial Number | {0} |", EscapeMd(rep.Motherboard.SerialNumber)));
                        sb.AppendLine(string.Format("| Nhà Cung Cấp BIOS | {0} |", EscapeMd(rep.Motherboard.BiosVendor)));
                        sb.AppendLine(string.Format("| Phiên Bản BIOS | {0} |", EscapeMd(rep.Motherboard.BiosVersion)));
                        sb.AppendLine(string.Format("| Ngày Phát Hành BIOS | {0} |", EscapeMd(rep.Motherboard.BiosReleaseDate)));
                        sb.AppendLine();

                        sb.AppendLine(string.Format("### ⚡ Chi Tiết Các Khe Cắm RAM (Tổng: {0:N0} MB ~ {1:0.0} GB)", rep.TotalRamMb, rep.TotalRamMb / 1024.0));
                        sb.AppendLine("| Khe Cắm | Dung Lượng | Tốc Độ Bus | Hãng Sản Xuất | Số Serial | Mã Part Number |");
                        sb.AppendLine("| :--- | :---: | :---: | :--- | :--- | :--- |");
                        foreach (var r in rep.RamSticks)
                        {
                            sb.AppendLine(string.Format("| {0} | {1} MB | {2} | {3} | {4} | {5} |",
                                EscapeMd(r.DeviceLocator), r.CapacityMb, r.SpeedMhz > 0 ? r.SpeedMhz + " MHz" : "--",
                                EscapeMd(r.Manufacturer), EscapeMd(r.SerialNumber), EscapeMd(r.PartNumber)));
                        }
                        sb.AppendLine();

                        sb.AppendLine("### 🎮 Card Đồ Họa (GPU) & Màn Hình");
                        sb.AppendLine("| Card Đồ Họa | Bộ Nhớ VRAM | Phiên Bản Driver | Độ Phân Giải |");
                        sb.AppendLine("| :--- | :--- | :--- | :--- |");
                        foreach (var g in rep.Gpus)
                        {
                            sb.AppendLine(string.Format("| {0} | {1} | {2} | {3} |",
                                EscapeMd(g.Name), g.AdapterRamMb > 0 ? g.AdapterRamMb + " MB" : "Chia sẻ hệ thống",
                                EscapeMd(g.DriverVersion), EscapeMd(g.CurrentResolution)));
                        }
                        sb.AppendLine();

                        sb.AppendLine("### 🌐 Card Mạng (Network Adapters)");
                        sb.AppendLine("| Tên Card Mạng | Địa Chỉ MAC | Địa Chỉ IP | Default Gateway | Tốc Độ | Trạng Thái |");
                        sb.AppendLine("| :--- | :--- | :--- | :--- | :---: | :---: |");
                        foreach (var n in rep.NetworkAdapters)
                        {
                            sb.AppendLine(string.Format("| {0} | {1} | {2} | {3} | {4} | {5} |",
                                EscapeMd(n.Name), EscapeMd(n.MacAddress), EscapeMd(n.IpAddresses),
                                EscapeMd(n.DefaultGateway), n.SpeedMbps > 0 ? n.SpeedMbps + " Mbps" : "--", EscapeMd(n.Status)));
                        }
                        sb.AppendLine();

                        // 3. Disks
                        sb.AppendLine("## 💾 3. Ổ Đĩa & Lưu Trữ");
                        sb.AppendLine();
                        sb.AppendLine("### 💽 Ổ Đĩa Vật Lý (Physical Disks)");
                        sb.AppendLine("| Thứ Tự | Model Ổ Đĩa | Chuẩn Giao Tiếp | Dung Lượng | Số Serial Number | Loại Media |");
                        sb.AppendLine("| :---: | :--- | :---: | :---: | :--- | :---: |");
                        foreach (var d in rep.PhysicalDisks)
                        {
                            sb.AppendLine(string.Format("| {0} | {1} | {2} | {3:0.0} GB | {4} | {5} |",
                                d.Index, EscapeMd(d.Model), EscapeMd(d.InterfaceType), d.SizeGb, EscapeMd(d.SerialNumber), EscapeMd(d.MediaType)));
                        }
                        sb.AppendLine();

                        sb.AppendLine("### 📁 Phân Vùng Ổ Đĩa (Logical Partitions)");
                        sb.AppendLine("| Ổ Đĩa | Tên Nhãn | Định Dạng | Tổng Dung Lượng | Dung Lượng Trống | Tỉ Lệ Trống |");
                        sb.AppendLine("| :---: | :--- | :---: | :---: | :---: | :---: |");
                        foreach (var l in rep.LogicalDrives)
                        {
                            sb.AppendLine(string.Format("| {0} | {1} | {2} | {3:0.0} GB | {4:0.0} GB | {5:0.0}% |",
                                EscapeMd(l.DriveLetter), EscapeMd(l.VolumeName), EscapeMd(l.FileSystem),
                                l.TotalSizeGb, l.FreeSpaceGb, l.FreePercent));
                        }
                        sb.AppendLine();

                        // 4. Peripherals
                        sb.AppendLine("## 🖨️ 4. Thiết Bị Ngoại Vi (Máy In, Scan, Vân Tay, USB...)");
                        sb.AppendLine();
                        sb.AppendLine("| Tên Thiết Bị / Model | Phân Loại | Hãng Sản Xuất | Mã Phần Cứng / Cổng | Trạng Thái | Mô Tả |");
                        sb.AppendLine("| :--- | :--- | :--- | :--- | :---: | :--- |");
                        foreach (var p in rep.Peripherals)
                        {
                            sb.AppendLine(string.Format("| {0} | {1} | {2} | {3} | {4} | {5} |",
                                EscapeMd(p.Name), EscapeMd(p.Category), EscapeMd(p.Manufacturer),
                                EscapeMd(p.DeviceID), EscapeMd(p.Status), EscapeMd(p.Description)));
                        }
                        sb.AppendLine();

                        // 5. Software
                        sb.AppendLine("## 📦 5. Danh Sách Phần Mềm Cài Đặt & Bản Quyền");
                        sb.AppendLine();
                        sb.AppendLine(string.Format("- **Tổng số phần mềm:** {0}", rep.TotalSoftwareCount));
                        sb.AppendLine(string.Format("- **⚠️ Nghi vấn Crack:** {0}", rep.CrackCount));
                        sb.AppendLine(string.Format("- **💼 Thương mại:** {0}", rep.CommercialCount));
                        sb.AppendLine(string.Format("- **🆓 Miễn phí:** {0}", rep.FreewareCount));
                        sb.AppendLine();

                        sb.AppendLine("| STT | Tên Phần Mềm Cài Đặt | Phiên Bản | Nhà Phát Hành | Ngày Cài Đặt | Bản Quyền | Ghi Chú Đánh Giá |");
                        sb.AppendLine("| :---: | :--- | :--- | :--- | :---: | :--- | :--- |");
                        int sIdx = 1;
                        foreach (var sw in rep.InstalledSoftware)
                        {
                            sb.AppendLine(string.Format("| {0} | {1} | {2} | {3} | {4} | {5} | {6} |",
                                sIdx++,
                                EscapeMd(sw.DisplayName),
                                EscapeMd(sw.DisplayVersion),
                                EscapeMd(sw.Publisher),
                                EscapeMd(sw.InstallDate),
                                GetLicenseCategoryBadge(sw.LicenseCategory),
                                EscapeMd(sw.CrackNotes)));
                        }
                        sb.AppendLine();
                        sb.AppendLine("<hr><p style='color:#64748b;font-size:12px;'>Báo cáo cấu hình được tạo tự động bởi Hệ Thống LAN Control Multi-VLAN Management.</p>");

                        File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
                        MessageBox.Show("Đã xuất báo cáo chi tiết Markdown (.md) thành công:\n" + dlg.FileName, "Xuất Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        Process.Start(dlg.FileName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Lỗi khi xuất file Markdown: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void ExportHistoryToMarkdown()
        {
            if (_currentSelectedAgent == null || !_reports.ContainsKey(_currentSelectedAgent.AgentId))
            {
                MessageBox.Show("Chưa có dữ liệu kiểm kê để xuất lịch sử!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SystemAuditReport rep = _reports[_currentSelectedAgent.AgentId];
            if (_currentComparedSnapshot == null && _currentChangeRecords.Count == 0)
            {
                MessageBox.Show("Chưa có dữ liệu so sánh biến động lịch sử để xuất báo cáo!\nVui lòng chọn một mốc quét trong danh sách để so sánh.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (SaveFileDialog dlg = new SaveFileDialog())
            {
                dlg.Title = "Xuất Báo Cáo Lịch Sử & Biến Động Cấu Hình (Markdown)";
                dlg.Filter = "Markdown Document (*.md)|*.md|Text Document (*.txt)|*.txt|All Files (*.*)|*.*";
                dlg.FileName = string.Format("LichSu_BienDong_{0}_{1}.md", rep.Hostname, DateTime.Now.ToString("yyyyMMdd_HHmm"));

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine("# 🕒 BÁO CÁO BIẾN ĐỘNG & LỊCH SỬ THAY ĐỔI CẤU HÌNH MÁY TRẠM");
                        sb.AppendLine();
                        sb.AppendLine(string.Format("- **Tên Máy Trạm:** `{0}`", rep.Hostname));
                        sb.AppendLine(string.Format("- **Địa Chỉ IP:** `{0}`", rep.IpAddress));
                        sb.AppendLine(string.Format("- **Phân Đoạn VLAN:** `{0}`", rep.VlanTag));
                        sb.AppendLine(string.Format("- **Lần Kiểm Tra Hiện Tại:** `{0}`", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")));
                        if (_currentComparedSnapshot != null)
                        {
                            sb.AppendLine(string.Format("- **Đối Chiếu Với Lần Trước:** `{0:dd/MM/yyyy HH:mm:ss}`", _currentComparedSnapshot.ScanTime));
                        }
                        sb.AppendLine();
                        sb.AppendLine("---");
                        sb.AppendLine();

                        // Callout Alert
                        if (_currentChangeRecords.Count > 0)
                        {
                            bool hasCritical = _currentChangeRecords.Exists(c => c.Severity == AuditChangeSeverity.Critical);
                            if (hasCritical)
                            {
                                sb.AppendLine("> [!CAUTION]");
                                sb.AppendLine(string.Format("> **CẢNH BÁO NGUY CƠ**: Phát hiện {0} thay đổi cấu hình, trong đó có cảnh báo phần cứng bị tháo gỡ hoặc nghi vấn phần mềm crack!", _currentChangeRecords.Count));
                            }
                            else
                            {
                                sb.AppendLine("> [!WARNING]");
                                sb.AppendLine(string.Format("> **PHÁT HIỆN BIẾN ĐỘNG**: Có {0} thay đổi cấu hình so với lần kiểm tra trước.", _currentChangeRecords.Count));
                            }
                        }
                        else
                        {
                            sb.AppendLine("> [!NOTE]");
                            sb.AppendLine("> **CẤU HÌNH NGUYÊN VẸN**: Toàn bộ cấu hình phần cứng và phần mềm hoàn toàn trùng khớp với mốc quét trước, không có biến động.");
                        }
                        sb.AppendLine();

                        // Summary stats table
                        int hwCount = 0;
                        int swNewCount = 0;
                        int swDelCount = 0;
                        int swUpdCount = 0;
                        int otherCount = 0;

                        foreach (var c in _currentChangeRecords)
                        {
                            if (c.Category.Contains("RAM") || c.Category.Contains("CPU") || c.Category.Contains("Ổ Đĩa") || c.Category.Contains("Mainboard") || c.Category.Contains("GPU"))
                                hwCount++;
                            else if (c.ChangeAction.Contains("mới"))
                                swNewCount++;
                            else if (c.ChangeAction.Contains("gỡ"))
                                swDelCount++;
                            else if (c.ChangeAction.Contains("Cập nhật"))
                                swUpdCount++;
                            else
                                otherCount++;
                        }

                        sb.AppendLine("### 📊 Thống Kê Thay Đổi Cấu Hình");
                        sb.AppendLine();
                        sb.AppendLine("| Hạng Mục | Số Lượng Biến Động |");
                        sb.AppendLine("| :--- | :--- |");
                        sb.AppendLine(string.Format("| **Tổng số thay đổi phát hiện** | **{0}** |", _currentChangeRecords.Count));
                        sb.AppendLine(string.Format("| Biến động phần cứng (RAM, CPU, Ổ đĩa) | {0} |", hwCount));
                        sb.AppendLine(string.Format("| Phần mềm mới cài đặt | {0} |", swNewCount));
                        sb.AppendLine(string.Format("| Phần mềm đã bị gỡ bỏ | {0} |", swDelCount));
                        sb.AppendLine(string.Format("| Phần mềm cập nhật phiên bản | {0} |", swUpdCount));
                        sb.AppendLine(string.Format("| Biến động khác (Ngoại vi, Bản quyền, Windows) | {0} |", otherCount));
                        sb.AppendLine();
                        sb.AppendLine("---");
                        sb.AppendLine();

                        // Detailed Table
                        sb.AppendLine("## 📝 Chi Tiết Các Mục Biến Động");
                        sb.AppendLine();
                        if (_currentChangeRecords.Count == 0)
                        {
                            sb.AppendLine("*Không có biến động nào được ghi nhận giữa hai lần kiểm tra.*");
                        }
                        else
                        {
                            sb.AppendLine("| STT | Phân Loại | Linh Kiện / Ứng Dụng | Hành Động | Cấu Hình Trước Đây | Cấu Hình Hiện Tại | Mức Độ |");
                            sb.AppendLine("| :---: | :--- | :--- | :--- | :--- | :--- | :--- |");
                            int idx = 1;
                            foreach (var c in _currentChangeRecords)
                            {
                                sb.AppendLine(string.Format("| {0} | {1} | **{2}** | {3} | {4} | {5} | {6} |",
                                    idx++,
                                    EscapeMd(c.Category),
                                    EscapeMd(c.ItemName),
                                    EscapeMd(c.ChangeAction),
                                    EscapeMd(c.OldValue),
                                    EscapeMd(c.NewValue),
                                    GetSeverityText(c.Severity)));
                            }
                        }
                        sb.AppendLine();
                        sb.AppendLine("---");
                        sb.AppendLine();

                        // Scan history timeline
                        List<AuditSnapshotItem> allHistory = LoadSnapshotHistory(rep.AgentId);
                        sb.AppendLine("## 📅 Lịch Sử Các Lần Kiểm Kê Đã Lưu Trữ");
                        sb.AppendLine();
                        sb.AppendLine("| Lần Quét | Thời Điểm | Địa Chỉ IP | Tên Máy | Số Lượng Phần Mềm | Ghi Chú |");
                        sb.AppendLine("| :---: | :--- | :--- | :--- | :---: | :--- |");
                        int hIdx = 1;
                        foreach (var snap in allHistory)
                        {
                            sb.AppendLine(string.Format("| {0} | {1:dd/MM/yyyy HH:mm:ss} | {2} | {3} | {4} | {5} |",
                                hIdx,
                                snap.ScanTime,
                                snap.IpAddress,
                                snap.Hostname,
                                snap.Report != null ? snap.Report.TotalSoftwareCount : 0,
                                hIdx == 1 ? "Bản ghi gốc (Baseline)" : "Kiểm tra định kỳ"));
                            hIdx++;
                        }
                        sb.AppendLine();
                        sb.AppendLine("<hr><p style='color:#64748b;font-size:12px;'>Báo cáo biến động cấu hình được tạo tự động bởi Hệ Thống LAN Control Multi-VLAN Management.</p>");

                        File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
                        MessageBox.Show("Đã xuất file lịch sử thay đổi Markdown (.md) thành công:\n" + dlg.FileName, "Xuất Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        Process.Start(dlg.FileName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Lỗi khi xuất file lịch sử: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        #endregion
    }

    #region Audit History Data Models

    [Serializable]
    public class AuditSnapshotItem
    {
        public string SnapshotId { get; set; }
        public DateTime ScanTime { get; set; }
        public string Hostname { get; set; }
        public string IpAddress { get; set; }
        public string VlanTag { get; set; }
        public SystemAuditReport Report { get; set; }

        public override string ToString()
        {
            return string.Format("{0:dd/MM/yyyy HH:mm:ss} - {1} ({2})", ScanTime, Hostname, IpAddress);
        }
    }

    public enum AuditChangeSeverity
    {
        Info,
        Warning,
        Critical
    }

    public class AuditChangeRecord
    {
        public DateTime ChangeDetectedTime { get; set; }
        public string Category { get; set; }
        public string ItemName { get; set; }
        public string ChangeAction { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public AuditChangeSeverity Severity { get; set; }
    }

    #endregion
}
