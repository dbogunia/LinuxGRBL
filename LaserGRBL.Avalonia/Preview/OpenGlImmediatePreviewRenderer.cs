using System.Runtime.InteropServices;
using Avalonia.Media;
using Avalonia.OpenGL;

namespace LaserGRBL.Avalonia.Preview;

public sealed class OpenGlImmediatePreviewRenderer
{
    private const uint GL_COLOR_BUFFER_BIT = 0x00004000;
    private const uint GL_DEPTH_BUFFER_BIT = 0x00000100;
    private const uint GL_DEPTH_TEST = 0x0B71;
    private const uint GL_LINES = 0x0001;

    private readonly IOpenGlApi gl;

    public OpenGlImmediatePreviewRenderer(IOpenGlApi gl) => this.gl = gl;

    public static OpenGlImmediatePreviewRenderer FromAvalonia(GlInterface gl) => new(new DelegateOpenGlApi(new AvaloniaOpenGlProcResolver(gl)));

    public void Render(Preview3DSceneModel scene, PreviewCamera3D camera, int width, int height)
    {
        var background = ToColor(scene.Style.Background);
        gl.Viewport(0, 0, Math.Max(1, width), Math.Max(1, height));
        gl.ClearColor(background.R, background.G, background.B, background.A);
        gl.Clear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
        gl.Enable(GL_DEPTH_TEST);
        if (scene.IsEmpty || scene.Bounds is null) return;

        DrawAxes(scene, camera);
        DrawLines(scene.RemainingLines, scene, camera, scene.Style.Path, 1.2f);
        DrawLines(scene.CompletedLines, scene, camera, scene.Style.CompletedPath, 2.3f);
        if (scene.MachinePosition is { } machine)
            DrawCross(scene, camera, machine, scene.Style.Cursor);
    }

    private void DrawAxes(Preview3DSceneModel scene, PreviewCamera3D camera)
    {
        var maxX = scene.Bounds!.MaxX + 10;
        var maxY = scene.Bounds.MaxY + 10;
        var maxZ = Math.Max(10, scene.Bounds.Depth + 10);
        DrawRawLine(scene, camera, new PreviewVertex3D(0, 0, 0), new PreviewVertex3D(maxX, 0, 0), scene.Style.AxisX, 1);
        DrawRawLine(scene, camera, new PreviewVertex3D(0, 0, 0), new PreviewVertex3D(0, maxY, 0), scene.Style.AxisY, 1);
        DrawRawLine(scene, camera, new PreviewVertex3D(0, 0, 0), new PreviewVertex3D(0, 0, maxZ), scene.Style.AxisZ, 1);
    }

    private void DrawLines(IReadOnlyList<PreviewLine3D> lines, Preview3DSceneModel scene, PreviewCamera3D camera, IBrush brush, float width)
    {
        var color = ToColor(brush);
        gl.Color4f(color.R, color.G, color.B, color.A);
        gl.LineWidth(width);
        gl.Begin(GL_LINES);
        foreach (var line in lines)
        {
            Vertex(scene, camera, line.Start);
            Vertex(scene, camera, line.End);
        }
        gl.End();
    }

    private void DrawCross(Preview3DSceneModel scene, PreviewCamera3D camera, PreviewVertex3D center, IBrush brush)
    {
        const double size = 1.5;
        DrawRawLine(scene, camera, center with { X = center.X - size }, center with { X = center.X + size }, brush, 1.7f);
        DrawRawLine(scene, camera, center with { Y = center.Y - size }, center with { Y = center.Y + size }, brush, 1.7f);
    }

    private void DrawRawLine(Preview3DSceneModel scene, PreviewCamera3D camera, PreviewVertex3D start, PreviewVertex3D endPoint, IBrush brush, float width)
    {
        var color = ToColor(brush);
        gl.Color4f(color.R, color.G, color.B, color.A);
        gl.LineWidth(width);
        gl.Begin(GL_LINES);
        Vertex(scene, camera, start);
        Vertex(scene, camera, endPoint);
        gl.End();
    }

    private void Vertex(Preview3DSceneModel scene, PreviewCamera3D camera, PreviewVertex3D value)
    {
        var projected = Preview3DProjector.Project(value, scene.Bounds, camera);
        var scale = FitScale(scene.Bounds!);
        gl.Vertex3d(projected.X * scale, projected.Y * scale, 0);
    }

    private static double FitScale(PreviewBounds3D bounds)
    {
        var span = Math.Max(1, Math.Max(bounds.Width, bounds.Height));
        return 1.6 / span;
    }

    private static GlColor ToColor(IBrush brush)
    {
        if (brush is not ISolidColorBrush solid) return new GlColor(1, 1, 1, 1);
        return new GlColor(solid.Color.R / 255f, solid.Color.G / 255f, solid.Color.B / 255f, solid.Color.A / 255f);
    }

    private readonly record struct GlColor(float R, float G, float B, float A);

}

public interface IOpenGlApi
{
    void Viewport(int x, int y, int width, int height);
    void ClearColor(float red, float green, float blue, float alpha);
    void Clear(uint mask);
    void Enable(uint cap);
    void LineWidth(float width);
    void Begin(uint mode);
    void End();
    void Color4f(float red, float green, float blue, float alpha);
    void Vertex3d(double x, double y, double z);
}

public interface IOpenGlProcResolver
{
    T GetProcAddress<T>(string name) where T : Delegate;
}

public sealed class AvaloniaOpenGlProcResolver(GlInterface gl) : IOpenGlProcResolver
{
    public T GetProcAddress<T>(string name) where T : Delegate
    {
        var pointer = gl.GetProcAddress(name);
        if (pointer == IntPtr.Zero) throw new OpenGlPreviewRendererException($"Required OpenGL function '{name}' is unavailable.");
        return Marshal.GetDelegateForFunctionPointer<T>(pointer);
    }
}

public sealed class DelegateOpenGlApi(IOpenGlProcResolver resolver) : IOpenGlApi
{
    private readonly GlViewport viewport = Required<GlViewport>(resolver, "glViewport");
    private readonly GlClearColor clearColor = Required<GlClearColor>(resolver, "glClearColor");
    private readonly GlClear clear = Required<GlClear>(resolver, "glClear");
    private readonly GlEnable enable = Required<GlEnable>(resolver, "glEnable");
    private readonly GlLineWidth lineWidth = Required<GlLineWidth>(resolver, "glLineWidth");
    private readonly GlBegin begin = Required<GlBegin>(resolver, "glBegin");
    private readonly GlEnd end = Required<GlEnd>(resolver, "glEnd");
    private readonly GlColor4f color4f = Required<GlColor4f>(resolver, "glColor4f");
    private readonly GlVertex3d vertex3d = Required<GlVertex3d>(resolver, "glVertex3d");

    public void Viewport(int x, int y, int width, int height) => viewport(x, y, width, height);
    public void ClearColor(float red, float green, float blue, float alpha) => clearColor(red, green, blue, alpha);
    public void Clear(uint mask) => clear(mask);
    public void Enable(uint cap) => enable(cap);
    public void LineWidth(float width) => lineWidth(width);
    public void Begin(uint mode) => begin(mode);
    public void End() => end();
    public void Color4f(float red, float green, float blue, float alpha) => color4f(red, green, blue, alpha);
    public void Vertex3d(double x, double y, double z) => vertex3d(x, y, z);

    private static T Required<T>(IOpenGlProcResolver resolver, string name) where T : Delegate
    {
        try { return resolver.GetProcAddress<T>(name); }
        catch (OpenGlPreviewRendererException) { throw; }
        catch (Exception exception)
        {
            throw new OpenGlPreviewRendererException($"Required OpenGL function '{name}' is unavailable: {exception.Message}");
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void GlViewport(int x, int y, int width, int height);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void GlClearColor(float red, float green, float blue, float alpha);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void GlClear(uint mask);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void GlEnable(uint cap);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void GlLineWidth(float width);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void GlBegin(uint mode);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void GlEnd();
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void GlColor4f(float red, float green, float blue, float alpha);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void GlVertex3d(double x, double y, double z);
}

public sealed class OpenGlPreviewRendererException(string message) : Exception(message);
