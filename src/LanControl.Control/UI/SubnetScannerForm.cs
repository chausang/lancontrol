using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using LanControl.Core.Models;
using LanControl.Core.Utils;

namespace LanControl.Control.UI
{
    public class SubnetScannerForm : Form
    {
        private readonly ControlClient _client;

        private ComboBox _cboPresets;
        private TextBox _txtStartIp;
        private TextBox _txtEndIp;
        private TextBox _txtCidr;
        private TextBox _txtPorts;
        private NumericUpDown _numTimeout;
        private NumericUpDown _numThreads;
        private Button _btnScan;
        private Button _btnExport;
        private Button _btnClear;
        private Label _lblStatus;
        private ProgressBar _progressBar;

        private DataGridView _grid;
        private bool _isScanning = false;
        private Thread _scanThread;

        public SubnetScannerForm(ControlClient client)
        {
            _client = client;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "LAN Control - Bộ Quét Mạng & Dò Tìm Thiết Bị Đa VLAN (Configurable Multi-Subnet Scanner)";
            this.Size = new Size(950, 680);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = ModernTheme.BackgroundDark;
            this.ForeColor = ModernTheme.TextPrimary;

            // Top Configuration Box
            GroupBox grpConfig = new GroupBox
            {
                Text = "⚙️ Cấu Hình Dãy IP & Cổng Cần Quét (Configurable IP Range & Ports)",
                Dock = DockStyle.Top,
                Height = 150,
                ForeColor = ModernTheme.AccentCyan,
                Font = ModernTheme.SubHeaderFont,
                BackColor = ModernTheme.PanelDark,
                Padding = new Padding(10)
            };

            // Preset Selection
            Label lblPreset = new Label { Text = "Mẫu Dãy IP Nhanh:", Location = new Point(15, 26), AutoSize = true, Font = ModernTheme.RegularFont, ForeColor = ModernTheme.TextSecondary };
            _cboPresets = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(140, 23),
                Width = 320,
                BackColor = ModernTheme.CardDark,
                ForeColor = ModernTheme.TextPrimary,
                Font = ModernTheme.RegularFont
            };
            _cboPresets.Items.Add("Tùy chỉnh dãy IP thủ công (Custom Range)");
            _cboPresets.Items.Add("Dải mạng hiện tại (192.168.1.1 - 192.168.1.254)");
            _cboPresets.Items.Add("VLAN 10 - Phòng Kỹ Thuật (192.168.10.0/24)");
            _cboPresets.Items.Add("VLAN 20 - Phòng Kế Toán (192.168.20.0/24)");
            _cboPresets.Items.Add("VLAN 30 - Chi Nhánh / Máy Chủ (10.0.30.0/24)");
            _cboPresets.Items.Add("Localhost Thử Nghiệm (127.0.0.1 - 127.0.0.10)");
            _cboPresets.SelectedIndex = 1;
            _cboPresets.SelectedIndexChanged += OnPresetChanged;

            // Start IP & End IP
            Label lblStart = new Label { Text = "IP Bắt Đầu:", Location = new Point(15, 58), AutoSize = true, Font = ModernTheme.RegularFont, ForeColor = ModernTheme.TextSecondary };
            _txtStartIp = new TextBox { Text = "192.168.1.1", Location = new Point(140, 55), Width = 140, BackColor = ModernTheme.CardDark, ForeColor = ModernTheme.TextPrimary, Font = ModernTheme.RegularFont, BorderStyle = BorderStyle.FixedSingle };

            Label lblEnd = new Label { Text = "IP Kết Thúc:", Location = new Point(295, 58), AutoSize = true, Font = ModernTheme.RegularFont, ForeColor = ModernTheme.TextSecondary };
            _txtEndIp = new TextBox { Text = "192.168.1.254", Location = new Point(375, 55), Width = 140, BackColor = ModernTheme.CardDark, ForeColor = ModernTheme.TextPrimary, Font = ModernTheme.RegularFont, BorderStyle = BorderStyle.FixedSingle };

            Label lblCidr = new Label { Text = "Hoặc CIDR:", Location = new Point(530, 58), AutoSize = true, Font = ModernTheme.RegularFont, ForeColor = ModernTheme.TextSecondary };
            _txtCidr = new TextBox { Text = "", Location = new Point(610, 55), Width = 120, BackColor = ModernTheme.CardDark, ForeColor = ModernTheme.TextPrimary, Font = ModernTheme.RegularFont, BorderStyle = BorderStyle.FixedSingle };

            // Ports, Timeout, Threads
            Label lblPorts = new Label { Text = "Cổng Cần Dò (Ports):", Location = new Point(15, 90), AutoSize = true, Font = ModernTheme.RegularFont, ForeColor = ModernTheme.TextSecondary };
            _txtPorts = new TextBox { Text = "8443, 3389, 22, 80, 443", Location = new Point(140, 87), Width = 200, BackColor = ModernTheme.CardDark, ForeColor = ModernTheme.TextPrimary, Font = ModernTheme.RegularFont, BorderStyle = BorderStyle.FixedSingle };

            Label lblTimeout = new Label { Text = "Timeout (ms):", Location = new Point(355, 90), AutoSize = true, Font = ModernTheme.RegularFont, ForeColor = ModernTheme.TextSecondary };
            _numTimeout = new NumericUpDown();
            _numTimeout.Minimum = 50;
            _numTimeout.Maximum = 5000;
            _numTimeout.Increment = 50;
            _numTimeout.Value = 400;
            _numTimeout.Location = new Point(440, 87);
            _numTimeout.Width = 75;
            _numTimeout.BackColor = ModernTheme.CardDark;
            _numTimeout.ForeColor = ModernTheme.TextPrimary;
            _numTimeout.Font = ModernTheme.RegularFont;

            Label lblThreads = new Label { Text = "Luồng (Threads):", Location = new Point(530, 90), AutoSize = true, Font = ModernTheme.RegularFont, ForeColor = ModernTheme.TextSecondary };
            _numThreads = new NumericUpDown();
            _numThreads.Minimum = 1;
            _numThreads.Maximum = 250;
            _numThreads.Value = 80;
            _numThreads.Location = new Point(635, 87);
            _numThreads.Width = 65;
            _numThreads.BackColor = ModernTheme.CardDark;
            _numThreads.ForeColor = ModernTheme.TextPrimary;
            _numThreads.Font = ModernTheme.RegularFont;

            // Action Buttons
            _btnScan = new Button { Text = "🔍 BẮT ĐẦU QUÉT", Location = new Point(745, 22), Size = new Size(165, 34) };
            ModernTheme.ApplyToButton(_btnScan, ModernTheme.AccentGreen, Color.White);
            _btnScan.Click += OnStartScanClicked;

            _btnExport = new Button { Text = "💾 Xuất File CSV", Location = new Point(745, 62), Size = new Size(165, 28) };
            ModernTheme.ApplyToButton(_btnExport, ModernTheme.AccentBlue, Color.White);
            _btnExport.Click += OnExportCsv;

            _btnClear = new Button { Text = "🧹 Xóa Bảng", Location = new Point(745, 96), Size = new Size(165, 28) };
            ModernTheme.ApplyToButton(_btnClear, ModernTheme.CardDark, ModernTheme.TextPrimary);
            _btnClear.Click += (s, e) => _grid.Rows.Clear();

            grpConfig.Controls.Add(lblPreset);
            grpConfig.Controls.Add(_cboPresets);
            grpConfig.Controls.Add(lblStart);
            grpConfig.Controls.Add(_txtStartIp);
            grpConfig.Controls.Add(lblEnd);
            grpConfig.Controls.Add(_txtEndIp);
            grpConfig.Controls.Add(lblCidr);
            grpConfig.Controls.Add(_txtCidr);
            grpConfig.Controls.Add(lblPorts);
            grpConfig.Controls.Add(_txtPorts);
            grpConfig.Controls.Add(lblTimeout);
            grpConfig.Controls.Add(_numTimeout);
            grpConfig.Controls.Add(lblThreads);
            grpConfig.Controls.Add(_numThreads);
            grpConfig.Controls.Add(_btnScan);
            grpConfig.Controls.Add(_btnExport);
            grpConfig.Controls.Add(_btnClear);

            // Status Bar & Progress
            Panel pnlProgress = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 35,
                BackColor = ModernTheme.PanelDark,
                Padding = new Padding(8)
            };

            _lblStatus = new Label
            {
                Text = "Sẵn sàng quét.",
                Dock = DockStyle.Left,
                AutoSize = true,
                ForeColor = ModernTheme.TextSecondary,
                Font = ModernTheme.RegularFont,
                Location = new Point(8, 8)
            };

            _progressBar = new ProgressBar
            {
                Dock = DockStyle.Right,
                Width = 280,
                Height = 18,
                Style = ProgressBarStyle.Continuous
            };

            pnlProgress.Controls.Add(_lblStatus);
            pnlProgress.Controls.Add(_progressBar);

            // DataGridView Results
            _grid = new DataGridView
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

            _grid.Columns.Add("IpAddress", "Địa Chỉ IP");
            _grid.Columns.Add("Hostname", "Tên Thiết Bị (Hostname)");
            _grid.Columns.Add("Status", "Trạng Thái");
            _grid.Columns.Add("Ping", "Ping (ms)");
            _grid.Columns.Add("OpenPorts", "Cổng Mở (Open Ports)");
            _grid.Columns.Add("HasAgent", "LAN Control Agent?");

            this.Controls.Add(_grid);
            this.Controls.Add(pnlProgress);
            this.Controls.Add(grpConfig);
        }

        private void OnPresetChanged(object sender, EventArgs e)
        {
            switch (_cboPresets.SelectedIndex)
            {
                case 1:
                    _txtStartIp.Text = "192.168.1.1";
                    _txtEndIp.Text = "192.168.1.254";
                    _txtCidr.Text = "";
                    break;
                case 2:
                    _txtStartIp.Text = "192.168.10.1";
                    _txtEndIp.Text = "192.168.10.254";
                    _txtCidr.Text = "192.168.10.0/24";
                    break;
                case 3:
                    _txtStartIp.Text = "192.168.20.1";
                    _txtEndIp.Text = "192.168.20.254";
                    _txtCidr.Text = "192.168.20.0/24";
                    break;
                case 4:
                    _txtStartIp.Text = "10.0.30.1";
                    _txtEndIp.Text = "10.0.30.254";
                    _txtCidr.Text = "10.0.30.0/24";
                    break;
                case 5:
                    _txtStartIp.Text = "127.0.0.1";
                    _txtEndIp.Text = "127.0.0.10";
                    _txtCidr.Text = "";
                    break;
            }
        }

        private void OnStartScanClicked(object sender, EventArgs e)
        {
            if (_isScanning)
            {
                // Cancel scan
                _isScanning = false;
                _btnScan.Text = "🔍 BẮT ĐẦU QUÉT";
                ModernTheme.ApplyToButton(_btnScan, ModernTheme.AccentGreen, Color.White);
                _lblStatus.Text = "Đã dừng quét.";
                return;
            }

            ScanConfig cfg = new ScanConfig
            {
                StartIp = _txtStartIp.Text.Trim(),
                EndIp = _txtEndIp.Text.Trim(),
                Cidr = _txtCidr.Text.Trim(),
                PortList = _txtPorts.Text.Trim(),
                TimeoutMs = (int)_numTimeout.Value,
                MaxConcurrency = (int)_numThreads.Value
            };

            _isScanning = true;
            _btnScan.Text = "🛑 DỪNG QUÉT";
            ModernTheme.ApplyToButton(_btnScan, ModernTheme.AccentRed, Color.White);
            _progressBar.Value = 0;
            _lblStatus.Text = "Đang khởi tạo danh sách IP...";

            _scanThread = new Thread(delegate()
            {
                try
                {
                    SubnetScanner scanner = new SubnetScanner();
                    scanner.ProgressChanged += (scanned, total) =>
                    {
                        if (this.InvokeRequired)
                        {
                            this.BeginInvoke(new Action(delegate
                            {
                                int pct = total > 0 ? (int)((double)scanned / total * 100) : 0;
                                _progressBar.Value = Math.Min(100, Math.Max(0, pct));
                                _lblStatus.Text = string.Format("Đã quét: {0}/{1} ({2}%)", scanned, total, pct);
                            }));
                        }
                    };

                    scanner.HostScanned += (host) =>
                    {
                        if (host.IsOnline)
                        {
                            if (this.InvokeRequired)
                            {
                                this.BeginInvoke(new Action(delegate
                                {
                                    int rowIndex = _grid.Rows.Add(
                                        host.IpAddress,
                                        host.Hostname,
                                        "Online",
                                        host.PingMs >= 0 ? host.PingMs + " ms" : "Open Port",
                                        host.OpenPorts,
                                        host.HasAgent ? "✅ ĐÃ CÀI ĐẶT (Port 8443)" : "Chưa cài đặt"
                                    );

                                    if (host.HasAgent)
                                    {
                                        _grid.Rows[rowIndex].DefaultCellStyle.BackColor = Color.FromArgb(20, 50, 40);
                                        _grid.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.FromArgb(80, 240, 140);
                                    }
                                }));
                            }
                        }
                    };

                    scanner.Scan(cfg);

                    if (this.InvokeRequired)
                    {
                        this.BeginInvoke(new Action(delegate
                        {
                            _isScanning = false;
                            _btnScan.Text = "🔍 BẮT ĐẦU QUÉT";
                            ModernTheme.ApplyToButton(_btnScan, ModernTheme.AccentGreen, Color.White);
                            _lblStatus.Text = string.Format("Quét hoàn tất! Tìm thấy {0} thiết bị hoạt động.", _grid.Rows.Count);
                        }));
                    }
                }
                catch (Exception ex)
                {
                    if (this.InvokeRequired)
                    {
                        this.BeginInvoke(new Action(delegate
                        {
                            _isScanning = false;
                            _btnScan.Text = "🔍 BẮT ĐẦU QUÉT";
                            _lblStatus.Text = "Lỗi khi quét: " + ex.Message;
                        }));
                    }
                }
            })
            {
                IsBackground = true
            };
            _scanThread.Start();
        }

        private void OnExportCsv(object sender, EventArgs e)
        {
            if (_grid.Rows.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu để xuất!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog { Filter = "CSV Files (*.csv)|*.csv", FileName = "Subnet_Scan_Results.csv" })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        using (StreamWriter sw = new StreamWriter(sfd.FileName))
                        {
                            sw.WriteLine("IP Address,Hostname,Status,Ping,Open Ports,Agent Installed");
                            foreach (DataGridViewRow row in _grid.Rows)
                            {
                                sw.WriteLine(string.Format("\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\"",
                                    row.Cells[0].Value, row.Cells[1].Value, row.Cells[2].Value,
                                    row.Cells[3].Value, row.Cells[4].Value, row.Cells[5].Value));
                            }
                        }
                        MessageBox.Show("Xuất file CSV thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Lỗi khi lưu file: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
}
