using LaserGRBL.Avalonia.Services;
using LaserGRBL.Avalonia.ViewModels;
using LaserGRBL.Core.Abstractions;
using LaserGRBL.Platform.Contracts;
using LaserGRBL.Platform.Implementations;
using Xunit;

namespace LaserGRBL.Linux.Tests;

public sealed class AvaloniaAppShellTests
{
    [Fact]
    public void Bootstrap_registers_core_platform_services_and_main_view_model()
    {
        var temp = Path.Combine(Path.GetTempPath(), $"lasergrbl-shell-{Guid.NewGuid():N}");
        try
        {
            var services = AppBootstrapper.CreateDefaultServices(_ => null, temp, temp);

            Assert.IsType<LinuxAppPaths>(services.Paths);
            Assert.IsType<JsonSettingsStore>(services.Settings);
            Assert.IsType<ProcessRunner>(services.Processes);
            Assert.IsType<LinuxSerialPortService>(services.SerialPorts);
            Assert.IsType<LinuxWifiService>(services.Wifi);
            Assert.IsAssignableFrom<IExecutionInhibitor>(services.ExecutionInhibitor);
            Assert.IsAssignableFrom<ISecretStore>(services.SecretStore);
            Assert.Equal("LaserGRBL", services.Localization.Get("App.Title"));
            Assert.EndsWith("lasergrbl.log", services.Log.LogFilePath);
            Assert.IsType<MainWindowViewModel>(services.MainWindow);
            Assert.Contains("LaserGRBL", services.Paths.ConfigDirectory);
        }
        finally
        {
            if (Directory.Exists(temp)) Directory.Delete(temp, true);
        }
    }

    [Fact]
    public void Main_view_model_surfaces_paths_settings_and_nonfatal_diagnostics()
    {
        var diagnostics = new StartupDiagnostics();
        diagnostics.Add("Secret store unavailable.");
        var paths = new LinuxAppPaths("LaserGRBL", _ => null, "/home/test", "/tmp");
        var viewModel = new MainWindowViewModel(paths, new JsonSettingsStore(paths), ColorSchemeCatalog.Default.Get("Default"), LocalizationCatalog.Default, diagnostics);

        Assert.Equal("LaserGRBL", viewModel.Title);
        Assert.Equal("Disconnected", viewModel.StatusText);
        Assert.Contains("settings.json", viewModel.SettingsSummary);
        Assert.Contains("/home/test", viewModel.PathsSummary);
        Assert.Contains("Secret store unavailable.", viewModel.Diagnostics);
    }

    [Fact]
    public void Color_scheme_catalog_preserves_legacy_named_and_semantic_colors()
    {
        var catalog = ColorSchemeCatalog.Default;
        var amber = catalog.Get("Safety Glasses Amber");

        Assert.Contains("Safety Glasses Green", catalog.Names);
        Assert.Equal("Safety Glasses Amber", amber.Name);
        Assert.NotNull(amber.PreviewPath);
        Assert.NotNull(amber.Command);
        Assert.NotNull(amber.Warning);
        Assert.NotNull(amber.Link);
        Assert.NotNull(amber.Disabled);
        Assert.Same(catalog.Get("Default"), catalog.Get("unknown"));
    }

    [Fact]
    public void Design_time_view_model_can_be_constructed_without_platform_startup()
    {
        var viewModel = MainWindowViewModel.DesignTime;

        Assert.NotNull(viewModel.Theme);
        Assert.NotEmpty(viewModel.LogLines);
    }
}
