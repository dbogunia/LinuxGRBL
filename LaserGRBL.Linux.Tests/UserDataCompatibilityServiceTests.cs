using LaserGRBL.Core.Protocol;
using LaserGRBL.Core.Settings;
using LaserGRBL.Core.UserData;
using LaserGRBL.Platform.Contracts;
using LaserGRBL.Platform.Implementations;
using Xunit;

namespace LaserGRBL.Linux.Tests;

public sealed class UserDataCompatibilityServiceTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"lasergrbl-user-data-{Guid.NewGuid():N}");
    private readonly UserDataCompatibilityService service;

    public UserDataCompatibilityServiceTests()
    {
        Directory.CreateDirectory(directory);
        service = new UserDataCompatibilityService(new TestPaths(directory));
    }

    [Fact]
    public void Compatibility_table_covers_required_legacy_formats()
    {
        var formats = string.Join('\n', UserDataCompatibilityService.Decisions.Select(decision => decision.LegacyFormat));

        Assert.Contains("LaserGRBL.Settings.bin", formats);
        Assert.Contains("UsageStats.bin", formats);
        Assert.Contains("LaserLifeCounter.bin", formats);
        Assert.Contains("CustomButtons.bin", formats);
        Assert.Contains("StandardMaterials.psh", formats);
        Assert.Contains("MaterialDB", formats);
        Assert.Contains("GRBL config", formats);
        Assert.Contains("*.lps", formats);
        Assert.Contains("DPAPI", formats);
    }

    [Fact]
    public async Task Settings_migration_from_supported_sample_is_normalized_and_idempotent()
    {
        var sample = Path.Combine(directory, "user-data.json");
        await File.WriteAllTextAsync(sample, """
        {
          "SchemaVersion": 0,
          "Settings": { "SchemaVersion": 1, "Firmware": "Marlin", "StreamingMode": "Synchronous", "ColorScheme": "Dark", "RecentFiles": [] },
          "CustomButtons": [{ "Id": 1, "Label": "Home", "Command": "$H" }],
          "Hotkeys": [],
          "Materials": [],
          "UsageCounters": { "JobsRun": 2, "LaserOnTime": "00:00:03", "MachineConnectedTime": "00:00:05" }
        }
        """);

        var first = await service.ImportBundleAsync(sample);
        Assert.True(first.Succeeded, first.Error?.Exception?.Message ?? first.Error?.Message);
        var save = await service.SaveBundleAsync(first.Value!);
        var second = await service.ImportBundleAsync(service.BundlePath);

        Assert.True(first.Succeeded);
        Assert.True(save.Succeeded);
        Assert.True(second.Succeeded);
        Assert.Equal(PortSettings.CurrentSchemaVersion, second.Value?.Settings.SchemaVersion);
        Assert.Equal(FirmwareType.Marlin, second.Value?.Settings.Firmware);
        Assert.Equal(1, second.Value?.CustomButtons.Length);
    }

    [Fact]
    public async Task Custom_buttons_round_trip_and_invalid_file_failure_are_clear()
    {
        var buttons = new[] { new CustomButtonData(1, "Unlock", "$X") };
        var export = await service.ExportCustomButtonsAsync(buttons);
        var import = await service.ImportCustomButtonsAsync(service.CustomButtonsPath);
        var invalidPath = Path.Combine(directory, "bad-buttons.json");
        await File.WriteAllTextAsync(invalidPath, "{ bad");
        var invalid = await service.ImportCustomButtonsAsync(invalidPath);

        Assert.True(export.Succeeded);
        Assert.True(import.Succeeded);
        Assert.Equal("$X", import.Value?.Single().Command);
        Assert.False(invalid.Succeeded);
        Assert.Contains("Custom button import failed", invalid.Error?.Message);
    }

    [Fact]
    public async Task Hotkey_import_preserves_conflict_data()
    {
        var path = Path.Combine(directory, "hotkeys-source.json");
        await File.WriteAllTextAsync(path, """
        [
          { "Action": "Run", "Gesture": "Ctrl+R" },
          { "Action": "Reset", "Gesture": "Ctrl+R" }
        ]
        """);

        var result = await service.ImportHotkeysAsync(path);
        var export = await service.ExportHotkeysAsync(result.Value!);

        Assert.True(result.Succeeded);
        Assert.True(export.Succeeded);
        Assert.All(result.Value!, binding => Assert.True(binding.IsConflict));
    }

    [Fact]
    public async Task Materials_import_clamps_supported_json_and_skips_binary_database()
    {
        var path = Path.Combine(directory, "materials.json");
        await File.WriteAllTextAsync(path, """[{ "Name": "Birch", "Power": 1200, "Speed": 0, "Source": "StandardMaterials.psh" }]""");
        var binary = Path.Combine(directory, "MaterialDB.bin");
        await File.WriteAllBytesAsync(binary, [0, 1, 2, 3]);

        var imported = await service.ImportMaterialsAsync(path);
        var skipped = await service.ImportMaterialsAsync(binary);

        Assert.True(imported.Succeeded);
        Assert.Equal(1000, imported.Value?.Single().Power);
        Assert.Equal(1, imported.Value?.Single().Speed);
        Assert.False(skipped.Succeeded);
        Assert.Contains("not portable", skipped.Error?.Message);
    }

    [Fact]
    public async Task Usage_counters_round_trip_and_invalid_serializer_file_fails()
    {
        var counters = new UsageCountersData(4, TimeSpan.FromSeconds(9), TimeSpan.FromMinutes(1));
        var export = await service.ExportUsageCountersAsync(counters);
        var import = await service.ImportUsageCountersAsync(service.UsageCountersPath);
        var invalid = Path.Combine(directory, "UsageStats.bin");
        await File.WriteAllBytesAsync(invalid, [0xff, 0x00]);
        var invalidResult = await service.ImportUsageCountersAsync(invalid);

        Assert.True(export.Succeeded);
        Assert.Equal(counters, import.Value);
        Assert.False(invalidResult.Succeeded);
        Assert.Contains("Usage counter import failed", invalidResult.Error?.Message);
    }

    [Fact]
    public void Grbl_configuration_round_trips_text_and_reports_invalid_lines()
    {
        var imported = service.ImportGrblConfiguration("$100=250\n$101=250\n");
        var exported = service.ExportGrblConfiguration(imported.Value!);
        var invalid = service.ImportGrblConfiguration("$100=250\nhello");

        Assert.True(imported.Succeeded);
        Assert.Equal("$100=250\n$101=250\n".Replace("\n", Environment.NewLine), exported);
        Assert.False(invalid.Succeeded);
        Assert.Contains("hello", invalid.Error?.Message);
    }

    [Fact]
    public async Task Project_round_trip_supports_embedded_image_and_rejects_binary_input()
    {
        var project = new LaserProjectData("demo", ["G0 X0", "M5"], Convert.ToBase64String([1, 2, 3]));
        var export = await service.ExportProjectAsync(project);
        var import = await service.ImportProjectAsync(Path.Combine(service.ProjectDirectory, "demo.lps.json"));
        var legacy = Path.Combine(directory, "legacy.lps");
        await File.WriteAllBytesAsync(legacy, [0, 1, 2, 3]);
        var invalid = await service.ImportProjectAsync(legacy);

        Assert.True(export.Succeeded);
        Assert.Equal(project.EmbeddedImageBase64, import.Value?.EmbeddedImageBase64);
        Assert.False(invalid.Succeeded);
        Assert.Contains("manual re-save", invalid.Error?.Message);
    }

    [Fact]
    public async Task Migration_preserves_legacy_binary_files_creates_backups_and_marks_dpapi_reentry()
    {
        var settings = Path.Combine(directory, "LaserGRBL.Settings.bin");
        var telegram = Path.Combine(directory, "Telegram.bin");
        await File.WriteAllBytesAsync(settings, [1, 2, 3]);
        await File.WriteAllBytesAsync(telegram, [4, 5, 6]);

        var first = await service.MigrateAsync(directory);
        var second = await service.MigrateAsync(directory);

        Assert.Contains(first.Items, item => item.Format == "LaserGRBL.Settings.bin" && item.Status == UserDataCompatibilityStatus.Skipped && File.Exists(item.BackupPath));
        Assert.Contains(first.Items, item => item.Format == "DPAPI Telegram credentials" && item.Message.Contains("re-entry"));
        Assert.Contains(second.Items, item => item.Format == "LaserGRBL.Settings.bin" && item.Status == UserDataCompatibilityStatus.Skipped);
        Assert.True(File.Exists(settings));
        Assert.True(File.Exists(telegram));
    }

    [Fact]
    public async Task Existing_linux_files_are_backed_up_before_export()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(service.CustomButtonsPath)!);
        await File.WriteAllTextAsync(service.CustomButtonsPath, "old");

        var result = await service.ExportCustomButtonsAsync([new CustomButtonData(1, "Home", "$H")]);

        Assert.True(result.Succeeded);
        Assert.True(File.Exists($"{service.CustomButtonsPath}.bak"));
        Assert.Equal("old", await File.ReadAllTextAsync($"{service.CustomButtonsPath}.bak"));
    }

    public void Dispose()
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
    }

    private sealed class TestPaths(string root) : IAppPaths
    {
        public string DataDirectory => root;
        public string ConfigDirectory => root;
        public string CacheDirectory => root;
        public string LogDirectory => root;
    }
}
