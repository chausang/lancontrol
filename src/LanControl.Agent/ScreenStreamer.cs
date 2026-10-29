using System;
using System.Threading;
using LanControl.Core.Models;
using LanControl.Core.Network;
using LanControl.Core.Utils;

namespace LanControl.Agent
{
    public class ScreenStreamer
    {
        private readonly AgentInfo _agentInfo;
        private readonly AgentConfig _config;
        private NetworkChannel _channel;
        private bool _isRunning;
        private bool _isLiveStreaming;
        private Thread _streamThread;

        public bool IsLiveStreaming { get { return _isLiveStreaming; } }

        public ScreenStreamer(AgentInfo agentInfo, AgentConfig config)
        {
            _agentInfo = agentInfo;
            _config = config;
        }

        public void SetChannel(NetworkChannel channel)
        {
            _channel = channel;
        }

        public void Start()
        {
            if (_isRunning) return;
            _isRunning = true;

            _streamThread = new Thread(StreamLoop)
            {
                IsBackground = true,
                Name = "Agent_ScreenStreamer"
            };
            _streamThread.Start();
        }

        public void Stop()
        {
            _isRunning = false;
        }

        public void SetLiveStreaming(bool active)
        {
            _isLiveStreaming = active;
            _agentInfo.IsStreaming = active;
        }

        private void StreamLoop()
        {
            while (_isRunning)
            {
                try
                {
                    if (_channel != null && _channel.IsConnected)
                    {
                        if (_isLiveStreaming)
                        {
                            // Live Interactive Remote Desktop mode (higher resolution & FPS)
                            byte[] frameBytes = ScreenCapture.CaptureDesktopJpeg(65, true, 0, 0);
                            ScreenFrameMeta meta = new ScreenFrameMeta
                            {
                                AgentId = _agentInfo.AgentId,
                                IsThumbnail = false
                            };

                            Packet packet = Packet.Create(PacketType.ScreenFrame, meta, frameBytes);
                            _channel.Send(packet);

                            Thread.Sleep(_config.LiveStreamIntervalMs > 0 ? _config.LiveStreamIntervalMs : 66);
                        }
                        else
                        {
                            // Thumbnail Grid mode (low bandwidth, 400x225, 40% quality)
                            byte[] thumbBytes = ScreenCapture.CaptureDesktopJpeg(40, true, 400, 225);
                            ScreenFrameMeta meta = new ScreenFrameMeta
                            {
                                AgentId = _agentInfo.AgentId,
                                IsThumbnail = true
                            };

                            Packet packet = Packet.Create(PacketType.ScreenFrame, meta, thumbBytes);
                            _channel.Send(packet);

                            Thread.Sleep(_config.ThumbnailIntervalMs > 0 ? _config.ThumbnailIntervalMs : 1500);
                        }
                    }
                    else
                    {
                        Thread.Sleep(500);
                    }
                }
                catch
                {
                    Thread.Sleep(1000);
                }
            }
        }
    }
}
