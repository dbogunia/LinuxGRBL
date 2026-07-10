namespace LaserGRBL.Core.Protocol;

/// <summary>Asynchronous controller channel; concrete serial/network implementations belong to platform tasks.</summary>
public interface IMachineTransport : ICommandTransport, IAsyncDisposable
{
    bool IsOpen { get; }

    Task OpenAsync(CancellationToken cancellationToken = default);

    IAsyncEnumerable<string> ReadLinesAsync(CancellationToken cancellationToken = default);
}
