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
        var appStream = File.ReadAllText(Path.Combine(root, "packaging", "linux", "metainfo", "io.github.dbogunia.LinuxGRBL.appdata.xml"));

        Assert.Contains("Exec=LaserGRBL.Avalonia %f", desktop);
        Assert.Contains("application/x-lasergrbl-project", desktop);
        Assert.Contains("application/x-gcode", desktop);
        Assert.Contains("*.lps", mime);
        Assert.Contains("*.gcode", mime);
        Assert.Contains("<svg", icon);
        Assert.Contains("<component type=\"desktop-application\">", appStream);
        Assert.Contains("<id>io.github.dbogunia.LinuxGRBL</id>", appStream);
        Assert.Contains("<launchable type=\"desktop-id\">io.github.dbogunia.LinuxGRBL.desktop</launchable>", appStream);
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
    public void Appimage_script_builds_appdir_with_desktop_mime_icon_and_checksums()
    {
        var script = File.ReadAllText(Path.Combine(RepositoryRoot(), "scripts", "build-linux-appimage.sh"));

        Assert.Contains("appimagetool", script);
        Assert.Contains("LinuxGRBL-$version-x86_64.AppImage", script);
        Assert.Contains("AppRun", script);
        Assert.Contains("Exec=AppRun %f", script);
        Assert.Contains("app_id=\"io.github.dbogunia.LinuxGRBL\"", script);
        Assert.Contains("$app_id.desktop", script);
        Assert.Contains("desktop-file-validate", script);
        Assert.Contains("application-x-lasergrbl-project.xml", script);
        Assert.Contains("io.github.dbogunia.LinuxGRBL.appdata.xml", script);
        Assert.Contains("linuxgrbl.svg", script);
        Assert.Contains("sha256sum", script);
        Assert.Contains("APPIMAGE_EXTRACT_AND_RUN=1", script);
        Assert.DoesNotContain("sudo", script);
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
        Assert.Equal("metainfo/io.github.dbogunia.LinuxGRBL.appdata.xml", desktop.GetProperty("appStream").GetString());

        var appImage = document.RootElement.GetProperty("additionalFormats").EnumerateArray().Single(format => format.GetProperty("format").GetString() == "AppImage");
        Assert.Equal("LinuxGRBL-0.1.0-x86_64.AppImage", appImage.GetProperty("artifact").GetString());
        Assert.Equal("metainfo/io.github.dbogunia.LinuxGRBL.appdata.xml", appImage.GetProperty("appStream").GetString());
        Assert.False(appImage.GetProperty("sandboxed").GetBoolean());
        Assert.Equal("host-serial-permissions", appImage.GetProperty("deviceAccess").GetString());
    }

    [Fact]
    public void Desktop_installer_rewrites_exec_to_extracted_package_path()
    {
        var installer = File.ReadAllText(Path.Combine(RepositoryRoot(), "packaging", "linux", "install-desktop-integration.sh"));

        Assert.Contains("app_dir=", installer);
        Assert.Contains("dirname \"${BASH_SOURCE[0]}\"", installer);
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
        Assert.Contains("AppImage as a second portable artifact", readme);
        Assert.Contains("No `.deb`, RPM, or Flatpak", readme);
        Assert.Contains("AppImage is not a sandbox", readme);
        Assert.Contains("sudo usermod -aG dialout", readme);
        Assert.Contains("./install-desktop-integration.sh", readme);
        Assert.Contains("A real clean-install smoke test", readme);
        Assert.Contains("release-blocking", readme);
    }

    [Fact]
    public void Release_hardware_validation_runner_is_fail_closed_and_prefers_stable_serial_paths()
    {
        var script = File.ReadAllText(Path.Combine(RepositoryRoot(), "scripts", "validate-release-hardware.sh"));

        Assert.Contains("sha256sum -c", script);
        Assert.Contains("LINUXGRBL_PACKAGE_FORMAT", script);
        Assert.Contains("appimage", script);
        Assert.Contains("LinuxGRBL-$version-x86_64.AppImage", script);
        Assert.Contains("--appimage-extract", script);
        Assert.Contains("APPIMAGE_EXTRACT_AND_RUN=1", script);
        Assert.Contains("install-desktop-integration.sh", script);
        Assert.Contains("/dev/serial/by-id", script);
        Assert.Contains("ttyUSB", script);
        Assert.Contains("ttyACM", script);
        Assert.Contains("blocked", script);
        Assert.Contains("exit 3", script);
        Assert.Contains("Manual GRBL Workflow Evidence Required", script);
        Assert.DoesNotContain("sudo", script);
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
