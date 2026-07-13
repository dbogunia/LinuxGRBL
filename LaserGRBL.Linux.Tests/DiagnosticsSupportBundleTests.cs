using System.IO.Compression;
using LaserGRBL.Core.Protocol;
using LaserGRBL.Core.Settings;
using LaserGRBL.Platform.Contracts;
using LaserGRBL.Platform.Implementations;
using Xunit;

namespace LaserGRBL.Linux.Tests;

public sealed class DiagnosticsSupportBundleTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"lasergrbl-diagnostics-{Guid.NewGuid():N}");

    [Fact]
    public void Log_paths_use_fake_app_paths()
    {
        var paths = new TestPaths(directory);
        var log = new AppLogSink(paths);

        Assert.Equal(Path.Combine(directory, "logs", "lasergrbl.log"), log.LogFilePath);
        Assert.Equal(Path.Combine(directory, "logs", "communication.log"), log.GetPath(DiagnosticLogChannel.Communication));
    }

    [Fact]
    public void Writes_application_session_communication_and_connection_logs()
    {
        var log = new AppLogSink(new TestPaths(directory));

        Assert.True(log.Info("started").Succeeded);
        Assert.True(log.Info("session opened", DiagnosticLogChannel.Session).Succeeded);
        Assert.True(log.CommunicationTx("G0 X0").Succeeded);
        Assert.True(log.CommunicationRx("ok").Succeeded);
        Assert.True(log.Warning("connected", DiagnosticLogChannel.Connection).Succeeded);

        Assert.Contains("started", File.ReadAllText(log.GetPath(DiagnosticLogChannel.Application)));
        Assert.Contains("session opened", File.ReadAllText(log.GetPath(DiagnosticLogChannel.Session)));
        Assert.Contains("TX G0 X0", File.ReadAllText(log.GetPath(DiagnosticLogChannel.Communication)));
        Assert.Contains("RX ok", File.ReadAllText(log.GetPath(DiagnosticLogChannel.Communication)));
        Assert.Contains("connected", File.ReadAllText(log.GetPath(DiagnosticLogChannel.Connection)));
    }

    [Fact]
    public void Log_write_failure_is_reported_without_throwing()
    {
        var rootFile = Path.Combine(directory, "not-a-directory");
        Directory.CreateDirectory(directory);
        File.WriteAllText(rootFile, "blocking directory creation");
        var log = new AppLogSink(new TestPaths(rootFile));

        var result = log.Info("will fail");

        Assert.False(result.Succeeded);
        Assert.Contains("Unable to write diagnostic log", result.Error?.Message);
    }

    [Fact]
    public void Log_rotation_retains_bounded_archives()
    {
        var log = new AppLogSink(new TestPaths(directory), new DiagnosticLogOptions(MaxFileBytes: 20, RetainedArchives: 2));

        for (var index = 0; index < 8; index++) log.Info($"line {index} with enough bytes to rotate");

        Assert.True(File.Exists(log.LogFilePath));
        Assert.True(File.Exists($"{log.LogFilePath}.1"));
        Assert.True(File.Exists($"{log.LogFilePath}.2"));
        Assert.False(File.Exists($"{log.LogFilePath}.3"));
    }

    [Fact]
    public void Redactor_removes_sensitive_values_and_home_paths()
    {
        var redactor = new DiagnosticRedactor();

        var redacted = redactor.Redact("ssid=Workshop\npassword=secret\nAuthorization: Bearer abc\ntoken=abc next=value");
        var path = redactor.RedactPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "job.nc"));

        Assert.Contains("password= [REDACTED]", redacted);
        Assert.Contains("Authorization: [REDACTED]", redacted);
        Assert.Contains("token= [REDACTED]", redacted);
        Assert.DoesNotContain("secret", redacted);
        Assert.DoesNotContain(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), path);
    }

    [Fact]
    public async Task Support_bundle_contains_redacted_diagnostics_and_reports_skipped_files()
    {
        var paths = new TestPaths(directory);
        var log = new AppLogSink(paths);
        log.Info($"Opened {Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "private.nc")}");
        log.CommunicationTx("G0 X0");
        log.Warning("password=secret", DiagnosticLogChannel.Connection);
        var service = new SupportBundleService(paths, log);
        var bundlePath = Path.Combine(directory, "support.zip");

        var result = await service.CreateAsync(new SupportBundleRequest(
            bundlePath,
            PortSettings.Default with { RecentFiles = [new RecentFile("/home/user/private.nc", DateTimeOffset.UnixEpoch)] },
            ["Secret store unavailable", "token=abc"],
            [new SerialPortDescriptor("usb-grbl", "USB GRBL", "/dev/ttyUSB0")]));

        Assert.True(result.Succeeded, result.Error?.Message);
        Assert.Contains("manifest.json", result.Value!.Included);
        Assert.Contains("package-metadata.json: no package metadata available", result.Value.Skipped);

        using var archive = ZipFile.OpenRead(bundlePath);
        var startup = ReadEntry(archive, "startup-diagnostics.txt");
        var connection = ReadEntry(archive, "logs/connection.log");
        var application = ReadEntry(archive, "logs/application.log");
        var communication = ReadEntry(archive, "logs/communication.log");

        Assert.Contains("token= [REDACTED]", startup);
        Assert.Contains("password= [REDACTED]", connection);
        Assert.DoesNotContain(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), application);
        Assert.Contains("TX G0 X0", communication);
    }

    public void Dispose()
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
    }

    private static string ReadEntry(ZipArchive archive, string name)
    {
        var entry = archive.GetEntry(name);
        Assert.NotNull(entry);
        using var reader = new StreamReader(entry!.Open());
        return reader.ReadToEnd();
    }

    private sealed class TestPaths(string root) : IAppPaths
    {
        public string DataDirectory => Path.Combine(root, "data");
        public string ConfigDirectory => Path.Combine(root, "config");
        public string CacheDirectory => Path.Combine(root, "cache");
        public string LogDirectory => Path.Combine(root, "logs");
    }
}
