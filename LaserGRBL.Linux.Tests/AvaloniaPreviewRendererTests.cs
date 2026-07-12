using Avalonia;
using Avalonia.Media;
using LaserGRBL.Avalonia.Preview;
using LaserGRBL.Avalonia.Services;
using LaserGRBL.Core.GCode;
using LaserGRBL.Core.Protocol;
using Xunit;

namespace LaserGRBL.Linux.Tests;

public sealed class AvaloniaPreviewRendererTests
{
    [Fact]
    public void Render_model_calculates_bounds_from_absolute_and_relative_moves()
    {
        var job = Job("G90", "G0 X0 Y0", "G1 X10 Y0", "G1 X10 Y5", "G91", "G1 X-2 Y3");
        var scene = Render(job);

        Assert.False(scene.IsEmpty);
        Assert.NotNull(scene.Bounds);
        Assert.Equal(0, scene.Bounds!.MinX);
        Assert.Equal(0, scene.Bounds.MinY);
        Assert.Equal(10, scene.Bounds.MaxX);
        Assert.Equal(8, scene.Bounds.MaxY);
        Assert.Equal(3, scene.Lines.Count);
    }

    [Fact]
    public void Zoom_pan_transform_round_trips_world_and_screen_coordinates()
    {
        var bounds = new PreviewBounds(0, 0, 10, 10);
        var interaction = new PreviewInteractionState().ZoomBy(2).PanBy(10, -5);
        var transform = PreviewViewport.Fit(bounds, 200, 100, interaction, padding: 0);

        var screen = transform.ToScreen(new PreviewPoint(4, 6));
        var world = transform.ToWorld(screen);

        Assert.Equal(4, world.X, precision: 6);
        Assert.Equal(6, world.Y, precision: 6);
        Assert.True(transform.Scale > 1);
    }

    [Fact]
    public void Progress_splits_completed_and_remaining_path_segments()
    {
        var scene = Render(Job("G1 X1", "G1 X2", "G1 X3", "G1 X4"), progress: 0.5);

        Assert.Equal(4, scene.Lines.Count);
        Assert.Equal(2, scene.CompletedLines.Count);
        Assert.Equal(2, scene.RemainingLines.Count);
        Assert.Equal(0.5, scene.Progress);
    }

    [Fact]
    public void Empty_scene_surfaces_no_file_state()
    {
        var scene = Render(new GCodeJob());

        Assert.True(scene.IsEmpty);
        Assert.Null(scene.Bounds);
        Assert.Equal("No file loaded", scene.StatusText);
    }

    [Fact]
    public void Renderer_interface_accepts_fake_scene_inputs_without_winforms_or_sharpgl()
    {
        IJobPreviewRenderer renderer = new GCodePreviewRenderer();
        var scene = renderer.BuildScene(Job("G1 X5 Y6"), Style(), 1, new MachinePosition(5, 6, 0));

        Assert.False(scene.IsEmpty);
        Assert.NotNull(scene.MachinePosition);
        Assert.Equal(5, scene.MachinePosition!.X);
        Assert.Single(scene.CompletedLines);
    }

    [Fact]
    public void Each_named_color_scheme_maps_preview_semantics_into_render_style()
    {
        foreach (var name in ColorSchemeCatalog.Default.Names)
        {
            var scheme = ColorSchemeCatalog.Default.Get(name);
            var style = PreviewRenderStyle.FromScheme(scheme);

            Assert.Same(scheme.PreviewBackground, style.Background);
            Assert.Same(scheme.Border, style.Grid);
            Assert.Same(scheme.PreviewPath, style.Path);
            Assert.Same(scheme.Command, style.CompletedPath);
            Assert.Same(scheme.Warning, style.Cursor);
            Assert.Same(scheme.MutedText, style.Text);
        }
    }

    [Fact]
    public void Preview_control_smoke_render_draws_nonblank_content()
    {
        var control = new JobPreviewControl
        {
            Width = 320,
            Height = 220,
            Scene = Render(Job("G1 X10 Y0", "G1 X10 Y10"), progress: 0.5)
        };
        control.Measure(new Size(320, 220));
        control.Arrange(new Rect(0, 0, 320, 220));

        Assert.False(control.Scene!.IsEmpty);
        Assert.Equal(2, control.Scene.Lines.Count);
    }

    private static PreviewSceneModel Render(GCodeJob job, double progress = 0) => new GCodePreviewRenderer().BuildScene(job, Style(), progress);

    private static PreviewRenderStyle Style() => PreviewRenderStyle.FromScheme(ColorSchemeCatalog.Default.Get("Default"));

    private static GCodeJob Job(params string[] lines)
    {
        var job = new GCodeJob();
        job.Load(lines, append: false);
        return job;
    }
}
