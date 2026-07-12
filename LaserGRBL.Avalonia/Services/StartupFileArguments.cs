namespace LaserGRBL.Avalonia.Services;

public static class StartupFileArguments
{
    public static string? FirstOpenPath(IEnumerable<string>? args)
    {
        if (args is null) return null;

        foreach (var argument in args)
        {
            if (string.IsNullOrWhiteSpace(argument)) continue;
            if (argument.StartsWith("-", StringComparison.Ordinal)) continue;
            return argument;
        }

        return null;
    }
}
