using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using LanControl.Core.Models;

namespace LanControl.Core.Network
{
    public class NetworkChannel
    {
        private readonly TcpClient _client;
        private readonly NetworkStream _stream;
        private readonly object _sendLock = new object();
        private bool _isDisposed;

        public event Action<NetworkChannel, Packet> PacketReceived;
        public event Action<NetworkChannel, Exception> Disconnected;

        public bool IsConnected
        {
            get { return _client != null && _client.Connected && !_isDisposed; }
        }

        public string RemoteEndPoint
        {
            get
            {
                try
                {
                    if (_client != null && _client.Client != null && _client.Client.RemoteEndPoint != null)
                        return _client.Client.RemoteEndPoint.ToString();
                }
                catch { }
                return "Unknown";
            }
        }

        public object Tag { get; set; }

        public NetworkChannel(TcpClient client)
        {
            _client = client;
            _client.NoDelay = true;
            _client.SendBufferSize = 256 * 1024;
            _client.ReceiveBufferSize = 256 * 1024;
            _stream = _client.GetStream();
        }

        public void StartReceiving()
        {
            Thread receiveThread = new Thread(ReceiveLoop)
            {
                IsBackground = true,
                Name = "LanControl_Channel_" + RemoteEndPoint
            };
            receiveThread.Start();
        }

        private void ReceiveLoop()
        {
            byte[] headerBuffer = new byte[PacketFramer.HEADER_SIZE];

            try
            {
                while (!_isDisposed && _client.Connected)
                {
                    // Read header exactly
                    int offset = 0;
                    while (offset < PacketFramer.HEADER_SIZE)
                    {
                        int read = _stream.Read(headerBuffer, offset, PacketFramer.HEADER_SIZE - offset);
                        if (read <= 0)
                        {
                            throw new EndOfStreamException("Connection terminated by remote host");
                        }
                        offset += read;
                    }

                    // Decode packet
                    Packet packet = PacketFramer.Decode(headerBuffer, _stream);

                    if (PacketReceived != null)
                    {
                        PacketReceived(this, packet);
                    }
                }
            }
            catch (Exception ex)
            {
                if (!_isDisposed)
                {
                    Close();
                    if (Disconnected != null)
                    {
                        Disconnected(this, ex);
                    }
                }
            }
        }

        public bool Send(Packet packet)
        {
            if (_isDisposed || !_client.Connected) return false;

            try
            {
                byte[] encoded = PacketFramer.Encode(packet);
                lock (_sendLock)
                {
                    _stream.Write(encoded, 0, encoded.Length);
                    _stream.Flush();
                }
                return true;
            }
            catch (Exception)
            {
                Close();
                return false;
            }
        }

        public void Close()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            try
            {
                if (_stream != null) _stream.Close();
                if (_client != null) _client.Close();
            }
            catch { }
        }
    }
}
