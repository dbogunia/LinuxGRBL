using System.Runtime.CompilerServices;
using LaserGRBL.Core.Abstractions;
using LaserGRBL.Core.Protocol;
using LaserGRBL.Platform.Implementations;
using Xunit;

namespace LaserGRBL.Linux.Tests;

public sealed class MachineShutdownCoordinatorTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "linuxgrbl-locks-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Ordinary_close_while_idle_disconnects_and_disposes_transport_without_safety_command()
    {
        var session = new MachineSession(FirmwareType.Grbl, new FakeClock(), new InlineDispatcher());
        var transport = new FakeMachineTransport();
        await session.ConnectAsync();
        await session.ReceiveLineAsync("Grbl 1.1f ['$' for help]");

        var result = await new MachineShutdownCoordinator(session).ShutdownAsync(transport, new MachineShutdownRequest(MachineShutdownReason.ApplicationClose, TimeSpan.FromSeconds(1)));

        Assert.False(result.SafetyAttempted);
        Assert.True(result.TransportDisposed);
        Assert.Equal(MachineStatus.Disconnected, session.Status);
        Assert.Empty(transport.Writes);
    }

    [Fact]
    public async Task Ordinary_close_while_active_job_sends_bounded_laser_off_sequence()
    {
        var session = new MachineSession(FirmwareType.Grbl, new FakeClock(), new InlineDispatcher());
        var transport = new FakeMachineTransport();
        await session.StartProgramAsync([new StreamedCommand("G1 X10")]);

        var result = await new MachineShutdownCoordinator(session).ShutdownAsync(transport, new MachineShutdownRequest(MachineShutdownReason.ApplicationClose, TimeSpan.FromSeconds(1)));

        Assert.True(result.SafetyAttempted);
        Assert.True(result.SafetySucceeded);
        Assert.Contains("M5", transport.Writes);
        Assert.Equal(MachineIssue.ManualAbort, result.Issue);
        Assert.Equal(MachineStatus.Disconnected, session.Status);
    }

    [Fact]
    public async Task Failed_safety_write_is_visible_but_resources_are_released()
    {
        var session = new MachineSession(FirmwareType.Grbl, new FakeClock(), new InlineDispatcher());
        var transport = new FakeMachineTransport { FailWrites = true };
        await session.StartProgramAsync([new StreamedCommand("G1 X10")]);

        var result = await new MachineShutdownCoordinator(session).ShutdownAsync(transport, new MachineShutdownRequest(MachineShutdownReason.ApplicationClose, TimeSpan.FromSeconds(1)));

        Assert.True(result.SafetyAttempted);
        Assert.False(result.SafetySucceeded);
        Assert.Contains("laser-off", result.Message);
        Assert.True(result.TransportDisposed);
        Assert.Equal(MachineStatus.Disconnected, session.Status);
    }

    [Fact]
    public async Task Transport_failure_marks_unexpected_disconnect_and_does_not_send_after_closed()
    {
        var session = new MachineSession(FirmwareType.Grbl, new FakeClock(), new InlineDispatcher());
        var transport = new FakeMachineTransport { Open = false };
        await session.ConnectAsync();

        var result = await new MachineShutdownCoordinator(session).ShutdownAsync(transport, new MachineShutdownRequest(MachineShutdownReason.TransportFailure, TimeSpan.FromSeconds(1)));

        Assert.False(result.SafetyAttempted);
        Assert.Equal(MachineIssue.UnexpectedDisconnect, result.Issue);
        Assert.Empty(transport.Writes);
    }

    [Fact]
    public void Recovery_refuses_resume_until_identity_position_homing_and_acknowledgement_are_verified()
    {
        Assert.Equal(RecoveryResumeDecision.Refused, MachineRecoveryPolicy.EvaluateResume(new MachineRecoverySnapshot(null, MachinePosition.Zero, true, true)).Decision);
        Assert.Equal(RecoveryResumeDecision.Refused, MachineRecoveryPolicy.EvaluateResume(new MachineRecoverySnapshot("job", null, true, true)).Decision);
        Assert.Equal(RecoveryResumeDecision.Refused, MachineRecoveryPolicy.EvaluateResume(new MachineRecoverySnapshot("job", MachinePosition.Zero, false, true)).Decision);
        Assert.Equal(RecoveryResumeDecision.Refused, MachineRecoveryPolicy.EvaluateResume(new MachineRecoverySnapshot("job", MachinePosition.Zero, true, false)).Decision);
        Assert.Equal(RecoveryResumeDecision.Allowed, MachineRecoveryPolicy.EvaluateResume(new MachineRecoverySnapshot("job", MachinePosition.Zero, true, true)).Decision);
    }

    [Fact]
    public async Task Resource_lock_blocks_competing_owner_and_releases_on_disposal()
    {
        var provider = new FileMachineResourceLockProvider(directory);
        var first = provider.TryAcquire("/dev/serial/by-id/usb-grbl");
        var second = provider.TryAcquire("/dev/serial/by-id/usb-grbl");

        Assert.True(first.Succeeded, first.Error?.Message);
        Assert.False(second.Succeeded);
        Assert.Contains("already owned", second.Error?.Message);

        await first.Value!.DisposeAsync();
        var third = provider.TryAcquire("/dev/serial/by-id/usb-grbl");

        Assert.True(third.Succeeded, third.Error?.Message);
        await third.Value!.DisposeAsync();
    }

    public void Dispose()
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, true);
    }

    private sealed class FakeClock : IMonotonicClock { public TimeSpan Elapsed => TimeSpan.Zero; }

    private sealed class InlineDispatcher : IUiDispatcher
    {
        public bool CheckAccess() => true;
        public Task InvokeAsync(Action action, CancellationToken cancellationToken = default) { action(); return Task.CompletedTask; }
    }

    private sealed class FakeMachineTransport : IMachineTransport
    {
        public List<string> Writes { get; } = [];
        public bool FailWrites { get; init; }
        public bool Open { get; init; } = true;
        public bool IsOpen => Open;
        public bool Disposed { get; private set; }

        public Task OpenAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task WriteAsync(string command, CancellationToken cancellationToken = default)
        {
            if (FailWrites) throw new IOException("write failed");
            Writes.Add(command);
            return Task.CompletedTask;
        }

        public async IAsyncEnumerable<string> ReadLinesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }
}
