using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using Microsoft.Win32;
using LanControl.Core.Models;

namespace LanControl.ServerInstaller
{
    public class ServerSetupForm : Form
    {
        // Controls
        private TextBox _txtInstallPath;
        private Button _btnBrowseTarget;
        private Label _lblSourceStatus;
        private Button _btnBrowseSource;

        private TextBox _txtGatewayPort;
        private Button _btnResetPort;
        private ComboBox _cboServerIps;
        private Button _btnCopyIp;

        private CheckBox _chkOpenFirewall;
        private CheckBox _chkAutoStart;
        private CheckBox _chkCreateShortcuts;

        private Label _lblGatewayStatus;
        private Button _btnStopGateway;
        private Button _btnRestartGateway;
        private Button _btnLaunchControl;
        private Button _btnOpenFolder;

        private Button _btnSaveConfigOnly;
        private Button _btnUninstall;
        private Button _btnInstallAndLaunch;
        private Label _lblLogStatus;

        private string _sourceGatewayExe = "";
        private string _sourceControlExe = "";
        private string _sourceCoreDll = "";

        public ServerSetupForm()
        {
            InitializeComponent();
            FindSourceFiles();
            LoadServerIps();
            LoadExistingConfigOrDefaults();
            UpdateGatewayRunningStatus();
        }

        private void InitializeComponent()
        {
            this.Text = "Cài Đặt & Cấu Hình Máy Chủ LAN Control Server (Gateway & Control)";
            this.Size = new Size(760, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = ModernTheme.BackgroundDark;
            this.ForeColor = ModernTheme.TextPrimary;

            // 1. Top Banner
            Panel topBanner = new Panel
            {
                Dock = DockStyle.Top,
                Height = 74,
                BackColor = ModernTheme.PanelDark,
                Padding = new Padding(16, 12, 16, 10)
            };

            Label lblTitle = new Label
            {
                Text = "🛡️ TRÌNH CÀI ĐẶT & CẤU HÌNH MÁY CHỦ (LAN CONTROL SERVER SETUP)",
                Font = ModernTheme.HeaderFont,
                ForeColor = ModernTheme.AccentCyan,
                AutoSize = true,
                Location = new Point(14, 12)
            };

            Label lblSubTitle = new Label
            {
                Text = "Cài đặt Gateway, tự động mở tường lửa Firewall và khởi chạy máy chủ chỉ với 1 cú click chuột.",
                Font = ModernTheme.RegularFont,
                ForeColor = ModernTheme.TextSecondary,
                AutoSize = true,
                Location = new Point(16, 38)
            };

            topBanner.Controls.Add(lblTitle);
            topBanner.Controls.Add(lblSubTitle);
            this.Controls.Add(topBanner);

            // Main Container Panel
            Panel mainContainer = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(18, 12, 18, 12)
            };
            this.Controls.Add(mainContainer);
            mainContainer.BringToFront();

            int currentY = 12;

            // SECTION 1: Nơi Lưu Trữ Máy Chủ
            Panel cardPath = CreateSectionCard(ref currentY, 105, mainContainer);

            Label lblPathTitle = new Label
            {
                Text = "1. NƠI LƯU TRỮ CÁC TẬP TIN MÁY CHỦ TRÊN MÁY TÍNH (TARGET DIRECTORY):",
                Font = ModernTheme.SubHeaderFont,
                ForeColor = ModernTheme.AccentCyan,
                Location = new Point(12, 10),
                AutoSize = true
            };
            cardPath.Controls.Add(lblPathTitle);

            _txtInstallPath = new TextBox
            {
                Text = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\'),
                Location = new Point(14, 35),
                Size = new Size(560, 26),
                Font = ModernTheme.RegularFont,
                BackColor = ModernTheme.BackgroundDark,
                ForeColor = ModernTheme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle
            };
            cardPath.Controls.Add(_txtInstallPath);

            _btnBrowseTarget = new Button
            {
                Text = "📂 Duyệt...",
                Location = new Point(584, 34),
                Size = new Size(110, 28)
            };
            ModernTheme.ApplyToButton(_btnBrowseTarget, ModernTheme.AccentBlue, Color.White);
            _btnBrowseTarget.Click += (s, e) => BrowseTargetFolder();
            cardPath.Controls.Add(_btnBrowseTarget);

            Label lblPathHint = new Label
            {
                Text = "💡 Thư mục chứa LanControl.Gateway.exe, LanControl.Control.exe và các tệp cấu hình máy chủ.",
                Font = ModernTheme.SmallFont,
                ForeColor = ModernTheme.TextMuted,
                Location = new Point(14, 68),
                AutoSize = true
            };
            cardPath.Controls.Add(lblPathHint);

            // SECTION 2: Quét & Chọn Tệp Nguồn
            Panel cardSource = CreateSectionCard(ref currentY, 80, mainContainer);

            Label lblSrcTitle = new Label
            {
                Text = "2. NGUỒN TẬP TIN HỆ THỐNG MÁY CHỦ (SOURCE FILES):",
                Font = ModernTheme.SubHeaderFont,
                ForeColor = ModernTheme.AccentCyan,
                Location = new Point(12, 10),
                AutoSize = true
            };
            cardSource.Controls.Add(lblSrcTitle);

            _lblSourceStatus = new Label
            {
                Text = "🔍 Đang tìm kiếm tập tin hệ thống nguồn...",
                Font = ModernTheme.RegularFont,
                ForeColor = ModernTheme.AccentYellow,
                Location = new Point(14, 38),
                AutoSize = true
            };
            cardSource.Controls.Add(_lblSourceStatus);

            _btnBrowseSource = new Button
            {
                Text = "🔍 Chọn File...",
                Location = new Point(584, 32),
                Size = new Size(110, 28)
            };
            ModernTheme.ApplyToButton(_btnBrowseSource, ModernTheme.BorderDark, ModernTheme.TextPrimary);
            _btnBrowseSource.Click += (s, e) => BrowseSourceFiles();
            cardSource.Controls.Add(_btnBrowseSource);

            // SECTION 3: Cấu Hình Mạng, Cổng & IP Máy Chủ
            Panel cardNetwork = CreateSectionCard(ref currentY, 185, mainContainer);

            Label lblNetTitle = new Label
            {
                Text = "3. CẤU HÌNH CỔNG GATEWAY & ĐỊA CHỈ IP MÁY CHỦ (NETWORK CONFIG):",
                Font = ModernTheme.SubHeaderFont,
                ForeColor = ModernTheme.AccentCyan,
                Location = new Point(12, 10),
                AutoSize = true
            };
            cardNetwork.Controls.Add(lblNetTitle);

            // Port Row
            Label lblPort = new Label
            {
                Text = "Cổng kết nối Gateway (Port):",
                Font = ModernTheme.RegularFont,
                Location = new Point(14, 38),
                AutoSize = true
            };
            cardNetwork.Controls.Add(lblPort);

            _txtGatewayPort = new TextBox
            {
                Text = "8443",
                Location = new Point(230, 35),
                Size = new Size(90, 26),
                Font = ModernTheme.RegularFont,
                BackColor = ModernTheme.BackgroundDark,
                ForeColor = ModernTheme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle
            };
            cardNetwork.Controls.Add(_txtGatewayPort);

            _btnResetPort = new Button
            {
                Text = "🔄 Đặt lại 8443",
                Location = new Point(330, 34),
                Size = new Size(125, 28)
            };
            ModernTheme.ApplyToButton(_btnResetPort, ModernTheme.BorderDark, ModernTheme.TextSecondary);
            _btnResetPort.Click += (s, e) => { _txtGatewayPort.Text = "8443"; };
            cardNetwork.Controls.Add(_btnResetPort);

            Label lblPortNotice = new Label
            {
                Text = "⚠️ Cổng mặc định của hệ thống là 8443. Nếu bạn lỡ xóa nhầm hoặc quên cổng, hãy bấm [Đặt lại 8443] bên cạnh.",
                Font = ModernTheme.SmallFont,
                ForeColor = ModernTheme.AccentYellow,
                Location = new Point(14, 68),
                AutoSize = true
            };
            cardNetwork.Controls.Add(lblPortNotice);

            // IP List Row
            Label lblIpTitle = new Label
            {
                Text = "Địa chỉ IP Máy Chủ phát hiện:",
                Font = ModernTheme.RegularFont,
                Location = new Point(14, 100),
                AutoSize = true
            };
            cardNetwork.Controls.Add(lblIpTitle);

            _cboServerIps = new ComboBox
            {
                Location = new Point(230, 97),
                Size = new Size(344, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = ModernTheme.RegularFont,
                BackColor = ModernTheme.BackgroundDark,
                ForeColor = ModernTheme.AccentGreen
            };
            cardNetwork.Controls.Add(_cboServerIps);

            _btnCopyIp = new Button
            {
                Text = "📋 Copy IP",
                Location = new Point(584, 96),
                Size = new Size(110, 28)
            };
            ModernTheme.ApplyToButton(_btnCopyIp, ModernTheme.AccentBlue, Color.White);
            _btnCopyIp.Click += (s, e) => CopySelectedIp();
            cardNetwork.Controls.Add(_btnCopyIp);

            Label lblIpHlp = new Label
            {
                Text = "📌 Đây là địa chỉ IP của máy chủ này. Hãy nhập địa chỉ IP này vào chương trình cài đặt Agent trên các máy trạm.",
                Font = ModernTheme.SmallFont,
                ForeColor = ModernTheme.TextSecondary,
                Location = new Point(14, 134),
                AutoSize = true
            };
            cardNetwork.Controls.Add(lblIpHlp);

            // SECTION 4: Tự Động Hóa Hệ Thống (Firewall, Auto-Start, Shortcuts)
            Panel cardAutomation = CreateSectionCard(ref currentY, 135, mainContainer);

            Label lblAutoTitle = new Label
            {
                Text = "4. TỰ ĐỘNG HÓA HỆ THỐNG MÁY CHỦ (AUTOMATION OPTIONS):",
                Font = ModernTheme.SubHeaderFont,
                ForeColor = ModernTheme.AccentCyan,
                Location = new Point(12, 10),
                AutoSize = true
            };
            cardAutomation.Controls.Add(lblAutoTitle);

            _chkOpenFirewall = new CheckBox
            {
                Text = "Tự động mở cổng tường lửa Windows Defender Firewall (TCP Inbound Rule)",
                Checked = true,
                Font = ModernTheme.RegularFont,
                ForeColor = ModernTheme.AccentGreen,
                Location = new Point(16, 36),
                AutoSize = true
            };
            cardAutomation.Controls.Add(_chkOpenFirewall);

            _chkAutoStart = new CheckBox
            {
                Text = "Tự động khởi động Central Gateway cùng Windows khi bật máy chủ",
                Checked = true,
                Font = ModernTheme.RegularFont,
                ForeColor = ModernTheme.TextPrimary,
                Location = new Point(16, 64),
                AutoSize = true
            };
            cardAutomation.Controls.Add(_chkAutoStart);

            _chkCreateShortcuts = new CheckBox
            {
                Text = "Tạo biểu tượng Lối tắt (Desktop Shortcuts) cho Master Control và Gateway ngoài màn hình",
                Checked = true,
                Font = ModernTheme.RegularFont,
                ForeColor = ModernTheme.TextPrimary,
                Location = new Point(16, 92),
                AutoSize = true
            };
            cardAutomation.Controls.Add(_chkCreateShortcuts);

            // SECTION 5: Trạng Thái Gateway & Điều Khiển Nhanh
            Panel cardStatus = CreateSectionCard(ref currentY, 82, mainContainer);

            _lblGatewayStatus = new Label
            {
                Text = "⚪ Trạng thái Gateway: Đang kiểm tra...",
                Font = ModernTheme.SubHeaderFont,
                ForeColor = ModernTheme.TextSecondary,
                Location = new Point(14, 14),
                AutoSize = true
            };
            cardStatus.Controls.Add(_lblGatewayStatus);

            _btnStopGateway = new Button
            {
                Text = "⏹️ Dừng",
                Location = new Point(14, 42),
                Size = new Size(80, 28)
            };
            ModernTheme.ApplyToButton(_btnStopGateway, ModernTheme.AccentRed, Color.White);
            _btnStopGateway.Click += (s, e) => StopGatewayProcess();
            cardStatus.Controls.Add(_btnStopGateway);

            _btnRestartGateway = new Button
            {
                Text = "🔄 Khởi Động Lại",
                Location = new Point(100, 42),
                Size = new Size(125, 28)
            };
            ModernTheme.ApplyToButton(_btnRestartGateway, ModernTheme.AccentYellow, Color.Black);
            _btnRestartGateway.Click += (s, e) => RestartGatewayProcess();
            cardStatus.Controls.Add(_btnRestartGateway);

            _btnLaunchControl = new Button
            {
                Text = "🖥️ Master Control",
                Location = new Point(231, 42),
                Size = new Size(145, 28)
            };
            ModernTheme.ApplyToButton(_btnLaunchControl, ModernTheme.AccentBlue, Color.White);
            _btnLaunchControl.Click += (s, e) => LaunchMasterControl();
            cardStatus.Controls.Add(_btnLaunchControl);

            Button btnOpenGateway = new Button
            {
                Text = "🛡️ Central Gateway",
                Location = new Point(382, 42),
                Size = new Size(155, 28)
            };
            ModernTheme.ApplyToButton(btnOpenGateway, ModernTheme.AccentCyan, Color.Black);
            btnOpenGateway.Click += (s, e) => LaunchGatewayGui();
            cardStatus.Controls.Add(btnOpenGateway);

            _btnOpenFolder = new Button
            {
                Text = "📂 Thư Mục Server",
                Location = new Point(543, 42),
                Size = new Size(150, 28)
            };
            ModernTheme.ApplyToButton(_btnOpenFolder, ModernTheme.BorderDark, ModernTheme.TextPrimary);
            _btnOpenFolder.Click += (s, e) => OpenInstallFolder();
            cardStatus.Controls.Add(_btnOpenFolder);

            // SECTION 6: Các Nút Thao Tác Chính & Nhật Ký
            Panel actionPanel = new Panel
            {
                Location = new Point(0, currentY),
                Size = new Size(706, 110),
                BackColor = Color.Transparent
            };

            _btnSaveConfigOnly = new Button
            {
                Text = "💾 Lưu Cấu Hình",
                Location = new Point(10, 10),
                Size = new Size(130, 46),
                Font = ModernTheme.SubHeaderFont
            };
            ModernTheme.ApplyToButton(_btnSaveConfigOnly, ModernTheme.BorderDark, ModernTheme.TextPrimary);
            _btnSaveConfigOnly.Click += (s, e) => SaveConfigOnly();
            actionPanel.Controls.Add(_btnSaveConfigOnly);

            _btnUninstall = new Button
            {
                Text = "🗑️ Gỡ Cài Đặt",
                Location = new Point(148, 10),
                Size = new Size(130, 46),
                Font = ModernTheme.SubHeaderFont
            };
            ModernTheme.ApplyToButton(_btnUninstall, ModernTheme.BorderDark, ModernTheme.AccentRed);
            _btnUninstall.Click += (s, e) => DoUninstall();
            actionPanel.Controls.Add(_btnUninstall);

            _btnInstallAndLaunch = new Button
            {
                Text = "🚀 CÀI ĐẶT & KHỞI CHẠY GATEWAY NGAY (1-CLICK)",
                Location = new Point(286, 10),
                Size = new Size(410, 46),
                Font = new Font("Segoe UI", 11, FontStyle.Bold)
            };
            ModernTheme.ApplyToButton(_btnInstallAndLaunch, ModernTheme.AccentGreen, Color.Black);
            _btnInstallAndLaunch.Click += (s, e) => InstallAndLaunchGateway();
            actionPanel.Controls.Add(_btnInstallAndLaunch);

            _lblLogStatus = new Label
            {
                Text = "Sẵn sàng cài đặt hệ thống máy chủ LAN Control.",
                Font = ModernTheme.RegularFont,
                ForeColor = ModernTheme.TextSecondary,
                Location = new Point(14, 68),
                AutoSize = true
            };
            actionPanel.Controls.Add(_lblLogStatus);

            mainContainer.Controls.Add(actionPanel);
        }

        private Panel CreateSectionCard(ref int currentY, int height, Panel parent)
        {
            Panel card = new Panel
            {
                Location = new Point(0, currentY),
                Size = new Size(706, height),
                BackColor = ModernTheme.CardDark,
                BorderStyle = BorderStyle.None
            };
            parent.Controls.Add(card);
            currentY += height + 10;
            return card;
        }

        private void FindSourceFiles()
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string parentDir = Directory.GetParent(appDir) != null ? Directory.GetParent(appDir).FullName : appDir;
            string rootDir = Directory.GetParent(parentDir) != null ? Directory.GetParent(parentDir).FullName : parentDir;

            string[] candidateDirs = new string[]
            {
                appDir,
                Path.Combine(appDir, "bin"),
                Path.Combine(parentDir, "bin"),
                Path.Combine(rootDir, "bin"),
                parentDir,
                rootDir
            };

            foreach (string dir in candidateDirs)
            {
                if (!Directory.Exists(dir)) continue;

                string gwPath = Path.Combine(dir, "LanControl.Gateway.exe");
                string ctrlPath = Path.Combine(dir, "LanControl.Control.exe");
                string corePath = Path.Combine(dir, "LanControl.Core.dll");

                if (File.Exists(gwPath) && string.IsNullOrEmpty(_sourceGatewayExe)) _sourceGatewayExe = gwPath;
                if (File.Exists(ctrlPath) && string.IsNullOrEmpty(_sourceControlExe)) _sourceControlExe = ctrlPath;
                if (File.Exists(corePath) && string.IsNullOrEmpty(_sourceCoreDll)) _sourceCoreDll = corePath;
            }

            int count = 0;
            if (!string.IsNullOrEmpty(_sourceGatewayExe)) count++;
            if (!string.IsNullOrEmpty(_sourceControlExe)) count++;
            if (!string.IsNullOrEmpty(_sourceCoreDll)) count++;

            if (count == 3)
            {
                _lblSourceStatus.Text = string.Format("✅ Đã tìm thấy đầy đủ 3/3 file hệ thống nguồn: {0}", Path.GetDirectoryName(_sourceGatewayExe));
                _lblSourceStatus.ForeColor = ModernTheme.AccentGreen;
            }
            else if (count > 0)
            {
                _lblSourceStatus.Text = string.Format("⚠️ Đã tìm thấy {0}/3 file hệ thống. Bấm 'Chọn File...' nếu cần chỉ định thêm.", count);
                _lblSourceStatus.ForeColor = ModernTheme.AccentYellow;
            }
            else
            {
                _lblSourceStatus.Text = "❌ Chưa tìm thấy file nguồn (LanControl.Gateway.exe, Control.exe, Core.dll)!";
                _lblSourceStatus.ForeColor = ModernTheme.AccentRed;
            }
        }

        private void LoadServerIps()
        {
            try
            {
                _cboServerIps.Items.Clear();
                string hostName = Dns.GetHostName();
                IPAddress[] addresses = Dns.GetHostAddresses(hostName);

                List<string> ipList = new List<string>();
                string preferredIp = "";

                foreach (IPAddress ip in addresses)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        string ipStr = ip.ToString();
                        if (ipStr.StartsWith("127.")) continue;

                        if (string.IsNullOrEmpty(preferredIp) &&
                            (ipStr.StartsWith("192.168.") || ipStr.StartsWith("10.") || ipStr.StartsWith("172.")))
                        {
                            preferredIp = ipStr;
                        }
                        ipList.Add(ipStr);
                    }
                }

                ipList.Add("127.0.0.1 (Localhost / Test máy này)");

                foreach (string ip in ipList)
                {
                    _cboServerIps.Items.Add(ip);
                }

                if (!string.IsNullOrEmpty(preferredIp))
                {
                    for (int i = 0; i < _cboServerIps.Items.Count; i++)
                    {
                        if (_cboServerIps.Items[i].ToString().StartsWith(preferredIp))
                        {
                            _cboServerIps.SelectedIndex = i;
                            break;
                        }
                    }
                }

                if (_cboServerIps.SelectedIndex < 0 && _cboServerIps.Items.Count > 0)
                {
                    _cboServerIps.SelectedIndex = 0;
                }
            }
            catch
            {
                _cboServerIps.Items.Add("127.0.0.1");
                _cboServerIps.SelectedIndex = 0;
            }
        }

        private void CopySelectedIp()
        {
            if (_cboServerIps.SelectedItem != null)
            {
                string raw = _cboServerIps.SelectedItem.ToString();
                string ip = raw.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[0].Trim();
                try
                {
                    Clipboard.SetText(ip);
                    _lblLogStatus.Text = string.Format("📋 Đã copy địa chỉ IP '{0}' vào bộ nhớ tạm (Clipboard)!", ip);
                    _lblLogStatus.ForeColor = ModernTheme.AccentGreen;
                    MessageBox.Show(string.Format("Đã sao chép địa chỉ IP '{0}' vào bộ nhớ tạm!\nBạn có thể dán vào file cài đặt của máy trạm Agent.", ip),
                                    "Đã Sao Chép", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch { }
            }
        }

        private void LoadExistingConfigOrDefaults()
        {
            string defaultPath = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
            _txtInstallPath.Text = defaultPath;

            string cfgPath = Path.Combine(defaultPath, "gateway_config.json");
            if (!File.Exists(cfgPath))
            {
                cfgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gateway_config.json");
            }

            if (File.Exists(cfgPath))
            {
                try
                {
                    GatewayConfig cfg = GatewayConfig.Load(cfgPath);
                    if (cfg != null)
                    {
                        if (cfg.Port > 0 && cfg.Port <= 65535)
                        {
                            _txtGatewayPort.Text = cfg.Port.ToString();
                        }
                        _chkAutoStart.Checked = cfg.AutoStart;
                        _chkOpenFirewall.Checked = cfg.ConfigureFirewall;
                        _chkCreateShortcuts.Checked = cfg.CreateDesktopShortcuts;
                        // Always keep current directory as default target unless text is empty
                        if (string.IsNullOrEmpty(_txtInstallPath.Text.Trim()))
                        {
                            _txtInstallPath.Text = defaultPath;
                        }
                    }
                }
                catch { }
            }
        }

        private void BrowseTargetFolder()
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Chọn thư mục lưu trữ các tập tin máy chủ LAN Control:";
                fbd.SelectedPath = _txtInstallPath.Text;
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    _txtInstallPath.Text = fbd.SelectedPath;
                }
            }
        }

        private void BrowseSourceFiles()
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Chọn tệp LanControl.Gateway.exe nguồn:";
                ofd.Filter = "LAN Control Gateway (LanControl.Gateway.exe)|LanControl.Gateway.exe|All files (*.*)|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    string dir = Path.GetDirectoryName(ofd.FileName);
                    _sourceGatewayExe = ofd.FileName;
                    string ctrl = Path.Combine(dir, "LanControl.Control.exe");
                    string core = Path.Combine(dir, "LanControl.Core.dll");
                    if (File.Exists(ctrl)) _sourceControlExe = ctrl;
                    if (File.Exists(core)) _sourceCoreDll = core;
                    FindSourceFiles();
                }
            }
        }

        private void UpdateGatewayRunningStatus()
        {
            try
            {
                Process[] procs = Process.GetProcessesByName("LanControl.Gateway");
                if (procs != null && procs.Length > 0)
                {
                    _lblGatewayStatus.Text = string.Format("🟢 Gateway đang chạy (Cổng {0} - PID: {1})", _txtGatewayPort.Text.Trim(), procs[0].Id);
                    _lblGatewayStatus.ForeColor = ModernTheme.AccentGreen;
                    _btnStopGateway.Enabled = true;
                    _btnRestartGateway.Enabled = true;
                }
                else
                {
                    _lblGatewayStatus.Text = "⚪ Gateway chưa chạy trên máy này.";
                    _lblGatewayStatus.ForeColor = ModernTheme.TextSecondary;
                    _btnStopGateway.Enabled = false;
                    _btnRestartGateway.Enabled = false;
                }
            }
            catch
            {
                _lblGatewayStatus.Text = "⚪ Không thể lấy thông tin tiến trình.";
                _lblGatewayStatus.ForeColor = ModernTheme.TextMuted;
            }
        }

        private void StopGatewayProcess()
        {
            try
            {
                Process[] procs = Process.GetProcessesByName("LanControl.Gateway");
                foreach (Process p in procs)
                {
                    try { p.Kill(); } catch { }
                }
                _lblLogStatus.Text = "⏹️ Đã dừng toàn bộ tiến trình LanControl.Gateway.";
                _lblLogStatus.ForeColor = ModernTheme.AccentYellow;
                UpdateGatewayRunningStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi dừng Gateway: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RestartGatewayProcess()
        {
            StopGatewayProcess();
            System.Threading.Thread.Sleep(500);

            string targetDir = _txtInstallPath.Text.Trim();
            string targetExe = Path.Combine(targetDir, "LanControl.Gateway.exe");
            if (!File.Exists(targetExe))
            {
                targetExe = _sourceGatewayExe;
                targetDir = Path.GetDirectoryName(targetExe);
            }

            if (!File.Exists(targetExe))
            {
                MessageBox.Show("Không tìm thấy LanControl.Gateway.exe để khởi chạy lại!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int port = 8443;
            int.TryParse(_txtGatewayPort.Text.Trim(), out port);

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = targetExe,
                Arguments = port.ToString(),
                WorkingDirectory = targetDir,
                UseShellExecute = true
            };
            Process.Start(psi);

            _lblLogStatus.Text = "🔄 Đã khởi động lại Central Gateway trên cổng " + port;
            _lblLogStatus.ForeColor = ModernTheme.AccentGreen;
            System.Threading.Thread.Sleep(500);
            UpdateGatewayRunningStatus();
        }

        private void LaunchMasterControl()
        {
            string targetDir = _txtInstallPath.Text.Trim();
            string targetExe = Path.Combine(targetDir, "LanControl.Control.exe");
            if (!File.Exists(targetExe))
            {
                targetExe = _sourceControlExe;
                targetDir = Path.GetDirectoryName(targetExe);
            }

            if (File.Exists(targetExe))
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = targetExe,
                    WorkingDirectory = targetDir,
                    UseShellExecute = true
                };
                Process.Start(psi);
                _lblLogStatus.Text = "🖥️ Đã mở Master Control Console thành công.";
                _lblLogStatus.ForeColor = ModernTheme.AccentCyan;
            }
            else
            {
                MessageBox.Show("Chưa tìm thấy file LanControl.Control.exe!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void LaunchGatewayGui()
        {
            string targetDir = _txtInstallPath.Text.Trim();
            string targetExe = Path.Combine(targetDir, "LanControl.Gateway.exe");
            if (!File.Exists(targetExe))
            {
                targetExe = _sourceGatewayExe;
                targetDir = Path.GetDirectoryName(targetExe);
            }

            if (File.Exists(targetExe))
            {
                int port = 8443;
                int.TryParse(_txtGatewayPort.Text.Trim(), out port);
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = targetExe,
                        Arguments = port.ToString(),
                        WorkingDirectory = targetDir,
                        UseShellExecute = true
                    };
                    Process.Start(psi);
                    _lblLogStatus.Text = "🛡️ Đã mở giao diện Central Gateway thành công.";
                    _lblLogStatus.ForeColor = ModernTheme.AccentCyan;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi mở Central Gateway: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Chưa tìm thấy file LanControl.Gateway.exe!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OpenInstallFolder()
        {
            string targetDir = _txtInstallPath.Text.Trim();
            if (Directory.Exists(targetDir))
            {
                Process.Start("explorer.exe", "\"" + targetDir + "\"");
            }
            else
            {
                MessageBox.Show("Thư mục cài đặt chưa tồn tại: " + targetDir, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void SaveConfigOnly()
        {
            try
            {
                string targetDir = _txtInstallPath.Text.Trim();
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                int port = 8443;
                if (!int.TryParse(_txtGatewayPort.Text.Trim(), out port) || port <= 0 || port > 65535)
                {
                    port = 8443;
                    _txtGatewayPort.Text = "8443";
                }

                GatewayConfig cfg = new GatewayConfig
                {
                    Port = port,
                    InstallPath = targetDir,
                    AutoStart = _chkAutoStart.Checked,
                    ConfigureFirewall = _chkOpenFirewall.Checked,
                    CreateDesktopShortcuts = _chkCreateShortcuts.Checked
                };

                string cfgPath = Path.Combine(targetDir, "gateway_config.json");
                cfg.Save(cfgPath);

                string exePath = Path.Combine(targetDir, "LanControl.Gateway.exe");
                SetAutoStart(exePath, port, _chkAutoStart.Checked);

                if (_chkOpenFirewall.Checked)
                {
                    ConfigureWindowsFirewall(port);
                }

                _lblLogStatus.Text = string.Format("💾 Đã lưu cấu hình Gateway vào {0}", cfgPath);
                _lblLogStatus.ForeColor = ModernTheme.AccentGreen;
                MessageBox.Show("Đã lưu cấu hình máy chủ thành công!\nFile: " + cfgPath, "Lưu Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi lưu cấu hình: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DoUninstall()
        {
            DialogResult dr = MessageBox.Show(
                "Bạn có chắc chắn muốn gỡ cấu hình máy chủ LAN Control Gateway trên máy này?\n\n" +
                "Hành động này sẽ thực hiện:\n" +
                "• Dừng tiến trình LAN Control Central Gateway đang chạy.\n" +
                "• Xóa tác vụ tự khởi động cùng Windows (Registry & Startup Folder).\n" +
                "• Xóa quy tắc mở cổng trên tường lửa Windows Defender Firewall (TCP 8443).\n" +
                "• Xóa tệp cấu hình máy chủ (gateway_config.json).\n" +
                "• Xóa các biểu tượng lối tắt Desktop của Gateway và Master Control.\n\n" +
                "LƯU Ý: Các tệp chương trình (.exe, .dll) KHÔNG bị xóa, bạn có thể cài đặt lại bất cứ lúc nào.",
                "Xác Nhận Gỡ Cài Đặt Máy Chủ",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (dr != DialogResult.Yes) return;

            try
            {
                // 1. Stop Gateway process
                StopGatewayProcess();

                // 2. Remove auto-start from Windows Registry and Startup folder
                SetAutoStart("", 0, false);
                _chkAutoStart.Checked = false;

                // 3. Remove Windows Firewall rule
                try
                {
                    ProcessStartInfo psiDel = new ProcessStartInfo("netsh", "advfirewall firewall delete rule name=\"LAN Control Gateway\"")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    using (Process p = Process.Start(psiDel))
                    {
                        p.WaitForExit(3000);
                    }
                }
                catch { }

                // 4. Remove Desktop shortcuts
                try
                {
                    string desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                    string controlLnk = Path.Combine(desktopDir, "LAN Control Master Control.lnk");
                    string gatewayLnk = Path.Combine(desktopDir, "LAN Control Central Gateway.lnk");
                    if (File.Exists(controlLnk)) File.Delete(controlLnk);
                    if (File.Exists(gatewayLnk)) File.Delete(gatewayLnk);
                }
                catch { }

                // 5. Delete config files
                string targetDir = _txtInstallPath.Text.Trim();
                string cfgPath = Path.Combine(targetDir, "gateway_config.json");
                if (File.Exists(cfgPath))
                {
                    try { File.Delete(cfgPath); } catch { }
                }

                string localCfg = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gateway_config.json");
                if (File.Exists(localCfg))
                {
                    try { File.Delete(localCfg); } catch { }
                }

                _lblLogStatus.Text = "🗑️ Đã gỡ bỏ cấu hình máy chủ, tường lửa và tác vụ khởi động cùng Windows thành công!";
                _lblLogStatus.ForeColor = ModernTheme.AccentGreen;
                UpdateGatewayRunningStatus();

                MessageBox.Show(
                    "Đã gỡ bỏ cấu hình máy chủ, luật tường lửa Firewall và tác vụ tự khởi động thành công!\n\n" +
                    "• Tiến trình Gateway đã dừng.\n" +
                    "• Quy tắc tường lửa cho cổng 8443 đã được xóa.\n" +
                    "• Tác vụ tự khởi động cùng Windows và lối tắt Desktop đã được xóa bỏ.\n" +
                    "• Các tệp chương trình máy chủ vẫn được giữ nguyên vẹn trên máy tính.",
                    "Gỡ Cài Đặt Hoàn Tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _lblLogStatus.Text = "❌ Lỗi khi gỡ cài đặt máy chủ: " + ex.Message;
                _lblLogStatus.ForeColor = ModernTheme.AccentRed;
                MessageBox.Show("Có lỗi xảy ra khi gỡ cài đặt:\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InstallAndLaunchGateway()
        {
            try
            {
                string targetDir = _txtInstallPath.Text.Trim();
                if (string.IsNullOrEmpty(targetDir))
                {
                    targetDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
                    _txtInstallPath.Text = targetDir;
                }

                int port = 8443;
                if (!int.TryParse(_txtGatewayPort.Text.Trim(), out port) || port <= 0 || port > 65535)
                {
                    MessageBox.Show("Cổng Gateway không hợp lệ! Mặc định là 8443.", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _txtGatewayPort.Text = "8443";
                    port = 8443;
                }

                if (string.IsNullOrEmpty(_sourceGatewayExe) || !File.Exists(_sourceGatewayExe))
                {
                    string directGw = Path.Combine(targetDir, "LanControl.Gateway.exe");
                    if (File.Exists(directGw))
                    {
                        _sourceGatewayExe = directGw;
                    }
                    else
                    {
                        MessageBox.Show("Chưa tìm thấy file nguồn LanControl.Gateway.exe!\nVui lòng bấm 'Chọn File...' để chọn tập tin nguồn.",
                                        "Thiếu Tệp Nguồn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                if (string.IsNullOrEmpty(_sourceControlExe) || !File.Exists(_sourceControlExe))
                {
                    string directCtrl = Path.Combine(targetDir, "LanControl.Control.exe");
                    if (File.Exists(directCtrl)) _sourceControlExe = directCtrl;
                }

                if (string.IsNullOrEmpty(_sourceCoreDll) || !File.Exists(_sourceCoreDll))
                {
                    string directCore = Path.Combine(targetDir, "LanControl.Core.dll");
                    if (File.Exists(directCore)) _sourceCoreDll = directCore;
                }

                _lblLogStatus.Text = "⏳ Đang chuẩn bị thư mục đích...";
                _lblLogStatus.ForeColor = ModernTheme.AccentYellow;
                Application.DoEvents();

                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                // Copy files
                string targetGateway = Path.Combine(targetDir, "LanControl.Gateway.exe");
                string targetControl = Path.Combine(targetDir, "LanControl.Control.exe");
                string targetCore = Path.Combine(targetDir, "LanControl.Core.dll");

                try
                {
                    StopGatewayProcess();
                    System.Threading.Thread.Sleep(300);
                }
                catch { }

                _lblLogStatus.Text = "⏳ Đang sao chép các tập tin máy chủ...";
                Application.DoEvents();

                if (File.Exists(_sourceGatewayExe))
                {
                    if (!string.Equals(Path.GetFullPath(_sourceGatewayExe), Path.GetFullPath(targetGateway), StringComparison.OrdinalIgnoreCase))
                    {
                        File.Copy(_sourceGatewayExe, targetGateway, true);
                    }
                }
                if (!string.IsNullOrEmpty(_sourceControlExe) && File.Exists(_sourceControlExe))
                {
                    if (!string.Equals(Path.GetFullPath(_sourceControlExe), Path.GetFullPath(targetControl), StringComparison.OrdinalIgnoreCase))
                    {
                        File.Copy(_sourceControlExe, targetControl, true);
                    }
                }
                if (!string.IsNullOrEmpty(_sourceCoreDll) && File.Exists(_sourceCoreDll))
                {
                    if (!string.Equals(Path.GetFullPath(_sourceCoreDll), Path.GetFullPath(targetCore), StringComparison.OrdinalIgnoreCase))
                    {
                        File.Copy(_sourceCoreDll, targetCore, true);
                    }
                }

                // Save config
                GatewayConfig cfg = new GatewayConfig
                {
                    Port = port,
                    InstallPath = targetDir,
                    AutoStart = _chkAutoStart.Checked,
                    ConfigureFirewall = _chkOpenFirewall.Checked,
                    CreateDesktopShortcuts = _chkCreateShortcuts.Checked
                };
                cfg.Save(Path.Combine(targetDir, "gateway_config.json"));

                // Configure Windows Firewall
                if (_chkOpenFirewall.Checked)
                {
                    _lblLogStatus.Text = "🛡️ Đang cấu hình tường lửa Windows Defender Firewall...";
                    Application.DoEvents();
                    ConfigureWindowsFirewall(port);
                }

                // Auto Start
                SetAutoStart(targetGateway, port, _chkAutoStart.Checked);

                // Create Desktop Shortcuts
                if (_chkCreateShortcuts.Checked)
                {
                    _lblLogStatus.Text = "📌 Đang tạo lối tắt Desktop Shortcuts...";
                    Application.DoEvents();
                    CreateDesktopShortcuts(targetControl, targetGateway, port);
                }

                // Launch Gateway
                _lblLogStatus.Text = "🚀 Đang khởi chạy Central Gateway...";
                Application.DoEvents();

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = targetGateway,
                    Arguments = port.ToString(),
                    WorkingDirectory = targetDir,
                    UseShellExecute = true
                };
                Process.Start(psi);

                _lblLogStatus.Text = "✅ Đã cài đặt và khởi chạy Central Gateway thành công!";
                _lblLogStatus.ForeColor = ModernTheme.AccentGreen;

                string selectedIp = _cboServerIps.SelectedItem != null ? _cboServerIps.SelectedItem.ToString() : "127.0.0.1";
                string cleanIp = selectedIp.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[0].Trim();

                MessageBox.Show(
                    string.Format("🎉 CÀI ĐẶT VÀ KHỞI CHẠY MÁY CHỦ THÀNH CÔNG!\n\n" +
                                  "📁 Nơi lưu trữ: {0}\n" +
                                  "🌐 Địa chỉ IP máy chủ: {1}\n" +
                                  "🔌 Cổng Gateway lắng nghe: {2}\n" +
                                  "🛡️ Mở tường lửa Firewall TCP {2}: {3}\n" +
                                  "🚀 Tự khởi động cùng Windows: {4}\n" +
                                  "📌 Tạo biểu tượng Desktop: {5}\n\n" +
                                  "Máy chủ Central Gateway đã hoạt động sẵn sàng nhận kết nối từ các máy trạm (Agent).\n" +
                                  "Quản trị viên có thể mở 'LAN Control Master Control' từ màn hình Desktop để quản lý.",
                                  targetDir, cleanIp, port,
                                  _chkOpenFirewall.Checked ? "ĐÃ MỞ THÀNH CÔNG" : "KHÔNG",
                                  _chkAutoStart.Checked ? "BẬT" : "TẮT",
                                  _chkCreateShortcuts.Checked ? "ĐÃ TẠO" : "KHÔNG"),
                    "Cài Đặt Máy Chủ Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);

                UpdateGatewayRunningStatus();
            }
            catch (Exception ex)
            {
                _lblLogStatus.Text = "❌ Lỗi: " + ex.Message;
                _lblLogStatus.ForeColor = ModernTheme.AccentRed;
                MessageBox.Show("Có lỗi xảy ra trong quá trình cài đặt máy chủ:\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void ConfigureWindowsFirewall(int port)
        {
            try
            {
                // Execute netsh advfirewall command
                // Delete old rule if present, then add new rule
                string delCmd = "advfirewall firewall delete rule name=\"LAN Control Gateway\"";
                string addCmd = string.Format("advfirewall firewall add rule name=\"LAN Control Gateway\" dir=in action=allow protocol=TCP localport={0}", port);

                ProcessStartInfo psiDel = new ProcessStartInfo("netsh", delCmd)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using (Process p = Process.Start(psiDel))
                {
                    p.WaitForExit(3000);
                }

                ProcessStartInfo psiAdd = new ProcessStartInfo("netsh", addCmd)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using (Process p = Process.Start(psiAdd))
                {
                    p.WaitForExit(3000);
                }
            }
            catch { }
        }

        private static void SetAutoStart(string exePath, int port, bool enable)
        {
            try
            {
                // 1. Registry HKCU Run
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null)
                    {
                        if (enable)
                        {
                            key.SetValue("LanControlGateway", string.Format("\"{0}\" {1}", exePath, port));
                        }
                        else
                        {
                            key.DeleteValue("LanControlGateway", false);
                        }
                    }
                }

                // 2. Windows Startup Folder Redundancy (.bat file)
                string startupDir = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                string startupBat = Path.Combine(startupDir, "LanControlGateway_AutoStart.bat");
                if (enable)
                {
                    string batContent = string.Format("@echo off\r\nstart \"\" \"{0}\" {1}\r\n", exePath, port);
                    File.WriteAllText(startupBat, batContent);
                }
                else
                {
                    if (File.Exists(startupBat))
                    {
                        File.Delete(startupBat);
                    }
                }
            }
            catch { }
        }

        private static void CreateDesktopShortcuts(string controlExe, string gatewayExe, int port)
        {
            try
            {
                string desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

                // Helper to create .lnk shortcut via VBScript
                string vbsPath = Path.Combine(Path.GetTempPath(), "CreateShortcut_" + Guid.NewGuid().ToString("N") + ".vbs");

                string controlLnk = Path.Combine(desktopDir, "LAN Control Master Control.lnk");
                string gatewayLnk = Path.Combine(desktopDir, "LAN Control Central Gateway.lnk");

                string vbsScript = string.Format(
                    "Set oWS = WScript.CreateObject(\"WScript.Shell\")\r\n" +
                    "Set oLink = oWS.CreateShortcut(\"{0}\")\r\n" +
                    "oLink.TargetPath = \"{1}\"\r\n" +
                    "oLink.WorkingDirectory = \"{2}\"\r\n" +
                    "oLink.Description = \"LAN Control Master Management Console\"\r\n" +
                    "oLink.Save\r\n\r\n" +
                    "Set oLink2 = oWS.CreateShortcut(\"{3}\")\r\n" +
                    "oLink2.TargetPath = \"{4}\"\r\n" +
                    "oLink2.Arguments = \"{5}\"\r\n" +
                    "oLink2.WorkingDirectory = \"{6}\"\r\n" +
                    "oLink2.Description = \"LAN Control Multi-VLAN Central Gateway\"\r\n" +
                    "oLink2.Save\r\n",
                    controlLnk, controlExe, Path.GetDirectoryName(controlExe),
                    gatewayLnk, gatewayExe, port, Path.GetDirectoryName(gatewayExe)
                );

                File.WriteAllText(vbsPath, vbsScript);

                ProcessStartInfo psi = new ProcessStartInfo("wscript.exe", "\"" + vbsPath + "\"")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using (Process p = Process.Start(psi))
                {
                    p.WaitForExit(3000);
                }

                if (File.Exists(vbsPath))
                {
                    File.Delete(vbsPath);
                }
            }
            catch { }
        }
    }
}
