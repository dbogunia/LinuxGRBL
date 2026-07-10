using System.Globalization;

namespace LaserGRBL.Core.Protocol;

public sealed record GrblSetting(int Number, string Value, string? Description = null);

public static class GrblConfigurationParser
{
    public static bool TryParse(string line, out GrblSetting? setting)
    {
        setting = null;
        if (string.IsNullOrWhiteSpace(line) || !line.StartsWith('$')) return false;
        var separator = line.IndexOf('=');
        if (separator <= 1) return false;
        if (!int.TryParse(line.AsSpan(1, separator - 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)) return false;

        var valueAndDescription = line[(separator + 1)..];
        var descriptionStart = valueAndDescription.IndexOf('(');
        var value = (descriptionStart < 0 ? valueAndDescription : valueAndDescription[..descriptionStart]).Trim();
        if (value.Length == 0) return false;
        var description = descriptionStart < 0 ? null : valueAndDescription[descriptionStart..].Trim();
        setting = new(number, value, description);
        return true;
    }

    public static IReadOnlyDictionary<int, GrblSetting> ParseMany(IEnumerable<string> lines) =>
        lines.Select(line => TryParse(line, out var setting) ? setting : null)
            .Where(setting => setting is not null)
            .Cast<GrblSetting>()
            .ToDictionary(setting => setting.Number);
}
