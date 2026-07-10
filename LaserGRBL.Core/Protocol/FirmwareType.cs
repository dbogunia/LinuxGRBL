namespace LaserGRBL.Core.Protocol;

public enum FirmwareType { Grbl, Smoothie, Marlin, VigoWork }

public enum MachineStatus { Disconnected, Connecting, Idle, Run, Hold, Door, Home, Alarm, Check, Jog, Queue, Cooling, AutoHold, Tool }

public enum MachineIssue { None, ManualAbort, ControllerAlarm, CommandRejected, StreamTimeout, UnexpectedDisconnect }

public enum StreamingMode { Buffered, Synchronous, RepeatOnError }

public readonly record struct MachinePosition(double X, double Y, double Z)
{
    public static readonly MachinePosition Zero = new(0, 0, 0);

    public static MachinePosition operator +(MachinePosition left, MachinePosition right) =>
        new(left.X + right.X, left.Y + right.Y, left.Z + right.Z);

    public static MachinePosition operator -(MachinePosition left, MachinePosition right) =>
        new(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
}
