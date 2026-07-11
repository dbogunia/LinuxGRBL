using LaserGRBL.Platform.Implementations;
using Xunit;

namespace LaserGRBL.Linux.Tests;

public sealed class LinuxAppPathsTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"lasergrbl-paths-{Guid.NewGuid():N}");

    [Fact]
    public void Honors_xdg_overrides_without_using_real_home()
    {
        var paths = new LinuxAppPaths("LaserGRBL", name => name switch { "XDG_CONFIG_HOME" => "/config", "XDG_DATA_HOME" => "/data", "XDG_CACHE_HOME" => "/cache", _ => null }, "/home/test", "/temp");

        Assert.Equal(Path.Combine("/config", "LaserGRBL"), paths.ConfigDirectory);
        Assert.Equal(Path.Combine("/data", "LaserGRBL"), paths.DataDirectory);
        Assert.Equal(Path.Combine("/cache", "LaserGRBL"), paths.CacheDirectory);
    }

    [Fact]
    public void Uses_xdg_fallbacks_when_variables_are_missing()
    {
        var paths = new LinuxAppPaths("LaserGRBL", _ => null, "/home/test", "/temp");

        Assert.Equal("/home/test/.config/LaserGRBL", paths.ConfigDirectory);
        Assert.Equal("/home/test/.local/share/LaserGRBL", paths.DataDirectory);
        Assert.DoesNotContain('\\', paths.LogDirectory);
    }

    [Fact]
    public void Finds_resource_and_reports_missing_resource()
    {
        Directory.CreateDirectory(directory);
        var resource = Path.Combine(directory, "Sound", "connect.wav");
        Directory.CreateDirectory(Path.GetDirectoryName(resource)!);
        File.WriteAllText(resource, "test");
        var locator = new ResourceLocator(directory);

        Assert.Equal(resource, locator.Find("Sound", "connect.wav").Value);
        var missing = locator.Find("Firmware", "avrdude");
        Assert.False(missing.Succeeded);
        Assert.NotNull(missing.Error?.Detail);
        Assert.DoesNotContain('\\', missing.Error!.Detail!);
    }

    public void Dispose()
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, true);
    }
}
