using LaserGRBL.Platform.Contracts;

namespace LaserGRBL.Platform.Implementations;

public sealed class LinuxAppPaths : IAppPaths
{
    public LinuxAppPaths(string applicationName, Func<string, string?>? environment = null, string? homeDirectory = null, string? tempDirectory = null)
    {
        var get = environment ?? Environment.GetEnvironmentVariable;
        var home = homeDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var configBase = get("XDG_CONFIG_HOME") ?? Path.Combine(home, ".config");
        var dataBase = get("XDG_DATA_HOME") ?? Path.Combine(home, ".local", "share");
        var cacheBase = get("XDG_CACHE_HOME") ?? Path.Combine(home, ".cache");
        ConfigDirectory = Path.Combine(configBase, applicationName);
        DataDirectory = Path.Combine(dataBase, applicationName);
        CacheDirectory = Path.Combine(cacheBase, applicationName);
        LogDirectory = Path.Combine(DataDirectory, "logs");
        TempDirectory = Path.Combine(tempDirectory ?? Path.GetTempPath(), applicationName);
    }

    public string DataDirectory { get; }
    public string ConfigDirectory { get; }
    public string CacheDirectory { get; }
    public string LogDirectory { get; }
    public string TempDirectory { get; }
}
