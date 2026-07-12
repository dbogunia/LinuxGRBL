using Avalonia.Media;
using Avalonia.Media.Immutable;
using LaserGRBL.Avalonia.Preview;
using LaserGRBL.Core.GCode;
using Xunit;

namespace LaserGRBL.Linux.Tests;

public sealed class OpenGlImmediatePreviewRendererTests
{
    [Fact]
    public void Immediate_renderer_sends_nonblank_line_draw_calls()
    {
        var api = new StubOpenGlApi();
        var renderer = new OpenGlImmediatePreviewRenderer(api);
        var scene = BuildScene("G1 X10 Y0", "G1 X10 Y10");

        renderer.Render(scene, new PreviewCamera3D(), 640, 480);

        Assert.True(api.ClearCalls > 0);
        Assert.True(api.BeginCalls > 0);
        Assert.True(api.VertexCalls >= scene.Lines.Count * 2);
    }

    [Fact]
    public void Immediate_renderer_reports_missing_required_gl_function()
    {
        var exception = Assert.Throws<OpenGlPreviewRendererException>(() => new DelegateOpenGlApi(new MissingResolver()));

        Assert.Contains("Required OpenGL function", exception.Message);
    }

    private static Preview3DSceneModel BuildScene(params string[] lines)
    {
        var job = new GCodeJob();
        job.Load(lines, append: false);
        var style = new PreviewRenderStyle(
            Brush(0xFF12161D),
            Brush(0xFF303744),
            Brush(0xFF8AB4F8),
            Brush(0xFF6EE7B7),
            Brush(0xFFF59E0B),
            Brush(0xFFE5E7EB),
            Brush(0xFF4B5563));
        var scene2d = new GCodePreviewRenderer().BuildScene(job, style, 0.5);
        return new Preview3DSceneBuilder().BuildScene(scene2d);
    }

    private static IBrush Brush(uint color) => new ImmutableSolidColorBrush(color);

    private sealed class MissingResolver : IOpenGlProcResolver
    {
        public T GetProcAddress<T>(string name) where T : Delegate => throw new OpenGlPreviewRendererException($"Required OpenGL function '{name}' is unavailable.");
    }

    private sealed class StubOpenGlApi : IOpenGlApi
    {
        public int BeginCalls { get; private set; }
        public int VertexCalls { get; private set; }
        public int ClearCalls { get; private set; }
        public void Viewport(int x, int y, int width, int height) { }
        public void ClearColor(float red, float green, float blue, float alpha) { }
        public void Clear(uint mask) => ClearCalls++;
        public void Enable(uint cap) { }
        public void LineWidth(float width) { }
        public void Begin(uint mode) => BeginCalls++;
        public void End() { }
        public void Color4f(float red, float green, float blue, float alpha) { }
        public void Vertex3d(double x, double y, double z) => VertexCalls++;
    }
}
