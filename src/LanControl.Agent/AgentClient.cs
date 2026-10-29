using System;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;
using LanControl.Core.Models;
using LanControl.Core.Network;
using LanControl.Core.Utils;

namespace LanControl.Agent
{
    public class AgentClient
    {
        private readonly AgentConfig _config;
        private readonly AgentInfo _info;
        private NetworkChannel _channel;
        private ScreenStreamer _streamer;
        private bool _isRunning;
        private Thread _connectionThread;
        private System.Threading.Timer _heartbeatTimer;

        private LockScreenForm _lockForm;
        private BroadcastViewerForm _broadcastViewer;
        private FileTransferHandler _fileHandler;
        private readonly object _lockState = new object();

        public AgentInfo Info { get { return _info; } }
        public bool IsConnected { get { return _channel != null && _channel.IsConnected; } }

        public event Action<bool> ConnectionStateChanged;
        public event Action<HelpResponse> HelpResponseReceived;

        public AgentClient(AgentConfig config)
        {
            _config = config;
            _info = new AgentInfo
            {
                AgentId = config.AgentId,
                Hostname = Environment.MachineName,
                IpAddress = SystemInfoCollector.GetLocalIpAddress(),
                MacAddress = SystemInfoCollector.GetMacAddress(),
                VlanTag = config.VlanTag,
                OsVersion = Environment.OSVersion.ToString(),
                UserName = Environment.UserName
            };

            long totalRam, availRam;
            SystemInfoCollector.GetMemoryInfo(out totalRam, out availRam);
            _info.TotalMemoryMb = totalRam;
            _info.AvailableMemoryMb = availRam;

            _streamer = new ScreenStreamer(_info, _config);
        }

        public void Start()
        {
            _isRunning = true;
            _streamer.Start();

            _connectionThread = new Thread(ConnectionLoop)
            {
                IsBackground = true,
                Name = "Agent_ConnectionLoop"
            };
            _connectionThread.Start();

            _heartbeatTimer = new System.Threading.Timer(SendHeartbeat, null, 3000, 3000);
        }

        public void Stop()
        {
            _isRunning = false;
            try { _heartbeatTimer.Dispose(); } catch { }
            _streamer.Stop();

            if (_channel != null)
            {
                _channel.Close();
            }

            UnlockScreen();
            CloseBroadcastViewer();
        }

        private void ConnectionLoop()
        {
            while (_isRunning)
            {
                if (_channel == null || !_channel.IsConnected)
                {
                    try
                    {
                        Logger.Info("Connecting reverse tunnel to Gateway at {0}:{1}...", _config.GatewayHost, _config.GatewayPort);
                        TcpClient tcp = new TcpClient();
                        tcp.Connect(_config.GatewayHost, _config.GatewayPort);

                        _channel = new NetworkChannel(tcp);
                        _channel.PacketReceived += OnPacketReceived;
                        _channel.Disconnected += OnDisconnected;
                        _channel.StartReceiving();

                        _streamer.SetChannel(_channel);
                        _fileHandler = new FileTransferHandler(_info, _channel);

                        // Send registration
                        Packet regPacket = Packet.Create(PacketType.Register, _info);
                        _channel.Send(regPacket);

                        Logger.Success("Connected to Central Gateway successfully! Assigned VLAN: [{0}]", _info.VlanTag);

                        if (ConnectionStateChanged != null)
                        {
                            ConnectionStateChanged(true);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn("Gateway connection failed ({0}). Retrying in 5 seconds...", ex.Message);
                        if (ConnectionStateChanged != null)
                        {
                            ConnectionStateChanged(false);
                        }
                    }
                }

                Thread.Sleep(5000);
            }
        }

        private void OnPacketReceived(NetworkChannel channel, Packet packet)
        {
            try
            {
                switch (packet.Type)
                {
                    case PacketType.InputEvent:
                        // Interactive input: mouse & keyboard
                        // NOTE: Local user is NOT blocked! Both can view and interact concurrently.
                        InputEvent ev = packet.GetPayload<InputEvent>();
                        if (ev != null)
                        {
                            InputSimulator.ProcessInput(ev);
                        }
                        break;

                    case PacketType.StartScreenStream:
                        Logger.Info("Remote Desktop session initiated by Master Console. Starting live screen stream.");
                        _streamer.SetLiveStreaming(true);
                        break;

                    case PacketType.StopScreenStream:
                        Logger.Info("Remote Desktop session closed. Returning to background thumbnail stream.");
                        _streamer.SetLiveStreaming(false);
                        break;

                    case PacketType.CommandRequest:
                        HandleCommand(packet.GetPayload<CommandRequest>());
                        break;

                    case PacketType.StartBroadcast:
                        OpenBroadcastViewer();
                        break;

                    case PacketType.BroadcastFrame:
                        if (_broadcastViewer != null && !_broadcastViewer.IsDisposed)
                        {
                            _broadcastViewer.UpdateFrame(packet.RawBytes);
                        }
                        break;

                    case PacketType.StopBroadcast:
                        CloseBroadcastViewer();
                        break;

                    case PacketType.HelpResponse:
                        HelpResponse helpResp = packet.GetPayload<HelpResponse>();
                        if (helpResp != null && HelpResponseReceived != null)
                        {
                            HelpResponseReceived(helpResp);
                        }
                        break;

                    case PacketType.FileTransferRequest:
                        if (_fileHandler != null)
                            _fileHandler.ProcessFileTransferRequest(packet.GetPayload<FileTransferRequest>());
                        break;

                    case PacketType.FileTransferChunk:
                        if (_fileHandler != null)
                            _fileHandler.ProcessFileChunk(packet);
                        break;

                    case PacketType.FileListRequest:
                        if (_fileHandler != null)
                            _fileHandler.ProcessFileListRequest(packet.GetPayload<RemoteDirectoryListRequest>());
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error handling packet on Agent: {0}", ex.Message);
            }
        }

        public bool SendHelpRequest(string message = "")
        {
            if (_channel == null || !_channel.IsConnected) return false;

            HelpRequest req = new HelpRequest
            {
                AgentId = _info.AgentId,
                Hostname = _info.Hostname,
                IpAddress = _info.IpAddress,
                VlanTag = _info.VlanTag,
                Message = string.IsNullOrEmpty(message) ? "Máy trạm yêu cầu hỗ trợ kỹ thuật từ quản trị viên." : message
            };

            Logger.Info("Sending Help Request to Master: {0}", req.Message);
            return _channel.Send(Packet.Create(PacketType.HelpRequest, req));
        }

        private void HandleCommand(CommandRequest req)
        {
            if (req == null) return;

            if (req.Command == CommandType.LockScreen)
            {
                Logger.Warn("Received explicit LOCK SCREEN command from Master.");
                LockScreen(req.Payload);

                CommandResponse resp = new CommandResponse
                {
                    RequestId = req.RequestId,
                    AgentId = _info.AgentId,
                    Success = true,
                    Output = "Screen locked."
                };
                _channel.Send(Packet.Create(PacketType.CommandResponse, resp));
                return;
            }
            else if (req.Command == CommandType.UnlockScreen)
            {
                Logger.Info("Received UNLOCK SCREEN command from Master.");
                UnlockScreen();

                CommandResponse resp = new CommandResponse
                {
                    RequestId = req.RequestId,
                    AgentId = _info.AgentId,
                    Success = true,
                    Output = "Screen unlocked."
                };
                _channel.Send(Packet.Create(PacketType.CommandResponse, resp));
                return;
            }

            // Other commands
            ThreadPool.QueueUserWorkItem(delegate
            {
                CommandResponse resp = CommandHandler.Execute(req);
                if (_channel != null && _channel.IsConnected)
                {
                    _channel.Send(Packet.Create(PacketType.CommandResponse, resp));
                }
            });
        }

        private void LockScreen(string message)
        {
            lock (_lockState)
            {
                _info.IsLocked = true;
                if (_lockForm == null || _lockForm.IsDisposed)
                {
                    Thread formThread = new Thread(new ThreadStart(delegate
                    {
                        _lockForm = new LockScreenForm(message);
                        Application.Run(_lockForm);
                    }))
                    {
                        IsBackground = true
                    };
                    formThread.SetApartmentState(ApartmentState.STA);
                    formThread.Start();
                }
                else
                {
                    _lockForm.UpdateMessage(message);
                }
            }
        }

        private void UnlockScreen()
        {
            lock (_lockState)
            {
                _info.IsLocked = false;
                if (_lockForm != null && !_lockForm.IsDisposed)
                {
                    try
                    {
                        _lockForm.Invoke(new MethodInvoker(delegate { _lockForm.Dispose(); }));
                    }
                    catch { }
                    _lockForm = null;
                }
            }
        }

        private void OpenBroadcastViewer()
        {
            if (_broadcastViewer == null || _broadcastViewer.IsDisposed)
            {
                Thread formThread = new Thread(new ThreadStart(delegate
                {
                    _broadcastViewer = new BroadcastViewerForm();
                    Application.Run(_broadcastViewer);
                }))
                {
                    IsBackground = true
                };
                formThread.SetApartmentState(ApartmentState.STA);
                formThread.Start();
            }
        }

        private void CloseBroadcastViewer()
        {
            if (_broadcastViewer != null && !_broadcastViewer.IsDisposed)
            {
                try
                {
                    _broadcastViewer.Invoke(new MethodInvoker(delegate { _broadcastViewer.Dispose(); }));
                }
                catch { }
                _broadcastViewer = null;
            }
        }

        private void SendHeartbeat(object state)
        {
            if (_channel != null && _channel.IsConnected)
            {
                try
                {
                    long totalRam, availRam;
                    SystemInfoCollector.GetMemoryInfo(out totalRam, out availRam);
                    _info.AvailableMemoryMb = availRam;
                    _info.CpuUsagePercent = SystemInfoCollector.GetCpuUsagePercent();

                    Packet packet = Packet.Create(PacketType.Heartbeat, _info);
                    _channel.Send(packet);
                }
                catch { }
            }
        }

        private void OnDisconnected(NetworkChannel channel, Exception ex)
        {
            Logger.Warn("Lost connection to Central Gateway ({0}). Reconnecting soon...", ex != null ? ex.Message : "closed");
            if (ConnectionStateChanged != null)
            {
                ConnectionStateChanged(false);
            }
        }
    }
}
