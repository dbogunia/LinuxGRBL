using System.Net.NetworkInformation;
using LaserGRBL.Core.Abstractions;
using LaserGRBL.Platform.Contracts;
using LaserGRBL.Platform.Implementations;
using Xunit;

namespace LaserGRBL.Linux.Tests;

public sealed class LinuxWifiServiceTests
{
    [Fact]
    public void Nmcli_parser_handles_connected_networks_escaped_colons_and_sorting()
    {
        var networks = LinuxWifiService.ParseWifiList("""
            *:Shop\:Laser:80:wlan0
            :Guest:35:wlan0
            :Shop\:Laser:45:wlan1
            :Hidden::wlan0
            """);

        Assert.Equal(3, networks.Count);
        Assert.Equal("Shop:Laser", networks[0].Ssid);
        Assert.True(networks[0].IsConnected);
        Assert.Equal(80, networks[0].SignalStrength);
        Assert.Equal("Guest", networks[1].Ssid);
    }

    [Fact]
    public async Task Lists_networks_with_actionable_missing_nmcli_failure()
    {
        var service = new LinuxWifiService(new RecordedRunner(OperationResult<ProcessResult>.Failure("missing", "nmcli")));

        var result = await service.ListAsync();

        Assert.False(result.Succeeded);
        Assert.Contains("Install NetworkManager", result.Error!.Message);
        Assert.Equal("nmcli", result.Error.Detail);
    }

    [Fact]
    public async Task Lists_networks_formats_permission_denied_result()
    {
        var service = new LinuxWifiService(new RecordedRunner(OperationResult<ProcessResult>.Success(new ProcessResult(4, "", "not authorized to control networking", false))));

        var result = await service.ListAsync();

        Assert.False(result.Succeeded);
        Assert.Contains("polkit", result.Error!.Message);
    }

    [Fact]
    public async Task Lists_linux_interfaces_using_filterable_provider()
    {
        var provider = new FakeNetworkInterfaceProvider([
            new("lo", "Loopback", NetworkInterfaceType.Loopback, OperationalStatus.Up, ["127.0.0.1"]),
            new("eth0", "Ethernet", NetworkInterfaceType.Ethernet, OperationalStatus.Down, ["192.168.1.2"]),
            new("wlan0", "Wi-Fi adapter", NetworkInterfaceType.Ethernet, OperationalStatus.Up, ["192.168.10.20"]),
            new("enp3s0", "Wired", NetworkInterfaceType.Ethernet, OperationalStatus.Up, ["10.0.0.2"])
        ]);
        var service = new LinuxWifiService(new RecordedRunner(), provider);

        var result = await service.ListInterfacesAsync();

        Assert.True(result.Succeeded);
        Assert.Equal(["wlan0", "enp3s0"], result.Value!.Select(network => network.Name).ToArray());
        Assert.True(result.Value![0].IsWireless);
        Assert.False(result.Value![1].IsWireless);
    }

    [Fact]
    public void Connect_arguments_are_individual_and_do_not_shell_concatenate_user_values()
    {
        var request = new LinuxWifiService(new RecordedRunner()).BuildConnectRequest(new WifiConnectionRequest("Shop Laser", "pa ss:word", "wlan0"));

        Assert.Equal("nmcli", request.FileName);
        Assert.Equal(["dev", "wifi", "connect", "Shop Laser", "password", "pa ss:word", "ifname", "wlan0"], request.Arguments);
    }

    [Fact]
    public async Task Dry_run_connect_does_not_execute_network_change()
    {
        var runner = new RecordedRunner();
        var service = new LinuxWifiService(runner);

        var result = await service.ConnectAsync(new WifiConnectionRequest("ssid", "password", DryRun: true));

        Assert.True(result.Succeeded);
        Assert.Null(runner.LastRequest);
    }

    private sealed class RecordedRunner(OperationResult<ProcessResult>? result = null) : IProcessRunner
    {
        public ProcessRequest? LastRequest { get; private set; }

        public Task<OperationResult<ProcessResult>> RunAsync(ProcessRequest request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(result ?? OperationResult<ProcessResult>.Success(new ProcessResult(0, "", "", false)));
        }
    }

    private sealed class FakeNetworkInterfaceProvider(IReadOnlyList<NetworkInterfaceDescriptor> networks) : INetworkInterfaceProvider
    {
        public IReadOnlyList<NetworkInterfaceDescriptor> GetAll() => networks;
    }
}
