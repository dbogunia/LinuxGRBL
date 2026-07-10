namespace LaserGRBL.Core.Protocol;

public sealed record OverrideState(int Feed = 100, int Rapid = 100, int Spindle = 100)
{
    public static readonly OverrideState Default = new();
}
