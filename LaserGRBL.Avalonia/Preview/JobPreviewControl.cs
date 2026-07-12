using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace LaserGRBL.Avalonia.Preview;

public sealed class JobPreviewControl : Control
{
    public static readonly StyledProperty<PreviewSceneModel?> SceneProperty =
        AvaloniaProperty.Register<JobPreviewControl, PreviewSceneModel?>(nameof(Scene));

    public static readonly StyledProperty<PreviewInteractionState> InteractionProperty =
        AvaloniaProperty.Register<JobPreviewControl, PreviewInteractionState>(nameof(Interaction), new PreviewInteractionState());

    static JobPreviewControl()
    {
        SceneProperty.Changed.AddClassHandler<JobPreviewControl>((control, _) => control.InvalidateVisual());
        InteractionProperty.Changed.AddClassHandler<JobPreviewControl>((control, _) => control.InvalidateVisual());
    }

    public PreviewSceneModel? Scene
    {
        get => GetValue(SceneProperty);
        set => SetValue(SceneProperty, value);
    }

    public PreviewInteractionState Interaction
    {
        get => GetValue(InteractionProperty);
        set => SetValue(InteractionProperty, value);
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

        context.DrawRectangle(scene.Style.Background, new Pen(scene.Style.Border, 1), bounds);
        DrawGrid(context, scene, bounds);

        if (scene.IsEmpty || scene.Bounds is null)
        {
            DrawCenteredText(context, scene.StatusText, bounds, scene.Style.Text);
            return;
        }

        var transform = PreviewViewport.Fit(scene.Bounds, bounds.Width, bounds.Height, Interaction);
        DrawLines(context, scene.RemainingLines, transform, new Pen(scene.Style.Path, 1.4));
        DrawLines(context, scene.CompletedLines, transform, new Pen(scene.Style.CompletedPath, 2.2));
        DrawMachinePosition(context, scene, transform);
        DrawStatus(context, scene, bounds);
    }

    private static void DrawGrid(DrawingContext context, PreviewSceneModel scene, Rect bounds)
    {
        var pen = new Pen(scene.Style.Grid, 0.7);
        const double spacing = 40;
        for (var x = spacing; x < bounds.Width; x += spacing)
            context.DrawLine(pen, new Point(x, 0), new Point(x, bounds.Height));
        for (var y = spacing; y < bounds.Height; y += spacing)
            context.DrawLine(pen, new Point(0, y), new Point(bounds.Width, y));
    }

    private static void DrawLines(DrawingContext context, IReadOnlyList<PreviewLine> lines, PreviewViewportTransform transform, Pen pen)
    {
        foreach (var line in lines)
        {
            var start = transform.ToScreen(line.Start);
            var end = transform.ToScreen(line.End);
            context.DrawLine(pen, new Point(start.X, start.Y), new Point(end.X, end.Y));
        }
    }

    private static void DrawMachinePosition(DrawingContext context, PreviewSceneModel scene, PreviewViewportTransform transform)
    {
        if (scene.MachinePosition is null) return;
        var point = transform.ToScreen(scene.MachinePosition);
        var center = new Point(point.X, point.Y);
        var pen = new Pen(scene.Style.Cursor, 1.8);
        context.DrawEllipse(null, pen, center, 6, 6);
        context.DrawLine(pen, center.WithX(center.X - 9), center.WithX(center.X + 9));
        context.DrawLine(pen, center.WithY(center.Y - 9), center.WithY(center.Y + 9));
    }

    private static void DrawStatus(DrawingContext context, PreviewSceneModel scene, Rect bounds)
    {
        var text = new FormattedText(
            $"{scene.StatusText}  Progress {scene.Progress:P0}",
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            Typeface.Default,
            12,
            scene.Style.Text);
        context.DrawText(text, new Point(12, bounds.Height - text.Height - 10));
    }

    private static void DrawCenteredText(DrawingContext context, string textValue, Rect bounds, IBrush brush)
    {
        var text = new FormattedText(textValue, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, 16, brush);
        context.DrawText(text, new Point((bounds.Width - text.Width) / 2, (bounds.Height - text.Height) / 2));
    }
}
