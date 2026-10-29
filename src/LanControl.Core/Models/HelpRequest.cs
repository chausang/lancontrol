using System;

namespace LanControl.Core.Models
{
    [Serializable]
    public class HelpRequest
    {
        public string RequestId { get; set; }
        public string AgentId { get; set; }
        public string Hostname { get; set; }
        public string IpAddress { get; set; }
        public string VlanTag { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }

        public HelpRequest()
        {
            RequestId = Guid.NewGuid().ToString("N");
            Timestamp = DateTime.Now;
        }
    }

    [Serializable]
    public class HelpResponse
    {
        public string RequestId { get; set; }
        public string AgentId { get; set; }
        public bool Accepted { get; set; }
        public string Message { get; set; }

        public HelpResponse()
        {
            Accepted = false;
            Message = string.Empty;
        }
    }
}
