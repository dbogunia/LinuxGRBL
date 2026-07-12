using System.Text.Json;
using LaserGRBL.Core.Abstractions;
using LaserGRBL.Platform.Contracts;
using LaserGRBL.Platform.Implementations;
using Xunit;

namespace LaserGRBL.Linux.Tests;

public sealed class SoundUpdatePackagingTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "linuxgrbl-task15-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
    }

    [Fact]
    public async Task Sound_missing_file_fails_without_throwing()
    {
        var service = new LinuxSoundService(new RecordedRunner(), directory);

        var result = await service.PlayAsync(SoundCue.Connect);

        Assert.False(result.Succeeded);
        Assert.Contains("not found", result.Error?.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Sound_disabled_succeeds_without_process()
    {
        var runner = new RecordedRunner();
        var service = new LinuxSoundService(runner, directory, enabled: false);

        var result = await service.PlayAsync(SoundCue.Warning);

        Assert.True(result.Succeeded);
        Assert.Empty(runner.Requests);
    }

    [Fact]
    public async Task Sound_uses_first_available_linux_player()
    {
        Directory.CreateDirectory(directory);
        File.WriteAllBytes(Path.Combine(directory, "success.wav"), [1, 2, 3]);
        var runner = new RecordedRunner(OperationResult<ProcessResult>.Failure("missing"), OperationResult<ProcessResult>.Success(new ProcessResult(0, "", "", false)));
        var service = new LinuxSoundService(runner, directory);

        var result = await service.PlayAsync(SoundCue.Success);

        Assert.True(result.Succeeded);
        Assert.Equal(["pw-play", "paplay"], runner.Requests.Select(request => request.FileName));
    }

    [Fact]
    public async Task Update_service_reports_available_release()
    {
        var service = new ReleaseManifestUpdateService(new ManifestClient("""{"version":"0.2.0","releaseUrl":"https://example.com/release","notes":"new"}"""), new Uri("https://example.com/manifest.json"), new Version(0, 1, 0));

        var result = await service.CheckAsync();

        Assert.True(result.Succeeded);
        Assert.Equal(new Version(0, 2, 0), result.Value?.Version);
        Assert.Equal("https://example.com/release", result.Value?.ReleaseUri.ToString());
    }

    [Fact]
    public async Task Update_service_returns_null_when_disabled_or_current()
    {
        var disabled = new ReleaseManifestUpdateService(new ManifestClient("""{"version":"9.9.9","releaseUrl":"https://example.com/release"}"""), new Uri("https://example.com/manifest.json"), new Version(0, 1, 0), enabled: false);
        var current = new ReleaseManifestUpdateService(new ManifestClient("""{"version":"0.1.0","releaseUrl":"https://example.com/release"}"""), new Uri("https://example.com/manifest.json"), new Version(0, 1, 0));

        Assert.Null((await disabled.CheckAsync()).Value);
        Assert.Null((await current.CheckAsync()).Value);
    }

    [Fact]
    public async Task Update_service_surfaces_manifest_failures()
    {
        var service = new ReleaseManifestUpdateService(new ManifestClient(null, OperationResult<string>.Failure("offline")), new Uri("https://example.com/manifest.json"), new Version(0, 1, 0));

        var result = await service.CheckAsync();

        Assert.False(result.Succeeded);
        Assert.Equal("offline", result.Error?.Message);
    }

    [Fact]
    public void Package_metadata_manifest_is_valid()
    {
        var manifest = File.ReadAllText(Path.Combine(RepositoryRoot(), "packaging", "linux", "package-manifest.json"));
        var metadata = JsonSerializer.Deserialize<PackageMetadata>(manifest, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        var result = new PackageMetadataService().Validate(metadata!);

        Assert.True(result.Succeeded);
        Assert.Contains(result.Value!.Dependencies, dependency => dependency.Name == "self-contained" && dependency.Required);
        Assert.Equal("SHA256", result.Value.IntegrityAlgorithm);
    }

    [Fact]
    public void Package_manifest_rejects_missing_notice()
    {
        var metadata = new PackageMetadata("linuxgrbl", new Version(0, 1, 0), "tar.gz", ["linux-x64"], [new PackageDependency("self-contained", "runtime", true)], [], "SHA256");

        var result = new PackageMetadataService().Validate(metadata);

        Assert.False(result.Succeeded);
        Assert.Contains("LICENSE", result.Error?.Message);
    }

    [Fact]
    public void Tarball_script_documents_publish_and_checksum_steps()
    {
        var script = File.ReadAllText(Path.Combine(RepositoryRoot(), "scripts", "build-linux-tarball.sh"));

        Assert.Contains("dotnet publish", script);
        Assert.Contains("--self-contained true", script);
        Assert.Contains("LaserGRBL/Sound", script);
        Assert.Contains("sha256sum", script);
        Assert.Contains("THIRD-PARTY-NOTICES.md", script);
    }

    private sealed class RecordedRunner(params OperationResult<ProcessResult>[] results) : IProcessRunner
    {
        private readonly Queue<OperationResult<ProcessResult>> results = new(results);
        public List<ProcessRequest> Requests { get; } = [];

        public Task<OperationResult<ProcessResult>> RunAsync(ProcessRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.FromResult(results.Count == 0 ? OperationResult<ProcessResult>.Failure("missing") : results.Dequeue());
        }
    }

    private sealed class ManifestClient(string? manifest, OperationResult<string>? result = null) : IUpdateManifestClient
    {
        public Task<OperationResult<string>> GetManifestAsync(Uri manifestUri, CancellationToken cancellationToken = default) =>
            Task.FromResult(result ?? OperationResult<string>.Success(manifest ?? ""));
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "LaserGRBL.Linux.sln"))) return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Unable to locate repository root.");
    }
}
