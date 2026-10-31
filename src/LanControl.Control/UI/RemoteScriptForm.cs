using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using LanControl.Core.Models;

namespace LanControl.Control.UI
{
    public class RemoteScriptForm : Form
    {
        private readonly ControlClient _client;
        private readonly List<AgentInfo> _targetAgents;
        private readonly string _targetVlan;
        private readonly List<AgentInfo> _allAgents;

        private enum TargetScope
        {
            All,
            Vlan,
            SelectedGroup,
            SingleMachine
        }

        private class TargetItem
        {
            public TargetScope Scope { get; set; }
            public AgentInfo Agent { get; set; }
            public string DisplayText { get; set; }
            public string Vlan { get; set; }

            public override string ToString()
            {
                return DisplayText;
            }
        }

        private ComboBox _cboTarget;
        private RadioButton _rbPowerShell;
        private RadioButton _rbCmd;
        private ComboBox _cboTemplates;
        private TextBox _txtScript;
        private Button _btnExecute;
        private TextBox _txtOutput;
        private Label _lblStatus;

        public RemoteScriptForm(ControlClient client, List<AgentInfo> targetAgents = null, string targetVlan = "", List<AgentInfo> allAgents = null)
        {
            _client = client;
            _targetAgents = targetAgents ?? new List<AgentInfo>();
            _targetVlan = targetVlan;
            _allAgents = allAgents ?? new List<AgentInfo>();
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "LAN Control - Thực Thi Lệnh & Script Hàng Loạt (Mass Remote Scripting)";
            this.Size = new Size(1060, 720);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = ModernTheme.BackgroundDark;
            this.ForeColor = ModernTheme.TextPrimary;

            // Top Configuration Panel
            Panel topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 138,
                BackColor = ModernTheme.PanelDark,
                Padding = new Padding(12)
            };

            Label lblTarget = new Label
            {
                Text = "Mục tiêu thực thi:",
                Font = ModernTheme.RegularFont,
                ForeColor = ModernTheme.TextSecondary,
                Location = new Point(15, 16),
                AutoSize = true
            };

            _cboTarget = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(135, 12),
                Width = 270,
                BackColor = ModernTheme.CardDark,
                ForeColor = ModernTheme.TextPrimary,
                Font = ModernTheme.RegularFont
            };

            _cboTarget.Items.Add(new TargetItem { Scope = TargetScope.All, DisplayText = "🌐 Tất cả máy trạm (All Online)" });
            if (!string.IsNullOrEmpty(_targetVlan))
            {
                _cboTarget.Items.Add(new TargetItem { Scope = TargetScope.Vlan, Vlan = _targetVlan, DisplayText = "🏢 Toàn bộ phân đoạn: " + _targetVlan });
            }
            if (_targetAgents.Count > 1)
            {
                _cboTarget.Items.Add(new TargetItem { Scope = TargetScope.SelectedGroup, DisplayText = string.Format("🎯 Các máy được chọn ({0} máy)", _targetAgents.Count) });
            }

            // Populate all individual online machines
            Dictionary<string, AgentInfo> machineDict = new Dictionary<string, AgentInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (AgentInfo a in _allAgents)
            {
                if (a != null && !string.IsNullOrEmpty(a.AgentId)) machineDict[a.AgentId] = a;
            }
            foreach (AgentInfo a in _targetAgents)
            {
                if (a != null && !string.IsNullOrEmpty(a.AgentId)) machineDict[a.AgentId] = a;
            }

            List<AgentInfo> sortedAgents = new List<AgentInfo>(machineDict.Values);
            sortedAgents.Sort((x, y) => string.Compare(x.Hostname ?? x.AgentId, y.Hostname ?? y.AgentId, StringComparison.OrdinalIgnoreCase));

            foreach (AgentInfo a in sortedAgents)
            {
                _cboTarget.Items.Add(new TargetItem
                {
                    Scope = TargetScope.SingleMachine,
                    Agent = a,
                    DisplayText = string.Format("🖥️ Máy: {0} ({1})", a.Hostname, a.IpAddress)
                });
            }

            int defaultIndex = 0;
            if (_targetAgents.Count == 1)
            {
                string targetId = _targetAgents[0].AgentId;
                for (int i = 0; i < _cboTarget.Items.Count; i++)
                {
                    TargetItem item = _cboTarget.Items[i] as TargetItem;
                    if (item != null && item.Scope == TargetScope.SingleMachine && item.Agent != null && item.Agent.AgentId == targetId)
                    {
                        defaultIndex = i;
                        break;
                    }
                }
            }
            else if (_targetAgents.Count > 1)
            {
                for (int i = 0; i < _cboTarget.Items.Count; i++)
                {
                    TargetItem item = _cboTarget.Items[i] as TargetItem;
                    if (item != null && item.Scope == TargetScope.SelectedGroup)
                    {
                        defaultIndex = i;
                        break;
                    }
                }
            }
            _cboTarget.SelectedIndex = defaultIndex;

            _rbPowerShell = new RadioButton
            {
                Text = "PowerShell (.ps1)",
                Checked = true,
                Location = new Point(415, 13),
                AutoSize = true,
                ForeColor = ModernTheme.AccentCyan,
                Font = ModernTheme.SubHeaderFont
            };

            _rbCmd = new RadioButton
            {
                Text = "CMD / Batch (.bat)",
                Location = new Point(565, 13),
                AutoSize = true,
                ForeColor = ModernTheme.AccentYellow,
                Font = ModernTheme.SubHeaderFont
            };

            Label lblTemplate = new Label
            {
                Text = "Mẫu lệnh mẫu:",
                Font = ModernTheme.RegularFont,
                ForeColor = ModernTheme.TextSecondary,
                Location = new Point(15, 52),
                AutoSize = true
            };

            _cboTemplates = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(135, 48),
                Width = 410,
                BackColor = ModernTheme.CardDark,
                ForeColor = ModernTheme.TextPrimary,
                Font = ModernTheme.RegularFont
            };
            _cboTemplates.Items.Add("-- Chọn Lệnh Mẫu Nhanh --");
            _cboTemplates.Items.Add("🔍 Kiểm kê toàn bộ phần cứng, màn hình, máy in, máy quét USB (Model, Serial...)");
            _cboTemplates.Items.Add("🧹 Xóa cache & dữ liệu toàn bộ trình duyệt (Chrome, Edge, Cốc Cốc, Firefox...)");
            _cboTemplates.Items.Add("🖨️ Khởi động lại service Print Spooler (Sửa kẹt lệnh in & Xóa spool)");
            _cboTemplates.Items.Add("Kiểm tra cấu hình IP (ipconfig /all)");
            _cboTemplates.Items.Add("Top 10 tiến trình tốn CPU nhất (PowerShell)");
            _cboTemplates.Items.Add("Danh sách dịch vụ đang chạy (Get-Service)");
            _cboTemplates.Items.Add("Dung lượng ổ đĩa (Get-PSDrive -PSProvider FileSystem)");
            _cboTemplates.Items.Add("Xóa file rác thư mục Temp (Clean Temp)");
            _cboTemplates.SelectedIndex = 1; // Default to the comprehensive hardware/peripherals audit
            _cboTemplates.SelectedIndexChanged += OnTemplateChanged;

            _btnExecute = new Button
            {
                Text = "⚡ Thực Thi Lệnh (Execute)",
                Location = new Point(560, 46),
                Size = new Size(210, 32)
            };
            ModernTheme.ApplyToButton(_btnExecute, ModernTheme.AccentGreen, Color.White);
            _btnExecute.Click += OnExecuteClicked;

            _lblStatus = new Label
            {
                Text = "Sẵn sàng thực thi.",
                Location = new Point(785, 53),
                AutoSize = true,
                Font = ModernTheme.RegularFont,
                ForeColor = ModernTheme.AccentYellow
            };

            // Row 3: Quick Administrative Tools
            Label lblQuick = new Label
            {
                Text = "Tiện ích nhanh:",
                Font = ModernTheme.RegularFont,
                ForeColor = ModernTheme.AccentCyan,
                Location = new Point(15, 96),
                AutoSize = true
            };

            Button btnBrowserClean = new Button
            {
                Text = "🧹 Xóa Cache Trình Duyệt...",
                Location = new Point(135, 90),
                Size = new Size(220, 32),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            ModernTheme.ApplyToButton(btnBrowserClean, ModernTheme.CardDark, ModernTheme.AccentYellow);
            btnBrowserClean.Click += (s, e) =>
            {
                string targetSummary = _cboTarget.SelectedItem != null ? _cboTarget.SelectedItem.ToString() : "Tất cả máy trạm";
                using (BrowserCleanOptionsForm form = new BrowserCleanOptionsForm(
                    (script) => ExecuteCustomScript(script, true),
                    (script) => { _txtScript.Text = script; _rbPowerShell.Checked = true; _lblStatus.Text = "Đã chèn script xóa cache trình duyệt."; },
                    targetSummary))
                {
                    form.ShowDialog(this);
                }
            };

            Button btnRestartSpooler = new Button
            {
                Text = "🖨️ Khởi Động Print Spooler",
                Location = new Point(365, 90),
                Size = new Size(205, 32),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            ModernTheme.ApplyToButton(btnRestartSpooler, ModernTheme.CardDark, ModernTheme.AccentGreen);
            btnRestartSpooler.Click += (s, e) =>
            {
                string targetSummary = _cboTarget.SelectedItem != null ? _cboTarget.SelectedItem.ToString() : "Tất cả máy trạm";
                DialogResult dr = MessageBox.Show(
                    string.Format("Bạn có muốn khởi động lại dịch vụ Print Spooler và dọn sạch hàng đợi in kẹt trên mục tiêu:\r\n\r\n{0}\r\n\r\n• Bấm [Yes / Có]: THỰC THI NGAY trên các máy trạm.\r\n• Bấm [No / Không]: CHÈN SCRIPT vào khung soạn thảo để xem trước.\r\n• Bấm [Cancel / Hủy]: Đóng hộp thoại.", targetSummary),
                    "Khởi Động Lại Print Spooler (Fix Kẹt Lệnh In)",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (dr == DialogResult.Yes)
                {
                    ExecuteCustomScript(GetPrintSpoolerRestartScript(), true);
                }
                else if (dr == DialogResult.No)
                {
                    _txtScript.Text = GetPrintSpoolerRestartScript();
                    _rbPowerShell.Checked = true;
                    _lblStatus.Text = "Đã nạp script khởi động lại Print Spooler.";
                }
            };

            Button btnHardwareAuditQuick = new Button
            {
                Text = "🔍 Kiểm Kê Thiết Bị",
                Location = new Point(580, 90),
                Size = new Size(160, 32),
                Font = ModernTheme.RegularFont
            };
            ModernTheme.ApplyToButton(btnHardwareAuditQuick, ModernTheme.CardDark, ModernTheme.TextPrimary);
            btnHardwareAuditQuick.Click += (s, e) => { _cboTemplates.SelectedIndex = 1; };

            Button btnCleanTempQuick = new Button
            {
                Text = "🗑️ Dọn Rác Temp",
                Location = new Point(750, 90),
                Size = new Size(135, 32),
                Font = ModernTheme.RegularFont
            };
            ModernTheme.ApplyToButton(btnCleanTempQuick, ModernTheme.CardDark, ModernTheme.TextSecondary);
            btnCleanTempQuick.Click += (s, e) => { _cboTemplates.SelectedIndex = 8; };

            ToolTip topTip = new ToolTip();
            topTip.SetToolTip(btnBrowserClean, "Dò tìm tất cả trình duyệt (Chrome, Edge, Cốc Cốc, Firefox, Brave, Opera, Vivaldi...) và dọn dẹp cache, lịch sử, cookies. Tuyệt đối KHÔNG xóa mật khẩu đã lưu.");
            topTip.SetToolTip(btnRestartSpooler, "Khởi động lại dịch vụ Print Spooler, cưỡng bức dừng spoolsv nếu bị treo, và dọn sạch tệp in kẹt trong C:\\Windows\\System32\\spool\\PRINTERS.");
            topTip.SetToolTip(btnHardwareAuditQuick, "Nạp script kiểm kê chi tiết cấu hình phần cứng, CPU, RAM, ổ đĩa, màn hình và máy in.");
            topTip.SetToolTip(btnCleanTempQuick, "Xóa các tệp tạm rác trong thư mục %TEMP%.");

            topPanel.Controls.Add(lblTarget);
            topPanel.Controls.Add(_cboTarget);
            topPanel.Controls.Add(_rbPowerShell);
            topPanel.Controls.Add(_rbCmd);
            topPanel.Controls.Add(lblTemplate);
            topPanel.Controls.Add(_cboTemplates);
            topPanel.Controls.Add(_btnExecute);
            topPanel.Controls.Add(_lblStatus);
            topPanel.Controls.Add(lblQuick);
            topPanel.Controls.Add(btnBrowserClean);
            topPanel.Controls.Add(btnRestartSpooler);
            topPanel.Controls.Add(btnHardwareAuditQuick);
            topPanel.Controls.Add(btnCleanTempQuick);

            // Split Container for Code and Output
            SplitContainer split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                BackColor = ModernTheme.BorderDark
            };

            // Top Editor Panel
            Panel pnlEditor = new Panel { Dock = DockStyle.Fill, BackColor = ModernTheme.CardDark, Padding = new Padding(8) };
            Label lblScriptTitle = new Label { Text = "Nội dung Script thực thi:", Dock = DockStyle.Top, Height = 22, ForeColor = ModernTheme.TextSecondary, Font = ModernTheme.SubHeaderFont };
            _txtScript = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                BackColor = Color.FromArgb(16, 20, 28),
                ForeColor = Color.FromArgb(220, 230, 245),
                Font = ModernTheme.CodeFont,
                BorderStyle = BorderStyle.FixedSingle,
                Text = GetHardwareAuditScript()
            };
            pnlEditor.Controls.Add(_txtScript);
            pnlEditor.Controls.Add(lblScriptTitle);

            // Bottom Output Panel with Export Toolbar
            Panel pnlOutput = new Panel { Dock = DockStyle.Fill, BackColor = ModernTheme.CardDark, Padding = new Padding(8) };
            
            Panel pnlOutputHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 36,
                BackColor = ModernTheme.CardDark,
                Padding = new Padding(0, 2, 0, 6)
            };

            Label lblOutTitle = new Label
            {
                Text = "Kết quả trả về từ các máy trạm (Console Output):",
                Dock = DockStyle.Left,
                AutoSize = true,
                ForeColor = ModernTheme.TextSecondary,
                Font = ModernTheme.SubHeaderFont,
                Padding = new Padding(0, 6, 0, 0)
            };

            FlowLayoutPanel pnlButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

            Button btnExportExcel = new Button
            {
                Text = "📊 Xuất File Excel",
                Width = 135,
                Height = 28,
                Margin = new Padding(2, 0, 3, 0)
            };
            ModernTheme.ApplyToButton(btnExportExcel, ModernTheme.CardDark, ModernTheme.AccentGreen);
            btnExportExcel.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnExportExcel.Click += OnExportExcelClicked;

            Button btnExportMd = new Button
            {
                Text = "📝 Xuất File .md",
                Width = 120,
                Height = 28,
                Margin = new Padding(2, 0, 3, 0)
            };
            ModernTheme.ApplyToButton(btnExportMd, ModernTheme.CardDark, ModernTheme.AccentCyan);
            btnExportMd.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            btnExportMd.Click += OnExportMdClicked;

            Button btnExportTxt = new Button
            {
                Text = "📄 Xuất TXT",
                Width = 100,
                Height = 28,
                Margin = new Padding(2, 0, 3, 0)
            };
            ModernTheme.ApplyToButton(btnExportTxt, ModernTheme.CardDark, ModernTheme.TextPrimary);
            btnExportTxt.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            btnExportTxt.Click += OnExportTxtClicked;

            Button btnClear = new Button
            {
                Text = "🧹 Xóa",
                Width = 65,
                Height = 28,
                Margin = new Padding(2, 0, 0, 0)
            };
            ModernTheme.ApplyToButton(btnClear, ModernTheme.CardDark, ModernTheme.TextSecondary);
            btnClear.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            btnClear.Click += (s, e) => { _txtOutput.Clear(); _lblStatus.Text = "Đã xóa kết quả."; };

            ToolTip tt = new ToolTip();
            tt.SetToolTip(btnExportExcel, "Xuất kết quả sang tệp bảng tính Excel (.csv / .xls) với đầy đủ cột phần cứng, màn hình, máy in");
            tt.SetToolTip(btnExportMd, "Xuất báo cáo kết quả sang tệp Markdown (.md) chuẩn có bảng biểu chi tiết");
            tt.SetToolTip(btnExportTxt, "Xuất nội dung kết quả ra tệp văn bản thô (.txt)");
            tt.SetToolTip(btnClear, "Xóa toàn bộ nội dung trong khung kết quả");

            pnlButtons.Controls.Add(btnExportExcel);
            pnlButtons.Controls.Add(btnExportMd);
            pnlButtons.Controls.Add(btnExportTxt);
            pnlButtons.Controls.Add(btnClear);

            pnlOutputHeader.Controls.Add(lblOutTitle);
            pnlOutputHeader.Controls.Add(pnlButtons);

            _txtOutput = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                BackColor = Color.FromArgb(12, 16, 22),
                ForeColor = Color.FromArgb(80, 220, 140),
                Font = ModernTheme.CodeFont,
                BorderStyle = BorderStyle.FixedSingle
            };

            pnlOutput.Controls.Add(_txtOutput);
            pnlOutput.Controls.Add(pnlOutputHeader);

            split.Panel1.Controls.Add(pnlEditor);
            split.Panel2.Controls.Add(pnlOutput);

            this.Controls.Add(split);
            this.Controls.Add(topPanel);

            this.Load += (s, e) =>
            {
                try
                {
                    if (split.Height > 250)
                        split.SplitterDistance = Math.Min(220, split.Height - 120);
                }
                catch { }
                _client.CommandResponseReceived += OnCommandResponse;
            };

            this.FormClosing += (s, e) =>
            {
                _client.CommandResponseReceived -= OnCommandResponse;
            };
        }

        private void OnTemplateChanged(object sender, EventArgs e)
        {
            switch (_cboTemplates.SelectedIndex)
            {
                case 1:
                    _rbPowerShell.Checked = true;
                    _txtScript.Text = GetHardwareAuditScript();
                    _lblStatus.Text = "Đã nạp script kiểm kê cấu hình phần cứng.";
                    break;
                case 2:
                    _rbPowerShell.Checked = true;
                    _txtScript.Text = BrowserCleanOptionsForm.BuildBrowserCleanScript("all_time", true, true, true, true, true);
                    _lblStatus.Text = "Đã nạp script dọn cache toàn bộ trình duyệt (Bảo vệ mật khẩu 100%).";
                    break;
                case 3:
                    _rbPowerShell.Checked = true;
                    _txtScript.Text = GetPrintSpoolerRestartScript();
                    _lblStatus.Text = "Đã nạp script khởi động lại Print Spooler và dọn hàng đợi in kẹt.";
                    break;
                case 4:
                    _rbCmd.Checked = true;
                    _txtScript.Text = "ipconfig /all";
                    _lblStatus.Text = "Đã nạp lệnh kiểm tra mạng ipconfig.";
                    break;
                case 5:
                    _rbPowerShell.Checked = true;
                    _txtScript.Text = "Get-Process | Sort-Object CPU -Descending | Select-Object -First 10 | Format-Table -AutoSize";
                    _lblStatus.Text = "Đã nạp lệnh kiểm tra Top CPU.";
                    break;
                case 6:
                    _rbPowerShell.Checked = true;
                    _txtScript.Text = "Get-Service | Where-Object {$_.Status -eq 'Running'} | Select-Object -First 15 | Format-Table -AutoSize";
                    _lblStatus.Text = "Đã nạp lệnh kiểm tra dịch vụ.";
                    break;
                case 7:
                    _rbPowerShell.Checked = true;
                    _txtScript.Text = "Get-PSDrive -PSProvider FileSystem | Select-Object Name, Used, Free, @{Name='TotalGB';Expression={ [math]::round(($_.Used + $_.Free)/1GB, 2) }} | Format-Table -AutoSize";
                    _lblStatus.Text = "Đã nạp lệnh kiểm tra dung lượng ổ đĩa.";
                    break;
                case 8:
                    _rbCmd.Checked = true;
                    _txtScript.Text = "del /q /f /s %TEMP%\\*";
                    _lblStatus.Text = "Đã nạp lệnh xóa file rác thư mục Temp.";
                    break;
            }
        }

        private void ExecuteCustomScript(string script, bool isPowerShell = true)
        {
            _txtScript.Text = script;
            if (isPowerShell) _rbPowerShell.Checked = true;
            else _rbCmd.Checked = true;
            OnExecuteClicked(_btnExecute, EventArgs.Empty);
        }

        private void OnExecuteClicked(object sender, EventArgs e)
        {
            string script = _txtScript.Text.Trim();
            if (string.IsNullOrEmpty(script))
            {
                MessageBox.Show("Vui lòng nhập lệnh hoặc script cần chạy!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            TargetItem selTarget = _cboTarget.SelectedItem as TargetItem;
            if (selTarget == null)
            {
                MessageBox.Show("Vui lòng chọn mục tiêu thực thi!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_client == null || !_client.IsConnected)
            {
                _txtOutput.Clear();
                _txtOutput.AppendText(string.Format("[{0:HH:mm:ss}] ❌ LỖI: Chưa kết nối tới máy chủ Gateway! Vui lòng kiểm tra LanControl.Gateway và kết nối mạng trước khi thực thi lệnh.\r\n", DateTime.Now));
                _lblStatus.Text = "Lỗi: Chưa kết nối tới Gateway.";
                MessageBox.Show("Chưa kết nối tới máy chủ Central Gateway!\nVui lòng kiểm tra kết nối Gateway trước khi gửi lệnh từ xa.", "Chưa kết nối Gateway", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            CommandType cmdType = _rbPowerShell.Checked ? CommandType.ExecutePs : CommandType.ExecuteCmd;
            _txtOutput.Clear();
            _txtOutput.AppendText(string.Format("[{0:HH:mm:ss}] Đang gửi lệnh thực thi...\r\n", DateTime.Now));
            _lblStatus.Text = "Đang gửi và chờ phản hồi...";

            if (selTarget.Scope == TargetScope.All)
            {
                _client.SendCommand("", cmdType, script, "All");
            }
            else if (selTarget.Scope == TargetScope.Vlan)
            {
                _client.SendCommand("", cmdType, script, selTarget.Vlan);
            }
            else if (selTarget.Scope == TargetScope.SelectedGroup)
            {
                foreach (AgentInfo a in _targetAgents)
                {
                    _client.SendCommand(a.AgentId, cmdType, script);
                }
            }
            else if (selTarget.Scope == TargetScope.SingleMachine && selTarget.Agent != null)
            {
                _client.SendCommand(selTarget.Agent.AgentId, cmdType, script);
            }
        }

        private void OnCommandResponse(CommandResponse resp)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action<CommandResponse>(OnCommandResponse), resp);
                return;
            }

            string rawOut = resp.Success ? resp.Output : "LỖI: " + resp.ErrorMessage;
            string cleanOut = FixMojibake(rawOut);

            string separator = new string('=', 70);
            string block = string.Format("\r\n{0}\r\n[{1:HH:mm:ss}] KẾT QUẢ TỪ AGENT: {2} | Thành công: {3}\r\n{0}\r\n{4}\r\n",
                separator, DateTime.Now, resp.AgentId, resp.Success, cleanOut);

            _txtOutput.AppendText(block);
            _lblStatus.Text = "Đã nhận kết quả.";
        }

        public static string FixMojibake(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            if (input.Contains("Ã") || input.Contains("áº") || input.Contains("á»") || 
                input.Contains("Cáº") || input.Contains("Ä") || input.Contains("á»‹") || 
                input.Contains("áº£") || input.Contains("á»ƒ") || input.Contains("áº¡") ||
                input.Contains("á»™") || input.Contains("á»‘") || input.Contains("Æ°") ||
                input.Contains("á»—") || input.Contains("á»“") || input.Contains("á» "))
            {
                try
                {
                    byte[] bytes = System.Text.Encoding.GetEncoding(1252).GetBytes(input);
                    string candidate = System.Text.Encoding.UTF8.GetString(bytes);
                    return candidate;
                }
                catch { }
            }
            return input;
        }

        #region Export Functions

        private string GetExportFileName(string extension, List<MachineAuditData> parsed = null)
        {
            string machineName = "";

            if (parsed != null && parsed.Count == 1 && !string.IsNullOrEmpty(parsed[0].ComputerName))
            {
                machineName = parsed[0].ComputerName;
            }
            else if (parsed != null && parsed.Count > 1)
            {
                machineName = string.Format("{0}May", parsed.Count);
            }
            else
            {
                TargetItem sel = _cboTarget.SelectedItem as TargetItem;
                if (sel != null)
                {
                    if (sel.Scope == TargetScope.SingleMachine && sel.Agent != null)
                    {
                        machineName = !string.IsNullOrEmpty(sel.Agent.Hostname) ? sel.Agent.Hostname : sel.Agent.AgentId;
                    }
                    else if (sel.Scope == TargetScope.SelectedGroup && _targetAgents != null && _targetAgents.Count > 0)
                    {
                        machineName = _targetAgents.Count == 1 ? _targetAgents[0].Hostname : string.Format("{0}May", _targetAgents.Count);
                    }
                    else if (sel.Scope == TargetScope.Vlan)
                    {
                        machineName = "VLAN_" + sel.Vlan;
                    }
                    else
                    {
                        machineName = "All";
                    }
                }
            }

            if (string.IsNullOrEmpty(machineName)) machineName = "All";

            string safeMachine = Regex.Replace(machineName, @"[\\/:*?""<>|\s]+", "_").Trim('_');
            if (string.IsNullOrEmpty(safeMachine)) safeMachine = "All";

            string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return string.Format("Thong_tin_phan_cung_{0}_{1}.{2}", safeMachine, timeStamp, extension.TrimStart('.'));
        }

        private void OnExportTxtClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_txtOutput.Text.Trim()))
            {
                MessageBox.Show("Chưa có kết quả thực thi nào để xuất!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string fixedText = FixMojibake(_txtOutput.Text);
            var parsed = ParseAuditOutput(fixedText);

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Title = "Lưu kết quả thực thi dạng văn bản (.txt)";
                sfd.Filter = "Tệp Văn Bản (*.txt)|*.txt|Tất cả tệp (*.*)|*.*";
                sfd.FileName = GetExportFileName(".txt", parsed);
                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        File.WriteAllText(sfd.FileName, fixedText, new UTF8Encoding(true));
                        MessageBox.Show("Đã xuất kết quả thành công ra tệp:\n" + sfd.FileName, "Xuất Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Lỗi khi lưu tệp: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void OnExportMdClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_txtOutput.Text.Trim()))
            {
                MessageBox.Show("Chưa có kết quả thực thi nào để xuất!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string fixedText = FixMojibake(_txtOutput.Text);
            var parsed = ParseAuditOutput(fixedText);

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Title = "Lưu báo cáo kiểm kê & kết quả dạng Markdown (.md)";
                sfd.Filter = "Tệp Markdown (*.md)|*.md|Tất cả tệp (*.*)|*.*";
                sfd.FileName = GetExportFileName(".md", parsed);
                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        string mdContent = GenerateMarkdownReport(parsed, fixedText);
                        File.WriteAllText(sfd.FileName, mdContent, new UTF8Encoding(true));
                        MessageBox.Show("Đã xuất báo cáo Markdown thành công ra tệp:\n" + sfd.FileName, "Xuất Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Lỗi khi lưu tệp: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void OnExportExcelClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_txtOutput.Text.Trim()))
            {
                MessageBox.Show("Chưa có kết quả thực thi nào để xuất!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string fixedText = FixMojibake(_txtOutput.Text);
            var parsed = ParseAuditOutput(fixedText);

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Title = "Lưu kết quả kiểm kê ra tệp Excel (.csv / .xls)";
                sfd.Filter = "Bảng tính Excel CSV (*.csv)|*.csv|Bảng tính Excel HTML (*.xls)|*.xls|Tất cả tệp (*.*)|*.*";
                sfd.FileName = GetExportFileName(".csv", parsed);
                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        string content;
                        if (sfd.FileName.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
                        {
                            content = GenerateExcelHtmlReport(parsed, fixedText);
                        }
                        else
                        {
                            content = GenerateCsvReport(parsed, fixedText);
                        }
                        File.WriteAllText(sfd.FileName, content, new UTF8Encoding(true));
                        MessageBox.Show("Đã xuất dữ liệu Excel thành công ra tệp:\n" + sfd.FileName, "Xuất Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Lỗi khi lưu tệp: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        #endregion

        #region Hardware Audit Script & Parser

        private string GetHardwareAuditScript()
        {
            return @"# LAN CONTROL - TRUY XUẤT NHANH PHẦN CỨNG, MÀN HÌNH, MÁY IN, MÁY QUÉT USB
$ErrorActionPreference = 'SilentlyContinue'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8
try { [Console]::InputEncoding = [System.Text.Encoding]::UTF8 } catch {}
try { chcp 65001 >$null } catch {}

Write-Output '================================================================================'
Write-Output '          BÁO CÁO THÔNG TIN CẤU HÌNH PHẦN CỨNG & THIẾT BỊ NGOẠI VI             '
Write-Output '================================================================================'
Write-Output ""Thời gian kiểm tra: $(Get-Date -Format 'dd/MM/yyyy HH:mm:ss')""

# 1. THÔNG TIN PHẦN CỨNG TỔNG QUAN
$cs = Get-WmiObject Win32_ComputerSystem
$os = Get-WmiObject Win32_OperatingSystem
$bb = Get-WmiObject Win32_BaseBoard
$bios = Get-WmiObject Win32_BIOS
$cpu = Get-WmiObject Win32_Processor | Select-Object -First 1
$ramSticks = @(Get-WmiObject Win32_PhysicalMemory)
$gpus = @(Get-WmiObject Win32_VideoController)
$disks = @(Get-WmiObject Win32_DiskDrive)
$net = Get-WmiObject Win32_NetworkAdapterConfiguration | Where-Object { $_.IPEnabled -eq $true } | Select-Object -First 1

$totalRamBytes = ($ramSticks | Measure-Object -Property Capacity -Sum).Sum
$totalRamGB = [math]::Round($totalRamBytes / 1GB, 1)
$ramDetails = ($ramSticks | ForEach-Object { ""$([math]::Round($_.Capacity / 1GB, 0))GB $($_.Manufacturer) @ $($_.Speed)MHz (SN: $($_.SerialNumber.Trim()))"" }) -join '; '
$diskList = ($disks | ForEach-Object { ""$($_.Model) ($([math]::Round($_.Size / 1GB, 1)) GB, SN: $($_.SerialNumber.Trim()))"" }) -join ' | '
$gpuList = ($gpus | ForEach-Object { ""$($_.Name) ($($_.CurrentHorizontalResolution)x$($_.CurrentVerticalResolution))"" }) -join ' | '
$sysSerial = if ($bios.SerialNumber -and $bios.SerialNumber -notmatch 'To be filled|Default|None') { $bios.SerialNumber.Trim() } else { $bb.SerialNumber.Trim() }

Write-Output ''
Write-Output '--------------------------------------------------------------------------------'
Write-Output '1. THÔNG TIN PHẦN CỨNG TỔNG QUAN'
Write-Output '--------------------------------------------------------------------------------'
Write-Output ""Tên máy tính:       $($env:COMPUTERNAME)""
Write-Output ""Mainboard:          $($bb.Manufacturer) $($bb.Product) (SN: $($bb.SerialNumber.Trim()))""
Write-Output ""Bộ vi xử lý (CPU):  $($cpu.Name) ($($cpu.NumberOfCores) Cores, $($cpu.NumberOfLogicalProcessors) Threads)""
Write-Output ""Bộ nhớ RAM:         $totalRamGB GB ($ramDetails)""
Write-Output ""Card đồ họa (GPU):  $gpuList""
Write-Output ""Ổ đĩa lưu trữ:      $diskList""
Write-Output ""Hệ điều hành:       $($os.Caption) ($($os.OSArchitecture), Build $($os.BuildNumber))""
Write-Output ""Số Serial:          $sysSerial""
Write-Output ""Địa chỉ MAC:        $($net.MACAddress) (IP: $($net.IPAddress[0]))""

# 2. DANH SÁCH MÀN HÌNH PHÁT HIỆN
Write-Output ''
Write-Output '--------------------------------------------------------------------------------'
Write-Output '2. DANH SÁCH MÀN HÌNH PHÁT HIỆN'
Write-Output '--------------------------------------------------------------------------------'
$monitors = @(Get-WmiObject -Namespace root\wmi -Class WmiMonitorID)
if ($monitors.Count -gt 0) {
    Write-Output ""Số lượng màn hình: $($monitors.Count)""
    $mIndex = 1
    foreach ($m in $monitors) {
        $mfgCodes = $m.ManufacturerName | Where-Object { $_ -ne 0 }
        $mfg = if ($mfgCodes) { [System.Text.Encoding]::ASCII.GetString([byte[]]$mfgCodes).Trim() } else { 'Unknown' }
        if ($mfg -eq 'DEL') { $mfg = 'Dell' }
        elseif ($mfg -eq 'SAM') { $mfg = 'Samsung' }
        elseif ($mfg -eq 'LGD') { $mfg = 'LG' }
        elseif ($mfg -eq 'ACR') { $mfg = 'Acer' }
        elseif ($mfg -eq 'AOC') { $mfg = 'AOC' }
        elseif ($mfg -eq 'HWP') { $mfg = 'HP' }
        elseif ($mfg -eq 'BNQ') { $mfg = 'BenQ' }
        elseif ($mfg -eq 'ASU') { $mfg = 'Asus' }
        
        $nameCodes = $m.UserFriendlyName | Where-Object { $_ -ne 0 }
        $model = if ($nameCodes) { [System.Text.Encoding]::ASCII.GetString([byte[]]$nameCodes).Trim() } else { 'Generic Monitor' }
        
        $serCodes = $m.SerialNumberID | Where-Object { $_ -ne 0 }
        $serial = if ($serCodes) { [System.Text.Encoding]::ASCII.GetString([byte[]]$serCodes).Trim() } else { 'N/A' }
        
        Write-Output ""[$mIndex] $model""
        Write-Output ""    Hãng SX: $mfg | Số Serial: $serial""
        $mIndex++
    }
} else {
    Write-Output 'Không phát hiện màn hình qua WMI/EDID.'
}

# 3. DANH SÁCH MÁY IN PHÁT HIỆN
Write-Output ''
Write-Output '--------------------------------------------------------------------------------'
Write-Output '3. DANH SÁCH MÁY IN PHÁT HIỆN'
Write-Output '--------------------------------------------------------------------------------'
$printers = @(Get-WmiObject Win32_Printer)
$usbPrintReg = @()
$usbRegPath = 'HKLM:\SYSTEM\CurrentControlSet\Enum\USBPRINT'
if (Test-Path $usbRegPath) {
    Get-ChildItem $usbRegPath -ErrorAction SilentlyContinue | ForEach-Object {
        $mKey = $_
        Get-ChildItem $mKey.PSPath -ErrorAction SilentlyContinue | ForEach-Object {
            $iKey = $_
            $p = Get-ItemProperty $iKey.PSPath -ErrorAction SilentlyContinue
            $usbPrintReg += [PSCustomObject]@{
                Model = $mKey.PSChildName
                InstanceId = $iKey.PSChildName
                ContainerID = $p.ContainerID
                DeviceDesc = $p.DeviceDesc
            }
        }
    }
}

$usbDevices = @()
$usbRootPath = 'HKLM:\SYSTEM\CurrentControlSet\Enum\USB'
if (Test-Path $usbRootPath) {
    Get-ChildItem $usbRootPath -Recurse -ErrorAction SilentlyContinue | ForEach-Object {
        $uPath = $_.PSPath
        $p = Get-ItemProperty $uPath -ErrorAction SilentlyContinue
        if ($p.Service -eq 'usbprint' -or $p.Class -eq 'USB' -or $p.ContainerID) {
            $usbDevices += [PSCustomObject]@{
                Child = $_.PSChildName
                ParentIdPrefix = $p.ParentIdPrefix
                ContainerID = $p.ContainerID
                DeviceDesc = $p.DeviceDesc
                Service = $p.Service
            }
        }
    }
}

Write-Output ""Số lượng máy in: $($printers.Count)""
$pIndex = 1
foreach ($pr in $printers) {
    $port = $pr.PortName
    $usbSerial = 'N/A'
    
    if ($port -like 'USB*') {
        $matchedPrint = $usbPrintReg | Where-Object { 
            ($port -and $_.InstanceId -like ""*$port*"") -or 
            ($_.DeviceDesc -and $pr.Name -like ""*$($_.DeviceDesc)*"") -or
            ($_.Model -and ($pr.Name -replace '[^a-zA-Z0-9]','') -like ""*$(($_.Model -replace '[^a-zA-Z0-9]',''))*"")
        }
        
        if ($matchedPrint) {
            $cid = $matchedPrint[0].ContainerID
            $inst = $matchedPrint[0].InstanceId
            
            if ($cid) {
                $usbMatch = $usbDevices | Where-Object { $_.ContainerID -eq $cid -and ($_.Service -eq 'usbprint' -or $_.DeviceDesc -like '*Printing*') }
                if (-not $usbMatch) {
                    $usbMatch = $usbDevices | Where-Object { $_.ContainerID -eq $cid -and $_.Child -notlike '*&*&*' }
                }
                if ($usbMatch) {
                    $usbSerial = $usbMatch[0].Child
                }
            }
            
            if ($usbSerial -eq 'N/A' -and $inst) {
                $usbSerial = $inst
            }
        }
        
        if ($usbSerial -eq 'N/A') {
            $pnpMatches = @(Get-WmiObject Win32_PnPEntity | Where-Object { $_.Service -eq 'usbprint' -or $_.DeviceID -like 'USBPRINT*' })
            if ($pnpMatches.Count -gt 0) {
                foreach ($pm in $pnpMatches) {
                    $parts = $pm.DeviceID -split '\\'
                    if ($parts.Count -ge 3) {
                        $usbSerial = $parts[2]
                        break
                    }
                }
            }
        }
    }
    
    $mfg = if ($pr.DriverName) { $pr.DriverName } else { 'Generic' }
    if ($usbSerial -ne 'N/A') {
        Write-Output ""[$pIndex] $($pr.Name)""
        Write-Output ""    Hãng SX: $mfg | Cổng: $port | Số Serial USB: $usbSerial""
    } else {
        Write-Output ""[$pIndex] $($pr.Name)""
        Write-Output ""    Hãng SX: $mfg | Cổng: $port""
    }
    $pIndex++
}

# 4. DANH SÁCH MÁY QUÉT (SCANNER) & THIẾT BỊ NGOẠI VI USB
Write-Output ''
Write-Output '--------------------------------------------------------------------------------'
Write-Output '4. DANH SÁCH MÁY QUÉT (SCANNER) & THIẾT BỊ NGOẠI VI USB'
Write-Output '--------------------------------------------------------------------------------'
$scanners = @(Get-WmiObject Win32_PnPEntity | Where-Object { 
    $_.PNPClass -eq 'Image' -or 
    $_.Service -eq 'usbscan' -or 
    $_.Name -like '*Scanner*' -or 
    $_.Name -like '*ScanSnap*' -or
    $_.Name -like '*Fujitsu fi*' -or
    $_.Name -like '*Canon DR*'
})

if ($scanners.Count -gt 0) {
    Write-Output ""Phát hiện $($scanners.Count) thiết bị Máy quét (Scanner / Image):""
    $sIdx = 1
    foreach ($sc in $scanners) {
        $ser = 'N/A'
        $parts = $sc.DeviceID -split '\\'
        if ($parts.Count -ge 3 -and $parts[2] -notlike '*&*') { $ser = $parts[2] }
        Write-Output ""[$sIdx] $($sc.Name)""
        Write-Output ""    Hãng SX: $($sc.Manufacturer) | Cổng/DeviceID: $($sc.DeviceID) | Số Serial: $ser""
        $sIdx++
    }
} else {
    Write-Output 'Không phát hiện máy quét chuyên dụng (Image/Scanner) đang kết nối.'
}

$usbDevs = @(Get-WmiObject Win32_PnPEntity | Where-Object {
    ($_.DeviceID -like 'USB\*' -or $_.DeviceID -like 'USBSTOR\*') -and
    $_.Name -notlike '*Root Hub*' -and
    $_.Name -notlike '*Host Controller*' -and
    $_.Name -notlike '*Generic USB Hub*'
})
Write-Output ''
Write-Output ""Các thiết bị ngoại vi USB khác đang cắm ($($usbDevs.Count) thiết bị):""
$uIdx = 1
foreach ($u in $usbDevs) {
    $ser = 'N/A'
    $parts = $u.DeviceID -split '\\'
    if ($parts.Count -ge 3 -and $parts[2] -notlike '*&*') { $ser = $parts[2] }
    Write-Output ""[$uIdx] $($u.Name) | Hãng: $($u.Manufacturer) | Serial: $ser | ID: $($u.DeviceID)""
    $uIdx++
}

Write-Output '================================================================================'
Write-Output '                       HOÀN TẤT THU THẬP DỮ LIỆU CẤU HÌNH                       '
Write-Output '================================================================================'
".Replace("\r\n", "\n").Replace("\n", "\r\n");
        }

        private class MachineAuditData
        {
            public string AgentId = "";
            public string ComputerName = "";
            public string Mainboard = "";
            public string Cpu = "";
            public string Ram = "";
            public string Gpu = "";
            public string Disks = "";
            public string Os = "";
            public string SerialNumber = "";
            public string Mac = "";
            public List<string> Monitors = new List<string>();
            public List<string> Printers = new List<string>();
            public List<string> Scanners = new List<string>();
            public List<string> UsbDevices = new List<string>();
            public string RawOutput = "";
        }

        private List<MachineAuditData> ParseAuditOutput(string consoleOutput)
        {
            List<MachineAuditData> list = new List<MachineAuditData>();
            if (string.IsNullOrEmpty(consoleOutput)) return list;
            consoleOutput = FixMojibake(consoleOutput);

            // Split by agent header if multiple: "KẾT QUẢ TỪ AGENT: {id}"
            string[] blocks = Regex.Split(consoleOutput, @"={30,}\r?\n\[\d{2}:\d{2}:\d{2}\]\s*K[^\r\n|]*:\s*([^\r\n|]*)\s*\|\s*Th[^\r\n|]*:\s*(\w+)\r?\n={30,}\r?\n");

            if (blocks.Length > 1)
            {
                for (int i = 1; i < blocks.Length; i += 3)
                {
                    string agentId = blocks[i].Trim();
                    string body = (i + 2 < blocks.Length) ? blocks[i + 2] : "";

                    MachineAuditData m = ParseSingleBody(body);
                    m.AgentId = string.IsNullOrEmpty(agentId) ? m.ComputerName : agentId;
                    list.Add(m);
                }
            }
            else
            {
                MachineAuditData m = ParseSingleBody(consoleOutput);
                list.Add(m);
            }

            return list;
        }

        private MachineAuditData ParseSingleBody(string body)
        {
            MachineAuditData m = new MachineAuditData();
            body = FixMojibake(body);
            m.RawOutput = body;

            string[] lines = body.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            string currentSection = "";

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                if (line.Contains("1. THÔNG TIN PHẦN CỨNG TỔNG QUAN")) { currentSection = "HW"; continue; }
                if (line.Contains("2. DANH SÁCH MÀN HÌNH PHÁT HIỆN")) { currentSection = "MONITOR"; continue; }
                if (line.Contains("3. DANH SÁCH MÁY IN PHÁT HIỆN")) { currentSection = "PRINTER"; continue; }
                if (line.Contains("4. DANH SÁCH MÁY QUÉT") || line.Contains("SCANNER")) { currentSection = "SCANNER"; continue; }
                if (line.Contains("Các thiết bị ngoại vi USB khác")) { currentSection = "USB"; continue; }

                if (currentSection == "HW")
                {
                    if (line.StartsWith("Tên máy tính:")) m.ComputerName = line.Substring(13).Trim();
                    else if (line.StartsWith("Mainboard:")) m.Mainboard = line.Substring(10).Trim();
                    else if (line.StartsWith("Bộ vi xử lý (CPU):")) m.Cpu = line.Substring(18).Trim();
                    else if (line.StartsWith("Bộ nhớ RAM:")) m.Ram = line.Substring(11).Trim();
                    else if (line.StartsWith("Card đồ họa (GPU):")) m.Gpu = line.Substring(18).Trim();
                    else if (line.StartsWith("Ổ đĩa lưu trữ:")) m.Disks = line.Substring(14).Trim();
                    else if (line.StartsWith("Hệ điều hành:")) m.Os = line.Substring(13).Trim();
                    else if (line.StartsWith("Số Serial:")) m.SerialNumber = line.Substring(10).Trim();
                    else if (line.StartsWith("Địa chỉ MAC:")) m.Mac = line.Substring(12).Trim();
                }
                else if (currentSection == "MONITOR")
                {
                    if (line.StartsWith("[") && line.Contains("]"))
                    {
                        string details = (i + 1 < lines.Length && (lines[i + 1].Trim().StartsWith("Hãng SX:") || lines[i + 1].Trim().StartsWith("Hãng:")))
                            ? " | " + lines[i + 1].Trim()
                            : "";
                        if (!string.IsNullOrEmpty(details)) i++;
                        m.Monitors.Add(line + details);
                    }
                }
                else if (currentSection == "PRINTER")
                {
                    if (line.StartsWith("[") && line.Contains("]"))
                    {
                        string details = (i + 1 < lines.Length && (lines[i + 1].Trim().StartsWith("Hãng SX:") || lines[i + 1].Trim().StartsWith("Hãng:")))
                            ? " | " + lines[i + 1].Trim()
                            : "";
                        if (!string.IsNullOrEmpty(details)) i++;
                        m.Printers.Add(line + details);
                    }
                }
                else if (currentSection == "SCANNER")
                {
                    if (line.StartsWith("[") && line.Contains("]"))
                    {
                        string details = (i + 1 < lines.Length && (lines[i + 1].Trim().StartsWith("Hãng SX:") || lines[i + 1].Trim().StartsWith("Hãng:")))
                            ? " | " + lines[i + 1].Trim()
                            : "";
                        if (!string.IsNullOrEmpty(details)) i++;
                        m.Scanners.Add(line + details);
                    }
                }
                else if (currentSection == "USB")
                {
                    if (line.StartsWith("["))
                    {
                        m.UsbDevices.Add(line);
                    }
                }
            }

            if (string.IsNullOrEmpty(m.ComputerName) && !string.IsNullOrEmpty(m.AgentId))
            {
                m.ComputerName = m.AgentId;
            }

            return m;
        }

        private string GenerateMarkdownReport(List<MachineAuditData> list, string rawOutput)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# Báo Cáo Thông Tin Cấu Hình Phần Cứng & Thiết Bị Ngoại Vi Máy Trạm");
            sb.AppendLine();
            sb.AppendLine(string.Format("- **Thời gian xuất báo cáo**: {0:dd/MM/yyyy HH:mm:ss}", DateTime.Now));
            sb.AppendLine(string.Format("- **Tổng số máy trạm báo cáo**: {0}", list.Count));
            sb.AppendLine();

            bool isHardwareReport = list.Exists(x => !string.IsNullOrEmpty(x.Cpu) || x.Monitors.Count > 0);

            if (isHardwareReport)
            {
                sb.AppendLine("## 1. Bảng Tổng Hợp Máy Trạm");
                sb.AppendLine();
                sb.AppendLine("| STT | Tên Máy Tính | Hệ Điều Hành | CPU | RAM | Ổ Đĩa | Serial Máy | Màn Hình | Máy In |");
                sb.AppendLine("| :---: | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |");

                int idx = 1;
                foreach (var m in list)
                {
                    string monStr = m.Monitors.Count > 0 ? string.Join("<br>", m.Monitors) : "---";
                    string prStr = m.Printers.Count > 0 ? string.Join("<br>", m.Printers) : "---";
                    sb.AppendLine(string.Format("| {0} | **{1}** | {2} | {3} | {4} | {5} | `{6}` | {7} | {8} |",
                        idx++,
                        EscapeMd(m.ComputerName),
                        EscapeMd(m.Os),
                        EscapeMd(m.Cpu),
                        EscapeMd(m.Ram),
                        EscapeMd(m.Disks),
                        EscapeMd(m.SerialNumber),
                        EscapeMd(monStr),
                        EscapeMd(prStr)));
                }
                sb.AppendLine();

                sb.AppendLine("## 2. Chi Tiết Từng Máy Trạm");
                sb.AppendLine();

                foreach (var m in list)
                {
                    sb.AppendLine(string.Format("### 🖥️ Máy Trạm: {0}", string.IsNullOrEmpty(m.ComputerName) ? m.AgentId : m.ComputerName));
                    sb.AppendLine();
                    sb.AppendLine("| Thông Số Phần Cứng | Chi Tiết Cấu Hình |");
                    sb.AppendLine("| :--- | :--- |");
                    sb.AppendLine(string.Format("| **Tên máy tính** | {0} |", EscapeMd(m.ComputerName)));
                    sb.AppendLine(string.Format("| **Mainboard** | {0} |", EscapeMd(m.Mainboard)));
                    sb.AppendLine(string.Format("| **Bộ vi xử lý (CPU)** | {0} |", EscapeMd(m.Cpu)));
                    sb.AppendLine(string.Format("| **Bộ nhớ RAM** | {0} |", EscapeMd(m.Ram)));
                    sb.AppendLine(string.Format("| **Card đồ họa (GPU)** | {0} |", EscapeMd(m.Gpu)));
                    sb.AppendLine(string.Format("| **Ổ đĩa lưu trữ** | {0} |", EscapeMd(m.Disks)));
                    sb.AppendLine(string.Format("| **Hệ điều hành** | {0} |", EscapeMd(m.Os)));
                    sb.AppendLine(string.Format("| **Số Serial máy** | `{0}` |", EscapeMd(m.SerialNumber)));
                    sb.AppendLine(string.Format("| **Địa chỉ MAC / IP** | `{0}` |", EscapeMd(m.Mac)));
                    sb.AppendLine();

                    if (m.Monitors.Count > 0)
                    {
                        sb.AppendLine("#### 🖥️ Danh Sách Màn Hình Phát Hiện");
                        foreach (var mon in m.Monitors) sb.AppendLine("- " + mon);
                        sb.AppendLine();
                    }

                    if (m.Printers.Count > 0)
                    {
                        sb.AppendLine("#### 🖨️ Danh Sách Máy In Phát Hiện");
                        foreach (var pr in m.Printers) sb.AppendLine("- " + pr);
                        sb.AppendLine();
                    }

                    if (m.Scanners.Count > 0)
                    {
                        sb.AppendLine("#### 📷 Danh Sách Máy Quét (Scanner) Phát Hiện");
                        foreach (var sc in m.Scanners) sb.AppendLine("- " + sc);
                        sb.AppendLine();
                    }

                    if (m.UsbDevices.Count > 0)
                    {
                        sb.AppendLine("#### 🔌 Danh Sách Thiết Bị USB Ngoại Vi Khác");
                        foreach (var u in m.UsbDevices) sb.AppendLine("- " + u);
                        sb.AppendLine();
                    }
                    sb.AppendLine("---");
                    sb.AppendLine();
                }
            }
            else
            {
                sb.AppendLine("## Kết Quả Thực Thi Script");
                sb.AppendLine("```text");
                sb.AppendLine(rawOutput);
                sb.AppendLine("```");
            }

            return sb.ToString();
        }

        private string GenerateCsvReport(List<MachineAuditData> list, string rawOutput)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('\uFEFF'); // UTF-8 BOM for Microsoft Excel

            bool isHardwareReport = list.Exists(x => !string.IsNullOrEmpty(x.Cpu) || x.Monitors.Count > 0);
            if (isHardwareReport)
            {
                int count = 0;
                foreach (var m in list)
                {
                    count++;
                    if (list.Count > 1)
                    {
                        sb.AppendLine(string.Format("=== THÔNG TIN PHẦN CỨNG MÁY: {0} ({1}/{2}) ===", EscapeCsv(m.ComputerName), count, list.Count));
                    }
                    sb.AppendLine("Thông Số / Hạng Mục,Chi Tiết Cấu Hình / Thiết Bị");
                    sb.AppendLine(string.Format("Tên máy tính,{0}", EscapeCsv(m.ComputerName)));
                    sb.AppendLine(string.Format("Số Serial máy,{0}", EscapeCsv(m.SerialNumber)));
                    sb.AppendLine(string.Format("Bo mạch chủ (Mainboard),{0}", EscapeCsv(m.Mainboard)));
                    sb.AppendLine(string.Format("Bộ vi xử lý (CPU),{0}", EscapeCsv(m.Cpu)));
                    sb.AppendLine(string.Format("Bộ nhớ RAM,{0}", EscapeCsv(m.Ram)));
                    sb.AppendLine(string.Format("Card đồ họa (GPU),{0}", EscapeCsv(m.Gpu)));
                    sb.AppendLine(string.Format("Ổ đĩa lưu trữ,{0}", EscapeCsv(m.Disks)));
                    sb.AppendLine(string.Format("Hệ điều hành,{0}", EscapeCsv(m.Os)));
                    sb.AppendLine(string.Format("Địa chỉ MAC & IP,{0}", EscapeCsv(m.Mac)));

                    if (m.Monitors.Count > 0)
                    {
                        for (int i = 0; i < m.Monitors.Count; i++)
                        {
                            string label = m.Monitors.Count > 1 ? string.Format("Màn hình [{0}]", i + 1) : "Màn hình";
                            sb.AppendLine(string.Format("{0},{1}", label, EscapeCsv(m.Monitors[i])));
                        }
                    }
                    else
                    {
                        sb.AppendLine("Màn hình,Không phát hiện");
                    }

                    if (m.Printers.Count > 0)
                    {
                        for (int i = 0; i < m.Printers.Count; i++)
                        {
                            string label = m.Printers.Count > 1 ? string.Format("Máy in [{0}]", i + 1) : "Máy in";
                            sb.AppendLine(string.Format("{0},{1}", label, EscapeCsv(m.Printers[i])));
                        }
                    }
                    else
                    {
                        sb.AppendLine("Máy in,Không phát hiện");
                    }

                    if (m.Scanners.Count > 0)
                    {
                        for (int i = 0; i < m.Scanners.Count; i++)
                        {
                            string label = m.Scanners.Count > 1 ? string.Format("Máy quét (Scanner) [{0}]", i + 1) : "Máy quét (Scanner)";
                            sb.AppendLine(string.Format("{0},{1}", label, EscapeCsv(m.Scanners[i])));
                        }
                    }

                    if (m.UsbDevices.Count > 0)
                    {
                        for (int i = 0; i < m.UsbDevices.Count; i++)
                        {
                            string label = m.UsbDevices.Count > 1 ? string.Format("Thiết bị ngoại vi USB [{0}]", i + 1) : "Thiết bị ngoại vi USB";
                            sb.AppendLine(string.Format("{0},{1}", label, EscapeCsv(m.UsbDevices[i])));
                        }
                    }

                    sb.AppendLine(string.Format("Thời gian kiểm kê,{0}", EscapeCsv(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"))));

                    if (count < list.Count)
                    {
                        sb.AppendLine();
                        sb.AppendLine();
                    }
                }
            }
            else
            {
                sb.AppendLine("Dòng,Nội Dung Kết Quả");
                string[] lines = rawOutput.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                for (int i = 0; i < lines.Length; i++)
                {
                    sb.AppendLine(string.Format("{0},{1}", i + 1, EscapeCsv(lines[i])));
                }
            }

            return sb.ToString();
        }

        private string GenerateExcelHtmlReport(List<MachineAuditData> list, string rawOutput)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<html xmlns:o=\"urn:schemas-microsoft-com:office:office\" xmlns:x=\"urn:schemas-microsoft-com:office:excel\" xmlns=\"http://www.w3.org/TR/REC-html40\">");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\">");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: Segoe UI, Tahoma, sans-serif; font-size: 10pt; background-color: #ffffff; }");
            sb.AppendLine("h2 { color: #1a5276; margin-bottom: 6px; font-family: Segoe UI, sans-serif; }");
            sb.AppendLine("table { border-collapse: collapse; margin-bottom: 25px; width: 100%; max-width: 950px; }");
            sb.AppendLine("th { background-color: #2471a3; color: white; border: 1px solid #1a5276; padding: 7px 12px; font-size: 10pt; text-align: left; }");
            sb.AppendLine("td { border: 1px solid #bdc3c7; padding: 6px 12px; font-size: 9.5pt; vertical-align: top; }");
            sb.AppendLine("tr:nth-child(even) { background-color: #f8f9fa; }");
            sb.AppendLine(".banner { background-color: #1a5276; color: white; font-weight: bold; font-size: 11pt; padding: 8px 12px; }");
            sb.AppendLine(".prop-name { font-weight: bold; width: 260px; background-color: #eaf2f8; color: #21618c; white-space: nowrap; }");
            sb.AppendLine(".bold { font-weight: bold; }");
            sb.AppendLine(".mono { font-family: Consolas, monospace; }");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");

            bool isHw = list.Exists(x => !string.IsNullOrEmpty(x.Cpu) || x.Monitors.Count > 0);
            if (isHw)
            {
                sb.AppendLine("<h2>BÁO CÁO CHI TIẾT CẤU HÌNH & THIẾT BỊ NGOẠI VI MÁY TRẠM</h2>");
                sb.AppendLine(string.Format("<p><b>Thời gian xuất báo cáo:</b> {0:dd/MM/yyyy HH:mm:ss} | <b>Tổng số máy:</b> {1}</p>", DateTime.Now, list.Count));

                int count = 0;
                foreach (var m in list)
                {
                    count++;
                    sb.AppendLine("<table>");
                    sb.AppendLine("<thead>");
                    sb.AppendLine(string.Format("<tr><th colspan=\"2\" class=\"banner\">🖥️ THÔNG TIN PHẦN CỨNG MÁY: {0} ({1}/{2})</th></tr>", EscapeHtml(m.ComputerName), count, list.Count));
                    sb.AppendLine("<tr><th style=\"width: 260px;\">Thông Số / Hạng Mục</th><th>Chi Tiết Cấu Hình / Thiết Bị</th></tr>");
                    sb.AppendLine("</thead>");
                    sb.AppendLine("<tbody>");

                    sb.AppendFormat("<tr><td class=\"prop-name\">Tên máy tính</td><td class=\"bold\">{0}</td></tr>\r\n", EscapeHtml(m.ComputerName));
                    sb.AppendFormat("<tr><td class=\"prop-name\">Số Serial máy</td><td class=\"mono bold\">{0}</td></tr>\r\n", EscapeHtml(m.SerialNumber));
                    sb.AppendFormat("<tr><td class=\"prop-name\">Bo mạch chủ (Mainboard)</td><td>{0}</td></tr>\r\n", EscapeHtml(m.Mainboard));
                    sb.AppendFormat("<tr><td class=\"prop-name\">Bộ vi xử lý (CPU)</td><td>{0}</td></tr>\r\n", EscapeHtml(m.Cpu));
                    sb.AppendFormat("<tr><td class=\"prop-name\">Bộ nhớ RAM</td><td>{0}</td></tr>\r\n", EscapeHtml(m.Ram));
                    sb.AppendFormat("<tr><td class=\"prop-name\">Card đồ họa (GPU)</td><td>{0}</td></tr>\r\n", EscapeHtml(m.Gpu));
                    sb.AppendFormat("<tr><td class=\"prop-name\">Ổ đĩa lưu trữ</td><td>{0}</td></tr>\r\n", EscapeHtml(m.Disks));
                    sb.AppendFormat("<tr><td class=\"prop-name\">Hệ điều hành</td><td>{0}</td></tr>\r\n", EscapeHtml(m.Os));
                    sb.AppendFormat("<tr><td class=\"prop-name\">Địa chỉ MAC & IP</td><td class=\"mono\">{0}</td></tr>\r\n", EscapeHtml(m.Mac));

                    if (m.Monitors.Count > 0)
                    {
                        for (int i = 0; i < m.Monitors.Count; i++)
                        {
                            string label = m.Monitors.Count > 1 ? string.Format("Màn hình [{0}]", i + 1) : "Màn hình";
                            sb.AppendFormat("<tr><td class=\"prop-name\">{0}</td><td>{1}</td></tr>\r\n", label, EscapeHtml(m.Monitors[i]));
                        }
                    }
                    else
                    {
                        sb.AppendLine("<tr><td class=\"prop-name\">Màn hình</td><td>Không phát hiện</td></tr>");
                    }

                    if (m.Printers.Count > 0)
                    {
                        for (int i = 0; i < m.Printers.Count; i++)
                        {
                            string label = m.Printers.Count > 1 ? string.Format("Máy in [{0}]", i + 1) : "Máy in";
                            sb.AppendFormat("<tr><td class=\"prop-name\">{0}</td><td>{1}</td></tr>\r\n", label, EscapeHtml(m.Printers[i]));
                        }
                    }
                    else
                    {
                        sb.AppendLine("<tr><td class=\"prop-name\">Máy in</td><td>Không phát hiện</td></tr>");
                    }

                    if (m.Scanners.Count > 0)
                    {
                        for (int i = 0; i < m.Scanners.Count; i++)
                        {
                            string label = m.Scanners.Count > 1 ? string.Format("Máy quét (Scanner) [{0}]", i + 1) : "Máy quét (Scanner)";
                            sb.AppendFormat("<tr><td class=\"prop-name\">{0}</td><td>{1}</td></tr>\r\n", label, EscapeHtml(m.Scanners[i]));
                        }
                    }

                    if (m.UsbDevices.Count > 0)
                    {
                        for (int i = 0; i < m.UsbDevices.Count; i++)
                        {
                            string label = m.UsbDevices.Count > 1 ? string.Format("Thiết bị ngoại vi USB [{0}]", i + 1) : "Thiết bị ngoại vi USB";
                            sb.AppendFormat("<tr><td class=\"prop-name\">{0}</td><td>{1}</td></tr>\r\n", label, EscapeHtml(m.UsbDevices[i]));
                        }
                    }

                    sb.AppendFormat("<tr><td class=\"prop-name\">Thời gian kiểm kê</td><td>{0:dd/MM/yyyy HH:mm:ss}</td></tr>\r\n", DateTime.Now);

                    sb.AppendLine("</tbody>");
                    sb.AppendLine("</table>");
                }
            }
            else
            {
                sb.AppendLine("<h2>KẾT QUẢ THỰC THI SCRIPT</h2>");
                sb.AppendLine("<pre>" + EscapeHtml(rawOutput) + "</pre>");
            }

            sb.AppendLine("</body></html>");
            return sb.ToString();
        }

        private static string EscapeMd(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("|", "\\|").Replace("\r\n", " ").Replace("\n", " ");
        }

        private static string EscapeCsv(string s)
        {
            if (string.IsNullOrEmpty(s)) return "\"\"";
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        }

        private static string EscapeHtml(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }

        #region System Maintenance Scripts (Print Spooler, etc.)

        public static string GetPrintSpoolerRestartScript()
        {
            return @"# LAN CONTROL - KHỞI ĐỘNG LẠI DỊCH VỤ PRINT SPOOLER & DỌN DẸP HÀNG ĐỢI IN BỊ KẸT
$ErrorActionPreference = 'SilentlyContinue'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8
try { [Console]::InputEncoding = [System.Text.Encoding]::UTF8 } catch {}
try { chcp 65001 >$null } catch {}

Write-Output '================================================================================'
Write-Output '       KHỞI ĐỘNG LẠI SERVICE PRINT SPOOLER & DỌN DẸP HÀNG ĐỢI IN BỊ KẸT        '
Write-Output '================================================================================'
Write-Output ""Thời gian thực thi: $(Get-Date -Format 'dd/MM/yyyy HH:mm:ss')""
Write-Output ""Tên máy tính:       $($env:COMPUTERNAME)""
Write-Output ""Người dùng:         $($env:USERNAME)""
Write-Output '--------------------------------------------------------------------------------'

# 1. DỪNG DỊCH VỤ PRINT SPOOLER
Write-Output ''
Write-Output '[1/4] Đang dừng dịch vụ Print Spooler (Spooler)...'
Stop-Service -Name 'Spooler' -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1

# Đảm bảo tiến trình spoolsv dừng hẳn nếu bị treo cứng
$spoolProc = Get-Process -Name 'spoolsv' -ErrorAction SilentlyContinue
if ($spoolProc) {
    Write-Output ""  -> Tiến trình spoolsv vẫn đang chạy (PID: $($spoolProc.Id)), đang buộc dừng...""
    Stop-Process -Id $spoolProc.Id -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 600
}
Write-Output '  -> Đã dừng dịch vụ Spooler thành công.'

# 2. XÓA CÁC TỆP IN BỊ KẸT TRONG THƯ MỤC SPOOL
Write-Output ''
Write-Output '[2/4] Đang dọn sạch hàng đợi và các tệp in bị kẹt (C:\Windows\System32\spool\PRINTERS)...'
$spoolDir = Join-Path $env:SystemRoot 'System32\spool\PRINTERS'
$stuckCount = 0
$freedBytes = 0

if (Test-Path $spoolDir) {
    $stuckFiles = Get-ChildItem -Path $spoolDir -File -Force -ErrorAction SilentlyContinue
    if ($stuckFiles -and $stuckFiles.Count -gt 0) {
        foreach ($f in $stuckFiles) {
            try {
                $freedBytes += $f.Length
                Remove-Item -Path $f.FullName -Force -ErrorAction SilentlyContinue
                $stuckCount++
            } catch {}
        }
        $freedKB = [math]::Round($freedBytes / 1KB, 2)
        Write-Output ""  -> [THÀNH CÔNG] Đã xóa $stuckCount lệnh in bị kẹt (Giải phóng: $freedKB KB).""
    } else {
        Write-Output '  -> [OK] Không có lệnh in nào bị kẹt trong hàng đợi.'
    }
} else {
    Write-Output ""  -> Không tìm thấy thư mục spool: $spoolDir""
}

# 3. KHỞI ĐỘNG LẠI DỊCH VỤ PRINT SPOOLER
Write-Output ''
Write-Output '[3/4] Đang khởi động lại dịch vụ Print Spooler...'
Start-Service -Name 'Spooler' -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1

$svc = Get-Service -Name 'Spooler' -ErrorAction SilentlyContinue
$statusText = if ($svc) { $svc.Status.ToString().ToUpper() } else { 'KHÔNG TÌM THẤY' }

if ($statusText -eq 'RUNNING') {
    Write-Output ""  -> [THÀNH CÔNG] Dịch vụ Print Spooler đã khởi động lại và đang hoạt động (Status: $statusText).""
} else {
    Write-Output ""  -> [CẢNH BÁO] Trạng thái dịch vụ Spooler hiện tại: $statusText.""
}

# 4. DANH SÁCH MÁY IN TRÊN HỆ THỐNG
Write-Output ''
Write-Output '[4/4] Danh sách máy in khả dụng trên máy trạm:'
try {
    $printers = Get-CimInstance Win32_Printer -ErrorAction SilentlyContinue
    if (-not $printers) { $printers = Get-WmiObject Win32_Printer -ErrorAction SilentlyContinue }
    if ($printers) {
        foreach ($p in $printers) {
            $defMark = if ($p.Default) { ' [MẶC ĐỊNH]' } else { '' }
            $netMark = if ($p.Network) { ' (Network Printer)' } else { ' (Local USB/Port)' }
            $stat = if ($p.PrinterStatus) { ""Trạng thái: $($p.PrinterStatus)"" } else { 'Sẵn sàng' }
            Write-Output ""  - $($p.Name)$defMark: Cổng: $($p.PortName) | Driver: $($p.DriverName)$netMark | $stat""
        }
    } else {
        Write-Output '  (Không phát hiện máy in nào được cài đặt)'
    }
} catch {
    Write-Output ""  (Lỗi lấy danh sách máy in: $($_.Exception.Message))""
}

Write-Output ''
Write-Output '================================================================================'
Write-Output ""KẾT QUẢ CUỐI CÙNG: $(if ($statusText -eq 'RUNNING') { 'THÀNH CÔNG - HỆ THỐNG IN ẤN ĐÃ HOẠT ĐỘNG BÌNH THƯỜNG' } else { 'CHƯA HOÀN TẤT - VUI LÒNG KIỂM TRA LẠI DỊCH VỤ' })""
Write-Output '================================================================================'
";
        }

        #endregion

        #endregion
    }
}
