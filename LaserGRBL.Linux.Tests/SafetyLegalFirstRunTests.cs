using System.Runtime.CompilerServices;
using LaserGRBL.Avalonia.Services;
using LaserGRBL.Avalonia.ViewModels;
using LaserGRBL.Core.Abstractions;
using LaserGRBL.Core.Protocol;
using LaserGRBL.Core.Safety;
using LaserGRBL.Core.Settings;
using LaserGRBL.Platform.Contracts;
using LaserGRBL.Platform.Implementations;
using Xunit;

namespace LaserGRBL.Linux.Tests;

public sealed class SafetyLegalFirstRunTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"lasergrbl-safety-{Guid.NewGuid():N}");

    [Fact]
    public async Task First_run_acknowledgement_persists_through_settings()
    {
        var store = new JsonSettingsStore(new TestPaths(directory));
        var service = new SafetyAcknowledgementService(SafetyAcknowledgementState.Empty);
        var accepted = service.AcceptFirstRun();

        await store.SaveAsync(PortSettings.Default with { SafetyAcknowledgements = accepted });
        var loaded = await store.LoadAsync();

        Assert.True(loaded.Value?.SafetyAcknowledgements?.FirstRunSafetyAccepted);
        Assert.Equal(SafetyAcknowledgementService.CurrentLegalNoticeVersion, loaded.Value?.SafetyAcknowledgements?.LegalNoticeVersion);
    }

    [Fact]
    public void Safety_countdown_completes_or_cancels_explicitly()
    {
        var countdown = new SafetyCountdown(2);

        Assert.False(countdown.IsComplete);
        Assert.True(countdown.Tick().Succeeded);
        Assert.Equal(1, countdown.RemainingTicks);
        Assert.True(countdown.Tick().Succeeded);
        Assert.True(countdown.IsComplete);

        var cancelled = new SafetyCountdown(2);
        cancelled.Cancel();
        Assert.False(cancelled.Tick().Succeeded);
    }

    [Fact]
    public async Task Job_start_is_blocked_before_required_acknowledgement()
    {
        var path = Path.Combine(directory, "job.gcode");
        Directory.CreateDirectory(directory);
        await File.WriteAllLinesAsync(path, ["G0 X1"]);
        var serial = new FakeSerialPortService();
        var messages = new FakeMessages();
        var workflow = new MainWorkflowViewModel(serial, new FakeInhibitor(), messages, safetyGate: new SafetyAcknowledgementService(SafetyAcknowledgementState.Empty));

        await workflow.RefreshPortsAsync();
        await workflow.ConnectAsync();
        await workflow.LoadFileAsync(path);
        await workflow.RunJobAsync();

        Assert.False(workflow.IsJobActive);
        Assert.Empty(serial.Connection.Writes);
        Assert.Contains(messages.Requests, request => request.Message.Contains("Safety acknowledgement", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Reset_and_abort_are_blocked_before_required_acknowledgement()
    {
        var serial = new FakeSerialPortService();
        var workflow = new MainWorkflowViewModel(serial, new FakeInhibitor(), new FakeMessages(), safetyGate: new SafetyAcknowledgementService(SafetyAcknowledgementState.Empty));

        await workflow.RefreshPortsAsync();
        await workflow.ConnectAsync();
        await workflow.ResetAsync();
        await workflow.StopAsync();

        Assert.Empty(serial.Connection.Writes);
    }

    [Fact]
    public async Task Firmware_flash_is_blocked_before_required_acknowledgement()
    {
        var firmware = new FakeFirmwareFlash();
        var messages = new FakeMessages();
        var tool = new FirmwareFlashToolViewModel(firmware, new FakeFiles(), messages, new SafetyAcknowledgementService(SafetyAcknowledgementState.Empty));

        var result = await tool.FlashAsync();

        Assert.False(result.Succeeded);
        Assert.Null(firmware.LastRequest);
        Assert.Contains("blocked", tool.Status, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Corrupt_settings_fail_closed_for_risky_operations()
    {
        Directory.CreateDirectory(directory);
        var store = new JsonSettingsStore(new TestPaths(directory));
        await File.WriteAllTextAsync(store.FilePath, "{ bad");

        var loaded = await store.LoadAsync();
        var service = new SafetyAcknowledgementService(loaded.Value?.SafetyAcknowledgements);
        var result = service.EnsureAllowed(RiskyOperation.StartJob);

        Assert.True(loaded.Succeeded);
        Assert.False(result.Succeeded);
    }

    [Fact]
    public void Safety_messages_are_localized()
    {
        var polish = LocalizationCatalog.Default.ForCulture("pl-PL");

        Assert.Contains("Potwierdź", polish.Get("Safety.StartJob"));
        Assert.Equal("Review laser safety and legal warnings before starting a job.", LocalizationCatalog.Default.Get("Safety.StartJob"));
    }

    public void Dispose()
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
    }

    private sealed class FakeMessages : IMessageService
    {
        public List<MessageRequest> Requests { get; } = [];
        public Task<bool> ShowAsync(MessageRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.FromResult(true);
        }
    }

    private sealed class FakeInhibitor : IExecutionInhibitor
    {
        public Task<OperationResult<IAsyncDisposable?>> AcquireAsync(string reason, CancellationToken cancellationToken = default) =>
            Task.FromResult(OperationResult<IAsyncDisposable?>.Success(null));
    }

    private sealed class FakeFiles : IFileDialogService
    {
        public Task<OperationResult<IReadOnlyList<string>>> OpenAsync(FileDialogRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(OperationResult<IReadOnlyList<string>>.Failure("No file."));

        public Task<OperationResult<string>> SaveAsync(FileDialogRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(OperationResult<string>.Failure("No file."));
    }

    private sealed class FakeFirmwareFlash : IFirmwareFlashService
    {
        public FirmwareFlashRequest? LastRequest { get; private set; }
        public Task<OperationResult> FlashAsync(FirmwareFlashRequest request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(OperationResult.Success());
        }
    }

    private sealed class FakeSerialPortService : ISerialPortService
    {
        private readonly SerialPortDescriptor port = new("ttyUSB0", "ttyUSB0", "/dev/ttyUSB0");
        public FakeSerialConnection Connection { get; } = new();
        public Task<OperationResult<IReadOnlyList<SerialPortDescriptor>>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(OperationResult<IReadOnlyList<SerialPortDescriptor>>.Success([port]));
        public Task<OperationResult<ISerialConnection>> OpenAsync(SerialPortDescriptor port, SerialPortOptions options, CancellationToken cancellationToken = default) =>
            Task.FromResult(OperationResult<ISerialConnection>.Success(Connection));
    }

    private sealed class FakeSerialConnection : ISerialConnection
    {
        public SerialPortDescriptor Port => new("ttyUSB0", "ttyUSB0", "/dev/ttyUSB0");
        public SerialPortOptions Options => new();
        public bool IsOpen { get; private set; }
        public List<string> Writes { get; } = [];
        public Task OpenAsync(CancellationToken cancellationToken = default) { IsOpen = true; return Task.CompletedTask; }
        public Task DiscardBuffersAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task WriteAsync(string command, CancellationToken cancellationToken = default) { Writes.Add(command); return Task.CompletedTask; }
        public async IAsyncEnumerable<string> ReadLinesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default) { await Task.CompletedTask; yield break; }
        public ValueTask DisposeAsync() { IsOpen = false; return ValueTask.CompletedTask; }
    }

    private sealed class TestPaths(string root) : IAppPaths
    {
        public string DataDirectory => root;
        public string ConfigDirectory => root;
        public string CacheDirectory => root;
        public string LogDirectory => root;
    }
}
