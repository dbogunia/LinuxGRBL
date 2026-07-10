using LaserGRBL.Core.Abstractions;
using LaserGRBL.Core.Protocol;
using Xunit;

namespace LaserGRBL.Linux.Tests;

public sealed class VigoMachineSessionTests
{
    [Fact]
    public async Task Vigo_buffer_acknowledgement_completes_the_pending_command()
    {
        var session = new MachineSession(FirmwareType.VigoWork, new FakeClock(), new InlineDispatcher());
        session.Streaming.Enqueue(new StreamedCommand("G1 X1"));
        await session.Streaming.PumpAsync(new FakeTransport());

        await session.ReceiveLineAsync("<VSta:2|SBuf:1,1,0>");

        Assert.Null(session.Streaming.Pending);
        Assert.Equal(new VigoStatusReport(2, 1, 1, 0), session.LastVigoStatus);
    }

    private sealed class FakeClock : IMonotonicClock { public TimeSpan Elapsed => TimeSpan.Zero; }
    private sealed class InlineDispatcher : IUiDispatcher { public bool CheckAccess() => true; public Task InvokeAsync(Action action, CancellationToken cancellationToken = default) { action(); return Task.CompletedTask; } }
    private sealed class FakeTransport : ICommandTransport { public Task WriteAsync(string command, CancellationToken cancellationToken = default) => Task.CompletedTask; }
}
