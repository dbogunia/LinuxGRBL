using System.Globalization;
using System.Xml.Linq;

namespace LaserGRBL.Core.Svg;

public enum SvgColorFilter { All, Red, Green, Blue, Black }

public readonly record struct SvgRgb(byte Red, byte Green, byte Blue);

public static class SvgColorFilterParser
{
    private const byte Low = 20;
    private const byte High = 127;

    public static bool Includes(XElement element, SvgColorFilter filter)
    {
        if (filter == SvgColorFilter.All) return true;
        var colorText = GetColor(element);
        if (colorText is null || !TryParse(colorText, out var color)) return true;
        return filter switch
        {
            SvgColorFilter.Red => color.Red >= High && color.Green <= Low && color.Blue <= Low,
            SvgColorFilter.Green => color.Red <= Low && color.Green > High && color.Blue <= Low,
            SvgColorFilter.Blue => color.Red <= Low && color.Green <= Low && color.Blue > High,
            SvgColorFilter.Black => color.Red <= Low && color.Green <= Low && color.Blue <= Low,
            _ => true
        };
    }

    public static bool TryParse(string value, out SvgRgb color)
    {
        color = default;
        value = value.Trim();
        if (value.StartsWith('#'))
        {
            var hex = value[1..];
            if (hex.Length == 3) hex = string.Concat(hex.Select(character => new string(character, 2)));
            if (hex.Length == 6 && byte.TryParse(hex[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var red) && byte.TryParse(hex[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var green) && byte.TryParse(hex[4..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var blue))
            {
                color = new(red, green, blue);
                return true;
            }
        }

        if (value.StartsWith("rgb(", StringComparison.OrdinalIgnoreCase) && value.EndsWith(')'))
        {
            var values = value[4..^1].Split(',');
            if (values.Length == 3 && byte.TryParse(values[0].Trim(), CultureInfo.InvariantCulture, out var red) && byte.TryParse(values[1].Trim(), CultureInfo.InvariantCulture, out var green) && byte.TryParse(values[2].Trim(), CultureInfo.InvariantCulture, out var blue))
            {
                color = new(red, green, blue);
                return true;
            }
        }

        return Named.TryGetValue(value, out color);
    }

    private static string? GetColor(XElement element)
    {
        var style = element.Attribute("style")?.Value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => part.Split(':', 2, StringSplitOptions.TrimEntries))
            .Where(pair => pair.Length == 2 && pair[1] != "none")
            .ToDictionary(pair => pair[0], pair => pair[1], StringComparer.OrdinalIgnoreCase);
        if (style?.TryGetValue("stroke", out var stroke) == true || style?.TryGetValue("fill", out stroke) == true) return stroke;
        return element.Attribute("stroke")?.Value is { } attributeStroke and not "none" ? attributeStroke : element.Attribute("fill")?.Value is { } attributeFill and not "none" ? attributeFill : null;
    }

    private static readonly IReadOnlyDictionary<string, SvgRgb> Named = new Dictionary<string, SvgRgb>(StringComparer.OrdinalIgnoreCase)
    {
        ["black"] = new(0, 0, 0), ["red"] = new(255, 0, 0), ["green"] = new(0, 128, 0), ["blue"] = new(0, 0, 255), ["white"] = new(255, 255, 255)
    };
}
