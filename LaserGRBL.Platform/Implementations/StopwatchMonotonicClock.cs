using System.Diagnostics;
using LaserGRBL.Core.Abstractions;

namespace LaserGRBL.Platform.Implementations;

public sealed class StopwatchMonotonicClock : IMonotonicClock
{
    private readonly Stopwatch stopwatch = Stopwatch.StartNew();

    public TimeSpan Elapsed => stopwatch.Elapsed;
}
