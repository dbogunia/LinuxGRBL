using LaserGRBL.Core.Abstractions;
using LaserGRBL.Core.Protocol;
using Xunit;

namespace LaserGRBL.Linux.Tests;

public sealed class MachineSafetyTests
{
    [Fact]
    public async Task Alarm_line_sets_alarm_status_and_issue()
    {
        var session = new MachineSession(FirmwareType.Grbl, new FakeClock(), new InlineDispatcher());
        await session.ReceiveLineAsync("ALARM:2");

        Assert.Equal(MachineStatus.Alarm, session.Status);
        Assert.Equal(MachineIssue.ControllerAlarm, session.LastIssue);
    }

    [Fact]
    public async Task Streaming_watchdog_fails_closed_when_pending_command_times_out()
    {
        var clock = new FakeClock();
        var session = new MachineSession(FirmwareType.Grbl, clock, new InlineDispatcher());
        session.Streaming.Enqueue(new StreamedCommand("G1 X1"));
        await session.Streaming.PumpAsync(new FakeTransport());
        clock.Elapsed = TimeSpan.FromSeconds(6);

        Assert.True(await session.CheckStreamingWatchdogAsync(TimeSpan.FromSeconds(5)));
        Assert.Equal(MachineStatus.Alarm, session.Status);
        Assert.Equal(MachineIssue.StreamTimeout, session.LastIssue);
    }

    [Fact]
    public async Task Transport_closure_marks_an_active_session_as_unexpected_disconnect()
    {
        var session = new MachineSession(FirmwareType.Grbl, new FakeClock(), new InlineDispatcher());
        await session.ConnectAsync();

        await session.HandleTransportClosedAsync();

        Assert.Equal(MachineStatus.Disconnected, session.Status);
        Assert.Equal(MachineIssue.UnexpectedDisconnect, session.LastIssue);
    }

    [Fact]
    public async Task Connection_timeout_uses_monotonic_elapsed_time()
    {
        var clock = new FakeClock();
        var session = new MachineSession(FirmwareType.Grbl, clock, new InlineDispatcher());
        await session.ConnectAsync();
        clock.Elapsed = TimeSpan.FromSeconds(11);

        Assert.True(await session.CheckConnectionTimeoutAsync(TimeSpan.FromSeconds(10)));
        Assert.Equal(MachineStatus.Disconnected, session.Status);
        Assert.Equal(MachineIssue.UnexpectedDisconnect, session.LastIssue);
    }

    private sealed class FakeClock : IMonotonicClock { public TimeSpan Elapsed { get; set; } }
    private sealed class InlineDispatcher : IUiDispatcher { public bool CheckAccess() => true; public Task InvokeAsync(Action action, CancellationToken cancellationToken = default) { action(); return Task.CompletedTask; } }
    private sealed class FakeTransport : ICommandTransport { public Task WriteAsync(string command, CancellationToken cancellationToken = default) => Task.CompletedTask; }
}
