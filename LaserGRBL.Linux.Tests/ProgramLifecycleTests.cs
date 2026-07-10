using LaserGRBL.Core.Abstractions;
using LaserGRBL.Core.Protocol;
using Xunit;

namespace LaserGRBL.Linux.Tests;

public sealed class ProgramLifecycleTests
{
    [Fact]
    public async Task Program_ends_only_after_the_last_command_is_acknowledged()
    {
        var session = new MachineSession(FirmwareType.Marlin, new FakeClock(), new InlineDispatcher());
        var transport = new FakeTransport();
        var ended = 0;
        session.ProgramEnded += (_, _) => ended++;
        await session.StartProgramAsync([new StreamedCommand("G1 X1"), new StreamedCommand("G1 X2")]);

        Assert.Equal(MachineStatus.Queue, session.Status);
        Assert.True(await session.PumpStreamingAsync(transport));
        Assert.Equal(MachineStatus.Run, session.Status);
        await session.ReceiveLineAsync("ok");
        Assert.True(session.IsProgramActive);
        Assert.True(await session.PumpStreamingAsync(transport));
        await session.ReceiveLineAsync("ok");

        Assert.False(session.IsProgramActive);
        Assert.Equal(MachineStatus.Idle, session.Status);
        Assert.Equal(1, ended);
        Assert.Equal(["G1 X1", "G1 X2"], transport.Writes);
    }

    private sealed class FakeClock : IMonotonicClock { public TimeSpan Elapsed => TimeSpan.Zero; }
    private sealed class InlineDispatcher : IUiDispatcher { public bool CheckAccess() => true; public Task InvokeAsync(Action action, CancellationToken cancellationToken = default) { action(); return Task.CompletedTask; } }
    private sealed class FakeTransport : ICommandTransport { public List<string> Writes { get; } = []; public Task WriteAsync(string command, CancellationToken cancellationToken = default) { Writes.Add(command); return Task.CompletedTask; } }
}
