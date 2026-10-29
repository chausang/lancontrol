using System;
using LanControl.Core.Models;
using LanControl.Core.Network;

namespace LanControl.Gateway
{
    public class ClientConnection
    {
        public string Id { get; set; }
        public ClientType Type { get; set; }
        public NetworkChannel Channel { get; set; }
        public AgentInfo Agent { get; set; }
        public DateTime ConnectedAt { get; set; }
        public DateTime LastSeen { get; set; }

        public ClientConnection(string id, ClientType type, NetworkChannel channel)
        {
            Id = id;
            Type = type;
            Channel = channel;
            ConnectedAt = DateTime.Now;
            LastSeen = DateTime.Now;
        }
    }
}
