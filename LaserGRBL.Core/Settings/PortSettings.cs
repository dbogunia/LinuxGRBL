using LaserGRBL.Core.Protocol;

namespace LaserGRBL.Core.Settings;

public sealed record RecentFile(string Path, DateTimeOffset OpenedAt);

public sealed record PortSettings(
    int SchemaVersion,
    FirmwareType Firmware,
    StreamingMode StreamingMode,
    string ColorScheme,
    string Language,
    IReadOnlyList<RecentFile> RecentFiles)
{
    public const int CurrentSchemaVersion = 2;

    public static PortSettings Default { get; } = new(CurrentSchemaVersion, FirmwareType.Grbl, StreamingMode.Buffered, "Default", "en", []);

    public PortSettings Normalize() =>
        this with
        {
            SchemaVersion = CurrentSchemaVersion,
            ColorScheme = string.IsNullOrWhiteSpace(ColorScheme) ? Default.ColorScheme : ColorScheme,
            Language = string.IsNullOrWhiteSpace(Language) ? Default.Language : Language,
            RecentFiles = RecentFiles ?? []
        };
}
