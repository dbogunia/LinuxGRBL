using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Text.Json;
using LaserGRBL.Core.Abstractions;
using LaserGRBL.Core.Settings;
using LaserGRBL.Platform.Contracts;

namespace LaserGRBL.Platform.Implementations;

public enum DiagnosticLogChannel { Application, Session, Communication, Connection }

public sealed record DiagnosticLogOptions(long MaxFileBytes = 512 * 1024, int RetainedArchives = 3);

public sealed record DiagnosticLogWriteResult(bool Succeeded, string Path, OperationError? Error = null);

public sealed class AppLogSink
{
    private readonly IAppPaths paths;
    private readonly DiagnosticLogOptions options;

    public AppLogSink(IAppPaths paths, DiagnosticLogOptions? options = null)
    {
        this.paths = paths;
        this.options = options ?? new DiagnosticLogOptions();
    }

    public string LogFilePath => GetPath(DiagnosticLogChannel.Application);

    public string GetPath(DiagnosticLogChannel channel) => Path.Combine(paths.LogDirectory, $"{FileStem(channel)}.log");

    public DiagnosticLogWriteResult Info(string message, DiagnosticLogChannel channel = DiagnosticLogChannel.Application) => Write("INFO", message, channel);

    public DiagnosticLogWriteResult Warning(string message, DiagnosticLogChannel channel = DiagnosticLogChannel.Application) => Write("WARN", message, channel);

    public DiagnosticLogWriteResult Error(string message, DiagnosticLogChannel channel = DiagnosticLogChannel.Application) => Write("ERROR", message, channel);

    public DiagnosticLogWriteResult CommunicationTx(string command) => Write("TX", command, DiagnosticLogChannel.Communication);

    public DiagnosticLogWriteResult CommunicationRx(string response) => Write("RX", response, DiagnosticLogChannel.Communication);

    public IReadOnlyList<string> RecentLines(DiagnosticLogChannel channel, int maxLines)
    {
        try
        {
            var path = GetPath(channel);
            if (!File.Exists(path)) return [];
            return File.ReadLines(path).TakeLast(Math.Max(0, maxLines)).ToArray();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return [$"Unable to read {channel} log: {exception.Message}"];
        }
    }

    private DiagnosticLogWriteResult Write(string level, string message, DiagnosticLogChannel channel)
    {
        var path = GetPath(channel);
        try
        {
            Directory.CreateDirectory(paths.LogDirectory);
            RotateIfNeeded(path);
            File.AppendAllText(path, $"{DateTimeOffset.UtcNow:O} {level} {message}{Environment.NewLine}");
            return new DiagnosticLogWriteResult(true, path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return new DiagnosticLogWriteResult(false, path, new OperationError("Unable to write diagnostic log.", path, exception));
        }
    }

    private void RotateIfNeeded(string path)
    {
        if (!File.Exists(path) || new FileInfo(path).Length < options.MaxFileBytes) return;
        for (var index = options.RetainedArchives; index >= 1; index--)
        {
            var source = index == 1 ? path : $"{path}.{index - 1}";
            var target = $"{path}.{index}";
            if (!File.Exists(source)) continue;
            if (index == options.RetainedArchives && File.Exists(target)) File.Delete(target);
            File.Move(source, target, overwrite: true);
        }
    }

    private static string FileStem(DiagnosticLogChannel channel) => channel switch
    {
        DiagnosticLogChannel.Application => "lasergrbl",
        DiagnosticLogChannel.Session => "session",
        DiagnosticLogChannel.Communication => "communication",
        DiagnosticLogChannel.Connection => "connection",
        _ => "lasergrbl"
    };
}

public sealed class DiagnosticRedactor
{
    private static readonly string[] SensitiveKeys = ["password", "passwd", "token", "secret", "authorization", "api_key", "apikey", "credential"];

    public string Redact(string value)
    {
        var lines = value.Split('\n');
        return string.Join('\n', lines.Select(RedactLine));
    }

    public string RedactPath(string value)
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return !string.IsNullOrWhiteSpace(home) ? value.Replace(home, "~", StringComparison.Ordinal) : value;
    }

    private static string RedactLine(string line)
    {
        var separator = line.Contains('=') ? '=' : line.Contains(':') ? ':' : '\0';
        if (separator == '\0') return RedactInlineSecrets(line);
        var index = line.IndexOf(separator);
        var key = line[..index].Trim();
        if (SensitiveKeys.Any(sensitive => key.Contains(sensitive, StringComparison.OrdinalIgnoreCase))) return $"{line[..(index + 1)]} [REDACTED]";
        return RedactInlineSecrets(line);
    }

    private static string RedactInlineSecrets(string line)
    {
        foreach (var key in SensitiveKeys)
        {
            var marker = $"{key}=";
            var index = line.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index < 0) continue;
            var end = line.IndexOf(' ', index);
            return end < 0 ? $"{line[..(index + marker.Length)]}[REDACTED]" : $"{line[..(index + marker.Length)]}[REDACTED]{line[end..]}";
        }

        return line;
    }
}

public sealed record SupportBundleRequest(
    string OutputPath,
    PortSettings Settings,
    IReadOnlyList<string> StartupDiagnostics,
    IReadOnlyList<SerialPortDescriptor> DiscoveredDevices,
    PackageMetadata? PackageMetadata = null,
    int RecentLogLines = 200);

public sealed record SupportBundleResult(string BundlePath, IReadOnlyList<string> Included, IReadOnlyList<string> Skipped);

public sealed class SupportBundleService
{
    private readonly IAppPaths paths;
    private readonly AppLogSink logs;
    private readonly DiagnosticRedactor redactor;

    public SupportBundleService(IAppPaths paths, AppLogSink logs, DiagnosticRedactor? redactor = null)
    {
        this.paths = paths;
        this.logs = logs;
        this.redactor = redactor ?? new DiagnosticRedactor();
    }

    public async Task<OperationResult<SupportBundleResult>> CreateAsync(SupportBundleRequest request, CancellationToken cancellationToken = default)
    {
        var included = new List<string>();
        var skipped = new List<string>();
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(request.OutputPath)!);
            if (File.Exists(request.OutputPath)) File.Delete(request.OutputPath);
            using var archive = ZipFile.Open(request.OutputPath, ZipArchiveMode.Create);

            AddText(archive, "manifest.json", JsonSerializer.Serialize(new
            {
                CreatedUtc = DateTimeOffset.UtcNow,
                AppVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown",
                Platform = Environment.OSVersion.ToString(),
                Runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription
            }, new JsonSerializerOptions { WriteIndented = true }), included);

            AddText(archive, "paths.txt", redactor.RedactPath($"""
            ConfigDirectory={paths.ConfigDirectory}
            DataDirectory={paths.DataDirectory}
            CacheDirectory={paths.CacheDirectory}
            LogDirectory={paths.LogDirectory}
            """), included);

            AddText(archive, "settings-summary.json", JsonSerializer.Serialize(new
            {
                request.Settings.SchemaVersion,
                request.Settings.Firmware,
                request.Settings.StreamingMode,
                request.Settings.ColorScheme,
                request.Settings.Language,
                RecentFileCount = request.Settings.RecentFiles.Count
            }, new JsonSerializerOptions { WriteIndented = true }), included);

            AddText(archive, "startup-diagnostics.txt", redactor.Redact(string.Join(Environment.NewLine, request.StartupDiagnostics)), included);
            AddText(archive, "device-discovery.json", JsonSerializer.Serialize(request.DiscoveredDevices, new JsonSerializerOptions { WriteIndented = true }), included);
            if (request.PackageMetadata is null) skipped.Add("package-metadata.json: no package metadata available");
            else AddText(archive, "package-metadata.json", JsonSerializer.Serialize(request.PackageMetadata, new JsonSerializerOptions { WriteIndented = true }), included);

            foreach (var channel in Enum.GetValues<DiagnosticLogChannel>())
            {
                var lines = logs.RecentLines(channel, request.RecentLogLines);
                if (lines.Count == 0)
                {
                    skipped.Add($"{channel}.log: no recent lines");
                    continue;
                }

                AddText(archive, $"logs/{channel.ToString().ToLowerInvariant()}.log", redactor.Redact(redactor.RedactPath(string.Join(Environment.NewLine, lines))), included);
            }

            await Task.CompletedTask.WaitAsync(cancellationToken);
            return OperationResult<SupportBundleResult>.Success(new SupportBundleResult(request.OutputPath, included, skipped));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            return OperationResult<SupportBundleResult>.Failure("Unable to create support bundle.", request.OutputPath, exception);
        }
    }

    private static void AddText(ZipArchive archive, string name, string content, ICollection<string> included)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
        using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
        writer.Write(content);
        included.Add(name);
    }
}
