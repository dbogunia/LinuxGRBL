using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;

namespace LaserGRBL.Avalonia.Preview;

public sealed class JobPreview3DControl : OpenGlControlBase
{
    private OpenGlImmediatePreviewRenderer? renderer;

    public static readonly StyledProperty<Preview3DSceneModel?> SceneProperty =
        AvaloniaProperty.Register<JobPreview3DControl, Preview3DSceneModel?>(nameof(Scene));

    public static readonly StyledProperty<PreviewCamera3D> CameraProperty =
        AvaloniaProperty.Register<JobPreview3DControl, PreviewCamera3D>(nameof(Camera), new PreviewCamera3D());

    public static readonly StyledProperty<OpenGlPreviewContextStatus> ContextStatusProperty =
        AvaloniaProperty.Register<JobPreview3DControl, OpenGlPreviewContextStatus>(nameof(ContextStatus), OpenGlPreviewContextStatus.Failure("OpenGL status unknown"));

    static JobPreview3DControl()
    {
        SceneProperty.Changed.AddClassHandler<JobPreview3DControl>((control, _) => control.RequestNextFrameRendering());
        CameraProperty.Changed.AddClassHandler<JobPreview3DControl>((control, _) => control.RequestNextFrameRendering());
        ContextStatusProperty.Changed.AddClassHandler<JobPreview3DControl>((control, _) => control.RequestNextFrameRendering());
    }

    public Preview3DSceneModel? Scene
    {
        get => GetValue(SceneProperty);
        set => SetValue(SceneProperty, value);
    }

    public PreviewCamera3D Camera
    {
        get => GetValue(CameraProperty);
        set => SetValue(CameraProperty, value);
    }

    public OpenGlPreviewContextStatus ContextStatus
    {
        get => GetValue(ContextStatusProperty);
        set => SetValue(ContextStatusProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);
        var scene = Scene;
        if (scene is null)
        {
            context.DrawRectangle(Brushes.WhiteSmoke, null, bounds);
            return;
        }

        context.DrawRectangle(scene.Style.Background, null, bounds);
        if (ContextStatus.Available) return;

        DrawDiagnostic(context, scene);

        if (scene.IsEmpty || scene.Bounds is null)
        {
            DrawText(context, scene.StatusText, new Point(12, 34), scene.Style.Text, 14);
            return;
        }

        var scale = FitScale(scene.Bounds, bounds);
        var center = new Point(bounds.Width / 2, bounds.Height / 2);
        DrawAxes(context, scene, center, scale);
        DrawLines(context, scene.RemainingLines, scene, center, scale, new Pen(scene.Style.Path, 1.2));
        DrawLines(context, scene.CompletedLines, scene, center, scale, new Pen(scene.Style.CompletedPath, 2.0));
        DrawMachine(context, scene, center, scale);
        DrawText(context, $"{scene.StatusText}  Progress {scene.Progress:P0}", new Point(12, bounds.Height - 28), scene.Style.Text, 12);
    }

    protected override void OnOpenGlInit(GlInterface gl)
    {
        try
        {
            renderer = OpenGlImmediatePreviewRenderer.FromAvalonia(gl);
            ContextStatus = OpenGlPreviewContextStatus.Success($"Avalonia OpenGL context initialized ({GlVersion}).");
            OpenGlPreviewDiagnostics.Record(ContextStatus.Diagnostic);
        }
        catch (Exception exception) when (exception is OpenGlPreviewRendererException or EntryPointNotFoundException or AccessViolationException)
        {
            renderer = null;
            ContextStatus = OpenGlPreviewContextStatus.Failure(exception.Message);
            OpenGlPreviewDiagnostics.Record(ContextStatus.Diagnostic);
        }
        base.OnOpenGlInit(gl);
    }

    protected override void OnOpenGlDeinit(GlInterface gl)
    {
        renderer = null;
        ContextStatus = OpenGlPreviewContextStatus.Failure("Avalonia OpenGL context was released.");
        base.OnOpenGlDeinit(gl);
    }

    protected override void OnOpenGlLost()
    {
        renderer = null;
        ContextStatus = OpenGlPreviewContextStatus.Failure("Avalonia OpenGL context was lost.");
        base.OnOpenGlLost();
    }

    protected override void OnOpenGlRender(GlInterface gl, int fb)
    {
        if (Scene is null || renderer is null) return;
        try
        {
            renderer.Render(Scene, Camera, Math.Max(1, (int)Bounds.Width), Math.Max(1, (int)Bounds.Height));
            ContextStatus = OpenGlPreviewContextStatus.Success($"Avalonia OpenGL rendered frame ({GlVersion}).");
            OpenGlPreviewDiagnostics.Record(ContextStatus.Diagnostic);
        }
        catch (Exception exception) when (exception is OpenGlPreviewRendererException or EntryPointNotFoundException or AccessViolationException)
        {
            ContextStatus = OpenGlPreviewContextStatus.Failure(exception.Message);
            OpenGlPreviewDiagnostics.Record(ContextStatus.Diagnostic);
        }
    }

    private void DrawDiagnostic(DrawingContext context, Preview3DSceneModel scene)
    {
        var text = $"3D/OpenGL fallback: {ContextStatus.Diagnostic}";
        DrawText(context, text, new Point(12, 10), scene.Style.Error, 12);
    }

    private void DrawAxes(DrawingContext context, Preview3DSceneModel scene, Point center, double scale)
    {
        var origin = ToPoint(Preview3DProjector.Project(new PreviewVertex3D(0, 0, 0), scene.Bounds, Camera), center, scale);
        var x = ToPoint(Preview3DProjector.Project(new PreviewVertex3D(scene.Bounds!.MaxX + 10, 0, 0), scene.Bounds, Camera), center, scale);
        var y = ToPoint(Preview3DProjector.Project(new PreviewVertex3D(0, scene.Bounds.MaxY + 10, 0), scene.Bounds, Camera), center, scale);
        var z = ToPoint(Preview3DProjector.Project(new PreviewVertex3D(0, 0, Math.Max(10, scene.Bounds.Depth + 10)), scene.Bounds, Camera), center, scale);
        context.DrawLine(new Pen(scene.Style.AxisX, 1), origin, x);
        context.DrawLine(new Pen(scene.Style.AxisY, 1), origin, y);
        context.DrawLine(new Pen(scene.Style.AxisZ, 1), origin, z);
    }

    private void DrawLines(DrawingContext context, IReadOnlyList<PreviewLine3D> lines, Preview3DSceneModel scene, Point center, double scale, Pen pen)
    {
        foreach (var line in lines)
        {
            var start = ToPoint(Preview3DProjector.Project(line.Start, scene.Bounds, Camera), center, scale);
            var end = ToPoint(Preview3DProjector.Project(line.End, scene.Bounds, Camera), center, scale);
            context.DrawLine(pen, start, end);
        }
    }

    private void DrawMachine(DrawingContext context, Preview3DSceneModel scene, Point center, double scale)
    {
        if (scene.MachinePosition is null) return;
        var point = ToPoint(Preview3DProjector.Project(scene.MachinePosition, scene.Bounds, Camera), center, scale);
        context.DrawEllipse(null, new Pen(scene.Style.Cursor, 1.7), point, 6, 6);
    }

    private static double FitScale(PreviewBounds3D bounds, Rect viewport)
    {
        var span = Math.Max(1, Math.Max(bounds.Width, bounds.Height));
        return Math.Max(1, Math.Min(viewport.Width, viewport.Height) * 0.74 / span);
    }

    private static Point ToPoint(PreviewPoint point, Point center, double scale) =>
        new(center.X + point.X * scale, center.Y + point.Y * scale);

    private static void DrawText(DrawingContext context, string value, Point origin, IBrush brush, double size)
    {
        var text = new FormattedText(value, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, size, brush);
        context.DrawText(text, origin);
    }
}

public static class OpenGlPreviewDiagnostics
{
    private const string DiagnosticsPathVariable = "LASERGRBL_OPENGL_DIAGNOSTICS_PATH";

    public static void Record(string message)
    {
        var path = Environment.GetEnvironmentVariable(DiagnosticsPathVariable);
        if (string.IsNullOrWhiteSpace(path)) return;

        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            File.AppendAllText(path, $"{DateTimeOffset.UtcNow:O} {message}{Environment.NewLine}");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Diagnostics must not affect preview rendering.
        }
    }
}
