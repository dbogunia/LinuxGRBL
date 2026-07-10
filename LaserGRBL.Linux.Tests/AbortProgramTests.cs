using LaserGRBL.Core.Abstractions;
using LaserGRBL.Core.Protocol;
using Xunit;

namespace LaserGRBL.Linux.Tests;

public sealed class AbortProgramTests
{
    [Fact]
    public async Task Abort_clears_streaming_queue_and_sends_laser_off_command()
    {
        var session = new MachineSession(FirmwareType.Grbl, new FakeClock(), new InlineDispatcher());
        var transport = new FakeTransport();
        await session.StartProgramAsync([new StreamedCommand("G1 X1")]);
        await session.PumpStreamingAsync(transport);

        var result = await session.AbortProgramAsync(transport);

        Assert.True(result.Succeeded);
        Assert.False(session.IsProgramActive);
        Assert.True(session.Streaming.IsIdle);
        Assert.Equal(MachineIssue.ManualAbort, session.LastIssue);
        Assert.Equal(MachineStatus.Idle, session.Status);
        Assert.Equal(["G1 X1", "M5"], transport.Writes);
    }

    private sealed class FakeClock : IMonotonicClock { public TimeSpan Elapsed => TimeSpan.Zero; }
    private sealed class InlineDispatcher : IUiDispatcher { public bool CheckAccess() => true; public Task InvokeAsync(Action action, CancellationToken cancellationToken = default) { action(); return Task.CompletedTask; } }
    private sealed class FakeTransport : ICommandTransport { public List<string> Writes { get; } = []; public Task WriteAsync(string command, CancellationToken cancellationToken = default) { Writes.Add(command); return Task.CompletedTask; } }
}
