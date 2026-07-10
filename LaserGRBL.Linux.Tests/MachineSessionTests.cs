using LaserGRBL.Core.Abstractions;
using LaserGRBL.Core.Protocol;
using Xunit;

namespace LaserGRBL.Linux.Tests;

public sealed class MachineSessionTests
{
    [Fact]
    public async Task Grbl_session_transitions_through_connection_and_status_reports()
    {
        var dispatcher = new InlineDispatcher();
        var session = new MachineSession(FirmwareType.Grbl, new FakeClock(), dispatcher);
        var states = new List<MachineStatus>();
        session.StatusChanged += (_, status) => states.Add(status);

        await session.ConnectAsync();
        await session.ReceiveLineAsync("<Idle|MPos:1,2,3|FS:0,0>");
        await session.DisconnectAsync();

        Assert.Equal([MachineStatus.Connecting, MachineStatus.Idle, MachineStatus.Disconnected], states);
        Assert.Equal(new MachinePosition(1, 2, 3), session.MachinePosition);
        Assert.Equal(4, dispatcher.InvocationCount);
    }

    [Fact]
    public async Task Marlin_session_does_not_adopt_grbl_true_jog_semantics()
    {
        var session = new MachineSession(FirmwareType.Marlin, new FakeClock(), new InlineDispatcher());
        session.Streaming.Enqueue(new StreamedCommand("G1 X1"));
        await session.Streaming.PumpAsync(new FakeTransport());

        await session.ReceiveLineAsync("X:4.00 Y:5.00 Z:6.00 E:0.00");

        Assert.Equal(MachineStatus.Run, session.Status);
        Assert.Equal(new MachinePosition(4, 5, 6), session.MachinePosition);
        Assert.False(FirmwareProtocols.For(session.Firmware).SupportsTrueJogging(new GrblVersion(1, 1)));
        Assert.Equal(StreamingMode.Synchronous, session.Streaming.Mode);
    }

    [Fact]
    public async Task Welcome_message_sets_version_and_completes_connection()
    {
        var session = new MachineSession(FirmwareType.Grbl, new FakeClock(), new InlineDispatcher());
        await session.ConnectAsync();

        await session.ReceiveLineAsync("GrblHAL 1.1f ['$' for help]");

        Assert.Equal(new GrblVersion(1, 1, 'f', IsHal: true), session.GrblVersion);
        Assert.Equal(MachineStatus.Idle, session.Status);
    }

    [Fact]
    public async Task Configuration_lines_are_retained_and_published_without_ui_types()
    {
        var session = new MachineSession(FirmwareType.Grbl, new FakeClock(), new InlineDispatcher());
        GrblSetting? changed = null;
        session.ConfigurationChanged += (_, setting) => changed = setting;

        await session.ReceiveLineAsync("$32=1 (Laser mode)");

        Assert.Equal(new GrblSetting(32, "1", "(Laser mode)"), session.Configuration[32]);
        Assert.Equal(session.Configuration[32], changed);
    }

    [Fact]
    public async Task Status_reports_publish_position_and_override_changes()
    {
        var session = new MachineSession(FirmwareType.Grbl, new FakeClock(), new InlineDispatcher());
        MachinePosition? position = null;
        OverrideState? overrides = null;
        session.PositionChanged += (_, value) => position = value;
        session.OverridesChanged += (_, value) => overrides = value;

        await session.ReceiveLineAsync("<Idle|MPos:1,2,3|Ov:90,50,110>");

        Assert.Equal(new MachinePosition(1, 2, 3), position);
        Assert.Equal(new OverrideState(90, 50, 110), overrides);
    }

    private sealed class FakeClock : IMonotonicClock { public TimeSpan Elapsed => TimeSpan.Zero; }

    private sealed class InlineDispatcher : IUiDispatcher
    {
        public int InvocationCount { get; private set; }
        public bool CheckAccess() => true;
        public Task InvokeAsync(Action action, CancellationToken cancellationToken = default) { InvocationCount++; action(); return Task.CompletedTask; }
    }

    private sealed class FakeTransport : ICommandTransport
    {
        public Task WriteAsync(string command, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
