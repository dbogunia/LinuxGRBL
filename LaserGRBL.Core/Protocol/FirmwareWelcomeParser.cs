using System.Text.RegularExpressions;

namespace LaserGRBL.Core.Protocol;

public static partial class FirmwareWelcomeParser
{
    public static bool TryParse(string line, string? vendorInfo, string? vendorVersion, out GrblVersion? version)
    {
        version = null;
        if (string.IsNullOrWhiteSpace(line)) return false;

        if (line.StartsWith("Ortur ", StringComparison.Ordinal)) return false;
        if (line.StartsWith("OLF", StringComparison.Ordinal)) return false;

        if (line.StartsWith("[Machine:", StringComparison.Ordinal) && line.EndsWith(']'))
        {
            version = new(1, 1, 'f', line[9..^1].Trim(), vendorVersion, false);
            return true;
        }

        if (line.StartsWith("[Software:", StringComparison.Ordinal) && line.EndsWith(']'))
        {
            version = new(1, 1, 'f', vendorInfo, line[10..^1].Trim(), false);
            return true;
        }

        var match = WelcomePattern().Match(line);
        if (!match.Success) return false;
        var major = int.Parse(match.Groups["major"].Value, System.Globalization.CultureInfo.InvariantCulture);
        var minor = int.Parse(match.Groups["minor"].Value, System.Globalization.CultureInfo.InvariantCulture);
        var build = match.Groups["build"].Value[0];
        var isHal = line.StartsWith("GrblHAL ", StringComparison.Ordinal);
        var isVigo = line.StartsWith("Grbl-Vigo:", StringComparison.Ordinal);
        var resolvedVendor = isVigo ? "Grbl-Vigo" : line.StartsWith("SimpleLaser ", StringComparison.Ordinal) ? "SimpleLaser" : vendorInfo;
        var resolvedVersion = isVigo && line.Contains("Build:", StringComparison.Ordinal) ? line[(line.IndexOf("Build:", StringComparison.Ordinal) + 6)..] : vendorVersion;
        version = new(major, minor, build, resolvedVendor, resolvedVersion, isHal);
        return true;
    }

    [GeneratedRegex("(?:GrblHAL |Grbl-Vigo:|Grbl |SimpleLaser |.+?\\s)(?<major>\\d)\\.(?<minor>\\d)(?<build>[A-Za-z#])", RegexOptions.CultureInvariant)]
    private static partial Regex WelcomePattern();
}
