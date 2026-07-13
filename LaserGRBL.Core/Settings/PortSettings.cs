using LaserGRBL.Core.Protocol;
using LaserGRBL.Core.Safety;

namespace LaserGRBL.Core.Settings;

public sealed record RecentFile(string Path, DateTimeOffset OpenedAt);

public sealed record PortSettings(
    int SchemaVersion,
    FirmwareType Firmware,
    StreamingMode StreamingMode,
    string ColorScheme,
    string Language,
    IReadOnlyList<RecentFile> RecentFiles,
    SafetyAcknowledgementState? SafetyAcknowledgements = null)
{
    public const int CurrentSchemaVersion = 3;

    public static PortSettings Default { get; } = new(CurrentSchemaVersion, FirmwareType.Grbl, StreamingMode.Buffered, "Default", "en", [], SafetyAcknowledgementState.Empty);

    public PortSettings Normalize() =>
        this with
        {
            SchemaVersion = CurrentSchemaVersion,
            ColorScheme = string.IsNullOrWhiteSpace(ColorScheme) ? Default.ColorScheme : ColorScheme,
            Language = string.IsNullOrWhiteSpace(Language) ? Default.Language : Language,
            RecentFiles = RecentFiles ?? [],
            SafetyAcknowledgements = SafetyAcknowledgements ?? SafetyAcknowledgementState.Empty
        };
}
