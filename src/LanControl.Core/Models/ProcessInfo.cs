using System;

namespace LanControl.Core.Models
{
    [Serializable]
    public class ProcessItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public long MemoryBytes { get; set; }
        public string MemoryFormatted { get; set; }

        public ProcessItem()
        {
            Name = string.Empty;
            Title = string.Empty;
            MemoryFormatted = "0 MB";
        }
    }
}
