using LaserGRBL.Core.Protocol;
using Xunit;

namespace LaserGRBL.Linux.Tests;

public sealed class GrblConfigurationParserTests
{
    [Fact]
    public void Parses_grbl_setting_and_optional_description()
    {
        Assert.True(GrblConfigurationParser.TryParse("$30=1000.000 (Max spindle speed, RPM)", out var setting));
        Assert.Equal(new GrblSetting(30, "1000.000", "(Max spindle speed, RPM)"), setting);
    }

    [Fact]
    public void Parses_configuration_snapshot_by_setting_number()
    {
        var settings = GrblConfigurationParser.ParseMany(["$32=1", "$30=1000", "ok", "error:3"]);

        Assert.Equal("1", settings[32].Value);
        Assert.Equal("1000", settings[30].Value);
        Assert.Equal(2, settings.Count);
    }

    [Theory]
    [InlineData("$=100")]
    [InlineData("$30")]
    [InlineData("[VER:1.1f]")]
    public void Rejects_non_settings(string line) => Assert.False(GrblConfigurationParser.TryParse(line, out _));
}
