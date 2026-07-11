namespace LaserGRBL.Platform.Contracts;

public enum TransportActivityDirection { Transmitted, Received }

public sealed record TransportActivity(long Sequence, TransportActivityDirection Direction, string Message, DateTimeOffset Timestamp);
