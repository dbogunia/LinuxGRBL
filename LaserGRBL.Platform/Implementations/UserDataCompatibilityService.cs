using System.Text.Json;
using System.Text.Json.Serialization;
using LaserGRBL.Core.Abstractions;
using LaserGRBL.Core.UserData;
using LaserGRBL.Platform.Contracts;

namespace LaserGRBL.Platform.Implementations;

public sealed class UserDataCompatibilityService
{
    private readonly IAppPaths paths;
    private readonly JsonSerializerOptions options = new() { WriteIndented = true, PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } };

    public UserDataCompatibilityService(IAppPaths paths) => this.paths = paths;

    public string BundlePath => Path.Combine(paths.DataDirectory, "user-data.json");
    public string CustomButtonsPath => Path.Combine(paths.DataDirectory, "custom-buttons.json");
    public string HotkeysPath => Path.Combine(paths.ConfigDirectory, "hotkeys.json");
    public string MaterialsPath => Path.Combine(paths.DataDirectory, "materials.json");
    public string UsageCountersPath => Path.Combine(paths.DataDirectory, "usage-counters.json");
    public string ProjectDirectory => Path.Combine(paths.DataDirectory, "projects");

    public static IReadOnlyList<UserDataCompatibilityDecision> Decisions { get; } =
    [
        new("LaserGRBL.Settings.bin", "settings.json + user-data.json", UserDataCompatibilityStatus.ImportOnly, "Supported JSON sample import only; arbitrary BinaryFormatter graphs are preserved and reported as manual migration.", "Export uses versioned JSON settings.", "Legacy source is copied to a .bak file before migration.", "Startup continues with defaults and a user-facing manual migration result."),
        new("CustomButtons.bin / *.zbn", "custom-buttons.json", UserDataCompatibilityStatus.Supported, "JSON button arrays are imported; legacy binary .zbn is preserved with unsupported-format guidance.", "Export writes custom-buttons.json.", "Existing Linux file is backed up before replacement.", "Invalid files return a clear import error without changing current data."),
        new("HotKeysManager serializer data", "hotkeys.json", UserDataCompatibilityStatus.Supported, "JSON action/gesture bindings import, preserving conflict flags for UI repair.", "Export writes hotkeys.json.", "Existing Linux file is backed up before replacement.", "Invalid or conflicting data is reported without blocking startup."),
        new("StandardMaterials.psh / PSHelper/MaterialDB.*", "materials.json", UserDataCompatibilityStatus.Supported, "JSON material arrays import for ported editor data; binary/password databases are skipped with rationale.", "Export writes materials.json.", "Existing Linux file is backed up before replacement.", "Unsupported database variants return a clear skip result."),
        new("UsageStats.bin / LaserLifeCounter.bin", "usage-counters.json", UserDataCompatibilityStatus.ImportOnly, "Portable JSON counters import; legacy binary counters are preserved and skipped.", "Export writes usage-counters.json.", "Existing Linux file is backed up before replacement.", "Counters fall back to zero when unreadable."),
        new("GRBL config text export", "plain text settings lines", UserDataCompatibilityStatus.Supported, "Plain $-prefixed setting lines round-trip.", "Export writes normalized text.", "No destructive migration is performed.", "Invalid lines identify the first bad setting."),
        new("*.lps project", "projects/*.lps.json", UserDataCompatibilityStatus.Supported, "JSON project files with optional embedded image payload import; legacy binary .lps is preserved and reported.", "Export writes JSON project data.", "Existing project file is backed up before replacement.", "Invalid binary input returns a clear project import error."),
        new("DPAPI-protected Telegram credentials", "ISecretStore entry after explicit re-entry", UserDataCompatibilityStatus.Skipped, "Windows DPAPI ciphertext is never decrypted or copied into JSON.", "No export from legacy ciphertext.", "Source file is preserved only.", "Telegram notifications require explicit token re-entry."),
    ];

    public async Task<OperationResult<UserDataBundle>> ImportBundleAsync(string path, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var stream = File.OpenRead(path);
            var bundle = await JsonSerializer.DeserializeAsync<UserDataBundle>(stream, options, cancellationToken);
            if (bundle is null) return OperationResult<UserDataBundle>.Failure("User data file was empty.", path);

            return OperationResult<UserDataBundle>.Success(Normalize(bundle));
        }
        catch (Exception exception) when (exception is JsonException or IOException or NotSupportedException)
        {
            return OperationResult<UserDataBundle>.Failure("User data import failed.", path, exception);
        }
    }

    public async Task<OperationResult> SaveBundleAsync(UserDataBundle bundle, CancellationToken cancellationToken = default)
    {
        var result = await WriteJsonAsync(BundlePath, Normalize(bundle), backupExisting: true, cancellationToken);
        return result.Succeeded ? OperationResult.Success() : OperationResult.Failure(result.Error!.Message, result.Error.Detail, result.Error.Exception);
    }

    public async Task<OperationResult<IReadOnlyList<CustomButtonData>>> ImportCustomButtonsAsync(string path, CancellationToken cancellationToken = default)
    {
        var result = await ReadJsonListAsync<CustomButtonData>(path, "Custom button import failed.", cancellationToken);
        if (!result.Succeeded) return result;

        var buttons = result.Value!.Where(button => !string.IsNullOrWhiteSpace(button.Label) && !string.IsNullOrWhiteSpace(button.Command)).ToArray();
        if (buttons.Length == 0) return OperationResult<IReadOnlyList<CustomButtonData>>.Failure("Custom button file contained no usable buttons.", path);
        return OperationResult<IReadOnlyList<CustomButtonData>>.Success(buttons);
    }

    public Task<OperationResult> ExportCustomButtonsAsync(IReadOnlyList<CustomButtonData> buttons, CancellationToken cancellationToken = default) =>
        WriteJsonAsync(CustomButtonsPath, buttons, backupExisting: true, cancellationToken);

    public async Task<OperationResult<IReadOnlyList<HotkeyBindingData>>> ImportHotkeysAsync(string path, CancellationToken cancellationToken = default)
    {
        var result = await ReadJsonListAsync<HotkeyBindingData>(path, "Hotkey import failed.", cancellationToken);
        if (!result.Succeeded) return result;

        var conflicts = result.Value!
            .GroupBy(binding => binding.Gesture, StringComparer.OrdinalIgnoreCase)
            .Where(group => !string.IsNullOrWhiteSpace(group.Key) && group.Select(binding => binding.Action).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
            .SelectMany(group => group.Select(binding => binding with { IsConflict = true }))
            .ToDictionary(binding => (binding.Action, binding.Gesture), binding => binding);
        var bindings = result.Value!.Select(binding => conflicts.TryGetValue((binding.Action, binding.Gesture), out var conflict) ? conflict : binding).ToArray();
        return OperationResult<IReadOnlyList<HotkeyBindingData>>.Success(bindings);
    }

    public Task<OperationResult> ExportHotkeysAsync(IReadOnlyList<HotkeyBindingData> hotkeys, CancellationToken cancellationToken = default) =>
        WriteJsonAsync(HotkeysPath, hotkeys, backupExisting: true, cancellationToken);

    public async Task<OperationResult<IReadOnlyList<MaterialProfileData>>> ImportMaterialsAsync(string path, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(path);
        if (extension.Equals(".bin", StringComparison.OrdinalIgnoreCase) || extension.Equals(".mdb", StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult<IReadOnlyList<MaterialProfileData>>.Failure("Material database format is not portable; source was preserved for manual migration.", path);
        }

        var result = await ReadJsonListAsync<MaterialProfileData>(path, "Material import failed.", cancellationToken);
        if (!result.Succeeded) return result;

        var materials = result.Value!.Select(material => material with { Power = Math.Clamp(material.Power, 0, 1000), Speed = Math.Max(1, material.Speed) }).ToArray();
        return OperationResult<IReadOnlyList<MaterialProfileData>>.Success(materials);
    }

    public Task<OperationResult> ExportMaterialsAsync(IReadOnlyList<MaterialProfileData> materials, CancellationToken cancellationToken = default) =>
        WriteJsonAsync(MaterialsPath, materials, backupExisting: true, cancellationToken);

    public async Task<OperationResult<UsageCountersData>> ImportUsageCountersAsync(string path, CancellationToken cancellationToken = default)
    {
        var result = await ReadJsonAsync<UsageCountersData>(path, "Usage counter import failed.", cancellationToken);
        return result.Succeeded
            ? OperationResult<UsageCountersData>.Success(result.Value!)
            : OperationResult<UsageCountersData>.Failure(result.Error!.Message, result.Error.Detail, result.Error.Exception);
    }

    public Task<OperationResult> ExportUsageCountersAsync(UsageCountersData counters, CancellationToken cancellationToken = default) =>
        WriteJsonAsync(UsageCountersPath, counters, backupExisting: true, cancellationToken);

    public OperationResult<IReadOnlyList<string>> ImportGrblConfiguration(string text)
    {
        var lines = text.Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0).ToArray();
        var invalid = lines.FirstOrDefault(line => !line.StartsWith('$') || !line.Contains('='));
        return invalid is null
            ? OperationResult<IReadOnlyList<string>>.Success(lines)
            : OperationResult<IReadOnlyList<string>>.Failure($"Invalid GRBL setting line: {invalid}");
    }

    public string ExportGrblConfiguration(IEnumerable<string> lines) => string.Join(Environment.NewLine, lines.Select(line => line.Trim()).Where(line => line.Length > 0)) + Environment.NewLine;

    public async Task<OperationResult<LaserProjectData>> ImportProjectAsync(string path, CancellationToken cancellationToken = default)
    {
        var result = await ReadJsonAsync<LaserProjectData>(path, "Project import failed.", cancellationToken);
        if (!result.Succeeded) return OperationResult<LaserProjectData>.Failure("Project import failed. Legacy binary .lps files require manual re-save from Windows LaserGRBL.", path, result.Error?.Exception);
        return OperationResult<LaserProjectData>.Success(result.Value!);
    }

    public Task<OperationResult> ExportProjectAsync(LaserProjectData project, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(ProjectDirectory);
        var safeName = string.Join("_", project.Name.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        return WriteJsonAsync(Path.Combine(ProjectDirectory, $"{safeName}.lps.json"), project, backupExisting: true, cancellationToken);
    }

    public UserDataMigrationItem InspectDpapiCredential(string path) =>
        File.Exists(path)
            ? new UserDataMigrationItem("DPAPI Telegram credentials", UserDataCompatibilityStatus.Skipped, "Credential is Windows DPAPI ciphertext; token re-entry is required when Telegram is enabled.", path)
            : new UserDataMigrationItem("DPAPI Telegram credentials", UserDataCompatibilityStatus.Skipped, "No legacy credential file found.", path);

    public async Task<UserDataMigrationReport> MigrateAsync(string sourceDirectory, CancellationToken cancellationToken = default)
    {
        var items = new List<UserDataMigrationItem>();
        var bundle = Path.Combine(sourceDirectory, "user-data.json");
        if (File.Exists(bundle))
        {
            var import = await ImportBundleAsync(bundle, cancellationToken);
            if (import.Succeeded)
            {
                var save = await SaveBundleAsync(import.Value!, cancellationToken);
                items.Add(new UserDataMigrationItem("user-data.json", save.Succeeded ? UserDataCompatibilityStatus.Supported : UserDataCompatibilityStatus.Unsupported, save.Succeeded ? "Imported user-data.json." : save.Error!.Message, bundle));
            }
            else items.Add(new UserDataMigrationItem("user-data.json", UserDataCompatibilityStatus.Unsupported, import.Error!.Message, bundle));
        }

        foreach (var legacyName in new[] { "LaserGRBL.Settings.bin", "UsageStats.bin", "LaserLifeCounter.bin", "CustomButtons.bin" })
        {
            var source = Path.Combine(sourceDirectory, legacyName);
            if (File.Exists(source)) items.Add(new UserDataMigrationItem(legacyName, UserDataCompatibilityStatus.Skipped, "Legacy binary serializer data was preserved and not deserialized by the Linux port.", source, await BackupAsync(source, cancellationToken)));
        }

        var telegram = Path.Combine(sourceDirectory, "Telegram.bin");
        items.Add(InspectDpapiCredential(telegram));
        return new UserDataMigrationReport(items);
    }

    private UserDataBundle Normalize(UserDataBundle bundle) => bundle with
    {
        SchemaVersion = UserDataBundle.CurrentSchemaVersion,
        Settings = (bundle.Settings ?? UserDataBundle.Empty.Settings).Normalize(),
        CustomButtons = (bundle.CustomButtons ?? []).Where(button => !string.IsNullOrWhiteSpace(button.Label) && !string.IsNullOrWhiteSpace(button.Command)).ToArray(),
        Hotkeys = (bundle.Hotkeys ?? []).Where(binding => !string.IsNullOrWhiteSpace(binding.Action) && !string.IsNullOrWhiteSpace(binding.Gesture)).ToArray(),
        Materials = (bundle.Materials ?? []).Select(material => material with { Power = Math.Clamp(material.Power, 0, 1000), Speed = Math.Max(1, material.Speed) }).ToArray(),
        UsageCounters = bundle.UsageCounters ?? UsageCountersData.Empty
    };

    private async Task<OperationResult<IReadOnlyList<T>>> ReadJsonListAsync<T>(string path, string message, CancellationToken cancellationToken)
    {
        var result = await ReadJsonAsync<T[]>(path, message, cancellationToken);
        return result.Succeeded
            ? OperationResult<IReadOnlyList<T>>.Success(result.Value ?? [])
            : OperationResult<IReadOnlyList<T>>.Failure(result.Error!.Message, result.Error.Detail, result.Error.Exception);
    }

    private async Task<OperationResult<T>> ReadJsonAsync<T>(string path, string message, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = File.OpenRead(path);
            var value = await JsonSerializer.DeserializeAsync<T>(stream, options, cancellationToken);
            return value is null ? OperationResult<T>.Failure(message, path) : OperationResult<T>.Success(value);
        }
        catch (Exception exception) when (exception is JsonException or IOException or NotSupportedException)
        {
            return OperationResult<T>.Failure(message, path, exception);
        }
    }

    private async Task<OperationResult> WriteJsonAsync<T>(string path, T value, bool backupExisting, CancellationToken cancellationToken)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            if (backupExisting) await BackupAsync(path, cancellationToken);
            var temporary = $"{path}.tmp";
            await using (var stream = File.Create(temporary)) await JsonSerializer.SerializeAsync(stream, value, options, cancellationToken);
            File.Move(temporary, path, overwrite: true);
            return OperationResult.Success();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return OperationResult.Failure("Unable to write user data file.", path, exception);
        }
    }

    private async Task<string?> BackupAsync(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path)) return null;
        var backup = $"{path}.bak";
        await using var source = File.OpenRead(path);
        await using var target = File.Create(backup);
        await source.CopyToAsync(target, cancellationToken);
        return backup;
    }
}
