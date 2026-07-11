using LaserGRBL.Core.Abstractions;

namespace LaserGRBL.Platform.Implementations;

public enum LegacySettingsImportStatus { NotFound, Imported, ManualMigrationRequired, Failed }

public sealed record LegacySettingsImportResult(LegacySettingsImportStatus Status, string? SourcePath = null, OperationError? Error = null);

/// <summary>Safe boundary for legacy BinaryFormatter settings. It deliberately never deserializes arbitrary binary input in the Linux app.</summary>
public sealed class LegacySettingsImportService
{
    public Task<LegacySettingsImportResult> InspectAsync(string legacyPath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(legacyPath)) return Task.FromResult(new LegacySettingsImportResult(LegacySettingsImportStatus.NotFound));
        return Task.FromResult(new LegacySettingsImportResult(
            LegacySettingsImportStatus.ManualMigrationRequired,
            legacyPath,
            new OperationError("Legacy binary settings were preserved but not deserialized.", "Task 18 defines supported compatibility import paths.")));
    }
}
