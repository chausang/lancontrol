using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using LanControl.Core.Models;
using LanControl.Core.Network;
using LanControl.Core.Utils;

namespace LanControl.Control
{
    public class ControlClient
    {
        private NetworkChannel _channel;
        private string _host;
        private int _port;
        private bool _isConnecting;

        public event Action<bool> ConnectionStatusChanged;
        public event Action<List<AgentInfo>> AgentListUpdated;
        public event Action<AgentInfo> HeartbeatReceived;
        public event Action<ScreenFrameMeta, byte[]> ScreenFrameReceived;
        public event Action<CommandResponse> CommandResponseReceived;
        public event Action<List<ScanHostResult>> ScanSubnetResponseReceived;
        public event Action<HelpRequest> HelpRequestReceived;
        public event Action<FileTransferProgressResponse> FileTransferProgressReceived;
        public event Action<RemoteDirectoryListResponse> DirectoryListReceived;
        public event Action<FileChunkMeta, byte[]> FileDownloadChunkReceived;

        public bool IsConnected
        {
            get { return _channel != null && _channel.IsConnected; }
        }

        public string Host { get { return _host; } }
        public int Port { get { return _port; } }

        public void Connect(string host = "127.0.0.1", int port = 8443)
        {
            if (IsConnected || _isConnecting) return;
            _host = host;
            _port = port;
            _isConnecting = true;

            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    TcpClient tcp = new TcpClient();
                    tcp.Connect(_host, _port);

                    _channel = new NetworkChannel(tcp);
                    _channel.PacketReceived += OnPacketReceived;
                    _channel.Disconnected += OnDisconnected;
                    _channel.StartReceiving();

                    // Register as Control
                    Packet reg = new Packet(PacketType.Register, "{\"Type\":\"Control\"}");
                    _channel.Send(reg);

                    _isConnecting = false;
                    if (ConnectionStatusChanged != null)
                    {
                        ConnectionStatusChanged(true);
                    }
                }
                catch (Exception ex)
                {
                    _isConnecting = false;
                    Logger.Error("Failed to connect to Gateway: {0}", ex.Message);
                    if (ConnectionStatusChanged != null)
                    {
                        ConnectionStatusChanged(false);
                    }
                }
            });
        }

        public void Disconnect()
        {
            if (_channel != null)
            {
                _channel.Close();
                _channel = null;
            }
            if (ConnectionStatusChanged != null)
            {
                ConnectionStatusChanged(false);
            }
        }

        private void OnPacketReceived(NetworkChannel channel, Packet packet)
        {
            try
            {
                switch (packet.Type)
                {
                    case PacketType.AgentList:
                        List<AgentInfo> list = packet.GetPayload<List<AgentInfo>>();
                        if (list != null && AgentListUpdated != null)
                        {
                            AgentListUpdated(list);
                        }
                        break;

                    case PacketType.Heartbeat:
                        AgentInfo hb = packet.GetPayload<AgentInfo>();
                        if (hb != null && HeartbeatReceived != null)
                        {
                            HeartbeatReceived(hb);
                        }
                        break;

                    case PacketType.ScreenFrame:
                        ScreenFrameMeta meta = packet.GetPayload<ScreenFrameMeta>();
                        if (meta != null && ScreenFrameReceived != null)
                        {
                            ScreenFrameReceived(meta, packet.RawBytes);
                        }
                        break;

                    case PacketType.CommandResponse:
                        CommandResponse resp = packet.GetPayload<CommandResponse>();
                        if (resp != null && CommandResponseReceived != null)
                        {
                            CommandResponseReceived(resp);
                        }
                        break;

                    case PacketType.ScanSubnetResponse:
                        List<ScanHostResult> scanResults = packet.GetPayload<List<ScanHostResult>>();
                        if (scanResults != null && ScanSubnetResponseReceived != null)
                        {
                            ScanSubnetResponseReceived(scanResults);
                        }
                        break;

                    case PacketType.HelpRequest:
                        HelpRequest helpReq = packet.GetPayload<HelpRequest>();
                        if (helpReq != null && HelpRequestReceived != null)
                        {
                            HelpRequestReceived(helpReq);
                        }
                        break;

                    case PacketType.FileTransferResponse:
                        FileTransferProgressResponse ftResp = packet.GetPayload<FileTransferProgressResponse>();
                        if (ftResp != null && FileTransferProgressReceived != null)
                        {
                            FileTransferProgressReceived(ftResp);
                        }
                        break;

                    case PacketType.FileListResponse:
                        RemoteDirectoryListResponse listResp = packet.GetPayload<RemoteDirectoryListResponse>();
                        if (listResp != null && DirectoryListReceived != null)
                        {
                            DirectoryListReceived(listResp);
                        }
                        break;

                    case PacketType.FileTransferChunk:
                        FileChunkMeta chunkMeta = packet.GetPayload<FileChunkMeta>();
                        if (chunkMeta != null && FileDownloadChunkReceived != null)
                        {
                            FileDownloadChunkReceived(chunkMeta, packet.RawBytes);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error handling packet in ControlClient: {0}", ex.Message);
            }
        }

        private void OnDisconnected(NetworkChannel channel, Exception ex)
        {
            if (ConnectionStatusChanged != null)
            {
                ConnectionStatusChanged(false);
            }
        }

        public void SendCommand(string agentId, CommandType cmd, string payload = "", string targetVlan = "")
        {
            if (!IsConnected) return;

            CommandRequest req = new CommandRequest
            {
                AgentId = agentId,
                Command = cmd,
                Payload = payload,
                TargetVlan = targetVlan
            };

            _channel.Send(Packet.Create(PacketType.CommandRequest, req));
        }

        public void SendInput(InputEvent ev)
        {
            if (!IsConnected || ev == null) return;
            _channel.Send(Packet.Create(PacketType.InputEvent, ev));
        }

        public void StartScreenStream(string agentId)
        {
            if (!IsConnected) return;
            _channel.Send(new Packet(PacketType.StartScreenStream, agentId));
        }

        public void StopScreenStream(string agentId)
        {
            if (!IsConnected) return;
            _channel.Send(new Packet(PacketType.StopScreenStream, agentId));
        }

        public void StartBroadcast(string targetVlan = "")
        {
            if (!IsConnected) return;
            _channel.Send(new Packet(PacketType.StartBroadcast, targetVlan ?? ""));
        }

        public void SendBroadcastFrame(byte[] imageBytes, string targetVlan = "")
        {
            if (!IsConnected || imageBytes == null) return;
            _channel.Send(new Packet(PacketType.BroadcastFrame, targetVlan ?? "", imageBytes));
        }

        public void StopBroadcast(string targetVlan = "")
        {
            if (!IsConnected) return;
            _channel.Send(new Packet(PacketType.StopBroadcast, targetVlan ?? ""));
        }

        public void RequestSubnetScan(ScanConfig config)
        {
            if (!IsConnected) return;
            _channel.Send(Packet.Create(PacketType.ScanSubnetRequest, config));
        }

        public void SendHelpResponse(HelpResponse resp)
        {
            if (!IsConnected || resp == null) return;
            _channel.Send(Packet.Create(PacketType.HelpResponse, resp));
        }

        public void SendFileTransferRequest(FileTransferRequest req)
        {
            if (!IsConnected || req == null) return;
            _channel.Send(Packet.Create(PacketType.FileTransferRequest, req));
        }

        public void SendFileChunk(FileChunkMeta meta, byte[] data)
        {
            if (!IsConnected || meta == null) return;
            _channel.Send(Packet.Create(PacketType.FileTransferChunk, meta, data));
        }

        public void RequestRemoteDirectory(string agentId, string path)
        {
            if (!IsConnected) return;
            RemoteDirectoryListRequest req = new RemoteDirectoryListRequest
            {
                AgentId = agentId,
                Path = path
            };
            _channel.Send(Packet.Create(PacketType.FileListRequest, req));
        }

        public void SendFileTransferAction(string agentId, FileTransferAction action, string remotePath, string executeArgs = "")
        {
            if (!IsConnected) return;
            FileTransferRequest req = new FileTransferRequest
            {
                AgentId = agentId,
                Action = action,
                RemotePath = remotePath,
                ExecuteArguments = executeArgs
            };
            SendFileTransferRequest(req);
        }
    }
}
