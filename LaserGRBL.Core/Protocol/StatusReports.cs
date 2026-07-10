namespace LaserGRBL.Core.Protocol;

public sealed record GrblStatusReport(
    MachineStatus Status,
    MachinePosition? MachinePosition = null,
    MachinePosition? WorkCoordinateOffset = null,
    double? FeedRate = null,
    double? SpindleSpeed = null,
    int? PlannerBuffer = null,
    int? SerialBuffer = null,
    int? FeedOverride = null,
    int? RapidOverride = null,
    int? SpindleOverride = null);

public sealed record MarlinStatusReport(MachineStatus Status, MachinePosition Position);

public sealed record VigoStatusReport(int State, int Received, int Managed, int Errors);
