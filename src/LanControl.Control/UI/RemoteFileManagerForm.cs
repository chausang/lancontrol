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
    public class RemoteFileManagerForm : Form
    {
        private readonly ControlClient _client;
        private readonly AgentInfo _agent;

        // Local Pane (Left)
        private ComboBox _cboLocalDrives;
        private TextBox _txtLocalPath;
        private Button _btnLocalUp;
        private Button _btnLocalRefresh;
        private ListView _lvLocal;

        // Remote Pane (Right)
        private ComboBox _cboRemoteDrives;
        private TextBox _txtRemotePath;
        private Button _btnRemoteUp;
        private Button _btnRemoteRefresh;
        private ListView _lvRemote;
        private ContextMenuStrip _remoteMenu;

        // Center Buttons
        private Button _btnUpload;
        private Button _btnDownload;

        // Bottom Status
        private StatusStrip _statusStrip;
        private ToolStripStatusLabel _lblStatus;
        private ToolStripProgressBar _pbProgress;

        // Active Download Session
        private class DownloadSession
        {
            public string TransferId { get; set; }
            public string LocalFinalPath { get; set; }
            public string LocalPartPath { get; set; }
            public FileStream Stream { get; set; }
        }
        private readonly Dictionary<string, DownloadSession> _activeDownloads = new Dictionary<string, DownloadSession>();
        private readonly object _dlLock = new object();

        public RemoteFileManagerForm(ControlClient client, AgentInfo agent)
        {
            _client = client;
            _agent = agent;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = string.Format("Quản Lý & Truyền Tệp Từ Xa 2 Chiều - Máy Trạm: {0} ({1} - {2})", _agent.Hostname, _agent.IpAddress, _agent.VlanTag);
            this.Size = new Size(1200, 720);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = ModernTheme.BackgroundDark;
            this.ForeColor = ModernTheme.TextPrimary;

            // Main 3-Column Layout: Left (Local), Middle (Transfer Buttons), Right (Remote)
            TableLayoutPanel tlp = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = ModernTheme.BorderDark
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46f));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70f));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54f));

            // 1. LOCAL PANE
            Panel pnlLocal = new Panel { Dock = DockStyle.Fill, BackColor = ModernTheme.PanelDark, Padding = new Padding(8) };
            Label lblLocalHeader = new Label
            {
                Text = "💻 MÁY QUẢN TRỊ (LOCAL CONSOLE)",
                Dock = DockStyle.Top,
                Height = 26,
                Font = ModernTheme.SubHeaderFont,
                ForeColor = ModernTheme.AccentCyan
            };

            Panel pnlLocalNav = new Panel { Dock = DockStyle.Top, Height = 34, Padding = new Padding(0, 2, 0, 4) };
            _cboLocalDrives = new ComboBox
            {
                Dock = DockStyle.Left,
                Width = 70,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = ModernTheme.CardDark,
                ForeColor = ModernTheme.TextPrimary,
                Font = ModernTheme.RegularFont
            };
            _cboLocalDrives.SelectedIndexChanged += OnLocalDriveChanged;

            _btnLocalUp = new Button { Text = "⬆️", Dock = DockStyle.Right, Width = 38 };
            ModernTheme.ApplyToButton(_btnLocalUp, ModernTheme.CardDark, ModernTheme.TextPrimary);
            _btnLocalUp.Click += (s, e) => NavigateLocalUp();

            _btnLocalRefresh = new Button { Text = "🔄", Dock = DockStyle.Right, Width = 38 };
            ModernTheme.ApplyToButton(_btnLocalRefresh, ModernTheme.CardDark, ModernTheme.AccentCyan);
            _btnLocalRefresh.Click += (s, e) => LoadLocalDirectory(_txtLocalPath.Text);

            _txtLocalPath = new TextBox
            {
                Dock = DockStyle.Fill,
                BackColor = ModernTheme.CardDark,
                ForeColor = ModernTheme.TextPrimary,
                Font = ModernTheme.RegularFont
            };
            _txtLocalPath.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) LoadLocalDirectory(_txtLocalPath.Text); };

            pnlLocalNav.Controls.Add(_txtLocalPath);
            pnlLocalNav.Controls.Add(_cboLocalDrives);
            pnlLocalNav.Controls.Add(_btnLocalUp);
            pnlLocalNav.Controls.Add(_btnLocalRefresh);

            _lvLocal = CreateFileListView();
            _lvLocal.DoubleClick += OnLocalItemDoubleClick;

            pnlLocal.Controls.Add(_lvLocal);
            pnlLocal.Controls.Add(pnlLocalNav);
            pnlLocal.Controls.Add(lblLocalHeader);
            tlp.Controls.Add(pnlLocal, 0, 0);

            // 2. CENTER BUTTONS PANE
            Panel pnlCenter = new Panel { Dock = DockStyle.Fill, BackColor = ModernTheme.BackgroundDark };
            _btnUpload = new Button
            {
                Text = "👉",
                Size = new Size(50, 42),
                Location = new Point(10, 220),
                Font = new Font("Segoe UI", 14f, FontStyle.Bold)
            };
            ModernTheme.ApplyToButton(_btnUpload, ModernTheme.AccentGreen, Color.White);
            _btnUpload.Click += OnUploadClicked;

            _btnDownload = new Button
            {
                Text = "👈",
                Size = new Size(50, 42),
                Location = new Point(10, 280),
                Font = new Font("Segoe UI", 14f, FontStyle.Bold)
            };
            ModernTheme.ApplyToButton(_btnDownload, ModernTheme.AccentBlue, Color.White);
            _btnDownload.Click += OnDownloadClicked;

            Label lblUp = new Label { Text = "Upload", Location = new Point(10, 262), AutoSize = true, Font = ModernTheme.SmallFont, ForeColor = ModernTheme.TextSecondary };
            Label lblDown = new Label { Text = "Download", Location = new Point(4, 322), AutoSize = true, Font = ModernTheme.SmallFont, ForeColor = ModernTheme.TextSecondary };

            pnlCenter.Controls.Add(_btnUpload);
            pnlCenter.Controls.Add(lblUp);
            pnlCenter.Controls.Add(_btnDownload);
            pnlCenter.Controls.Add(lblDown);
            tlp.Controls.Add(pnlCenter, 1, 0);

            // 3. REMOTE PANE
            Panel pnlRemote = new Panel { Dock = DockStyle.Fill, BackColor = ModernTheme.PanelDark, Padding = new Padding(8) };
            Label lblRemoteHeader = new Label
            {
                Text = string.Format("🖥️ MÁY TRẠM: {0} ({1})", _agent.Hostname, _agent.IpAddress),
                Dock = DockStyle.Top,
                Height = 26,
                Font = ModernTheme.SubHeaderFont,
                ForeColor = ModernTheme.AccentGreen
            };

            Panel pnlRemoteNav = new Panel { Dock = DockStyle.Top, Height = 34, Padding = new Padding(0, 2, 0, 4) };
            _cboRemoteDrives = new ComboBox
            {
                Dock = DockStyle.Left,
                Width = 90,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = ModernTheme.CardDark,
                ForeColor = ModernTheme.TextPrimary,
                Font = ModernTheme.RegularFont
            };
            _cboRemoteDrives.SelectedIndexChanged += OnRemoteDriveChanged;

            _btnRemoteUp = new Button { Text = "⬆️", Dock = DockStyle.Right, Width = 38 };
            ModernTheme.ApplyToButton(_btnRemoteUp, ModernTheme.CardDark, ModernTheme.TextPrimary);
            _btnRemoteUp.Click += (s, e) => NavigateRemoteUp();

            _btnRemoteRefresh = new Button { Text = "🔄", Dock = DockStyle.Right, Width = 38 };
            ModernTheme.ApplyToButton(_btnRemoteRefresh, ModernTheme.CardDark, ModernTheme.AccentGreen);
            _btnRemoteRefresh.Click += (s, e) => RequestRemoteDirectory(_txtRemotePath.Text);

            _txtRemotePath = new TextBox
            {
                Dock = DockStyle.Fill,
                BackColor = ModernTheme.CardDark,
                ForeColor = ModernTheme.TextPrimary,
                Font = ModernTheme.RegularFont
            };
            _txtRemotePath.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) RequestRemoteDirectory(_txtRemotePath.Text); };

            pnlRemoteNav.Controls.Add(_txtRemotePath);
            pnlRemoteNav.Controls.Add(_cboRemoteDrives);
            pnlRemoteNav.Controls.Add(_btnRemoteUp);
            pnlRemoteNav.Controls.Add(_btnRemoteRefresh);

            _lvRemote = CreateFileListView();
            _lvRemote.DoubleClick += OnRemoteItemDoubleClick;

            // Setup Context Menu for Remote
            _remoteMenu = new ContextMenuStrip();
            _remoteMenu.Items.Add("👈 Tải Về Máy Quản Trị (Download)", null, (s, e) => OnDownloadClicked(s, e));
            _remoteMenu.Items.Add(new ToolStripSeparator());
            _remoteMenu.Items.Add("▶️ Chạy / Mở Tệp Từ Xa (Execute)", null, OnRemoteExecuteClicked);
            _remoteMenu.Items.Add(new ToolStripSeparator());
            _remoteMenu.Items.Add("📁 Tạo Thư Mục Mới (New Folder)...", null, OnRemoteNewFolderClicked);
            _remoteMenu.Items.Add("🗑️ Xóa Tệp / Thư Mục (Delete)", null, OnRemoteDeleteClicked);
            _remoteMenu.Items.Add(new ToolStripSeparator());
            _remoteMenu.Items.Add("🔄 Làm Mới (Refresh)", null, (s, e) => RequestRemoteDirectory(_txtRemotePath.Text));
            _lvRemote.ContextMenuStrip = _remoteMenu;

            pnlRemote.Controls.Add(_lvRemote);
            pnlRemote.Controls.Add(pnlRemoteNav);
            pnlRemote.Controls.Add(lblRemoteHeader);
            tlp.Controls.Add(pnlRemote, 2, 0);

            // Bottom Status Strip
            _statusStrip = new StatusStrip { BackColor = ModernTheme.PanelDark, ForeColor = ModernTheme.TextSecondary };
            _lblStatus = new ToolStripStatusLabel { Text = "Sẵn sàng duyệt và truyền tệp.", Spring = true, TextAlign = ContentAlignment.MiddleLeft };
            _pbProgress = new ToolStripProgressBar { Width = 160, Visible = false };
            _statusStrip.Items.Add(_lblStatus);
            _statusStrip.Items.Add(_pbProgress);

            this.Controls.Add(tlp);
            this.Controls.Add(_statusStrip);

            // Network Events
            this.Load += (s, e) =>
            {
                InitLocalDrives();
                _client.DirectoryListReceived += OnRemoteDirectoryListReceived;
                _client.FileTransferProgressReceived += OnFileTransferProgressReceived;
                _client.FileDownloadChunkReceived += OnFileDownloadChunkReceived;

                // Load initial remote Desktop or C:\
                RequestRemoteDirectory("%Desktop%");
            };

            this.FormClosing += (s, e) =>
            {
                _client.DirectoryListReceived -= OnRemoteDirectoryListReceived;
                _client.FileTransferProgressReceived -= OnFileTransferProgressReceived;
                _client.FileDownloadChunkReceived -= OnFileDownloadChunkReceived;
            };
        }

        private ListView CreateFileListView()
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
            lv.Columns.Add("Tên Tập Tin / Thư Mục", 220);
            lv.Columns.Add("Dung Lượng", 90, HorizontalAlignment.Right);
            lv.Columns.Add("Ngày Sửa Đổi", 130);
            return lv;
        }

        #region Local File Operations

        private void InitLocalDrives()
        {
            _cboLocalDrives.Items.Clear();
            foreach (DriveInfo d in DriveInfo.GetDrives())
            {
                if (d.IsReady)
                {
                    _cboLocalDrives.Items.Add(d.Name);
                }
            }
            if (_cboLocalDrives.Items.Count > 0)
            {
                _cboLocalDrives.SelectedIndex = 0;
            }
            else
            {
                LoadLocalDirectory(@"C:\");
            }
        }

        private void OnLocalDriveChanged(object sender, EventArgs e)
        {
            if (_cboLocalDrives.SelectedItem != null)
            {
                LoadLocalDirectory(_cboLocalDrives.SelectedItem.ToString());
            }
        }

        private void NavigateLocalUp()
        {
            try
            {
                DirectoryInfo di = new DirectoryInfo(_txtLocalPath.Text);
                if (di.Parent != null)
                {
                    LoadLocalDirectory(di.Parent.FullName);
                }
            }
            catch { }
        }

        private void LoadLocalDirectory(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                {
                    path = @"C:\";
                }

                DirectoryInfo di = new DirectoryInfo(path);
                _txtLocalPath.Text = di.FullName;

                _lvLocal.BeginUpdate();
                _lvLocal.Items.Clear();

                foreach (DirectoryInfo sub in di.GetDirectories())
                {
                    try
                    {
                        ListViewItem lvi = new ListViewItem("📁 " + sub.Name);
                        lvi.SubItems.Add("<Thư mục>");
                        lvi.SubItems.Add(sub.LastWriteTime.ToString("dd/MM/yyyy HH:mm"));
                        lvi.Tag = sub.FullName;
                        _lvLocal.Items.Add(lvi);
                    }
                    catch { }
                }

                foreach (FileInfo f in di.GetFiles())
                {
                    try
                    {
                        ListViewItem lvi = new ListViewItem("📄 " + f.Name);
                        lvi.SubItems.Add(FormatSize(f.Length));
                        lvi.SubItems.Add(f.LastWriteTime.ToString("dd/MM/yyyy HH:mm"));
                        lvi.Tag = f.FullName;
                        _lvLocal.Items.Add(lvi);
                    }
                    catch { }
                }

                _lvLocal.EndUpdate();
                _lblStatus.Text = string.Format("Local: {0} mục", _lvLocal.Items.Count);
            }
            catch (Exception ex)
            {
                _lblStatus.Text = "Lỗi duyệt thư mục cục bộ: " + ex.Message;
            }
        }

        private void OnLocalItemDoubleClick(object sender, EventArgs e)
        {
            if (_lvLocal.SelectedItems.Count == 0) return;
            string fullPath = _lvLocal.SelectedItems[0].Tag as string;
            if (Directory.Exists(fullPath))
            {
                LoadLocalDirectory(fullPath);
            }
        }

        #endregion

        #region Remote File Operations

        private void RequestRemoteDirectory(string path)
        {
            _lblStatus.Text = "Đang lấy danh sách tệp từ máy trạm: " + path;
            _client.RequestRemoteDirectory(_agent.AgentId, path);
        }

        private void OnRemoteDriveChanged(object sender, EventArgs e)
        {
            if (_cboRemoteDrives.SelectedItem != null)
            {
                string driveStr = _cboRemoteDrives.SelectedItem.ToString();
                int idx = driveStr.IndexOf(' ');
                string driveLetter = idx > 0 ? driveStr.Substring(0, idx) : driveStr;
                RequestRemoteDirectory(driveLetter);
            }
        }

        private void NavigateRemoteUp()
        {
            string current = _txtRemotePath.Text.Trim();
            if (string.IsNullOrEmpty(current)) return;

            int lastSlash = current.TrimEnd('\\').LastIndexOf('\\');
            if (lastSlash > 0)
            {
                string parent = current.Substring(0, lastSlash + 1);
                RequestRemoteDirectory(parent);
            }
            else if (lastSlash == 2 && current.Length > 3) // C:\foo -> C:\
            {
                RequestRemoteDirectory(current.Substring(0, 3));
            }
        }

        private void OnRemoteItemDoubleClick(object sender, EventArgs e)
        {
            if (_lvRemote.SelectedItems.Count == 0) return;
            RemoteFileSystemItem item = _lvRemote.SelectedItems[0].Tag as RemoteFileSystemItem;
            if (item != null && item.IsDirectory)
            {
                RequestRemoteDirectory(item.FullPath);
            }
        }

        private void OnRemoteDirectoryListReceived(RemoteDirectoryListResponse resp)
        {
            if (resp == null || resp.AgentId != _agent.AgentId) return;

            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action<RemoteDirectoryListResponse>(OnRemoteDirectoryListReceived), resp);
                return;
            }

            if (!string.IsNullOrEmpty(resp.ErrorMessage))
            {
                _lblStatus.Text = "Lỗi máy trạm: " + resp.ErrorMessage;
                MessageBox.Show(resp.ErrorMessage, "Lỗi Máy Trạm", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _txtRemotePath.Text = resp.CurrentPath;

            // Populate Drives dropdown if not populated or changed
            if (resp.Drives != null && resp.Drives.Count > 0 && _cboRemoteDrives.Items.Count != resp.Drives.Count)
            {
                _cboRemoteDrives.Items.Clear();
                foreach (string d in resp.Drives)
                {
                    _cboRemoteDrives.Items.Add(d);
                }
                if (_cboRemoteDrives.Items.Count > 0)
                {
                    _cboRemoteDrives.SelectedIndex = 0;
                }
            }

            _lvRemote.BeginUpdate();
            _lvRemote.Items.Clear();

            foreach (var item in resp.Items)
            {
                string icon = item.IsDirectory ? "📁 " : "📄 ";
                ListViewItem lvi = new ListViewItem(icon + item.Name);
                lvi.SubItems.Add(item.IsDirectory ? "<Thư mục>" : FormatSize(item.Size));
                lvi.SubItems.Add(item.LastModified);
                lvi.Tag = item;

                if (item.IsDirectory) lvi.ForeColor = ModernTheme.AccentCyan;
                _lvRemote.Items.Add(lvi);
            }

            _lvRemote.EndUpdate();
            _lblStatus.Text = string.Format("Máy trạm [{0}]: {1} mục trong {2}", _agent.Hostname, resp.Items.Count, resp.CurrentPath);
        }

        private void OnRemoteExecuteClicked(object sender, EventArgs e)
        {
            if (_lvRemote.SelectedItems.Count == 0) return;
            RemoteFileSystemItem item = _lvRemote.SelectedItems[0].Tag as RemoteFileSystemItem;
            if (item == null || item.IsDirectory) return;

            if (MessageBox.Show(string.Format("Bạn có muốn khởi chạy tệp '{0}' trên máy trạm {1}?", item.Name, _agent.Hostname),
                "Xác Nhận Khởi Chạy", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _client.SendFileTransferAction(_agent.AgentId, FileTransferAction.ExecuteFile, item.FullPath);
            }
        }

        private void OnRemoteNewFolderClicked(object sender, EventArgs e)
        {
            string folderName = PromptInput("Nhập tên thư mục mới trên máy trạm:", "Tạo Thư Mục Mới", "NewFolder");
            if (!string.IsNullOrEmpty(folderName))
            {
                string newPath = Path.Combine(_txtRemotePath.Text.Trim(), folderName.Trim());
                _client.SendFileTransferAction(_agent.AgentId, FileTransferAction.CreateDirectory, newPath);
                Thread.Sleep(200);
                RequestRemoteDirectory(_txtRemotePath.Text);
            }
        }

        private string PromptInput(string text, string caption, string defaultValue)
        {
            using (Form prompt = new Form())
            {
                prompt.Width = 380;
                prompt.Height = 170;
                prompt.FormBorderStyle = FormBorderStyle.FixedDialog;
                prompt.Text = caption;
                prompt.StartPosition = FormStartPosition.CenterParent;
                prompt.BackColor = ModernTheme.BackgroundDark;
                prompt.ForeColor = ModernTheme.TextPrimary;

                Label textLabel = new Label() { Left = 20, Top = 20, Text = text, AutoSize = true, Font = ModernTheme.RegularFont };
                TextBox textBox = new TextBox() { Left = 20, Top = 48, Width = 320, Text = defaultValue, Font = ModernTheme.RegularFont, BackColor = ModernTheme.CardDark, ForeColor = ModernTheme.TextPrimary };
                Button confirmation = new Button() { Text = "Đồng ý", Left = 170, Width = 80, Top = 85, DialogResult = DialogResult.OK };
                Button cancel = new Button() { Text = "Hủy", Left = 260, Width = 80, Top = 85, DialogResult = DialogResult.Cancel };
                ModernTheme.ApplyToButton(confirmation, ModernTheme.AccentGreen, Color.White);
                ModernTheme.ApplyToButton(cancel, ModernTheme.CardDark, ModernTheme.TextSecondary);

                prompt.Controls.Add(textLabel);
                prompt.Controls.Add(textBox);
                prompt.Controls.Add(confirmation);
                prompt.Controls.Add(cancel);
                prompt.AcceptButton = confirmation;
                prompt.CancelButton = cancel;

                return prompt.ShowDialog(this) == DialogResult.OK ? textBox.Text.Trim() : "";
            }
        }

        private void OnRemoteDeleteClicked(object sender, EventArgs e)
        {
            if (_lvRemote.SelectedItems.Count == 0) return;
            RemoteFileSystemItem item = _lvRemote.SelectedItems[0].Tag as RemoteFileSystemItem;
            if (item == null) return;

            if (MessageBox.Show(string.Format("Bạn có chắc muốn XÓA {0} '{1}' trên máy trạm {2}?", item.IsDirectory ? "thư mục" : "tập tin", item.Name, _agent.Hostname),
                "Xác Nhận Xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                _client.SendFileTransferAction(_agent.AgentId, FileTransferAction.DeleteFile, item.FullPath);
                Thread.Sleep(200);
                RequestRemoteDirectory(_txtRemotePath.Text);
            }
        }

        #endregion

        #region Upload and Download Execution

        private void OnUploadClicked(object sender, EventArgs e)
        {
            if (_lvLocal.SelectedItems.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn tệp tin trên máy quản trị (khung bên trái) để tải lên!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string remoteDir = _txtRemotePath.Text.Trim();
            if (string.IsNullOrEmpty(remoteDir))
            {
                MessageBox.Show("Chưa xác định được thư mục đích trên máy trạm!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            List<string> files = new List<string>();
            foreach (ListViewItem item in _lvLocal.SelectedItems)
            {
                string p = item.Tag as string;
                if (File.Exists(p)) files.Add(p);
            }

            if (files.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn tệp tin (không hỗ trợ tải trực tiếp thư mục bằng nút này)!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    int chunkSize = 256 * 1024;
                    foreach (string filePath in files)
                    {
                        FileInfo fi = new FileInfo(filePath);
                        long fileSize = fi.Length;
                        int totalChunks = fileSize == 0 ? 1 : (int)Math.Ceiling((double)fileSize / chunkSize);
                        string transferId = Guid.NewGuid().ToString("N");

                        this.BeginInvoke(new Action(() =>
                        {
                            _lblStatus.Text = string.Format("Đang tải lên máy trạm: {0} ({1})...", fi.Name, FormatSize(fileSize));
                            _pbProgress.Visible = true;
                            _pbProgress.Value = 0;
                        }));

                        FileTransferRequest req = new FileTransferRequest
                        {
                            TransferId = transferId,
                            AgentId = _agent.AgentId,
                            Action = FileTransferAction.UploadToAgent,
                            RemotePath = remoteDir,
                            FileName = fi.Name,
                            FileSize = fileSize,
                            Overwrite = true
                        };
                        _client.SendFileTransferRequest(req);
                        Thread.Sleep(50);

                        using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            byte[] buffer = new byte[chunkSize];
                            long offset = 0;

                            for (int i = 0; i < totalChunks; i++)
                            {
                                int read = fs.Read(buffer, 0, chunkSize);
                                bool isLast = (i == totalChunks - 1) || read <= 0;

                                byte[] chunkData = new byte[read];
                                if (read > 0)
                                {
                                    Buffer.BlockCopy(buffer, 0, chunkData, 0, read);
                                }

                                FileChunkMeta meta = new FileChunkMeta
                                {
                                    TransferId = transferId,
                                    AgentId = _agent.AgentId,
                                    ChunkIndex = i,
                                    TotalChunks = totalChunks,
                                    Offset = offset,
                                    Length = read,
                                    IsLastChunk = isLast
                                };
                                _client.SendFileChunk(meta, chunkData);

                                offset += read;
                                int pct = fileSize > 0 ? (int)((offset * 100) / fileSize) : 100;

                                if (i % 4 == 0 || isLast)
                                {
                                    this.BeginInvoke(new Action(() =>
                                    {
                                        _pbProgress.Value = pct > 100 ? 100 : pct;
                                    }));
                                }

                                if (isLast) break;
                                Thread.Sleep(8);
                            }
                        }

                        Thread.Sleep(100);
                    }

                    this.BeginInvoke(new Action(() =>
                    {
                        _pbProgress.Visible = false;
                        _lblStatus.Text = "Đã tải lên tệp tin thành công!";
                        RequestRemoteDirectory(remoteDir);
                    }));
                }
                catch (Exception ex)
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        _pbProgress.Visible = false;
                        _lblStatus.Text = "Lỗi khi tải lên: " + ex.Message;
                    }));
                }
            });
        }

        private void OnDownloadClicked(object sender, EventArgs e)
        {
            if (_lvRemote.SelectedItems.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn tệp tin trên máy trạm (khung bên phải) để tải về!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string localDir = _txtLocalPath.Text.Trim();
            if (string.IsNullOrEmpty(localDir) || !Directory.Exists(localDir))
            {
                MessageBox.Show("Thư mục lưu trữ cục bộ không hợp lệ!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            foreach (ListViewItem item in _lvRemote.SelectedItems)
            {
                RemoteFileSystemItem rItem = item.Tag as RemoteFileSystemItem;
                if (rItem == null || rItem.IsDirectory) continue;

                string transferId = Guid.NewGuid().ToString("N");
                string finalPath = Path.Combine(localDir, rItem.Name);
                string partPath = finalPath + "." + transferId + ".part";

                lock (_dlLock)
                {
                    if (File.Exists(partPath)) try { File.Delete(partPath); } catch { }
                    FileStream fs = new FileStream(partPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                    _activeDownloads[transferId] = new DownloadSession
                    {
                        TransferId = transferId,
                        LocalFinalPath = finalPath,
                        LocalPartPath = partPath,
                        Stream = fs
                    };
                }

                _lblStatus.Text = string.Format("Đang yêu cầu máy trạm gửi tệp: {0}...", rItem.Name);
                _pbProgress.Visible = true;
                _pbProgress.Value = 0;

                FileTransferRequest req = new FileTransferRequest
                {
                    TransferId = transferId,
                    AgentId = _agent.AgentId,
                    Action = FileTransferAction.DownloadFromAgent,
                    RemotePath = rItem.FullPath,
                    FileName = rItem.Name,
                    FileSize = rItem.Size
                };
                _client.SendFileTransferRequest(req);
            }
        }

        private void OnFileDownloadChunkReceived(FileChunkMeta meta, byte[] data)
        {
            if (meta == null || meta.AgentId != _agent.AgentId) return;

            DownloadSession session = null;
            lock (_dlLock)
            {
                _activeDownloads.TryGetValue(meta.TransferId, out session);
            }

            if (session == null) return;

            try
            {
                if (data != null && data.Length > 0)
                {
                    session.Stream.Write(data, 0, data.Length);
                }

                if (this.InvokeRequired)
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        int pct = meta.TotalChunks > 0 ? (int)(((meta.ChunkIndex + 1) * 100) / meta.TotalChunks) : 100;
                        _pbProgress.Value = pct > 100 ? 100 : pct;
                    }));
                }

                if (meta.IsLastChunk)
                {
                    lock (_dlLock)
                    {
                        try { session.Stream.Flush(); } catch { }
                        try { session.Stream.Dispose(); } catch { }
                        _activeDownloads.Remove(meta.TransferId);
                    }

                    if (File.Exists(session.LocalFinalPath))
                    {
                        try { File.Delete(session.LocalFinalPath); } catch { }
                    }

                    File.Move(session.LocalPartPath, session.LocalFinalPath);

                    if (this.InvokeRequired)
                    {
                        this.BeginInvoke(new Action(() =>
                        {
                            _pbProgress.Visible = false;
                            _lblStatus.Text = "Tải về tệp thành công: " + Path.GetFileName(session.LocalFinalPath);
                            LoadLocalDirectory(_txtLocalPath.Text);
                        }));
                    }
                }
            }
            catch (Exception ex)
            {
                lock (_dlLock)
                {
                    try { session.Stream.Dispose(); } catch { }
                    _activeDownloads.Remove(meta.TransferId);
                }
                if (this.InvokeRequired)
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        _pbProgress.Visible = false;
                        _lblStatus.Text = "Lỗi ghi tệp tải về: " + ex.Message;
                    }));
                }
            }
        }

        private void OnFileTransferProgressReceived(FileTransferProgressResponse resp)
        {
            if (resp == null || resp.AgentId != _agent.AgentId) return;

            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action<FileTransferProgressResponse>(OnFileTransferProgressReceived), resp);
                return;
            }

            if (!resp.Success)
            {
                _lblStatus.Text = "Lỗi: " + resp.ErrorMessage;
                MessageBox.Show(resp.ErrorMessage, "Lỗi Truyền Tệp", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else if (resp.Percentage >= 100)
            {
                _lblStatus.Text = "Hoàn thành: " + resp.FileName;
            }
            else
            {
                _lblStatus.Text = string.Format("Tiến trình: {0} ({1}%)", resp.FileName, resp.Percentage);
            }
        }

        #endregion

        private string FormatSize(long bytes)
        {
            if (bytes < 1024) return bytes + " B";
            if (bytes < 1024 * 1024) return (bytes / 1024.0).ToString("N1") + " KB";
            if (bytes < 1024 * 1024 * 1024) return (bytes / (1024.0 * 1024)).ToString("N1") + " MB";
            return (bytes / (1024.0 * 1024 * 1024)).ToString("N2") + " GB";
        }
    }
}
