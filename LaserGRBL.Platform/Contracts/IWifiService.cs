using LaserGRBL.Core.Abstractions;

namespace LaserGRBL.Platform.Contracts;

public sealed record WifiNetwork(string Ssid, int? SignalStrength = null, bool IsConnected = false, string? InterfaceName = null);

public sealed record WifiInterface(string Name, string Description, bool IsWireless, IReadOnlyList<string> Addresses);

public sealed record WifiConnectionRequest(string Ssid, string Password, string? InterfaceName = null, bool DryRun = false);

public interface IWifiService
{
    Task<OperationResult<IReadOnlyList<WifiNetwork>>> ListAsync(CancellationToken cancellationToken = default);

    Task<OperationResult<IReadOnlyList<WifiInterface>>> ListInterfacesAsync(CancellationToken cancellationToken = default);

    Task<OperationResult> ConnectAsync(WifiConnectionRequest request, CancellationToken cancellationToken = default);
}
