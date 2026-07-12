using LaserGRBL.Core.Raster;
using SkiaSharp;
using Xunit;

namespace LaserGRBL.Linux.Tests;

public sealed class ImageCompatibilityTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "linuxgrbl-image-" + Guid.NewGuid().ToString("N"));

    public ImageCompatibilityTests() => Directory.CreateDirectory(directory);

    public void Dispose()
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
    }

    [Theory]
    [InlineData("fixture.png", 3, 2)]
    [InlineData("fixture.jpg", 3, 2)]
    [InlineData("fixture.gif", 1, 1)]
    [InlineData("fixture.bmp", 3, 2)]
    public void Skia_decoder_loads_representative_legacy_raster_formats(string fileName, int expectedWidth, int expectedHeight)
    {
        var path = WriteFixture(fileName);

        var decoded = new SkiaImageDecoder().Decode(path);

        Assert.True(decoded.Succeeded, decoded.Error?.Message);
        Assert.Equal(expectedWidth, decoded.Value?.Width);
        Assert.Equal(expectedHeight, decoded.Value?.Height);
    }

    [Fact]
    public void Raster_conversion_preserves_alpha_by_compositing_on_white()
    {
        var pixel = new RgbaPixel(0, 0, 0, 128);

        Assert.InRange(pixel.ToGrayscaleOnWhite(), 126, 128);
    }

    [Fact]
    public void Raster_conversion_is_deterministic_for_golden_dither_pattern()
    {
        var path = Path.Combine(directory, "golden.png");
        using var bitmap = new SKBitmap(4, 1, true);
        for (var x = 0; x < 4; x++) bitmap.SetPixel(x, 0, new SKColor(128, 128, 128));
        WriteBitmap(bitmap, path, SKEncodedImageFormat.Png);

        var result = new RasterImageConverter(new SkiaImageDecoder()).Convert(path, dither: true);

        Assert.True(result.Succeeded, result.Error?.Message);
        Assert.Equal(new[] { false, true, false, true }, Enumerable.Range(0, 4).Select(x => result.Value!.Dithered![0, x]));
    }

    [Fact]
    public void Decoder_handles_non_ascii_paths()
    {
        var path = WriteFixture("zażółć.png");

        var decoded = new SkiaImageDecoder().Decode(path);

        Assert.True(decoded.Succeeded, decoded.Error?.Message);
    }

    [Fact]
    public void Decoder_reports_corrupt_and_unsupported_files_without_throwing()
    {
        var corrupt = Path.Combine(directory, "corrupt.png");
        var unsupported = Path.Combine(directory, "image.tiff");
        File.WriteAllBytes(corrupt, [0, 1, 2, 3]);
        File.WriteAllBytes(unsupported, [0, 1, 2, 3]);

        var corruptResult = new SkiaImageDecoder().Decode(corrupt);
        var unsupportedResult = new SkiaImageDecoder().Decode(unsupported);

        Assert.False(corruptResult.Succeeded);
        Assert.Contains("codec", corruptResult.Error?.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(unsupportedResult.Succeeded);
        Assert.Contains("Unsupported", unsupportedResult.Error?.Message);
    }

    [Fact]
    public void Font_fallback_is_deterministic_when_legacy_font_is_absent()
    {
        var resolver = new FontFallbackResolver(["Liberation Sans", "DejaVu Sans"]);

        Assert.Equal("DejaVu Sans", resolver.Resolve("Microsoft Sans Serif"));
        Assert.Equal("Liberation Sans", new FontFallbackResolver(["Liberation Sans"]).Resolve("Missing"));
        Assert.Equal("sans-serif", new FontFallbackResolver([]).Resolve("Missing"));
    }

    private string WriteFixture(string fileName)
    {
        var path = Path.Combine(directory, fileName);
        if (Path.GetExtension(fileName).Equals(".gif", StringComparison.OrdinalIgnoreCase))
        {
            WriteOnePixelGif(path);
            return path;
        }

        if (Path.GetExtension(fileName).Equals(".bmp", StringComparison.OrdinalIgnoreCase))
        {
            WriteBmpFixture(path);
            return path;
        }

        using var bitmap = new SKBitmap(3, 2, true);
        bitmap.SetPixel(0, 0, SKColors.Red);
        bitmap.SetPixel(1, 0, SKColors.Green);
        bitmap.SetPixel(2, 0, SKColors.Blue);
        bitmap.SetPixel(0, 1, SKColors.Black);
        bitmap.SetPixel(1, 1, SKColors.White);
        bitmap.SetPixel(2, 1, new SKColor(64, 64, 64));
        var format = Path.GetExtension(fileName).Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            ? SKEncodedImageFormat.Jpeg
            : SKEncodedImageFormat.Png;
        WriteBitmap(bitmap, path, format);
        return path;
    }

    private static void WriteOnePixelGif(string path)
    {
        File.WriteAllBytes(path, Convert.FromBase64String("R0lGODlhAQABAPAAAP8AAAAAACH5BAAAAAAALAAAAAABAAEAAAICRAEAOw=="));
    }

    private static void WriteBmpFixture(string path)
    {
        const int width = 3;
        const int height = 2;
        const int rowSize = 12;
        const int pixelDataSize = rowSize * height;
        const int fileSize = 54 + pixelDataSize;
        var bytes = new byte[fileSize];

        bytes[0] = (byte)'B';
        bytes[1] = (byte)'M';
        BitConverter.GetBytes(fileSize).CopyTo(bytes, 2);
        BitConverter.GetBytes(54).CopyTo(bytes, 10);
        BitConverter.GetBytes(40).CopyTo(bytes, 14);
        BitConverter.GetBytes(width).CopyTo(bytes, 18);
        BitConverter.GetBytes(height).CopyTo(bytes, 22);
        BitConverter.GetBytes((short)1).CopyTo(bytes, 26);
        BitConverter.GetBytes((short)24).CopyTo(bytes, 28);
        BitConverter.GetBytes(pixelDataSize).CopyTo(bytes, 34);

        WriteBmpPixel(bytes, rowSize, width, 0, 0, SKColors.Red);
        WriteBmpPixel(bytes, rowSize, width, 1, 0, SKColors.Green);
        WriteBmpPixel(bytes, rowSize, width, 2, 0, SKColors.Blue);
        WriteBmpPixel(bytes, rowSize, width, 0, 1, SKColors.Black);
        WriteBmpPixel(bytes, rowSize, width, 1, 1, SKColors.White);
        WriteBmpPixel(bytes, rowSize, width, 2, 1, new SKColor(64, 64, 64));

        File.WriteAllBytes(path, bytes);
    }

    private static void WriteBmpPixel(byte[] bytes, int rowSize, int width, int x, int yFromTop, SKColor color)
    {
        var bottomUpY = 1 - yFromTop;
        var offset = 54 + bottomUpY * rowSize + x * 3;
        bytes[offset] = color.Blue;
        bytes[offset + 1] = color.Green;
        bytes[offset + 2] = color.Red;
    }

    private static void WriteBitmap(SKBitmap bitmap, string path, SKEncodedImageFormat format)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(format, 100);
        Assert.NotNull(data);
        using var stream = File.Create(path);
        data.SaveTo(stream);
    }
}
