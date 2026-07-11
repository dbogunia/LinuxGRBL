namespace LaserGRBL.Core.GCode;

public sealed record GCodeBounds(decimal MinX, decimal MinY, decimal MaxX, decimal MaxY)
{
    public static GCodeBounds? From(IEnumerable<GCodeLine> lines)
    {
        decimal? minX = null, minY = null, maxX = null, maxY = null;
        foreach (var line in lines)
        {
            if (line.Words.TryGetValue('X', out var x)) { minX = minX is null ? x : Math.Min(minX.Value, x); maxX = maxX is null ? x : Math.Max(maxX.Value, x); }
            if (line.Words.TryGetValue('Y', out var y)) { minY = minY is null ? y : Math.Min(minY.Value, y); maxY = maxY is null ? y : Math.Max(maxY.Value, y); }
        }

        return minX is null || minY is null || maxX is null || maxY is null ? null : new(minX.Value, minY.Value, maxX.Value, maxY.Value);
    }
}

public sealed class GCodeJob
{
    private readonly List<GCodeLine> lines = [];

    public const string DefaultHeader = "G90 (use absolute coordinates)";
    public const string DefaultPasses = ";(Uncomment if you want to sink Z axis)\r\n;G91 (use relative coordinates)\r\n;G0 Z-1 (sinks the Z axis, 1mm)\r\n;G90 (use absolute coordinates)";
    public const string DefaultFooter = "G0 X0 Y0 Z0 (move back to origin)";

    public IReadOnlyList<GCodeLine> Lines => lines;
    public GCodeBounds? Bounds => GCodeBounds.From(lines);

    public void Load(IEnumerable<string> sourceLines, bool append)
    {
        if (!append) lines.Clear();
        lines.AddRange(sourceLines.Where(line => !string.IsNullOrWhiteSpace(line)).Select(GCodeLine.Parse));
    }

    public IEnumerable<string> Render(bool includeHeader, bool includeFooter, bool includePasses, int cycles, string? header = null, string? passes = null, string? footer = null)
    {
        if (cycles < 1) throw new ArgumentOutOfRangeException(nameof(cycles));
        if (includeHeader) foreach (var line in SplitNonEmpty(header ?? DefaultHeader)) yield return line;
        for (var cycle = 0; cycle < cycles; cycle++)
        {
            foreach (var line in lines) yield return line.Text;
            if (includePasses && cycle < cycles - 1) foreach (var line in SplitNonEmpty(passes ?? DefaultPasses)) yield return line;
        }
        if (includeFooter) foreach (var line in SplitNonEmpty(footer ?? DefaultFooter)) yield return line;
    }

    private static IEnumerable<string> SplitNonEmpty(string text) => text.Split(["\r\n", "\n"], StringSplitOptions.None).Where(line => !string.IsNullOrWhiteSpace(line));
}
