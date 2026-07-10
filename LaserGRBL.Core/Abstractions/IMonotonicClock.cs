namespace LaserGRBL.Core.Abstractions;

/// <summary>Provides elapsed time that is unaffected by wall-clock changes.</summary>
public interface IMonotonicClock
{
    TimeSpan Elapsed { get; }
}
