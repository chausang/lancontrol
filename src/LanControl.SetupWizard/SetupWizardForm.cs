using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace LanControl.SetupWizard
{
    public class SetupWizardForm : Form
    {
        private InstallConfig _config;
        private InstallerEngine _engine;
        private Timer _statusTimer;
        private bool _isInstalling = false;
        private bool _isUpdatingEditionSelection = false;

        // Top Banner
        private Panel _pnlHeader;
        private Label _lblTitle;
        private Label _lblSubtitle;
        private Label _lblAgentBadge;
        private Label _lblGatewayBadge;

        // Section 1: Edition Cards (Agent / Server / Full)
        private Panel _cardAgent;
        private Panel _cardServer;
        private Panel _cardFull;
        private RadioButton _radAgent;
        private RadioButton _radServer;
        private RadioButton _radFull;

        // Section 2: Target Directory & Source Status
        private TextBox _txtTargetFolder;
        private Button _btnBrowseFolder;
        private Button _btnOpenFolder;
        private Label _lblCalculatedPath;
        private Label _lblSourceStatus;

        // Section 3A: Agent Configuration
        private GroupBox _grpAgentConfig;
        private TextBox _txtGatewayHost;
        private Button _btnUseLocalIp;
        private TextBox _txtGatewayPort;
        private Button _btnResetAgentPort;
        private ComboBox _cboVlan;
        private CheckBox _chkAgentAutoStart;
        private CheckBox _chkUacSupport;

        // Section 3B: Server Configuration
        private GroupBox _grpServerConfig;
        private TextBox _txtServerPort;
        private Button _btnResetServerPort;
        private ComboBox _cboServerIps;
        private Button _btnCopyServerIp;
        private CheckBox _chkFirewall;
        private CheckBox _chkServerAutoStart;
        private Button _btnOpenConsole;
        private Button _btnOpenGateway;
        private Button _btnStopServices;

        // Section 4: Common Options & Progress Bar
        private CheckBox _chkDesktopShortcut;
        private CheckBox _chkLaunchAfterInstall;
        private ProgressBar _progressBar;
        private Label _lblProgressStatus;

        // Bottom Action Bar
        private Panel _pnlBottom;
        private Button _btnInstallOneClick;
        private Button _btnSaveConfigOnly;
        private Button _btnUninstall;
        private Button _btnClose;

        public SetupWizardForm() : this(InstallEdition.FullSuite)
        {
        }

        public SetupWizardForm(InstallEdition initialEdition)
        {
            _config = new InstallConfig();
            _engine = new InstallerEngine();
            _engine.ProgressChanged += OnInstallProgressChanged;

            InitializeComponent();
            ApplyTheme();
            InitializeData();
            SelectEdition(initialEdition);
            UpdateRunningStatus();

            _statusTimer = new Timer { Interval = 2500 };
            _statusTimer.Tick += (s, e) => UpdateRunningStatus();
            _statusTimer.Start();
        }

        private void InitializeComponent()
        {
            this.Text = "Trình Cài Đặt 1-Click LAN Control Multi-VLAN Suite (Agent / Server / Full)";
            this.Size = new Size(900, 770);
            this.MinimumSize = new Size(900, 770);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            string iconPath = InstallerEngine.ResolveSourceFile("icon.ico");
            if (!string.IsNullOrEmpty(iconPath) && File.Exists(iconPath))
            {
                try { this.Icon = new Icon(iconPath); } catch { }
            }

            // 1. TOP BANNER
            _pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 68,
                BackColor = ModernTheme.PanelDark,
                Padding = new Padding(18, 10, 18, 10)
            };

            _lblTitle = new Label
            {
                Text = "⚡ TRÌNH CÀI ĐẶT 1-CLICK LAN CONTROL MULTI-VLAN SUITE",
                Font = ModernTheme.HeaderFont,
                ForeColor = ModernTheme.AccentCyan,
                AutoSize = true,
                Location = new Point(16, 10)
            };

            _lblSubtitle = new Label
            {
                Text = "Chọn cài đặt Máy Trạm (Agent), Máy Chủ (Server) hoặc Cả Hai — Thiết lập & khởi chạy chỉ với 1 cú click.",
                UseMnemonic = false,
                Font = ModernTheme.RegularFont,
                ForeColor = ModernTheme.TextSecondary,
                AutoSize = true,
                Location = new Point(18, 38)
            };

            _lblAgentBadge = new Label
            {
                Text = "⚪ Agent: Đang kiểm tra...",
                Font = ModernTheme.SmallFont,
                ForeColor = ModernTheme.TextSecondary,
                BackColor = ModernTheme.CardDark,
                Padding = new Padding(8, 4, 8, 4),
                AutoSize = true,
                Location = new Point(685, 10)
            };

            _lblGatewayBadge = new Label
            {
                Text = "⚪ Gateway: Đang kiểm tra...",
                Font = ModernTheme.SmallFont,
                ForeColor = ModernTheme.TextSecondary,
                BackColor = ModernTheme.CardDark,
                Padding = new Padding(8, 4, 8, 4),
                AutoSize = true,
                Location = new Point(685, 36)
            };

            _pnlHeader.Controls.Add(_lblTitle);
            _pnlHeader.Controls.Add(_lblSubtitle);
            _pnlHeader.Controls.Add(_lblAgentBadge);
            _pnlHeader.Controls.Add(_lblGatewayBadge);

            // 2. BOTTOM ACTION BAR
            _pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 66,
                BackColor = ModernTheme.PanelDark,
                Padding = new Padding(16, 10, 16, 10)
            };

            _btnClose = new Button
            {
                Text = "✕ Đóng",
                Size = new Size(85, 44),
                Location = new Point(16, 11),
                UseMnemonic = false
            };
            ModernTheme.ApplyToButton(_btnClose, ModernTheme.CardDark, ModernTheme.TextSecondary);
            _btnClose.Click += (s, e) => this.Close();

            _btnUninstall = new Button
            {
                Text = "🗑️ Gỡ Cài Đặt",
                Size = new Size(155, 44),
                Location = new Point(108, 11),
                UseMnemonic = false
            };
            ModernTheme.ApplyToButton(_btnUninstall, ModernTheme.CardDark, ModernTheme.AccentRed);
            _btnUninstall.Click += OnUninstallClicked;

            _btnSaveConfigOnly = new Button
            {
                Text = "💾 Chỉ Lưu Cấu Hình",
                Size = new Size(150, 44),
                Location = new Point(270, 11),
                UseMnemonic = false
            };
            ModernTheme.ApplyToButton(_btnSaveConfigOnly, ModernTheme.CardDark, ModernTheme.TextPrimary);
            _btnSaveConfigOnly.Click += OnSaveConfigOnlyClicked;

            _btnInstallOneClick = new Button
            {
                Text = "🚀 CÀI ĐẶT & KHỞI CHẠY NGAY (1-CLICK)",
                Size = new Size(440, 44),
                Location = new Point(428, 11),
                UseMnemonic = false
            };
            ModernTheme.ApplyToButton(_btnInstallOneClick, ModernTheme.AccentGreen, Color.FromArgb(12, 18, 24));
            _btnInstallOneClick.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            _btnInstallOneClick.Click += OnInstallOneClickClicked;

            _pnlBottom.Controls.Add(_btnClose);
            _pnlBottom.Controls.Add(_btnUninstall);
            _pnlBottom.Controls.Add(_btnSaveConfigOnly);
            _pnlBottom.Controls.Add(_btnInstallOneClick);

            // 3. MAIN BODY CONTENT
            Panel pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ModernTheme.BackgroundDark,
                Padding = new Padding(16, 10, 16, 10)
            };

            // SECTION 1: CHỌN PHIÊN BẢN CÀI ĐẶT (3 CARDS)
            Label lblSec1 = new Label
            {
                Text = "1. CHỌN THÀNH PHẦN CÀI ĐẶT (NHẤP CHỌN 1 TRONG 3 CHẾ ĐỘ):",
                Font = ModernTheme.SubHeaderFont,
                ForeColor = ModernTheme.AccentCyan,
                AutoSize = true,
                Location = new Point(16, 8)
            };
            pnlBody.Controls.Add(lblSec1);

            _cardAgent = CreateEditionCard(16, 32, 274, 118, out _radAgent,
                "🖥️ Chỉ Cài Agent (Máy Trạm)",
                "Dành cho máy tính học viên, nhân viên phòng ban.\n" +
                "• Tệp: LanControl.Agent.exe, Core.dll\n" +
                "• Chạy ngầm khay hệ thống, kết nối về Server.",
                InstallEdition.AgentClient);

            _cardServer = CreateEditionCard(303, 32, 274, 118, out _radServer,
                "🏢 Chỉ Cài Server (Máy Chủ)",
                "Dành cho máy giáo viên, quản trị viên CNTT.\n" +
                "• Tệp: Gateway.exe, Control.exe, Core.dll\n" +
                "• Điều khiển đa VLAN, phát sóng, gửi file.",
                InstallEdition.ServerMaster);

            _cardFull = CreateEditionCard(590, 32, 278, 118, out _radFull,
                "🌟 Cài Cả 2 (Agent + Server)",
                "Bộ đầy đủ (Full Suite) trên cùng 1 máy tính.\n" +
                "• Tệp: Agent + Gateway + Master Control\n" +
                "• Vừa quản trị vừa nhận lệnh / kiểm thử.",
                InstallEdition.FullSuite);

            pnlBody.Controls.Add(_cardAgent);
            pnlBody.Controls.Add(_cardServer);
            pnlBody.Controls.Add(_cardFull);

            // SECTION 2: THƯ MỤC LƯU TRỮ & NGUỒN TỆP
            GroupBox grpFolder = new GroupBox
            {
                Text = "2. NƠI LƯU TRỮ CÀI ĐẶT && TRẠNG THÁI TỆP NGUỒN",
                Font = ModernTheme.SubHeaderFont,
                ForeColor = ModernTheme.AccentCyan,
                BackColor = ModernTheme.PanelDark,
                Location = new Point(16, 158),
                Size = new Size(852, 86)
            };

            Label lblDir = new Label
            {
                Text = "📁 Thư mục đích:",
                Font = ModernTheme.RegularFont,
                ForeColor = ModernTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(14, 26)
            };

            _txtTargetFolder = new TextBox
            {
                Location = new Point(125, 23),
                Width = 455,
                Font = ModernTheme.RegularFont,
                BackColor = ModernTheme.CardDark,
                ForeColor = ModernTheme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle
            };
            _txtTargetFolder.TextChanged += (s, e) => UpdateCalculatedTarget();

            _btnBrowseFolder = new Button
            {
                Text = "📂 Chọn Nơi Lưu...",
                Location = new Point(588, 21),
                Size = new Size(130, 27)
            };
            ModernTheme.ApplyToButton(_btnBrowseFolder, ModernTheme.AccentBlue, Color.White);
            _btnBrowseFolder.Font = ModernTheme.SmallFont;
            _btnBrowseFolder.Click += OnBrowseFolderClicked;

            _btnOpenFolder = new Button
            {
                Text = "📂 Mở Thư Mục",
                Location = new Point(724, 21),
                Size = new Size(114, 27)
            };
            ModernTheme.ApplyToButton(_btnOpenFolder, ModernTheme.CardDark, ModernTheme.TextPrimary);
            _btnOpenFolder.Font = ModernTheme.SmallFont;
            _btnOpenFolder.Click += OnOpenFolderClicked;

            _lblCalculatedPath = new Label
            {
                Text = "➜ Đường dẫn cài đặt thực tế: C:\\Program Files\\lancontrol",
                Font = ModernTheme.SmallFont,
                ForeColor = ModernTheme.AccentGreen,
                AutoSize = true,
                Location = new Point(14, 56)
            };

            _lblSourceStatus = new Label
            {
                Text = "✅ Đang kiểm tra tệp nguồn...",
                UseMnemonic = false,
                Font = ModernTheme.SmallFont,
                ForeColor = ModernTheme.AccentYellow,
                AutoSize = true,
                Location = new Point(440, 56)
            };

            grpFolder.Controls.Add(lblDir);
            grpFolder.Controls.Add(_txtTargetFolder);
            grpFolder.Controls.Add(_btnBrowseFolder);
            grpFolder.Controls.Add(_btnOpenFolder);
            grpFolder.Controls.Add(_lblCalculatedPath);
            grpFolder.Controls.Add(_lblSourceStatus);
            pnlBody.Controls.Add(grpFolder);

            // SECTION 3A: CẤU HÌNH AGENT (LEFT)
            _grpAgentConfig = new GroupBox
            {
                Text = "3A. CẤU HÌNH MÁY TRẠM (AGENT CLIENT)",
                Font = ModernTheme.SubHeaderFont,
                ForeColor = ModernTheme.AccentCyan,
                BackColor = ModernTheme.PanelDark,
                Location = new Point(16, 252),
                Size = new Size(420, 215)
            };

            Label lblGwHost = new Label
            {
                Text = "🌐 IP Máy Chủ Gateway:",
                Font = ModernTheme.RegularFont,
                ForeColor = ModernTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(14, 28)
            };

            _txtGatewayHost = new TextBox
            {
                Text = "127.0.0.1",
                Location = new Point(165, 25),
                Width = 145,
                Font = ModernTheme.RegularFont,
                BackColor = ModernTheme.CardDark,
                ForeColor = ModernTheme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle
            };

            _btnUseLocalIp = new Button
            {
                Text = "🏠 IP Máy Này",
                Location = new Point(316, 24),
                Size = new Size(92, 25)
            };
            ModernTheme.ApplyToButton(_btnUseLocalIp, ModernTheme.CardDark, ModernTheme.AccentCyan);
            _btnUseLocalIp.Font = ModernTheme.SmallFont;
            _btnUseLocalIp.Click += (s, e) =>
            {
                if (_cboServerIps.SelectedItem != null)
                {
                    string raw = _cboServerIps.SelectedItem.ToString();
                    _txtGatewayHost.Text = raw.Split(' ')[0].Trim();
                }
                else
                {
                    _txtGatewayHost.Text = "127.0.0.1";
                }
            };

            Label lblGwPort = new Label
            {
                Text = "🔌 Cổng kết nối (Port):",
                Font = ModernTheme.RegularFont,
                ForeColor = ModernTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(14, 62)
            };

            _txtGatewayPort = new TextBox
            {
                Text = "8443",
                Location = new Point(165, 59),
                Width = 75,
                Font = ModernTheme.RegularFont,
                BackColor = ModernTheme.CardDark,
                ForeColor = ModernTheme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle
            };

            _btnResetAgentPort = new Button
            {
                Text = "🔄 Mặc định 8443",
                Location = new Point(248, 58),
                Size = new Size(115, 25)
            };
            ModernTheme.ApplyToButton(_btnResetAgentPort, ModernTheme.CardDark, ModernTheme.TextSecondary);
            _btnResetAgentPort.Font = ModernTheme.SmallFont;
            _btnResetAgentPort.Click += (s, e) => _txtGatewayPort.Text = "8443";

            Label lblVlan = new Label
            {
                Text = "🏷️ Phân đoạn VLAN:",
                Font = ModernTheme.RegularFont,
                ForeColor = ModernTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(14, 96)
            };

            _cboVlan = new ComboBox
            {
                Location = new Point(165, 93),
                Width = 243,
                Font = ModernTheme.RegularFont,
                BackColor = ModernTheme.CardDark,
                ForeColor = ModernTheme.TextPrimary,
                DropDownStyle = ComboBoxStyle.DropDown
            };
            _cboVlan.Items.AddRange(new object[]
            {
                "VLAN 10 - Phòng Kỹ Thuật",
                "VLAN 20 - Phòng Kế Toán",
                "VLAN 30 - Phòng Đào Tạo",
                "Phòng Máy Tính 1",
                "Phòng Máy Tính 2",
                "Khoa Khám Bệnh",
                "Khoa Cấp Cứu",
                "Khu Vực Văn Phòng"
            });
            _cboVlan.SelectedIndex = 0;

            _chkAgentAutoStart = new CheckBox
            {
                Text = "Tự động khởi chạy Agent ngầm khi bật Windows",
                Checked = true,
                Font = ModernTheme.RegularFont,
                ForeColor = ModernTheme.AccentGreen,
                AutoSize = true,
                Location = new Point(16, 134)
            };

            _chkUacSupport = new CheckBox
            {
                Text = "🛡️ Hỗ trợ điều khiển xuyên qua cửa sổ Admin UAC (Tắt UIPI)",
                Checked = true,
                Font = ModernTheme.SmallFont,
                ForeColor = ModernTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(16, 166)
            };

            _grpAgentConfig.Controls.Add(lblGwHost);
            _grpAgentConfig.Controls.Add(_txtGatewayHost);
            _grpAgentConfig.Controls.Add(_btnUseLocalIp);
            _grpAgentConfig.Controls.Add(lblGwPort);
            _grpAgentConfig.Controls.Add(_txtGatewayPort);
            _grpAgentConfig.Controls.Add(_btnResetAgentPort);
            _grpAgentConfig.Controls.Add(lblVlan);
            _grpAgentConfig.Controls.Add(_cboVlan);
            _grpAgentConfig.Controls.Add(_chkAgentAutoStart);
            _grpAgentConfig.Controls.Add(_chkUacSupport);
            pnlBody.Controls.Add(_grpAgentConfig);

            // SECTION 3B: CẤU HÌNH SERVER (RIGHT)
            _grpServerConfig = new GroupBox
            {
                Text = "3B. CẤU HÌNH MÁY CHỦ (SERVER && MASTER CONSOLE)",
                Font = ModernTheme.SubHeaderFont,
                ForeColor = ModernTheme.AccentGreen,
                BackColor = ModernTheme.PanelDark,
                Location = new Point(448, 252),
                Size = new Size(420, 215)
            };

            Label lblSrvPort = new Label
            {
                Text = "🔌 Cổng lắng nghe Gateway:",
                Font = ModernTheme.RegularFont,
                ForeColor = ModernTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(14, 28)
            };

            _txtServerPort = new TextBox
            {
                Text = "8443",
                Location = new Point(190, 25),
                Width = 75,
                Font = ModernTheme.RegularFont,
                BackColor = ModernTheme.CardDark,
                ForeColor = ModernTheme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle
            };

            _btnResetServerPort = new Button
            {
                Text = "🔄 Mặc định 8443",
                Location = new Point(273, 24),
                Size = new Size(115, 25)
            };
            ModernTheme.ApplyToButton(_btnResetServerPort, ModernTheme.CardDark, ModernTheme.TextSecondary);
            _btnResetServerPort.Font = ModernTheme.SmallFont;
            _btnResetServerPort.Click += (s, e) => _txtServerPort.Text = "8443";

            Label lblSrvIp = new Label
            {
                Text = "🖧 IP Máy Chủ hiện tại:",
                Font = ModernTheme.RegularFont,
                ForeColor = ModernTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(14, 62)
            };

            _cboServerIps = new ComboBox
            {
                Location = new Point(165, 59),
                Width = 155,
                Font = ModernTheme.RegularFont,
                BackColor = ModernTheme.CardDark,
                ForeColor = ModernTheme.AccentGreen,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            _btnCopyServerIp = new Button
            {
                Text = "📋 Copy IP",
                Location = new Point(326, 58),
                Size = new Size(82, 25)
            };
            ModernTheme.ApplyToButton(_btnCopyServerIp, ModernTheme.AccentBlue, Color.White);
            _btnCopyServerIp.Font = ModernTheme.SmallFont;
            _btnCopyServerIp.Click += OnCopyServerIpClicked;

            _chkFirewall = new CheckBox
            {
                Text = "Tự động mở tường lửa Windows Firewall (TCP Inbound)",
                Checked = true,
                Font = ModernTheme.RegularFont,
                ForeColor = ModernTheme.AccentGreen,
                AutoSize = true,
                Location = new Point(16, 98)
            };

            _chkServerAutoStart = new CheckBox
            {
                Text = "Tự động khởi chạy Central Gateway cùng Windows",
                Checked = true,
                Font = ModernTheme.RegularFont,
                ForeColor = ModernTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(16, 128)
            };

            _btnOpenConsole = new Button
            {
                Text = "🖥️ Mở Master Console",
                Location = new Point(14, 168),
                Size = new Size(145, 30)
            };
            ModernTheme.ApplyToButton(_btnOpenConsole, ModernTheme.AccentBlue, Color.White);
            _btnOpenConsole.Font = ModernTheme.SmallFont;
            _btnOpenConsole.Click += (s, e) => LaunchQuickTool("LanControl.Control.exe", "");

            _btnOpenGateway = new Button
            {
                Text = "🛡️ Mở Gateway",
                Location = new Point(166, 168),
                Size = new Size(115, 30)
            };
            ModernTheme.ApplyToButton(_btnOpenGateway, ModernTheme.CardDark, ModernTheme.AccentCyan);
            _btnOpenGateway.Font = ModernTheme.SmallFont;
            _btnOpenGateway.Click += (s, e) => LaunchQuickTool("LanControl.Gateway.exe", _txtServerPort.Text.Trim());

            _btnStopServices = new Button
            {
                Text = "⏹️ Dừng Dịch Vụ",
                Location = new Point(288, 168),
                Size = new Size(118, 30)
            };
            ModernTheme.ApplyToButton(_btnStopServices, ModernTheme.CardDark, ModernTheme.AccentRed);
            _btnStopServices.Font = ModernTheme.SmallFont;
            _btnStopServices.Click += OnStopServicesClicked;

            _grpServerConfig.Controls.Add(lblSrvPort);
            _grpServerConfig.Controls.Add(_txtServerPort);
            _grpServerConfig.Controls.Add(_btnResetServerPort);
            _grpServerConfig.Controls.Add(lblSrvIp);
            _grpServerConfig.Controls.Add(_cboServerIps);
            _grpServerConfig.Controls.Add(_btnCopyServerIp);
            _grpServerConfig.Controls.Add(_chkFirewall);
            _grpServerConfig.Controls.Add(_chkServerAutoStart);
            _grpServerConfig.Controls.Add(_btnOpenConsole);
            _grpServerConfig.Controls.Add(_btnOpenGateway);
            _grpServerConfig.Controls.Add(_btnStopServices);
            pnlBody.Controls.Add(_grpServerConfig);

            // SECTION 4: TÙY CHỌN CHUNG & TIẾN TRÌNH CÀI ĐẶT
            GroupBox grpCommon = new GroupBox
            {
                Text = "4. LỐI TẮT DESKTOP && TIẾN TRÌNH THỰC THI",
                Font = ModernTheme.SubHeaderFont,
                ForeColor = ModernTheme.AccentYellow,
                BackColor = ModernTheme.PanelDark,
                Location = new Point(16, 475),
                Size = new Size(852, 118)
            };

            _chkDesktopShortcut = new CheckBox
            {
                Text = "📌 Tạo biểu tượng lối tắt ngoài màn hình Desktop (gắn icon.ico)",
                Checked = true,
                Font = ModernTheme.RegularFont,
                ForeColor = ModernTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(16, 26)
            };

            _chkLaunchAfterInstall = new CheckBox
            {
                Text = "🚀 Tự động khởi chạy ngay sau khi cài đặt xong",
                Checked = true,
                Font = ModernTheme.RegularFont,
                ForeColor = ModernTheme.AccentCyan,
                AutoSize = true,
                Location = new Point(470, 26)
            };

            _progressBar = new ProgressBar
            {
                Location = new Point(16, 58),
                Size = new Size(820, 20),
                Minimum = 0,
                Maximum = 100,
                Value = 0
            };

            _lblProgressStatus = new Label
            {
                Text = "Sẵn sàng: Hãy chọn phiên bản (Agent / Server / Cả 2) và nhấn nút [🚀 CÀI ĐẶT & KHỞI CHẠY NGAY (1-CLICK)] bên dưới.",
                UseMnemonic = false,
                Font = ModernTheme.RegularFont,
                ForeColor = ModernTheme.AccentGreen,
                AutoSize = false,
                Size = new Size(820, 24),
                Location = new Point(16, 84),
                AutoEllipsis = true
            };

            grpCommon.Controls.Add(_chkDesktopShortcut);
            grpCommon.Controls.Add(_chkLaunchAfterInstall);
            grpCommon.Controls.Add(_progressBar);
            grpCommon.Controls.Add(_lblProgressStatus);
            pnlBody.Controls.Add(grpCommon);

            this.Controls.Add(pnlBody);
            this.Controls.Add(_pnlBottom);
            this.Controls.Add(_pnlHeader);
        }

        private Panel CreateEditionCard(int x, int y, int w, int h, out RadioButton rad, string title, string desc, InstallEdition edition)
        {
            Panel pnl = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(w, h),
                BackColor = ModernTheme.CardDark,
                Cursor = Cursors.Hand,
                Padding = new Padding(10)
            };

            rad = new RadioButton
            {
                Text = " " + title,
                Font = ModernTheme.SubHeaderFont,
                ForeColor = ModernTheme.AccentCyan,
                Location = new Point(10, 8),
                AutoSize = true,
                Cursor = Cursors.Hand,
                AutoCheck = false
            };

            Label lblDesc = new Label
            {
                Text = desc,
                Font = ModernTheme.SmallFont,
                ForeColor = ModernTheme.TextSecondary,
                Location = new Point(12, 34),
                Size = new Size(w - 22, h - 40),
                Cursor = Cursors.Hand
            };

            pnl.Paint += (s, e) =>
            {
                bool isSelected = (_config.Edition == edition);
                Color borderColor = isSelected ? ModernTheme.AccentCyan : ModernTheme.BorderDark;
                int borderWidth = isSelected ? 2 : 1;
                ControlPaint.DrawBorder(e.Graphics, pnl.ClientRectangle,
                    borderColor, borderWidth, ButtonBorderStyle.Solid,
                    borderColor, borderWidth, ButtonBorderStyle.Solid,
                    borderColor, borderWidth, ButtonBorderStyle.Solid,
                    borderColor, borderWidth, ButtonBorderStyle.Solid);
            };

            EventHandler selectHandler = (s, e) => SelectEdition(edition);
            pnl.Click += selectHandler;
            rad.Click += selectHandler;
            lblDesc.Click += selectHandler;

            pnl.Controls.Add(rad);
            pnl.Controls.Add(lblDesc);
            return pnl;
        }

        private void ApplyTheme()
        {
            this.BackColor = ModernTheme.BackgroundDark;
            this.ForeColor = ModernTheme.TextPrimary;
        }

        private void InitializeData()
        {
            // Load Server LAN IPs
            List<string> ips = InstallerEngine.GetLocalLanIps();
            _cboServerIps.Items.Clear();
            foreach (string ip in ips)
            {
                _cboServerIps.Items.Add(ip);
            }
            if (!_cboServerIps.Items.Contains("127.0.0.1"))
            {
                _cboServerIps.Items.Add("127.0.0.1 (Localhost)");
            }
            if (_cboServerIps.Items.Count > 0)
            {
                _cboServerIps.SelectedIndex = 0;
            }

            // Default installation directory
            string defaultDir = @"C:\Program Files\lancontrol";
            string baseDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\', '/');
            if (File.Exists(Path.Combine(baseDir, "agent_config.json")) ||
                File.Exists(Path.Combine(baseDir, "gateway_config.json")))
            {
                defaultDir = baseDir;
            }

            _txtTargetFolder.Text = defaultDir;

            // Load existing config values if present
            InstallerEngine.LoadExistingConfig(defaultDir, _config);
            _txtGatewayHost.Text = _config.GatewayHost;
            _txtGatewayPort.Text = _config.GatewayPort.ToString();
            _cboVlan.Text = _config.VlanTag;
            _txtServerPort.Text = _config.ServerPort.ToString();

            CheckSourceFiles();
            UpdateCalculatedTarget();
        }

        private void CheckSourceFiles()
        {
            string[] coreFiles = new string[]
            {
                "LanControl.Core.dll",
                "LanControl.Agent.exe",
                "LanControl.Gateway.exe",
                "LanControl.Control.exe"
            };

            int found = 0;
            foreach (string f in coreFiles)
            {
                if (!string.IsNullOrEmpty(InstallerEngine.ResolveSourceFile(f)))
                {
                    found++;
                }
            }

            if (found == coreFiles.Length)
            {
                _lblSourceStatus.Text = "✅ Nguồn bộ cài: Đã tìm thấy đầy đủ 4/4 tệp thực thi (.exe/.dll) & icon.ico";
                _lblSourceStatus.ForeColor = ModernTheme.AccentGreen;
            }
            else
            {
                _lblSourceStatus.Text = string.Format("⚠️ Nguồn bộ cài: Tìm thấy {0}/{1} tệp thực thi (Hãy chạy build.bat nếu thiếu)", found, coreFiles.Length);
                _lblSourceStatus.ForeColor = ModernTheme.AccentYellow;
            }
        }

        private void UpdateCalculatedTarget()
        {
            string finalDir = InstallerEngine.EnsureLanControlDirectory(_txtTargetFolder.Text);
            _config.TargetDirectory = finalDir;
            _lblCalculatedPath.Text = "➜ Đường dẫn cài đặt thực tế: " + finalDir;
        }

        private void SelectEdition(InstallEdition edition)
        {
            if (_isUpdatingEditionSelection) return;
            _isUpdatingEditionSelection = true;
            try
            {
                _config.Edition = edition;
                if (_radAgent != null) _radAgent.Checked = (edition == InstallEdition.AgentClient);
                if (_radServer != null) _radServer.Checked = (edition == InstallEdition.ServerMaster);
                if (_radFull != null) _radFull.Checked = (edition == InstallEdition.FullSuite);

                UpdateCardHighlight(_cardAgent, edition == InstallEdition.AgentClient);
                UpdateCardHighlight(_cardServer, edition == InstallEdition.ServerMaster);
                UpdateCardHighlight(_cardFull, edition == InstallEdition.FullSuite);

                bool enableAgent = (edition == InstallEdition.AgentClient || edition == InstallEdition.FullSuite);
                bool enableServer = (edition == InstallEdition.ServerMaster || edition == InstallEdition.FullSuite);

                _grpAgentConfig.Enabled = enableAgent;
                _grpServerConfig.Enabled = enableServer;

                _grpAgentConfig.ForeColor = enableAgent ? ModernTheme.AccentCyan : ModernTheme.TextMuted;
                _grpServerConfig.ForeColor = enableServer ? ModernTheme.AccentGreen : ModernTheme.TextMuted;

                if (edition == InstallEdition.AgentClient)
                {
                    _btnInstallOneClick.Text = "🚀 CÀI ĐẶT & KHỞI CHẠY AGENT NGAY (1-CLICK)";
                    _btnUninstall.Text = "🗑️ Gỡ Cài Đặt Agent";
                }
                else if (edition == InstallEdition.ServerMaster)
                {
                    _btnInstallOneClick.Text = "🚀 CÀI ĐẶT & KHỞI CHẠY SERVER NGAY (1-CLICK)";
                    _btnUninstall.Text = "🗑️ Gỡ Cài Đặt Server";
                }
                else
                {
                    _btnInstallOneClick.Text = "🚀 CÀI ĐẶT & CHẠY CẢ HAI: AGENT + SERVER (1-CLICK)";
                    _btnUninstall.Text = "🗑️ Gỡ Cài Đặt Cả Hai";
                }
            }
            finally
            {
                _isUpdatingEditionSelection = false;
            }
        }

        private void UpdateCardHighlight(Panel card, bool isSelected)
        {
            if (card == null) return;
            card.BackColor = isSelected ? ModernTheme.CardSelected : ModernTheme.CardDark;
            card.Invalidate();
        }

        private void UpdateRunningStatus()
        {
            try
            {
                Process[] agentProcs = Process.GetProcessesByName("LanControl.Agent");
                if (agentProcs != null && agentProcs.Length > 0)
                {
                    _lblAgentBadge.Text = string.Format("🟢 Agent: Đang chạy (PID {0})", agentProcs[0].Id);
                    _lblAgentBadge.ForeColor = ModernTheme.AccentGreen;
                }
                else
                {
                    _lblAgentBadge.Text = "⚪ Agent: Chưa chạy";
                    _lblAgentBadge.ForeColor = ModernTheme.TextMuted;
                }

                Process[] gwProcs = Process.GetProcessesByName("LanControl.Gateway");
                if (gwProcs != null && gwProcs.Length > 0)
                {
                    _lblGatewayBadge.Text = string.Format("🟢 Gateway: Đang chạy (PID {0})", gwProcs[0].Id);
                    _lblGatewayBadge.ForeColor = ModernTheme.AccentGreen;
                }
                else
                {
                    _lblGatewayBadge.Text = "⚪ Gateway: Chưa chạy";
                    _lblGatewayBadge.ForeColor = ModernTheme.TextMuted;
                }
            }
            catch { }
        }

        private void OnBrowseFolderClicked(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Chọn thư mục cài đặt LAN Control (thư mục con 'lancontrol' sẽ tự động được tạo nếu cần):";
                fbd.SelectedPath = _txtTargetFolder.Text;
                if (fbd.ShowDialog(this) == DialogResult.OK)
                {
                    _txtTargetFolder.Text = fbd.SelectedPath;
                    UpdateCalculatedTarget();
                }
            }
        }

        private void OnOpenFolderClicked(object sender, EventArgs e)
        {
            string dir = _config.TargetDirectory;
            if (!Directory.Exists(dir))
            {
                dir = _txtTargetFolder.Text.Trim();
            }
            if (Directory.Exists(dir))
            {
                Process.Start("explorer.exe", "\"" + dir + "\"");
            }
            else
            {
                MessageBox.Show("Thư mục đích chưa tồn tại (sẽ được tự động tạo khi bấm Cài Đặt):\n" + _config.TargetDirectory,
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void OnCopyServerIpClicked(object sender, EventArgs e)
        {
            if (_cboServerIps.SelectedItem != null)
            {
                string raw = _cboServerIps.SelectedItem.ToString();
                string ip = raw.Split(' ')[0].Trim();
                try
                {
                    Clipboard.SetText(ip);
                    _lblProgressStatus.Text = string.Format("📋 Đã sao chép địa chỉ IP máy chủ '{0}' vào bộ nhớ tạm (Clipboard)!", ip);
                    _lblProgressStatus.ForeColor = ModernTheme.AccentCyan;
                }
                catch { }
            }
        }

        private void LaunchQuickTool(string exeName, string args)
        {
            string targetExe = Path.Combine(_config.TargetDirectory, exeName);
            if (!File.Exists(targetExe))
            {
                targetExe = InstallerEngine.ResolveSourceFile(exeName);
            }

            if (!string.IsNullOrEmpty(targetExe) && File.Exists(targetExe))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = targetExe,
                        Arguments = args,
                        WorkingDirectory = Path.GetDirectoryName(targetExe),
                        UseShellExecute = true
                    });
                    System.Threading.Thread.Sleep(300);
                    UpdateRunningStatus();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Không thể khởi chạy " + exeName + ":\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Không tìm thấy tệp " + exeName + ". Vui lòng nhấn [Cài Đặt & Khởi Chạy Ngay] trước!",
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OnStopServicesClicked(object sender, EventArgs e)
        {
            InstallerEngine.TryKillRunningProcess("LanControl.Agent");
            InstallerEngine.TryKillRunningProcess("LanControl.Gateway");
            UpdateRunningStatus();
            _lblProgressStatus.Text = "⏹️ Đã dừng các tiến trình LanControl.Agent và LanControl.Gateway đang chạy.";
            _lblProgressStatus.ForeColor = ModernTheme.AccentYellow;
        }

        private bool CollectAndValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(_txtTargetFolder.Text))
            {
                MessageBox.Show("Vui lòng chọn thư mục lưu trữ cài đặt!", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtTargetFolder.Focus();
                return false;
            }

            _config.TargetDirectory = InstallerEngine.EnsureLanControlDirectory(_txtTargetFolder.Text);

            bool needAgent = (_config.Edition == InstallEdition.AgentClient || _config.Edition == InstallEdition.FullSuite);
            bool needServer = (_config.Edition == InstallEdition.ServerMaster || _config.Edition == InstallEdition.FullSuite);

            if (needAgent)
            {
                string host = _txtGatewayHost.Text.Trim();
                if (string.IsNullOrEmpty(host))
                {
                    MessageBox.Show("Vui lòng nhập địa chỉ IP máy chủ Gateway cho Agent!", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _txtGatewayHost.Focus();
                    return false;
                }
                _config.GatewayHost = host;

                int gwPort;
                if (!int.TryParse(_txtGatewayPort.Text.Trim(), out gwPort) || gwPort <= 0 || gwPort > 65535)
                {
                    gwPort = 8443;
                    _txtGatewayPort.Text = "8443";
                }
                _config.GatewayPort = gwPort;

                string vlan = _cboVlan.Text.Trim();
                _config.VlanTag = string.IsNullOrEmpty(vlan) ? "VLAN 10 - Phòng Kỹ Thuật" : vlan;
                _config.AgentAutoStart = _chkAgentAutoStart.Checked;
                _config.EnableUacRemoteSupport = _chkUacSupport.Checked;
            }

            if (needServer)
            {
                int srvPort;
                if (!int.TryParse(_txtServerPort.Text.Trim(), out srvPort) || srvPort <= 0 || srvPort > 65535)
                {
                    srvPort = 8443;
                    _txtServerPort.Text = "8443";
                }
                _config.ServerPort = srvPort;
                _config.OpenFirewall = _chkFirewall.Checked;
                _config.ServerAutoStart = _chkServerAutoStart.Checked;
            }

            _config.CreateDesktopShortcuts = _chkDesktopShortcut.Checked;
            _config.LaunchAfterInstall = _chkLaunchAfterInstall.Checked;
            return true;
        }

        private void SetUiBusy(bool busy)
        {
            _isInstalling = busy;
            _btnInstallOneClick.Enabled = !busy;
            _btnSaveConfigOnly.Enabled = !busy;
            _btnUninstall.Enabled = !busy;
            _cardAgent.Enabled = !busy;
            _cardServer.Enabled = !busy;
            _cardFull.Enabled = !busy;
        }

        private void OnInstallOneClickClicked(object sender, EventArgs e)
        {
            if (_isInstalling) return;
            if (!CollectAndValidateInputs()) return;

            SetUiBusy(true);
            _progressBar.Value = 0;

            System.Threading.ThreadPool.QueueUserWorkItem((state) =>
            {
                string errMsg;
                bool ok = _engine.ExecuteInstallation(_config, out errMsg);

                this.BeginInvoke(new Action(() =>
                {
                    SetUiBusy(false);
                    UpdateRunningStatus();

                    if (ok)
                    {
                        string editionName = _config.Edition == InstallEdition.AgentClient
                            ? "Máy Trạm (LAN Control Agent Client)"
                            : (_config.Edition == InstallEdition.ServerMaster
                                ? "Máy Chủ (LAN Control Server & Master Console)"
                                : "Bộ Đầy Đủ (Cả 2: Agent + Server)");

                        string details = string.Format(
                            "🎉 CÀI ĐẶT 1-CLICK HOÀN TẤT THÀNH CÔNG!\n\n" +
                            "• Phiên bản đã cài: {0}\n" +
                            "• Thư mục cài đặt: {1}\n",
                            editionName,
                            _config.TargetDirectory
                        );

                        if (_config.Edition == InstallEdition.AgentClient || _config.Edition == InstallEdition.FullSuite)
                        {
                            details += string.Format("• Kết nối Agent -> Gateway: {0}:{1} ({2})\n",
                                _config.GatewayHost, _config.GatewayPort, _config.VlanTag);
                        }
                        if (_config.Edition == InstallEdition.ServerMaster || _config.Edition == InstallEdition.FullSuite)
                        {
                            details += string.Format("• Cổng lắng nghe Server Gateway: {0} (Firewall: {1})\n",
                                _config.ServerPort, _config.OpenFirewall ? "Đã mở" : "Không");
                        }

                        details += string.Format("• Lối tắt Desktop: {0}\n• Trạng thái khởi chạy: {1}",
                            _config.CreateDesktopShortcuts ? "Đã tạo kèm icon.ico" : "Không tạo",
                            _config.LaunchAfterInstall ? "Đã khởi chạy tự động" : "Chưa khởi chạy");

                        MessageBox.Show(this, details, "Cài Đặt 1-Click Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        this.Close();
                    }
                    else
                    {
                        _lblProgressStatus.Text = "❌ Lỗi cài đặt: " + errMsg;
                        _lblProgressStatus.ForeColor = ModernTheme.AccentRed;
                        MessageBox.Show(this, "Có lỗi xảy ra trong quá trình cài đặt:\n\n" + errMsg,
                            "Lỗi Cài Đặt", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }));
            });
        }

        private void OnSaveConfigOnlyClicked(object sender, EventArgs e)
        {
            if (_isInstalling) return;
            if (!CollectAndValidateInputs()) return;

            string errMsg;
            if (_engine.SaveConfigurationOnly(_config, out errMsg))
            {
                _lblProgressStatus.Text = "💾 Đã lưu cấu hình thành công vào: " + _config.TargetDirectory;
                _lblProgressStatus.ForeColor = ModernTheme.AccentGreen;
                MessageBox.Show(this,
                    "Đã lưu cấu hình và thiết lập hệ thống thành công vào:\n" + _config.TargetDirectory,
                    "Lưu Cấu Hình Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                _lblProgressStatus.Text = "❌ Lỗi lưu cấu hình: " + errMsg;
                _lblProgressStatus.ForeColor = ModernTheme.AccentRed;
                MessageBox.Show(this, "Không thể lưu cấu hình:\n" + errMsg, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnUninstallClicked(object sender, EventArgs e)
        {
            if (_isInstalling) return;
            UpdateCalculatedTarget();

            string targetName = _config.Edition == InstallEdition.AgentClient
                ? "Máy Trạm (Agent Client)"
                : (_config.Edition == InstallEdition.ServerMaster
                    ? "Máy Chủ (Gateway & Master Console)"
                    : "Cả Hai (Toàn bộ Agent + Server)");

            DialogResult dr = MessageBox.Show(this,
                string.Format(
                    "Bạn có chắc chắn muốn gỡ cài đặt cấu hình [{0}] trên máy tính này?\n\n" +
                    "Hành động này sẽ:\n" +
                    "• Dừng các tiến trình đang chạy tương ứng.\n" +
                    "• Xóa tự khởi động cùng Windows (Registry, Startup & Task Scheduler).\n" +
                    "• Xóa tệp cấu hình (.json), luật tường lửa Firewall và lối tắt Desktop.\n" +
                    "• Giữ nguyên các tệp .exe / .dll gốc để có thể cài lại bất cứ lúc nào.",
                    targetName),
                "Xác Nhận Gỡ Cài Đặt",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (dr != DialogResult.Yes) return;

            string summary, errMsg;
            if (_engine.ExecuteUninstall(_config, out summary, out errMsg))
            {
                UpdateRunningStatus();
                _lblProgressStatus.Text = "🗑️ Đã gỡ cài đặt [" + targetName + "] thành công!";
                _lblProgressStatus.ForeColor = ModernTheme.AccentYellow;
                MessageBox.Show(this,
                    "Đã hoàn tất gỡ cài đặt [" + targetName + "]:\n\n" + summary,
                    "Gỡ Cài Đặt Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(this, "Lỗi khi gỡ cài đặt:\n" + errMsg, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnInstallProgressChanged(int pct, string status)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => OnInstallProgressChanged(pct, status)));
                return;
            }

            _progressBar.Value = Math.Min(100, Math.Max(0, pct));
            _lblProgressStatus.Text = status;
            _lblProgressStatus.ForeColor = (pct >= 100) ? ModernTheme.AccentGreen : ModernTheme.AccentCyan;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_statusTimer != null)
            {
                _statusTimer.Stop();
                _statusTimer.Dispose();
            }
            base.OnFormClosing(e);
        }
    }
}
