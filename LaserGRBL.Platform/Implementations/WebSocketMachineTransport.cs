using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using LaserGRBL.Core.Protocol;

namespace LaserGRBL.Platform.Implementations;

public sealed class WebSocketMachineTransport(Uri endpoint, TimeSpan? connectTimeout = null) : IMachineTransport
{
    private readonly TimeSpan timeout = connectTimeout ?? TimeSpan.FromSeconds(5);
    private readonly ClientWebSocket socket = new();
    public bool IsOpen => socket.State == WebSocketState.Open;

    public async Task OpenAsync(CancellationToken cancellationToken = default)
    {
        using var timeoutSource = new CancellationTokenSource(timeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);
        await socket.ConnectAsync(endpoint, linked.Token);
    }

    public Task WriteAsync(string command, CancellationToken cancellationToken = default) =>
        socket.SendAsync(Encoding.UTF8.GetBytes(command + "\n"), WebSocketMessageType.Text, true, cancellationToken);

    public async IAsyncEnumerable<string> ReadLinesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var buffer = new byte[4096];
        while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
        {
            var message = new StringBuilder();
            WebSocketReceiveResult result;
            do
            {
                result = await socket.ReceiveAsync(buffer, cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close) yield break;
                message.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
            } while (!result.EndOfMessage);
            foreach (var line in WebSocketMessageParser.SplitLines(message.ToString())) yield return line;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
            await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
        socket.Dispose();
    }
}

public static class WebSocketMessageParser
{
    public static IReadOnlyList<string> SplitLines(string message) => message.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
