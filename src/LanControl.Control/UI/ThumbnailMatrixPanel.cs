using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using LanControl.Core.Models;

namespace LanControl.Control.UI
{
    public class ThumbnailMatrixPanel : UserControl
    {
        private FlowLayoutPanel _flow;
        private readonly Dictionary<string, ThumbnailCard> _cards = new Dictionary<string, ThumbnailCard>();
        private string _vlanFilter = "ALL";
        private string _searchFilter = "";

        public event Action<AgentInfo> RemoteDesktopRequested;
        public event Action<AgentInfo> LockScreenRequested;
        public event Action<AgentInfo> UnlockScreenRequested;
        public event Action<AgentInfo> RunScriptRequested;
        public event Action<AgentInfo> ProcessManagerRequested;
        public event Action<AgentInfo> FileManagerRequested;
        public event Action<AgentInfo> FileDistributionRequested;
        public event Action<AgentInfo> HardwareInventoryRequested;
        public event Action<AgentInfo> SendMessageRequested;
        public event Action<AgentInfo> RebootRequested;
        public event Action<AgentInfo> ShutdownRequested;

        public ThumbnailMatrixPanel()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = ModernTheme.BackgroundDark;

            _flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = ModernTheme.BackgroundDark,
                Padding = new Padding(10)
            };

            // Enable double buffering for smooth rendering
            typeof(FlowLayoutPanel).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null, _flow, new object[] { true });

            this.Controls.Add(_flow);
        }

        public void SetFilter(string vlan, string search)
        {
            _vlanFilter = string.IsNullOrEmpty(vlan) ? "ALL" : vlan.ToUpperInvariant();
            _searchFilter = string.IsNullOrEmpty(search) ? "" : search.Trim().ToLowerInvariant();
            ApplyFilters();
        }

        public void UpdateAgents(List<AgentInfo> agents)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<List<AgentInfo>>(UpdateAgents), agents);
                return;
            }

            _flow.SuspendLayout();

            HashSet<string> currentIds = new HashSet<string>();
            foreach (AgentInfo a in agents)
            {
                currentIds.Add(a.AgentId);

                ThumbnailCard card;
                if (_cards.TryGetValue(a.AgentId, out card))
                {
                    card.UpdateData(a);
                }
                else
                {
                    card = new ThumbnailCard(a);
                    card.RemoteDesktopRequested += (ag) => { if (RemoteDesktopRequested != null) RemoteDesktopRequested(ag); };
                    card.LockScreenRequested += (ag) => { if (LockScreenRequested != null) LockScreenRequested(ag); };
                    card.UnlockScreenRequested += (ag) => { if (UnlockScreenRequested != null) UnlockScreenRequested(ag); };
                    card.RunScriptRequested += (ag) => { if (RunScriptRequested != null) RunScriptRequested(ag); };
                    card.ProcessManagerRequested += (ag) => { if (ProcessManagerRequested != null) ProcessManagerRequested(ag); };
                    card.FileManagerRequested += (ag) => { if (FileManagerRequested != null) FileManagerRequested(ag); };
                    card.FileDistributionRequested += (ag) => { if (FileDistributionRequested != null) FileDistributionRequested(ag); };
                    card.HardwareInventoryRequested += (ag) => { if (HardwareInventoryRequested != null) HardwareInventoryRequested(ag); };
                    card.SendMessageRequested += (ag) => { if (SendMessageRequested != null) SendMessageRequested(ag); };
                    card.RebootRequested += (ag) => { if (RebootRequested != null) RebootRequested(ag); };
                    card.ShutdownRequested += (ag) => { if (ShutdownRequested != null) ShutdownRequested(ag); };

                    _cards[a.AgentId] = card;
                    _flow.Controls.Add(card);
                }
            }

            // Remove disconnected cards
            List<string> toRemove = new List<string>();
            foreach (KeyValuePair<string, ThumbnailCard> kv in _cards)
            {
                if (!currentIds.Contains(kv.Key))
                {
                    toRemove.Add(kv.Key);
                }
            }

            foreach (string id in toRemove)
            {
                ThumbnailCard card = _cards[id];
                _flow.Controls.Remove(card);
                card.Dispose();
                _cards.Remove(id);
            }

            ApplyFilters();
            _flow.ResumeLayout();
        }

        public void UpdateThumbnail(string agentId, byte[] imageBytes)
        {
            ThumbnailCard card;
            if (_cards.TryGetValue(agentId, out card))
            {
                card.UpdateThumbnail(imageBytes);
            }
        }

        public void UpdateHeartbeat(AgentInfo a)
        {
            ThumbnailCard card;
            if (_cards.TryGetValue(a.AgentId, out card))
            {
                card.UpdateData(a);
            }
        }

        private void ApplyFilters()
        {
            foreach (ThumbnailCard card in _cards.Values)
            {
                bool vlanMatch = (_vlanFilter == "ALL") ||
                    (!string.IsNullOrEmpty(card.Agent.VlanTag) && card.Agent.VlanTag.ToUpperInvariant().Contains(_vlanFilter));

                bool searchMatch = string.IsNullOrEmpty(_searchFilter) ||
                    (card.Agent.Hostname != null && card.Agent.Hostname.ToLowerInvariant().Contains(_searchFilter)) ||
                    (card.Agent.IpAddress != null && card.Agent.IpAddress.Contains(_searchFilter));

                card.Visible = vlanMatch && searchMatch;
            }
        }

        public List<AgentInfo> GetSelectedAgents()
        {
            List<AgentInfo> list = new List<AgentInfo>();
            foreach (ThumbnailCard card in _cards.Values)
            {
                if (card.Visible && card.IsSelected)
                {
                    list.Add(card.Agent);
                }
            }
            return list;
        }

        public List<AgentInfo> GetAllAgents()
        {
            List<AgentInfo> list = new List<AgentInfo>();
            foreach (ThumbnailCard card in _cards.Values)
            {
                if (card.Agent != null)
                {
                    list.Add(card.Agent);
                }
            }
            return list;
        }

        public void SelectAll(bool select)
        {
            foreach (ThumbnailCard card in _cards.Values)
            {
                if (card.Visible)
                {
                    card.IsSelected = select;
                }
            }
        }
    }
}
