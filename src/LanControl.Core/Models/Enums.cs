using System;

namespace LanControl.Core.Models
{
    public enum ClientType
    {
        Agent = 1,
        Control = 2
    }

    public enum PacketType : byte
    {
        Unknown = 0,
        Register = 1,
        Heartbeat = 2,
        AgentList = 3,
        ScreenFrame = 4,
        InputEvent = 5,
        CommandRequest = 6,
        CommandResponse = 7,
        BroadcastFrame = 8,
        StartScreenStream = 9,
        StopScreenStream = 10,
        StartBroadcast = 11,
        StopBroadcast = 12,
        ScanSubnetRequest = 13,
        ScanSubnetResponse = 14,
        Disconnect = 15,
        HelpRequest = 16,
        HelpResponse = 17,
        FileTransferRequest = 18,
        FileTransferChunk = 19,
        FileTransferResponse = 20,
        FileListRequest = 21,
        FileListResponse = 22
    }

    public enum CommandType
    {
        LockScreen,
        UnlockScreen,
        ExecuteCmd,
        ExecutePs,
        GetProcessList,
        KillProcess,
        SendMessage,
        Reboot,
        Shutdown,
        HideWallpaper,
        RestoreWallpaper,
        EnableUacBypass,
        DisableUacBypass,
        UacYes,
        UacNo,
        GetHardwareInventory,
        SendAdminPassword
    }


    public enum MouseEventType
    {
        None = 0,
        Move = 1,
        LeftDown = 2,
        LeftUp = 3,
        RightDown = 4,
        RightUp = 5,
        MiddleDown = 6,
        MiddleUp = 7,
        DoubleClick = 8,
        Wheel = 9
    }

    public enum KeyEventType
    {
        None = 0,
        KeyDown = 1,
        KeyUp = 2
    }
}
