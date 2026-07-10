using System.Globalization;

namespace LaserGRBL.Core.Protocol;

public static class FirmwareStatusParsers
{
    public static bool TryParseMarlin(string line, bool inProgram, out MarlinStatusReport? report)
    {
        report = null;
        var fields = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (fields.Length < 3 || !TryCoordinate(fields[0], 'X', out var x) || !TryCoordinate(fields[1], 'Y', out var y) || !TryCoordinate(fields[2], 'Z', out var z)) return false;
        report = new(inProgram ? MachineStatus.Run : MachineStatus.Idle, new MachinePosition(x, y, z));
        return true;
    }

    public static bool TryParseVigo(string line, out VigoStatusReport? report)
    {
        report = null;
        if (!line.StartsWith("<VSta", StringComparison.Ordinal) || !line.EndsWith('>')) return false;
        int? state = null;
        int? received = null;
        int? managed = null;
        int? errors = null;
        foreach (var field in line[1..^1].Split('|'))
        {
            var pair = field.Split(':', 2);
            if (pair.Length != 2) continue;
            if (field.StartsWith("VSta:", StringComparison.Ordinal) && int.TryParse(pair[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedState)) state = parsedState;
            if (field.StartsWith("SBuf:", StringComparison.Ordinal))
            {
                var values = pair[1].Split(',');
                if (values.Length == 3 && int.TryParse(values[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedReceived) && int.TryParse(values[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedManaged) && int.TryParse(values[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedErrors))
                {
                    received = parsedReceived;
                    managed = parsedManaged;
                    errors = parsedErrors;
                }
            }
        }

        if (state is null || received is null || managed is null || errors is null) return false;
        report = new(state.Value, received.Value, managed.Value, errors.Value);
        return true;
    }

    private static bool TryCoordinate(string field, char coordinate, out double value)
    {
        value = default;
        return field.StartsWith($"{coordinate}:", StringComparison.Ordinal) &&
            double.TryParse(field[2..], NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }
}
