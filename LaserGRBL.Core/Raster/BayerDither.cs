namespace LaserGRBL.Core.Raster;

/// <summary>Headless ordered dithering over 8-bit grayscale pixels; false means laser-off/white.</summary>
public static class BayerDither
{
    private static readonly int[,] Matrix4 = { { 0, 8, 2, 10 }, { 12, 4, 14, 6 }, { 3, 11, 1, 9 }, { 15, 7, 13, 5 } };

    public static bool[,] Apply(byte[,] grayscale)
    {
        var height = grayscale.GetLength(0);
        var width = grayscale.GetLength(1);
        var result = new bool[height, width];
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var threshold = (Matrix4[y % 4, x % 4] + 0.5) * 255 / 16;
            result[y, x] = grayscale[y, x] < threshold;
        }

        return result;
    }
}
