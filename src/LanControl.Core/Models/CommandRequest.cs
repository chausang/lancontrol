using System;

namespace LanControl.Core.Models
{
    [Serializable]
    public class CommandRequest
    {
        public string RequestId { get; set; }
        public string AgentId { get; set; }
        public string TargetVlan { get; set; }
        public CommandType Command { get; set; }
        public string Payload { get; set; }

        public CommandRequest()
        {
            RequestId = Guid.NewGuid().ToString("N");
        }
    }
}
