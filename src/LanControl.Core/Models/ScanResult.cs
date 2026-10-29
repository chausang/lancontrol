using System;
using System.Collections.Generic;

namespace LanControl.Core.Models
{
    [Serializable]
    public class ScanConfig
    {
        public string StartIp { get; set; }
        public string EndIp { get; set; }
        public string Cidr { get; set; }
        public string PortList { get; set; }
        public int TimeoutMs { get; set; }
        public int MaxConcurrency { get; set; }

        public ScanConfig()
        {
            StartIp = "192.168.1.1";
            EndIp = "192.168.1.254";
            Cidr = string.Empty;
            PortList = "8443, 3389, 22, 80, 443";
            TimeoutMs = 500;
            MaxConcurrency = 100;
        }
    }

    [Serializable]
    public class ScanHostResult
    {
        public string IpAddress { get; set; }
        public string Hostname { get; set; }
        public bool IsOnline { get; set; }
        public long PingMs { get; set; }
        public string OpenPorts { get; set; }
        public bool HasAgent { get; set; }
        public string Note { get; set; }

        public ScanHostResult()
        {
            Hostname = string.Empty;
            OpenPorts = string.Empty;
            Note = string.Empty;
        }
    }
}
