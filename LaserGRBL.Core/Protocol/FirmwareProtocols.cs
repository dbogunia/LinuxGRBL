namespace LaserGRBL.Core.Protocol;

public interface IFirmwareProtocol
{
    FirmwareType Firmware { get; }

    bool UsesSynchronousStreaming { get; }

    bool SupportsTrueJogging(GrblVersion? version);

    bool SupportsGrblRealtimeCommands { get; }

    string PositionQuery { get; }

    string? ResetCommand { get; }
}

public static class FirmwareProtocols
{
    public static IFirmwareProtocol For(FirmwareType firmware) => firmware switch
    {
        FirmwareType.Grbl => Grbl,
        FirmwareType.Smoothie => Smoothie,
        FirmwareType.Marlin => Marlin,
        FirmwareType.VigoWork => VigoWork,
        _ => throw new ArgumentOutOfRangeException(nameof(firmware))
    };

    public static IFirmwareProtocol Grbl { get; } = new Protocol(FirmwareType.Grbl, false, true, "?", null, version => version?.CompareTo(new GrblVersion(1, 1)) >= 0);
    public static IFirmwareProtocol Smoothie { get; } = new Protocol(FirmwareType.Smoothie, true, false, "\n", "reset\r\n", _ => false);
    public static IFirmwareProtocol Marlin { get; } = new Protocol(FirmwareType.Marlin, true, false, "M114\n", null, _ => false);
    public static IFirmwareProtocol VigoWork { get; } = new Protocol(FirmwareType.VigoWork, false, false, "\x88", null, _ => false);

    private sealed record Protocol(FirmwareType Firmware, bool UsesSynchronousStreaming, bool SupportsGrblRealtimeCommands, string PositionQuery, string? ResetCommand, Func<GrblVersion?, bool> TrueJogPredicate) : IFirmwareProtocol
    {
        public bool SupportsTrueJogging(GrblVersion? version) => TrueJogPredicate(version);
    }
}
