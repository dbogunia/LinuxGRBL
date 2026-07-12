using LaserGRBL.Avalonia.Preview;
using LaserGRBL.Avalonia.Services;
using LaserGRBL.Avalonia.ViewModels;
using LaserGRBL.Core.Abstractions;
using LaserGRBL.Core.GCode;
using LaserGRBL.Platform.Contracts;
using Xunit;
using AvaloniaRect = Avalonia.Rect;
using AvaloniaSize = Avalonia.Size;

namespace LaserGRBL.Linux.Tests;

public sealed class AvaloniaOpenGlPreviewTests
{
    [Fact]
    public void Three_d_scene_model_is_generated_from_sample_gcode()
    {
        var scene = Build3D("G0 X0 Y0", "G1 X10 Y0", "G1 X10 Y5");

        Assert.False(scene.IsEmpty);
        Assert.Equal(2, scene.Lines.Count);
        Assert.NotNull(scene.Bounds);
        Assert.Equal(10, scene.Bounds!.MaxX);
        Assert.Equal(5, scene.Bounds.MaxY);
        Assert.Equal(0, scene.Bounds.MinZ);
    }

    [Fact]
    public void Camera_rotation_zoom_and_pan_project_scene_coordinates()
    {
        var bounds = new PreviewBounds3D(0, 0, 0, 10, 10, 10);
        var camera = new PreviewCamera3D().RotateBy(10, 5, 15).ZoomBy(2).PanBy(3, -4);

        var projected = Preview3DProjector.Project(new PreviewVertex3D(10, 10, 10), bounds, camera);

        Assert.NotEqual(0, projected.X);
        Assert.NotEqual(0, projected.Y);
        Assert.Equal(2, camera.Zoom);
        Assert.Equal(3, camera.PanX);
    }

    [Fact]
    public void Three_d_progress_segments_completed_and_remaining_paths()
    {
        var scene2d = new GCodePreviewRenderer().BuildScene(Job("G1 X1", "G1 X2", "G1 X3", "G1 X4"), Style(), 0.5);
        var scene3d = new Preview3DSceneBuilder().BuildScene(scene2d);

        Assert.Equal(4, scene3d.Lines.Count);
        Assert.Equal(2, scene3d.CompletedLines.Count);
        Assert.Equal(2, scene3d.RemainingLines.Count);
    }

    [Fact]
    public void Empty_scene_preserves_invalid_or_no_file_render_state()
    {
        var scene3d = new Preview3DSceneBuilder().BuildScene(PreviewSceneModel.Empty(Style()));

        Assert.True(scene3d.IsEmpty);
        Assert.Null(scene3d.Bounds);
        Assert.Equal("No file loaded", scene3d.StatusText);
    }

    [Fact]
    public async Task Open_gl_initialization_failure_path_is_visible_in_workflow()
    {
        var path = Path.Combine(Path.GetTempPath(), $"lasergrbl-{Guid.NewGuid():N}.gcode");
        await File.WriteAllLinesAsync(path, ["G1 X10 Y10"]);
        var workflow = new MainWorkflowViewModel(new EmptySerialPortService(), new EmptyInhibitor(), new EmptyMessageService(), openGlContextFactory: new FailingOpenGlFactory());

        await workflow.LoadFileAsync(path);

        Assert.False(workflow.Preview3DStatus.Available);
        Assert.Contains("fake OpenGL failure", workflow.Preview3DStatus.Diagnostic);
        Assert.False(workflow.Preview3DScene.IsEmpty);
        File.Delete(path);
    }

    [Fact]
    public void Every_named_color_scheme_maps_into_3d_preview_style()
    {
        foreach (var name in ColorSchemeCatalog.Default.Names)
        {
            var scheme = ColorSchemeCatalog.Default.Get(name);
            var style = Preview3DStyle.From2D(PreviewRenderStyle.FromScheme(scheme));

            Assert.Same(scheme.PreviewBackground, style.Background);
            Assert.Same(scheme.Border, style.Grid);
            Assert.Same(scheme.PreviewPath, style.Path);
            Assert.Same(scheme.Command, style.CompletedPath);
            Assert.Same(scheme.Warning, style.Cursor);
        }
    }

    [Fact]
    public void Three_d_control_smoke_path_accepts_scene_camera_and_failure_status()
    {
        var control = new JobPreview3DControl
        {
            Width = 320,
            Height = 220,
            Scene = Build3D("G1 X10 Y0", "G1 X10 Y10"),
            Camera = new PreviewCamera3D().RotateBy(5, 0, 5),
            ContextStatus = OpenGlPreviewContextStatus.Failure("headless")
        };

        control.Measure(new AvaloniaSize(320, 220));
        control.Arrange(new AvaloniaRect(0, 0, 320, 220));

        Assert.False(control.Scene!.IsEmpty);
        Assert.False(control.ContextStatus.Available);
    }

    private static Preview3DSceneModel Build3D(params string[] lines)
    {
        var scene2d = new GCodePreviewRenderer().BuildScene(Job(lines), Style());
        return new Preview3DSceneBuilder().BuildScene(scene2d);
    }

    private static PreviewRenderStyle Style() => PreviewRenderStyle.FromScheme(ColorSchemeCatalog.Default.Get("Default"));

    private static GCodeJob Job(params string[] lines)
    {
        var job = new GCodeJob();
        job.Load(lines, append: false);
        return job;
    }

    private sealed class FailingOpenGlFactory : IOpenGlPreviewContextFactory
    {
        public OpenGlPreviewContextStatus Probe() => OpenGlPreviewContextStatus.Failure("fake OpenGL failure");
    }

    private sealed class EmptyMessageService : IMessageService
    {
        public Task<bool> ShowAsync(MessageRequest request, CancellationToken cancellationToken = default) => Task.FromResult(true);
    }

    private sealed class EmptyInhibitor : IExecutionInhibitor
    {
        public Task<OperationResult<IAsyncDisposable?>> AcquireAsync(string reason, CancellationToken cancellationToken = default) =>
            Task.FromResult(OperationResult<IAsyncDisposable?>.Success(null));
    }

    private sealed class EmptySerialPortService : ISerialPortService
    {
        public Task<OperationResult<IReadOnlyList<SerialPortDescriptor>>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(OperationResult<IReadOnlyList<SerialPortDescriptor>>.Success([]));

        public Task<OperationResult<ISerialConnection>> OpenAsync(SerialPortDescriptor port, SerialPortOptions options, CancellationToken cancellationToken = default) =>
            Task.FromResult(OperationResult<ISerialConnection>.Failure("No serial ports."));
    }
}
