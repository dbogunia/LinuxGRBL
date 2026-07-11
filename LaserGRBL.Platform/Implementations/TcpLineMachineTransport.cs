using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using LaserGRBL.Core.Protocol;

namespace LaserGRBL.Platform.Implementations;

public sealed class TcpLineMachineTransport(string host, int port, TimeSpan? connectTimeout = null) : IMachineTransport
{
    private readonly TimeSpan timeout = connectTimeout ?? TimeSpan.FromSeconds(5);
    private TcpClient? client;
    private StreamReader? reader;
    private StreamWriter? writer;

    public bool IsOpen => client?.Connected == true;

    public async Task OpenAsync(CancellationToken cancellationToken = default)
    {
        client = new TcpClient();
        using var timeoutSource = new CancellationTokenSource(timeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);
        await client.ConnectAsync(host, port, linked.Token);
        var stream = client.GetStream();
        reader = new StreamReader(stream, Encoding.ASCII, false, leaveOpen: true);
        writer = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true) { NewLine = "\n", AutoFlush = true };
    }

    public Task WriteAsync(string command, CancellationToken cancellationToken = default) =>
        writer is null ? throw new InvalidOperationException("TCP transport is not connected.") : writer.WriteLineAsync(command.AsMemory(), cancellationToken);

    public async IAsyncEnumerable<string> ReadLinesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (reader is null) throw new InvalidOperationException("TCP transport is not connected.");
        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null) yield break;
            yield return line;
        }
    }

    public ValueTask DisposeAsync()
    {
        writer?.Dispose(); reader?.Dispose(); client?.Dispose();
        return ValueTask.CompletedTask;
    }
}
