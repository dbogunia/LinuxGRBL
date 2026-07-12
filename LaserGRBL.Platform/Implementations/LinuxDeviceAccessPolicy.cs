namespace LaserGRBL.Platform.Implementations;

public static class LinuxDeviceAccessPolicy
{
    public const string SerialGroup = "dialout";

    public static string SerialPermissionDeniedMessage(string devicePath) =>
        $"Permission denied opening serial device '{devicePath}'. Prefer a stable /dev/serial/by-id device and add the user to the '{SerialGroup}' group, then log out and back in.";

    public static string SerialGroupCommand(string user = "$USER") => $"sudo usermod -aG {SerialGroup} {user}";
}
