using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
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
        private List<AgentInfo> _allAgents;
        private readonly HashSet<string> _selectedAgentIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly object _sendSync = new object();

        private Panel _topConfigPanel;
        private Panel _floatingBar;

        private ComboBox _cboTarget;
        private Button _btnPickAgents;
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

        private volatile bool _isBroadcasting = false;
        private bool _isFloatingMode = false;
        private bool _suppressTargetComboEvent = false;
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
        private volatile string _activeTargetFilter = "";

        public BroadcastSenderForm(ControlClient client, List<AgentInfo> allAgents, List<AgentInfo> preselectedAgents = null)
        {
            _client = client;
            _allAgents = allAgents != null ? new List<AgentInfo>(allAgents) : new List<AgentInfo>();

            if (preselectedAgents != null)
            {
                foreach (AgentInfo a in preselectedAgents)
                {
                    if (a != null && !string.IsNullOrEmpty(a.AgentId))
                    {
                        _selectedAgentIds.Add(a.AgentId);
                    }
                }
            }

            InitializeComponent();
            PopulateTargetComboBox(_selectedAgentIds.Count > 0);
        }

        public void UpdateAgentsList(List<AgentInfo> allAgents, List<AgentInfo> preselectedAgents)
        {
            _allAgents = allAgents != null ? new List<AgentInfo>(allAgents) : new List<AgentInfo>();

            if (preselectedAgents != null && preselectedAgents.Count > 0)
            {
                _selectedAgentIds.Clear();
                foreach (AgentInfo a in preselectedAgents)
                {
                    if (a != null && !string.IsNullOrEmpty(a.AgentId))
                    {
                        _selectedAgentIds.Add(a.AgentId);
                    }
                }
                PopulateTargetComboBox(true);
            }
            else
            {
                PopulateTargetComboBox(IsCustomAgentsTargetSelected());
            }
        }

        private void InitializeComponent()
        {
            this.Text = "LAN Control - Trình Chiếu & Phát Sóng Màn Hình / Chương Trình (Đa Máy - Đa VLAN)";
            this.Size = new Size(950, 650);
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
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = ModernTheme.AccentGreen,
                AutoEllipsis = true,
                Cursor = Cursors.SizeAll
            };

            _btnFloatingPin = new Button
            {
                Text = "📌",
                Size = new Size(34, 26),
                Location = new Point(215, 6)
            };
            ModernTheme.ApplyToButton(_btnFloatingPin, ModernTheme.AccentBlue, Color.White);
            _btnFloatingPin.Click += (s, e) => SetTopMostState(!this.TopMost);

            _btnFloatingExpand = new Button
            {
                Text = "🗖 Mở rộng",
                Size = new Size(88, 26),
                Location = new Point(253, 6)
            };
            ModernTheme.ApplyToButton(_btnFloatingExpand, ModernTheme.CardDark, ModernTheme.AccentCyan);
            _btnFloatingExpand.Click += (s, e) => SetFloatingMode(false);

            _btnFloatingStop = new Button
            {
                Text = "🛑 Dừng",
                Size = new Size(72, 26),
                Location = new Point(345, 6)
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

            // --- ROW 1: Target Scope (All / VLAN / Custom Multi-Machine across VLANs), FPS, Quality, TopMost ---
            Label lblTarget = new Label
            {
                Text = "Phạm vi phát:",
                Location = new Point(15, 14),
                AutoSize = true,
                Font = ModernTheme.RegularFont,
                ForeColor = ModernTheme.TextSecondary
            };

            _cboTarget = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(112, 11),
                Width = 228,
                BackColor = ModernTheme.CardDark,
                ForeColor = ModernTheme.TextPrimary,
                Font = ModernTheme.RegularFont
            };
            _cboTarget.SelectedIndexChanged += OnTargetScopeComboChanged;

            _btnPickAgents = new Button
            {
                Text = "☑️ Chọn máy...",
                Location = new Point(345, 10),
                Size = new Size(115, 27)
            };
            ModernTheme.ApplyToButton(_btnPickAgents, ModernTheme.AccentBlue, Color.White);
            _btnPickAgents.Click += (s, e) => ShowMultiAgentPickerDialog();

            Label lblFps = new Label
            {
                Text = "Tốc độ:",
                Location = new Point(468, 14),
                AutoSize = true,
                Font = ModernTheme.RegularFont,
                ForeColor = ModernTheme.TextSecondary
            };

            _cboFps = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(522, 11),
                Width = 76,
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
                Location = new Point(606, 14),
                AutoSize = true,
                Font = ModernTheme.RegularFont,
                ForeColor = ModernTheme.TextSecondary
            };

            _cboQuality = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(684, 11),
                Width = 118,
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
                Text = "📌 Luôn nổi",
                Location = new Point(812, 13),
                AutoSize = true,
                Checked = true,
                Font = ModernTheme.SmallFont,
                ForeColor = ModernTheme.AccentCyan
            };
            _chkTopMost.CheckedChanged += (s, e) => SetTopMostState(_chkTopMost.Checked);

            // --- ROW 2: Broadcast Source Mode (Full Screen vs Specific Program Window) ---
            Label lblSource = new Label
            {
                Text = "Nguồn phát:",
                Location = new Point(15, 50),
                AutoSize = true,
                Font = ModernTheme.RegularFont,
                ForeColor = ModernTheme.AccentCyan
            };

            _radFullScreen = new RadioButton
            {
                Text = "🖥️ Toàn màn hình",
                Location = new Point(112, 48),
                AutoSize = true,
                Checked = true,
                Font = ModernTheme.SubHeaderFont,
                ForeColor = ModernTheme.TextPrimary
            };

            _radAppWindow = new RadioButton
            {
                Text = "💻 Chọn 1 chương trình:",
                Location = new Point(258, 48),
                AutoSize = true,
                Checked = false,
                Font = ModernTheme.SubHeaderFont,
                ForeColor = ModernTheme.TextPrimary
            };

            _cboAppWindow = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(445, 46),
                Width = 370,
                BackColor = ModernTheme.CardDark,
                ForeColor = ModernTheme.TextPrimary,
                Font = ModernTheme.RegularFont,
                Enabled = false
            };

            _btnRefreshApps = new Button
            {
                Text = "🔄 Làm mới",
                Location = new Point(822, 45),
                Size = new Size(95, 27)
            };
            ModernTheme.ApplyToButton(_btnRefreshApps, ModernTheme.CardDark, ModernTheme.AccentCyan);
            _btnRefreshApps.Click += (s, e) => RefreshOpenWindowsList(true);

            _topConfigPanel.Resize += (s, e) =>
            {
                int w = _topConfigPanel.ClientSize.Width;
                if (w > 560)
                {
                    _btnRefreshApps.Location = new Point(w - _btnRefreshApps.Width - 14, 45);
                    _cboAppWindow.Width = Math.Max(140, _btnRefreshApps.Left - _cboAppWindow.Left - 8);
                }
            };

            _radFullScreen.CheckedChanged += (s, e) => OnSourceModeChanged();
            _radAppWindow.CheckedChanged += (s, e) => OnSourceModeChanged();
            _cboAppWindow.SelectedIndexChanged += (s, e) => OnSelectedAppChanged();

            // --- ROW 3: Start/Stop Button, Floating Window Toggle, Options & Status ---
            _btnToggleBroadcast = new Button
            {
                Text = "📡 BẮT ĐẦU PHÁT SÓNG MÀN HÌNH",
                Location = new Point(112, 86),
                Size = new Size(245, 36)
            };
            ModernTheme.ApplyToButton(_btnToggleBroadcast, ModernTheme.AccentGreen, Color.White);
            _btnToggleBroadcast.Click += OnToggleBroadcast;

            _btnSwitchFloating = new Button
            {
                Text = "🗗 Cửa sổ nổi (PiP)",
                Location = new Point(365, 86),
                Size = new Size(138, 36)
            };
            ModernTheme.ApplyToButton(_btnSwitchFloating, ModernTheme.AccentBlue, Color.White);
            _btnSwitchFloating.Click += (s, e) => SetFloatingMode(true);

            _chkAutoFloating = new CheckBox
            {
                Text = "Tự thu gọn cửa sổ nổi khi phát",
                Location = new Point(514, 84),
                AutoSize = true,
                Checked = true,
                Font = ModernTheme.SmallFont,
                ForeColor = ModernTheme.TextPrimary
            };

            _chkBringToFront = new CheckBox
            {
                Text = "Đưa chương trình lên trên khi phát",
                Location = new Point(514, 104),
                AutoSize = true,
                Checked = true,
                Font = ModernTheme.SmallFont,
                ForeColor = ModernTheme.TextSecondary,
                Enabled = false
            };

            _chkExcludeSelfCapture = new CheckBox
            {
                Text = "Ẩn cửa sổ nổi khỏi luồng phát",
                Location = new Point(718, 84),
                AutoSize = true,
                Checked = true,
                Font = ModernTheme.SmallFont,
                ForeColor = ModernTheme.TextSecondary
            };
            _chkExcludeSelfCapture.CheckedChanged += (s, e) => ApplyWindowCaptureAffinity();

            _lblStatus = new Label
            {
                Text = "Sẵn sàng (Toàn màn hình)",
                Location = new Point(718, 104),
                AutoSize = true,
                Font = ModernTheme.SmallFont,
                ForeColor = ModernTheme.AccentYellow
            };

            _topConfigPanel.Controls.Add(lblTarget);
            _topConfigPanel.Controls.Add(_cboTarget);
            _topConfigPanel.Controls.Add(_btnPickAgents);
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

            AttachDragHandlers(_picPreview);
            AttachDragHandlers(_floatingBar);
            AttachDragHandlers(_lblFloatingStatus);

            _picPreview.DoubleClick += (s, e) => SetFloatingMode(!_isFloatingMode);
            _floatingBar.DoubleClick += (s, e) => SetFloatingMode(false);
            _lblFloatingStatus.DoubleClick += (s, e) => SetFloatingMode(false);

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

        // =========================================================================
        // MULTI-AGENT / MULTI-VLAN SELECTION LOGIC
        // =========================================================================
        private void PopulateTargetComboBox(bool selectCustomAgentsOption)
        {
            _suppressTargetComboEvent = true;
            try
            {
                string previousSelection = _cboTarget.SelectedItem != null ? _cboTarget.SelectedItem.ToString() : "";

                _cboTarget.BeginUpdate();
                _cboTarget.Items.Clear();

                _cboTarget.Items.Add(string.Format("🌐 Tất cả máy trạm ({0} máy)", _allAgents.Count));

                string customLabel = _selectedAgentIds.Count > 0
                    ? string.Format("🎯 Đã chọn {0} máy trạm (Đa VLAN)", _selectedAgentIds.Count)
                    : "🎯 Chọn 1 hoặc nhiều máy (Đa VLAN)...";
                _cboTarget.Items.Add(customLabel);

                HashSet<string> vlans = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (AgentInfo a in _allAgents)
                {
                    if (a != null && !string.IsNullOrEmpty(a.VlanTag) && !vlans.Contains(a.VlanTag))
                    {
                        vlans.Add(a.VlanTag);
                        _cboTarget.Items.Add("📂 Chỉ phát VLAN: " + a.VlanTag);
                    }
                }

                if (selectCustomAgentsOption)
                {
                    _cboTarget.SelectedIndex = 1;
                }
                else if (!string.IsNullOrEmpty(previousSelection))
                {
                    int foundIdx = 0;
                    for (int i = 0; i < _cboTarget.Items.Count; i++)
                    {
                        if (_cboTarget.Items[i].ToString().Equals(previousSelection, StringComparison.OrdinalIgnoreCase))
                        {
                            foundIdx = i;
                            break;
                        }
                    }
                    _cboTarget.SelectedIndex = foundIdx;
                }
                else
                {
                    _cboTarget.SelectedIndex = 0;
                }

                _cboTarget.EndUpdate();
                UpdatePickAgentsButtonLabel();
            }
            finally
            {
                _suppressTargetComboEvent = false;
            }
        }

        private void UpdatePickAgentsButtonLabel()
        {
            if (_btnPickAgents == null) return;
            if (_selectedAgentIds.Count > 0 && IsCustomAgentsTargetSelected())
            {
                _btnPickAgents.Text = string.Format("☑️ Đã chọn ({0})", _selectedAgentIds.Count);
                ModernTheme.ApplyToButton(_btnPickAgents, ModernTheme.AccentGreen, Color.White);
            }
            else
            {
                _btnPickAgents.Text = "☑️ Chọn máy...";
                ModernTheme.ApplyToButton(_btnPickAgents, ModernTheme.AccentBlue, Color.White);
            }
        }

        private bool IsCustomAgentsTargetSelected()
        {
            return _cboTarget != null && _cboTarget.SelectedIndex == 1;
        }

        private void OnTargetScopeComboChanged(object sender, EventArgs e)
        {
            if (_suppressTargetComboEvent) return;

            if (_cboTarget.SelectedIndex == 1 && _selectedAgentIds.Count == 0)
            {
                ShowMultiAgentPickerDialog();
                return;
            }

            UpdatePickAgentsButtonLabel();
            ApplyTargetScopeChangeLiveIfBroadcasting();
        }

        private void ShowMultiAgentPickerDialog()
        {
            using (Form dlg = new Form())
            {
                dlg.Text = "☑️ Chọn 1 Hoặc Nhiều Máy Trạm Nhận Phát Sóng (Hỗ Trợ Đa VLAN)";
                dlg.Size = new Size(740, 520);
                dlg.MinimumSize = new Size(640, 400);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.BackColor = ModernTheme.BackgroundDark;
                dlg.ForeColor = ModernTheme.TextPrimary;
                dlg.TopMost = true;

                // Top Filter Bar (VLAN filter + Search + Quick Select buttons + Top OK button)
                Panel topFilter = new Panel
                {
                    Dock = DockStyle.Top,
                    Height = 86,
                    BackColor = ModernTheme.PanelDark,
                    Padding = new Padding(10)
                };

                Label lblVlanFilter = new Label
                {
                    Text = "Lọc hiển thị VLAN:",
                    Location = new Point(12, 14),
                    AutoSize = true,
                    Font = ModernTheme.RegularFont,
                    ForeColor = ModernTheme.TextSecondary
                };

                ComboBox cboVlanFilter = new ComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Location = new Point(135, 11),
                    Width = 210,
                    BackColor = ModernTheme.CardDark,
                    ForeColor = ModernTheme.TextPrimary,
                    Font = ModernTheme.RegularFont
                };
                cboVlanFilter.Items.Add("Tất cả VLAN (" + _allAgents.Count + " máy)");
                HashSet<string> vlans = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (AgentInfo a in _allAgents)
                {
                    if (a != null && !string.IsNullOrEmpty(a.VlanTag) && !vlans.Contains(a.VlanTag))
                    {
                        vlans.Add(a.VlanTag);
                        cboVlanFilter.Items.Add(a.VlanTag);
                    }
                }
                cboVlanFilter.SelectedIndex = 0;

                Label lblSearch = new Label
                {
                    Text = "Tìm nhanh:",
                    Location = new Point(360, 14),
                    AutoSize = true,
                    Font = ModernTheme.RegularFont,
                    ForeColor = ModernTheme.TextSecondary
                };

                TextBox txtSearch = new TextBox
                {
                    Location = new Point(440, 11),
                    Width = 260,
                    BackColor = ModernTheme.CardDark,
                    ForeColor = ModernTheme.TextPrimary,
                    BorderStyle = BorderStyle.FixedSingle,
                    Font = ModernTheme.RegularFont
                };

                Button btnCheckVisible = new Button
                {
                    Text = "✅ Chọn tất cả đang hiện",
                    Location = new Point(12, 46),
                    Size = new Size(195, 30)
                };
                ModernTheme.ApplyToButton(btnCheckVisible, ModernTheme.CardDark, ModernTheme.AccentGreen);

                Button btnUncheckVisible = new Button
                {
                    Text = "✖ Bỏ chọn đang hiện",
                    Location = new Point(214, 46),
                    Size = new Size(165, 30)
                };
                ModernTheme.ApplyToButton(btnUncheckVisible, ModernTheme.CardDark, ModernTheme.TextPrimary);

                Button btnClearAll = new Button
                {
                    Text = "🧹 Xóa chọn tất cả",
                    Location = new Point(386, 46),
                    Size = new Size(155, 30)
                };
                ModernTheme.ApplyToButton(btnClearAll, ModernTheme.CardDark, ModernTheme.AccentYellow);

                Button btnTopOk = new Button
                {
                    Text = "✔️ OK - Chọn",
                    Location = new Point(550, 46),
                    Size = new Size(150, 30)
                };
                ModernTheme.ApplyToButton(btnTopOk, ModernTheme.AccentGreen, Color.White);

                topFilter.Controls.Add(lblVlanFilter);
                topFilter.Controls.Add(cboVlanFilter);
                topFilter.Controls.Add(lblSearch);
                topFilter.Controls.Add(txtSearch);
                topFilter.Controls.Add(btnCheckVisible);
                topFilter.Controls.Add(btnUncheckVisible);
                topFilter.Controls.Add(btnClearAll);
                topFilter.Controls.Add(btnTopOk);

                // Bottom Confirmation Bar
                Panel bottomBar = new Panel
                {
                    Dock = DockStyle.Bottom,
                    Height = 54,
                    BackColor = ModernTheme.PanelDark,
                    Padding = new Padding(12, 8, 12, 8)
                };

                Label lblSummary = new Label
                {
                    Text = "Đang chọn: 0 máy trạm",
                    Location = new Point(12, 17),
                    AutoSize = true,
                    Font = ModernTheme.SubHeaderFont,
                    ForeColor = ModernTheme.AccentCyan
                };

                Button btnOk = new Button
                {
                    Text = "✔️ OK - Xác Nhận Chọn",
                    Size = new Size(195, 36),
                    Location = new Point(400, 9)
                };
                ModernTheme.ApplyToButton(btnOk, ModernTheme.AccentGreen, Color.White);

                Button btnCancel = new Button
                {
                    Text = "Hủy",
                    Size = new Size(90, 36),
                    Location = new Point(605, 9),
                    DialogResult = DialogResult.Cancel
                };
                ModernTheme.ApplyToButton(btnCancel, ModernTheme.CardDark, ModernTheme.TextPrimary);

                bottomBar.Controls.Add(lblSummary);
                bottomBar.Controls.Add(btnOk);
                bottomBar.Controls.Add(btnCancel);

                Action layoutDialogBars = () =>
                {
                    int topW = topFilter.ClientSize.Width;
                    if (topW > 460)
                    {
                        txtSearch.Width = Math.Max(120, topW - txtSearch.Left - 14);
                        btnTopOk.Location = new Point(Math.Max(548, topW - btnTopOk.Width - 14), 46);
                    }

                    int botW = bottomBar.ClientSize.Width;
                    if (botW > 320)
                    {
                        btnCancel.Location = new Point(botW - btnCancel.Width - 14, 9);
                        btnOk.Location = new Point(btnCancel.Left - btnOk.Width - 10, 9);
                    }
                };
                topFilter.Resize += (s, e) => layoutDialogBars();
                bottomBar.Resize += (s, e) => layoutDialogBars();

                // Grid of machines across all VLANs
                DataGridView grid = new DataGridView
                {
                    Dock = DockStyle.Fill,
                    BackgroundColor = ModernTheme.BackgroundDark,
                    BorderStyle = BorderStyle.None,
                    AllowUserToAddRows = false,
                    AllowUserToDeleteRows = false,
                    AllowUserToResizeRows = false,
                    RowHeadersVisible = false,
                    MultiSelect = false,
                    SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                    EnableHeadersVisualStyles = false,
                    GridColor = ModernTheme.BorderDark
                };
                grid.ColumnHeadersDefaultCellStyle.BackColor = ModernTheme.CardDark;
                grid.ColumnHeadersDefaultCellStyle.ForeColor = ModernTheme.AccentCyan;
                grid.ColumnHeadersDefaultCellStyle.Font = ModernTheme.SubHeaderFont;
                grid.ColumnHeadersHeight = 34;

                grid.DefaultCellStyle.BackColor = ModernTheme.BackgroundDark;
                grid.DefaultCellStyle.ForeColor = ModernTheme.TextPrimary;
                grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(35, 55, 80);
                grid.DefaultCellStyle.SelectionForeColor = Color.White;
                grid.DefaultCellStyle.Font = ModernTheme.RegularFont;
                grid.RowTemplate.Height = 30;

                DataGridViewCheckBoxColumn colCheck = new DataGridViewCheckBoxColumn
                {
                    HeaderText = "Chọn",
                    ReadOnly = true,
                    FillWeight = 14
                };
                DataGridViewTextBoxColumn colVlan = new DataGridViewTextBoxColumn
                {
                    HeaderText = "Phân Đoạn VLAN",
                    ReadOnly = true,
                    FillWeight = 24
                };
                DataGridViewTextBoxColumn colHost = new DataGridViewTextBoxColumn
                {
                    HeaderText = "Tên Máy Trạm (Hostname)",
                    ReadOnly = true,
                    FillWeight = 32
                };
                DataGridViewTextBoxColumn colIp = new DataGridViewTextBoxColumn
                {
                    HeaderText = "Địa Chỉ IP",
                    ReadOnly = true,
                    FillWeight = 22
                };
                DataGridViewTextBoxColumn colUser = new DataGridViewTextBoxColumn
                {
                    HeaderText = "Người Dùng",
                    ReadOnly = true,
                    FillWeight = 20
                };

                grid.ReadOnly = true;
                grid.Columns.AddRange(new DataGridViewColumn[] { colCheck, colVlan, colHost, colIp, colUser });

                // Working copy of selected AgentIds inside dialog
                HashSet<string> tempSelected = new HashSet<string>(_selectedAgentIds, StringComparer.OrdinalIgnoreCase);

                Action updateSummary = () =>
                {
                    HashSet<string> selectedVlans = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (AgentInfo a in _allAgents)
                    {
                        if (a != null && !string.IsNullOrEmpty(a.AgentId) && tempSelected.Contains(a.AgentId))
                        {
                            selectedVlans.Add(string.IsNullOrEmpty(a.VlanTag) ? "Default" : a.VlanTag);
                        }
                    }
                    lblSummary.Text = string.Format("📌 Đã chọn: {0} / {1} máy trạm (thuộc {2} VLAN)",
                        tempSelected.Count, _allAgents.Count, selectedVlans.Count);
                    btnOk.Text = string.Format("✔️ OK - Chọn ({0} máy)", tempSelected.Count);
                    btnTopOk.Text = string.Format("✔️ OK ({0} máy)", tempSelected.Count);
                };

                Action populateGrid = () =>
                {
                    string vlanFilter = cboVlanFilter.SelectedIndex <= 0 ? "" : cboVlanFilter.SelectedItem.ToString();
                    string search = txtSearch.Text.Trim().ToLowerInvariant();

                    grid.Rows.Clear();
                    foreach (AgentInfo a in _allAgents)
                    {
                        if (a == null || string.IsNullOrEmpty(a.AgentId)) continue;

                        string vlan = string.IsNullOrEmpty(a.VlanTag) ? "Default" : a.VlanTag;
                        if (!string.IsNullOrEmpty(vlanFilter) && !vlan.Equals(vlanFilter, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        if (!string.IsNullOrEmpty(search))
                        {
                            bool match = (a.Hostname != null && a.Hostname.ToLowerInvariant().Contains(search)) ||
                                         (a.IpAddress != null && a.IpAddress.ToLowerInvariant().Contains(search)) ||
                                         (a.UserName != null && a.UserName.ToLowerInvariant().Contains(search)) ||
                                         vlan.ToLowerInvariant().Contains(search);
                            if (!match) continue;
                        }

                        int rowIdx = grid.Rows.Add(
                            tempSelected.Contains(a.AgentId),
                            vlan,
                            a.Hostname,
                            a.IpAddress,
                            a.UserName
                        );
                        grid.Rows[rowIdx].Tag = a;
                    }
                    updateSummary();
                };

                // Clicking anywhere on a row toggles its checkbox
                grid.CellClick += (s, e) =>
                {
                    if (e.RowIndex < 0 || e.RowIndex >= grid.Rows.Count) return;
                    DataGridViewRow row = grid.Rows[e.RowIndex];
                    AgentInfo a = row.Tag as AgentInfo;
                    if (a == null || string.IsNullOrEmpty(a.AgentId)) return;

                    bool newChecked = !tempSelected.Contains(a.AgentId);
                    if (newChecked) tempSelected.Add(a.AgentId);
                    else tempSelected.Remove(a.AgentId);

                    row.Cells[0].Value = newChecked;
                    updateSummary();
                };

                cboVlanFilter.SelectedIndexChanged += (s, e) => populateGrid();
                txtSearch.TextChanged += (s, e) => populateGrid();

                btnCheckVisible.Click += (s, e) =>
                {
                    foreach (DataGridViewRow row in grid.Rows)
                    {
                        AgentInfo a = row.Tag as AgentInfo;
                        if (a != null && !string.IsNullOrEmpty(a.AgentId))
                        {
                            tempSelected.Add(a.AgentId);
                            row.Cells[0].Value = true;
                        }
                    }
                    updateSummary();
                };

                btnUncheckVisible.Click += (s, e) =>
                {
                    foreach (DataGridViewRow row in grid.Rows)
                    {
                        AgentInfo a = row.Tag as AgentInfo;
                        if (a != null && !string.IsNullOrEmpty(a.AgentId))
                        {
                            tempSelected.Remove(a.AgentId);
                            row.Cells[0].Value = false;
                        }
                    }
                    updateSummary();
                };

                btnClearAll.Click += (s, e) =>
                {
                    tempSelected.Clear();
                    foreach (DataGridViewRow row in grid.Rows)
                    {
                        row.Cells[0].Value = false;
                    }
                    updateSummary();
                };

                EventHandler confirmHandler = (s, e) =>
                {
                    if (tempSelected.Count == 0)
                    {
                        MessageBox.Show(dlg, "Vui lòng tích chọn ít nhất 1 máy trạm trong danh sách (hoặc bấm Hủy để phát tất cả)!",
                            "Chưa chọn máy trạm", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    _selectedAgentIds.Clear();
                    foreach (string id in tempSelected)
                    {
                        _selectedAgentIds.Add(id);
                    }

                    PopulateTargetComboBox(true);
                    ApplyTargetScopeChangeLiveIfBroadcasting();
                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                };

                btnOk.Click += confirmHandler;
                btnTopOk.Click += confirmHandler;

                dlg.AcceptButton = btnOk;
                dlg.CancelButton = btnCancel;

                dlg.Controls.Add(grid);
                dlg.Controls.Add(topFilter);
                dlg.Controls.Add(bottomBar);

                dlg.Shown += (s, e) => layoutDialogBars();
                layoutDialogBars();
                populateGrid();
                dlg.ShowDialog(this);

                // If user cancelled and had 0 selected while on index 1, revert to index 0
                if (_cboTarget.SelectedIndex == 1 && _selectedAgentIds.Count == 0)
                {
                    _suppressTargetComboEvent = true;
                    _cboTarget.SelectedIndex = 0;
                    _suppressTargetComboEvent = false;
                    UpdatePickAgentsButtonLabel();
                }
            }
        }

        private string ComputeTargetFilterFromSelection()
        {
            if (_cboTarget == null || _cboTarget.SelectedIndex <= 0 || _cboTarget.SelectedItem == null)
            {
                return ""; // Broadcast ALL
            }

            if (_cboTarget.SelectedIndex == 1)
            {
                if (_selectedAgentIds.Count == 0) return "";
                StringBuilder sb = new StringBuilder("AGENTS:");
                bool first = true;
                foreach (string id in _selectedAgentIds)
                {
                    if (!first) sb.Append(',');
                    sb.Append(id);
                    first = false;
                }
                return sb.ToString();
            }

            string text = _cboTarget.SelectedItem.ToString();
            const string prefix = "📂 Chỉ phát VLAN: ";
            if (text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return text.Substring(prefix.Length).Trim();
            }
            return "";
        }

        private void ApplyTargetScopeChangeLiveIfBroadcasting()
        {
            if (!_isBroadcasting) return;

            string newFilter = ComputeTargetFilterFromSelection();
            string oldFilter = _activeTargetFilter;
            if (string.Equals(newFilter, oldFilter, StringComparison.OrdinalIgnoreCase)) return;

            lock (_sendSync)
            {
                // Stop broadcast on old targets that may no longer be included, then start on new targets
                _client.StopBroadcast(oldFilter);
                _activeTargetFilter = newFilter;
                _client.StartBroadcast(_activeTargetFilter);
            }
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

            ToolStripMenuItem mnuPickAgents = new ToolStripMenuItem("☑️ Chọn máy trạm nhận phát sóng (Đa VLAN)...");
            mnuPickAgents.Click += (s, e) => ShowMultiAgentPickerDialog();

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

            _floatingMenu.Items.Add(mnuPickAgents);
            _floatingMenu.Items.Add(new ToolStripSeparator());
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
                if (this.WindowState == FormWindowState.Normal)
                {
                    _savedFloatingBounds = this.Bounds;
                }

                _isFloatingMode = false;
                _floatingBar.Visible = false;
                _topConfigPanel.Visible = true;

                this.Text = "LAN Control - Trình Chiếu & Phát Sóng Màn Hình / Chương Trình (Đa Máy - Đa VLAN)";

                if (_savedNormalBounds != Rectangle.Empty && _savedNormalBounds.Width >= 600)
                {
                    this.Bounds = _savedNormalBounds;
                }
                else
                {
                    Rectangle workArea = Screen.FromControl(this).WorkingArea;
                    int normW = 950;
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
                _lblFloatingStatus.Text = "⏸️ Đã dừng (" + _selectedWindowLabel + ")";
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

            if (IsCustomAgentsTargetSelected() && _selectedAgentIds.Count == 0)
            {
                ShowMultiAgentPickerDialog();
                if (_selectedAgentIds.Count == 0) return;
            }

            ApplyWindowCaptureAffinity();

            _framesSent = 0;
            _startTime = DateTime.Now;
            _activeTargetFilter = ComputeTargetFilterFromSelection();

            lock (_sendSync)
            {
                _isBroadcasting = true;
                _client.StartBroadcast(_activeTargetFilter);
            }

            _btnToggleBroadcast.Text = "🛑 DỪNG PHÁT SÓNG";
            ModernTheme.ApplyToButton(_btnToggleBroadcast, ModernTheme.AccentRed, Color.White);
            _lblStatus.Text = "Đang phát (" + _selectedWindowLabel + ")...";
            UpdateFloatingBarState();

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

                        if (!_isBroadcasting) break;

                        if (screenBytes != null && screenBytes.Length > 0)
                        {
                            lock (_sendSync)
                            {
                                if (!_isBroadcasting) break;
                                _client.SendBroadcastFrame(screenBytes, _activeTargetFilter);
                                _framesSent++;
                            }

                            // Update local preview
                            if (_framesSent % 2 == 0 && _isBroadcasting)
                            {
                                if (this.IsHandleCreated && !this.IsDisposed)
                                {
                                    this.BeginInvoke(new Action(delegate
                                    {
                                        try
                                        {
                                            if (this.IsDisposed || !_isBroadcasting) return;
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
                IsBackground = true,
                Name = "Master_BroadcastSenderLoop"
            };
            _broadcastThread.Start();
        }

        private void StopBroadcasting(bool restoreFullViewIfAutoFloated)
        {
            string lastFilter = _activeTargetFilter;
            _isBroadcasting = false;

            // Wait briefly for capture loop to exit before sending StopBroadcast so no late frame overtakes StopBroadcast
            Thread t = _broadcastThread;
            if (t != null && t.IsAlive && Thread.CurrentThread != t)
            {
                try { t.Join(250); } catch { }
            }

            lock (_sendSync)
            {
                _isBroadcasting = false;
                if (!string.IsNullOrEmpty(lastFilter))
                {
                    _client.StopBroadcast(lastFilter);
                }
                // Also send global StopBroadcast to ensure all client viewers close reliably
                _client.StopBroadcast("");
            }

            // Send a second confirmation StopBroadcast after 180ms to guarantee no in-flight packet remains
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    Thread.Sleep(180);
                    lock (_sendSync)
                    {
                        if (!_isBroadcasting)
                        {
                            _client.StopBroadcast("");
                        }
                    }
                }
                catch { }
            });

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
