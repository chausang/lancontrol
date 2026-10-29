using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using LanControl.Core.Models;
using LanControl.Core.Network;

namespace LanControl.Control.UI
{
    public class FileDistributionForm : Form
    {
        private readonly ControlClient _client;
        private readonly List<AgentInfo> _targetAgents;
        private readonly List<string> _filesToSend = new List<string>();

        private bool _isTransferring = false;
        private bool _cancelRequested = false;
        private Thread _workerThread;

        // UI Controls
        private ListView _lvFiles;
        private CheckedListBox _clbAgents;
        private ComboBox _cboTargetFolder;
        private TextBox _txtCustomPath;
        private CheckBox _chkOverwrite;
        private CheckBox _chkExecute;
        private TextBox _txtArguments;
        private DataGridView _gridProgress;
        private Button _btnAddFiles;
        private Button _btnAddFolder;
        private Button _btnRemoveFile;
        private Button _btnClearFiles;
        private Button _btnStart;
        private Button _btnCancel;
        private Label _lblSummary;
        private ProgressBar _overallProgress;

        public FileDistributionForm(ControlClient client, List<AgentInfo> targetAgents)
            : this(client, targetAgents, null)
        {
        }

        public FileDistributionForm(ControlClient client, List<AgentInfo> targetAgents, List<string> initialFiles)
        {
            _client = client;
            _targetAgents = targetAgents != null && targetAgents.Count > 0 ? targetAgents : new List<AgentInfo>();
            if (initialFiles != null)
            {
                foreach (string f in initialFiles)
                {
                    if (File.Exists(f) && !_filesToSend.Contains(f))
                    {
                        _filesToSend.Add(f);
                    }
                }
            }
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            if (_targetAgents != null && _targetAgents.Count == 1)
            {
                this.Text = string.Format("Truyền Tập Tin Nhanh Đến Máy Trạm: {0} ({1})", _targetAgents[0].Hostname, _targetAgents[0].IpAddress);
            }
            else
            {
                this.Text = "Phát Tán & Truyền Tập Tin Hàng Loạt Tới Các Máy Trạm (Mass File Distribution)";
            }
            this.Size = new Size(1180, 750);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = ModernTheme.BackgroundDark;
            this.ForeColor = ModernTheme.TextPrimary;

            // Main Layout: SplitContainer Left (Agents + Files) & Right (Config + Progress)
            SplitContainer splitMain = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                BackColor = ModernTheme.BorderDark
            };

            // LEFT PANEL: Files to send & Agent Selector
            Panel pnlLeft = new Panel { Dock = DockStyle.Fill, BackColor = ModernTheme.PanelDark, Padding = new Padding(10) };

            Label lblAgentHeader = new Label
            {
                Text = "🖥️ DANH SÁCH MÁY TRẠM NHẬN TẬP TIN",
                Dock = DockStyle.Top,
                Height = 26,
                Font = ModernTheme.SubHeaderFont,
                ForeColor = ModernTheme.AccentCyan
            };

            _clbAgents = new CheckedListBox
            {
                Dock = DockStyle.Top,
                Height = 160,
                BackColor = ModernTheme.CardDark,
                ForeColor = ModernTheme.TextPrimary,
                Font = ModernTheme.RegularFont,
                BorderStyle = BorderStyle.FixedSingle,
                CheckOnClick = true
            };

            Panel pnlAgentBtns = new Panel { Dock = DockStyle.Top, Height = 34, Padding = new Padding(0, 4, 0, 4) };
            Button btnSelectAll = new Button { Text = "Tất cả", Width = 70, Dock = DockStyle.Left };
            ModernTheme.ApplyToButton(btnSelectAll, ModernTheme.CardDark, ModernTheme.TextPrimary);
            btnSelectAll.Click += (s, e) => SetAllAgentsChecked(true);

            Button btnSelectNone = new Button { Text = "Bỏ chọn", Width = 70, Dock = DockStyle.Left, Margin = new Padding(6, 0, 0, 0) };
            ModernTheme.ApplyToButton(btnSelectNone, ModernTheme.CardDark, ModernTheme.TextSecondary);
            btnSelectNone.Click += (s, e) => SetAllAgentsChecked(false);

            pnlAgentBtns.Controls.Add(btnSelectNone);
            pnlAgentBtns.Controls.Add(btnSelectAll);

            Label lblFileHeader = new Label
            {
                Text = "📦 DANH SÁCH TẬP TIN SẼ TRUYỀN",
                Dock = DockStyle.Top,
                Height = 26,
                Font = ModernTheme.SubHeaderFont,
                ForeColor = ModernTheme.AccentGreen,
                Margin = new Padding(0, 10, 0, 0)
            };

            _lvFiles = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                BackColor = ModernTheme.CardDark,
                ForeColor = ModernTheme.TextPrimary,
                Font = ModernTheme.RegularFont,
                BorderStyle = BorderStyle.FixedSingle
            };
            _lvFiles.Columns.Add("Tên Tập Tin / Thư Mục", 230);
            _lvFiles.Columns.Add("Dung Lượng", 90, HorizontalAlignment.Right);
            _lvFiles.Columns.Add("Đường Dẫn Nguồn", 120);

            Panel pnlFileBtns = new Panel { Dock = DockStyle.Bottom, Height = 40, Padding = new Padding(0, 6, 0, 0) };
            _btnAddFiles = new Button { Text = "➕ Thêm Tệp...", Width = 110, Dock = DockStyle.Left };
            ModernTheme.ApplyToButton(_btnAddFiles, ModernTheme.AccentBlue, Color.White);
            _btnAddFiles.Click += OnAddFilesClicked;

            _btnAddFolder = new Button { Text = "📁 Thêm Thư Mục...", Width = 135, Dock = DockStyle.Left };
            ModernTheme.ApplyToButton(_btnAddFolder, ModernTheme.CardDark, ModernTheme.AccentCyan);
            _btnAddFolder.Click += OnAddFolderClicked;

            _btnRemoveFile = new Button { Text = "❌ Xóa", Width = 75, Dock = DockStyle.Left };
            ModernTheme.ApplyToButton(_btnRemoveFile, ModernTheme.CardDark, ModernTheme.AccentRed);
            _btnRemoveFile.Click += OnRemoveFileClicked;

            _btnClearFiles = new Button { Text = "🧹 Xóa Hết", Width = 85, Dock = DockStyle.Left };
            ModernTheme.ApplyToButton(_btnClearFiles, ModernTheme.CardDark, ModernTheme.TextSecondary);
            _btnClearFiles.Click += (s, e) => { _filesToSend.Clear(); RefreshFilesList(); };

            pnlFileBtns.Controls.Add(_btnClearFiles);
            pnlFileBtns.Controls.Add(_btnRemoveFile);
            pnlFileBtns.Controls.Add(_btnAddFolder);
            pnlFileBtns.Controls.Add(_btnAddFiles);

            pnlLeft.Controls.Add(_lvFiles);
            pnlLeft.Controls.Add(pnlFileBtns);
            pnlLeft.Controls.Add(lblFileHeader);
            pnlLeft.Controls.Add(pnlAgentBtns);
            pnlLeft.Controls.Add(_clbAgents);
            pnlLeft.Controls.Add(lblAgentHeader);
            splitMain.Panel1.Controls.Add(pnlLeft);

            // RIGHT PANEL: Configuration & Real-time Progress Grid
            Panel pnlRight = new Panel { Dock = DockStyle.Fill, BackColor = ModernTheme.PanelDark, Padding = new Padding(12) };

            // GroupBox: Destination Configuration
            GroupBox grpDest = new GroupBox
            {
                Text = "⚙️ CẤU HÌNH ĐÍCH ĐẾN & TÙY CHỌN TRUYỀN",
                Dock = DockStyle.Top,
                Height = 165,
                Font = ModernTheme.SubHeaderFont,
                ForeColor = ModernTheme.AccentCyan,
                Padding = new Padding(10)
            };

            Label lblTarget = new Label { Text = "Nơi lưu tệp trên máy trạm:", Location = new Point(14, 28), AutoSize = true, Font = ModernTheme.RegularFont, ForeColor = ModernTheme.TextPrimary };
            _cboTargetFolder = new ComboBox
            {
                Location = new Point(200, 24),
                Width = 260,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = ModernTheme.CardDark,
                ForeColor = ModernTheme.TextPrimary,
                Font = ModernTheme.RegularFont
            };
            _cboTargetFolder.Items.AddRange(new object[]
            {
                "🖥️ Màn hình chính (Desktop)",
                "📥 Thư mục Tải về (Downloads)",
                "📁 Thư mục Tạm (C:\\Temp)",
                "📂 Đường dẫn tùy chỉnh..."
            });
            _cboTargetFolder.SelectedIndex = 0;
            _cboTargetFolder.SelectedIndexChanged += (s, e) =>
            {
                _txtCustomPath.Enabled = (_cboTargetFolder.SelectedIndex == 3);
            };

            _txtCustomPath = new TextBox
            {
                Location = new Point(470, 24),
                Width = 200,
                Text = @"C:\Data",
                Enabled = false,
                BackColor = ModernTheme.CardDark,
                ForeColor = ModernTheme.TextPrimary,
                Font = ModernTheme.RegularFont
            };

            _chkOverwrite = new CheckBox
            {
                Text = "Ghi đè tệp tin nếu đã tồn tại trên máy trạm (Overwrite)",
                Location = new Point(18, 62),
                AutoSize = true,
                Checked = true,
                Font = ModernTheme.RegularFont,
                ForeColor = ModernTheme.TextPrimary
            };

            _chkExecute = new CheckBox
            {
                Text = "⚡ Tự động mở / chạy tệp sau khi truyền xong (Run after transfer)",
                Location = new Point(18, 92),
                AutoSize = true,
                Checked = false,
                Font = ModernTheme.RegularFont,
                ForeColor = ModernTheme.AccentGreen
            };
            _chkExecute.CheckedChanged += (s, e) => { _txtArguments.Enabled = _chkExecute.Checked; };

            Label lblArgs = new Label { Text = "Tham số chạy (Arguments, vd: /s /silent):", Location = new Point(36, 126), AutoSize = true, Font = ModernTheme.RegularFont, ForeColor = ModernTheme.TextSecondary };
            _txtArguments = new TextBox
            {
                Location = new Point(310, 123),
                Width = 240,
                Enabled = false,
                BackColor = ModernTheme.CardDark,
                ForeColor = ModernTheme.TextPrimary,
                Font = ModernTheme.RegularFont
            };

            grpDest.Controls.Add(lblTarget);
            grpDest.Controls.Add(_cboTargetFolder);
            grpDest.Controls.Add(_txtCustomPath);
            grpDest.Controls.Add(_chkOverwrite);
            grpDest.Controls.Add(_chkExecute);
            grpDest.Controls.Add(lblArgs);
            grpDest.Controls.Add(_txtArguments);

            // Progress Section
            Label lblProgHeader = new Label
            {
                Text = "📊 TIẾN TRÌNH TRUYỀN TẬP TIN THỜI GIAN THỰC",
                Dock = DockStyle.Top,
                Height = 28,
                Font = ModernTheme.SubHeaderFont,
                ForeColor = ModernTheme.AccentYellow
            };

            _gridProgress = new DataGridView
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
            _gridProgress.DefaultCellStyle.BackColor = ModernTheme.CardDark;
            _gridProgress.DefaultCellStyle.ForeColor = ModernTheme.TextPrimary;
            _gridProgress.DefaultCellStyle.SelectionBackColor = ModernTheme.AccentBlue;
            _gridProgress.DefaultCellStyle.Font = ModernTheme.RegularFont;
            _gridProgress.ColumnHeadersDefaultCellStyle.BackColor = ModernTheme.PanelDark;
            _gridProgress.ColumnHeadersDefaultCellStyle.ForeColor = ModernTheme.TextPrimary;
            _gridProgress.ColumnHeadersDefaultCellStyle.Font = ModernTheme.SubHeaderFont;
            _gridProgress.EnableHeadersVisualStyles = false;

            _gridProgress.Columns.Add("Hostname", "Tên Máy");
            _gridProgress.Columns.Add("IpAddress", "Địa Chỉ IP");
            _gridProgress.Columns.Add("Vlan", "Phân Đoạn VLAN");
            _gridProgress.Columns.Add("CurrentFile", "Tập Tin");
            _gridProgress.Columns.Add("Progress", "Tiến Độ");
            _gridProgress.Columns.Add("Transferred", "Đã Gửi");
            _gridProgress.Columns.Add("Status", "Trạng Thái");

            // Bottom Actions Ribbon
            Panel pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 64, Padding = new Padding(0, 8, 0, 0) };

            _overallProgress = new ProgressBar { Dock = DockStyle.Top, Height = 10, Minimum = 0, Maximum = 100, Value = 0 };

            Panel pnlBottomBtns = new Panel { Dock = DockStyle.Bottom, Height = 48, Padding = new Padding(0, 8, 0, 0) };
            _lblSummary = new Label
            {
                Text = "Sẵn sàng truyền tập tin.",
                Dock = DockStyle.Left,
                AutoSize = true,
                Font = ModernTheme.RegularFont,
                ForeColor = ModernTheme.TextSecondary,
                Location = new Point(0, 14)
            };

            _btnStart = new Button
            {
                Text = "🚀 BẮT ĐẦU TRUYỀN HÀNG LOẠT",
                Size = new Size(230, 36),
                Dock = DockStyle.Right,
                Font = ModernTheme.SubHeaderFont
            };
            ModernTheme.ApplyToButton(_btnStart, ModernTheme.AccentGreen, Color.White);
            _btnStart.Click += OnStartClicked;

            _btnCancel = new Button
            {
                Text = "⏹️ Dừng Lại",
                Size = new Size(110, 36),
                Dock = DockStyle.Right,
                Enabled = false
            };
            ModernTheme.ApplyToButton(_btnCancel, ModernTheme.CardDark, ModernTheme.AccentRed);
            _btnCancel.Click += OnCancelClicked;

            pnlBottomBtns.Controls.Add(_lblSummary);
            pnlBottomBtns.Controls.Add(_btnCancel);
            pnlBottomBtns.Controls.Add(_btnStart);

            pnlBottom.Controls.Add(pnlBottomBtns);
            pnlBottom.Controls.Add(_overallProgress);

            pnlRight.Controls.Add(_gridProgress);
            pnlRight.Controls.Add(lblProgHeader);
            pnlRight.Controls.Add(pnlBottom);
            pnlRight.Controls.Add(grpDest);
            splitMain.Panel2.Controls.Add(pnlRight);

            this.Controls.Add(splitMain);

            // Events & Populate
            this.Load += (s, e) =>
            {
                try
                {
                    if (splitMain.Width > 500)
                        splitMain.SplitterDistance = Math.Min(460, splitMain.Width - 100);
                }
                catch { }
                PopulateAgents();
                if (_filesToSend.Count > 0)
                {
                    RefreshFilesList();
                }
                _client.FileTransferProgressReceived += OnTransferProgressReceived;
            };

            this.FormClosing += (s, e) =>
            {
                if (_isTransferring)
                {
                    if (MessageBox.Show("Quá trình truyền tập tin đang diễn ra. Bạn có chắc muốn hủy bỏ và đóng cửa sổ?", "Xác nhận đóng", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
                    {
                        e.Cancel = true;
                        return;
                    }
                }
                _client.FileTransferProgressReceived -= OnTransferProgressReceived;
                _cancelRequested = true;
            };
        }

        private void PopulateAgents()
        {
            _clbAgents.Items.Clear();
            _gridProgress.Rows.Clear();

            foreach (var a in _targetAgents)
            {
                string label = string.Format("{0} ({1}) - {2}", a.Hostname, a.IpAddress, a.VlanTag);
                _clbAgents.Items.Add(label, true);

                int rowIdx = _gridProgress.Rows.Add(
                    a.Hostname,
                    a.IpAddress,
                    a.VlanTag,
                    "---",
                    "0%",
                    "0 KB",
                    "⏳ Đang chờ..."
                );
                _gridProgress.Rows[rowIdx].Tag = a.AgentId;
            }
        }

        private void SetAllAgentsChecked(bool isChecked)
        {
            for (int i = 0; i < _clbAgents.Items.Count; i++)
            {
                _clbAgents.SetItemChecked(i, isChecked);
            }
        }

        private List<AgentInfo> GetSelectedAgents()
        {
            List<AgentInfo> list = new List<AgentInfo>();
            for (int i = 0; i < _clbAgents.Items.Count; i++)
            {
                if (_clbAgents.GetItemChecked(i) && i < _targetAgents.Count)
                {
                    list.Add(_targetAgents[i]);
                }
            }
            return list;
        }

        private void OnAddFilesClicked(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Chọn các tập tin cần truyền tới máy trạm";
                ofd.Multiselect = true;
                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    foreach (string f in ofd.FileNames)
                    {
                        if (!_filesToSend.Contains(f))
                        {
                            _filesToSend.Add(f);
                        }
                    }
                    RefreshFilesList();
                }
            }
        }

        private void OnAddFolderClicked(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Chọn thư mục nguồn (toàn bộ tệp trong thư mục sẽ được thêm vào)";
                if (fbd.ShowDialog(this) == DialogResult.OK)
                {
                    string[] files = Directory.GetFiles(fbd.SelectedPath, "*.*", SearchOption.AllDirectories);
                    foreach (string f in files)
                    {
                        if (!_filesToSend.Contains(f))
                        {
                            _filesToSend.Add(f);
                        }
                    }
                    RefreshFilesList();
                }
            }
        }

        private void OnRemoveFileClicked(object sender, EventArgs e)
        {
            if (_lvFiles.SelectedItems.Count == 0) return;
            List<string> toRemove = new List<string>();
            foreach (ListViewItem item in _lvFiles.SelectedItems)
            {
                toRemove.Add(item.Tag as string);
            }
            foreach (string p in toRemove)
            {
                _filesToSend.Remove(p);
            }
            RefreshFilesList();
        }

        private void RefreshFilesList()
        {
            _lvFiles.Items.Clear();
            long totalBytes = 0;

            foreach (string f in _filesToSend)
            {
                try
                {
                    FileInfo fi = new FileInfo(f);
                    ListViewItem lvi = new ListViewItem(fi.Name);
                    lvi.SubItems.Add(FormatSize(fi.Length));
                    lvi.SubItems.Add(fi.FullName);
                    lvi.Tag = f;
                    _lvFiles.Items.Add(lvi);
                    totalBytes += fi.Length;
                }
                catch { }
            }

            _lblSummary.Text = string.Format("Đã chọn: {0} tập tin ({1})", _filesToSend.Count, FormatSize(totalBytes));
        }

        private string GetSelectedRemoteFolder()
        {
            switch (_cboTargetFolder.SelectedIndex)
            {
                case 0: return "%Desktop%";
                case 1: return "%Downloads%";
                case 2: return "%Temp%";
                case 3: return _txtCustomPath.Text.Trim();
                default: return "%Desktop%";
            }
        }

        private void OnStartClicked(object sender, EventArgs e)
        {
            if (_filesToSend.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn ít nhất một tập tin để truyền!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            List<AgentInfo> selectedAgents = GetSelectedAgents();
            if (selectedAgents.Count == 0)
            {
                MessageBox.Show("Vui lòng tích chọn ít nhất một máy trạm nhận tập tin!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _isTransferring = true;
            _cancelRequested = false;
            _btnStart.Enabled = false;
            _btnCancel.Enabled = true;
            _btnAddFiles.Enabled = false;
            _btnAddFolder.Enabled = false;
            _btnRemoveFile.Enabled = false;
            _btnClearFiles.Enabled = false;

            string remoteFolder = GetSelectedRemoteFolder();
            bool overwrite = _chkOverwrite.Checked;
            bool execute = _chkExecute.Checked;
            string args = _txtArguments.Text.Trim();

            // Reset status in grid
            foreach (DataGridViewRow row in _gridProgress.Rows)
            {
                row.Cells[3].Value = "Chuẩn bị...";
                row.Cells[4].Value = "0%";
                row.Cells[5].Value = "0 MB";
                row.Cells[6].Value = "⏳ Đang gửi...";
                row.DefaultCellStyle.BackColor = ModernTheme.CardDark;
            }

            _workerThread = new Thread(() => RunMassTransfer(selectedAgents, remoteFolder, overwrite, execute, args))
            {
                IsBackground = true
            };
            _workerThread.Start();
        }

        private void OnCancelClicked(object sender, EventArgs e)
        {
            _cancelRequested = true;
            _lblSummary.Text = "Đang dừng truyền tập tin...";
        }

        private void RunMassTransfer(List<AgentInfo> agents, string remoteFolder, bool overwrite, bool execute, string args)
        {
            int chunkSize = 256 * 1024; // 256 KB chunk
            int totalFiles = _filesToSend.Count;

            for (int fileIdx = 0; fileIdx < totalFiles; fileIdx++)
            {
                if (_cancelRequested) break;

                string filePath = _filesToSend[fileIdx];
                FileInfo fi = new FileInfo(filePath);
                if (!fi.Exists) continue;

                long fileSize = fi.Length;
                int totalChunks = fileSize == 0 ? 1 : (int)Math.Ceiling((double)fileSize / chunkSize);

                // Update UI: Current file
                this.BeginInvoke(new Action(() =>
                {
                    _lblSummary.Text = string.Format("Đang truyền ({0}/{1}): {2} ({3})", fileIdx + 1, totalFiles, fi.Name, FormatSize(fileSize));
                    foreach (DataGridViewRow row in _gridProgress.Rows)
                    {
                        row.Cells[3].Value = fi.Name;
                    }
                }));

                // For each agent, send FileTransferRequest
                string transferId = Guid.NewGuid().ToString("N");
                foreach (var agent in agents)
                {
                    FileTransferRequest req = new FileTransferRequest
                    {
                        TransferId = transferId,
                        AgentId = agent.AgentId,
                        Action = FileTransferAction.UploadToAgent,
                        RemotePath = remoteFolder,
                        FileName = fi.Name,
                        FileSize = fileSize,
                        Overwrite = overwrite,
                        ExecuteAfterTransfer = execute,
                        ExecuteArguments = args
                    };
                    _client.SendFileTransferRequest(req);
                }

                Thread.Sleep(50); // Small pause for agents to initialize file streams

                // Stream Chunks
                try
                {
                    using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        byte[] buffer = new byte[chunkSize];
                        long offset = 0;

                        for (int chunkIdx = 0; chunkIdx < totalChunks; chunkIdx++)
                        {
                            if (_cancelRequested) break;

                            int read = fs.Read(buffer, 0, chunkSize);
                            bool isLast = (chunkIdx == totalChunks - 1) || read <= 0;

                            byte[] chunkData = new byte[read];
                            if (read > 0)
                            {
                                Buffer.BlockCopy(buffer, 0, chunkData, 0, read);
                            }

                            // Send chunk to agents (using broadcast or per agent)
                            // To optimize network broadcast over Gateway, we can send to each selected agent
                            foreach (var agent in agents)
                            {
                                FileChunkMeta meta = new FileChunkMeta
                                {
                                    TransferId = transferId,
                                    AgentId = agent.AgentId,
                                    ChunkIndex = chunkIdx,
                                    TotalChunks = totalChunks,
                                    Offset = offset,
                                    Length = read,
                                    IsLastChunk = isLast
                                };
                                _client.SendFileChunk(meta, chunkData);
                            }

                            offset += read;

                            int overallPct = totalFiles > 0 ? (int)(((fileIdx * 100.0) / totalFiles) + (((double)offset / fileSize) * (100.0 / totalFiles))) : 0;
                            if (overallPct > 100) overallPct = 100;

                            if (chunkIdx % 4 == 0 || isLast)
                            {
                                this.BeginInvoke(new Action(() =>
                                {
                                    _overallProgress.Value = overallPct;
                                }));
                            }

                            if (isLast) break;
                            Thread.Sleep(8); // Pacing
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        MessageBox.Show("Lỗi đọc tệp nguồn: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
                    break;
                }

                Thread.Sleep(100);
            }

            this.BeginInvoke(new Action(() =>
            {
                _isTransferring = false;
                _btnStart.Enabled = true;
                _btnCancel.Enabled = false;
                _btnAddFiles.Enabled = true;
                _btnAddFolder.Enabled = true;
                _btnRemoveFile.Enabled = true;
                _btnClearFiles.Enabled = true;
                _overallProgress.Value = _cancelRequested ? 0 : 100;
                _lblSummary.Text = _cancelRequested ? "Đã hủy bỏ quá trình truyền." : "Hoàn tất phát tán tập tin!";

                if (!_cancelRequested)
                {
                    MessageBox.Show("Đã hoàn tất truyền tập tin tới các máy trạm!", "Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }));
        }

        private void OnTransferProgressReceived(FileTransferProgressResponse resp)
        {
            if (resp == null) return;

            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action<FileTransferProgressResponse>(OnTransferProgressReceived), resp);
                return;
            }

            foreach (DataGridViewRow row in _gridProgress.Rows)
            {
                string agentId = row.Tag as string;
                if (agentId == resp.AgentId)
                {
                    row.Cells[3].Value = resp.FileName;
                    row.Cells[4].Value = resp.Percentage + "%";
                    row.Cells[5].Value = FormatSize(resp.BytesTransferred);

                    if (!resp.Success)
                    {
                        row.Cells[6].Value = "❌ Lỗi: " + resp.ErrorMessage;
                        row.DefaultCellStyle.ForeColor = ModernTheme.AccentRed;
                    }
                    else if (resp.Percentage >= 100)
                    {
                        row.Cells[6].Value = "✅ Hoàn tất (" + resp.SavedPath + ")";
                        row.DefaultCellStyle.ForeColor = ModernTheme.AccentGreen;
                    }
                    else
                    {
                        row.Cells[6].Value = string.Format("⚡ Đang nhận ({0}%)...", resp.Percentage);
                        row.DefaultCellStyle.ForeColor = ModernTheme.AccentCyan;
                    }
                    break;
                }
            }
        }

        private string FormatSize(long bytes)
        {
            if (bytes < 1024) return bytes + " B";
            if (bytes < 1024 * 1024) return (bytes / 1024.0).ToString("N1") + " KB";
            if (bytes < 1024 * 1024 * 1024) return (bytes / (1024.0 * 1024)).ToString("N1") + " MB";
            return (bytes / (1024.0 * 1024 * 1024)).ToString("N2") + " GB";
        }
    }
}
