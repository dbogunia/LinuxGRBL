using System.Text.Json;
using LaserGRBL.Core.Abstractions;
using LaserGRBL.Core.Settings;
using LaserGRBL.Platform.Contracts;

namespace LaserGRBL.Platform.Implementations;

public sealed class JsonSettingsStore
{
    private readonly IAppPaths paths;
    private readonly JsonSerializerOptions options = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    public JsonSettingsStore(IAppPaths paths) => this.paths = paths;

    public string FilePath => Path.Combine(paths.ConfigDirectory, "settings.json");

    public async Task<OperationResult<PortSettings>> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(FilePath)) return OperationResult<PortSettings>.Success(PortSettings.Default);
        try
        {
            await using var stream = File.OpenRead(FilePath);
            var settings = await JsonSerializer.DeserializeAsync<PortSettings>(stream, options, cancellationToken);
            return OperationResult<PortSettings>.Success(settings is null || settings.SchemaVersion > PortSettings.CurrentSchemaVersion ? PortSettings.Default : settings.Normalize());
        }
        catch (Exception exception) when (exception is JsonException or IOException)
        {
            TryPreserveCorruptFile();
            return OperationResult<PortSettings>.Success(PortSettings.Default);
        }
    }

    public async Task<OperationResult> SaveAsync(PortSettings settings, CancellationToken cancellationToken = default)
    {
        try
        {
            Directory.CreateDirectory(paths.ConfigDirectory);
            var temporary = $"{FilePath}.tmp";
            await using (var stream = File.Create(temporary)) await JsonSerializer.SerializeAsync(stream, settings with { SchemaVersion = PortSettings.CurrentSchemaVersion }, options, cancellationToken);
            File.Move(temporary, FilePath, overwrite: true);
            return OperationResult.Success();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return OperationResult.Failure("Unable to save settings.", FilePath, exception);
        }
    }

    private void TryPreserveCorruptFile()
    {
        try
        {
            if (File.Exists(FilePath)) File.Copy(FilePath, $"{FilePath}.corrupt-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}");
        }
        catch (IOException)
        {
            // Falling back to defaults remains more important than retaining a second copy.
        }
    }
}
