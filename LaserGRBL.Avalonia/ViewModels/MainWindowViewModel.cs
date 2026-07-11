using LaserGRBL.Avalonia.Services;
using LaserGRBL.Platform.Contracts;
using LaserGRBL.Platform.Implementations;

namespace LaserGRBL.Avalonia.ViewModels;

public sealed class MainWindowViewModel
{
    public MainWindowViewModel(IAppPaths paths, JsonSettingsStore settings, SemanticColorScheme theme, LocalizationCatalog localization, StartupDiagnostics diagnostics)
    {
        Theme = theme;
        Title = localization.Get("App.Title");
        StatusText = localization.Get("Status.Disconnected");
        FirmwareText = localization.Get("Firmware.NotSelected");
        Diagnostics = diagnostics.Messages.Count == 0 ? ["Application shell initialized."] : diagnostics.Messages;
        PathsSummary = $"{paths.ConfigDirectory} | {paths.DataDirectory}";
        SettingsSummary = $"Settings file: {settings.FilePath}";
        LogLines =
        [
            "Avalonia shell started.",
            "Core and Linux platform services registered.",
            localization.Get("Shell.WorkflowDeferred")
        ];
    }

    public static MainWindowViewModel DesignTime { get; } = new(
        new LinuxAppPaths("LaserGRBL", _ => null, "/home/user", "/tmp"),
        new JsonSettingsStore(new LinuxAppPaths("LaserGRBL", _ => null, "/home/user", "/tmp")),
        ColorSchemeCatalog.Default.Get("Default"),
        LocalizationCatalog.Default,
        new StartupDiagnostics());

    public string Title { get; }

    public SemanticColorScheme Theme { get; }

    public string StatusText { get; }

    public string FirmwareText { get; }

    public string ConnectionSummary { get; } = "No machine connected";

    public string DeviceAccessSummary { get; } = "Serial, TCP, WebSocket, emulator, process, and WiFi services are registered for future workflow screens.";

    public string PreviewSummary { get; } = "2D/3D preview arrives in Tasks 13 and 13B";

    public string PathsSummary { get; }

    public string SettingsSummary { get; }

    public IReadOnlyList<string> Diagnostics { get; }

    public IReadOnlyList<string> LogLines { get; }
}
