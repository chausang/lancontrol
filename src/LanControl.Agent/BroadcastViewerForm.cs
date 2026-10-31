using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace LanControl.Agent
{
    public class BroadcastViewerForm : Form
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("user32.dll")]
        private static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);

        private const uint SC_CLOSE = 0xF060;
        private const uint SC_MINIMIZE = 0xF020;
        private const uint MF_BYCOMMAND = 0x00000000;
        private const uint MF_GRAYED = 0x00000001;
        private const uint MF_DISABLED = 0x00000002;
        private const int WM_SYSCOMMAND = 0x0112;

        private readonly Func<bool> _shouldStayOpenCheck;
        private PictureBox _picBox;
        private Label _lblStatus;
        private Timer _keepAliveTimer;
        private volatile bool _allowProgrammaticClose = false;
        private FormWindowState _desiredWindowState = FormWindowState.Maximized;

        public BroadcastViewerForm(Func<bool> shouldStayOpenCheck = null)
        {
            _shouldStayOpenCheck = shouldStayOpenCheck;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "LAN Control - Màn Hình Trình Chiếu Của Quản Trị Viên (Đang phát sóng - Không thể đóng)";
            this.Size = new Size(960, 560);
            this.MinimumSize = new Size(360, 220);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.WindowState = FormWindowState.Maximized;
            this.MinimizeBox = false;
            this.BackColor = Color.Black;
            this.TopMost = true;
            this.DoubleBuffered = true;

            _picBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black
            };

            _lblStatus = new Label
            {
                Text = "📡 Đang kết nối luồng phát sóng màn hình của Quản trị viên...",
                Font = new Font("Segoe UI", 12, FontStyle.Regular),
                ForeColor = Color.Gold,
                BackColor = Color.FromArgb(160, 0, 0, 0),
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleCenter,
                Height = 35
            };

            this.Controls.Add(_picBox);
            this.Controls.Add(_lblStatus);
            _lblStatus.BringToFront();

            this.HandleCreated += (s, e) => DisableCloseButton();
            this.Shown += (s, e) =>
            {
                DisableCloseButton();
                StartWatchdogTimer();
            };
        }

        private void StartWatchdogTimer()
        {
            if (_keepAliveTimer != null) return;

            _keepAliveTimer = new Timer { Interval = 250 };
            _keepAliveTimer.Tick += (s, e) =>
            {
                if (this.IsDisposed) return;

                // Auto-close immediately if broadcast stopped or timed out
                if (_allowProgrammaticClose || (_shouldStayOpenCheck != null && !_shouldStayOpenCheck()))
                {
                    AllowCloseAndDispose();
                    return;
                }

                try
                {
                    if (!this.Visible)
                    {
                        this.Show();
                    }
                    if (this.WindowState == FormWindowState.Minimized)
                    {
                        this.WindowState = _desiredWindowState;
                    }
                    if (!this.TopMost)
                    {
                        this.TopMost = true;
                    }
                }
                catch { }
            };
            _keepAliveTimer.Start();
        }

        private void DisableCloseButton()
        {
            try
            {
                if (this.IsHandleCreated)
                {
                    IntPtr hMenu = GetSystemMenu(this.Handle, false);
                    if (hMenu != IntPtr.Zero)
                    {
                        EnableMenuItem(hMenu, SC_CLOSE, MF_BYCOMMAND | MF_GRAYED | MF_DISABLED);
                    }
                }
            }
            catch { }
        }

        public void AllowCloseAndDispose()
        {
            _allowProgrammaticClose = true;

            if (this.IsDisposed) return;

            if (this.InvokeRequired)
            {
                try
                {
                    if (this.IsHandleCreated)
                    {
                        this.BeginInvoke(new MethodInvoker(AllowCloseAndDispose));
                    }
                }
                catch { }
                return;
            }

            try
            {
                if (_keepAliveTimer != null)
                {
                    _keepAliveTimer.Stop();
                    _keepAliveTimer.Dispose();
                    _keepAliveTimer = null;
                }
            }
            catch { }

            try
            {
                this.Hide();
                this.Close();
                this.Dispose();
            }
            catch { }
        }

        public void UpdateFrame(byte[] imageBytes)
        {
            if (_allowProgrammaticClose || this.IsDisposed) return;

            if (this.InvokeRequired)
            {
                try
                {
                    if (this.IsHandleCreated)
                    {
                        this.BeginInvoke(new Action<byte[]>(UpdateFrame), (object)imageBytes);
                    }
                }
                catch { }
                return;
            }

            if (_allowProgrammaticClose || this.IsDisposed || imageBytes == null || imageBytes.Length == 0) return;

            try
            {
                using (MemoryStream ms = new MemoryStream(imageBytes))
                {
                    Image newImg = Image.FromStream(ms);
                    Image oldImg = _picBox.Image;
                    _picBox.Image = newImg;
                    if (oldImg != null) oldImg.Dispose();
                }

                if (_lblStatus.Visible)
                {
                    _lblStatus.Visible = false;
                }
            }
            catch { }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_SYSCOMMAND)
            {
                uint cmd = (uint)(m.WParam.ToInt64() & 0xFFF0);
                if ((cmd == SC_CLOSE || cmd == SC_MINIMIZE) && !_allowProgrammaticClose)
                {
                    // Block closing or minimizing by client user
                    return;
                }
            }
            base.WndProc(ref m);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Block Alt+F4 and Escape from closing or hiding the broadcast window
            if (keyData == (Keys.Alt | Keys.F4) || keyData == Keys.Escape)
            {
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (this.WindowState != FormWindowState.Minimized)
            {
                _desiredWindowState = this.WindowState;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!_allowProgrammaticClose &&
                (e.CloseReason == CloseReason.UserClosing || e.CloseReason == CloseReason.None))
            {
                e.Cancel = true;
                return;
            }
            base.OnFormClosing(e);
        }
    }
}
