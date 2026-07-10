namespace LaserGRBL.Platform.Contracts;

public interface IAppPaths
{
    string DataDirectory { get; }

    string ConfigDirectory { get; }

    string CacheDirectory { get; }

    string LogDirectory { get; }
}
