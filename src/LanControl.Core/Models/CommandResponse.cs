using System;

namespace LanControl.Core.Models
{
    [Serializable]
    public class CommandResponse
    {
        public string RequestId { get; set; }
        public string AgentId { get; set; }
        public bool Success { get; set; }
        public string Output { get; set; }
        public string ErrorMessage { get; set; }

        public CommandResponse()
        {
            Success = true;
            Output = string.Empty;
            ErrorMessage = string.Empty;
        }
    }
}
