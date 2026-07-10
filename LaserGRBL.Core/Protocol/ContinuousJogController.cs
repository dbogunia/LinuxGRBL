namespace LaserGRBL.Core.Protocol;

public sealed record ContinuousJogRequest(JogDirection? Direction, MachinePosition? Target, double Speed, bool IsAbort = false);

public sealed record ContinuousJogAction(bool AbortPrevious, string? Command);

public sealed class ContinuousJogController
{
    private ContinuousJogRequest? current;
    private ContinuousJogRequest? previous;

    public void RequestDirection(JogDirection direction, double speed)
    {
        if (direction is JogDirection.ZUp or JogDirection.ZDown or JogDirection.Home)
            throw new ArgumentOutOfRangeException(nameof(direction), "Continuous jog supports planar directions only.");
        current = new ContinuousJogRequest(direction, null, speed);
    }

    public void RequestPosition(MachinePosition target, double speed) => current = new ContinuousJogRequest(null, target, speed);

    public void Abort() => current = new ContinuousJogRequest(null, null, 0, true);

    public ContinuousJogAction? TakeNext()
    {
        if (current is null) return null;
        var next = current;
        current = null;
        var abortPrevious = previous is { IsAbort: false };
        previous = next;
        if (next.IsAbort) return new(abortPrevious, null);
        var command = next.Target is { } target
            ? JogCommandFactory.CreateAbsolute(target, next.Speed, true)
            : JogCommandFactory.CreateRelative(next.Direction!.Value, 1, next.Speed, true).Single();
        return new(abortPrevious, command);
    }
}
