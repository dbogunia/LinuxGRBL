using LaserGRBL.Core.Svg;
using Xunit;

namespace LaserGRBL.Linux.Tests;

public sealed class BezierToolsTests
{
    [Fact]
    public void Linear_curve_keeps_its_control_points()
    {
        var points = new[] { new SvgPoint(0, 0), new SvgPoint(1, 1), new SvgPoint(2, 2), new SvgPoint(3, 3) };
        Assert.Equal(points, BezierTools.FlattenTo(points, 0.001).ToArray());
    }

    [Fact]
    public void Curved_segment_subdivides_with_smaller_error()
    {
        var points = new[] { new SvgPoint(0, 0), new SvgPoint(0, 1), new SvgPoint(1, 1), new SvgPoint(1, 0) };
        Assert.True(BezierTools.FlattenTo(points, 0.25).Count() > BezierTools.FlattenTo(points, 1).Count());
    }
}
