using System;

namespace LanControl.Core.Models
{
    [Serializable]
    public class AgentInfo
    {
        public string AgentId { get; set; }
        public string Hostname { get; set; }
        public string IpAddress { get; set; }
        public string MacAddress { get; set; }
        public string VlanTag { get; set; }
        public string OsVersion { get; set; }
        public string UserName { get; set; }
        public long TotalMemoryMb { get; set; }
        public long AvailableMemoryMb { get; set; }
        public int CpuUsagePercent { get; set; }
        public bool IsLocked { get; set; }
        public bool IsStreaming { get; set; }
        public DateTime LastSeen { get; set; }

        public AgentInfo()
        {
            AgentId = Guid.NewGuid().ToString("N");
            Hostname = Environment.MachineName;
            IpAddress = "127.0.0.1";
            MacAddress = "00:00:00:00:00:00";
            VlanTag = "Default";
            OsVersion = Environment.OSVersion.ToString();
            UserName = Environment.UserName;
            LastSeen = DateTime.Now;
        }

        public string GetDisplayName()
        {
            return string.Format("{0} ({1})", Hostname, IpAddress);
        }
    }
}
