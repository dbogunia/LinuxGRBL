using LaserGRBL.Core.Protocol;
using LaserGRBL.Core.Settings;
using LaserGRBL.Platform.Contracts;
using LaserGRBL.Platform.Implementations;
using Xunit;

namespace LaserGRBL.Linux.Tests;

public sealed class JsonSettingsStoreTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"lasergrbl-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task Missing_file_returns_documented_defaults()
    {
        var settings = await new JsonSettingsStore(new TestPaths(directory)).LoadAsync();

        Assert.True(settings.Succeeded);
        Assert.Equal(PortSettings.Default, settings.Value);
    }

    [Fact]
    public async Task Saves_and_loads_versioned_settings()
    {
        var store = new JsonSettingsStore(new TestPaths(directory));
        var expected = new PortSettings(999, FirmwareType.Marlin, StreamingMode.Synchronous, "Dark", [new RecentFile("/tmp/test.nc", DateTimeOffset.UnixEpoch)]);

        Assert.True((await store.SaveAsync(expected)).Succeeded);
        var loaded = await store.LoadAsync();

        Assert.Equal(PortSettings.CurrentSchemaVersion, loaded.Value?.SchemaVersion);
        Assert.Equal(expected.Firmware, loaded.Value?.Firmware);
        Assert.Equal(expected.StreamingMode, loaded.Value?.StreamingMode);
        Assert.Equal(expected.ColorScheme, loaded.Value?.ColorScheme);
        Assert.Equal(expected.RecentFiles, loaded.Value?.RecentFiles);
        Assert.Contains("\"SchemaVersion\": 1", await File.ReadAllTextAsync(store.FilePath));
    }

    [Fact]
    public async Task Corrupt_json_falls_back_without_throwing()
    {
        var store = new JsonSettingsStore(new TestPaths(directory));
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(store.FilePath, "{ invalid");

        var loaded = await store.LoadAsync();

        Assert.True(loaded.Succeeded);
        Assert.Equal(PortSettings.Default, loaded.Value);
        Assert.True(File.Exists(store.FilePath));
        Assert.Single(Directory.GetFiles(directory, "settings.json.corrupt-*"));
    }

    public void Dispose()
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
    }

    [Fact]
    public async Task Legacy_binary_settings_are_preserved_without_binaryformatter_deserialization()
    {
        Directory.CreateDirectory(directory);
        var legacy = Path.Combine(directory, "LaserGRBL.Settings.bin");
        await File.WriteAllBytesAsync(legacy, [0, 1, 2, 3]);

        var result = await new LegacySettingsImportService().InspectAsync(legacy);

        Assert.Equal(LegacySettingsImportStatus.ManualMigrationRequired, result.Status);
        Assert.True(File.Exists(legacy));
        Assert.Contains("not deserialized", result.Error?.Message);
    }

    private sealed class TestPaths(string configDirectory) : IAppPaths
    {
        public string DataDirectory => configDirectory;
        public string ConfigDirectory => configDirectory;
        public string CacheDirectory => configDirectory;
        public string LogDirectory => configDirectory;
    }
}
