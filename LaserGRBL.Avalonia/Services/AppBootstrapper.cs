using LaserGRBL.Avalonia.ViewModels;
using LaserGRBL.Avalonia.Preview;
using LaserGRBL.Core.Abstractions;
using LaserGRBL.Core.Safety;
using LaserGRBL.Platform.Contracts;
using LaserGRBL.Platform.Implementations;

namespace LaserGRBL.Avalonia.Services;

public static class AppBootstrapper
{
    public static AppServices CreateDefaultServices(Func<string, string?>? environment = null, string? homeDirectory = null, string? tempDirectory = null)
    {
        var diagnostics = new StartupDiagnostics();
        var paths = new LinuxAppPaths("LaserGRBL", environment, homeDirectory, tempDirectory);
        EnsureDirectories(paths, diagnostics);

        IProcessRunner processes = new ProcessRunner();
        var logger = new AppLogSink(paths);
        logger.Info("Avalonia app shell bootstrap started.");
        var settings = new JsonSettingsStore(paths);
        var persistedSettings = settings.LoadAsync().GetAwaiter().GetResult().Value ?? LaserGRBL.Core.Settings.PortSettings.Default;
        var safetyGate = new SafetyAcknowledgementService(persistedSettings.SafetyAcknowledgements);
        var localization = LocalizationCatalog.Default.ForCulture(persistedSettings.Language);
        var themeCatalog = ColorSchemeCatalog.Default;
        var theme = themeCatalog.Get("Default");

        IExecutionInhibitor inhibitor = new UnavailableExecutionInhibitor();
        ISecretStore secretStore = new UnavailableSecretStore();
        diagnostics.Add(localization.Get("Diagnostics.SleepInhibitionUnavailable"));
        diagnostics.Add(localization.Get("Diagnostics.SecretStoreUnavailable"));

        var serialPorts = new LinuxSerialPortService();
        var messageService = new LoggingMessageService(logger);
        var wifi = new LinuxWifiService(processes);
        var fileDialogs = new UnavailableFileDialogService();
        var firmwareFlash = new LinuxFirmwareFlashService(processes);
        var sound = new LinuxSoundService(processes, Path.Combine(AppContext.BaseDirectory, "Sound"));
        var updates = new ReleaseManifestUpdateService(new HttpUpdateManifestClient(), new Uri("https://github.com/dbogunia/LinuxGRBL/releases/latest/download/linuxgrbl-update.json"), new Version(0, 1, 0), enabled: false);
        var packageMetadata = new PackageMetadataService();
        var resourceLocks = new FileMachineResourceLockProvider(Path.Combine(paths.CacheDirectory, "locks"));
        var workflow = new MainWorkflowViewModel(serialPorts, inhibitor, messageService, new GCodePreviewRenderer(), PreviewRenderStyle.FromScheme(theme), new Preview3DSceneBuilder(), new AvaloniaOpenGlPreviewContextFactory(), resourceLocks, safetyGate);
        var tools = new DialogToolsViewModel(settings, fileDialogs, messageService, wifi, firmwareFlash, localization, safetyGate: safetyGate);
        var viewModel = new MainWindowViewModel(paths, settings, theme, localization, diagnostics, workflow, tools);
        return new AppServices(paths, settings, processes, serialPorts, wifi, firmwareFlash, fileDialogs, sound, updates, packageMetadata, inhibitor, secretStore, messageService, themeCatalog, localization, logger, diagnostics, viewModel, workflow, tools);
    }

    private static void EnsureDirectories(IAppPaths paths, StartupDiagnostics diagnostics)
    {
        foreach (var directory in new[] { paths.ConfigDirectory, paths.DataDirectory, paths.CacheDirectory, paths.LogDirectory })
        {
            try { Directory.CreateDirectory(directory); }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                diagnostics.Add($"Unable to initialize directory '{directory}': {exception.Message}");
            }
        }
    }
}

public sealed record AppServices(
    IAppPaths Paths,
    JsonSettingsStore Settings,
    IProcessRunner Processes,
    ISerialPortService SerialPorts,
    IWifiService Wifi,
    IFirmwareFlashService FirmwareFlash,
    IFileDialogService FileDialogs,
    ISoundService Sound,
    IUpdateService Updates,
    IPackageMetadataService PackageMetadata,
    IExecutionInhibitor ExecutionInhibitor,
    ISecretStore SecretStore,
    IMessageService Messages,
    ColorSchemeCatalog ColorSchemes,
    LocalizationCatalog Localization,
    AppLogSink Log,
    StartupDiagnostics Diagnostics,
    MainWindowViewModel MainWindow,
    MainWorkflowViewModel Workflow,
    DialogToolsViewModel Tools);

public sealed class StartupDiagnostics
{
    private readonly List<string> messages = [];

    public IReadOnlyList<string> Messages => messages;

    public void Add(string message)
    {
        if (!string.IsNullOrWhiteSpace(message)) messages.Add(message);
    }
}

public sealed class LoggingMessageService(AppLogSink log) : IMessageService
{
    public Task<bool> ShowAsync(MessageRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Severity is MessageSeverity.Error) log.Warning($"{request.Title}: {request.Message}");
        else log.Info($"{request.Title}: {request.Message}");
        return Task.FromResult(request.Severity != MessageSeverity.Confirmation);
    }
}

public sealed class UnavailableFileDialogService : IFileDialogService
{
    public Task<OperationResult<IReadOnlyList<string>>> OpenAsync(FileDialogRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(OperationResult<IReadOnlyList<string>>.Failure("Native file dialogs are not wired in this shell build.", request.Title));

    public Task<OperationResult<string>> SaveAsync(FileDialogRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(OperationResult<string>.Failure("Native file dialogs are not wired in this shell build.", request.Title));
}
