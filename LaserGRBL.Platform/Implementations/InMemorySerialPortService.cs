using System.Runtime.CompilerServices;
using LaserGRBL.Core.Abstractions;
using LaserGRBL.Platform.Contracts;

namespace LaserGRBL.Platform.Implementations;

public sealed class InMemorySerialPortService : ISerialPortService
{
    private readonly List<SerialPortDescriptor> ports;
    public InMemorySerialPortService(params SerialPortDescriptor[] ports) => this.ports = ports.ToList();
    public Task<OperationResult<IReadOnlyList<SerialPortDescriptor>>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult(OperationResult<IReadOnlyList<SerialPortDescriptor>>.Success(ports));
    public Task<OperationResult<ISerialConnection>> OpenAsync(SerialPortDescriptor port, SerialPortOptions options, CancellationToken cancellationToken = default) =>
        ports.Contains(port) ? Task.FromResult(OperationResult<ISerialConnection>.Success(new Connection(port, options))) : Task.FromResult(OperationResult<ISerialConnection>.Failure("Serial device was not found.", port.DevicePath));

    private sealed class Connection(SerialPortDescriptor port, SerialPortOptions options) : ISerialConnection
    {
        public SerialPortDescriptor Port => port; public SerialPortOptions Options => options; public bool IsOpen { get; private set; }
        public List<string> Writes { get; } = [];
        public Task OpenAsync(CancellationToken cancellationToken = default) { IsOpen = true; return Task.CompletedTask; }
        public Task WriteAsync(string command, CancellationToken cancellationToken = default) { Writes.Add(command); return Task.CompletedTask; }
        public Task DiscardBuffersAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public ValueTask DisposeAsync() { IsOpen = false; return ValueTask.CompletedTask; }
        public async IAsyncEnumerable<string> ReadLinesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default) { await Task.CompletedTask; yield break; }
    }
}
