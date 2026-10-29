using System;

namespace LanControl.Core.Models
{
    [Serializable]
    public class ScreenFrameMeta
    {
        public string AgentId { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public long Timestamp { get; set; }
        public bool IsThumbnail { get; set; }

        public ScreenFrameMeta()
        {
            Timestamp = DateTime.UtcNow.Ticks;
        }
    }
}
