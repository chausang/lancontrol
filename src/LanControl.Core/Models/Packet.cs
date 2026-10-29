using System;

namespace LanControl.Core.Models
{
    public class Packet
    {
        public PacketType Type { get; set; }
        public string PayloadJson { get; set; }
        public byte[] RawBytes { get; set; }

        public Packet()
        {
            Type = PacketType.Unknown;
            PayloadJson = string.Empty;
            RawBytes = null;
        }

        public Packet(PacketType type, string json = "", byte[] raw = null)
        {
            Type = type;
            PayloadJson = json ?? string.Empty;
            RawBytes = raw;
        }

        public static Packet Create<T>(PacketType type, T obj, byte[] raw = null)
        {
            string json = LanControl.Core.Network.PacketSerializer.Serialize(obj);
            return new Packet(type, json, raw);
        }

        public T GetPayload<T>()
        {
            if (string.IsNullOrEmpty(PayloadJson))
                return default(T);

            return LanControl.Core.Network.PacketSerializer.Deserialize<T>(PayloadJson);
        }
    }
}
