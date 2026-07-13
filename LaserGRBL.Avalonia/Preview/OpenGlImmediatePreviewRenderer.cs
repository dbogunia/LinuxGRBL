using System.Runtime.InteropServices;
using Avalonia.Media;
using Avalonia.OpenGL;

namespace LaserGRBL.Avalonia.Preview;

public sealed class OpenGlImmediatePreviewRenderer
{
    private const uint GL_COLOR_BUFFER_BIT = 0x00004000;
    private const uint GL_DEPTH_BUFFER_BIT = 0x00000100;
    private const uint GL_DEPTH_TEST = 0x0B71;
    private const uint GL_ARRAY_BUFFER = 0x8892;
    private const uint GL_STATIC_DRAW = 0x88E4;
    private const uint GL_LINES = 0x0001;
    private const uint GL_FLOAT = 0x1406;
    private const uint GL_FALSE = 0;
    private const uint GL_VERTEX_SHADER = 0x8B31;
    private const uint GL_FRAGMENT_SHADER = 0x8B30;
    private const uint GL_COMPILE_STATUS = 0x8B81;
    private const uint GL_LINK_STATUS = 0x8B82;

    private readonly IOpenGlApi gl;
    private uint program;
    private uint vertexBuffer;
    private uint vertexArray;

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

        var vertices = new List<float>();
        AddAxes(vertices, scene, camera);
        AddLines(vertices, scene.RemainingLines, scene, camera, scene.Style.Path);
        AddLines(vertices, scene.CompletedLines, scene, camera, scene.Style.CompletedPath);
        if (scene.MachinePosition is { } machine)
            AddCross(vertices, scene, camera, machine, scene.Style.Cursor);

        if (vertices.Count == 0) return;
        EnsureProgram();
        gl.UseProgram(program);
        gl.BindVertexArray(vertexArray);
        gl.BindBuffer(GL_ARRAY_BUFFER, vertexBuffer);
        gl.BufferData(GL_ARRAY_BUFFER, vertices.Count * sizeof(float), vertices.ToArray(), GL_STATIC_DRAW);
        gl.EnableVertexAttribArray(0);
        gl.VertexAttribPointer(0, 3, GL_FLOAT, normalized: false, stride: 7 * sizeof(float), offset: 0);
        gl.EnableVertexAttribArray(1);
        gl.VertexAttribPointer(1, 4, GL_FLOAT, normalized: false, stride: 7 * sizeof(float), offset: 3 * sizeof(float));
        gl.DrawArrays(GL_LINES, 0, vertices.Count / 7);
        var error = gl.GetError();
        if (error != 0) throw new OpenGlPreviewRendererException($"OpenGL draw failed with error 0x{error:X}.");
    }

    private void AddAxes(List<float> vertices, Preview3DSceneModel scene, PreviewCamera3D camera)
    {
        var maxX = scene.Bounds!.MaxX + 10;
        var maxY = scene.Bounds.MaxY + 10;
        var maxZ = Math.Max(10, scene.Bounds.Depth + 10);
        AddRawLine(vertices, scene, camera, new PreviewVertex3D(0, 0, 0), new PreviewVertex3D(maxX, 0, 0), scene.Style.AxisX);
        AddRawLine(vertices, scene, camera, new PreviewVertex3D(0, 0, 0), new PreviewVertex3D(0, maxY, 0), scene.Style.AxisY);
        AddRawLine(vertices, scene, camera, new PreviewVertex3D(0, 0, 0), new PreviewVertex3D(0, 0, maxZ), scene.Style.AxisZ);
    }

    private void AddLines(List<float> vertices, IReadOnlyList<PreviewLine3D> lines, Preview3DSceneModel scene, PreviewCamera3D camera, IBrush brush)
    {
        foreach (var line in lines)
            AddRawLine(vertices, scene, camera, line.Start, line.End, brush);
    }

    private void AddCross(List<float> vertices, Preview3DSceneModel scene, PreviewCamera3D camera, PreviewVertex3D center, IBrush brush)
    {
        const double size = 1.5;
        AddRawLine(vertices, scene, camera, center with { X = center.X - size }, center with { X = center.X + size }, brush);
        AddRawLine(vertices, scene, camera, center with { Y = center.Y - size }, center with { Y = center.Y + size }, brush);
    }

    private void AddRawLine(List<float> vertices, Preview3DSceneModel scene, PreviewCamera3D camera, PreviewVertex3D start, PreviewVertex3D endPoint, IBrush brush)
    {
        var color = ToColor(brush);
        AddVertex(vertices, scene, camera, start, color);
        AddVertex(vertices, scene, camera, endPoint, color);
    }

    private void AddVertex(List<float> vertices, Preview3DSceneModel scene, PreviewCamera3D camera, PreviewVertex3D value, GlColor color)
    {
        var projected = Preview3DProjector.Project(value, scene.Bounds, camera);
        var scale = FitScale(scene.Bounds!);
        vertices.Add((float)(projected.X * scale));
        vertices.Add((float)(projected.Y * scale));
        vertices.Add(0);
        vertices.Add(color.R);
        vertices.Add(color.G);
        vertices.Add(color.B);
        vertices.Add(color.A);
    }

    private void EnsureProgram()
    {
        if (program != 0) return;
        const string vertexShader = """
            #version 330 core
            layout(location = 0) in vec3 aPosition;
            layout(location = 1) in vec4 aColor;
            out vec4 vColor;
            void main()
            {
                gl_Position = vec4(aPosition, 1.0);
                vColor = aColor;
            }
            """;
        const string fragmentShader = """
            #version 330 core
            in vec4 vColor;
            out vec4 FragColor;
            void main()
            {
                FragColor = vColor;
            }
            """;

        var vertex = CompileShader(GL_VERTEX_SHADER, vertexShader);
        var fragment = CompileShader(GL_FRAGMENT_SHADER, fragmentShader);
        program = gl.CreateProgram();
        gl.AttachShader(program, vertex);
        gl.AttachShader(program, fragment);
        gl.LinkProgram(program);
        gl.GetProgramiv(program, GL_LINK_STATUS, out var linked);
        if (linked == 0) throw new OpenGlPreviewRendererException($"OpenGL shader program link failed: {gl.GetProgramInfoLog(program)}");
        gl.DeleteShader(vertex);
        gl.DeleteShader(fragment);
        var buffers = new uint[1];
        gl.GenBuffers(1, buffers);
        vertexBuffer = buffers[0];
        var arrays = new uint[1];
        gl.GenVertexArrays(1, arrays);
        vertexArray = arrays[0];
    }

    private uint CompileShader(uint type, string source)
    {
        var shader = gl.CreateShader(type);
        gl.ShaderSource(shader, source);
        gl.CompileShader(shader);
        gl.GetShaderiv(shader, GL_COMPILE_STATUS, out var compiled);
        if (compiled == 0) throw new OpenGlPreviewRendererException($"OpenGL shader compile failed: {gl.GetShaderInfoLog(shader)}");
        return shader;
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
    uint CreateShader(uint type);
    void ShaderSource(uint shader, string source);
    void CompileShader(uint shader);
    void GetShaderiv(uint shader, uint pname, out int value);
    string GetShaderInfoLog(uint shader);
    uint CreateProgram();
    void AttachShader(uint program, uint shader);
    void LinkProgram(uint program);
    void GetProgramiv(uint program, uint pname, out int value);
    string GetProgramInfoLog(uint program);
    void DeleteShader(uint shader);
    void UseProgram(uint program);
    void GenVertexArrays(int count, uint[] arrays);
    void BindVertexArray(uint array);
    void GenBuffers(int count, uint[] buffers);
    void BindBuffer(uint target, uint buffer);
    void BufferData(uint target, int size, float[] data, uint usage);
    void EnableVertexAttribArray(uint index);
    void VertexAttribPointer(uint index, int size, uint type, bool normalized, int stride, int offset);
    void DrawArrays(uint mode, int first, int count);
    uint GetError();
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
    private readonly GlCreateShader createShader = Required<GlCreateShader>(resolver, "glCreateShader");
    private readonly GlShaderSource shaderSource = Required<GlShaderSource>(resolver, "glShaderSource");
    private readonly GlCompileShader compileShader = Required<GlCompileShader>(resolver, "glCompileShader");
    private readonly GlGetShaderiv getShaderiv = Required<GlGetShaderiv>(resolver, "glGetShaderiv");
    private readonly GlGetShaderInfoLog getShaderInfoLog = Required<GlGetShaderInfoLog>(resolver, "glGetShaderInfoLog");
    private readonly GlCreateProgram createProgram = Required<GlCreateProgram>(resolver, "glCreateProgram");
    private readonly GlAttachShader attachShader = Required<GlAttachShader>(resolver, "glAttachShader");
    private readonly GlLinkProgram linkProgram = Required<GlLinkProgram>(resolver, "glLinkProgram");
    private readonly GlGetProgramiv getProgramiv = Required<GlGetProgramiv>(resolver, "glGetProgramiv");
    private readonly GlGetProgramInfoLog getProgramInfoLog = Required<GlGetProgramInfoLog>(resolver, "glGetProgramInfoLog");
    private readonly GlDeleteShader deleteShader = Required<GlDeleteShader>(resolver, "glDeleteShader");
    private readonly GlUseProgram useProgram = Required<GlUseProgram>(resolver, "glUseProgram");
    private readonly GlGenVertexArrays genVertexArrays = Required<GlGenVertexArrays>(resolver, "glGenVertexArrays");
    private readonly GlBindVertexArray bindVertexArray = Required<GlBindVertexArray>(resolver, "glBindVertexArray");
    private readonly GlGenBuffers genBuffers = Required<GlGenBuffers>(resolver, "glGenBuffers");
    private readonly GlBindBuffer bindBuffer = Required<GlBindBuffer>(resolver, "glBindBuffer");
    private readonly GlBufferData bufferData = Required<GlBufferData>(resolver, "glBufferData");
    private readonly GlEnableVertexAttribArray enableVertexAttribArray = Required<GlEnableVertexAttribArray>(resolver, "glEnableVertexAttribArray");
    private readonly GlVertexAttribPointer vertexAttribPointer = Required<GlVertexAttribPointer>(resolver, "glVertexAttribPointer");
    private readonly GlDrawArrays drawArrays = Required<GlDrawArrays>(resolver, "glDrawArrays");
    private readonly GlGetError getError = Required<GlGetError>(resolver, "glGetError");

    public void Viewport(int x, int y, int width, int height) => viewport(x, y, width, height);
    public void ClearColor(float red, float green, float blue, float alpha) => clearColor(red, green, blue, alpha);
    public void Clear(uint mask) => clear(mask);
    public void Enable(uint cap) => enable(cap);
    public uint CreateShader(uint type) => createShader(type);
    public void ShaderSource(uint shader, string source)
    {
        var count = 1;
        var sources = new[] { source };
        var lengths = new[] { source.Length };
        shaderSource(shader, count, sources, lengths);
    }
    public void CompileShader(uint shader) => compileShader(shader);
    public void GetShaderiv(uint shader, uint pname, out int value) => getShaderiv(shader, pname, out value);
    public string GetShaderInfoLog(uint shader) => InfoLog(buffer => getShaderInfoLog(shader, buffer.Length, out _, buffer));
    public uint CreateProgram() => createProgram();
    public void AttachShader(uint program, uint shader) => attachShader(program, shader);
    public void LinkProgram(uint program) => linkProgram(program);
    public void GetProgramiv(uint program, uint pname, out int value) => getProgramiv(program, pname, out value);
    public string GetProgramInfoLog(uint program) => InfoLog(buffer => getProgramInfoLog(program, buffer.Length, out _, buffer));
    public void DeleteShader(uint shader) => deleteShader(shader);
    public void UseProgram(uint program) => useProgram(program);
    public void GenVertexArrays(int count, uint[] arrays) => genVertexArrays(count, arrays);
    public void BindVertexArray(uint array) => bindVertexArray(array);
    public void GenBuffers(int count, uint[] buffers) => genBuffers(count, buffers);
    public void BindBuffer(uint target, uint buffer) => bindBuffer(target, buffer);
    public void BufferData(uint target, int size, float[] data, uint usage) => bufferData(target, size, data, usage);
    public void EnableVertexAttribArray(uint index) => enableVertexAttribArray(index);
    public void VertexAttribPointer(uint index, int size, uint type, bool normalized, int stride, int offset) => vertexAttribPointer(index, size, type, normalized ? (byte)1 : (byte)0, stride, offset);
    public void DrawArrays(uint mode, int first, int count) => drawArrays(mode, first, count);
    public uint GetError() => getError();

    private static string InfoLog(Action<byte[]> read)
    {
        var bytes = new byte[4096];
        read(bytes);
        var length = Array.IndexOf(bytes, (byte)0);
        if (length < 0) length = bytes.Length;
        return System.Text.Encoding.UTF8.GetString(bytes, 0, length).Trim();
    }

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
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate uint GlCreateShader(uint type);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void GlShaderSource(uint shader, int count, string[] source, int[] length);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void GlCompileShader(uint shader);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void GlGetShaderiv(uint shader, uint pname, out int value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void GlGetShaderInfoLog(uint shader, int bufferSize, out int length, byte[] infoLog);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate uint GlCreateProgram();
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void GlAttachShader(uint program, uint shader);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void GlLinkProgram(uint program);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void GlGetProgramiv(uint program, uint pname, out int value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void GlGetProgramInfoLog(uint program, int bufferSize, out int length, byte[] infoLog);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void GlDeleteShader(uint shader);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void GlUseProgram(uint program);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void GlGenVertexArrays(int count, uint[] arrays);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void GlBindVertexArray(uint array);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void GlGenBuffers(int count, uint[] buffers);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void GlBindBuffer(uint target, uint buffer);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void GlBufferData(uint target, int size, float[] data, uint usage);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void GlEnableVertexAttribArray(uint index);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void GlVertexAttribPointer(uint index, int size, uint type, byte normalized, int stride, int offset);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void GlDrawArrays(uint mode, int first, int count);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate uint GlGetError();
}

public sealed class OpenGlPreviewRendererException(string message) : Exception(message);
