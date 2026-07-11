using System.Net.NetworkInformation;
using System.Net.Sockets;
using LaserGRBL.Core.Abstractions;
using LaserGRBL.Platform.Contracts;

namespace LaserGRBL.Platform.Implementations;

public interface INetworkInterfaceProvider
{
    IReadOnlyList<NetworkInterfaceDescriptor> GetAll();
}

public sealed record NetworkInterfaceDescriptor(string Name, string Description, NetworkInterfaceType Type, OperationalStatus Status, IReadOnlyList<string> Addresses);

public sealed class SystemNetworkInterfaceProvider : INetworkInterfaceProvider
{
    public IReadOnlyList<NetworkInterfaceDescriptor> GetAll() => NetworkInterface.GetAllNetworkInterfaces()
        .Select(network => new NetworkInterfaceDescriptor(network.Name, network.Description, network.NetworkInterfaceType, network.OperationalStatus, network.GetIPProperties().UnicastAddresses
            .Where(address => address.Address.AddressFamily is AddressFamily.InterNetwork or AddressFamily.InterNetworkV6)
            .Select(address => address.Address.ToString())
            .ToArray()))
        .ToArray();
}

public sealed class LinuxWifiService(IProcessRunner processes, INetworkInterfaceProvider? interfaces = null, string nmcliPath = "nmcli") : IWifiService
{
    private readonly INetworkInterfaceProvider interfaces = interfaces ?? new SystemNetworkInterfaceProvider();

    public async Task<OperationResult<IReadOnlyList<WifiNetwork>>> ListAsync(CancellationToken cancellationToken = default)
    {
        var process = await processes.RunAsync(new ProcessRequest(nmcliPath, ["-t", "-f", "IN-USE,SSID,SIGNAL,DEVICE", "dev", "wifi", "list"], Timeout: TimeSpan.FromSeconds(15)), cancellationToken);
        if (!process.Succeeded) return OperationResult<IReadOnlyList<WifiNetwork>>.Failure("Unable to run nmcli. Install NetworkManager/nmcli or use serial/network connection manually.", process.Error?.Detail, process.Error?.Exception);
        if (process.Value!.TimedOut) return OperationResult<IReadOnlyList<WifiNetwork>>.Failure("nmcli WiFi scan timed out.");
        if (process.Value.ExitCode != 0) return OperationResult<IReadOnlyList<WifiNetwork>>.Failure(FormatNmcliFailure("Unable to list WiFi networks", process.Value.StandardError), process.Value.StandardError);
        return OperationResult<IReadOnlyList<WifiNetwork>>.Success(ParseWifiList(process.Value.StandardOutput));
    }

    public Task<OperationResult<IReadOnlyList<WifiInterface>>> ListInterfacesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = interfaces.GetAll()
                .Where(network => network.Status == OperationalStatus.Up && network.Type != NetworkInterfaceType.Loopback)
                .Select(network => new WifiInterface(network.Name, network.Description, IsWireless(network), network.Addresses))
                .OrderByDescending(network => network.IsWireless)
                .ThenBy(network => network.Name, StringComparer.Ordinal)
                .ToArray();
            return Task.FromResult(OperationResult<IReadOnlyList<WifiInterface>>.Success(result));
        }
        catch (Exception exception) when (exception is NetworkInformationException or UnauthorizedAccessException)
        {
            return Task.FromResult(OperationResult<IReadOnlyList<WifiInterface>>.Failure("Unable to inspect Linux network interfaces.", exception: exception));
        }
    }

    public async Task<OperationResult> ConnectAsync(WifiConnectionRequest request, CancellationToken cancellationToken = default)
    {
        var processRequest = BuildConnectRequest(request);
        if (request.DryRun) return OperationResult.Success();
        var process = await processes.RunAsync(processRequest, cancellationToken);
        if (!process.Succeeded) return OperationResult.Failure("Unable to run nmcli. Install NetworkManager/nmcli before configuring WiFi.", process.Error?.Detail, process.Error?.Exception);
        if (process.Value!.TimedOut) return OperationResult.Failure("nmcli WiFi connection timed out.");
        return process.Value.ExitCode == 0 ? OperationResult.Success() : OperationResult.Failure(FormatNmcliFailure("Unable to configure WiFi connection", process.Value.StandardError), process.Value.StandardError);
    }

    public ProcessRequest BuildConnectRequest(WifiConnectionRequest request)
    {
        var arguments = new List<string> { "dev", "wifi", "connect", request.Ssid, "password", request.Password };
        if (!string.IsNullOrWhiteSpace(request.InterfaceName)) arguments.AddRange(["ifname", request.InterfaceName]);
        return new ProcessRequest(nmcliPath, arguments, Timeout: TimeSpan.FromSeconds(30));
    }

    public static IReadOnlyList<WifiNetwork> ParseWifiList(string output)
    {
        var networks = new Dictionary<string, WifiNetwork>(StringComparer.Ordinal);
        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var fields = SplitNmcliTerseLine(line);
            if (fields.Count < 2 || string.IsNullOrWhiteSpace(fields[1])) continue;
            var ssid = fields[1];
            int? signal = fields.Count > 2 && int.TryParse(fields[2], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var value) ? value : null;
            var network = new WifiNetwork(ssid, signal, fields[0] == "*", fields.Count > 3 && !string.IsNullOrWhiteSpace(fields[3]) ? fields[3] : null);
            if (!networks.TryGetValue(ssid, out var existing) || network.IsConnected || (network.SignalStrength ?? -1) > (existing.SignalStrength ?? -1)) networks[ssid] = network;
        }
        return networks.Values.OrderByDescending(network => network.IsConnected).ThenByDescending(network => network.SignalStrength ?? -1).ThenBy(network => network.Ssid, StringComparer.Ordinal).ToArray();
    }

    private static IReadOnlyList<string> SplitNmcliTerseLine(string line)
    {
        var fields = new List<string>();
        var current = new System.Text.StringBuilder();
        var escaped = false;
        foreach (var character in line)
        {
            if (escaped) { current.Append(character); escaped = false; continue; }
            if (character == '\\') { escaped = true; continue; }
            if (character == ':') { fields.Add(current.ToString()); current.Clear(); continue; }
            current.Append(character);
        }
        fields.Add(current.ToString());
        return fields;
    }

    private static bool IsWireless(NetworkInterfaceDescriptor network) =>
        network.Type == NetworkInterfaceType.Wireless80211 || network.Name.StartsWith("wl", StringComparison.OrdinalIgnoreCase) || network.Description.Contains("wireless", StringComparison.OrdinalIgnoreCase) || network.Description.Contains("wi-fi", StringComparison.OrdinalIgnoreCase);

    private static string FormatNmcliFailure(string prefix, string stderr)
    {
        if (stderr.Contains("not authorized", StringComparison.OrdinalIgnoreCase) || stderr.Contains("permission", StringComparison.OrdinalIgnoreCase))
            return $"{prefix}. NetworkManager denied the request; run with an authorized desktop session or adjust polkit permissions.";
        if (stderr.Contains("NetworkManager is not running", StringComparison.OrdinalIgnoreCase) || stderr.Contains("Could not create NMClient", StringComparison.OrdinalIgnoreCase))
            return $"{prefix}. NetworkManager is unavailable on this system.";
        return $"{prefix}.";
    }
}
