using System;
using System.Drawing;
using System.Windows.Forms;

namespace LanControl.Agent
{
    public class LockScreenForm : Form
    {
        private Label _lblMessage;
        private Label _lblTitle;

        public LockScreenForm(string message = "MÁY TÍNH ĐANG ĐƯỢC KHÓA BỞI QUẢN TRỊ VIÊN")
        {
            InitializeComponent(message);
        }

        private void InitializeComponent(string message)
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.TopMost = true;
            this.BackColor = Color.FromArgb(18, 22, 30);
            this.ShowInTaskbar = false;
            this.DoubleBuffered = true;

            TableLayoutPanel panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3
            };
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 60));

            _lblTitle = new Label
            {
                Text = "🔒 LAN CONTROL LOCK SCREEN",
                Font = new Font("Segoe UI", 28, FontStyle.Bold),
                ForeColor = Color.FromArgb(240, 100, 100),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                AutoSize = false,
                Height = 60
            };

            _lblMessage = new Label
            {
                Text = string.IsNullOrEmpty(message) ? "Máy tính tạm thời bị khóa theo yêu cầu của Quản trị viên." : message,
                Font = new Font("Segoe UI", 16, FontStyle.Regular),
                ForeColor = Color.FromArgb(200, 210, 225),
                TextAlign = ContentAlignment.TopCenter,
                Dock = DockStyle.Fill,
                AutoSize = false,
                Height = 80
            };

            panel.Controls.Add(_lblTitle, 0, 1);
            panel.Controls.Add(_lblMessage, 0, 2);

            this.Controls.Add(panel);
        }

        public void UpdateMessage(string msg)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string>(UpdateMessage), msg);
                return;
            }
            if (_lblMessage != null)
            {
                _lblMessage.Text = msg;
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Block Alt+F4, Windows keys, etc.
            if (keyData == (Keys.Alt | Keys.F4) || keyData == Keys.Escape)
            {
                return true; // Ignore
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Only allow closing programmatically
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                return;
            }
            base.OnFormClosing(e);
        }
    }
}
