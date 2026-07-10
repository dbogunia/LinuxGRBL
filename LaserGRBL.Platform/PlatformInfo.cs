using System.Runtime.InteropServices;

namespace LaserGRBL.Platform;

/// <summary>Minimal host probe; platform contracts are introduced in Task 02.</summary>
public static class PlatformInfo
{
    public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
}
