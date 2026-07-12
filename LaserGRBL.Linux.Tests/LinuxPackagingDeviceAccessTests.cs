using System.Text.Json;
using LaserGRBL.Avalonia.Services;
using LaserGRBL.Platform.Implementations;
using Xunit;

namespace LaserGRBL.Linux.Tests;

public sealed class LinuxPackagingDeviceAccessTests
{
    [Fact]
    public void Desktop_and_mime_assets_are_shipped_and_match_file_open_contract()
    {
        var root = RepositoryRoot();
        var desktop = File.ReadAllText(Path.Combine(root, "packaging", "linux", "desktop", "linuxgrbl.desktop"));
        var mime = File.ReadAllText(Path.Combine(root, "packaging", "linux", "mime", "application-x-lasergrbl-project.xml"));
        var icon = File.ReadAllText(Path.Combine(root, "packaging", "linux", "icons", "linuxgrbl.svg"));

        Assert.Contains("Exec=LaserGRBL.Avalonia %f", desktop);
        Assert.Contains("application/x-lasergrbl-project", desktop);
        Assert.Contains("application/x-gcode", desktop);
        Assert.Contains("*.lps", mime);
        Assert.Contains("*.gcode", mime);
        Assert.Contains("<svg", icon);
    }

    [Fact]
    public void Tarball_script_copies_desktop_mime_and_icon_assets()
    {
        var script = File.ReadAllText(Path.Combine(RepositoryRoot(), "scripts", "build-linux-tarball.sh"));

        Assert.Contains("desktop/linuxgrbl.desktop", script);
        Assert.Contains("install-desktop-integration.sh", script);
        Assert.Contains("mime/application-x-lasergrbl-project.xml", script);
        Assert.Contains("icons/linuxgrbl.svg", script);
    }

    [Fact]
    public void Package_manifest_declares_user_scoped_desktop_integration()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(RepositoryRoot(), "packaging", "linux", "package-manifest.json")));
        var desktop = document.RootElement.GetProperty("desktopIntegration");

        Assert.Equal("desktop/linuxgrbl.desktop", desktop.GetProperty("desktopEntry").GetString());
        Assert.Equal("icons/linuxgrbl.svg", desktop.GetProperty("icon").GetString());
        Assert.Equal("mime/application-x-lasergrbl-project.xml", desktop.GetProperty("mimeInfo").GetString());
        Assert.Equal("install-desktop-integration.sh", desktop.GetProperty("installer").GetString());
        Assert.Equal("user", desktop.GetProperty("installScope").GetString());
    }

    [Fact]
    public void Desktop_installer_rewrites_exec_to_extracted_package_path()
    {
        var installer = File.ReadAllText(Path.Combine(RepositoryRoot(), "packaging", "linux", "install-desktop-integration.sh"));

        Assert.Contains("app_dir=", installer);
        Assert.Contains("Exec=$app_dir/LaserGRBL.Avalonia %f", installer);
        Assert.Contains("update-mime-database", installer);
        Assert.DoesNotContain("sudo", installer);
    }

    [Theory]
    [InlineData(new[] { "/tmp/job.gcode" }, "/tmp/job.gcode")]
    [InlineData(new[] { "--trace", "/tmp/job.lps" }, "/tmp/job.lps")]
    [InlineData(new[] { "" }, null)]
    public void Startup_file_arguments_use_first_non_option_path(string[] args, string? expected)
    {
        Assert.Equal(expected, StartupFileArguments.FirstOpenPath(args));
    }

    [Fact]
    public void Serial_permission_guidance_is_least_privilege_and_actionable()
    {
        var message = LinuxDeviceAccessPolicy.SerialPermissionDeniedMessage("/dev/ttyUSB0");

        Assert.Contains("/dev/ttyUSB0", message);
        Assert.Contains("/dev/serial/by-id", message);
        Assert.Contains("dialout", message);
        Assert.Equal("sudo usermod -aG dialout $USER", LinuxDeviceAccessPolicy.SerialGroupCommand());
    }

    [Fact]
    public void Packaging_readme_declares_supported_format_and_clean_install_blocker()
    {
        var readme = File.ReadAllText(Path.Combine(RepositoryRoot(), "packaging", "linux", "README.md"));

        Assert.Contains("self-contained `tar.gz`", readme);
        Assert.Contains("No `.deb`, RPM, AppImage, or Flatpak", readme);
        Assert.Contains("sudo usermod -aG dialout", readme);
        Assert.Contains("./install-desktop-integration.sh", readme);
        Assert.Contains("A real clean-install smoke test", readme);
        Assert.Contains("release-blocking", readme);
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
