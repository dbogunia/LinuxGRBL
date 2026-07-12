using Avalonia.Media;
using LaserGRBL.Core.Protocol;

namespace LaserGRBL.Avalonia.Preview;

public sealed record PreviewVertex3D(double X, double Y, double Z);

public sealed record PreviewLine3D(PreviewVertex3D Start, PreviewVertex3D End, int SourceIndex);

public sealed record PreviewBounds3D(double MinX, double MinY, double MinZ, double MaxX, double MaxY, double MaxZ)
{
    public double Width => Math.Max(0, MaxX - MinX);
    public double Height => Math.Max(0, MaxY - MinY);
    public double Depth => Math.Max(0, MaxZ - MinZ);
    public PreviewVertex3D Center => new((MinX + MaxX) / 2, (MinY + MaxY) / 2, (MinZ + MaxZ) / 2);
}

public sealed record PreviewCamera3D(double RotationX = 55, double RotationY = 0, double RotationZ = 35, double Zoom = 1, double PanX = 0, double PanY = 0)
{
    public PreviewCamera3D RotateBy(double x, double y, double z = 0) => this with { RotationX = RotationX + x, RotationY = RotationY + y, RotationZ = RotationZ + z };
    public PreviewCamera3D PanBy(double x, double y) => this with { PanX = PanX + x, PanY = PanY + y };
    public PreviewCamera3D ZoomBy(double factor)
    {
        if (factor <= 0) throw new ArgumentOutOfRangeException(nameof(factor));
        return this with { Zoom = Math.Clamp(Zoom * factor, 0.1, 30) };
    }

    public PreviewCamera3D Reset() => new();
}

public sealed record Preview3DStyle(IBrush Background, IBrush Grid, IBrush AxisX, IBrush AxisY, IBrush AxisZ, IBrush Path, IBrush CompletedPath, IBrush Cursor, IBrush Text, IBrush Error)
{
    public static Preview3DStyle From2D(PreviewRenderStyle style) =>
        new(style.Background, style.Grid, Brushes.IndianRed, Brushes.SeaGreen, Brushes.SteelBlue, style.Path, style.CompletedPath, style.Cursor, style.Text, style.Cursor);
}

public sealed record Preview3DSceneModel(
    IReadOnlyList<PreviewLine3D> Lines,
    IReadOnlyList<PreviewLine3D> CompletedLines,
    IReadOnlyList<PreviewLine3D> RemainingLines,
    PreviewBounds3D? Bounds,
    PreviewVertex3D? MachinePosition,
    Preview3DStyle Style,
    double Progress,
    string StatusText)
{
    public bool IsEmpty => Lines.Count == 0;

    public static Preview3DSceneModel Empty(Preview3DStyle style) =>
        new([], [], [], null, null, style, 0, "No file loaded");
}

public interface IPreview3DRenderer
{
    Preview3DSceneModel BuildScene(PreviewSceneModel scene);
}

public sealed class Preview3DSceneBuilder : IPreview3DRenderer
{
    public Preview3DSceneModel BuildScene(PreviewSceneModel scene)
    {
        var style = Preview3DStyle.From2D(scene.Style);
        if (scene.IsEmpty) return Preview3DSceneModel.Empty(style);

        var lines = scene.Lines.Select(To3D).ToArray();
        var completed = scene.CompletedLines.Select(To3D).ToArray();
        var remaining = scene.RemainingLines.Select(To3D).ToArray();
        var bounds = CalculateBounds(lines);
        var machine = scene.MachinePosition is null ? null : new PreviewVertex3D(scene.MachinePosition.X, scene.MachinePosition.Y, 0);
        return new Preview3DSceneModel(lines, completed, remaining, bounds, machine, style, scene.Progress, scene.StatusText);
    }

    public static PreviewBounds3D CalculateBounds(IReadOnlyList<PreviewLine3D> lines)
    {
        if (lines.Count == 0) return new PreviewBounds3D(0, 0, 0, 0, 0, 0);
        var points = lines.SelectMany(line => new[] { line.Start, line.End }).ToArray();
        return new PreviewBounds3D(points.Min(p => p.X), points.Min(p => p.Y), points.Min(p => p.Z), points.Max(p => p.X), points.Max(p => p.Y), points.Max(p => p.Z));
    }

    private static PreviewLine3D To3D(PreviewLine line) =>
        new(new PreviewVertex3D(line.Start.X, line.Start.Y, 0), new PreviewVertex3D(line.End.X, line.End.Y, 0), line.SourceIndex);
}

public static class Preview3DProjector
{
    public static PreviewPoint Project(PreviewVertex3D point, PreviewBounds3D? bounds, PreviewCamera3D camera)
    {
        var center = bounds?.Center ?? new PreviewVertex3D(0, 0, 0);
        var x = point.X - center.X;
        var y = point.Y - center.Y;
        var z = point.Z - center.Z;

        (y, z) = Rotate(y, z, Degrees(camera.RotationX));
        (x, z) = Rotate(x, z, Degrees(camera.RotationY));
        (x, y) = Rotate(x, y, Degrees(camera.RotationZ));

        return new PreviewPoint(x * camera.Zoom + camera.PanX, -y * camera.Zoom + camera.PanY);
    }

    private static (double A, double B) Rotate(double a, double b, double radians)
    {
        var sin = Math.Sin(radians);
        var cos = Math.Cos(radians);
        return (a * cos - b * sin, a * sin + b * cos);
    }

    private static double Degrees(double value) => value * Math.PI / 180;
}

public interface IOpenGlPreviewContextFactory
{
    OpenGlPreviewContextStatus Probe();
}

public sealed record OpenGlPreviewContextStatus(bool Available, string Diagnostic)
{
    public static OpenGlPreviewContextStatus Success(string diagnostic = "OpenGL context available") => new(true, diagnostic);
    public static OpenGlPreviewContextStatus Failure(string diagnostic) => new(false, diagnostic);
}

public sealed class AvaloniaOpenGlPreviewContextFactory : IOpenGlPreviewContextFactory
{
    public OpenGlPreviewContextStatus Probe() =>
        OpenGlPreviewContextStatus.Failure("Avalonia OpenGL runtime host is not active in this headless build; using 2D fallback while keeping 3D scene state available.");
}
