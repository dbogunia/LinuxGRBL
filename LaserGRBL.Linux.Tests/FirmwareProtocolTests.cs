using LaserGRBL.Core.Protocol;
using Xunit;

namespace LaserGRBL.Linux.Tests;

public sealed class FirmwareProtocolTests
{
    [Theory]
    [InlineData(FirmwareType.Smoothie, true)]
    [InlineData(FirmwareType.Marlin, true)]
    [InlineData(FirmwareType.Grbl, false)]
    public void Preserves_streaming_strategy(FirmwareType firmware, bool synchronous) =>
        Assert.Equal(synchronous, FirmwareProtocols.For(firmware).UsesSynchronousStreaming);

    [Fact]
    public void Only_grbl_11_supports_true_jogging()
    {
        Assert.True(FirmwareProtocols.Grbl.SupportsTrueJogging(new GrblVersion(1, 1)));
        Assert.False(FirmwareProtocols.Grbl.SupportsTrueJogging(new GrblVersion(1, 0, 'c')));
        Assert.False(FirmwareProtocols.Marlin.SupportsTrueJogging(new GrblVersion(1, 1)));
        Assert.False(FirmwareProtocols.Smoothie.SupportsTrueJogging(new GrblVersion(1, 1)));
    }

    [Fact]
    public void Parses_marlin_position_with_in_program_status()
    {
        var parsed = FirmwareStatusParsers.TryParseMarlin("X:10.00 Y:20.00 Z:3.50 E:0.00", true, out var report);

        Assert.True(parsed);
        Assert.Equal(MachineStatus.Run, report?.Status);
        Assert.Equal(new MachinePosition(10, 20, 3.5), report?.Position);
    }

    [Fact]
    public void Parses_vigo_buffer_counters()
    {
        var parsed = FirmwareStatusParsers.TryParseVigo("<VSta:2|SBuf:10,8,1>", out var report);

        Assert.True(parsed);
        Assert.Equal(new VigoStatusReport(2, 10, 8, 1), report);
    }

    [Fact]
    public void Generates_grbl_11_jog_without_legacy_mode_switches() =>
        Assert.Equal(["$J=G91X5.0Y5.0F1200"], JogCommandFactory.CreateRelative(JogDirection.NorthEast, 5, 1200, true));

    [Fact]
    public void Generates_grbl_09_jog_with_legacy_mode_switches() =>
        Assert.Equal(["G91", "G1X-2.0F600", "G90"], JogCommandFactory.CreateRelative(JogDirection.West, 2, 600, false));
}
