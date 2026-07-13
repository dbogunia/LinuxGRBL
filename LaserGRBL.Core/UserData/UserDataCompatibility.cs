using LaserGRBL.Core.Settings;

namespace LaserGRBL.Core.UserData;

public enum UserDataCompatibilityStatus
{
    Supported,
    ImportOnly,
    Skipped,
    Unsupported
}

public sealed record UserDataCompatibilityDecision(
    string LegacyFormat,
    string LinuxFormat,
    UserDataCompatibilityStatus Status,
    string ImportBehavior,
    string ExportBehavior,
    string BackupBehavior,
    string FailureBehavior);

public sealed record UserDataMigrationItem(string Format, UserDataCompatibilityStatus Status, string Message, string? SourcePath = null, string? BackupPath = null);

public sealed record UserDataMigrationReport(IReadOnlyList<UserDataMigrationItem> Items)
{
    public bool HasFailures => Items.Any(item => item.Status is UserDataCompatibilityStatus.Unsupported);
}

public sealed record UserDataBundle(
    int SchemaVersion,
    PortSettings Settings,
    CustomButtonData[] CustomButtons,
    HotkeyBindingData[] Hotkeys,
    MaterialProfileData[] Materials,
    UsageCountersData UsageCounters,
    LaserProjectData? Project = null)
{
    public const int CurrentSchemaVersion = 1;

    public static UserDataBundle Empty { get; } = new(
        CurrentSchemaVersion,
        PortSettings.Default,
        [],
        [],
        [],
        UsageCountersData.Empty);
}

public sealed record CustomButtonData(int Id, string Label, string Command);

public sealed record HotkeyBindingData(string Action, string Gesture, bool IsConflict = false);

public sealed record MaterialProfileData(string Name, int Power, int Speed, string? Source = null);

public sealed record UsageCountersData(long JobsRun, TimeSpan LaserOnTime, TimeSpan MachineConnectedTime)
{
    public static UsageCountersData Empty { get; } = new(0, TimeSpan.Zero, TimeSpan.Zero);
}

public sealed record LaserProjectData(string Name, string[] GCodeLines, string? EmbeddedImageBase64 = null);
