using LaserGRBL.Core.Protocol;
using Xunit;

namespace LaserGRBL.Linux.Tests;

public sealed class GrblStatusParserTests
{
    [Fact]
    public void Infers_grbl_11_when_pipe_report_has_no_pin_field() =>
        Assert.Equal(new GrblVersion(1, 1), GrblStatusParser.InferVersion("Idle|MPos:0,0,0|FS:0,0"));

    [Fact]
    public void Infers_grbl_10c_when_pipe_report_has_pin_field() =>
        Assert.Equal(new GrblVersion(1, 0, 'c'), GrblStatusParser.InferVersion("Idle|MPos:0,0,0|Pin:XYZ"));

    [Fact]
    public void Parses_grbl_11_status_and_fields()
    {
        var parsed = GrblStatusParser.TryParse("<Run|MPos:1.5,2,3|WCO:0.5,1,1.5|FS:1200,250|Bf:15,128|Ov:90,100,110>", new GrblVersion(1, 1), out var report);

        Assert.True(parsed);
        Assert.Equal(MachineStatus.Run, report?.Status);
        Assert.Equal(new MachinePosition(1.5, 2, 3), report?.MachinePosition);
        Assert.Equal(new MachinePosition(0.5, 1, 1.5), report?.WorkCoordinateOffset);
        Assert.Equal(1200, report?.FeedRate);
        Assert.Equal(250, report?.SpindleSpeed);
        Assert.Equal(15, report?.PlannerBuffer);
        Assert.Equal(128, report?.SerialBuffer);
        Assert.Equal(90, report?.FeedOverride);
    }

    [Fact]
    public void Parses_grbl_09_machine_and_work_positions()
    {
        var parsed = GrblStatusParser.TryParse("<Idle,MPos:10,20,30,WPos:1,2,3>", new GrblVersion(0, 9), out var report);

        Assert.True(parsed);
        Assert.Equal(MachineStatus.Idle, report?.Status);
        Assert.Equal(new MachinePosition(10, 20, 30), report?.MachinePosition);
        Assert.Equal(new MachinePosition(9, 18, 27), report?.WorkCoordinateOffset);
    }

    [Fact]
    public void Computes_machine_position_when_work_position_precedes_offset()
    {
        Assert.True(GrblStatusParser.TryParse("<Idle|WPos:1,2,3|WCO:10,20,30>", new GrblVersion(1, 1), out var report));

        Assert.Equal(new MachinePosition(11, 22, 33), report?.MachinePosition);
    }

    [Fact]
    public void Rejects_malformed_status_line() =>
        Assert.False(GrblStatusParser.TryParse("Idle|MPos:0,0,0", null, out _));

    [Theory]
    [InlineData("Grbl 1.1f ['$' for help]", false, 1, 1, 'f')]
    [InlineData("GrblHAL 1.1f ['$' for help]", true, 1, 1, 'f')]
    [InlineData("Grbl-Vigo:1.1f|Build:G-20200720", false, 1, 1, 'f')]
    [InlineData("SimpleLaser 1.0c ['$' for help]", false, 1, 0, 'c')]
    public void Parses_firmware_welcome_messages(string line, bool isHal, int major, int minor, char build)
    {
        Assert.True(FirmwareWelcomeParser.TryParse(line, null, null, out var version));
        Assert.Equal(new GrblVersion(major, minor, build, version?.VendorInfo, version?.VendorVersion, isHal), version);
        Assert.Equal(isHal, version?.IsHal);
    }

    [Fact]
    public void Parses_longer_machine_announcement() 
    {
        Assert.True(FirmwareWelcomeParser.TryParse("[Machine: Longer Nano]", null, "1.2", out var version));
        Assert.Equal("Longer Nano", version?.VendorInfo);
        Assert.True(version?.IsLonger);
    }
}
