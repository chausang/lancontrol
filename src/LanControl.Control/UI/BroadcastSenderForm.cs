using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using LanControl.Core.Models;
using LanControl.Core.Utils;

namespace LanControl.Control.UI
{
    public class BroadcastSenderForm : Form
    {
        private readonly ControlClient _client;
        private readonly List<AgentInfo> _allAgents;

        private ComboBox _cboTarget;
        private ComboBox _cboFps;
        private ComboBox _cboQuality;

        private RadioButton _radFullScreen;
        private RadioButton _radAppWindow;
        private ComboBox _cboAppWindow;
        private Button _btnRefreshApps;
        private CheckBox _chkBringToFront;

        private Button _btnToggleBroadcast;
        private Label _lblStatus;
        private PictureBox _picPreview;

        private bool _isBroadcasting = false;
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
            this.Size = new Size(880, 640);
            this.MinimumSize = new Size(840, 560);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = ModernTheme.BackgroundDark;
            this.ForeColor = ModernTheme.TextPrimary;

            // Header Config Panel (3 Rows)
            Panel topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 132,
                BackColor = ModernTheme.PanelDark,
                Padding = new Padding(12)
            };

            // --- ROW 1: Target VLAN, FPS, Quality ---
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
                Location = new Point(145, 11),
                Width = 240,
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
                Text = "Tốc độ (FPS):",
                Location = new Point(405, 14),
                AutoSize = true,
                Font = ModernTheme.RegularFont,
                ForeColor = ModernTheme.TextSecondary
            };

            _cboFps = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(495, 11),
                Width = 90,
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
                Location = new Point(605, 14),
                AutoSize = true,
                Font = ModernTheme.RegularFont,
                ForeColor = ModernTheme.TextSecondary
            };

            _cboQuality = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(690, 11),
                Width = 140,
                BackColor = ModernTheme.CardDark,
                ForeColor = ModernTheme.TextPrimary,
                Font = ModernTheme.RegularFont
            };
            _cboQuality.Items.Add("40% (Tiết kiệm)");
            _cboQuality.Items.Add("60% (Cân bằng)");
            _cboQuality.Items.Add("80% (Sắc nét)");
            _cboQuality.SelectedIndex = 1; // 60%

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
                Location = new Point(145, 48),
                AutoSize = true,
                Checked = true,
                Font = ModernTheme.SubHeaderFont,
                ForeColor = ModernTheme.TextPrimary
            };

            _radAppWindow = new RadioButton
            {
                Text = "🪟 Chọn 1 chương trình:",
                Location = new Point(295, 48),
                AutoSize = true,
                Checked = false,
                Font = ModernTheme.SubHeaderFont,
                ForeColor = ModernTheme.TextPrimary
            };

            _cboAppWindow = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(478, 46),
                Width = 250,
                BackColor = ModernTheme.CardDark,
                ForeColor = ModernTheme.TextPrimary,
                Font = ModernTheme.RegularFont,
                Enabled = false
            };

            _btnRefreshApps = new Button
            {
                Text = "🔄 Làm mới",
                Location = new Point(735, 45),
                Size = new Size(95, 27)
            };
            ModernTheme.ApplyToButton(_btnRefreshApps, ModernTheme.CardDark, ModernTheme.AccentCyan);
            _btnRefreshApps.Click += (s, e) => RefreshOpenWindowsList(true);

            _radFullScreen.CheckedChanged += (s, e) => OnSourceModeChanged();
            _radAppWindow.CheckedChanged += (s, e) => OnSourceModeChanged();
            _cboAppWindow.SelectedIndexChanged += (s, e) => OnSelectedAppChanged();

            // --- ROW 3: Start/Stop Button, Auto-Bring-To-Front & Status ---
            _btnToggleBroadcast = new Button
            {
                Text = "📡 BẮT ĐẦU PHÁT SÓNG MÀN HÌNH",
                Location = new Point(145, 86),
                Size = new Size(270, 34)
            };
            ModernTheme.ApplyToButton(_btnToggleBroadcast, ModernTheme.AccentGreen, Color.White);
            _btnToggleBroadcast.Click += OnToggleBroadcast;

            _chkBringToFront = new CheckBox
            {
                Text = "Đưa chương trình lên trên khi phát",
                Location = new Point(430, 94),
                AutoSize = true,
                Checked = true,
                Font = ModernTheme.SmallFont,
                ForeColor = ModernTheme.TextSecondary,
                Enabled = false
            };

            _lblStatus = new Label
            {
                Text = "Trạng thái: Chưa phát sóng (Chế độ: Toàn màn hình).",
                Location = new Point(645, 94),
                AutoSize = true,
                Font = ModernTheme.RegularFont,
                ForeColor = ModernTheme.AccentYellow
            };

            topPanel.Controls.Add(lblTarget);
            topPanel.Controls.Add(_cboTarget);
            topPanel.Controls.Add(lblFps);
            topPanel.Controls.Add(_cboFps);
            topPanel.Controls.Add(lblQuality);
            topPanel.Controls.Add(_cboQuality);

            topPanel.Controls.Add(lblSource);
            topPanel.Controls.Add(_radFullScreen);
            topPanel.Controls.Add(_radAppWindow);
            topPanel.Controls.Add(_cboAppWindow);
            topPanel.Controls.Add(_btnRefreshApps);

            topPanel.Controls.Add(_btnToggleBroadcast);
            topPanel.Controls.Add(_chkBringToFront);
            topPanel.Controls.Add(_lblStatus);

            // Preview Box
            _picPreview = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black
            };

            this.Controls.Add(_picPreview);
            this.Controls.Add(topPanel);

            this.Shown += (s, e) =>
            {
                RefreshOpenWindowsList(false);
                CapturePreviewSnapshotOnce();
            };

            this.FormClosing += (s, e) =>
            {
                if (_isBroadcasting)
                {
                    StopBroadcasting();
                }
            };
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
                _lblStatus.Text = "Chưa phát sóng (" + _selectedWindowLabel + ").";
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
                StopBroadcasting();
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

            if (_captureAppWindowMode && _selectedWindowHandle != IntPtr.Zero)
            {
                ScreenCapture.BringWindowToFrontIfMinimized(_selectedWindowHandle, _chkBringToFront.Checked);
            }

            _isBroadcasting = true;
            _framesSent = 0;
            _startTime = DateTime.Now;
            _activeTargetVlan = GetSelectedTargetVlan();

            _btnToggleBroadcast.Text = "🛑 DỪNG PHÁT SÓNG";
            ModernTheme.ApplyToButton(_btnToggleBroadcast, ModernTheme.AccentRed, Color.White);
            _lblStatus.Text = "Đang phát sóng (" + _selectedWindowLabel + ")...";

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
                            if (_framesSent % 3 == 0)
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
                                            _lblStatus.Text = string.Format("Đang phát [{0}]: {1:mm\\:ss} | {2:F1} FPS",
                                                _selectedWindowLabel, elapsed, (double)_framesSent / Math.Max(1, elapsed.TotalSeconds));
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

        private void StopBroadcasting()
        {
            _isBroadcasting = false;
            _client.StopBroadcast(_activeTargetVlan);

            _btnToggleBroadcast.Text = _radAppWindow.Checked
                ? "📡 BẮT ĐẦU PHÁT SÓNG CHƯƠNG TRÌNH"
                : "📡 BẮT ĐẦU PHÁT SÓNG MÀN HÌNH";
            ModernTheme.ApplyToButton(_btnToggleBroadcast, ModernTheme.AccentGreen, Color.White);
            _lblStatus.Text = "Đã dừng phát sóng.";
        }
    }
}
