using LaserGRBL.Core.Abstractions;
using LaserGRBL.Platform.Contracts;

namespace LaserGRBL.Platform.Implementations;

public sealed class LinuxSerialPortService : ISerialPortService
{
    private readonly string devDirectory;
    private readonly string byIdDirectory;
    public LinuxSerialPortService(string devDirectory = "/dev", string byIdDirectory = "/dev/serial/by-id") { this.devDirectory = devDirectory; this.byIdDirectory = byIdDirectory; }

    public Task<OperationResult<IReadOnlyList<SerialPortDescriptor>>> ListAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var ports = new List<SerialPortDescriptor>();
            if (Directory.Exists(devDirectory)) ports.AddRange(Directory.EnumerateFiles(devDirectory, "tty*", SearchOption.TopDirectoryOnly).Where(path => Path.GetFileName(path).StartsWith("ttyUSB", StringComparison.Ordinal) || Path.GetFileName(path).StartsWith("ttyACM", StringComparison.Ordinal)).Select(path => new SerialPortDescriptor(path, Path.GetFileName(path), path)));
            if (Directory.Exists(byIdDirectory)) ports.AddRange(Directory.EnumerateFiles(byIdDirectory).Select(path => new SerialPortDescriptor(path, Path.GetFileName(path), path)));
            return Task.FromResult(OperationResult<IReadOnlyList<SerialPortDescriptor>>.Success(ports.OrderBy(port => port.DevicePath).ToArray()));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Task.FromResult(OperationResult<IReadOnlyList<SerialPortDescriptor>>.Failure("Unable to enumerate Linux serial devices.", exception: exception));
        }
    }

    public async Task<OperationResult<ISerialConnection>> OpenAsync(SerialPortDescriptor port, SerialPortOptions options, CancellationToken cancellationToken = default)
    {
        var connection = new SystemSerialConnection(port, options);
        try { await connection.OpenAsync(cancellationToken); return OperationResult<ISerialConnection>.Success(connection); }
        catch (UnauthorizedAccessException exception) { await connection.DisposeAsync(); return OperationResult<ISerialConnection>.Failure(LinuxDeviceAccessPolicy.SerialPermissionDeniedMessage(port.DevicePath), port.DevicePath, exception); }
        catch (Exception exception) when (exception is IOException or ArgumentException) { await connection.DisposeAsync(); return OperationResult<ISerialConnection>.Failure("Unable to open serial device.", port.DevicePath, exception); }
    }
}
