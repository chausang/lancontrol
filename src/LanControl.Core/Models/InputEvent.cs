using System;

namespace LanControl.Core.Models
{
    [Serializable]
    public class InputEvent
    {
        public string AgentId { get; set; }
        public bool IsMouse { get; set; }
        public MouseEventType MouseType { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Delta { get; set; } // Mouse wheel
        public KeyEventType KeyType { get; set; }
        public int KeyCode { get; set; }
        public bool LeftButtonDown { get; set; }
        public bool RightButtonDown { get; set; }

        public InputEvent()
        {
            MouseType = MouseEventType.None;
            KeyType = KeyEventType.None;
        }
    }
}
