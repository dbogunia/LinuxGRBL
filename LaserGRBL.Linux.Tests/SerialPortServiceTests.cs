using LaserGRBL.Platform.Contracts;
using LaserGRBL.Platform.Implementations;
using Xunit;

namespace LaserGRBL.Linux.Tests;

public sealed class SerialPortServiceTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"lasergrbl-serial-{Guid.NewGuid():N}");

    [Fact]
    public async Task Enumerates_ttyusb_ttyacm_and_stable_by_id_paths()
    {
        var dev = Path.Combine(root, "dev");
        var byId = Path.Combine(dev, "serial", "by-id");
        Directory.CreateDirectory(byId);
        File.WriteAllText(Path.Combine(dev, "ttyUSB0"), "");
        File.WriteAllText(Path.Combine(dev, "ttyACM0"), "");
        File.WriteAllText(Path.Combine(dev, "ttyS0"), "");
        File.WriteAllText(Path.Combine(byId, "usb-grbl"), "");

        var result = await new LinuxSerialPortService(dev, byId).ListAsync();

        Assert.True(result.Succeeded);
        Assert.Equal(3, result.Value?.Count);
        Assert.DoesNotContain(result.Value!, port => port.DisplayName == "ttyS0");
        Assert.StartsWith(byId, result.Value![0].DevicePath, StringComparison.Ordinal);
    }

    [Fact]
    public async Task In_memory_connection_preserves_dtr_rts_and_write_contract()
    {
        var port = new SerialPortDescriptor("fake", "Fake", "/dev/fake");
        var options = new SerialPortOptions(BaudRate: 230400, DataBits: 7, DtrEnable: true, RtsEnable: true, NewLine: "\r\n", ReadTimeout: TimeSpan.FromSeconds(2));
        var result = await new InMemorySerialPortService(port).OpenAsync(port, options);

        Assert.True(result.Succeeded);
        await result.Value!.OpenAsync();
        await result.Value.WriteAsync("G0 X0");
        Assert.True(result.Value.IsOpen);
        Assert.Equal(230400, result.Value.Options.BaudRate);
        Assert.Equal(7, result.Value.Options.DataBits);
        Assert.True(result.Value.Options.DtrEnable);
        Assert.True(result.Value.Options.RtsEnable);
        Assert.Equal("\r\n", result.Value.Options.NewLine);
        await result.Value.DiscardBuffersAsync();
    }

    [Fact]
    public async Task Missing_in_memory_device_returns_actionable_failure()
    {
        var result = await new InMemorySerialPortService().OpenAsync(new SerialPortDescriptor("missing", "Missing", "/dev/ttyUSB9"), new SerialPortOptions());

        Assert.False(result.Succeeded);
        Assert.Equal("/dev/ttyUSB9", result.Error?.Detail);
    }

    [Fact]
    public async Task Missing_system_device_returns_actionable_failure()
    {
        var missingPath = Path.Combine(root, "ttyUSB-missing");
        var port = new SerialPortDescriptor(missingPath, "Missing", missingPath);

        var result = await new LinuxSerialPortService().OpenAsync(port, new SerialPortOptions());

        Assert.False(result.Succeeded);
        Assert.Equal(missingPath, result.Error?.Detail);
    }

    public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root, true); }
}
