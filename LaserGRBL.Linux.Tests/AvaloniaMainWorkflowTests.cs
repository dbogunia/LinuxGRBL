using System.Runtime.CompilerServices;
using LaserGRBL.Avalonia.ViewModels;
using LaserGRBL.Core.Abstractions;
using LaserGRBL.Core.Protocol;
using LaserGRBL.Platform.Contracts;
using Xunit;

namespace LaserGRBL.Linux.Tests;

public sealed class AvaloniaMainWorkflowTests
{
    [Fact]
    public async Task Refresh_connect_disconnect_updates_state()
    {
        var service = new FakeSerialPortService();
        var workflow = CreateWorkflow(service);

        await workflow.RefreshPortsAsync();
        Assert.True(workflow.CanConnect);

        await workflow.ConnectAsync();
        Assert.True(workflow.IsConnected);
        Assert.True(workflow.CanDisconnect);

        await workflow.DisconnectAsync();
        Assert.False(workflow.IsConnected);
    }

    [Fact]
    public void Firmware_selection_creates_matching_session()
    {
        var workflow = CreateWorkflow(new FakeSerialPortService());

        workflow.SelectedFirmware = FirmwareType.Marlin;

        Assert.Equal(FirmwareType.Marlin, workflow.ActiveFirmware);
    }

    [Fact]
    public async Task Manual_command_and_jog_dispatch_to_connection()
    {
        var service = new FakeSerialPortService();
        var workflow = CreateWorkflow(service);
        await workflow.RefreshPortsAsync();
        await workflow.ConnectAsync();

        workflow.ManualCommand = "G0 X1\n";
        await workflow.SendManualCommandAsync();
        await workflow.JogAsync("X+", 1, 1000);

        Assert.Contains("G0 X1", service.Connection.Writes);
        Assert.Contains(service.Connection.Writes, command => command.Contains("X1.0", StringComparison.Ordinal));
    }

    [Fact]
    public async Task File_load_append_and_reopen_update_job_state()
    {
        var path = Path.Combine(Path.GetTempPath(), $"lasergrbl-{Guid.NewGuid():N}.gcode");
        await File.WriteAllLinesAsync(path, ["G0 X1", "G0 Y1"]);
        var workflow = CreateWorkflow(new FakeSerialPortService());

        await workflow.LoadFileAsync(path);
        await workflow.LoadFileAsync(path, append: true);
        await workflow.ReopenLastFileAsync();

        Assert.True(workflow.HasLoadedFile);
        Assert.Equal(path, workflow.LastFilePath);
        Assert.True(workflow.CanReopenLastFile);
        File.Delete(path);
    }

    [Fact]
    public async Task File_load_and_run_update_preview_scene()
    {
        var path = Path.Combine(Path.GetTempPath(), $"lasergrbl-{Guid.NewGuid():N}.gcode");
        await File.WriteAllLinesAsync(path, ["G0 X0 Y0", "G1 X10 Y0", "G1 X10 Y5"]);
        var service = new FakeSerialPortService();
        var workflow = CreateWorkflow(service);

        await workflow.RefreshPortsAsync();
        await workflow.ConnectAsync();
        await workflow.LoadFileAsync(path);

        Assert.False(workflow.PreviewScene.IsEmpty);
        Assert.Equal(2, workflow.PreviewScene.Lines.Count);
        Assert.Equal(0, workflow.PreviewScene.Progress);

        await workflow.RunJobAsync();

        Assert.Equal(1, workflow.PreviewScene.Progress);
        Assert.Equal(workflow.PreviewScene.Lines.Count, workflow.PreviewScene.CompletedLines.Count);
        File.Delete(path);
    }

    [Fact]
    public async Task File_load_failure_routes_message_and_keeps_job_unloaded()
    {
        var messages = new FakeMessageService();
        var workflow = new MainWorkflowViewModel(new FakeSerialPortService(), new FakeExecutionInhibitor(), messages);

        await workflow.LoadFileAsync("/tmp/missing-file.svg");

        Assert.False(workflow.HasLoadedFile);
        Assert.Contains(messages.Requests, request => request.Message.Contains("routing is recognized", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Run_hold_resume_reset_and_stop_manage_commands_and_inhibitor()
    {
        var path = Path.Combine(Path.GetTempPath(), $"lasergrbl-{Guid.NewGuid():N}.gcode");
        await File.WriteAllLinesAsync(path, ["G0 X1"]);
        var service = new FakeSerialPortService();
        var inhibitor = new FakeExecutionInhibitor();
        var workflow = CreateWorkflow(service, inhibitor);
        await workflow.RefreshPortsAsync();
        await workflow.ConnectAsync();
        await workflow.LoadFileAsync(path);

        await workflow.RunJobAsync();
        Assert.True(workflow.IsJobActive);
        Assert.Equal(1, inhibitor.AcquireCount);

        await workflow.HoldAsync();
        await workflow.ResumeAsync();
        await workflow.ResetAsync();

        Assert.False(workflow.IsJobActive);
        Assert.Equal(1, inhibitor.Lease.DisposeCount);
        Assert.Contains("G0 X1", service.Connection.Writes);
        Assert.Contains("!", service.Connection.Writes);
        Assert.Contains("~", service.Connection.Writes);
        Assert.Contains("\u0018", service.Connection.Writes);
        File.Delete(path);
    }

    [Fact]
    public async Task Unavailable_inhibitor_logs_warning_but_allows_run()
    {
        var path = Path.Combine(Path.GetTempPath(), $"lasergrbl-{Guid.NewGuid():N}.gcode");
        await File.WriteAllLinesAsync(path, ["G0 X1"]);
        var service = new FakeSerialPortService();
        var workflow = CreateWorkflow(service, new FakeExecutionInhibitor(succeed: false));
        await workflow.RefreshPortsAsync();
        await workflow.ConnectAsync();
        await workflow.LoadFileAsync(path);

        await workflow.RunJobAsync();

        Assert.True(workflow.IsJobActive);
        Assert.Contains("G0 X1", service.Connection.Writes);
        File.Delete(path);
    }

    [Fact]
    public async Task Connect_uses_machine_resource_lock_and_releases_it_on_disconnect()
    {
        var service = new FakeSerialPortService();
        var locks = new FakeResourceLocks();
        var messages = new FakeMessageService();
        var workflow = new MainWorkflowViewModel(service, new FakeExecutionInhibitor(), messages, resourceLocks: locks);
        await workflow.RefreshPortsAsync();

        await workflow.ConnectAsync();
        Assert.True(workflow.IsConnected);
        Assert.Equal("/dev/ttyUSB0", locks.AcquiredResource);

        await workflow.DisconnectAsync();
        Assert.Equal(1, locks.Lock.DisposeCount);

        locks.Fail = true;
        await workflow.ConnectAsync();
        Assert.False(workflow.IsConnected);
        Assert.Contains(messages.Requests, request => request.Message.Contains("already owned", StringComparison.Ordinal));
    }

    private static MainWorkflowViewModel CreateWorkflow(FakeSerialPortService service, FakeExecutionInhibitor? inhibitor = null) =>
        new(service, inhibitor ?? new FakeExecutionInhibitor(), new FakeMessageService());

    private sealed class FakeMessageService : IMessageService
    {
        public List<MessageRequest> Requests { get; } = [];
        public Task<bool> ShowAsync(MessageRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.FromResult(true);
        }
    }

    private sealed class FakeExecutionInhibitor(bool succeed = true) : IExecutionInhibitor
    {
        public int AcquireCount { get; private set; }
        public FakeLease Lease { get; } = new();
        public Task<OperationResult<IAsyncDisposable?>> AcquireAsync(string reason, CancellationToken cancellationToken = default)
        {
            AcquireCount++;
            return Task.FromResult(succeed ? OperationResult<IAsyncDisposable?>.Success(Lease) : OperationResult<IAsyncDisposable?>.Failure("Unavailable"));
        }
    }

    private sealed class FakeLease : IAsyncDisposable
    {
        public int DisposeCount { get; private set; }
        public ValueTask DisposeAsync() { DisposeCount++; return ValueTask.CompletedTask; }
    }

    private sealed class FakeResourceLocks : IMachineResourceLockProvider
    {
        public FakeResourceLock Lock { get; } = new();
        public string? AcquiredResource { get; private set; }
        public bool Fail { get; set; }

        public OperationResult<IMachineResourceLock> TryAcquire(string resourceId)
        {
            AcquiredResource = resourceId;
            return Fail
                ? OperationResult<IMachineResourceLock>.Failure("Machine resource is already owned by another process.", resourceId)
                : OperationResult<IMachineResourceLock>.Success(Lock);
        }
    }

    private sealed class FakeResourceLock : IMachineResourceLock
    {
        public string ResourceId => "/dev/ttyUSB0";
        public int DisposeCount { get; private set; }
        public ValueTask DisposeAsync() { DisposeCount++; return ValueTask.CompletedTask; }
    }

    private sealed class FakeSerialPortService : ISerialPortService
    {
        private readonly SerialPortDescriptor port = new("ttyUSB0", "ttyUSB0", "/dev/ttyUSB0");
        public FakeSerialConnection Connection { get; } = new();
        public Task<OperationResult<IReadOnlyList<SerialPortDescriptor>>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(OperationResult<IReadOnlyList<SerialPortDescriptor>>.Success([port]));
        public Task<OperationResult<ISerialConnection>> OpenAsync(SerialPortDescriptor port, SerialPortOptions options, CancellationToken cancellationToken = default)
        {
            Connection.PortValue = port;
            Connection.OptionsValue = options;
            return Task.FromResult(OperationResult<ISerialConnection>.Success(Connection));
        }
    }

    private sealed class FakeSerialConnection : ISerialConnection
    {
        public SerialPortDescriptor PortValue { get; set; } = new("ttyUSB0", "ttyUSB0", "/dev/ttyUSB0");
        public SerialPortOptions OptionsValue { get; set; } = new();
        public List<string> Writes { get; } = [];
        public SerialPortDescriptor Port => PortValue;
        public SerialPortOptions Options => OptionsValue;
        public bool IsOpen { get; private set; }
        public Task OpenAsync(CancellationToken cancellationToken = default) { IsOpen = true; return Task.CompletedTask; }
        public Task DiscardBuffersAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task WriteAsync(string command, CancellationToken cancellationToken = default)
        {
            Writes.Add(command);
            return Task.CompletedTask;
        }

        public async IAsyncEnumerable<string> ReadLinesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        public ValueTask DisposeAsync() { IsOpen = false; return ValueTask.CompletedTask; }
    }
}
