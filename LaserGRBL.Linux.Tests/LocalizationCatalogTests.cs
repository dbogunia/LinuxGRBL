using LaserGRBL.Avalonia.Services;
using LaserGRBL.Avalonia.ViewModels;
using LaserGRBL.Core.Abstractions;
using LaserGRBL.Core.Protocol;
using LaserGRBL.Core.Settings;
using LaserGRBL.Platform.Contracts;
using LaserGRBL.Platform.Implementations;
using Xunit;

namespace LaserGRBL.Linux.Tests;

public sealed class LocalizationCatalogTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"linuxgrbl-l10n-{Guid.NewGuid():N}");

    [Fact]
    public void Culture_lookup_uses_specific_neutral_and_english_fallback()
    {
        var catalog = new LocalizationCatalog(new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = new Dictionary<string, string> { ["A"] = "English", ["B"] = "English fallback" },
            ["pl"] = new Dictionary<string, string> { ["A"] = "Polski" },
            ["pl-PL"] = new Dictionary<string, string> { ["C"] = "Polska" }
        });

        Assert.Equal("Polska", catalog.Get("C", "pl-PL"));
        Assert.Equal("Polski", catalog.Get("A", "pl-PL"));
        Assert.Equal("English fallback", catalog.Get("B", "pl-PL"));
    }

    [Fact]
    public void Missing_keys_fall_back_to_key_and_are_detectable()
    {
        var catalog = LocalizationCatalog.Default;

        Assert.Equal("Missing.Key", catalog.Get("Missing.Key", "pl-PL"));
        Assert.Contains("Missing.Key", catalog.MissingKeys);
    }

    [Fact]
    public void Default_catalog_contains_translated_shell_and_tool_strings()
    {
        var polish = LocalizationCatalog.Default.ForCulture("pl-PL");

        Assert.Equal("Rozłączony", polish.Get("Status.Disconnected"));
        Assert.Equal("Ustawienia", polish.Get("Tool.Settings.Name"));
        Assert.Equal("Custom buttons", polish.Get("Tool.CustomButtons.Name"));
        Assert.Contains("pl-PL", LocalizationCatalog.Default.SupportedCultures);
    }

    [Fact]
    public async Task Language_selection_persists_in_json_settings()
    {
        var store = new JsonSettingsStore(new TestPaths(directory));
        var settings = PortSettings.Default with { Language = "pl-PL" };

        Assert.True((await store.SaveAsync(settings)).Succeeded);
        var loaded = await store.LoadAsync();

        Assert.Equal("pl-PL", loaded.Value?.Language);
    }

    [Fact]
    public async Task Old_settings_schema_is_normalized_with_default_language()
    {
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory, "settings.json"), """
        {
          "SchemaVersion": 1,
          "Firmware": 2,
          "StreamingMode": 1,
          "ColorScheme": "Default",
          "RecentFiles": []
        }
        """);

        var loaded = await new JsonSettingsStore(new TestPaths(directory)).LoadAsync();

        Assert.True(loaded.Succeeded);
        Assert.Equal(PortSettings.CurrentSchemaVersion, loaded.Value?.SchemaVersion);
        Assert.Equal("en", loaded.Value?.Language);
        Assert.Equal(FirmwareType.Marlin, loaded.Value?.Firmware);
    }

    [Fact]
    public void Main_window_and_tool_groups_use_selected_localization()
    {
        var paths = new LinuxAppPaths("LaserGRBL", _ => null, "/home/test", "/tmp");
        var settings = new JsonSettingsStore(paths);
        var messages = new TestMessageService();
        var localization = LocalizationCatalog.Default.ForCulture("pl-PL");
        var tools = new DialogToolsViewModel(settings, new UnavailableFileDialogService(), messages, new TestWifiService(), new TestFirmwareFlashService(), localization);
        var viewModel = new MainWindowViewModel(paths, settings, ColorSchemeCatalog.Default.Get("Default"), localization, new StartupDiagnostics(), new MainWorkflowViewModel(new InMemorySerialPortService(), new UnavailableExecutionInhibitor(), messages), tools);

        Assert.Equal("Firmware: niewybrane", viewModel.FirmwareText);
        Assert.Contains("Powłoka Avalonia", viewModel.LogLines[0]);
        Assert.Contains(tools.Groups, group => group.Name == "Ustawienia");
    }

    [Fact]
    public void Non_text_resx_resource_policy_is_documented()
    {
        var document = File.ReadAllText(Path.Combine(RepositoryRoot(), "docs", "localization-and-resx-migration.md"));

        Assert.Contains("System.Drawing.Color", document);
        Assert.Contains("Bitmap", document);
        Assert.Contains("Icon", document);
        Assert.Contains("WinForms", document);
    }

    public void Dispose()
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, true);
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "LaserGRBL.Linux.sln"))) return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Unable to locate repository root.");
    }

    private sealed class TestPaths(string root) : IAppPaths
    {
        public string DataDirectory => root;
        public string ConfigDirectory => root;
        public string CacheDirectory => root;
        public string LogDirectory => root;
    }

    private sealed class TestMessageService : IMessageService
    {
        public Task<bool> ShowAsync(MessageRequest request, CancellationToken cancellationToken = default) => Task.FromResult(true);
    }

    private sealed class TestWifiService : IWifiService
    {
        public Task<OperationResult<IReadOnlyList<WifiNetwork>>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(OperationResult<IReadOnlyList<WifiNetwork>>.Success([]));

        public Task<OperationResult<IReadOnlyList<WifiInterface>>> ListInterfacesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(OperationResult<IReadOnlyList<WifiInterface>>.Success([]));

        public Task<OperationResult> ConnectAsync(WifiConnectionRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(OperationResult.Success());
    }

    private sealed class TestFirmwareFlashService : IFirmwareFlashService
    {
        public Task<OperationResult> FlashAsync(FirmwareFlashRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(OperationResult.Success());
    }
}
