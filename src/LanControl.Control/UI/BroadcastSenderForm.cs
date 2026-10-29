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
        private Button _btnToggleBroadcast;
        private Label _lblStatus;
        private PictureBox _picPreview;

        private bool _isBroadcasting = false;
        private Thread _broadcastThread;
        private int _framesSent = 0;
        private DateTime _startTime;

        public BroadcastSenderForm(ControlClient client, List<AgentInfo> allAgents)
        {
            _client = client;
            _allAgents = allAgents ?? new List<AgentInfo>();
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "LAN Control - Trình Chiếu & Phát Sóng Màn Hình Giáo Viên (1-to-Many Screen Broadcaster)";
            this.Size = new Size(820, 580);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = ModernTheme.BackgroundDark;
            this.ForeColor = ModernTheme.TextPrimary;

            // Header Config Panel
            Panel topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 85,
                BackColor = ModernTheme.PanelDark,
                Padding = new Padding(12)
            };

            Label lblTarget = new Label
            {
                Text = "Phạm vi phát sóng:",
                Location = new Point(15, 15),
                AutoSize = true,
                Font = ModernTheme.RegularFont,
                ForeColor = ModernTheme.TextSecondary
            };

            _cboTarget = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(140, 12),
                Width = 220,
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
                Location = new Point(380, 15),
                AutoSize = true,
                Font = ModernTheme.RegularFont,
                ForeColor = ModernTheme.TextSecondary
            };

            _cboFps = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(470, 12),
                Width = 85,
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
                Location = new Point(575, 15),
                AutoSize = true,
                Font = ModernTheme.RegularFont,
                ForeColor = ModernTheme.TextSecondary
            };

            _cboQuality = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(660, 12),
                Width = 95,
                BackColor = ModernTheme.CardDark,
                ForeColor = ModernTheme.TextPrimary,
                Font = ModernTheme.RegularFont
            };
            _cboQuality.Items.Add("40% (Tiết kiệm)");
            _cboQuality.Items.Add("60% (Cân bằng)");
            _cboQuality.Items.Add("80% (Sắc nét)");
            _cboQuality.SelectedIndex = 1; // 60%

            _btnToggleBroadcast = new Button
            {
                Text = "📡 BẮT ĐẦU PHÁT SÓNG MÀN HÌNH",
                Location = new Point(140, 46),
                Size = new Size(280, 32)
            };
            ModernTheme.ApplyToButton(_btnToggleBroadcast, ModernTheme.AccentGreen, Color.White);
            _btnToggleBroadcast.Click += OnToggleBroadcast;

            _lblStatus = new Label
            {
                Text = "Trạng thái: Chưa phát sóng.",
                Location = new Point(435, 53),
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
            topPanel.Controls.Add(_btnToggleBroadcast);
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

            this.FormClosing += (s, e) =>
            {
                if (_isBroadcasting)
                {
                    StopBroadcasting();
                }
            };
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
            _isBroadcasting = true;
            _framesSent = 0;
            _startTime = DateTime.Now;

            _btnToggleBroadcast.Text = "🛑 DỪNG PHÁT SÓNG";
            ModernTheme.ApplyToButton(_btnToggleBroadcast, ModernTheme.AccentRed, Color.White);
            _lblStatus.Text = "Đang phát sóng màn hình quản trị viên...";

            _client.StartBroadcast();

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
                        byte[] screenBytes = ScreenCapture.CaptureDesktopJpeg(quality, true, 0, 0);
                        if (screenBytes != null && screenBytes.Length > 0)
                        {
                            _client.SendBroadcastFrame(screenBytes);
                            _framesSent++;

                            // Update local preview
                            if (_framesSent % 3 == 0)
                            {
                                if (this.InvokeRequired)
                                {
                                    this.BeginInvoke(new Action(delegate
                                    {
                                        try
                                        {
                                            using (MemoryStream ms = new MemoryStream(screenBytes))
                                            {
                                                Image newImg = Image.FromStream(ms);
                                                Image oldImg = _picPreview.Image;
                                                _picPreview.Image = newImg;
                                                if (oldImg != null) oldImg.Dispose();
                                            }

                                            TimeSpan elapsed = DateTime.Now - _startTime;
                                            _lblStatus.Text = string.Format("Đang phát sóng: {0:mm\\:ss} | {1} frames | {2:F1} FPS",
                                                elapsed, _framesSent, (double)_framesSent / Math.Max(1, elapsed.TotalSeconds));
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
            _client.StopBroadcast();

            _btnToggleBroadcast.Text = "📡 BẮT ĐẦU PHÁT SÓNG MÀN HÌNH";
            ModernTheme.ApplyToButton(_btnToggleBroadcast, ModernTheme.AccentGreen, Color.White);
            _lblStatus.Text = "Đã dừng phát sóng.";
        }
    }
}
