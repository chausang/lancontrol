using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using LanControl.Core.Models;

namespace LanControl.Control.UI
{
    public class RemoteDesktopForm : Form
    {
        private readonly ControlClient _client;
        private readonly AgentInfo _agent;
        private PictureBox _picDesktop;
        private FlowLayoutPanel _toolbar;
        private Label _lblInfo;
        private Label _lblFps;
        private Button _btnToggleInput;
        private Button _btnFileTransfer;
        private Button _btnFullScreen;
        private Button _btnLock;
        private Button _btnUnlock;
        private Button _btnMsg;
        private Button _btnWallpaper;
        private Button _btnFixUac;
        private Button _btnUacYes;
        private Button _btnUacNo;
        private Button _btnSendPass;
        private bool _hideWallpaper = false;
        private bool _allowInput = true;
        private bool _isFullScreen = false;
        private FormWindowState _prevWindowState;
        private FormBorderStyle _prevBorderStyle;

        private int _remoteWidth = 1920;
        private int _remoteHeight = 1080;
        private int _frameCount = 0;
        private DateTime _lastFpsTime = DateTime.Now;

        private readonly object _frameLock = new object();
        private byte[] _latestRawBytes = null;
        private ScreenFrameMeta _latestMeta = null;
        private bool _renderPending = false;

        private bool _isLeftMouseDown = false;
        private bool _isRightMouseDown = false;
        private DateTime _lastMouseMoveTime = DateTime.MinValue;
        private Point _lastSentMousePos = new Point(-1, -1);

        public AgentInfo Agent { get { return _agent; } }

        public RemoteDesktopForm(ControlClient client, AgentInfo agent)
        {
            _client = client;
            _agent = agent;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = string.Format("LAN Control Remote Desktop - [{0}] {1} ({2})", _agent.VlanTag, _agent.Hostname, _agent.IpAddress);
            this.Size = new Size(1560, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = ModernTheme.BackgroundDark;
            this.DoubleBuffered = true;

            // Top Toolbar
            _toolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 42,
                BackColor = ModernTheme.PanelDark,
                Padding = new Padding(8, 6, 8, 6),
                AutoScroll = true,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight
            };

            _lblInfo = new Label
            {
                Text = string.Format("🖥️ {0} | IP: {1} | {2}", _agent.Hostname, _agent.IpAddress, _agent.VlanTag),
                ForeColor = ModernTheme.TextPrimary,
                Font = ModernTheme.SubHeaderFont,
                AutoSize = true,
                Margin = new Padding(0, 5, 8, 0)
            };

            _lblFps = new Label
            {
                Text = "FPS: -- | Live Stream",
                ForeColor = ModernTheme.AccentGreen,
                Font = ModernTheme.RegularFont,
                AutoSize = true,
                Margin = new Padding(0, 6, 12, 0)
            };

            _btnToggleInput = new Button
            {
                Text = "🎮 Điều Khiển",
                Size = new Size(120, 28),
                Margin = new Padding(0, 1, 5, 0)
            };
            ModernTheme.ApplyToButton(_btnToggleInput, ModernTheme.AccentBlue, Color.White);
            _btnToggleInput.Click += (s, e) =>
            {
                _allowInput = !_allowInput;
                _btnToggleInput.Text = _allowInput ? "🎮 Điều Khiển" : "👁️ Chỉ Xem";
                _btnToggleInput.BackColor = _allowInput ? ModernTheme.AccentBlue : ModernTheme.CardDark;
            };

            _btnFileTransfer = new Button
            {
                Text = "📁 Truyền Tệp ▾",
                Size = new Size(130, 28),
                Margin = new Padding(0, 1, 5, 0)
            };
            ModernTheme.ApplyToButton(_btnFileTransfer, ModernTheme.CardDark, ModernTheme.AccentCyan);

            ContextMenuStrip fileMenu = new ContextMenuStrip();
            fileMenu.BackColor = ModernTheme.PanelDark;
            fileMenu.ForeColor = ModernTheme.TextPrimary;
            fileMenu.Font = ModernTheme.RegularFont;
            fileMenu.ShowImageMargin = false;

            ToolStripMenuItem miQuick = new ToolStripMenuItem("🚀 Truyền Tập Tin Nhanh (Desktop / Downloads / Tự Chạy)...");
            miQuick.Font = new Font(ModernTheme.RegularFont, FontStyle.Bold);
            miQuick.ForeColor = ModernTheme.AccentCyan;
            miQuick.Click += (s, e) =>
            {
                FileDistributionForm form = new FileDistributionForm(_client, new List<AgentInfo> { _agent });
                form.Show(this);
            };

            ToolStripMenuItem miFastDesktop = new ToolStripMenuItem("⚡ Chọn Tệp Gửi Ngay Ra Desktop (Direct Desktop Send)...");
            miFastDesktop.Click += (s, e) =>
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Title = string.Format("Chọn tệp để gửi ngay đến Desktop máy {0}", _agent.Hostname);
                    ofd.Multiselect = true;
                    if (ofd.ShowDialog(this) == DialogResult.OK && ofd.FileNames.Length > 0)
                    {
                        FileDistributionForm form = new FileDistributionForm(_client, new List<AgentInfo> { _agent }, new List<string>(ofd.FileNames));
                        form.Show(this);
                    }
                }
            };

            ToolStripMenuItem miExplorer = new ToolStripMenuItem("📂 Quản Lý & Duyệt Tệp Từ Xa 2 Chiều (Remote File Explorer)...");
            miExplorer.Click += (s, e) =>
            {
                RemoteFileManagerForm form = new RemoteFileManagerForm(_client, _agent);
                form.Show(this);
            };

            fileMenu.Items.Add(miQuick);
            fileMenu.Items.Add(miFastDesktop);
            fileMenu.Items.Add(new ToolStripSeparator());
            fileMenu.Items.Add(miExplorer);

            _btnFileTransfer.Click += (s, e) =>
            {
                fileMenu.Show(_btnFileTransfer, new Point(0, _btnFileTransfer.Height));
            };
            _btnFileTransfer.ContextMenuStrip = fileMenu;

            _btnMsg = new Button
            {
                Text = "💬 Nhắn Tin",
                Size = new Size(100, 28),
                Margin = new Padding(0, 1, 5, 0)
            };
            ModernTheme.ApplyToButton(_btnMsg, ModernTheme.CardDark, ModernTheme.TextPrimary);
            _btnMsg.Click += (s, e) =>
            {
                using (SendMessageForm f = new SendMessageForm(_client, _agent))
                {
                    f.ShowDialog(this);
                }
            };

            _btnLock = new Button
            {
                Text = "🔒 Khóa",
                Size = new Size(75, 28),
                Margin = new Padding(0, 1, 5, 0)
            };
            ModernTheme.ApplyToButton(_btnLock, ModernTheme.AccentRed, Color.White);
            _btnLock.Click += (s, e) =>
            {
                if (MessageBox.Show("Bạn có chắc muốn KHÓA màn hình máy trạm này?", "Khóa Màn Hình", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    _client.SendCommand(_agent.AgentId, CommandType.LockScreen, "Máy tính đang được quản trị viên khóa.");
                }
            };

            _btnUnlock = new Button
            {
                Text = "🔓 Mở",
                Size = new Size(70, 28),
                Margin = new Padding(0, 1, 5, 0)
            };
            ModernTheme.ApplyToButton(_btnUnlock, ModernTheme.AccentGreen, Color.White);
            _btnUnlock.Click += (s, e) =>
            {
                _client.SendCommand(_agent.AgentId, CommandType.UnlockScreen);
            };

            _btnWallpaper = new Button
            {
                Text = "🖼️ Ẩn Nền",
                Size = new Size(95, 28),
                Margin = new Padding(0, 1, 5, 0)
            };
            ModernTheme.ApplyToButton(_btnWallpaper, ModernTheme.CardDark, ModernTheme.TextPrimary);
            _btnWallpaper.Click += (s, e) =>
            {
                _hideWallpaper = !_hideWallpaper;
                if (_hideWallpaper)
                {
                    _client.SendCommand(_agent.AgentId, CommandType.HideWallpaper);
                    _btnWallpaper.Text = "🖼️ Hiện Nền";
                    _btnWallpaper.BackColor = ModernTheme.AccentCyan;
                    _btnWallpaper.ForeColor = Color.Black;
                }
                else
                {
                    _client.SendCommand(_agent.AgentId, CommandType.RestoreWallpaper);
                    _btnWallpaper.Text = "🖼️ Ẩn Nền";
                    _btnWallpaper.BackColor = ModernTheme.CardDark;
                    _btnWallpaper.ForeColor = ModernTheme.TextPrimary;
                }
            };

            _btnFixUac = new Button
            {
                Text = "🛡️ Hiện UAC & MK",
                Size = new Size(130, 28),
                Margin = new Padding(0, 1, 5, 0)
            };
            ModernTheme.ApplyToButton(_btnFixUac, ModernTheme.CardDark, ModernTheme.AccentYellow);
            _btnFixUac.Click += (s, e) =>
            {
                _client.SendCommand(_agent.AgentId, CommandType.EnableUacBypass);
                MessageBox.Show(
                    "Đã kích hoạt cấu hình Hiện UAC & Mở Khung Mật Khẩu Admin trên máy trạm!\n\n" +
                    "• Đưa UAC về Desktop (PromptOnSecureDesktop = 0): Hộp thoại UAC và ô nhập mật khẩu Admin sẽ hiển thị trực tiếp trên màn hình điều khiển.\n" +
                    "• Mở Chặn Chuột Phím UIPI (EnableUIPI = 0): Cho phép chuột phím từ xa click và gõ trực tiếp vào ô mật khẩu Admin.\n" +
                    "• Tự Động Nâng Quyền (ConsentPromptBehaviorAdmin = 0): Cho phép ứng dụng quản trị tự nâng quyền.\n" +
                    "• Xử lý UAC treo: Đã tự động đóng các hộp thoại UAC cũ bị kẹt trên màn hình bảo mật (nếu có).\n\n" +
                    "💡 Lúc này, bạn có thể click mở lại chương trình trên máy trạm. Khung nhập mật khẩu Admin sẽ hiển thị đầy đủ ngay trên màn hình để bạn nhập mật khẩu!",
                    "Đã Kích Hoạt Hiển Thị UAC & Mật Khẩu Admin", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            _btnSendPass = new Button
            {
                Text = "🔑 Nhập MK Admin",
                Size = new Size(130, 28),
                Margin = new Padding(0, 1, 5, 0)
            };
            ModernTheme.ApplyToButton(_btnSendPass, ModernTheme.CardDark, ModernTheme.AccentCyan);
            _btnSendPass.Click += (s, e) => ShowSendAdminPasswordDialog();

            _btnUacYes = new Button
            {
                Text = "✅ Đồng Ý",
                Size = new Size(80, 28),
                Margin = new Padding(0, 1, 5, 0)
            };
            ModernTheme.ApplyToButton(_btnUacYes, ModernTheme.CardDark, ModernTheme.AccentGreen);
            _btnUacYes.Click += (s, e) =>
            {
                _client.SendCommand(_agent.AgentId, CommandType.UacYes);
            };

            _btnUacNo = new Button
            {
                Text = "❌ Hủy UAC",
                Size = new Size(85, 28),
                Margin = new Padding(0, 1, 5, 0)
            };
            ModernTheme.ApplyToButton(_btnUacNo, ModernTheme.CardDark, ModernTheme.AccentRed);
            _btnUacNo.Click += (s, e) =>
            {
                _client.SendCommand(_agent.AgentId, CommandType.UacNo);
            };

            _btnFullScreen = new Button
            {
                Text = "⛶ Toàn Màn",
                Size = new Size(100, 28),
                Margin = new Padding(0, 1, 5, 0)
            };
            ModernTheme.ApplyToButton(_btnFullScreen, ModernTheme.CardDark, ModernTheme.TextPrimary);
            _btnFullScreen.Click += (s, e) => ToggleFullScreen();

            // Tooltips
            ToolTip tt = new ToolTip();
            tt.SetToolTip(_btnToggleInput, "Bật / Tắt gửi chuột và bàn phím đến máy trạm");
            tt.SetToolTip(_btnFileTransfer, "Truyền tập tin nhanh đến máy trạm hoặc quản lý tệp tin từ xa (Kéo thả tệp vào màn hình để gửi nhanh)");
            tt.SetToolTip(_btnMsg, "Gửi tin nhắn hoặc thông báo toàn màn hình đến máy trạm");
            tt.SetToolTip(_btnLock, "Khóa màn hình máy trạm ngay lập tức");
            tt.SetToolTip(_btnUnlock, "Mở khóa màn hình máy trạm");
            tt.SetToolTip(_btnWallpaper, "Ẩn hình nền máy trạm để tăng tốc độ truyền hình ảnh");
            tt.SetToolTip(_btnFixUac, "Tự động đưa hộp thoại UAC và khung nhập mật khẩu Admin hiển thị trực tiếp trên màn hình điều khiển (Tắt Secure Desktop)");
            tt.SetToolTip(_btnSendPass, "Mở hộp thoại nhập và gửi trực tiếp mật khẩu Administrator vào ô nhập UAC trên máy trạm");
            tt.SetToolTip(_btnUacYes, "Bấm Yes chấp nhận hộp thoại UAC đang hiển thị");
            tt.SetToolTip(_btnUacNo, "Bấm No từ chối hoặc giải phóng hộp thoại UAC đang bị treo");
            tt.SetToolTip(_btnFullScreen, "Bật / Tắt chế độ toàn màn hình");

            _toolbar.Controls.Add(_lblInfo);
            _toolbar.Controls.Add(_lblFps);
            _toolbar.Controls.Add(_btnToggleInput);
            _toolbar.Controls.Add(_btnFileTransfer);
            _toolbar.Controls.Add(_btnMsg);
            _toolbar.Controls.Add(_btnLock);
            _toolbar.Controls.Add(_btnUnlock);
            _toolbar.Controls.Add(_btnWallpaper);
            _toolbar.Controls.Add(_btnFixUac);
            _toolbar.Controls.Add(_btnSendPass);
            _toolbar.Controls.Add(_btnUacYes);
            _toolbar.Controls.Add(_btnUacNo);
            _toolbar.Controls.Add(_btnFullScreen);


            // PictureBox Desktop Screen
            _picDesktop = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.StretchImage,
                BackColor = Color.Black
            };

            // Hook mouse and keyboard inputs
            _picDesktop.MouseDown += OnDesktopMouseDown;
            _picDesktop.MouseUp += OnDesktopMouseUp;
            _picDesktop.MouseMove += OnDesktopMouseMove;
            _picDesktop.MouseWheel += OnDesktopMouseWheel;
            _picDesktop.DoubleClick += OnDesktopDoubleClick;
            _picDesktop.MouseCaptureChanged += (s, e) => SafeReleaseMouseButtons();
            _picDesktop.MouseLeave += (s, e) => { if (!_picDesktop.Capture) SafeReleaseMouseButtons(); };
            this.Deactivate += (s, e) => SafeReleaseMouseButtons();

            this.KeyPreview = true;
            this.KeyDown += OnDesktopKeyDown;
            this.KeyUp += OnDesktopKeyUp;

            // Drag and drop files onto remote screen for instant transfer
            this.AllowDrop = true;
            _picDesktop.AllowDrop = true;

            DragEventHandler onDragEnter = (s, e) =>
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    e.Effect = DragDropEffects.Copy;
                }
            };

            DragEventHandler onDragDrop = (s, e) =>
            {
                try
                {
                    if (e.Data.GetDataPresent(DataFormats.FileDrop))
                    {
                        string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                        if (files != null && files.Length > 0)
                        {
                            FileDistributionForm form = new FileDistributionForm(_client, new List<AgentInfo> { _agent }, new List<string>(files));
                            form.Show(this);
                        }
                    }
                }
                catch { }
            };

            this.DragEnter += onDragEnter;
            this.DragDrop += onDragDrop;
            _picDesktop.DragEnter += onDragEnter;
            _picDesktop.DragDrop += onDragDrop;

            this.Controls.Add(_picDesktop);
            this.Controls.Add(_toolbar);

            this.Load += (s, e) =>
            {
                _client.ScreenFrameReceived += OnScreenFrame;
                _client.StartScreenStream(_agent.AgentId);
            };

            this.FormClosing += (s, e) =>
            {
                SafeReleaseMouseButtons();
                if (_hideWallpaper)
                {
                    _client.SendCommand(_agent.AgentId, CommandType.RestoreWallpaper);
                }
                _client.ScreenFrameReceived -= OnScreenFrame;
                _client.StopScreenStream(_agent.AgentId);
            };
        }

        private void SafeReleaseMouseButtons()
        {
            if (!_allowInput) return;

            if (_isLeftMouseDown)
            {
                _isLeftMouseDown = false;
                Point p = _lastSentMousePos.X >= 0 ? _lastSentMousePos : new Point(0, 0);
                _client.SendInput(new InputEvent
                {
                    AgentId = _agent.AgentId,
                    IsMouse = true,
                    X = p.X,
                    Y = p.Y,
                    MouseType = MouseEventType.LeftUp,
                    LeftButtonDown = false,
                    RightButtonDown = _isRightMouseDown
                });
            }
            if (_isRightMouseDown)
            {
                _isRightMouseDown = false;
                Point p = _lastSentMousePos.X >= 0 ? _lastSentMousePos : new Point(0, 0);
                _client.SendInput(new InputEvent
                {
                    AgentId = _agent.AgentId,
                    IsMouse = true,
                    X = p.X,
                    Y = p.Y,
                    MouseType = MouseEventType.RightUp,
                    LeftButtonDown = false,
                    RightButtonDown = false
                });
            }
        }

        private void ToggleFullScreen()
        {
            if (!_isFullScreen)
            {
                _prevWindowState = this.WindowState;
                _prevBorderStyle = this.FormBorderStyle;
                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Maximized;
                _btnFullScreen.Text = "🗗 Thu Nhỏ";
                _isFullScreen = true;
            }
            else
            {
                this.FormBorderStyle = _prevBorderStyle;
                this.WindowState = _prevWindowState;
                _btnFullScreen.Text = "⛶ Toàn Màn";
                _isFullScreen = false;
            }
        }

        private void OnScreenFrame(ScreenFrameMeta meta, byte[] rawBytes)
        {
            if (meta == null || meta.AgentId != _agent.AgentId || meta.IsThumbnail) return;

            lock (_frameLock)
            {
                _latestRawBytes = rawBytes;
                _latestMeta = meta;
                if (!_renderPending)
                {
                    _renderPending = true;
                    try
                    {
                        this.BeginInvoke(new Action(RenderLatestFrame));
                    }
                    catch
                    {
                        _renderPending = false;
                    }
                }
            }
        }

        private void RenderLatestFrame()
        {
            byte[] bytesToRender;
            lock (_frameLock)
            {
                bytesToRender = _latestRawBytes;
                _renderPending = false;
            }

            if (bytesToRender == null || this.IsDisposed) return;

            try
            {
                using (MemoryStream ms = new MemoryStream(bytesToRender))
                {
                    Image newImg = Image.FromStream(ms);
                    _remoteWidth = newImg.Width;
                    _remoteHeight = newImg.Height;

                    Image oldImg = _picDesktop.Image;
                    _picDesktop.Image = newImg;
                    if (oldImg != null) oldImg.Dispose();
                }

                _frameCount++;
                if ((DateTime.Now - _lastFpsTime).TotalSeconds >= 1.0)
                {
                    _lblFps.Text = string.Format("FPS: {0} | {1}x{2}", _frameCount, _remoteWidth, _remoteHeight);
                    _frameCount = 0;
                    _lastFpsTime = DateTime.Now;
                }
            }
            catch { }
        }

        private Point ScaleCoordinates(int clientX, int clientY)
        {
            if (_picDesktop.Width <= 0 || _picDesktop.Height <= 0) return new Point(clientX, clientY);

            int x = (int)((double)clientX / _picDesktop.Width * _remoteWidth);
            int y = (int)((double)clientY / _picDesktop.Height * _remoteHeight);

            if (x < 0) x = 0;
            if (x >= _remoteWidth) x = _remoteWidth - 1;
            if (y < 0) y = 0;
            if (y >= _remoteHeight) y = _remoteHeight - 1;

            return new Point(x, y);
        }

        private void OnDesktopMouseDown(object sender, MouseEventArgs e)
        {
            if (!_allowInput) return;
            _picDesktop.Focus();
            Point p = ScaleCoordinates(e.X, e.Y);
            _lastSentMousePos = p;

            if (e.Button == MouseButtons.Left) _isLeftMouseDown = true;
            if (e.Button == MouseButtons.Right) _isRightMouseDown = true;

            InputEvent ev = new InputEvent
            {
                AgentId = _agent.AgentId,
                IsMouse = true,
                X = p.X,
                Y = p.Y,
                MouseType = (e.Button == MouseButtons.Left) ? MouseEventType.LeftDown :
                            (e.Button == MouseButtons.Right) ? MouseEventType.RightDown : MouseEventType.MiddleDown,
                LeftButtonDown = _isLeftMouseDown,
                RightButtonDown = _isRightMouseDown
            };
            _client.SendInput(ev);
        }

        private void OnDesktopMouseUp(object sender, MouseEventArgs e)
        {
            if (!_allowInput) return;
            Point p = ScaleCoordinates(e.X, e.Y);
            _lastSentMousePos = p;

            if (e.Button == MouseButtons.Left) _isLeftMouseDown = false;
            if (e.Button == MouseButtons.Right) _isRightMouseDown = false;

            InputEvent ev = new InputEvent
            {
                AgentId = _agent.AgentId,
                IsMouse = true,
                X = p.X,
                Y = p.Y,
                MouseType = (e.Button == MouseButtons.Left) ? MouseEventType.LeftUp :
                            (e.Button == MouseButtons.Right) ? MouseEventType.RightUp : MouseEventType.MiddleUp,
                LeftButtonDown = _isLeftMouseDown,
                RightButtonDown = _isRightMouseDown
            };
            _client.SendInput(ev);
        }

        private void OnDesktopMouseMove(object sender, MouseEventArgs e)
        {
            if (!_allowInput) return;
            Point p = ScaleCoordinates(e.X, e.Y);

            DateTime now = DateTime.Now;
            bool isButtonDown = (e.Button != MouseButtons.None);
            int dx = Math.Abs(p.X - _lastSentMousePos.X);
            int dy = Math.Abs(p.Y - _lastSentMousePos.Y);

            // Throttle to at most ~40 moves/sec unless dragging or significant mouse jump
            if (!isButtonDown && (now - _lastMouseMoveTime).TotalMilliseconds < 25 && (dx < 3 && dy < 3))
            {
                return;
            }

            _lastMouseMoveTime = now;
            _lastSentMousePos = p;

            InputEvent ev = new InputEvent
            {
                AgentId = _agent.AgentId,
                IsMouse = true,
                X = p.X,
                Y = p.Y,
                MouseType = MouseEventType.Move,
                LeftButtonDown = (e.Button == MouseButtons.Left),
                RightButtonDown = (e.Button == MouseButtons.Right)
            };
            _client.SendInput(ev);
        }

        private void OnDesktopDoubleClick(object sender, EventArgs e)
        {
            if (!_allowInput) return;
            Point p = _picDesktop.PointToClient(Cursor.Position);
            Point scaled = ScaleCoordinates(p.X, p.Y);
            _lastSentMousePos = scaled;

            InputEvent ev = new InputEvent
            {
                AgentId = _agent.AgentId,
                IsMouse = true,
                X = scaled.X,
                Y = scaled.Y,
                MouseType = MouseEventType.DoubleClick,
                LeftButtonDown = false,
                RightButtonDown = false
            };
            _client.SendInput(ev);
        }

        private void OnDesktopMouseWheel(object sender, MouseEventArgs e)
        {
            if (!_allowInput) return;
            Point p = ScaleCoordinates(e.X, e.Y);

            InputEvent ev = new InputEvent
            {
                AgentId = _agent.AgentId,
                IsMouse = true,
                X = p.X,
                Y = p.Y,
                Delta = e.Delta,
                MouseType = MouseEventType.Wheel
            };
            _client.SendInput(ev);
        }

        private void OnDesktopKeyDown(object sender, KeyEventArgs e)
        {
            if (!_allowInput) return;
            InputEvent ev = new InputEvent
            {
                AgentId = _agent.AgentId,
                IsMouse = false,
                KeyType = KeyEventType.KeyDown,
                KeyCode = (int)e.KeyCode
            };
            _client.SendInput(ev);
            e.Handled = true;
        }

        private void OnDesktopKeyUp(object sender, KeyEventArgs e)
        {
            if (!_allowInput) return;
            InputEvent ev = new InputEvent
            {
                AgentId = _agent.AgentId,
                IsMouse = false,
                KeyType = KeyEventType.KeyUp,
                KeyCode = (int)e.KeyCode
            };
            _client.SendInput(ev);
            e.Handled = true;
        }

        private void ShowSendAdminPasswordDialog()
        {
            using (Form dlg = new Form())
            {
                dlg.Text = "🔑 Gửi Mật Khẩu Quản Trị Viên (Admin Password)";
                dlg.Size = new Size(460, 230);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.BackColor = ModernTheme.BackgroundDark;
                dlg.ForeColor = ModernTheme.TextPrimary;

                Label lblHeader = new Label
                {
                    Text = "Nhập mật khẩu Administrator của máy trạm:",
                    Location = new Point(20, 18),
                    AutoSize = true,
                    Font = ModernTheme.SubHeaderFont,
                    ForeColor = ModernTheme.AccentCyan
                };

                Label lblHint = new Label
                {
                    Text = "Mật khẩu sẽ được truyền thẳng vào ô nhập mật khẩu UAC bằng mã phím Unicode chuẩn (tránh gõ sai ký tự đặc biệt hoặc bộ gõ tiếng Việt).",
                    Location = new Point(20, 42),
                    Size = new Size(405, 34),
                    Font = ModernTheme.SmallFont,
                    ForeColor = ModernTheme.TextSecondary
                };

                TextBox txtPass = new TextBox
                {
                    Location = new Point(22, 82),
                    Size = new Size(400, 26),
                    Font = ModernTheme.RegularFont,
                    BackColor = ModernTheme.CardDark,
                    ForeColor = ModernTheme.TextPrimary,
                    BorderStyle = BorderStyle.FixedSingle,
                    UseSystemPasswordChar = true
                };

                CheckBox chkShow = new CheckBox
                {
                    Text = "Hiện mật khẩu",
                    Location = new Point(22, 114),
                    AutoSize = true,
                    Font = ModernTheme.SmallFont,
                    ForeColor = ModernTheme.TextSecondary
                };
                chkShow.CheckedChanged += (s, e) => { txtPass.UseSystemPasswordChar = !chkShow.Checked; };

                Button btnSend = new Button
                {
                    Text = "🚀 Gửi & Xác Nhận (Enter)",
                    Location = new Point(190, 145),
                    Size = new Size(150, 32),
                    DialogResult = DialogResult.OK
                };
                ModernTheme.ApplyToButton(btnSend, ModernTheme.AccentGreen, Color.White);

                Button btnCancel = new Button
                {
                    Text = "Hủy Bỏ",
                    Location = new Point(348, 145),
                    Size = new Size(75, 32),
                    DialogResult = DialogResult.Cancel
                };
                ModernTheme.ApplyToButton(btnCancel, ModernTheme.CardDark, ModernTheme.TextPrimary);

                dlg.Controls.Add(lblHeader);
                dlg.Controls.Add(lblHint);
                dlg.Controls.Add(txtPass);
                dlg.Controls.Add(chkShow);
                dlg.Controls.Add(btnSend);
                dlg.Controls.Add(btnCancel);
                dlg.AcceptButton = btnSend;
                dlg.CancelButton = btnCancel;

                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    string pass = txtPass.Text;
                    if (!string.IsNullOrEmpty(pass))
                    {
                        _client.SendCommand(_agent.AgentId, CommandType.SendAdminPassword, pass);
                    }
                }
            }
        }
    }
}
