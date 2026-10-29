using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using LanControl.Core.Models;

namespace LanControl.Core.Utils
{
    public static class InputSimulator
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public MOUSEKEYBDHARDWAREINPUT mkhi;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct MOUSEKEYBDHARDWAREINPUT
        {
            [FieldOffset(0)]
            public MOUSEINPUT mi;
            [FieldOffset(0)]
            public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private const uint INPUT_MOUSE = 0;
        private const uint INPUT_KEYBOARD = 1;

        private const uint MOUSEEVENTF_MOVE = 0x0001;
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        private const uint MOUSEEVENTF_WHEEL = 0x0800;
        private const uint MOUSEEVENTF_ABSOLUTE = 0x8000;

        private const uint KEYEVENTF_KEYDOWN = 0x0000;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint KEYEVENTF_UNICODE = 0x0004;

        private const int VK_LBUTTON = 0x01;
        private const int VK_RBUTTON = 0x02;
        private const int VK_MBUTTON = 0x04;
        private const uint GA_ROOT = 2;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(POINT Point);

        [DllImport("user32.dll")]
        private static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

        public static void ProcessInput(InputEvent ev, int remoteWidth = 0, int remoteHeight = 0)
        {
            if (ev == null) return;

            try
            {
                if (ev.IsMouse)
                {
                    HandleMouseEvent(ev, remoteWidth, remoteHeight);
                }
                else
                {
                    HandleKeyEvent(ev);
                }
            }
            catch { }
        }

        private static void HandleMouseEvent(InputEvent ev, int remoteWidth, int remoteHeight)
        {
            ScreenCapture.AttachToActiveInputDesktop();
            Rectangle screen = Screen.PrimaryScreen.Bounds;
            int screenW = screen.Width > 0 ? screen.Width : 1920;
            int screenH = screen.Height > 0 ? screen.Height : 1080;

            int targetX = ev.X;
            int targetY = ev.Y;

            if (remoteWidth > 0 && remoteHeight > 0)
            {
                targetX = (int)((double)ev.X / remoteWidth * screenW);
                targetY = (int)((double)ev.Y / remoteHeight * screenH);
            }

            // Clamp coordinates to valid screen bounds
            if (targetX < 0) targetX = 0;
            if (targetX >= screenW) targetX = screenW - 1;
            if (targetY < 0) targetY = 0;
            if (targetY >= screenH) targetY = screenH - 1;

            // Update physical cursor position with exact pixel accuracy
            SetCursorPos(targetX, targetY);

            switch (ev.MouseType)
            {
                case MouseEventType.Move:
                    SendMouseInput(MOUSEEVENTF_MOVE, 0, targetX, targetY, screenW, screenH);

                    // Anti-stuck watchdog:
                    // If controller is NOT holding the mouse button, but Windows thinks it is down
                    // (e.g. from an interrupted drag, window minimize, or lost network packet):
                    // Automatically inject clean UP events to unfreeze Windows desktop and taskbar.
                    if (!ev.LeftButtonDown && (GetAsyncKeyState(VK_LBUTTON) & 0x8000) != 0)
                    {
                        SendMouseInput(MOUSEEVENTF_LEFTUP, 0, targetX, targetY, screenW, screenH);
                    }
                    if (!ev.RightButtonDown && (GetAsyncKeyState(VK_RBUTTON) & 0x8000) != 0)
                    {
                        SendMouseInput(MOUSEEVENTF_RIGHTUP, 0, targetX, targetY, screenW, screenH);
                    }
                    break;

                case MouseEventType.LeftDown:
                    // When clicking, ensure the window under the cursor receives focus and activation
                    TryActivateWindowAt(targetX, targetY);
                    SendMouseInput(MOUSEEVENTF_LEFTDOWN, 0, targetX, targetY, screenW, screenH);
                    break;

                case MouseEventType.LeftUp:
                    SendMouseInput(MOUSEEVENTF_LEFTUP, 0, targetX, targetY, screenW, screenH);
                    break;

                case MouseEventType.RightDown:
                    TryActivateWindowAt(targetX, targetY);
                    SendMouseInput(MOUSEEVENTF_RIGHTDOWN, 0, targetX, targetY, screenW, screenH);
                    break;

                case MouseEventType.RightUp:
                    SendMouseInput(MOUSEEVENTF_RIGHTUP, 0, targetX, targetY, screenW, screenH);
                    break;

                case MouseEventType.MiddleDown:
                    SendMouseInput(MOUSEEVENTF_MIDDLEDOWN, 0, targetX, targetY, screenW, screenH);
                    break;

                case MouseEventType.MiddleUp:
                    SendMouseInput(MOUSEEVENTF_MIDDLEUP, 0, targetX, targetY, screenW, screenH);
                    break;

                case MouseEventType.DoubleClick:
                    TryActivateWindowAt(targetX, targetY);
                    SendMouseInput(MOUSEEVENTF_LEFTDOWN, 0, targetX, targetY, screenW, screenH);
                    SendMouseInput(MOUSEEVENTF_LEFTUP, 0, targetX, targetY, screenW, screenH);
                    Thread.Sleep(30);
                    SendMouseInput(MOUSEEVENTF_LEFTDOWN, 0, targetX, targetY, screenW, screenH);
                    SendMouseInput(MOUSEEVENTF_LEFTUP, 0, targetX, targetY, screenW, screenH);
                    break;

                case MouseEventType.Wheel:
                    SendMouseInput(MOUSEEVENTF_WHEEL, (uint)ev.Delta, targetX, targetY, screenW, screenH);
                    break;
            }
        }

        private static void TryActivateWindowAt(int x, int y)
        {
            try
            {
                POINT pt = new POINT { x = x, y = y };
                IntPtr wnd = WindowFromPoint(pt);
                if (wnd != IntPtr.Zero)
                {
                    IntPtr root = GetAncestor(wnd, GA_ROOT);
                    if (root == IntPtr.Zero) root = wnd;
                    SetForegroundWindow(root);
                }
            }
            catch { }
        }

        private static void SendMouseInput(uint flags, uint data, int targetX = -1, int targetY = -1, int screenW = 0, int screenH = 0)
        {
            INPUT[] inputs = new INPUT[1];
            inputs[0].type = INPUT_MOUSE;
            inputs[0].mkhi.mi.mouseData = data;
            inputs[0].mkhi.mi.time = 0;
            inputs[0].mkhi.mi.dwExtraInfo = IntPtr.Zero;

            if (targetX >= 0 && targetY >= 0 && screenW > 0 && screenH > 0)
            {
                // Normalize coordinates to 0..65535 for hardware-level precision across desktop layers
                int normX = (int)Math.Round((double)targetX * 65535.0 / (screenW > 1 ? screenW - 1 : 1));
                int normY = (int)Math.Round((double)targetY * 65535.0 / (screenH > 1 ? screenH - 1 : 1));
                inputs[0].mkhi.mi.dx = normX;
                inputs[0].mkhi.mi.dy = normY;
                inputs[0].mkhi.mi.dwFlags = flags | MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_MOVE;

                uint sent = SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
                if (sent == 0)
                {
                    mouse_event(flags | MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_MOVE, (uint)normX, (uint)normY, data, 0);
                }
            }
            else
            {
                inputs[0].mkhi.mi.dx = 0;
                inputs[0].mkhi.mi.dy = 0;
                inputs[0].mkhi.mi.dwFlags = flags;

                uint sent = SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
                if (sent == 0)
                {
                    mouse_event(flags, 0, 0, data, 0);
                }
            }
        }

        public static void SendUacResponse(bool accept)
        {
            try
            {
                ScreenCapture.AttachToActiveInputDesktop();

                if (accept)
                {
                    try
                    {
                        System.Diagnostics.Process[] consents = System.Diagnostics.Process.GetProcessesByName("consent");
                        if (consents.Length > 0 && consents[0].MainWindowHandle != IntPtr.Zero)
                        {
                            SetForegroundWindow(consents[0].MainWindowHandle);
                        }
                    }
                    catch { }

                    byte VK_MENU = 0x12; // Alt
                    byte VK_Y = 0x59;    // Y key
                    byte VK_RETURN = 0x0D; // Enter

                    keybd_event(VK_MENU, 0, KEYEVENTF_KEYDOWN, 0);
                    Thread.Sleep(50);
                    keybd_event(VK_Y, 0, KEYEVENTF_KEYDOWN, 0);
                    Thread.Sleep(50);
                    keybd_event(VK_Y, 0, KEYEVENTF_KEYUP, 0);
                    Thread.Sleep(50);
                    keybd_event(VK_MENU, 0, KEYEVENTF_KEYUP, 0);
                    Thread.Sleep(100);

                    keybd_event(VK_RETURN, 0, KEYEVENTF_KEYDOWN, 0);
                    Thread.Sleep(50);
                    keybd_event(VK_RETURN, 0, KEYEVENTF_KEYUP, 0);
                }
                else
                {
                    byte VK_ESCAPE = 0x1B;
                    keybd_event(VK_ESCAPE, 0, KEYEVENTF_KEYDOWN, 0);
                    Thread.Sleep(50);
                    keybd_event(VK_ESCAPE, 0, KEYEVENTF_KEYUP, 0);

                    Thread.Sleep(100);
                    try
                    {
                        System.Diagnostics.Process[] consents = System.Diagnostics.Process.GetProcessesByName("consent");
                        foreach (var p in consents)
                        {
                            try { p.Kill(); } catch { }
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        public static void SendUnicodeText(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            ScreenCapture.AttachToActiveInputDesktop();

            foreach (char c in text)
            {
                INPUT[] inputs = new INPUT[2];
                inputs[0].type = INPUT_KEYBOARD;
                inputs[0].mkhi.ki.wVk = 0;
                inputs[0].mkhi.ki.wScan = (ushort)c;
                inputs[0].mkhi.ki.dwFlags = KEYEVENTF_UNICODE;
                inputs[0].mkhi.ki.time = 0;
                inputs[0].mkhi.ki.dwExtraInfo = IntPtr.Zero;

                inputs[1] = inputs[0];
                inputs[1].mkhi.ki.dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP;

                SendInput(2, inputs, Marshal.SizeOf(typeof(INPUT)));
                Thread.Sleep(15);
            }
        }

        public static void SendAdminPassword(string password)
        {
            try
            {
                ScreenCapture.AttachToActiveInputDesktop();

                try
                {
                    System.Diagnostics.Process[] consents = System.Diagnostics.Process.GetProcessesByName("consent");
                    if (consents.Length > 0 && consents[0].MainWindowHandle != IntPtr.Zero)
                    {
                        SetForegroundWindow(consents[0].MainWindowHandle);
                    }
                }
                catch { }

                Thread.Sleep(100);

                if (!string.IsNullOrEmpty(password))
                {
                    SendUnicodeText(password);
                    Thread.Sleep(100);
                }

                // Press Enter to confirm password
                byte VK_RETURN = 0x0D;
                keybd_event(VK_RETURN, 0, KEYEVENTF_KEYDOWN, 0);
                Thread.Sleep(50);
                keybd_event(VK_RETURN, 0, KEYEVENTF_KEYUP, 0);
            }
            catch { }
        }

        private static void HandleKeyEvent(InputEvent ev)
        {
            ScreenCapture.AttachToActiveInputDesktop();
            if (ev.KeyCode <= 0) return;

            ushort vk = (ushort)ev.KeyCode;
            INPUT[] inputs = new INPUT[1];
            inputs[0].type = INPUT_KEYBOARD;
            inputs[0].mkhi.ki.wVk = vk;
            inputs[0].mkhi.ki.wScan = 0;
            inputs[0].mkhi.ki.time = 0;
            inputs[0].mkhi.ki.dwExtraInfo = IntPtr.Zero;

            if (ev.KeyType == KeyEventType.KeyDown)
            {
                inputs[0].mkhi.ki.dwFlags = KEYEVENTF_KEYDOWN;
            }
            else if (ev.KeyType == KeyEventType.KeyUp)
            {
                inputs[0].mkhi.ki.dwFlags = KEYEVENTF_KEYUP;
            }

            uint sent = SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
            if (sent == 0)
            {
                // Fallback to legacy keybd_event
                byte bVk = (byte)ev.KeyCode;
                uint dwFlags = (ev.KeyType == KeyEventType.KeyUp) ? KEYEVENTF_KEYUP : KEYEVENTF_KEYDOWN;
                keybd_event(bVk, 0, dwFlags, 0);
            }
        }
    }
}
