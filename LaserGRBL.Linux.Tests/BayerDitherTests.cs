using LaserGRBL.Core.Raster;
using Xunit;

namespace LaserGRBL.Linux.Tests;

public sealed class BayerDitherTests
{
    [Fact]
    public void Dithers_black_and_white_extremes_deterministically()
    {
        var result = BayerDither.Apply(new byte[,] { { 0, 255 }, { 255, 0 } });

        Assert.True(result[0, 0]);
        Assert.False(result[0, 1]);
        Assert.False(result[1, 0]);
        Assert.True(result[1, 1]);
    }

    [Fact]
    public void Preserves_input_dimensions()
    {
        var result = BayerDither.Apply(new byte[3, 5]);

        Assert.Equal(3, result.GetLength(0));
        Assert.Equal(5, result.GetLength(1));
    }

    [Fact]
    public void Mid_gray_uses_ordered_threshold_pattern()
    {
        var result = BayerDither.Apply(new byte[,] { { 128, 128, 128, 128 } });

        Assert.Equal(new[] { false, true, false, true }, Enumerable.Range(0, 4).Select(index => result[0, index]));
    }
}
