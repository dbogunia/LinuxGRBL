using System.Runtime.CompilerServices;
using System.Threading.Channels;
using LaserGRBL.Core.Protocol;
using LaserGRBL.Platform.Contracts;

namespace LaserGRBL.Platform.Implementations;

public sealed class EmulatorMachineTransport : IMachineTransport
{
    private readonly Channel<string> received = Channel.CreateUnbounded<string>();
    private readonly Queue<TransportActivity> history = [];
    private readonly int historyLimit;
    private long sequence;
    private bool open;

    public EmulatorMachineTransport(int historyLimit = 200)
    {
        if (historyLimit <= 0) throw new ArgumentOutOfRangeException(nameof(historyLimit));
        this.historyLimit = historyLimit;
    }

    public event EventHandler<TransportActivity>? Activity;
    public bool IsOpen => open;
    public IReadOnlyList<TransportActivity> ActivityHistory { get { lock (history) return history.ToArray(); } }

    public Task OpenAsync(CancellationToken cancellationToken = default)
    {
        open = true;
        Publish(TransportActivityDirection.Received, "Grbl 1.1h ['$' for help]");
        return Task.CompletedTask;
    }

    public Task WriteAsync(string command, CancellationToken cancellationToken = default)
    {
        if (!open) throw new InvalidOperationException("Emulator transport is not open.");
        Publish(TransportActivityDirection.Transmitted, command);
        var response = command == "?" ? "<Idle|MPos:0.000,0.000,0.000|FS:0,0>" : "ok";
        Publish(TransportActivityDirection.Received, response);
        return Task.CompletedTask;
    }

    public async IAsyncEnumerable<string> ReadLinesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var line in received.Reader.ReadAllAsync(cancellationToken)) yield return line;
    }

    public ValueTask DisposeAsync() { open = false; received.Writer.TryComplete(); return ValueTask.CompletedTask; }

    private void Publish(TransportActivityDirection direction, string message)
    {
        var item = new TransportActivity(Interlocked.Increment(ref sequence), direction, message, DateTimeOffset.UtcNow);
        lock (history) { history.Enqueue(item); while (history.Count > historyLimit) history.Dequeue(); }
        Activity?.Invoke(this, item);
        if (direction == TransportActivityDirection.Received) received.Writer.TryWrite(message);
    }
}
