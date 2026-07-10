using LaserGRBL.Core.Protocol;
using Xunit;

namespace LaserGRBL.Linux.Tests;

public sealed class GrblOverrideCommandsTests
{
    [Theory]
    [InlineData(OverrideChannel.Feed, 90, 100, 0x90)]
    [InlineData(OverrideChannel.Feed, 100, 110, 0x91)]
    [InlineData(OverrideChannel.Feed, 110, 100, 0x90)]
    [InlineData(OverrideChannel.Feed, 100, 101, 0x93)]
    [InlineData(OverrideChannel.Spindle, 101, 100, 0x99)]
    [InlineData(OverrideChannel.Rapid, 50, 100, 0x95)]
    [InlineData(OverrideChannel.Rapid, 100, 25, 0x97)]
    public void Preserves_legacy_realtime_override_mapping(OverrideChannel channel, int current, int target, byte expected) =>
        Assert.Equal(expected, GrblOverrideCommands.NextCommand(channel, current, target));

    [Fact]
    public void Does_not_emit_command_when_target_is_already_current() =>
        Assert.Null(GrblOverrideCommands.NextCommand(OverrideChannel.Spindle, 100, 100));
}
