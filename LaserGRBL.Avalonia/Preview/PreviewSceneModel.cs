using Avalonia.Media;
using LaserGRBL.Avalonia.Services;
using LaserGRBL.Core.GCode;
using LaserGRBL.Core.Protocol;

namespace LaserGRBL.Avalonia.Preview;

public interface IJobPreviewRenderer
{
    PreviewSceneModel BuildScene(GCodeJob job, PreviewRenderStyle style, double progress = 0, MachinePosition? machinePosition = null);
}

public sealed record PreviewPoint(double X, double Y);

public sealed record PreviewLine(PreviewPoint Start, PreviewPoint End, int SourceIndex);

public sealed record PreviewBounds(double MinX, double MinY, double MaxX, double MaxY)
{
    public double Width => Math.Max(0, MaxX - MinX);
    public double Height => Math.Max(0, MaxY - MinY);
    public PreviewPoint Center => new((MinX + MaxX) / 2, (MinY + MaxY) / 2);
}

public sealed record PreviewRenderStyle(
    IBrush Background,
    IBrush Grid,
    IBrush Path,
    IBrush CompletedPath,
    IBrush Cursor,
    IBrush Text,
    IBrush Border)
{
    public static PreviewRenderStyle FromScheme(SemanticColorScheme scheme) =>
        new(scheme.PreviewBackground, scheme.Border, scheme.PreviewPath, scheme.Command, scheme.Warning, scheme.MutedText, scheme.Border);
}

public sealed record PreviewSceneModel(
    IReadOnlyList<PreviewLine> Lines,
    IReadOnlyList<PreviewLine> CompletedLines,
    IReadOnlyList<PreviewLine> RemainingLines,
    PreviewBounds? Bounds,
    PreviewPoint? MachinePosition,
    PreviewRenderStyle Style,
    double Progress,
    string StatusText)
{
    public bool IsEmpty => Lines.Count == 0;

    public static PreviewSceneModel Empty(PreviewRenderStyle style) =>
        new([], [], [], null, null, style, 0, "No file loaded");
}

public sealed record PreviewInteractionState(double Zoom = 1, double PanX = 0, double PanY = 0)
{
    public PreviewInteractionState ZoomBy(double factor)
    {
        if (factor <= 0) throw new ArgumentOutOfRangeException(nameof(factor));
        return this with { Zoom = Math.Clamp(Zoom * factor, 0.1, 20) };
    }

    public PreviewInteractionState PanBy(double dx, double dy) => this with { PanX = PanX + dx, PanY = PanY + dy };

    public PreviewInteractionState AutoFit() => new();
}

public sealed record PreviewViewportTransform(double Scale, double OffsetX, double OffsetY)
{
    public PreviewPoint ToScreen(PreviewPoint point) => new(point.X * Scale + OffsetX, OffsetY - point.Y * Scale);

    public PreviewPoint ToWorld(PreviewPoint point) => new((point.X - OffsetX) / Scale, (OffsetY - point.Y) / Scale);
}

public static class PreviewViewport
{
    public static PreviewViewportTransform Fit(PreviewBounds? bounds, double width, double height, PreviewInteractionState interaction, double padding = 24)
    {
        if (width <= 0 || height <= 0) return new(1, 0, 0);
        if (bounds is null || bounds.Width <= 0 || bounds.Height <= 0)
            return new(interaction.Zoom, width / 2 + interaction.PanX, height / 2 + interaction.PanY);

        var availableWidth = Math.Max(1, width - padding * 2);
        var availableHeight = Math.Max(1, height - padding * 2);
        var scale = Math.Min(availableWidth / bounds.Width, availableHeight / bounds.Height) * interaction.Zoom;
        var center = bounds.Center;
        var offsetX = width / 2 - center.X * scale + interaction.PanX;
        var offsetY = height / 2 + center.Y * scale + interaction.PanY;
        return new(scale, offsetX, offsetY);
    }
}

public sealed class GCodePreviewRenderer : IJobPreviewRenderer
{
    public PreviewSceneModel BuildScene(GCodeJob job, PreviewRenderStyle style, double progress = 0, MachinePosition? machinePosition = null)
    {
        var lines = BuildLines(job.Lines);
        if (lines.Count == 0) return PreviewSceneModel.Empty(style);

        var bounds = CalculateBounds(lines);
        progress = Math.Clamp(progress, 0, 1);
        var completedCount = (int)Math.Round(lines.Count * progress, MidpointRounding.AwayFromZero);
        completedCount = Math.Clamp(completedCount, 0, lines.Count);
        var machine = machinePosition is null ? null : new PreviewPoint(machinePosition.Value.X, machinePosition.Value.Y);
        return new PreviewSceneModel(lines, lines.Take(completedCount).ToArray(), lines.Skip(completedCount).ToArray(), bounds, machine, style, progress, $"{lines.Count} path segment(s)");
    }

    public static IReadOnlyList<PreviewLine> BuildLines(IReadOnlyList<GCodeLine> source)
    {
        var result = new List<PreviewLine>();
        var current = new PreviewPoint(0, 0);
        var absolute = true;

        for (var index = 0; index < source.Count; index++)
        {
            var line = source[index];
            if (line.Words.TryGetValue('G', out var g))
            {
                if (g == 90) absolute = true;
                if (g == 91) absolute = false;
            }

            var hasMoveCode = !line.Words.TryGetValue('G', out g) || g is 0 or 1 or 2 or 3;
            var hasXY = line.Words.ContainsKey('X') || line.Words.ContainsKey('Y');
            if (!hasMoveCode || !hasXY) continue;

            var x = line.Words.TryGetValue('X', out var nextX) ? (double)nextX : 0;
            var y = line.Words.TryGetValue('Y', out var nextY) ? (double)nextY : 0;
            var next = absolute
                ? new PreviewPoint(line.Words.ContainsKey('X') ? x : current.X, line.Words.ContainsKey('Y') ? y : current.Y)
                : new PreviewPoint(current.X + x, current.Y + y);

            if (next != current) result.Add(new PreviewLine(current, next, index));
            current = next;
        }

        return result;
    }

    public static PreviewBounds CalculateBounds(IReadOnlyList<PreviewLine> lines)
    {
        if (lines.Count == 0) return new PreviewBounds(0, 0, 0, 0);
        var points = lines.SelectMany(line => new[] { line.Start, line.End }).ToArray();
        return new PreviewBounds(points.Min(point => point.X), points.Min(point => point.Y), points.Max(point => point.X), points.Max(point => point.Y));
    }
}
