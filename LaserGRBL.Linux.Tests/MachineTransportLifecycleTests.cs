using System.Runtime.CompilerServices;
using LaserGRBL.Core.Abstractions;
using LaserGRBL.Core.Protocol;
using Xunit;

namespace LaserGRBL.Linux.Tests;

public sealed class MachineTransportLifecycleTests
{
    [Fact]
    public async Task Receive_loop_consumes_controller_lines_and_closes_the_session_when_transport_ends()
    {
        var session = new MachineSession(FirmwareType.Grbl, new FakeClock(), new InlineDispatcher());
        await session.ConnectAsync();

        await session.RunReceiveLoopAsync(new ScriptedTransport("Grbl 1.1f ['$' for help]", "<Idle|MPos:1,2,3>"));

        Assert.Equal(new GrblVersion(1, 1, 'f'), session.GrblVersion);
        Assert.Equal(new MachinePosition(1, 2, 3), session.MachinePosition);
        Assert.Equal(MachineStatus.Disconnected, session.Status);
        Assert.Equal(MachineIssue.UnexpectedDisconnect, session.LastIssue);
    }

    private sealed class FakeClock : IMonotonicClock { public TimeSpan Elapsed => TimeSpan.Zero; }
    private sealed class InlineDispatcher : IUiDispatcher { public bool CheckAccess() => true; public Task InvokeAsync(Action action, CancellationToken cancellationToken = default) { action(); return Task.CompletedTask; } }
    private sealed class ScriptedTransport(params string[] lines) : IMachineTransport
    {
        public bool IsOpen => true;
        public Task OpenAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task WriteAsync(string command, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public async IAsyncEnumerable<string> ReadLinesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var line in lines) { yield return line; await Task.Yield(); }
        }
    }
}
