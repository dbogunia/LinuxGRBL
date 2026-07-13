using System.Text.Json;
using Xunit;

namespace LaserGRBL.Linux.Tests;

public sealed class LinuxCiSupportMatrixTests
{
    [Fact]
    public void Global_json_pins_dotnet_8_sdk()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(RepositoryRoot(), "global.json")));
        var sdk = document.RootElement.GetProperty("sdk");

        Assert.Equal("8.0.422", sdk.GetProperty("version").GetString());
        Assert.Equal("latestFeature", sdk.GetProperty("rollForward").GetString());
    }

    [Fact]
    public void Linux_ci_targets_linux_solution_only()
    {
        var workflow = File.ReadAllText(Path.Combine(RepositoryRoot(), ".github", "workflows", "linux-port.yml"));

        Assert.Contains("LaserGRBL.Linux.sln", workflow);
        Assert.DoesNotContain("dotnet build LaserGRBL.sln", workflow);
        Assert.DoesNotContain("dotnet test LaserGRBL.sln", workflow);
        Assert.Contains("packaging/linux/package-manifest.json", workflow);
    }

    [Fact]
    public void Support_matrix_documents_unsupported_graphics_and_arm64()
    {
        var matrix = File.ReadAllText(Path.Combine(RepositoryRoot(), "docs", "linux-support-matrix.md"));

        Assert.Contains("linux-x64", matrix);
        Assert.Contains("linux-arm64", matrix);
        Assert.Contains("not supported yet", matrix, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GPU/display validation remains a release blocker", matrix);
        Assert.Contains("LaserGRBL.Linux.sln", matrix);
    }

    [Fact]
    public void Ci_workflow_validates_package_metadata_without_publishing_release()
    {
        var workflow = File.ReadAllText(Path.Combine(RepositoryRoot(), ".github", "workflows", "linux-port.yml"));

        Assert.Contains("Validate package metadata", workflow);
        Assert.Contains("sha256sum", workflow);
        Assert.Contains("Build AppImage", workflow);
        Assert.Contains("Upload AppImage artifact", workflow);
        Assert.DoesNotContain("softprops/action-gh-release", workflow);
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
