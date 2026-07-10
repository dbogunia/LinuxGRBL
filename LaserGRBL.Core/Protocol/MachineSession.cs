using LaserGRBL.Core.Abstractions;

namespace LaserGRBL.Core.Protocol;

/// <summary>UI-independent machine state and streaming coordinator for the ported communication path.</summary>
public sealed class MachineSession
{
    private readonly IFirmwareProtocol protocol;
    private readonly IUiDispatcher dispatcher;
    private readonly CommandStreamingSession streaming;
    private readonly ContinuousJogController continuousJog = new();
    private readonly IMonotonicClock clock;
    private int lastVigoManaged;
    private int lastVigoErrors;
    private TimeSpan? connectionStarted;
    private readonly Dictionary<int, GrblSetting> configuration = [];
    private bool programActive;

    public MachineSession(FirmwareType firmware, IMonotonicClock clock, IUiDispatcher dispatcher, StreamingMode? streamingMode = null)
    {
        protocol = FirmwareProtocols.For(firmware);
        this.clock = clock;
        this.dispatcher = dispatcher;
        streaming = new CommandStreamingSession(clock, streamingMode ?? (protocol.UsesSynchronousStreaming ? StreamingMode.Synchronous : StreamingMode.Buffered));
    }

    public FirmwareType Firmware => protocol.Firmware;

    public MachineStatus Status { get; private set; } = MachineStatus.Disconnected;

    public GrblVersion? GrblVersion { get; private set; }

    public MachinePosition? MachinePosition { get; private set; }

    public OverrideState CurrentOverrides { get; private set; } = OverrideState.Default;

    public OverrideState TargetOverrides { get; private set; } = OverrideState.Default;

    public VigoStatusReport? LastVigoStatus { get; private set; }

    public CommandStreamingSession Streaming => streaming;

    public MachineIssue LastIssue { get; private set; }

    public bool IsProgramActive => programActive;

    public IReadOnlyDictionary<int, GrblSetting> Configuration => configuration;

    public event EventHandler<MachineStatus>? StatusChanged;

    public event EventHandler<MachineIssue>? IssueDetected;

    public event EventHandler<GrblSetting>? ConfigurationChanged;

    public event EventHandler? ProgramEnded;

    public event EventHandler<MachinePosition>? PositionChanged;

    public event EventHandler<OverrideState>? OverridesChanged;

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        connectionStarted = clock.Elapsed;
        await SetStatusAsync(MachineStatus.Connecting, cancellationToken);
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default) =>
        await SetStatusAsync(MachineStatus.Disconnected, cancellationToken);

    public async Task<bool> CheckConnectionTimeoutAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        if (Status != MachineStatus.Connecting || connectionStarted is null || clock.Elapsed - connectionStarted <= timeout) return false;
        await ReportIssueAsync(MachineIssue.UnexpectedDisconnect, cancellationToken);
        await SetStatusAsync(MachineStatus.Disconnected, cancellationToken);
        return true;
    }

    public async Task HandleTransportClosedAsync(CancellationToken cancellationToken = default)
    {
        if (Status is not MachineStatus.Disconnected)
            await ReportIssueAsync(MachineIssue.UnexpectedDisconnect, cancellationToken);
        await SetStatusAsync(MachineStatus.Disconnected, cancellationToken);
    }

    public async Task RunReceiveLoopAsync(IMachineTransport transport, CancellationToken cancellationToken = default)
    {
        try
        {
            await foreach (var line in transport.ReadLinesAsync(cancellationToken))
                await ReceiveLineAsync(line, cancellationToken);
        }
        finally
        {
            await HandleTransportClosedAsync(CancellationToken.None);
        }
    }

    public async Task ReceiveLineAsync(string line, CancellationToken cancellationToken = default)
    {
        if (GrblConfigurationParser.TryParse(line, out var setting) && setting is not null)
        {
            configuration[setting.Number] = setting;
            await dispatcher.InvokeAsync(() => ConfigurationChanged?.Invoke(this, setting), cancellationToken);
            return;
        }

        if (line.StartsWith("ALARM:", StringComparison.OrdinalIgnoreCase))
        {
            await ReportIssueAsync(MachineIssue.ControllerAlarm, cancellationToken);
            await SetStatusAsync(MachineStatus.Alarm, cancellationToken);
            return;
        }

        if (FirmwareWelcomeParser.TryParse(line, GrblVersion?.VendorInfo, GrblVersion?.VendorVersion, out var version) && version is not null)
        {
            GrblVersion = version;
            if (Status == MachineStatus.Connecting) await SetStatusAsync(MachineStatus.Idle, cancellationToken);
            return;
        }

        if (protocol.Firmware == FirmwareType.Grbl && GrblStatusParser.TryParse(line, GrblVersion, out var grblReport) && grblReport is not null)
        {
            if (grblReport.MachinePosition is { } position && position != MachinePosition)
            {
                MachinePosition = position;
                await dispatcher.InvokeAsync(() => PositionChanged?.Invoke(this, position), cancellationToken);
            }
            if (grblReport.FeedOverride is not null && grblReport.RapidOverride is not null && grblReport.SpindleOverride is not null)
            {
                var overrides = new OverrideState(grblReport.FeedOverride.Value, grblReport.RapidOverride.Value, grblReport.SpindleOverride.Value);
                if (overrides != CurrentOverrides)
                {
                    CurrentOverrides = overrides;
                    await dispatcher.InvokeAsync(() => OverridesChanged?.Invoke(this, overrides), cancellationToken);
                }
            }
            await SetStatusAsync(grblReport.Status, cancellationToken);
            return;
        }

        if (protocol.Firmware == FirmwareType.Marlin && FirmwareStatusParsers.TryParseMarlin(line, streaming.Pending is not null, out var marlinReport) && marlinReport is not null)
        {
            if (MachinePosition != marlinReport.Position)
            {
                MachinePosition = marlinReport.Position;
                await dispatcher.InvokeAsync(() => PositionChanged?.Invoke(this, marlinReport.Position), cancellationToken);
            }
            await SetStatusAsync(marlinReport.Status, cancellationToken);
            return;
        }

        if (protocol.Firmware == FirmwareType.VigoWork && FirmwareStatusParsers.TryParseVigo(line, out var vigoReport) && vigoReport is not null)
        {
            LastVigoStatus = vigoReport;
            if (vigoReport.Received < lastVigoManaged)
            {
                lastVigoManaged = 0;
                lastVigoErrors = 0;
            }

            while (lastVigoManaged < vigoReport.Managed)
            {
                streaming.HandleResponse(true);
                lastVigoManaged++;
            }

            while (lastVigoErrors < vigoReport.Errors)
            {
                streaming.HandleResponse(false, "error:99");
                lastVigoErrors++;
            }

            return;
        }

        if (line.Equals("ok", StringComparison.OrdinalIgnoreCase))
        {
            streaming.HandleResponse(true);
            if (Status == MachineStatus.Connecting) await SetStatusAsync(MachineStatus.Idle, cancellationToken);
            await CompleteProgramIfIdleAsync(cancellationToken);
            return;
        }

        if (line.StartsWith("error:", StringComparison.OrdinalIgnoreCase))
        {
            streaming.HandleResponse(false, line);
            await ReportIssueAsync(MachineIssue.CommandRejected, cancellationToken);
        }
    }

    public void SetGrblVersion(GrblVersion version) => GrblVersion = version;

    public async Task StartProgramAsync(IEnumerable<StreamedCommand> commands, CancellationToken cancellationToken = default)
    {
        if (programActive) throw new InvalidOperationException("A program is already active.");
        foreach (var command in commands) streaming.Enqueue(command);
        programActive = streaming.QueuedCount > 0;
        if (programActive) await SetStatusAsync(MachineStatus.Queue, cancellationToken);
    }

    public async Task<bool> PumpStreamingAsync(ICommandTransport transport, CancellationToken cancellationToken = default)
    {
        var sent = await streaming.PumpAsync(transport, cancellationToken);
        if (sent && programActive && Status == MachineStatus.Queue) await SetStatusAsync(MachineStatus.Run, cancellationToken);
        return sent;
    }

    public async Task<OperationResult> AbortProgramAsync(ICommandTransport transport, CancellationToken cancellationToken = default)
    {
        if (!programActive) return OperationResult.Failure("No active program can be aborted.");
        try
        {
            await transport.WriteAsync("M5", cancellationToken);
            streaming.Cancel();
            programActive = false;
            await ReportIssueAsync(MachineIssue.ManualAbort, cancellationToken);
            await SetStatusAsync(MachineStatus.Idle, cancellationToken);
            return OperationResult.Success();
        }
        catch (Exception exception)
        {
            return OperationResult.Failure("Unable to send the laser-off command while aborting.", exception: exception);
        }
    }

    public void SetOverrideTargets(OverrideState targets) => TargetOverrides = targets;

    public void RequestContinuousJog(JogDirection direction, double speed) => continuousJog.RequestDirection(direction, speed);

    public void RequestContinuousJog(MachinePosition target, double speed) => continuousJog.RequestPosition(target, speed);

    public void AbortContinuousJog() => continuousJog.Abort();

    public async Task<OperationResult> PumpContinuousJogAsync(ICommandTransport transport, CancellationToken cancellationToken = default)
    {
        if (!protocol.SupportsTrueJogging(GrblVersion)) return OperationResult.Failure($"{protocol.Firmware} does not support continuous GRBL jog.");
        var action = continuousJog.TakeNext();
        if (action is null) return OperationResult.Success();
        if (action.AbortPrevious) await transport.WriteRealtimeAsync(0x85, cancellationToken);
        if (action.Command is not null) streaming.Enqueue(new StreamedCommand(action.Command));
        return OperationResult.Success();
    }

    public async Task<OperationResult> ApplyOverridesAsync(ICommandTransport transport, CancellationToken cancellationToken = default)
    {
        if (!protocol.SupportsGrblRealtimeCommands)
            return OperationResult.Failure($"{protocol.Firmware} does not support GRBL realtime overrides.");

        foreach (var command in new[]
        {
            GrblOverrideCommands.NextCommand(OverrideChannel.Feed, CurrentOverrides.Feed, TargetOverrides.Feed),
            GrblOverrideCommands.NextCommand(OverrideChannel.Rapid, CurrentOverrides.Rapid, TargetOverrides.Rapid),
            GrblOverrideCommands.NextCommand(OverrideChannel.Spindle, CurrentOverrides.Spindle, TargetOverrides.Spindle)
        })
        {
            if (command is not null) await transport.WriteRealtimeAsync(command.Value, cancellationToken);
        }

        return OperationResult.Success();
    }

    public async Task<bool> CheckStreamingWatchdogAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        if (!streaming.IsTimedOut(timeout)) return false;
        await ReportIssueAsync(MachineIssue.StreamTimeout, cancellationToken);
        await SetStatusAsync(MachineStatus.Alarm, cancellationToken);
        return true;
    }

    public async Task<OperationResult> FeedHoldAsync(ICommandTransport transport, CancellationToken cancellationToken = default) =>
        await SendRealtimeAsync(transport, (byte)'!', MachineStatus.Hold, cancellationToken);

    public async Task<OperationResult> ResumeAsync(ICommandTransport transport, CancellationToken cancellationToken = default) =>
        await SendRealtimeAsync(transport, (byte)'~', MachineStatus.Run, cancellationToken);

    public async Task<OperationResult> ResetAsync(ICommandTransport transport, CancellationToken cancellationToken = default) =>
        await SendRealtimeAsync(transport, 0x18, MachineStatus.Connecting, cancellationToken);

    public async Task<OperationResult> QueryPositionAsync(ICommandTransport transport, CancellationToken cancellationToken = default)
    {
        try
        {
            await transport.WriteAsync(protocol.PositionQuery, cancellationToken);
            return OperationResult.Success();
        }
        catch (Exception exception)
        {
            return OperationResult.Failure("Unable to request machine position.", exception: exception);
        }
    }

    private async Task<OperationResult> SendRealtimeAsync(ICommandTransport transport, byte command, MachineStatus targetStatus, CancellationToken cancellationToken)
    {
        if (!protocol.SupportsGrblRealtimeCommands)
            return OperationResult.Failure($"{protocol.Firmware} does not support GRBL realtime commands.");
        await transport.WriteRealtimeAsync(command, cancellationToken);
        await SetStatusAsync(targetStatus, cancellationToken);
        return OperationResult.Success();
    }

    private async Task SetStatusAsync(MachineStatus status, CancellationToken cancellationToken)
    {
        if (Status == status) return;
        Status = status;
        if (status != MachineStatus.Connecting) connectionStarted = null;
        await dispatcher.InvokeAsync(() => StatusChanged?.Invoke(this, status), cancellationToken);
    }

    private async Task ReportIssueAsync(MachineIssue issue, CancellationToken cancellationToken)
    {
        LastIssue = issue;
        await dispatcher.InvokeAsync(() => IssueDetected?.Invoke(this, issue), cancellationToken);
    }

    private async Task CompleteProgramIfIdleAsync(CancellationToken cancellationToken)
    {
        if (!programActive || !streaming.IsIdle) return;
        programActive = false;
        await SetStatusAsync(MachineStatus.Idle, cancellationToken);
        await dispatcher.InvokeAsync(() => ProgramEnded?.Invoke(this, EventArgs.Empty), cancellationToken);
    }
}
