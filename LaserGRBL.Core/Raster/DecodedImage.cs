using LaserGRBL.Core.Abstractions;
using SkiaSharp;

namespace LaserGRBL.Core.Raster;

public sealed record DecodedImage(int Width, int Height, RgbaPixel[] Pixels)
{
    public RgbaPixel PixelAt(int x, int y) => Pixels[y * Width + x];
}

public readonly record struct RgbaPixel(byte Red, byte Green, byte Blue, byte Alpha)
{
    public byte ToGrayscaleOnWhite()
    {
        var alpha = Alpha / 255.0;
        var red = Red * alpha + 255 * (1 - alpha);
        var green = Green * alpha + 255 * (1 - alpha);
        var blue = Blue * alpha + 255 * (1 - alpha);
        return (byte)Math.Clamp(Math.Round(red * 0.299 + green * 0.587 + blue * 0.114), 0, 255);
    }
}

public interface IImageDecoder
{
    OperationResult<DecodedImage> Decode(string path);
}

public sealed class SkiaImageDecoder : IImageDecoder
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".bmp", ".png", ".jpg", ".jpeg", ".gif"
    };

    public OperationResult<DecodedImage> Decode(string path)
    {
        if (!SupportedExtensions.Contains(Path.GetExtension(path)))
            return OperationResult<DecodedImage>.Failure("Unsupported image format.", Path.GetExtension(path));
        if (!File.Exists(path)) return OperationResult<DecodedImage>.Failure("Image file was not found.", path);

        try
        {
            using var bitmap = SKBitmap.Decode(path);
            if (bitmap is null) return OperationResult<DecodedImage>.Failure("Image codec could not decode the file.", path);
            var pixels = new RgbaPixel[bitmap.Width * bitmap.Height];
            for (var y = 0; y < bitmap.Height; y++)
            for (var x = 0; x < bitmap.Width; x++)
            {
                var color = bitmap.GetPixel(x, y);
                pixels[y * bitmap.Width + x] = new RgbaPixel(color.Red, color.Green, color.Blue, color.Alpha);
            }

            return OperationResult<DecodedImage>.Success(new DecodedImage(bitmap.Width, bitmap.Height, pixels));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            return OperationResult<DecodedImage>.Failure("Unable to decode image file.", path, exception);
        }
    }
}

public sealed record RasterConversionFixture(int Width, int Height, byte[,] Grayscale, bool[,]? Dithered);

public sealed class RasterImageConverter(IImageDecoder decoder)
{
    public OperationResult<RasterConversionFixture> Convert(string path, bool dither)
    {
        var decoded = decoder.Decode(path);
        if (!decoded.Succeeded || decoded.Value is null)
            return OperationResult<RasterConversionFixture>.Failure(decoded.Error?.Message ?? "Image decode failed.", decoded.Error?.Detail, decoded.Error?.Exception);

        var image = decoded.Value;
        var grayscale = new byte[image.Height, image.Width];
        for (var y = 0; y < image.Height; y++)
        for (var x = 0; x < image.Width; x++)
            grayscale[y, x] = image.PixelAt(x, y).ToGrayscaleOnWhite();

        return OperationResult<RasterConversionFixture>.Success(new RasterConversionFixture(image.Width, image.Height, grayscale, dither ? BayerDither.Apply(grayscale) : null));
    }
}

public sealed class FontFallbackResolver(IReadOnlyList<string> installedFamilies)
{
    public string Resolve(string requestedFamily)
    {
        if (installedFamilies.FirstOrDefault(family => family.Equals(requestedFamily, StringComparison.OrdinalIgnoreCase)) is { } exact)
            return exact;
        return installedFamilies.FirstOrDefault(family => family.Equals("DejaVu Sans", StringComparison.OrdinalIgnoreCase))
            ?? installedFamilies.FirstOrDefault()
            ?? "sans-serif";
    }
}
