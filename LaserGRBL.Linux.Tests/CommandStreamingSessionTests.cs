using LaserGRBL.Core.Abstractions;
using LaserGRBL.Core.Protocol;
using Xunit;

namespace LaserGRBL.Linux.Tests;

public sealed class CommandStreamingSessionTests
{
    [Fact]
    public async Task Streams_one_command_until_controller_acknowledges_it()
    {
        var clock = new FakeClock();
        var transport = new FakeTransport();
        var session = new CommandStreamingSession(clock, StreamingMode.Synchronous);
        session.Enqueue(new StreamedCommand("G1 X1", TimeSpan.FromSeconds(1)));
        session.Enqueue(new StreamedCommand("G1 X2", TimeSpan.FromSeconds(2)));

        Assert.True(await session.PumpAsync(transport));
        Assert.False(await session.PumpAsync(transport));
        Assert.Equal(["G1 X1"], transport.Writes);
        Assert.True(session.HandleResponse(true).Succeeded);
        Assert.True(await session.PumpAsync(transport));
        Assert.Equal(["G1 X1", "G1 X2"], transport.Writes);
    }

    [Fact]
    public async Task Uses_monotonic_clock_for_watchdog_and_duration()
    {
        var clock = new FakeClock();
        var session = new CommandStreamingSession(clock);
        session.Enqueue(new StreamedCommand("G1 X1", TimeSpan.FromSeconds(3)));
        await session.PumpAsync(new FakeTransport());

        clock.Elapsed = TimeSpan.FromSeconds(6);
        Assert.True(session.IsTimedOut(TimeSpan.FromSeconds(5)));
        Assert.True(session.HandleResponse(true).Succeeded);
        Assert.Equal(TimeSpan.FromSeconds(3), session.ExecutedDuration);
    }

    [Fact]
    public async Task Records_rejected_controller_responses()
    {
        var session = new CommandStreamingSession(new FakeClock());
        session.Enqueue(new StreamedCommand("M3"));
        await session.PumpAsync(new FakeTransport());

        var result = session.HandleResponse(false, "error:17");

        Assert.False(result.Succeeded);
        Assert.Equal("error:17", result.Error?.Detail);
        Assert.Equal(1, session.RetryCount);
    }

    [Fact]
    public async Task Repeat_on_error_requeues_only_the_final_rejected_command_up_to_three_times()
    {
        var transport = new FakeTransport();
        var session = new CommandStreamingSession(new FakeClock(), StreamingMode.RepeatOnError);
        session.Enqueue(new StreamedCommand("M3"));
        await session.PumpAsync(transport);

        Assert.False(session.HandleResponse(false, "error:1").Succeeded);
        Assert.True(await session.PumpAsync(transport));
        Assert.False(session.HandleResponse(false, "error:1").Succeeded);
        Assert.True(await session.PumpAsync(transport));
        Assert.False(session.HandleResponse(false, "error:1").Succeeded);
        Assert.True(await session.PumpAsync(transport));
        Assert.False(session.HandleResponse(false, "error:1").Succeeded);

        Assert.False(await session.PumpAsync(transport));
        Assert.Equal(["M3", "M3", "M3", "M3"], transport.Writes);
    }

    [Fact]
    public async Task Repeat_on_error_does_not_skip_queued_commands()
    {
        var transport = new FakeTransport();
        var session = new CommandStreamingSession(new FakeClock(), StreamingMode.RepeatOnError);
        session.Enqueue(new StreamedCommand("G1 X1"));
        session.Enqueue(new StreamedCommand("G1 X2"));
        await session.PumpAsync(transport);

        session.HandleResponse(false, "error:1");
        Assert.True(await session.PumpAsync(transport));

        Assert.Equal(["G1 X1", "G1 X2"], transport.Writes);
    }

    [Fact]
    public async Task Buffered_mode_sends_multiple_commands_until_the_byte_window_is_full()
    {
        var transport = new FakeTransport();
        var session = new CommandStreamingSession(new FakeClock(), StreamingMode.Buffered, bufferCapacity: 12);
        session.Enqueue(new StreamedCommand("G1 X1")); // 6 bytes with newline
        session.Enqueue(new StreamedCommand("G1 X2"));

        Assert.True(await session.PumpAsync(transport));
        Assert.True(await session.PumpAsync(transport));
        Assert.Equal(12, session.UsedBuffer);
        Assert.True(session.HandleResponse(true).Succeeded);
        Assert.Equal(6, session.UsedBuffer);
        Assert.Equal(["G1 X1", "G1 X2"], transport.Writes);
    }

    private sealed class FakeClock : IMonotonicClock { public TimeSpan Elapsed { get; set; } }

    private sealed class FakeTransport : ICommandTransport
    {
        public List<string> Writes { get; } = [];

        public Task WriteAsync(string command, CancellationToken cancellationToken = default)
        {
            Writes.Add(command);
            return Task.CompletedTask;
        }
    }
}
