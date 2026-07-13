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
        Assert.True(api.DrawArraysCalls > 0);
        Assert.True(api.UploadedVertexCount >= scene.Lines.Count * 2);
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
        public int DrawArraysCalls { get; private set; }
        public int UploadedVertexCount { get; private set; }
        public int ClearCalls { get; private set; }
        public void Viewport(int x, int y, int width, int height) { }
        public void ClearColor(float red, float green, float blue, float alpha) { }
        public void Clear(uint mask) => ClearCalls++;
        public void Enable(uint cap) { }
        public uint CreateShader(uint type) => type;
        public void ShaderSource(uint shader, string source) { }
        public void CompileShader(uint shader) { }
        public void GetShaderiv(uint shader, uint pname, out int value) => value = 1;
        public string GetShaderInfoLog(uint shader) => "";
        public uint CreateProgram() => 10;
        public void AttachShader(uint program, uint shader) { }
        public void LinkProgram(uint program) { }
        public void GetProgramiv(uint program, uint pname, out int value) => value = 1;
        public string GetProgramInfoLog(uint program) => "";
        public void DeleteShader(uint shader) { }
        public void UseProgram(uint program) { }
        public void GenVertexArrays(int count, uint[] arrays) => arrays[0] = 30;
        public void BindVertexArray(uint array) { }
        public void GenBuffers(int count, uint[] buffers) => buffers[0] = 20;
        public void BindBuffer(uint target, uint buffer) { }
        public void BufferData(uint target, int size, float[] data, uint usage) => UploadedVertexCount = data.Length / 7;
        public void EnableVertexAttribArray(uint index) { }
        public void VertexAttribPointer(uint index, int size, uint type, bool normalized, int stride, int offset) { }
        public void DrawArrays(uint mode, int first, int count) => DrawArraysCalls++;
        public uint GetError() => 0;
    }
}
