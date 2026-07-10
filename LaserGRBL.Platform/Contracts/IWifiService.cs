using LaserGRBL.Core.Abstractions;

namespace LaserGRBL.Platform.Contracts;

public sealed record WifiNetwork(string Ssid, int? SignalStrength = null, bool IsConnected = false);

public interface IWifiService
{
    Task<OperationResult<IReadOnlyList<WifiNetwork>>> ListAsync(CancellationToken cancellationToken = default);
}
