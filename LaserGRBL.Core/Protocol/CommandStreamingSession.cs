using LaserGRBL.Core.Abstractions;

namespace LaserGRBL.Core.Protocol;

public interface ICommandTransport
{
    Task WriteAsync(string command, CancellationToken cancellationToken = default);

    Task WriteRealtimeAsync(byte command, CancellationToken cancellationToken = default) =>
        WriteAsync(((char)command).ToString(), cancellationToken);
}

public sealed record StreamedCommand(string Text, TimeSpan? JobOffset = null, int RepeatCount = 0);

public sealed class CommandStreamingSession
{
    private readonly Queue<StreamedCommand> queued = new();
    private readonly Queue<StreamedCommand> pendingCommands = new();
    private readonly IMonotonicClock clock;
    private StreamedCommand? retry;
    private TimeSpan pendingSince;
    private int usedBuffer;

    public CommandStreamingSession(IMonotonicClock clock, StreamingMode mode = StreamingMode.Buffered, int bufferCapacity = 128)
    {
        this.clock = clock;
        Mode = mode;
        BufferCapacity = bufferCapacity;
    }

    public StreamingMode Mode { get; }

    public int BufferCapacity { get; }

    public int UsedBuffer => usedBuffer;

    public int QueuedCount => queued.Count;

    public bool IsIdle => queued.Count == 0 && pendingCommands.Count == 0 && retry is null;

    public StreamedCommand? Pending => pendingCommands.TryPeek(out var command) ? command : null;

    public TimeSpan ExecutedDuration { get; private set; }

    public int RetryCount { get; private set; }

    public void Enqueue(StreamedCommand command) => queued.Enqueue(command);

    public void Cancel()
    {
        queued.Clear();
        pendingCommands.Clear();
        retry = null;
        usedBuffer = 0;
    }

    public async Task<bool> PumpAsync(ICommandTransport transport, CancellationToken cancellationToken = default)
    {
        if ((Mode == StreamingMode.Synchronous && pendingCommands.Count > 0) || (retry is null && queued.Count == 0)) return false;
        var next = retry ?? queued.Peek();
        var commandLength = EncodedLength(next);
        if (Mode == StreamingMode.Buffered && pendingCommands.Count > 0 && usedBuffer + commandLength > BufferCapacity) return false;
        if (retry is null) queued.Dequeue();
        retry = null;
        if (pendingCommands.Count == 0) pendingSince = clock.Elapsed;
        pendingCommands.Enqueue(next);
        usedBuffer += commandLength;
        await transport.WriteAsync(next.Text, cancellationToken);
        return true;
    }

    public OperationResult HandleResponse(bool accepted, string? detail = null)
    {
        if (pendingCommands.Count == 0) return OperationResult.Failure("Received a controller response with no pending command.", detail);
        var completed = pendingCommands.Dequeue();
        usedBuffer = Math.Max(0, usedBuffer - EncodedLength(completed));
        if (pendingCommands.Count > 0) pendingSince = clock.Elapsed;
        if (!accepted)
        {
            RetryCount++;
            if (Mode == StreamingMode.RepeatOnError && queued.Count == 0 && pendingCommands.Count == 0 && completed.RepeatCount < 3)
                retry = completed with { RepeatCount = completed.RepeatCount + 1 };
            return OperationResult.Failure("Controller rejected a command.", detail);
        }
        if (completed.JobOffset is { } offset && offset > ExecutedDuration) ExecutedDuration = offset;
        return OperationResult.Success();
    }

    public bool IsTimedOut(TimeSpan timeout) => pendingCommands.Count > 0 && clock.Elapsed - pendingSince > timeout;

    private static int EncodedLength(StreamedCommand command) => command.Text.Length + 1;
}
