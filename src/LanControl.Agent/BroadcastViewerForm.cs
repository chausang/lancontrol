using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace LanControl.Agent
{
    public class BroadcastViewerForm : Form
    {
        private PictureBox _picBox;
        private Label _lblStatus;

        public BroadcastViewerForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "LAN Control - Màn Hình Trình Chiếu Của Quản Trị Viên (Teacher Broadcast)";
            this.Size = new Size(960, 560);
            this.MinimumSize = new Size(320, 200);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.WindowState = FormWindowState.Maximized;
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
        }

        public void UpdateFrame(byte[] imageBytes)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action<byte[]>(UpdateFrame), (object)imageBytes);
                return;
            }

            if (imageBytes == null || imageBytes.Length == 0) return;

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

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape || keyData == (Keys.Alt | Keys.F4))
            {
                // Let user minimize or keep in background if needed
                this.WindowState = FormWindowState.Normal;
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
