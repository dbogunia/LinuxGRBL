using LaserGRBL.Core.Protocol;

namespace LaserGRBL.Core.Settings;

public sealed record RecentFile(string Path, DateTimeOffset OpenedAt);

public sealed record PortSettings(
    int SchemaVersion,
    FirmwareType Firmware,
    StreamingMode StreamingMode,
    string ColorScheme,
    IReadOnlyList<RecentFile> RecentFiles)
{
    public const int CurrentSchemaVersion = 1;

    public static PortSettings Default { get; } = new(CurrentSchemaVersion, FirmwareType.Grbl, StreamingMode.Buffered, "Default", []);
}
