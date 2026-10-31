using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using LanControl.Core.Models;
using LanControl.Core.Utils;

namespace LanControl.Control.UI
{
    public class BroadcastSenderForm : Form
    {
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        private static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);

        private const int WM_NCLBUTTONDOWN = 0x00A1;
        private const int HTCAPTION = 0x0002;
        private const uint WDA_NONE = 0x00000000;
        private const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;

        private readonly ControlClient _client;
        private readonly List<AgentInfo> _allAgents;

        private Panel _topConfigPanel;
        private Panel _floatingBar;

        private ComboBox _cboTarget;
        private ComboBox _cboFps;
        private ComboBox _cboQuality;
        private CheckBox _chkTopMost;

        private RadioButton _radFullScreen;
        private RadioButton _radAppWindow;
        private ComboBox _cboAppWindow;
        private Button _btnRefreshApps;
        private CheckBox _chkBringToFront;
        private CheckBox _chkAutoFloating;
        private CheckBox _chkExcludeSelfCapture;

        private Button _btnToggleBroadcast;
        private Button _btnSwitchFloating;
        private Label _lblStatus;
        private PictureBox _picPreview;

        // Floating Mini-Bar Controls
        private Label _lblFloatingStatus;
        private Button _btnFloatingExpand;
        private Button _btnFloatingStop;
        private Button _btnFloatingPin;
        private ContextMenuStrip _floatingMenu;

        private bool _isBroadcasting = false;
        private bool _isFloatingMode = false;
        private Rectangle _savedNormalBounds = Rectangle.Empty;
        private Rectangle _savedFloatingBounds = Rectangle.Empty;

        private bool _isMouseDownForDrag = false;
        private Point _dragStartPoint = Point.Empty;

        private Thread _broadcastThread;
        private int _framesSent = 0;
        private DateTime _startTime;

        // Thread-safe capture settings read by broadcast loop
        private volatile bool _captureAppWindowMode = false;
        private IntPtr _selectedWindowHandle = IntPtr.Zero;
        private string _selectedWindowLabel = "Toàn màn hình";
        private string _activeTargetVlan = "";

        public BroadcastSenderForm(ControlClient client, List<AgentInfo> allAgents)
        {
            _client = client;
            _allAgents = allAgents ?? new List<AgentInfo>();
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "LAN Control - Trình Chiếu & Phát Sóng Màn Hình / Chương Trình (1-to-Many Broadcaster)";
            this.Size = new Size(920, 650);
            this.MinimumSize = new Size(280, 180);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = ModernTheme.BackgroundDark;
            this.ForeColor = ModernTheme.TextPrimary;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.DoubleBuffered = true;

            // =========================================================================
            // 1. COMPACT FLOATING CONTROL BAR (Shown in Floating Monitor / PiP Mode)
            // =========================================================================
            _floatingBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 38,
                BackColor = ModernTheme.PanelDark,
                Padding = new Padding(8, 5, 8, 5),
                Visible = false
            };

            _lblFloatingStatus = new Label
            {
                Text = "🟢 Đang phát: Toàn màn hình",
                Location = new Point(8, 10),
                AutoSize = false,
                Size = new Size(200, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = ModernTheme.AccentGreen,
                AutoEllipsis = true,
                Cursor = Cursors.SizeAll
            };

            _btnFloatingPin = new Button
            {
                Text = "📌",
                Size = new Size(34, 26),
                Location = new Point(215, 6),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            ModernTheme.ApplyToButton(_btnFloatingPin, ModernTheme.AccentBlue, Color.White);
            _btnFloatingPin.Click += (s, e) =>
            {
                SetTopMostState(!this.TopMost);
            };

            _btnFloatingExpand = new Button
            {
                Text = "🗖 Mở rộng",
                Size = new Size(88, 26),
                Location = new Point(253, 6),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            ModernTheme.ApplyToButton(_btnFloatingExpand, ModernTheme.CardDark, ModernTheme.AccentCyan);
            _btnFloatingExpand.Click += (s, e) => SetFloatingMode(false);

            _btnFloatingStop = new Button
            {
                Text = "🛑 Dừng",
                Size = new Size(72, 26),
                Location = new Point(345, 6),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            ModernTheme.ApplyToButton(_btnFloatingStop, ModernTheme.AccentRed, Color.White);
            _btnFloatingStop.Click += OnToggleBroadcast;

            _floatingBar.Controls.Add(_lblFloatingStatus);
            _floatingBar.Controls.Add(_btnFloatingPin);
            _floatingBar.Controls.Add(_btnFloatingExpand);
            _floatingBar.Controls.Add(_btnFloatingStop);
            _floatingBar.Resize += (s, e) => LayoutFloatingBarControls();

            // =========================================================================
            // 2. FULL HEADER CONFIG PANEL (3 Rows)
            // =========================================================================
            _topConfigPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 136,
                BackColor = ModernTheme.PanelDark,
                Padding = new Padding(12)
            };

            // --- ROW 1: Target VLAN, FPS, Quality, TopMost ---
            Label lblTarget = new Label
            {
                Text = "Phạm vi phát sóng:",
                Location = new Point(15, 14),
                AutoSize = true,
                Font = ModernTheme.RegularFont,
                ForeColor = ModernTheme.TextSecondary
            };

            _cboTarget = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(142, 11),
                Width = 215,
                BackColor = ModernTheme.CardDark,
                ForeColor = ModernTheme.TextPrimary,
                Font = ModernTheme.RegularFont
            };
            _cboTarget.Items.Add("Tất cả máy trạm (Broadcast All)");
            HashSet<string> vlans = new HashSet<string>();
            foreach (AgentInfo a in _allAgents)
            {
                if (!string.IsNullOrEmpty(a.VlanTag) && !vlans.Contains(a.VlanTag))
                {
                    vlans.Add(a.VlanTag);
                    _cboTarget.Items.Add("Chỉ phát VLAN: " + a.VlanTag);
                }
            }
            _cboTarget.SelectedIndex = 0;

            Label lblFps = new Label
            {
                Text = "Tốc độ:",
                Location = new Point(370, 14),
                AutoSize = true,
                Font = ModernTheme.RegularFont,
                ForeColor = ModernTheme.TextSecondary
            };

            _cboFps = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(428, 11),
                Width = 82,
                BackColor = ModernTheme.CardDark,
                ForeColor = ModernTheme.TextPrimary,
                Font = ModernTheme.RegularFont
            };
            _cboFps.Items.Add("5 FPS");
            _cboFps.Items.Add("10 FPS");
            _cboFps.Items.Add("15 FPS");
            _cboFps.Items.Add("20 FPS");
            _cboFps.SelectedIndex = 2; // 15 FPS

            Label lblQuality = new Label
            {
                Text = "Chất lượng:",
                Location = new Point(522, 14),
                AutoSize = true,
                Font = ModernTheme.RegularFont,
                ForeColor = ModernTheme.TextSecondary
            };

            _cboQuality = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(603, 11),
                Width = 132,
                BackColor = ModernTheme.CardDark,
                ForeColor = ModernTheme.TextPrimary,
                Font = ModernTheme.RegularFont
            };
            _cboQuality.Items.Add("40% (Tiết kiệm)");
            _cboQuality.Items.Add("60% (Cân bằng)");
            _cboQuality.Items.Add("80% (Sắc nét)");
            _cboQuality.SelectedIndex = 1; // 60%

            _chkTopMost = new CheckBox
            {
                Text = "📌 Luôn nổi trên cùng",
                Location = new Point(748, 13),
                AutoSize = true,
                Checked = true,
                Font = ModernTheme.SmallFont,
                ForeColor = ModernTheme.AccentCyan
            };
            _chkTopMost.CheckedChanged += (s, e) => SetTopMostState(_chkTopMost.Checked);

            // --- ROW 2: Broadcast Source Mode (Full Screen vs Specific Program Window) ---
            Label lblSource = new Label
            {
                Text = "Nguồn phát sóng:",
                Location = new Point(15, 50),
                AutoSize = true,
                Font = ModernTheme.RegularFont,
                ForeColor = ModernTheme.AccentCyan
            };

            _radFullScreen = new RadioButton
            {
                Text = "🖥️ Toàn màn hình",
                Location = new Point(142, 48),
                AutoSize = true,
                Checked = true,
                Font = ModernTheme.SubHeaderFont,
                ForeColor = ModernTheme.TextPrimary
            };

            _radAppWindow = new RadioButton
            {
                Text = "💻 Chọn 1 chương trình:",
                Location = new Point(288, 48),
                AutoSize = true,
                Checked = false,
                Font = ModernTheme.SubHeaderFont,
                ForeColor = ModernTheme.TextPrimary
            };

            _cboAppWindow = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(475, 46),
                Width = 315,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = ModernTheme.CardDark,
                ForeColor = ModernTheme.TextPrimary,
                Font = ModernTheme.RegularFont,
                Enabled = false
            };

            _btnRefreshApps = new Button
            {
                Text = "🔄 Làm mới",
                Location = new Point(798, 45),
                Size = new Size(92, 27),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            ModernTheme.ApplyToButton(_btnRefreshApps, ModernTheme.CardDark, ModernTheme.AccentCyan);
            _btnRefreshApps.Click += (s, e) => RefreshOpenWindowsList(true);

            _radFullScreen.CheckedChanged += (s, e) => OnSourceModeChanged();
            _radAppWindow.CheckedChanged += (s, e) => OnSourceModeChanged();
            _cboAppWindow.SelectedIndexChanged += (s, e) => OnSelectedAppChanged();

            // --- ROW 3: Start/Stop Button, Floating Window Toggle, Options & Status ---
            _btnToggleBroadcast = new Button
            {
                Text = "📡 BẮT ĐẦU PHÁT SÓNG MÀN HÌNH",
                Location = new Point(142, 86),
                Size = new Size(245, 36)
            };
            ModernTheme.ApplyToButton(_btnToggleBroadcast, ModernTheme.AccentGreen, Color.White);
            _btnToggleBroadcast.Click += OnToggleBroadcast;

            _btnSwitchFloating = new Button
            {
                Text = "🗗 Cửa sổ nổi (PiP)",
                Location = new Point(395, 86),
                Size = new Size(138, 36)
            };
            ModernTheme.ApplyToButton(_btnSwitchFloating, ModernTheme.AccentBlue, Color.White);
            _btnSwitchFloating.Click += (s, e) => SetFloatingMode(true);

            _chkAutoFloating = new CheckBox
            {
                Text = "Tự thu gọn cửa sổ nổi khi phát",
                Location = new Point(544, 84),
                AutoSize = true,
                Checked = true,
                Font = ModernTheme.SmallFont,
                ForeColor = ModernTheme.TextPrimary
            };

            _chkBringToFront = new CheckBox
            {
                Text = "Đưa chương trình lên trên khi phát",
                Location = new Point(544, 104),
                AutoSize = true,
                Checked = true,
                Font = ModernTheme.SmallFont,
                ForeColor = ModernTheme.TextSecondary,
                Enabled = false
            };

            _chkExcludeSelfCapture = new CheckBox
            {
                Text = "Ẩn cửa sổ nổi khỏi luồng phát",
                Location = new Point(748, 84),
                AutoSize = true,
                Checked = true,
                Font = ModernTheme.SmallFont,
                ForeColor = ModernTheme.TextSecondary
            };
            _chkExcludeSelfCapture.CheckedChanged += (s, e) => ApplyWindowCaptureAffinity();

            _lblStatus = new Label
            {
                Text = "Sẵn sàng (Toàn màn hình)",
                Location = new Point(748, 104),
                AutoSize = true,
                Font = ModernTheme.SmallFont,
                ForeColor = ModernTheme.AccentYellow
            };

            _topConfigPanel.Controls.Add(lblTarget);
            _topConfigPanel.Controls.Add(_cboTarget);
            _topConfigPanel.Controls.Add(lblFps);
            _topConfigPanel.Controls.Add(_cboFps);
            _topConfigPanel.Controls.Add(lblQuality);
            _topConfigPanel.Controls.Add(_cboQuality);
            _topConfigPanel.Controls.Add(_chkTopMost);

            _topConfigPanel.Controls.Add(lblSource);
            _topConfigPanel.Controls.Add(_radFullScreen);
            _topConfigPanel.Controls.Add(_radAppWindow);
            _topConfigPanel.Controls.Add(_cboAppWindow);
            _topConfigPanel.Controls.Add(_btnRefreshApps);

            _topConfigPanel.Controls.Add(_btnToggleBroadcast);
            _topConfigPanel.Controls.Add(_btnSwitchFloating);
            _topConfigPanel.Controls.Add(_chkAutoFloating);
            _topConfigPanel.Controls.Add(_chkBringToFront);
            _topConfigPanel.Controls.Add(_chkExcludeSelfCapture);
            _topConfigPanel.Controls.Add(_lblStatus);

            // =========================================================================
            // 3. LIVE PREVIEW PICTUREBOX (Draggable, Resizable, ContextMenu, Zoomable)
            // =========================================================================
            _picPreview = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black,
                Cursor = Cursors.SizeAll
            };

            BuildFloatingContextMenu();
            _picPreview.ContextMenuStrip = _floatingMenu;
            _floatingBar.ContextMenuStrip = _floatingMenu;

            // Enable dragging the window from the preview image or floating bar
            AttachDragHandlers(_picPreview);
            AttachDragHandlers(_floatingBar);
            AttachDragHandlers(_lblFloatingStatus);

            // Double-click on preview or floating bar toggles between Floating PiP Mode and Full Mode
            _picPreview.DoubleClick += (s, e) => SetFloatingMode(!_isFloatingMode);
            _floatingBar.DoubleClick += (s, e) => SetFloatingMode(false);
            _lblFloatingStatus.DoubleClick += (s, e) => SetFloatingMode(false);

            // Mouse wheel on preview in floating mode scales the floating window smoothly
            this.MouseWheel += OnFormMouseWheel;
            _picPreview.MouseEnter += (s, e) =>
            {
                if (_isFloatingMode && this.ContainsFocus)
                {
                    _picPreview.Focus();
                }
            };

            this.Controls.Add(_picPreview);
            this.Controls.Add(_floatingBar);
            this.Controls.Add(_topConfigPanel);

            this.Shown += (s, e) =>
            {
                SetTopMostState(_chkTopMost.Checked);
                ApplyWindowCaptureAffinity();
                RefreshOpenWindowsList(false);
                CapturePreviewSnapshotOnce();
            };

            this.FormClosing += (s, e) =>
            {
                if (_isBroadcasting)
                {
                    StopBroadcasting(false);
                }
            };
        }

        private void LayoutFloatingBarControls()
        {
            if (_floatingBar == null) return;
            int w = _floatingBar.ClientSize.Width;
            int rightX = w - 6;

            if (_btnFloatingStop != null)
            {
                rightX -= _btnFloatingStop.Width;
                _btnFloatingStop.Location = new Point(rightX, 6);
                rightX -= 4;
            }
            if (_btnFloatingExpand != null)
            {
                rightX -= _btnFloatingExpand.Width;
                _btnFloatingExpand.Location = new Point(rightX, 6);
                rightX -= 4;
            }
            if (_btnFloatingPin != null)
            {
                rightX -= _btnFloatingPin.Width;
                _btnFloatingPin.Location = new Point(rightX, 6);
                rightX -= 6;
            }
            if (_lblFloatingStatus != null)
            {
                _lblFloatingStatus.Location = new Point(8, 10);
                _lblFloatingStatus.Width = Math.Max(60, rightX - 10);
            }
        }

        private void BuildFloatingContextMenu()
        {
            _floatingMenu = new ContextMenuStrip();

            ToolStripMenuItem mnuToggleFloat = new ToolStripMenuItem("🗗 Chuyển đổi Cửa sổ nổi thu gọn / Mở rộng cài đặt (Đúp chuột)");
            mnuToggleFloat.Click += (s, e) => SetFloatingMode(!_isFloatingMode);

            ToolStripMenuItem mnuTopMost = new ToolStripMenuItem("📌 Ghim cửa sổ luôn nổi trên cùng (TopMost)");
            mnuTopMost.Click += (s, e) => SetTopMostState(!this.TopMost);

            ToolStripMenuItem mnuSizeSmall = new ToolStripMenuItem("📐 Kích thước nổi: Nhỏ (320 x 210)");
            mnuSizeSmall.Click += (s, e) => ResizeFloatingWindow(320, 210);

            ToolStripMenuItem mnuSizeMedium = new ToolStripMenuItem("📐 Kích thước nổi: Vừa (440 x 290)");
            mnuSizeMedium.Click += (s, e) => ResizeFloatingWindow(440, 290);

            ToolStripMenuItem mnuSizeLarge = new ToolStripMenuItem("📐 Kích thước nổi: Lớn (580 x 380)");
            mnuSizeLarge.Click += (s, e) => ResizeFloatingWindow(580, 380);

            ToolStripMenuItem mnuSwitchFull = new ToolStripMenuItem("🖥️ Chuyển nhanh sang: Phát Toàn Màn Hình");
            mnuSwitchFull.Click += (s, e) => { _radFullScreen.Checked = true; };

            ToolStripMenuItem mnuStop = new ToolStripMenuItem("🛑 Dừng / Bắt đầu phát sóng");
            mnuStop.Click += OnToggleBroadcast;

            _floatingMenu.Items.Add(mnuToggleFloat);
            _floatingMenu.Items.Add(mnuTopMost);
            _floatingMenu.Items.Add(new ToolStripSeparator());
            _floatingMenu.Items.Add(mnuSizeSmall);
            _floatingMenu.Items.Add(mnuSizeMedium);
            _floatingMenu.Items.Add(mnuSizeLarge);
            _floatingMenu.Items.Add(new ToolStripSeparator());
            _floatingMenu.Items.Add(mnuSwitchFull);
            _floatingMenu.Items.Add(mnuStop);
        }

        private void ResizeFloatingWindow(int width, int height)
        {
            if (!_isFloatingMode)
            {
                SetFloatingMode(true);
            }
            this.Size = new Size(width, height);
            _savedFloatingBounds = this.Bounds;
        }

        private void AttachDragHandlers(System.Windows.Forms.Control ctrl)
        {
            ctrl.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    _isMouseDownForDrag = true;
                    _dragStartPoint = e.Location;
                }
            };

            ctrl.MouseMove += (s, e) =>
            {
                if (_isMouseDownForDrag && e.Button == MouseButtons.Left)
                {
                    if (Math.Abs(e.X - _dragStartPoint.X) > 4 || Math.Abs(e.Y - _dragStartPoint.Y) > 4)
                    {
                        _isMouseDownForDrag = false;
                        try
                        {
                            ReleaseCapture();
                            SendMessage(this.Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
                            if (_isFloatingMode && this.WindowState == FormWindowState.Normal)
                            {
                                _savedFloatingBounds = this.Bounds;
                            }
                        }
                        catch { }
                    }
                }
            };

            ctrl.MouseUp += (s, e) =>
            {
                _isMouseDownForDrag = false;
            };
        }

        private void OnFormMouseWheel(object sender, MouseEventArgs e)
        {
            if (!_isFloatingMode || this.WindowState != FormWindowState.Normal) return;

            int deltaW = e.Delta > 0 ? 36 : -36;
            int deltaH = e.Delta > 0 ? 24 : -24;

            int newW = Math.Max(this.MinimumSize.Width, Math.Min(1200, this.Width + deltaW));
            int newH = Math.Max(this.MinimumSize.Height, Math.Min(900, this.Height + deltaH));

            this.Size = new Size(newW, newH);
            _savedFloatingBounds = this.Bounds;
        }

        private void SetTopMostState(bool topMost)
        {
            this.TopMost = topMost;
            if (_chkTopMost != null && _chkTopMost.Checked != topMost)
            {
                _chkTopMost.Checked = topMost;
            }
            if (_btnFloatingPin != null)
            {
                ModernTheme.ApplyToButton(_btnFloatingPin, topMost ? ModernTheme.AccentBlue : ModernTheme.CardDark, Color.White);
            }
        }

        private void ApplyWindowCaptureAffinity()
        {
            if (!this.IsHandleCreated) return;
            try
            {
                uint affinity = (_chkExcludeSelfCapture != null && _chkExcludeSelfCapture.Checked)
                    ? WDA_EXCLUDEFROMCAPTURE
                    : WDA_NONE;
                SetWindowDisplayAffinity(this.Handle, affinity);
            }
            catch { }
        }

        public void SetFloatingMode(bool floating)
        {
            if (_isFloatingMode == floating) return;

            if (floating)
            {
                // Save current normal bounds before collapsing
                if (this.WindowState == FormWindowState.Normal)
                {
                    _savedNormalBounds = this.Bounds;
                }
                else
                {
                    this.WindowState = FormWindowState.Normal;
                }

                _isFloatingMode = true;
                _topConfigPanel.Visible = false;
                _floatingBar.Visible = true;
                SetTopMostState(true);

                this.Text = "📡 Cửa Sổ Nổi Kiểm Soát Phát Sóng (Kéo để di chuyển | Kéo viền/Lăn chuột để đổi cỡ)";

                if (_savedFloatingBounds != Rectangle.Empty && _savedFloatingBounds.Width >= this.MinimumSize.Width)
                {
                    this.Bounds = _savedFloatingBounds;
                }
                else
                {
                    Rectangle workArea = Screen.FromControl(this).WorkingArea;
                    int floatW = 440;
                    int floatH = 290;
                    int floatX = Math.Max(workArea.Left + 10, workArea.Right - floatW - 18);
                    int floatY = Math.Max(workArea.Top + 10, workArea.Bottom - floatH - 18);
                    this.Bounds = new Rectangle(floatX, floatY, floatW, floatH);
                    _savedFloatingBounds = this.Bounds;
                }

                LayoutFloatingBarControls();
                UpdateFloatingBarState();
            }
            else
            {
                // Save current floating bounds so user's custom size & position are remembered
                if (this.WindowState == FormWindowState.Normal)
                {
                    _savedFloatingBounds = this.Bounds;
                }

                _isFloatingMode = false;
                _floatingBar.Visible = false;
                _topConfigPanel.Visible = true;

                this.Text = "LAN Control - Trình Chiếu & Phát Sóng Màn Hình / Chương Trình (1-to-Many Broadcaster)";

                if (_savedNormalBounds != Rectangle.Empty && _savedNormalBounds.Width >= 600)
                {
                    this.Bounds = _savedNormalBounds;
                }
                else
                {
                    Rectangle workArea = Screen.FromControl(this).WorkingArea;
                    int normW = 920;
                    int normH = 650;
                    int normX = workArea.Left + (workArea.Width - normW) / 2;
                    int normY = workArea.Top + (workArea.Height - normH) / 2;
                    this.Bounds = new Rectangle(normX, normY, normW, normH);
                }
            }
        }

        private void UpdateFloatingBarState()
        {
            if (_btnFloatingStop == null || _lblFloatingStatus == null) return;

            if (_isBroadcasting)
            {
                _btnFloatingStop.Text = "🛑 Dừng";
                ModernTheme.ApplyToButton(_btnFloatingStop, ModernTheme.AccentRed, Color.White);
                _lblFloatingStatus.ForeColor = ModernTheme.AccentGreen;
            }
            else
            {
                _btnFloatingStop.Text = "▶️ Phát";
                ModernTheme.ApplyToButton(_btnFloatingStop, ModernTheme.AccentGreen, Color.White);
                _lblFloatingStatus.ForeColor = ModernTheme.AccentYellow;
                _lblFloatingStatus.Text = "⏸️ Chưa phát (" + _selectedWindowLabel + ")";
            }
        }

        private void RefreshOpenWindowsList(bool switchToAppModeIfClicked)
        {
            IntPtr previousHandle = _selectedWindowHandle;
            IntPtr selfHandle = this.IsHandleCreated ? this.Handle : IntPtr.Zero;
            List<WindowCaptureItem> windows = ScreenCapture.GetBroadcastableWindows(selfHandle);

            _cboAppWindow.BeginUpdate();
            _cboAppWindow.Items.Clear();

            int selectIdx = -1;
            for (int i = 0; i < windows.Count; i++)
            {
                _cboAppWindow.Items.Add(windows[i]);
                if (windows[i].Handle == previousHandle && previousHandle != IntPtr.Zero)
                {
                    selectIdx = i;
                }
            }

            if (_cboAppWindow.Items.Count > 0)
            {
                _cboAppWindow.SelectedIndex = selectIdx >= 0 ? selectIdx : 0;
            }
            _cboAppWindow.EndUpdate();

            if (switchToAppModeIfClicked && _cboAppWindow.Items.Count > 0 && !_radAppWindow.Checked)
            {
                _radAppWindow.Checked = true;
            }
        }

        private void OnSourceModeChanged()
        {
            bool appMode = _radAppWindow.Checked;
            _cboAppWindow.Enabled = appMode;
            _chkBringToFront.Enabled = appMode;

            if (appMode && _cboAppWindow.Items.Count == 0)
            {
                RefreshOpenWindowsList(false);
            }

            UpdateCaptureTargetState();

            if (!_isBroadcasting)
            {
                _btnToggleBroadcast.Text = appMode
                    ? "📡 BẮT ĐẦU PHÁT SÓNG CHƯƠNG TRÌNH"
                    : "📡 BẮT ĐẦU PHÁT SÓNG MÀN HÌNH";
                _lblStatus.Text = "Chưa phát (" + _selectedWindowLabel + ")";
                UpdateFloatingBarState();
                CapturePreviewSnapshotOnce();
            }
        }

        private void OnSelectedAppChanged()
        {
            UpdateCaptureTargetState();
            if (_radAppWindow.Checked && !_isBroadcasting)
            {
                CapturePreviewSnapshotOnce();
            }
        }

        private void UpdateCaptureTargetState()
        {
            if (_radAppWindow.Checked)
            {
                WindowCaptureItem item = _cboAppWindow.SelectedItem as WindowCaptureItem;
                if (item != null)
                {
                    _selectedWindowHandle = item.Handle;
                    _selectedWindowLabel = string.Format("Chương trình: {0}", item.ProcessName);
                    _captureAppWindowMode = true;
                }
                else
                {
                    _selectedWindowHandle = IntPtr.Zero;
                    _selectedWindowLabel = "Chưa chọn chương trình";
                    _captureAppWindowMode = true;
                }
            }
            else
            {
                _captureAppWindowMode = false;
                _selectedWindowHandle = IntPtr.Zero;
                _selectedWindowLabel = "Toàn màn hình";
            }
        }

        private void CapturePreviewSnapshotOnce()
        {
            try
            {
                byte[] previewBytes = _captureAppWindowMode && _selectedWindowHandle != IntPtr.Zero
                    ? ScreenCapture.CaptureWindowJpeg(_selectedWindowHandle, 60, false)
                    : ScreenCapture.CaptureDesktopJpeg(60, false, 0, 0);

                if (previewBytes != null && previewBytes.Length > 0)
                {
                    using (MemoryStream ms = new MemoryStream(previewBytes))
                    {
                        Image newImg = Image.FromStream(ms);
                        Image oldImg = _picPreview.Image;
                        _picPreview.Image = newImg;
                        if (oldImg != null) oldImg.Dispose();
                    }
                }
            }
            catch { }
        }

        private string GetSelectedTargetVlan()
        {
            if (_cboTarget.SelectedIndex <= 0 || _cboTarget.SelectedItem == null)
            {
                return "";
            }
            string text = _cboTarget.SelectedItem.ToString();
            const string prefix = "Chỉ phát VLAN: ";
            if (text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return text.Substring(prefix.Length).Trim();
            }
            return "";
        }

        private void OnToggleBroadcast(object sender, EventArgs e)
        {
            if (!_isBroadcasting)
            {
                StartBroadcasting();
            }
            else
            {
                StopBroadcasting(true);
            }
        }

        private void StartBroadcasting()
        {
            UpdateCaptureTargetState();

            if (_captureAppWindowMode && _selectedWindowHandle == IntPtr.Zero)
            {
                MessageBox.Show(this, "Vui lòng chọn 1 chương trình đang mở trong danh sách để phát sóng (hoặc bấm '🔄 Làm mới')!",
                    "Chưa chọn chương trình", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ApplyWindowCaptureAffinity();

            _isBroadcasting = true;
            _framesSent = 0;
            _startTime = DateTime.Now;
            _activeTargetVlan = GetSelectedTargetVlan();

            _btnToggleBroadcast.Text = "🛑 DỪNG PHÁT SÓNG";
            ModernTheme.ApplyToButton(_btnToggleBroadcast, ModernTheme.AccentRed, Color.White);
            _lblStatus.Text = "Đang phát (" + _selectedWindowLabel + ")...";
            UpdateFloatingBarState();

            // Automatically switch to compact floating monitor window if option is enabled
            if (_chkAutoFloating.Checked && !_isFloatingMode)
            {
                SetFloatingMode(true);
            }
            else if (_chkTopMost.Checked)
            {
                SetTopMostState(true);
            }

            if (_captureAppWindowMode && _selectedWindowHandle != IntPtr.Zero)
            {
                ScreenCapture.BringWindowToFrontIfMinimized(_selectedWindowHandle, _chkBringToFront.Checked);
            }

            _client.StartBroadcast(_activeTargetVlan);

            int targetFps = 15;
            if (_cboFps.SelectedIndex == 0) targetFps = 5;
            else if (_cboFps.SelectedIndex == 1) targetFps = 10;
            else if (_cboFps.SelectedIndex == 2) targetFps = 15;
            else if (_cboFps.SelectedIndex == 3) targetFps = 20;

            int delayMs = 1000 / targetFps;

            long quality = 60;
            if (_cboQuality.SelectedIndex == 0) quality = 40;
            else if (_cboQuality.SelectedIndex == 1) quality = 60;
            else if (_cboQuality.SelectedIndex == 2) quality = 80;

            _broadcastThread = new Thread(delegate()
            {
                while (_isBroadcasting)
                {
                    try
                    {
                        byte[] screenBytes;
                        if (_captureAppWindowMode && _selectedWindowHandle != IntPtr.Zero)
                        {
                            screenBytes = ScreenCapture.CaptureWindowJpeg(_selectedWindowHandle, quality, true);
                        }
                        else
                        {
                            screenBytes = ScreenCapture.CaptureDesktopJpeg(quality, true, 0, 0);
                        }

                        if (screenBytes != null && screenBytes.Length > 0)
                        {
                            _client.SendBroadcastFrame(screenBytes, _activeTargetVlan);
                            _framesSent++;

                            // Update local preview
                            if (_framesSent % 2 == 0)
                            {
                                if (this.IsHandleCreated && !this.IsDisposed)
                                {
                                    this.BeginInvoke(new Action(delegate
                                    {
                                        try
                                        {
                                            if (this.IsDisposed) return;
                                            using (MemoryStream ms = new MemoryStream(screenBytes))
                                            {
                                                Image newImg = Image.FromStream(ms);
                                                Image oldImg = _picPreview.Image;
                                                _picPreview.Image = newImg;
                                                if (oldImg != null) oldImg.Dispose();
                                            }

                                            TimeSpan elapsed = DateTime.Now - _startTime;
                                            double fps = (double)_framesSent / Math.Max(1, elapsed.TotalSeconds);
                                            _lblStatus.Text = string.Format("Đang phát [{0}] | {1:F1} FPS",
                                                _selectedWindowLabel, fps);
                                            _lblFloatingStatus.Text = string.Format("🟢 {0} ({1:mm\\:ss} | {2:F0} FPS)",
                                                _selectedWindowLabel, elapsed, fps);
                                        }
                                        catch { }
                                    }));
                                }
                            }
                        }

                        Thread.Sleep(delayMs);
                    }
                    catch { }
                }
            })
            {
                IsBackground = true
            };
            _broadcastThread.Start();
        }

        private void StopBroadcasting(bool restoreFullViewIfAutoFloated)
        {
            _isBroadcasting = false;
            _client.StopBroadcast(_activeTargetVlan);

            _btnToggleBroadcast.Text = _radAppWindow.Checked
                ? "📡 BẮT ĐẦU PHÁT SÓNG CHƯƠNG TRÌNH"
                : "📡 BẮT ĐẦU PHÁT SÓNG MÀN HÌNH";
            ModernTheme.ApplyToButton(_btnToggleBroadcast, ModernTheme.AccentGreen, Color.White);
            _lblStatus.Text = "Đã dừng phát sóng.";
            UpdateFloatingBarState();

            if (restoreFullViewIfAutoFloated && _isFloatingMode && _chkAutoFloating.Checked)
            {
                SetFloatingMode(false);
            }
        }
    }
}
