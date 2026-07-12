using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using LaserGRBL.Avalonia.Preview;
using LaserGRBL.Avalonia.Services;
using LaserGRBL.Core.Abstractions;
using LaserGRBL.Core.GCode;
using LaserGRBL.Core.Protocol;
using LaserGRBL.Platform.Contracts;

namespace LaserGRBL.Avalonia.ViewModels;

public sealed class MainWorkflowViewModel : INotifyPropertyChanged, IAsyncDisposable
{
    private readonly ISerialPortService serialPorts;
    private readonly IExecutionInhibitor executionInhibitor;
    private readonly IMessageService messages;
    private readonly IJobPreviewRenderer previewRenderer;
    private readonly PreviewRenderStyle previewStyle;
    private readonly GCodeJob job = new();
    private ISerialConnection? connection;
    private MachineSession session;
    private IAsyncDisposable? sleepLease;
    private bool isBusy;
    private bool hasLoadedFile;
    private bool isPaused;
    private bool isJobActive;
    private string? lastFilePath;
    private SerialPortDescriptor? selectedPort;
    private int selectedBaudRate = 115200;
    private FirmwareType selectedFirmware = FirmwareType.Grbl;
    private string manualCommand = "";
    private string statusText = "Disconnected";
    private string machineCoordinates = "X0 Y0 Z0";
    private MachinePosition currentMachinePosition = MachinePosition.Zero;
    private PreviewSceneModel previewScene;
    private PreviewInteractionState previewInteraction = new();

    public MainWorkflowViewModel(ISerialPortService serialPorts, IExecutionInhibitor executionInhibitor, IMessageService messages, IJobPreviewRenderer? previewRenderer = null, PreviewRenderStyle? previewStyle = null)
    {
        this.serialPorts = serialPorts;
        this.executionInhibitor = executionInhibitor;
        this.messages = messages;
        this.previewRenderer = previewRenderer ?? new GCodePreviewRenderer();
        this.previewStyle = previewStyle ?? PreviewRenderStyle.FromScheme(ColorSchemeCatalog.Default.Get("Default"));
        previewScene = PreviewSceneModel.Empty(this.previewStyle);
        session = CreateSession(selectedFirmware);
        FirmwareTypes = Enum.GetValues<FirmwareType>();
        BaudRates = [9600, 57600, 115200, 230400, 250000];
        RefreshPortsCommand = new AsyncWorkflowCommand(() => RefreshPortsAsync(), () => CanRefreshPorts);
        ConnectCommand = new AsyncWorkflowCommand(() => ConnectAsync(), () => CanConnect);
        DisconnectCommand = new AsyncWorkflowCommand(() => DisconnectAsync(), () => CanDisconnect);
        RunCommand = new AsyncWorkflowCommand(() => RunJobAsync(), () => CanRun);
        HoldCommand = new AsyncWorkflowCommand(() => HoldAsync(), () => CanHold);
        ResumeCommand = new AsyncWorkflowCommand(() => ResumeAsync(), () => CanResume);
        ResetCommand = new AsyncWorkflowCommand(() => ResetAsync(), () => CanReset);
        StopCommand = new AsyncWorkflowCommand(() => StopAsync(), () => CanStop);
        SendManualCommand = new AsyncWorkflowCommand(() => SendManualCommandAsync(), () => CanSendManualCommand);
        ReopenLastFileCommand = new AsyncWorkflowCommand(() => ReopenLastFileAsync(), () => CanReopenLastFile);
        JogXPositiveCommand = new AsyncWorkflowCommand(() => JogAsync("X+", 1, 1000), () => CanJog);
        JogXNegativeCommand = new AsyncWorkflowCommand(() => JogAsync("X-", 1, 1000), () => CanJog);
        JogYPositiveCommand = new AsyncWorkflowCommand(() => JogAsync("Y+", 1, 1000), () => CanJog);
        JogYNegativeCommand = new AsyncWorkflowCommand(() => JogAsync("Y-", 1, 1000), () => CanJog);
        JogZPositiveCommand = new AsyncWorkflowCommand(() => JogAsync("Z+", 1, 500), () => CanJog);
        JogZNegativeCommand = new AsyncWorkflowCommand(() => JogAsync("Z-", 1, 500), () => CanJog);
        PreviewZoomInCommand = new RelayCommand(() => PreviewInteraction = PreviewInteraction.ZoomBy(1.25));
        PreviewZoomOutCommand = new RelayCommand(() => PreviewInteraction = PreviewInteraction.ZoomBy(0.8));
        PreviewAutoFitCommand = new RelayCommand(() => PreviewInteraction = PreviewInteraction.AutoFit());
        PreviewPanLeftCommand = new RelayCommand(() => PreviewInteraction = PreviewInteraction.PanBy(-24, 0));
        PreviewPanRightCommand = new RelayCommand(() => PreviewInteraction = PreviewInteraction.PanBy(24, 0));
        PreviewPanUpCommand = new RelayCommand(() => PreviewInteraction = PreviewInteraction.PanBy(0, -24));
        PreviewPanDownCommand = new RelayCommand(() => PreviewInteraction = PreviewInteraction.PanBy(0, 24));
        AddLog("Workflow ready.");
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<SerialPortDescriptor> Ports { get; } = [];
    public ObservableCollection<string> LogLines { get; } = [];
    public IReadOnlyList<FirmwareType> FirmwareTypes { get; }
    public IReadOnlyList<int> BaudRates { get; }
    public ICommand RefreshPortsCommand { get; }
    public ICommand ConnectCommand { get; }
    public ICommand DisconnectCommand { get; }
    public ICommand RunCommand { get; }
    public ICommand HoldCommand { get; }
    public ICommand ResumeCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand SendManualCommand { get; }
    public ICommand ReopenLastFileCommand { get; }
    public ICommand JogXPositiveCommand { get; }
    public ICommand JogXNegativeCommand { get; }
    public ICommand JogYPositiveCommand { get; }
    public ICommand JogYNegativeCommand { get; }
    public ICommand JogZPositiveCommand { get; }
    public ICommand JogZNegativeCommand { get; }
    public ICommand PreviewZoomInCommand { get; }
    public ICommand PreviewZoomOutCommand { get; }
    public ICommand PreviewAutoFitCommand { get; }
    public ICommand PreviewPanLeftCommand { get; }
    public ICommand PreviewPanRightCommand { get; }
    public ICommand PreviewPanUpCommand { get; }
    public ICommand PreviewPanDownCommand { get; }
    public FirmwareType ActiveFirmware => session.Firmware;

    public SerialPortDescriptor? SelectedPort { get => selectedPort; set { if (Set(ref selectedPort, value)) OnStateChanged(); } }
    public int SelectedBaudRate { get => selectedBaudRate; set => Set(ref selectedBaudRate, value); }
    public FirmwareType SelectedFirmware { get => selectedFirmware; set { if (Set(ref selectedFirmware, value)) session = CreateSession(value); OnPropertyChanged(nameof(ActiveFirmware)); } }
    public string ManualCommand { get => manualCommand; set { if (Set(ref manualCommand, value)) OnStateChanged(); } }
    public string StatusText { get => statusText; private set => Set(ref statusText, value); }
    public string MachineCoordinates { get => machineCoordinates; private set => Set(ref machineCoordinates, value); }
    public string? LastFilePath { get => lastFilePath; private set => Set(ref lastFilePath, value); }
    public PreviewSceneModel PreviewScene { get => previewScene; private set => Set(ref previewScene, value); }
    public PreviewInteractionState PreviewInteraction { get => previewInteraction; private set => Set(ref previewInteraction, value); }
    public bool IsConnected => connection is not null && session.Status != MachineStatus.Disconnected;
    public bool IsBusy { get => isBusy; private set { if (Set(ref isBusy, value)) OnStateChanged(); } }
    public bool HasLoadedFile { get => hasLoadedFile; private set { if (Set(ref hasLoadedFile, value)) OnStateChanged(); } }
    public bool IsJobActive { get => isJobActive; private set { if (Set(ref isJobActive, value)) OnStateChanged(); } }
    public bool IsPaused { get => isPaused; private set { if (Set(ref isPaused, value)) OnStateChanged(); } }
    public bool CanRefreshPorts => !IsBusy && !IsConnected;
    public bool CanConnect => !IsBusy && !IsConnected && SelectedPort is not null;
    public bool CanDisconnect => !IsBusy && IsConnected;
    public bool CanRun => !IsBusy && IsConnected && HasLoadedFile && !IsJobActive;
    public bool CanHold => !IsBusy && IsConnected && IsJobActive && !IsPaused;
    public bool CanResume => !IsBusy && IsConnected && IsJobActive && IsPaused;
    public bool CanReset => !IsBusy && IsConnected;
    public bool CanStop => !IsBusy && IsConnected && IsJobActive;
    public bool CanJog => !IsBusy && IsConnected && !IsJobActive;
    public bool CanSendManualCommand => !IsBusy && IsConnected && !string.IsNullOrWhiteSpace(ManualCommand);
    public bool CanReopenLastFile => !IsBusy && LastFilePath is not null;

    public async Task RefreshPortsAsync(CancellationToken cancellationToken = default)
    {
        await RunUiOperationAsync(async () =>
        {
            var result = await serialPorts.ListAsync(cancellationToken);
            if (!result.Succeeded || result.Value is null)
            {
                await ReportErrorAsync("Unable to refresh ports.", result.Error, cancellationToken);
                return;
            }

            Ports.Clear();
            foreach (var port in result.Value) Ports.Add(port);
            SelectedPort ??= Ports.FirstOrDefault();
            AddLog($"Found {Ports.Count} serial port(s).");
        });
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (SelectedPort is null)
        {
            await ReportErrorAsync("Select a serial port before connecting.", null, cancellationToken);
            return;
        }

        await RunUiOperationAsync(async () =>
        {
            var result = await serialPorts.OpenAsync(SelectedPort, new SerialPortOptions(SelectedBaudRate), cancellationToken);
            if (!result.Succeeded || result.Value is null)
            {
                await ReportErrorAsync("Unable to open serial connection.", result.Error, cancellationToken);
                return;
            }

            connection = result.Value;
            await connection.OpenAsync(cancellationToken);
            session = CreateSession(SelectedFirmware);
            await session.ConnectAsync(cancellationToken);
            await session.ReceiveLineAsync($"{SelectedFirmware} connected", cancellationToken);
            StatusText = $"{session.Status} on {SelectedPort.DisplayName}";
            AddLog($"Connected to {SelectedPort.DisplayName} at {SelectedBaudRate} baud as {SelectedFirmware}.");
        });
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        await RunUiOperationAsync(async () =>
        {
            await ReleaseInhibitorAsync();
            IsJobActive = false;
            IsPaused = false;
            if (connection is not null) await connection.DisposeAsync();
            connection = null;
            await session.DisconnectAsync(cancellationToken);
            StatusText = "Disconnected";
            AddLog("Disconnected.");
        });
    }

    public async Task LoadFileAsync(string path, bool append = false, CancellationToken cancellationToken = default)
    {
        await RunUiOperationAsync(async () =>
        {
            var type = GCodeFileRouter.Classify(path);
            if (type != GCodeFileType.GCode)
            {
                await ReportErrorAsync($"{type} routing is recognized but conversion belongs to a later task.", null, cancellationToken);
                LastFilePath = path;
                return;
            }

            var result = await GCodeImportService.ImportAsync(job, path, append, cancellationToken);
            if (!result.Succeeded)
            {
                await ReportErrorAsync("Unable to load file.", result.Error, cancellationToken);
                return;
            }

            HasLoadedFile = job.Lines.Count > 0;
            LastFilePath = path;
            UpdatePreview(progress: 0);
            AddLog($"{(append ? "Appended" : "Loaded")} {job.Lines.Count} G-code line(s) from {Path.GetFileName(path)}.");
        });
    }

    public Task ReopenLastFileAsync(CancellationToken cancellationToken = default) =>
        LastFilePath is null ? Task.CompletedTask : LoadFileAsync(LastFilePath, append: false, cancellationToken);

    public async Task RunJobAsync(CancellationToken cancellationToken = default)
    {
        if (connection is null || !HasLoadedFile) return;
        await RunUiOperationAsync(async () =>
        {
            var inhibitor = await executionInhibitor.AcquireAsync("LaserGRBL job is active.", cancellationToken);
            if (inhibitor.Succeeded) sleepLease = inhibitor.Value;
            else await ReportErrorAsync("Sleep inhibition is unavailable; continuing job.", inhibitor.Error, cancellationToken, MessageSeverity.Warning);

            await session.StartProgramAsync(job.Render(false, false, false, 1).Select(line => new StreamedCommand(line)), cancellationToken);
            while (await session.PumpStreamingAsync(connection, cancellationToken)) { }
            IsJobActive = true;
            IsPaused = false;
            StatusText = "Run";
            UpdatePreview(progress: 1);
            AddLog("Job started.");
        });
    }

    public async Task HoldAsync(CancellationToken cancellationToken = default)
    {
        if (connection is null) return;
        await connection.WriteRealtimeAsync((byte)'!', cancellationToken);
        IsPaused = true;
        StatusText = "Hold";
        AddLog("Hold requested.");
    }

    public async Task ResumeAsync(CancellationToken cancellationToken = default)
    {
        if (connection is null) return;
        await connection.WriteRealtimeAsync((byte)'~', cancellationToken);
        IsPaused = false;
        StatusText = "Run";
        AddLog("Resume requested.");
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        if (connection is null) return;
        await connection.WriteRealtimeAsync(0x18, cancellationToken);
        await ReleaseInhibitorAsync();
        IsJobActive = false;
        IsPaused = false;
        StatusText = "Reset requested";
        AddLog("Soft reset requested.");
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (connection is null) return;
        await session.AbortProgramAsync(connection, cancellationToken);
        await ReleaseInhibitorAsync();
        IsJobActive = false;
        IsPaused = false;
        StatusText = "Idle";
        AddLog("Job aborted.");
    }

    public async Task SendManualCommandAsync(CancellationToken cancellationToken = default)
    {
        if (connection is null || string.IsNullOrWhiteSpace(ManualCommand)) return;
        var command = ManualCommand.TrimEnd('\r', '\n');
        await connection.WriteAsync(command, cancellationToken);
        AddLog($"> {command}");
        ManualCommand = "";
    }

    public async Task JogAsync(string axis, double distance, double feed, CancellationToken cancellationToken = default)
    {
        if (connection is null || !CanJog) return;
        var direction = axis.ToUpperInvariant() switch
        {
            "X+" => JogDirection.East,
            "X-" => JogDirection.West,
            "Y+" => JogDirection.North,
            "Y-" => JogDirection.South,
            "Z+" => JogDirection.ZUp,
            "Z-" => JogDirection.ZDown,
            _ => (JogDirection?)null
        };
        if (direction is null) return;
        foreach (var command in JogCommandFactory.CreateRelative(direction.Value, (decimal)distance, feed, session.Firmware == FirmwareType.Grbl))
        {
            await connection.WriteAsync(command, cancellationToken);
            AddLog($"> {command}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await ReleaseInhibitorAsync();
        if (connection is not null) await connection.DisposeAsync();
    }

    private MachineSession CreateSession(FirmwareType firmware)
    {
        var next = new MachineSession(firmware, new SystemMonotonicClock(), new ImmediateDispatcher());
        next.PositionChanged += (_, position) => MachineCoordinates = $"X{position.X:0.###} Y{position.Y:0.###} Z{position.Z:0.###}";
        next.PositionChanged += (_, position) => { currentMachinePosition = position; UpdatePreview(PreviewScene.Progress); };
        next.StatusChanged += (_, status) => StatusText = status.ToString();
        return next;
    }

    private void UpdatePreview(double progress)
    {
        PreviewScene = previewRenderer.BuildScene(job, previewStyle, progress, currentMachinePosition);
    }

    private async Task RunUiOperationAsync(Func<Task> action)
    {
        IsBusy = true;
        try { await action(); }
        finally { IsBusy = false; }
    }

    private async Task ReportErrorAsync(string message, OperationError? error, CancellationToken cancellationToken, MessageSeverity severity = MessageSeverity.Error)
    {
        var detail = error?.Detail is null ? message : $"{message} {error.Detail}";
        AddLog(detail);
        await messages.ShowAsync(new MessageRequest("LaserGRBL", detail, severity), cancellationToken);
    }

    private async Task ReleaseInhibitorAsync()
    {
        if (sleepLease is null) return;
        await sleepLease.DisposeAsync();
        sleepLease = null;
    }

    private void AddLog(string line)
    {
        LogLines.Add(line);
        while (LogLines.Count > 300) LogLines.RemoveAt(0);
    }

    private void OnStateChanged()
    {
        foreach (var name in new[] { nameof(IsConnected), nameof(CanRefreshPorts), nameof(CanConnect), nameof(CanDisconnect), nameof(CanRun), nameof(CanHold), nameof(CanResume), nameof(CanReset), nameof(CanStop), nameof(CanJog), nameof(CanSendManualCommand), nameof(CanReopenLastFile) })
            OnPropertyChanged(name);
        foreach (var command in new[] { RefreshPortsCommand, ConnectCommand, DisconnectCommand, RunCommand, HoldCommand, ResumeCommand, ResetCommand, StopCommand, SendManualCommand, ReopenLastFileCommand, JogXPositiveCommand, JogXNegativeCommand, JogYPositiveCommand, JogYNegativeCommand, JogZPositiveCommand, JogZNegativeCommand }.OfType<AsyncWorkflowCommand>())
            command.RaiseCanExecuteChanged();
    }

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private sealed class ImmediateDispatcher : IUiDispatcher
    {
        public bool CheckAccess() => true;
        public Task InvokeAsync(Action action, CancellationToken cancellationToken = default) { action(); return Task.CompletedTask; }
    }

    private sealed class SystemMonotonicClock : IMonotonicClock
    {
        private readonly DateTimeOffset started = DateTimeOffset.UtcNow;
        public TimeSpan Elapsed => DateTimeOffset.UtcNow - started;
    }

    private sealed class AsyncWorkflowCommand(Func<Task> execute, Func<bool> canExecute) : ICommand
    {
        private bool running;
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => !running && canExecute();
        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter)) return;
            running = true;
            RaiseCanExecuteChanged();
            try { await execute(); }
            finally { running = false; RaiseCanExecuteChanged(); }
        }

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    private sealed class RelayCommand(Action execute) : ICommand
    {
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => execute();
    }
}
