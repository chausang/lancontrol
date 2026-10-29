using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;
using LanControl.Core.Models;

namespace LanControl.Installer
{
    public class SetupForm : Form
    {
        // Controls
        private TextBox _txtInstallPath;
        private Button _btnBrowseTarget;
        private Label _lblSourceStatus;
        private Button _btnBrowseSource;

        private TextBox _txtGatewayHost;
        private TextBox _txtGatewayPort;
        private Button _btnResetPort;
        private ComboBox _cboVlanTag;

        private CheckBox _chkAutoStart;
        private CheckBox _chkFixUac;
        private Label _lblAgentStatus;
        private Button _btnStopAgent;
        private Button _btnRestartAgent;

        private Button _btnSaveConfigOnly;
        private Button _btnUninstall;
        private Button _btnOpenFolder;
        private Button _btnInstallAndLaunch;
        private Label _lblLogStatus;

        private string _sourceAgentExe = "";
        private string _sourceCoreDll = "";

        public SetupForm()
        {
            InitializeComponent();
            FindSourceFiles();
            LoadExistingConfigOrDefaults();
            UpdateAgentRunningStatus();
        }

        private void InitializeComponent()
        {
            this.Text = "Cài Đặt & Cấu Hình LAN Control Client Agent";
            this.Size = new Size(730, 755);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = ModernTheme.BackgroundDark;
            this.ForeColor = ModernTheme.TextPrimary;

            // 1. Top Banner
            Panel topBanner = new Panel
            {
                Dock = DockStyle.Top,
                Height = 72,
                BackColor = ModernTheme.PanelDark,
                Padding = new Padding(16, 12, 16, 10)
            };

            Label lblTitle = new Label
            {
                Text = "⚙️ TRÌNH CÀI ĐẶT & CẤU HÌNH MÁY TRẠM (LAN CONTROL AGENT SETUP)",
                Font = ModernTheme.HeaderFont,
                ForeColor = ModernTheme.AccentCyan,
                AutoSize = true,
                Location = new Point(14, 12)
            };

            Label lblSubTitle = new Label
            {
                Text = "Cài đặt và thiết lập kết nối máy trạm tới máy chủ quản trị đa VLAN chỉ với 1 cú click chuột.",
                Font = ModernTheme.RegularFont,
                ForeColor = ModernTheme.TextSecondary,
                AutoSize = true,
                Location = new Point(16, 38)
            };

            topBanner.Controls.Add(lblTitle);
            topBanner.Controls.Add(lblSubTitle);

            // Main Container (Flow or Group Panels)
            Panel mainContainer = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(14)
            };

            int currentY = 10;

            // SECTION 1: Target Path & Source
            GroupBox grpPath = CreateGroupBox("1. NƠI LƯU TRỮ TẬP TIN & NGUỒN CÀI ĐẶT", currentY, 155);
            currentY += 165;

            Label lblTargetPath = new Label
            {
                Text = "📂 Thư mục cài đặt / nơi lưu trữ LanControl.Agent.exe trên máy:",
                Location = new Point(16, 26),
                AutoSize = true,
                Font = ModernTheme.SubHeaderFont,
                ForeColor = ModernTheme.TextPrimary
            };

            _txtInstallPath = new TextBox
            {
                Text = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\'),
                Location = new Point(18, 50),
                Width = 510,
                Font = ModernTheme.RegularFont,
                BackColor = ModernTheme.CardDark,
                ForeColor = ModernTheme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle
            };

            _btnBrowseTarget = new Button
            {
                Text = "📁 Chọn Thư Mục...",
                Location = new Point(535, 48),
                Size = new Size(135, 26)
            };
            ModernTheme.ApplyToButton(_btnBrowseTarget, ModernTheme.AccentBlue, Color.White);
            _btnBrowseTarget.Click += OnBrowseTargetClicked;

            _lblSourceStatus = new Label
            {
                Text = "🔍 Nguồn file Agent: Đang tìm kiếm...",
                Location = new Point(18, 86),
                Size = new Size(510, 36),
                Font = ModernTheme.SmallFont,
                ForeColor = ModernTheme.AccentYellow
            };

            _btnBrowseSource = new Button
            {
                Text = "🔍 Tìm File Nguồn...",
                Location = new Point(535, 88),
                Size = new Size(135, 26)
            };
            ModernTheme.ApplyToButton(_btnBrowseSource, ModernTheme.CardDark, ModernTheme.TextPrimary);
            _btnBrowseSource.Click += OnBrowseSourceClicked;

            grpPath.Controls.Add(lblTargetPath);
            grpPath.Controls.Add(_txtInstallPath);
            grpPath.Controls.Add(_btnBrowseTarget);
            grpPath.Controls.Add(_lblSourceStatus);
            grpPath.Controls.Add(_btnBrowseSource);
            mainContainer.Controls.Add(grpPath);

            // SECTION 2: Network & VLAN Config
            GroupBox grpConfig = CreateGroupBox("2. THÔNG TIN MÁY CHỦ GATEWAY & PHÂN ĐOẠN VLAN", currentY, 195);
            currentY += 205;

            Label lblHost = new Label
            {
                Text = "🌐 Địa chỉ IP / Tên miền máy chủ Gateway:",
                Location = new Point(16, 26),
                AutoSize = true,
                Font = ModernTheme.SubHeaderFont,
                ForeColor = ModernTheme.TextPrimary
            };

            _txtGatewayHost = new TextBox
            {
                Text = "127.0.0.1",
                Location = new Point(18, 50),
                Width = 280,
                Font = ModernTheme.RegularFont,
                BackColor = ModernTheme.CardDark,
                ForeColor = ModernTheme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle
            };

            Label lblPort = new Label
            {
                Text = "🔌 Cổng (Port):",
                Location = new Point(320, 26),
                AutoSize = true,
                Font = ModernTheme.SubHeaderFont,
                ForeColor = ModernTheme.TextPrimary
            };

            _txtGatewayPort = new TextBox
            {
                Text = "8443",
                Location = new Point(322, 50),
                Width = 90,
                Font = ModernTheme.RegularFont,
                BackColor = ModernTheme.CardDark,
                ForeColor = ModernTheme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle
            };

            _btnResetPort = new Button
            {
                Text = "🔄 Đặt lại 8443",
                Location = new Point(418, 48),
                Size = new Size(110, 26)
            };
            ModernTheme.ApplyToButton(_btnResetPort, ModernTheme.CardDark, ModernTheme.AccentCyan);
            _btnResetPort.Click += (s, e) => { _txtGatewayPort.Text = "8443"; };

            // Notice about Port 8443
            Label lblPortNote = new Label
            {
                Text = "💡 Ghi chú: Cổng mặc định của hệ thống là 8443. Nếu bạn lỡ xóa hoặc quên port, hãy nhập lại 8443 (hoặc bấm nút [Đặt lại 8443]).",
                Location = new Point(18, 78),
                Size = new Size(650, 18),
                Font = ModernTheme.SmallFont,
                ForeColor = ModernTheme.AccentYellow
            };

            Label lblVlan = new Label
            {
                Text = "🏷️ Phân đoạn VLAN / Tên phòng ban:",
                Location = new Point(16, 108),
                AutoSize = true,
                Font = ModernTheme.SubHeaderFont,
                ForeColor = ModernTheme.TextPrimary
            };

            _cboVlanTag = new ComboBox
            {
                Location = new Point(18, 132),
                Width = 430,
                Font = ModernTheme.RegularFont,
                BackColor = ModernTheme.CardDark,
                ForeColor = ModernTheme.TextPrimary,
                DropDownStyle = ComboBoxStyle.DropDown
            };
            _cboVlanTag.Items.AddRange(new object[]
            {
                "VLAN 10 - Phòng Kỹ Thuật",
                "VLAN 20 - Phòng Kế Toán",
                "VLAN 30 - Phòng Đào Tạo",
                "Phòng Máy Tính 1",
                "Phòng Máy Tính 2",
                "Phòng Học Học Viên",
                "Khu Vực Văn Phòng",
                "Default"
            });
            _cboVlanTag.SelectedIndex = 0;

            grpConfig.Controls.Add(lblHost);
            grpConfig.Controls.Add(_txtGatewayHost);
            grpConfig.Controls.Add(lblPort);
            grpConfig.Controls.Add(_txtGatewayPort);
            grpConfig.Controls.Add(_btnResetPort);
            grpConfig.Controls.Add(lblPortNote);
            grpConfig.Controls.Add(lblVlan);
            grpConfig.Controls.Add(_cboVlanTag);
            mainContainer.Controls.Add(grpConfig);

            // SECTION 3: Auto-start & Current Status
            GroupBox grpStatus = CreateGroupBox("3. KHỞI ĐỘNG CÙNG WINDOWS & HỖ TRỢ UAC ADMIN", currentY, 145);
            currentY += 155;

            _chkAutoStart = new CheckBox
            {
                Text = "☑ Tự động khởi chạy LAN Control Agent khi máy tính khởi động lại (Startup & Registry)",
                Checked = true,
                Location = new Point(18, 25),
                AutoSize = true,
                Font = ModernTheme.SubHeaderFont,
                ForeColor = ModernTheme.AccentGreen
            };

            _chkFixUac = new CheckBox
            {
                Text = "☑ Tự động nâng quyền Admin & Cho phép điều khiển qua cửa sổ UAC (UIPI & Auto-Elevate)",
                Checked = true,
                Location = new Point(18, 56),
                AutoSize = true,
                Font = ModernTheme.RegularFont,
                ForeColor = ModernTheme.AccentCyan
            };

            _lblAgentStatus = new Label
            {
                Text = "Trạng thái Agent trên máy: Đang kiểm tra...",
                Location = new Point(18, 96),
                Size = new Size(380, 24),
                Font = ModernTheme.RegularFont,
                ForeColor = ModernTheme.TextSecondary
            };

            _btnStopAgent = new Button
            {
                Text = "⏹️ Dừng Agent",
                Location = new Point(410, 92),
                Size = new Size(110, 26)
            };
            ModernTheme.ApplyToButton(_btnStopAgent, ModernTheme.CardDark, ModernTheme.AccentRed);
            _btnStopAgent.Click += (s, e) => { KillRunningAgent(); UpdateAgentRunningStatus(); };

            _btnRestartAgent = new Button
            {
                Text = "🔄 Khởi Động Lại",
                Location = new Point(530, 92),
                Size = new Size(120, 26)
            };
            ModernTheme.ApplyToButton(_btnRestartAgent, ModernTheme.CardDark, ModernTheme.TextPrimary);
            _btnRestartAgent.Click += (s, e) => { DoInstallAndLaunch(); };

            grpStatus.Controls.Add(_chkAutoStart);
            grpStatus.Controls.Add(_chkFixUac);
            grpStatus.Controls.Add(_lblAgentStatus);
            grpStatus.Controls.Add(_btnStopAgent);
            grpStatus.Controls.Add(_btnRestartAgent);
            mainContainer.Controls.Add(grpStatus);

            // SECTION 4: Action Buttons Panel
            Panel actionPanel = new Panel
            {
                Location = new Point(14, currentY),
                Size = new Size(675, 100)
            };

            _btnSaveConfigOnly = new Button
            {
                Text = "💾 Lưu Cấu Hình",
                Location = new Point(4, 10),
                Size = new Size(115, 42)
            };
            ModernTheme.ApplyToButton(_btnSaveConfigOnly, ModernTheme.CardDark, ModernTheme.TextPrimary);
            _btnSaveConfigOnly.Click += OnSaveConfigClicked;

            _btnUninstall = new Button
            {
                Text = "🗑️ Gỡ Cài Đặt",
                Location = new Point(124, 10),
                Size = new Size(115, 42)
            };
            ModernTheme.ApplyToButton(_btnUninstall, ModernTheme.CardDark, ModernTheme.AccentRed);
            _btnUninstall.Click += (s, e) => DoUninstall();

            _btnOpenFolder = new Button
            {
                Text = "📂 Thư Mục",
                Location = new Point(244, 10),
                Size = new Size(100, 42)
            };
            ModernTheme.ApplyToButton(_btnOpenFolder, ModernTheme.CardDark, ModernTheme.TextPrimary);
            _btnOpenFolder.Click += OnOpenFolderClicked;

            _btnInstallAndLaunch = new Button
            {
                Text = "🚀 CÀI ĐẶT & KHỞI CHẠY NGAY",
                Location = new Point(350, 8),
                Size = new Size(322, 46)
            };
            ModernTheme.ApplyToButton(_btnInstallAndLaunch, ModernTheme.AccentGreen, Color.White);
            _btnInstallAndLaunch.Font = ModernTheme.HeaderFont;
            _btnInstallAndLaunch.Click += (s, e) => DoInstallAndLaunch();

            _lblLogStatus = new Label
            {
                Text = "Sẵn sàng thiết lập.",
                Location = new Point(6, 62),
                Size = new Size(660, 22),
                Font = ModernTheme.SmallFont,
                ForeColor = ModernTheme.TextMuted
            };

            actionPanel.Controls.Add(_btnSaveConfigOnly);
            actionPanel.Controls.Add(_btnUninstall);
            actionPanel.Controls.Add(_btnOpenFolder);
            actionPanel.Controls.Add(_btnInstallAndLaunch);
            actionPanel.Controls.Add(_lblLogStatus);
            mainContainer.Controls.Add(actionPanel);

            this.Controls.Add(mainContainer);
            this.Controls.Add(topBanner);
        }

        private GroupBox CreateGroupBox(string title, int top, int height)
        {
            return new GroupBox
            {
                Text = title,
                Location = new Point(14, top),
                Size = new Size(675, height),
                Font = ModernTheme.SubHeaderFont,
                ForeColor = ModernTheme.AccentCyan,
                BackColor = ModernTheme.PanelDark
            };
        }

        private void FindSourceFiles()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] possibleDirs = new string[]
            {
                baseDir,
                Path.Combine(baseDir, "bin"),
                Path.Combine(baseDir, "..", "bin"),
                @"D:\lancontrol\bin",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "LAN Control Agent"),
                @"C:\LanControlAgent"
            };

            foreach (string dir in possibleDirs)
            {
                try
                {
                    if (Directory.Exists(dir))
                    {
                        string exePath = Path.Combine(dir, "LanControl.Agent.exe");
                        string dllPath = Path.Combine(dir, "LanControl.Core.dll");

                        if (File.Exists(exePath))
                        {
                            _sourceAgentExe = Path.GetFullPath(exePath);
                            _sourceCoreDll = File.Exists(dllPath) ? Path.GetFullPath(dllPath) : "";
                            _lblSourceStatus.Text = string.Format("✅ Nguồn file: {0}", _sourceAgentExe);
                            _lblSourceStatus.ForeColor = ModernTheme.AccentGreen;
                            return;
                        }
                    }
                }
                catch { }
            }

            _lblSourceStatus.Text = "⚠️ Chưa tìm thấy file nguồn LanControl.Agent.exe. Bấm nút [Tìm File Nguồn...] bên cạnh để chọn.";
            _lblSourceStatus.ForeColor = ModernTheme.AccentYellow;
        }

        private void LoadExistingConfigOrDefaults()
        {
            string targetDir = _txtInstallPath.Text.Trim();
            if (string.IsNullOrEmpty(targetDir))
            {
                targetDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
                _txtInstallPath.Text = targetDir;
            }
            string cfgPath = Path.Combine(targetDir, "agent_config.json");

            if (!File.Exists(cfgPath) && !string.IsNullOrEmpty(_sourceAgentExe))
            {
                string srcCfg = Path.Combine(Path.GetDirectoryName(_sourceAgentExe), "agent_config.json");
                if (File.Exists(srcCfg)) cfgPath = srcCfg;
            }

            if (File.Exists(cfgPath))
            {
                AgentConfig cfg = AgentConfig.Load(cfgPath);
                if (cfg != null)
                {
                    _txtGatewayHost.Text = string.IsNullOrEmpty(cfg.GatewayHost) ? "127.0.0.1" : cfg.GatewayHost;
                    _txtGatewayPort.Text = cfg.GatewayPort > 0 ? cfg.GatewayPort.ToString() : "8443";
                    if (!string.IsNullOrEmpty(cfg.VlanTag))
                    {
                        _cboVlanTag.Text = cfg.VlanTag;
                    }
                }
            }

            // Check if registry key is currently set
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false))
                {
                    if (key != null)
                    {
                        object val = key.GetValue("LanControlAgent");
                        _chkAutoStart.Checked = (val != null);
                    }
                }
            }
            catch { }
        }

        private void UpdateAgentRunningStatus()
        {
            Process[] procs = Process.GetProcessesByName("LanControl.Agent");
            if (procs.Length > 0)
            {
                _lblAgentStatus.Text = string.Format("🟢 Agent đang chạy ngầm (PID: {0}, Phiên bản hoạt động)", procs[0].Id);
                _lblAgentStatus.ForeColor = ModernTheme.AccentGreen;
                _btnStopAgent.Enabled = true;
            }
            else
            {
                _lblAgentStatus.Text = "⚪ Agent hiện chưa chạy trên máy này.";
                _lblAgentStatus.ForeColor = ModernTheme.TextMuted;
                _btnStopAgent.Enabled = false;
            }
        }

        private void KillRunningAgent()
        {
            try
            {
                Process[] procs = Process.GetProcessesByName("LanControl.Agent");
                foreach (Process p in procs)
                {
                    try
                    {
                        p.Kill();
                        p.WaitForExit(2000);
                    }
                    catch { }
                }
            }
            catch { }
        }

        private void OnBrowseTargetClicked(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Chọn thư mục lưu trữ tập tin LAN Control Agent trên máy:";
                dlg.SelectedPath = Directory.Exists(_txtInstallPath.Text.Trim()) ? _txtInstallPath.Text.Trim() : @"C:\";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    _txtInstallPath.Text = dlg.SelectedPath;
                    LoadExistingConfigOrDefaults();
                }
            }
        }

        private void OnBrowseSourceClicked(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Title = "Chọn tập tin LanControl.Agent.exe nguồn";
                dlg.Filter = "LAN Control Agent Executable (LanControl.Agent.exe)|LanControl.Agent.exe|All Executables (*.exe)|*.exe";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    _sourceAgentExe = dlg.FileName;
                    string dir = Path.GetDirectoryName(_sourceAgentExe);
                    string corePath = Path.Combine(dir, "LanControl.Core.dll");
                    _sourceCoreDll = File.Exists(corePath) ? corePath : "";

                    _lblSourceStatus.Text = string.Format("✅ Nguồn file: {0}", _sourceAgentExe);
                    _lblSourceStatus.ForeColor = ModernTheme.AccentGreen;
                }
            }
        }

        private void OnSaveConfigClicked(object sender, EventArgs e)
        {
            string targetDir = _txtInstallPath.Text.Trim();
            if (string.IsNullOrEmpty(targetDir))
            {
                MessageBox.Show("Vui lòng chọn thư mục cài đặt!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!Directory.Exists(targetDir))
            {
                try { Directory.CreateDirectory(targetDir); } catch { }
            }

            string host = _txtGatewayHost.Text.Trim();
            int port;
            if (!int.TryParse(_txtGatewayPort.Text.Trim(), out port) || port <= 0)
            {
                port = 8443;
                _txtGatewayPort.Text = "8443";
            }
            string vlan = string.IsNullOrEmpty(_cboVlanTag.Text.Trim()) ? "Default" : _cboVlanTag.Text.Trim();

            string cfgPath = Path.Combine(targetDir, "agent_config.json");
            AgentConfig cfg = AgentConfig.Load(cfgPath);
            cfg.GatewayHost = host;
            cfg.GatewayPort = port;
            cfg.VlanTag = vlan;
            cfg.Save(cfgPath);

            string targetExe = Path.Combine(targetDir, "LanControl.Agent.exe");
            SetAutoStart(targetExe, _chkAutoStart.Checked);
            ConfigureUacSupport(_chkFixUac.Checked);

            _lblLogStatus.Text = "Đã lưu cấu hình vào: " + cfgPath;
            _lblLogStatus.ForeColor = ModernTheme.AccentGreen;
            MessageBox.Show("Đã lưu cấu hình vào:\n" + cfgPath + "\n\nCổng: " + port + "\nVLAN: " + vlan, "Lưu Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OnOpenFolderClicked(object sender, EventArgs e)
        {
            string targetDir = _txtInstallPath.Text.Trim();
            if (Directory.Exists(targetDir))
            {
                Process.Start("explorer.exe", targetDir);
            }
            else
            {
                MessageBox.Show("Thư mục chưa tồn tại trên máy: " + targetDir, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void DoUninstall()
        {
            DialogResult dr = MessageBox.Show(
                "Bạn có chắc chắn muốn gỡ cài đặt cấu hình LAN Control Agent trên máy này?\n\n" +
                "Hành động này sẽ thực hiện:\n" +
                "• Dừng tiến trình LAN Control Agent đang chạy.\n" +
                "• Xóa tác vụ tự khởi động cùng Windows (Registry & Startup Folder).\n" +
                "• Xóa tệp cấu hình máy trạm (agent_config.json).\n\n" +
                "LƯU Ý: Các tệp chương trình (.exe, .dll) KHÔNG bị xóa, bạn có thể cài đặt lại bất cứ lúc nào.",
                "Xác Nhận Gỡ Cài Đặt Agent",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (dr != DialogResult.Yes) return;

            try
            {
                // 1. Stop Agent process
                KillRunningAgent();

                // 2. Remove auto-start from Windows Registry and Startup folder
                SetAutoStart("", false);
                _chkAutoStart.Checked = false;
                ConfigureUacSupport(false);

                // 3. Delete config files
                string targetDir = _txtInstallPath.Text.Trim();
                string cfgPath = Path.Combine(targetDir, "agent_config.json");
                if (File.Exists(cfgPath))
                {
                    try { File.Delete(cfgPath); } catch { }
                }

                string localCfg = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "agent_config.json");
                if (File.Exists(localCfg))
                {
                    try { File.Delete(localCfg); } catch { }
                }

                _lblLogStatus.Text = "🗑️ Đã gỡ bỏ cấu hình và tác vụ tự khởi động cùng Windows của Agent thành công!";
                _lblLogStatus.ForeColor = ModernTheme.AccentGreen;
                UpdateAgentRunningStatus();

                MessageBox.Show(
                    "Đã gỡ bỏ cấu hình và tác vụ tự khởi động của LAN Control Agent thành công!\n\n" +
                    "• Tiến trình Agent đã dừng.\n" +
                    "• Tác vụ tự chạy khi bật máy tính đã được xóa bỏ.\n" +
                    "• Các tệp chương trình vẫn được giữ nguyên vẹn trên máy tính.",
                    "Gỡ Cài Đặt Hoàn Tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _lblLogStatus.Text = "❌ Lỗi khi gỡ cài đặt: " + ex.Message;
                _lblLogStatus.ForeColor = ModernTheme.AccentRed;
                MessageBox.Show("Có lỗi xảy ra khi gỡ cài đặt:\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DoInstallAndLaunch()
        {
            string host = _txtGatewayHost.Text.Trim();
            if (string.IsNullOrEmpty(host))
            {
                MessageBox.Show("Vui lòng nhập địa chỉ IP hoặc tên máy chủ Gateway!", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtGatewayHost.Focus();
                return;
            }

            int port;
            if (!int.TryParse(_txtGatewayPort.Text.Trim(), out port) || port <= 0 || port > 65535)
            {
                port = 8443;
                _txtGatewayPort.Text = "8443";
            }

            string vlan = _cboVlanTag.Text.Trim();
            if (string.IsNullOrEmpty(vlan))
            {
                vlan = "VLAN 10 - Tech";
            }

            string targetDir = _txtInstallPath.Text.Trim();
            if (string.IsNullOrEmpty(targetDir))
            {
                targetDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
                _txtInstallPath.Text = targetDir;
            }

            try
            {
                _lblLogStatus.Text = "Đang tạo thư mục và sao chép tập tin...";
                _lblLogStatus.ForeColor = ModernTheme.AccentYellow;

                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                string targetExe = Path.Combine(targetDir, "LanControl.Agent.exe");
                string targetDll = Path.Combine(targetDir, "LanControl.Core.dll");

                // If target files don't exist yet, copy from source
                if (!string.IsNullOrEmpty(_sourceAgentExe) && File.Exists(_sourceAgentExe))
                {
                    if (!string.Equals(Path.GetFullPath(_sourceAgentExe), Path.GetFullPath(targetExe), StringComparison.OrdinalIgnoreCase))
                    {
                        File.Copy(_sourceAgentExe, targetExe, true);
                    }
                }
                else if (!File.Exists(targetExe))
                {
                    MessageBox.Show("Không tìm thấy file nguồn LanControl.Agent.exe!\nVui lòng bấm [Tìm File Nguồn...] để chọn file.", "Lỗi Nguồn", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (!string.IsNullOrEmpty(_sourceCoreDll) && File.Exists(_sourceCoreDll))
                {
                    if (!string.Equals(Path.GetFullPath(_sourceCoreDll), Path.GetFullPath(targetDll), StringComparison.OrdinalIgnoreCase))
                    {
                        File.Copy(_sourceCoreDll, targetDll, true);
                    }
                }

                // Save config in target directory
                _lblLogStatus.Text = "Đang lưu cấu hình agent_config.json...";
                string configPath = Path.Combine(targetDir, "agent_config.json");
                AgentConfig cfg = AgentConfig.Load(configPath);
                cfg.GatewayHost = host;
                cfg.GatewayPort = port;
                cfg.VlanTag = vlan;
                cfg.Save(configPath);

                // Configure Auto-start & UAC
                _lblLogStatus.Text = "Đang thiết lập khởi động cùng Windows & quyền UAC...";
                SetAutoStart(targetExe, _chkAutoStart.Checked);
                ConfigureUacSupport(_chkFixUac.Checked);

                // Kill running old agent instance
                _lblLogStatus.Text = "Đang khởi động Agent...";
                KillRunningAgent();

                // Launch agent
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = targetExe,
                    Arguments = string.Format("\"{0}\" {1} \"{2}\"", host, port, vlan),
                    WorkingDirectory = targetDir,
                    UseShellExecute = true
                };
                Process.Start(psi);

                _lblLogStatus.Text = "✅ Đã cài đặt và khởi chạy LAN Control Agent thành công!";
                _lblLogStatus.ForeColor = ModernTheme.AccentGreen;

                MessageBox.Show(
                    string.Format("🎉 CÀI ĐẶT VÀ KHỞI CHẠY AGENT THÀNH CÔNG!\n\n" +
                                  "📁 Nơi lưu trữ: {0}\n" +
                                  "🌐 Máy chủ Gateway: {1}:{2}\n" +
                                  "🏷️ Phân đoạn VLAN: {3}\n" +
                                  "🚀 Khởi động cùng Windows: {4}\n\n" +
                                  "Biểu tượng chữ 'N' màu xanh đã xuất hiện tại khay hệ thống (System Tray góc dưới bên phải màn hình).\n" +
                                  "Máy trạm đã sẵn sàng kết nối tới máy chủ quản trị.",
                                  targetDir, host, port, vlan, _chkAutoStart.Checked ? "BẬT" : "TẮT"),
                    "Cài Đặt Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);

                UpdateAgentRunningStatus();
            }
            catch (Exception ex)
            {
                _lblLogStatus.Text = "❌ Lỗi: " + ex.Message;
                _lblLogStatus.ForeColor = ModernTheme.AccentRed;
                MessageBox.Show("Có lỗi xảy ra trong quá trình cài đặt:\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void ConfigureUacSupport(bool enable)
        {
            try
            {
                int val = enable ? 0 : 1;
                int adminVal = enable ? 0 : 5;
                Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "PromptOnSecureDesktop", val, RegistryValueKind.DWord);
                Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "EnableUIPI", val, RegistryValueKind.DWord);
                Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "ConsentPromptBehaviorAdmin", adminVal, RegistryValueKind.DWord);
                Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "EnableSecureUIAPaths", val, RegistryValueKind.DWord);
            }
            catch
            {
                try
                {
                    string cmd = string.Format(
                        "/c reg.exe add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\" /v PromptOnSecureDesktop /t REG_DWORD /d {0} /f & " +
                        "reg.exe add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\" /v EnableUIPI /t REG_DWORD /d {1} /f & " +
                        "reg.exe add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\" /v ConsentPromptBehaviorAdmin /t REG_DWORD /d {2} /f & " +
                        "reg.exe add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\" /v EnableSecureUIAPaths /t REG_DWORD /d {3} /f",
                        enable ? 0 : 1,
                        enable ? 0 : 1,
                        enable ? 0 : 5,
                        enable ? 0 : 1
                    );
                    ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", cmd)
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    using (Process p = Process.Start(psi))
                    {
                        p.WaitForExit(3000);
                    }
                }
                catch { }
            }
        }

        private static void SetAutoStart(string exePath, bool enable)
        {
            try
            {
                // 1. Windows Registry (HKCU Run Key)
                try
                {
                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                    {
                        if (key != null)
                        {
                            if (enable)
                            {
                                key.SetValue("LanControlAgent", "\"" + exePath + "\"");
                            }
                            else
                            {
                                key.DeleteValue("LanControlAgent", false);
                            }
                        }
                    }
                }
                catch { }

                // 2. Windows Registry (HKLM Run Key for All Users)
                try
                {
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                    {
                        if (key != null)
                        {
                            if (enable)
                            {
                                key.SetValue("LanControlAgent", "\"" + exePath + "\"");
                            }
                            else
                            {
                                key.DeleteValue("LanControlAgent", false);
                            }
                        }
                    }
                }
                catch { }

                // 3. User Startup Folder (.bat file)
                try
                {
                    string startupDir = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                    if (!string.IsNullOrEmpty(startupDir) && Directory.Exists(startupDir))
                    {
                        string startupBat = Path.Combine(startupDir, "LanControlAgent_AutoStart.bat");
                        if (enable)
                        {
                            string batContent = string.Format("@echo off\r\nstart \"\" \"{0}\"\r\n", exePath);
                            File.WriteAllText(startupBat, batContent);
                        }
                        else if (File.Exists(startupBat))
                        {
                            File.Delete(startupBat);
                        }
                    }
                }
                catch { }

                // 4. Common Startup Folder (All Users)
                try
                {
                    string commonStartupDir = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup);
                    if (!string.IsNullOrEmpty(commonStartupDir) && Directory.Exists(commonStartupDir))
                    {
                        string commonBat = Path.Combine(commonStartupDir, "LanControlAgent_AutoStart.bat");
                        if (enable)
                        {
                            string batContent = string.Format("@echo off\r\nstart \"\" \"{0}\"\r\n", exePath);
                            File.WriteAllText(commonBat, batContent);
                        }
                        else if (File.Exists(commonBat))
                        {
                            File.Delete(commonBat);
                        }
                    }
                }
                catch { }

                // 5. Task Scheduler with HIGHEST privileges
                try
                {
                    if (enable)
                    {
                        string taskCmd = string.Format("/create /tn \"LanControlAgent\" /tr \"\\\"{0}\\\"\" /sc onlogon /rl HIGHEST /f", exePath);
                        ProcessStartInfo psi = new ProcessStartInfo("schtasks.exe", taskCmd)
                        {
                            CreateNoWindow = true,
                            UseShellExecute = false
                        };
                        using (Process p = Process.Start(psi))
                        {
                            p.WaitForExit(3000);
                        }
                    }
                    else
                    {
                        ProcessStartInfo psi = new ProcessStartInfo("schtasks.exe", "/delete /tn \"LanControlAgent\" /f")
                        {
                            CreateNoWindow = true,
                            UseShellExecute = false
                        };
                        using (Process p = Process.Start(psi))
                        {
                            p.WaitForExit(3000);
                        }
                    }
                }
                catch { }
            }
            catch { }
        }
    }
}


