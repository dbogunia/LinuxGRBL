using LaserGRBL.Core.Abstractions;
using LaserGRBL.Core.Protocol;
using Xunit;

namespace LaserGRBL.Linux.Tests;

public sealed class MachineControlTests
{
    [Fact]
    public async Task Grbl_realtime_controls_write_protocol_bytes_and_update_state()
    {
        var transport = new FakeTransport();
        var session = new MachineSession(FirmwareType.Grbl, new FakeClock(), new InlineDispatcher());

        Assert.True((await session.FeedHoldAsync(transport)).Succeeded);
        Assert.Equal(MachineStatus.Hold, session.Status);
        Assert.True((await session.ResumeAsync(transport)).Succeeded);
        Assert.Equal(MachineStatus.Run, session.Status);
        Assert.True((await session.ResetAsync(transport)).Succeeded);
        Assert.Equal([(byte)'!', (byte)'~', (byte)0x18], transport.Realtime);
    }

    [Fact]
    public async Task Marlin_rejects_grbl_realtime_controls_without_fallback()
    {
        var result = await new MachineSession(FirmwareType.Marlin, new FakeClock(), new InlineDispatcher()).FeedHoldAsync(new FakeTransport());

        Assert.False(result.Succeeded);
        Assert.Contains("does not support", result.Error?.Message);
    }

    [Fact]
    public async Task Firmware_position_queries_are_preserved()
    {
        var transport = new FakeTransport();
        var session = new MachineSession(FirmwareType.Marlin, new FakeClock(), new InlineDispatcher());

        Assert.True((await session.QueryPositionAsync(transport)).Succeeded);
        Assert.Equal(["M114\n"], transport.Commands);
    }

    [Fact]
    public async Task Applies_override_targets_from_latest_grbl_status_report()
    {
        var transport = new FakeTransport();
        var session = new MachineSession(FirmwareType.Grbl, new FakeClock(), new InlineDispatcher());
        await session.ReceiveLineAsync("<Idle|Ov:90,50,110>");
        session.SetOverrideTargets(new OverrideState(100, 25, 100));

        Assert.True((await session.ApplyOverridesAsync(transport)).Succeeded);
        Assert.Equal([(byte)0x90, (byte)0x97, (byte)0x99], transport.Realtime);
    }

    [Fact]
    public async Task Continuous_jog_uses_abort_only_for_a_replacement_request()
    {
        var transport = new FakeTransport();
        var session = new MachineSession(FirmwareType.Grbl, new FakeClock(), new InlineDispatcher());
        session.SetGrblVersion(new GrblVersion(1, 1));
        session.RequestContinuousJog(JogDirection.East, 1000);
        Assert.True((await session.PumpContinuousJogAsync(transport)).Succeeded);
        Assert.True(await session.Streaming.PumpAsync(transport));
        session.Streaming.HandleResponse(true);
        session.RequestContinuousJog(JogDirection.North, 1000);

        Assert.True((await session.PumpContinuousJogAsync(transport)).Succeeded);
        Assert.Equal([(byte)0x85], transport.Realtime);
        Assert.True(await session.Streaming.PumpAsync(transport));
        Assert.Equal(["$J=G91X1.0F1000", "$J=G91Y1.0F1000"], transport.Commands);
    }

    private sealed class FakeClock : IMonotonicClock { public TimeSpan Elapsed => TimeSpan.Zero; }
    private sealed class InlineDispatcher : IUiDispatcher { public bool CheckAccess() => true; public Task InvokeAsync(Action action, CancellationToken cancellationToken = default) { action(); return Task.CompletedTask; } }
    private sealed class FakeTransport : ICommandTransport
    {
        public List<byte> Realtime { get; } = [];
        public List<string> Commands { get; } = [];
        public Task WriteAsync(string command, CancellationToken cancellationToken = default) { Commands.Add(command); return Task.CompletedTask; }
        public Task WriteRealtimeAsync(byte command, CancellationToken cancellationToken = default) { Realtime.Add(command); return Task.CompletedTask; }
    }
}
