using Xunit;

namespace LaserGRBL.Linux.Tests;

public sealed class FeatureParityDocumentationTests
{
    [Fact]
    public void Feature_parity_matrix_documents_required_task22_areas()
    {
        var document = File.ReadAllText(Path.Combine(RepositoryRoot(), "docs", "feature-parity-and-sharpgl-decision.md"));

        foreach (var required in new[]
        {
            "Main connect/run/jog/manual command workflow",
            "Firmware variants: GRBL, Smoothie, Marlin, VigoWork",
            "SincroStart",
            "Raster/SVG conversion",
            ".lps",
            "Preview 3D/OpenGL",
            "Named color schemes",
            "Custom buttons",
            "Hotkeys",
            "Material editor",
            "Generators",
            "Firmware flashing",
            "WiFi",
            "Logs, diagnostics, support bundle",
            "Safety/legal/first-run",
            "Localization/resx",
            "User data compatibility",
            "Secret storage / Telegram",
            "Packaging, desktop/MIME, device/sandbox access",
            "Update/privacy/artifact integrity",
            "Emulator console",
            "Timing / sleep inhibition",
            "CI/support matrix",
            "Shutdown/recovery",
            "Single-device/endpoint ownership",
            "Image/font/GDI backend"
        })
        {
            Assert.Contains(required, document);
        }
    }

    [Fact]
    public void Sharpgl_decision_and_release_blockers_are_explicit()
    {
        var document = File.ReadAllText(Path.Combine(RepositoryRoot(), "docs", "feature-parity-and-sharpgl-decision.md"));

        Assert.Contains("required SharpGL/3D behavior is replaced by an Avalonia/OpenGL path", document);
        Assert.Contains("release-blocking incomplete", document);
        Assert.Contains("Real GPU/display nonblank OpenGL validation missing", document);
        Assert.Contains("Real USB GRBL hardware validation missing", document);
        Assert.Contains("Clean-install package/device validation missing", document);
        Assert.Contains("global named-event dependency", document);
        Assert.Contains("Cross-process coordinator", document, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Readiness_report_links_final_parity_and_updated_blockers()
    {
        var readiness = File.ReadAllText(Path.Combine(RepositoryRoot(), "docs", "linux-port-readiness.md"));

        Assert.Contains("Final parity matrix", readiness);
        Assert.Contains("Tasks 17-22 now provide", readiness);
        Assert.DoesNotContain("final parity, diagnostics, safety/legal, privacy, localization, and user-data tasks from Tasks 17-22", readiness);
        Assert.Contains("real OpenGL/GPU validation, real hardware serial validation, and clean-install package validation", readiness);
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
