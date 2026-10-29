using System;
using System.IO;
using LanControl.Core.Network;

namespace LanControl.Core.Models
{
    [Serializable]
    public class GatewayConfig
    {
        public int Port { get; set; }
        public bool AutoStart { get; set; }
        public bool ConfigureFirewall { get; set; }
        public bool CreateDesktopShortcuts { get; set; }
        public string InstallPath { get; set; }

        public GatewayConfig()
        {
            Port = 8443;
            AutoStart = true;
            ConfigureFirewall = true;
            CreateDesktopShortcuts = true;
            InstallPath = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
        }

        public static GatewayConfig Load(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    GatewayConfig cfg = PacketSerializer.Deserialize<GatewayConfig>(json);
                    if (cfg != null)
                    {
                        if (cfg.Port <= 0 || cfg.Port > 65535) cfg.Port = 8443;
                        return cfg;
                    }
                }
            }
            catch { }
            return new GatewayConfig();
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
