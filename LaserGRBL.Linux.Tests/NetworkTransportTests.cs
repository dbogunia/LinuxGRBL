using System.Net;
using System.Net.Sockets;
using LaserGRBL.Platform.Contracts;
using LaserGRBL.Platform.Implementations;
using Xunit;

namespace LaserGRBL.Linux.Tests;

public sealed class NetworkTransportTests
{
    [Fact]
    public async Task Emulator_publishes_ordered_bounded_activity_and_protocol_responses()
    {
        await using var transport = new EmulatorMachineTransport(historyLimit: 3);
        var activity = new List<TransportActivity>();
        transport.Activity += (_, item) => activity.Add(item);

        await transport.OpenAsync();
        await transport.WriteAsync("?");
        await transport.WriteAsync("G0 X1");

        Assert.True(transport.IsOpen);
        Assert.Equal([1L, 2L, 3L, 4L, 5L], activity.Select(item => item.Sequence));
        Assert.Equal(3, transport.ActivityHistory.Count);
        Assert.Equal("G0 X1", transport.ActivityHistory[1].Message);
        await using var lines = transport.ReadLinesAsync().GetAsyncEnumerator();
        Assert.True(await lines.MoveNextAsync()); Assert.StartsWith("Grbl 1.1", lines.Current);
        Assert.True(await lines.MoveNextAsync()); Assert.StartsWith("<Idle", lines.Current);
        Assert.True(await lines.MoveNextAsync()); Assert.Equal("ok", lines.Current);
    }

    [Fact]
    public async Task Emulator_receive_wait_is_cancellable()
    {
        await using var transport = new EmulatorMachineTransport();
        await transport.OpenAsync();
        await using var lines = transport.ReadLinesAsync().GetAsyncEnumerator();
        Assert.True(await lines.MoveNextAsync()); // welcome message
        using var cancellation = new CancellationTokenSource();
        await using var pending = transport.ReadLinesAsync(cancellation.Token).GetAsyncEnumerator();
        var move = pending.MoveNextAsync().AsTask();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => move);
    }

    [Fact]
    public async Task Tcp_line_transport_round_trips_loopback_data()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var server = Task.Run(async () =>
        {
            using var accepted = await listener.AcceptTcpClientAsync();
            using var stream = accepted.GetStream();
            using var reader = new StreamReader(stream);
            using var writer = new StreamWriter(stream) { AutoFlush = true };
            Assert.Equal("G0 X1", await reader.ReadLineAsync());
            await writer.WriteLineAsync("ok");
        });

        await using var transport = new TcpLineMachineTransport("127.0.0.1", port);
        await transport.OpenAsync();
        await transport.WriteAsync("G0 X1");
        await using var lines = transport.ReadLinesAsync().GetAsyncEnumerator();
        Assert.True(await lines.MoveNextAsync());
        Assert.Equal("ok", lines.Current);
        await server;
        listener.Stop();
    }

    [Theory]
    [InlineData("ok\r\nerror:2\n", 2)]
    [InlineData("\n<Idle|MPos:0,0,0>\n", 1)]
    public void Web_socket_parser_splits_protocol_lines(string message, int expectedLineCount)
    {
        Assert.All(WebSocketMessageParser.SplitLines(message), line => Assert.NotEmpty(line));
        Assert.Equal(expectedLineCount, WebSocketMessageParser.SplitLines(message).Count);
    }
}
