using LaserGRBL.Core.Abstractions;

namespace LaserGRBL.Platform.Contracts;

public sealed record SerialPortDescriptor(string Id, string DisplayName, string DevicePath);

public interface ISerialPortService
{
    Task<OperationResult<IReadOnlyList<SerialPortDescriptor>>> ListAsync(CancellationToken cancellationToken = default);

    Task<OperationResult<ISerialConnection>> OpenAsync(SerialPortDescriptor port, SerialPortOptions options, CancellationToken cancellationToken = default);
}
