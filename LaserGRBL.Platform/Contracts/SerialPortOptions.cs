using LaserGRBL.Core.Protocol;

namespace LaserGRBL.Platform.Contracts;

public sealed record SerialPortOptions(int BaudRate = 115200, int DataBits = 8, bool DtrEnable = false, bool RtsEnable = false, string NewLine = "\n", TimeSpan? ReadTimeout = null);

public interface ISerialConnection : IMachineTransport
{
    SerialPortDescriptor Port { get; }
    SerialPortOptions Options { get; }
    Task DiscardBuffersAsync(CancellationToken cancellationToken = default);
}
