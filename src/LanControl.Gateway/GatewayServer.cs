using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using LanControl.Core.Models;
using LanControl.Core.Network;
using LanControl.Core.Utils;

namespace LanControl.Gateway
{
    public class GatewayServer
    {
        private readonly int _port;
        private TcpListener _listener;
        private bool _isRunning;
        private readonly Dictionary<string, ClientConnection> _agents = new Dictionary<string, ClientConnection>();
        private readonly List<ClientConnection> _controls = new List<ClientConnection>();
        private readonly object _stateLock = new object();
        private Timer _heartbeatCheckTimer;

        public int Port { get { return _port; } }
        public bool IsRunning { get { return _isRunning; } }
        public event Action AgentsUpdated;

        public GatewayServer(int port = 8443)
        {
            _port = port;
        }

        public void Start()
        {
            _isRunning = true;
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start(100);

            Logger.Success("Gateway Server started on 0.0.0.0:{0} (Multi-VLAN Reverse Connection Ready)", _port);

            Thread acceptThread = new Thread(AcceptLoop)
            {
                IsBackground = true,
                Name = "Gateway_AcceptLoop"
            };
            acceptThread.Start();

            _heartbeatCheckTimer = new Timer(CheckStaleAgents, null, 5000, 5000);
        }

        public void Stop()
        {
            _isRunning = false;
            try { _heartbeatCheckTimer.Dispose(); } catch { }
            try { _listener.Stop(); } catch { }

            lock (_stateLock)
            {
                foreach (ClientConnection conn in _agents.Values)
                {
                    conn.Channel.Close();
                }
                _agents.Clear();

                foreach (ClientConnection conn in _controls)
                {
                    conn.Channel.Close();
                }
                _controls.Clear();
            }

            Logger.Info("Gateway Server stopped.");
        }

        private void AcceptLoop()
        {
            while (_isRunning)
            {
                try
                {
                    TcpClient client = _listener.AcceptTcpClient();
                    NetworkChannel channel = new NetworkChannel(client);
                    channel.PacketReceived += OnPacketReceived;
                    channel.Disconnected += OnChannelDisconnected;
                    channel.StartReceiving();
                }
                catch (Exception)
                {
                    if (!_isRunning) break;
                }
            }
        }

        private void OnPacketReceived(NetworkChannel channel, Packet packet)
        {
            try
            {
                switch (packet.Type)
                {
                    case PacketType.Register:
                        HandleRegister(channel, packet);
                        break;

                    case PacketType.Heartbeat:
                        HandleHeartbeat(channel, packet);
                        break;

                    case PacketType.ScreenFrame:
                        HandleScreenFrame(channel, packet);
                        break;

                    case PacketType.InputEvent:
                        HandleInputEvent(channel, packet);
                        break;

                    case PacketType.CommandRequest:
                        HandleCommandRequest(channel, packet);
                        break;

                    case PacketType.CommandResponse:
                        HandleCommandResponse(channel, packet);
                        break;

                    case PacketType.BroadcastFrame:
                    case PacketType.StartBroadcast:
                    case PacketType.StopBroadcast:
                        HandleBroadcast(channel, packet);
                        break;

                    case PacketType.StartScreenStream:
                    case PacketType.StopScreenStream:
                        HandleStreamControl(channel, packet);
                        break;

                    case PacketType.ScanSubnetRequest:
                        HandleScanSubnet(channel, packet);
                        break;

                    case PacketType.HelpRequest:
                        HandleHelpRequest(channel, packet);
                        break;

                    case PacketType.HelpResponse:
                        HandleHelpResponse(channel, packet);
                        break;

                    case PacketType.FileTransferRequest:
                    case PacketType.FileListRequest:
                        HandleFileTransferDispatch(channel, packet);
                        break;

                    case PacketType.FileTransferChunk:
                        ClientConnection conn = channel.Tag as ClientConnection;
                        if (conn != null && conn.Type == ClientType.Agent)
                        {
                            ForwardToControls(packet);
                        }
                        else
                        {
                            HandleFileTransferDispatch(channel, packet);
                        }
                        break;

                    case PacketType.FileTransferResponse:
                    case PacketType.FileListResponse:
                        ForwardToControls(packet);
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error processing packet {0}: {1}", packet.Type, ex.Message);
            }
        }

        private void HandleRegister(NetworkChannel channel, Packet packet)
        {
            string payload = packet.PayloadJson;
            if (payload.Contains("\"AgentId\""))
            {
                // It is an Agent registering
                AgentInfo agent = packet.GetPayload<AgentInfo>();
                if (agent == null) return;

                ClientConnection conn = new ClientConnection(agent.AgentId, ClientType.Agent, channel)
                {
                    Agent = agent
                };
                channel.Tag = conn;

                lock (_stateLock)
                {
                    string duplicateKey = null;
                    foreach (KeyValuePair<string, ClientConnection> pair in _agents)
                    {
                        if (pair.Key == agent.AgentId ||
                            (pair.Value.Agent != null &&
                             pair.Value.Agent.Hostname.Equals(agent.Hostname, StringComparison.OrdinalIgnoreCase) &&
                             pair.Value.Agent.IpAddress == agent.IpAddress &&
                             pair.Value.Agent.VlanTag == agent.VlanTag))
                        {
                            duplicateKey = pair.Key;
                            break;
                        }
                    }

                    if (duplicateKey != null)
                    {
                        try { _agents[duplicateKey].Channel.Close(); } catch { }
                        _agents.Remove(duplicateKey);
                    }
                    _agents[agent.AgentId] = conn;
                }

                Logger.Success("Agent Registered: [{0}] {1} (IP: {2}, VLAN: {3})", 
                    agent.AgentId.Substring(0, Math.Min(6, agent.AgentId.Length)),
                    agent.Hostname, agent.IpAddress, agent.VlanTag);

                BroadcastAgentListToControls();
            }
            else
            {
                // It is a Master Control Console registering
                ClientConnection conn = new ClientConnection(Guid.NewGuid().ToString("N"), ClientType.Control, channel);
                channel.Tag = conn;

                lock (_stateLock)
                {
                    if (!_controls.Contains(conn))
                        _controls.Add(conn);
                }

                Logger.Info("Master Control Console Connected from {0}", channel.RemoteEndPoint);
                SendAgentListToControl(channel);
            }
        }

        private void HandleHeartbeat(NetworkChannel channel, Packet packet)
        {
            ClientConnection conn = channel.Tag as ClientConnection;
            if (conn != null && conn.Type == ClientType.Agent)
            {
                AgentInfo update = packet.GetPayload<AgentInfo>();
                if (update != null && conn.Agent != null)
                {
                    conn.Agent.CpuUsagePercent = update.CpuUsagePercent;
                    conn.Agent.AvailableMemoryMb = update.AvailableMemoryMb;
                    conn.Agent.IsLocked = update.IsLocked;
                    conn.Agent.IsStreaming = update.IsStreaming;
                    conn.LastSeen = DateTime.Now;
                }

                // Forward heartbeat to all master consoles
                ForwardToControls(packet);
            }
        }

        private void HandleScreenFrame(NetworkChannel channel, Packet packet)
        {
            // Relay screen frame (thumbnail or interactive) from agent to master controls
            ForwardToControls(packet);
        }

        private void HandleInputEvent(NetworkChannel channel, Packet packet)
        {
            // Control sent input event to Agent
            InputEvent ev = packet.GetPayload<InputEvent>();
            if (ev != null && !string.IsNullOrEmpty(ev.AgentId))
            {
                ClientConnection agentConn;
                lock (_stateLock)
                {
                    _agents.TryGetValue(ev.AgentId, out agentConn);
                }

                if (agentConn != null && agentConn.Channel.IsConnected)
                {
                    agentConn.Channel.Send(packet);
                }
            }
        }

        private void HandleCommandRequest(NetworkChannel channel, Packet packet)
        {
            CommandRequest req = packet.GetPayload<CommandRequest>();
            if (req == null) return;

            Logger.Info("Command Dispatch: {0} for Agent {1} (VLAN: {2})", req.Command, req.AgentId ?? "ALL", req.TargetVlan ?? "ALL");

            lock (_stateLock)
            {
                // Specific agent
                if (!string.IsNullOrEmpty(req.AgentId))
                {
                    ClientConnection agentConn;
                    if (_agents.TryGetValue(req.AgentId, out agentConn))
                    {
                        agentConn.Channel.Send(packet);
                    }
                }
                else
                {
                    // Mass command by VLAN or ALL
                    foreach (ClientConnection agentConn in _agents.Values)
                    {
                        if (string.IsNullOrEmpty(req.TargetVlan) ||
                            req.TargetVlan.Equals("All", StringComparison.OrdinalIgnoreCase) ||
                            (agentConn.Agent != null && agentConn.Agent.VlanTag.Equals(req.TargetVlan, StringComparison.OrdinalIgnoreCase)))
                        {
                            agentConn.Channel.Send(packet);
                        }
                    }
                }
            }
        }

        private void HandleCommandResponse(NetworkChannel channel, Packet packet)
        {
            // Relay response from Agent back to Control consoles
            ForwardToControls(packet);
        }

        private void HandleBroadcast(NetworkChannel channel, Packet packet)
        {
            // Master console broadcasting screen frame to all or selected agents
            lock (_stateLock)
            {
                foreach (ClientConnection agentConn in _agents.Values)
                {
                    agentConn.Channel.Send(packet);
                }
            }
        }

        private void HandleStreamControl(NetworkChannel channel, Packet packet)
        {
            string agentId = packet.PayloadJson;
            if (string.IsNullOrEmpty(agentId)) return;

            lock (_stateLock)
            {
                ClientConnection agentConn;
                if (_agents.TryGetValue(agentId, out agentConn))
                {
                    agentConn.Channel.Send(packet);
                }
            }
        }

        private void HandleScanSubnet(NetworkChannel channel, Packet packet)
        {
            ScanConfig config = packet.GetPayload<ScanConfig>();
            if (config == null) config = new ScanConfig();

            Logger.Info("Running Multi-Subnet Scan on Gateway: Range [{0} - {1}], CIDR [{2}], Ports [{3}]",
                config.StartIp, config.EndIp, config.Cidr, config.PortList);

            ThreadPool.QueueUserWorkItem(delegate
            {
                SubnetScanner scanner = new SubnetScanner();
                List<ScanHostResult> results = scanner.Scan(config);

                Packet resp = Packet.Create(PacketType.ScanSubnetResponse, results);
                channel.Send(resp);
            });
        }

        private void HandleHelpRequest(NetworkChannel channel, Packet packet)
        {
            // Agent requested help -> Forward to all Master Control consoles
            ForwardToControls(packet);
            Logger.Info("Help Request forwarded to Master Consoles from Agent {0}", channel.RemoteEndPoint);
        }

        private void HandleHelpResponse(NetworkChannel channel, Packet packet)
        {
            // Master console responded to Help Request -> Forward to specific Agent
            HelpResponse resp = packet.GetPayload<HelpResponse>();
            if (resp != null && !string.IsNullOrEmpty(resp.AgentId))
            {
                ClientConnection agentConn;
                lock (_stateLock)
                {
                    _agents.TryGetValue(resp.AgentId, out agentConn);
                }

                if (agentConn != null && agentConn.Channel.IsConnected)
                {
                    agentConn.Channel.Send(packet);
                    Logger.Info("Help Response ({0}) routed to Agent {1}", resp.Accepted ? "ACCEPTED" : "DECLINED", resp.AgentId);
                }
            }
        }

        private void HandleFileTransferDispatch(NetworkChannel channel, Packet packet)
        {
            string agentId = null;
            string targetVlan = null;

            if (packet.Type == PacketType.FileTransferRequest)
            {
                FileTransferRequest req = packet.GetPayload<FileTransferRequest>();
                if (req != null)
                {
                    agentId = req.AgentId;
                    targetVlan = req.TargetVlan;
                }
            }
            else if (packet.Type == PacketType.FileTransferChunk)
            {
                FileChunkMeta meta = packet.GetPayload<FileChunkMeta>();
                if (meta != null)
                {
                    agentId = meta.AgentId;
                }
            }
            else if (packet.Type == PacketType.FileListRequest)
            {
                RemoteDirectoryListRequest req = packet.GetPayload<RemoteDirectoryListRequest>();
                if (req != null)
                {
                    agentId = req.AgentId;
                }
            }

            lock (_stateLock)
            {
                if (!string.IsNullOrEmpty(agentId))
                {
                    ClientConnection agentConn;
                    if (_agents.TryGetValue(agentId, out agentConn) && agentConn.Channel.IsConnected)
                    {
                        agentConn.Channel.Send(packet);
                    }
                }
                else
                {
                    // Mass file distribution: forward to all agents or matching VLAN
                    foreach (ClientConnection agentConn in _agents.Values)
                    {
                        if (agentConn.Channel != null && agentConn.Channel.IsConnected)
                        {
                            if (string.IsNullOrEmpty(targetVlan) ||
                                targetVlan.Equals("All", StringComparison.OrdinalIgnoreCase) ||
                                (agentConn.Agent != null && agentConn.Agent.VlanTag.Equals(targetVlan, StringComparison.OrdinalIgnoreCase)))
                            {
                                agentConn.Channel.Send(packet);
                            }
                        }
                    }
                }
            }
        }

        private void OnChannelDisconnected(NetworkChannel channel, Exception ex)
        {
            ClientConnection conn = channel.Tag as ClientConnection;
            if (conn == null) return;

            lock (_stateLock)
            {
                if (conn.Type == ClientType.Agent)
                {
                    if (_agents.ContainsKey(conn.Id))
                    {
                        _agents.Remove(conn.Id);
                        Logger.Warn("Agent Disconnected: {0} ({1})", conn.Agent != null ? conn.Agent.Hostname : conn.Id, channel.RemoteEndPoint);
                    }
                }
                else if (conn.Type == ClientType.Control)
                {
                    _controls.Remove(conn);
                    Logger.Info("Master Control Console Disconnected: {0}", channel.RemoteEndPoint);
                }
            }

            if (conn.Type == ClientType.Agent)
            {
                BroadcastAgentListToControls();
            }
        }

        private void CheckStaleAgents(object state)
        {
            List<ClientConnection> stale = new List<ClientConnection>();
            lock (_stateLock)
            {
                DateTime now = DateTime.Now;
                foreach (ClientConnection conn in _agents.Values)
                {
                    if ((now - conn.LastSeen).TotalSeconds > 15)
                    {
                        stale.Add(conn);
                    }
                }

                foreach (ClientConnection s in stale)
                {
                    _agents.Remove(s.Id);
                    s.Channel.Close();
                    Logger.Warn("Agent timed out (Heartbeat lost): {0}", s.Agent != null ? s.Agent.Hostname : s.Id);
                }
            }

            if (stale.Count > 0)
            {
                BroadcastAgentListToControls();
            }
        }

        private void ForwardToControls(Packet packet)
        {
            lock (_stateLock)
            {
                for (int i = _controls.Count - 1; i >= 0; i--)
                {
                    ClientConnection ctrl = _controls[i];
                    if (!ctrl.Channel.IsConnected)
                    {
                        _controls.RemoveAt(i);
                        continue;
                    }
                    ctrl.Channel.Send(packet);
                }
            }
        }

        public void BroadcastAgentListToControls()
        {
            List<AgentInfo> list = GetActiveAgents();
            Packet packet = Packet.Create(PacketType.AgentList, list);
            ForwardToControls(packet);

            if (AgentsUpdated != null)
            {
                try { AgentsUpdated(); } catch { }
            }
        }

        private void SendAgentListToControl(NetworkChannel controlChannel)
        {
            List<AgentInfo> list = GetActiveAgents();
            Packet packet = Packet.Create(PacketType.AgentList, list);
            controlChannel.Send(packet);
        }

        public List<AgentInfo> GetActiveAgents()
        {
            List<AgentInfo> list = new List<AgentInfo>();
            lock (_stateLock)
            {
                foreach (ClientConnection conn in _agents.Values)
                {
                    if (conn.Agent != null)
                    {
                        list.Add(conn.Agent);
                    }
                }
            }
            return list;
        }

        public Dictionary<string, int> GetVlanStatistics()
        {
            Dictionary<string, int> dict = new Dictionary<string, int>();
            lock (_stateLock)
            {
                foreach (ClientConnection conn in _agents.Values)
                {
                    string vlan = (conn.Agent != null && !string.IsNullOrEmpty(conn.Agent.VlanTag)) 
                        ? conn.Agent.VlanTag 
                        : "Default";

                    if (!dict.ContainsKey(vlan)) dict[vlan] = 0;
                    dict[vlan]++;
                }
            }
            return dict;
        }
    }
}
