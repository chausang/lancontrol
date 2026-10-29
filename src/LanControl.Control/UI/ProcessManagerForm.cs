using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using LanControl.Core.Models;
using LanControl.Core.Network;

namespace LanControl.Control.UI
{
    public class ProcessManagerForm : Form
    {
        private readonly ControlClient _client;
        private readonly AgentInfo _agent;
        private ListView _lvProcesses;
        private TextBox _txtSearch;
        private Button _btnRefresh;
        private Button _btnKill;
        private Label _lblCount;
        private List<ProcessItem> _allProcesses = new List<ProcessItem>();

        public ProcessManagerForm(ControlClient client, AgentInfo agent)
        {
            _client = client;
            _agent = agent;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = string.Format("Quản Lý Tác Vụ Từ Xa (Remote Task Manager) - {0} ({1})", _agent.Hostname, _agent.IpAddress);
            this.Size = new Size(780, 560);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = ModernTheme.BackgroundDark;
            this.ForeColor = ModernTheme.TextPrimary;

            // Toolbar Panel
            Panel topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = ModernTheme.PanelDark,
                Padding = new Padding(10, 8, 10, 8)
            };

            Label lblSearch = new Label
            {
                Text = "🔍 Tìm kiếm:",
                Font = ModernTheme.RegularFont,
                ForeColor = ModernTheme.TextSecondary,
                Location = new Point(12, 14),
                AutoSize = true
            };

            _txtSearch = new TextBox
            {
                Location = new Point(95, 11),
                Width = 200,
                BackColor = ModernTheme.CardDark,
                ForeColor = ModernTheme.TextPrimary,
                Font = ModernTheme.RegularFont,
                BorderStyle = BorderStyle.FixedSingle
            };
            _txtSearch.TextChanged += (s, e) => ApplySearchFilter();

            _btnRefresh = new Button
            {
                Text = "🔄 Làm Mới",
                Location = new Point(315, 8),
                Size = new Size(100, 30)
            };
            ModernTheme.ApplyToButton(_btnRefresh, ModernTheme.AccentBlue, Color.White);
            _btnRefresh.Click += (s, e) => RefreshProcesses();

            _btnKill = new Button
            {
                Text = "⛔ Kết Thúc Tiến Trình (Kill)",
                Location = new Point(425, 8),
                Size = new Size(185, 30)
            };
            ModernTheme.ApplyToButton(_btnKill, ModernTheme.AccentRed, Color.White);
            _btnKill.Click += OnKillClicked;

            _lblCount = new Label
            {
                Text = "Đang tải dữ liệu...",
                Font = ModernTheme.SmallFont,
                ForeColor = ModernTheme.AccentGreen,
                Location = new Point(620, 15),
                AutoSize = true
            };

            topPanel.Controls.Add(lblSearch);
            topPanel.Controls.Add(_txtSearch);
            topPanel.Controls.Add(_btnRefresh);
            topPanel.Controls.Add(_btnKill);
            topPanel.Controls.Add(_lblCount);

            // ListView Processes
            _lvProcesses = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                BackColor = ModernTheme.CardDark,
                ForeColor = ModernTheme.TextPrimary,
                Font = ModernTheme.RegularFont,
                BorderStyle = BorderStyle.None
            };

            _lvProcesses.Columns.Add("PID", 70, HorizontalAlignment.Right);
            _lvProcesses.Columns.Add("Tên Tiến Trình (Process Name)", 220, HorizontalAlignment.Left);
            _lvProcesses.Columns.Add("Tiêu Đề Cửa Sổ (Window Title)", 300, HorizontalAlignment.Left);
            _lvProcesses.Columns.Add("RAM Sử Dụng", 120, HorizontalAlignment.Right);

            this.Controls.Add(_lvProcesses);
            this.Controls.Add(topPanel);

            this.Load += (s, e) =>
            {
                _client.CommandResponseReceived += OnCommandResponse;
                RefreshProcesses();
            };

            this.FormClosing += (s, e) =>
            {
                _client.CommandResponseReceived -= OnCommandResponse;
            };
        }

        private void RefreshProcesses()
        {
            _lblCount.Text = "Đang lấy danh sách tiến trình...";
            _client.SendCommand(_agent.AgentId, CommandType.GetProcessList);
        }

        private void OnKillClicked(object sender, EventArgs e)
        {
            if (_lvProcesses.SelectedItems.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn tiến trình cần dừng!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ListViewItem item = _lvProcesses.SelectedItems[0];
            string pidStr = item.Text;
            string procName = item.SubItems[1].Text;

            if (MessageBox.Show(string.Format("Bạn có chắc muốn tắt tiến trình '{0}' (PID: {1})?", procName, pidStr),
                "Xác nhận dừng tiến trình", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                _client.SendCommand(_agent.AgentId, CommandType.KillProcess, pidStr);
                RefreshProcesses();
            }
        }

        private void OnCommandResponse(CommandResponse resp)
        {
            if (resp == null || resp.AgentId != _agent.AgentId) return;

            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action<CommandResponse>(OnCommandResponse), resp);
                return;
            }

            if (resp.Success && !string.IsNullOrEmpty(resp.Output) && resp.Output.TrimStart().StartsWith("["))
            {
                try
                {
                    _allProcesses = PacketSerializer.Deserialize<List<ProcessItem>>(resp.Output);
                    ApplySearchFilter();
                }
                catch { }
            }
        }

        private void ApplySearchFilter()
        {
            string filter = _txtSearch.Text.Trim().ToLowerInvariant();
            _lvProcesses.BeginUpdate();
            _lvProcesses.Items.Clear();

            int visibleCount = 0;
            foreach (ProcessItem p in _allProcesses)
            {
                if (string.IsNullOrEmpty(filter) ||
                    p.Name.ToLowerInvariant().Contains(filter) ||
                    p.Title.ToLowerInvariant().Contains(filter) ||
                    p.Id.ToString().Contains(filter))
                {
                    ListViewItem lvi = new ListViewItem(p.Id.ToString());
                    lvi.SubItems.Add(p.Name);
                    lvi.SubItems.Add(p.Title);
                    lvi.SubItems.Add(p.MemoryFormatted);
                    _lvProcesses.Items.Add(lvi);
                    visibleCount++;
                }
            }

            _lvProcesses.EndUpdate();
            _lblCount.Text = string.Format("Tổng số: {0} tiến trình ({1} hiển thị)", _allProcesses.Count, visibleCount);
        }
    }
}
