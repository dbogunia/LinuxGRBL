using System.Globalization;

namespace LaserGRBL.Core.Protocol;

public static class GrblStatusParser
{
    public static GrblVersion InferVersion(string line, GrblVersion? knownVersion = null)
    {
        if (knownVersion is not null) return knownVersion;
        if (!line.Contains('|')) return new GrblVersion(0, 9);
        return line.Contains("Pin:", StringComparison.Ordinal) ? new GrblVersion(1, 0, 'c') : new GrblVersion(1, 1);
    }

    public static bool TryParse(string line, GrblVersion? knownVersion, out GrblStatusReport? report)
    {
        report = null;
        if (string.IsNullOrWhiteSpace(line) || line[0] != '<' || line[^1] != '>') return false;

        var body = line[1..^1];
        return InferVersion(body, knownVersion).CompareTo(new GrblVersion(1, 1)) >= 0
            ? TryParseV11(body, out report)
            : TryParseV09(body, out report);
    }

    private static bool TryParseV11(string body, out GrblStatusReport? report)
    {
        report = null;
        var parts = body.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0 || !TryParseStatus(parts[0], out var status)) return false;

        MachinePosition? machinePosition = null;
        MachinePosition? workPosition = null;
        MachinePosition? wco = null;
        double? feed = null;
        double? spindle = null;
        int? planner = null;
        int? serial = null;
        int? feedOverride = null;
        int? rapidOverride = null;
        int? spindleOverride = null;

        foreach (var part in parts.Skip(1))
        {
            if (part.StartsWith("MPos:", StringComparison.Ordinal) && TryParsePosition(part[5..], out var position)) machinePosition = position;
            else if (part.StartsWith("WCO:", StringComparison.Ordinal) && TryParsePosition(part[4..], out var offset)) wco = offset;
            else if (part.StartsWith("WPos:", StringComparison.Ordinal) && TryParsePosition(part[5..], out var parsedWorkPosition)) workPosition = parsedWorkPosition;
            else if (part.StartsWith("FS:", StringComparison.Ordinal) && TryParseNumbers(part[3..], 2, out var fs)) { feed = fs[0]; spindle = fs[1]; }
            else if (part.StartsWith("F:", StringComparison.Ordinal) && TryParseNumbers(part[2..], 1, out var f)) feed = f[0];
            else if (part.StartsWith("Bf:", StringComparison.Ordinal) && TryParseIntegers(part[3..], 2, out var bf)) { planner = bf[0]; serial = bf[1]; }
            else if (part.StartsWith("Ov:", StringComparison.Ordinal) && TryParseIntegers(part[3..], 3, out var ov)) { feedOverride = ov[0]; rapidOverride = ov[1]; spindleOverride = ov[2]; }
        }

        if (machinePosition is null && workPosition is not null && wco is not null) machinePosition = workPosition + wco.Value;

        report = new(status, machinePosition, wco, feed, spindle, planner, serial, feedOverride, rapidOverride, spindleOverride);
        return true;
    }

    private static bool TryParseV09(string body, out GrblStatusReport? report)
    {
        report = null;
        var parts = body.Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length == 0 || !TryParseStatus(parts[0], out var status)) return false;
        if (parts.Length < 7 || !parts[1].StartsWith("MPos:", StringComparison.Ordinal) || !TryParsePosition(string.Join(',', parts.Skip(1).Take(3)).Replace("MPos:", string.Empty, StringComparison.Ordinal), out var machine) || !parts[4].StartsWith("WPos:", StringComparison.Ordinal) || !TryParsePosition(string.Join(',', parts.Skip(4).Take(3)).Replace("WPos:", string.Empty, StringComparison.Ordinal), out var work))
        {
            report = new(status);
            return true;
        }

        report = new(status, machine, machine - work);
        return true;
    }

    private static bool TryParseStatus(string value, out MachineStatus status) =>
        Enum.TryParse(value.Split(':', 2)[0], ignoreCase: true, out status);

    private static bool TryParsePosition(string value, out MachinePosition position)
    {
        position = default;
        if (!TryParseNumbers(value, 3, out var values)) return false;
        position = new(values[0], values[1], values[2]);
        return true;
    }

    private static bool TryParseNumbers(string value, int count, out double[] values)
    {
        var parts = value.Split(',');
        values = new double[parts.Length];
        if (parts.Length < count) return false;
        for (var index = 0; index < parts.Length; index++)
        {
            if (!double.TryParse(parts[index], NumberStyles.Float, CultureInfo.InvariantCulture, out values[index])) return false;
        }

        return true;
    }

    private static bool TryParseIntegers(string value, int count, out int[] values)
    {
        var parts = value.Split(',');
        values = new int[parts.Length];
        if (parts.Length < count) return false;
        for (var index = 0; index < parts.Length; index++)
        {
            if (!int.TryParse(parts[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out values[index])) return false;
        }

        return true;
    }
}
