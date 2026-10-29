using System;
using System.IO;
using LanControl.Core.Network;

namespace LanControl.Core.Models
{
    [Serializable]
    public class AgentConfig
    {
        public string AgentId { get; set; }
        public string GatewayHost { get; set; }
        public int GatewayPort { get; set; }
        public string VlanTag { get; set; }
        public int ThumbnailIntervalMs { get; set; }
        public int LiveStreamIntervalMs { get; set; }

        public AgentConfig()
        {
            AgentId = Guid.NewGuid().ToString("N");
            GatewayHost = "127.0.0.1";
            GatewayPort = 8443;
            VlanTag = "VLAN 10 - Tech";
            ThumbnailIntervalMs = 1500;
            LiveStreamIntervalMs = 66; // ~15 FPS
        }

        public static AgentConfig Load(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    AgentConfig cfg = PacketSerializer.Deserialize<AgentConfig>(json);
                    if (cfg != null)
                    {
                        if (string.IsNullOrEmpty(cfg.AgentId))
                        {
                            cfg.AgentId = Guid.NewGuid().ToString("N");
                        }
                        return cfg;
                    }
                }
            }
            catch { }
            return new AgentConfig();
        }

        public void Save(string filePath)
        {
            try
            {
                string dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                string json = PacketSerializer.Serialize(this);
                File.WriteAllText(filePath, json);
            }
            catch { }
        }
    }
}
