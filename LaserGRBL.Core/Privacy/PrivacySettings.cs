namespace LaserGRBL.Core.Privacy;

public sealed record PrivacySettings(
    bool TelemetryEnabled,
    bool UpdateChecksEnabled,
    bool MaterialUpdatesEnabled,
    bool UsageStatisticsEnabled,
    bool LaserStatisticsEnabled,
    bool TelegramNotificationsEnabled)
{
    public static PrivacySettings Default { get; } = new(
        TelemetryEnabled: false,
        UpdateChecksEnabled: false,
        MaterialUpdatesEnabled: false,
        UsageStatisticsEnabled: false,
        LaserStatisticsEnabled: false,
        TelegramNotificationsEnabled: false);
}

public enum OutboundNetworkPurpose
{
    UpdateCheck,
    MaterialDatabaseUpdate,
    UsageStatistics,
    LaserStatistics,
    TelegramNotification,
    ExternalUrlOpen
}
