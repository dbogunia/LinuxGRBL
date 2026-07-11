using System.IO.Ports;
using System.Runtime.CompilerServices;
using LaserGRBL.Platform.Contracts;

namespace LaserGRBL.Platform.Implementations;

public sealed class SystemSerialConnection : ISerialConnection
{
    private readonly SerialPort serial;
    public SystemSerialConnection(SerialPortDescriptor port, SerialPortOptions options)
    {
        Port = port; Options = options;
        serial = new SerialPort(port.DevicePath, options.BaudRate, Parity.None, options.DataBits, StopBits.One) { DtrEnable = options.DtrEnable, RtsEnable = options.RtsEnable, NewLine = options.NewLine, ReadTimeout = (int)(options.ReadTimeout ?? TimeSpan.FromSeconds(5)).TotalMilliseconds };
    }
    public SerialPortDescriptor Port { get; }
    public SerialPortOptions Options { get; }
    public bool IsOpen => serial.IsOpen;
    public Task OpenAsync(CancellationToken cancellationToken = default) { serial.Open(); return Task.CompletedTask; }
    public Task WriteAsync(string command, CancellationToken cancellationToken = default) => serial.BaseStream.WriteAsync(System.Text.Encoding.ASCII.GetBytes(command + Options.NewLine), cancellationToken).AsTask();
    public Task DiscardBuffersAsync(CancellationToken cancellationToken = default) { serial.DiscardInBuffer(); serial.DiscardOutBuffer(); return Task.CompletedTask; }
    public async IAsyncEnumerable<string> ReadLinesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (serial.IsOpen) yield return await Task.Run(serial.ReadLine, cancellationToken);
    }
    public ValueTask DisposeAsync() { serial.Dispose(); return ValueTask.CompletedTask; }
}
