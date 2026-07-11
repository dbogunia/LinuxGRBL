using System.Xml.Linq;
using LaserGRBL.Core.Svg;
using Xunit;

namespace LaserGRBL.Linux.Tests;

public sealed class SvgColorFilterTests
{
    [Theory]
    [InlineData("#ff0000", SvgColorFilter.Red, true)]
    [InlineData("rgb(0, 0, 255)", SvgColorFilter.Blue, true)]
    [InlineData("#000000", SvgColorFilter.Black, true)]
    [InlineData("#00ff00", SvgColorFilter.Red, false)]
    public void Applies_legacy_primary_color_filter_thresholds(string color, SvgColorFilter filter, bool expected) =>
        Assert.Equal(expected, SvgColorFilterParser.Includes(new XElement("path", new XAttribute("stroke", color)), filter));

    [Fact]
    public void Uses_style_before_presentation_attribute()
    {
        var element = new XElement("path", new XAttribute("stroke", "#ff0000"), new XAttribute("style", "fill:#0000ff"));
        Assert.True(SvgColorFilterParser.Includes(element, SvgColorFilter.Blue));
    }

    [Fact]
    public void Includes_path_when_color_is_missing_or_unparseable()
    {
        Assert.True(SvgColorFilterParser.Includes(new XElement("path"), SvgColorFilter.Red));
        Assert.True(SvgColorFilterParser.Includes(new XElement("path", new XAttribute("fill", "url(#gradient)")), SvgColorFilter.Red));
    }
}
