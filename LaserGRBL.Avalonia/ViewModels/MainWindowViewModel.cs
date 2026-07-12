using LaserGRBL.Avalonia.Services;
using LaserGRBL.Platform.Contracts;
using LaserGRBL.Platform.Implementations;

namespace LaserGRBL.Avalonia.ViewModels;

public sealed class MainWindowViewModel
{
    public MainWindowViewModel(IAppPaths paths, JsonSettingsStore settings, SemanticColorScheme theme, LocalizationCatalog localization, StartupDiagnostics diagnostics, MainWorkflowViewModel workflow, DialogToolsViewModel tools)
    {
        Workflow = workflow;
        Tools = tools;
        Theme = theme;
        Title = localization.Get("App.Title");
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
        new StartupDiagnostics(),
        new MainWorkflowViewModel(new InMemorySerialPortService(), new UnavailableExecutionInhibitor(), new DesignTimeMessageService()),
        new DialogToolsViewModel(
            new JsonSettingsStore(new LinuxAppPaths("LaserGRBL", _ => null, "/home/user", "/tmp")),
            new UnavailableFileDialogService(),
            new DesignTimeMessageService(),
            new DesignTimeWifiService(),
            new DesignTimeFirmwareFlashService()));

    public string Title { get; }

    public SemanticColorScheme Theme { get; }

    public MainWorkflowViewModel Workflow { get; }

    public DialogToolsViewModel Tools { get; }

    public string StatusText => Workflow.StatusText;

    public string FirmwareText { get; }

    public string ConnectionSummary { get; } = "No machine connected";

    public string DeviceAccessSummary { get; } = "Serial, TCP, WebSocket, emulator, process, and WiFi services are registered for future workflow screens.";

    public string PreviewSummary { get; } = "2D/3D preview arrives in Tasks 13 and 13B";

    public string PathsSummary { get; }

    public string SettingsSummary { get; }

    public IReadOnlyList<string> Diagnostics { get; }

    public IReadOnlyList<string> LogLines { get; }

    private sealed class DesignTimeMessageService : LaserGRBL.Core.Abstractions.IMessageService
    {
        public Task<bool> ShowAsync(LaserGRBL.Core.Abstractions.MessageRequest request, CancellationToken cancellationToken = default) => Task.FromResult(true);
    }

    private sealed class DesignTimeWifiService : IWifiService
    {
        public Task<LaserGRBL.Core.Abstractions.OperationResult<IReadOnlyList<WifiNetwork>>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(LaserGRBL.Core.Abstractions.OperationResult<IReadOnlyList<WifiNetwork>>.Success([new WifiNetwork("Workshop", 82)]));

        public Task<LaserGRBL.Core.Abstractions.OperationResult<IReadOnlyList<WifiInterface>>> ListInterfacesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(LaserGRBL.Core.Abstractions.OperationResult<IReadOnlyList<WifiInterface>>.Success([new WifiInterface("wlan0", "Wireless", true, [])]));

        public Task<LaserGRBL.Core.Abstractions.OperationResult> ConnectAsync(WifiConnectionRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(LaserGRBL.Core.Abstractions.OperationResult.Success());
    }

    private sealed class DesignTimeFirmwareFlashService : IFirmwareFlashService
    {
        public Task<LaserGRBL.Core.Abstractions.OperationResult> FlashAsync(FirmwareFlashRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(LaserGRBL.Core.Abstractions.OperationResult.Success());
    }
}
