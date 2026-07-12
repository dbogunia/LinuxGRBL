using LaserGRBL.Avalonia.Services;
using LaserGRBL.Avalonia.ViewModels;
using LaserGRBL.Core.Abstractions;
using LaserGRBL.Core.Protocol;
using LaserGRBL.Core.Settings;
using LaserGRBL.Platform.Contracts;
using LaserGRBL.Platform.Implementations;
using Xunit;

namespace LaserGRBL.Linux.Tests;

public sealed class AvaloniaDialogToolsTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "linuxgrbl-dialog-tools-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
    }

    [Fact]
    public async Task Settings_tool_loads_saves_and_cancels_draft()
    {
        var store = new JsonSettingsStore(new TestPaths(directory));
        var tool = new SettingsToolViewModel(store, new FakeMessages());

        tool.Update(230400, FirmwareType.Marlin, StreamingMode.Synchronous, "Dark");
        await tool.SaveAsync();
        tool.Update(9600, FirmwareType.Grbl, StreamingMode.Buffered, "Default");
        tool.Cancel();
        await tool.LoadAsync();

        Assert.Equal(9600, tool.BaudRate);
        Assert.Equal(FirmwareType.Marlin, tool.Draft.Firmware);
        Assert.Equal("Settings loaded.", tool.Status);
    }

    [Fact]
    public async Task Custom_buttons_support_edit_delete_import_export_routing()
    {
        var files = new FakeFiles(open: ["/tmp/buttons.zbn"], save: "/tmp/buttons.json");
        var tool = new CustomButtonsToolViewModel(files);

        var button = tool.Add("Frame", "$H");
        tool.Edit(button.Id, "Home", "$H");
        var import = await tool.ImportAsync();
        var export = await tool.ExportAsync();
        tool.Delete(button.Id);

        Assert.Empty(tool.Buttons);
        Assert.True(import.Succeeded);
        Assert.Equal("/tmp/buttons.json", export.Value);
        Assert.NotNull(files.LastOpenRequest);
        Assert.Contains(".zbn", files.LastOpenRequest.Filters.Single().Extensions);
    }

    [Fact]
    public void Hotkeys_detect_conflicts()
    {
        var tool = new HotkeyManagerViewModel();

        var first = tool.Assign("Run", "Ctrl+R");
        var conflict = tool.Assign("Reset", "Ctrl+R");

        Assert.True(first.Succeeded);
        Assert.False(conflict.Succeeded);
        Assert.Contains("conflict", tool.Status, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Materials_upsert_clamps_values()
    {
        var tool = new MaterialEditorViewModel();

        var material = tool.Upsert("Birch", 1200, -4);

        Assert.Equal(1000, material.Power);
        Assert.Equal(1, material.Speed);
    }

    [Fact]
    public async Task Import_options_propagate_and_route_raster_files()
    {
        var files = new FakeFiles(open: ["/tmp/image.png"]);
        var tool = new ImportOptionsToolViewModel(files, new FakeMessages());

        tool.UpdateRaster(0, 1200, dither: false);
        tool.UpdateSvg(900, preserveColors: false);
        var selected = await tool.SelectRasterAsync();

        Assert.Equal(1, tool.Raster.Speed);
        Assert.Equal(1000, tool.Raster.Power);
        Assert.False(tool.Raster.Dither);
        Assert.False(tool.Svg.PreserveColors);
        Assert.True(selected.Succeeded);
        Assert.NotNull(files.LastOpenRequest);
        Assert.Contains(".png", files.LastOpenRequest.Filters.Single().Extensions);
    }

    [Fact]
    public async Task Firmware_flash_uses_selected_file_and_fake_service()
    {
        var firmware = new FakeFirmwareFlash();
        var tool = new FirmwareFlashToolViewModel(firmware, new FakeFiles(open: ["/tmp/fw.hex"]), new FakeMessages());

        await tool.SelectFirmwareAsync();
        var result = await tool.FlashAsync();

        Assert.True(result.Succeeded);
        Assert.Equal("/tmp/fw.hex", firmware.LastRequest?.FirmwarePath);
        Assert.True(firmware.LastRequest?.DryRun);
    }

    [Fact]
    public async Task Wifi_tool_discovers_and_configures_through_fake_service()
    {
        var wifi = new FakeWifi();
        var tool = new WifiToolViewModel(wifi, new FakeMessages()) { Password = "secret" };

        await tool.RefreshAsync();
        var result = await tool.ConnectAsync("Workshop", "wlan0");

        Assert.Single(tool.Networks);
        Assert.Single(tool.Interfaces);
        Assert.True(result.Succeeded);
        Assert.Equal("Workshop", wifi.LastRequest?.Ssid);
        Assert.True(wifi.LastRequest?.DryRun);
    }

    [Fact]
    public void Grbl_configuration_imports_valid_lines_and_rejects_invalid_text()
    {
        var tool = new GrblConfigurationToolViewModel(new FakeFiles(), new FakeMessages());

        var valid = tool.ImportText("$100=250\n$101=250");
        var invalid = tool.ImportText("$100=250\nbad");

        Assert.True(valid.Succeeded);
        Assert.False(invalid.Succeeded);
    }

    [Fact]
    public async Task Project_flows_surface_deferred_compatibility_status()
    {
        var messages = new FakeMessages();
        var tool = new ProjectFileToolViewModel(new FakeFiles(open: ["/tmp/job.lps"], save: "/tmp/out.lps"), messages);

        var open = await tool.OpenProjectAsync();
        var save = await tool.SaveProjectAsync();

        Assert.True(open.Succeeded);
        Assert.True(save.Succeeded);
        Assert.Contains("deferred to Task 18", tool.Status);
        Assert.NotEmpty(messages.Requests);
    }

    [Fact]
    public async Task Emulator_console_records_bounded_activity()
    {
        await using var transport = new EmulatorMachineTransport(historyLimit: 5);
        var console = new EmulatorActivityConsoleViewModel(historyLimit: 3);
        console.Attach(transport);

        await transport.OpenAsync();
        await transport.WriteAsync("?");
        await transport.WriteAsync("$I");

        Assert.Equal(3, console.Activity.Count);
        Assert.Equal("3 emulator event(s).", console.Status);
    }

    [Fact]
    public void Tool_hub_exposes_expected_groups()
    {
        var hub = new DialogToolsViewModel(new JsonSettingsStore(new TestPaths(directory)), new FakeFiles(), new FakeMessages(), new FakeWifi(), new FakeFirmwareFlash());

        Assert.Contains(hub.Groups, group => group.Name == "WiFi");
        Assert.Contains(hub.Groups, group => group.Name == "GRBL config");
        Assert.Contains(hub.Groups, group => group.Name == "Emulator");
    }

    private sealed class FakeFiles(IReadOnlyList<string>? open = null, string? save = null) : IFileDialogService
    {
        public FileDialogRequest? LastOpenRequest { get; private set; }
        public FileDialogRequest? LastSaveRequest { get; private set; }

        public Task<OperationResult<IReadOnlyList<string>>> OpenAsync(FileDialogRequest request, CancellationToken cancellationToken = default)
        {
            LastOpenRequest = request;
            return Task.FromResult(open is null ? OperationResult<IReadOnlyList<string>>.Failure("cancelled") : OperationResult<IReadOnlyList<string>>.Success(open));
        }

        public Task<OperationResult<string>> SaveAsync(FileDialogRequest request, CancellationToken cancellationToken = default)
        {
            LastSaveRequest = request;
            return Task.FromResult(save is null ? OperationResult<string>.Failure("cancelled") : OperationResult<string>.Success(save));
        }
    }

    private sealed class FakeMessages : IMessageService
    {
        public List<MessageRequest> Requests { get; } = [];
        public Task<bool> ShowAsync(MessageRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.FromResult(true);
        }
    }

    private sealed class FakeFirmwareFlash : IFirmwareFlashService
    {
        public FirmwareFlashRequest? LastRequest { get; private set; }
        public Task<OperationResult> FlashAsync(FirmwareFlashRequest request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(OperationResult.Success());
        }
    }

    private sealed class FakeWifi : IWifiService
    {
        public WifiConnectionRequest? LastRequest { get; private set; }

        public Task<OperationResult<IReadOnlyList<WifiNetwork>>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(OperationResult<IReadOnlyList<WifiNetwork>>.Success([new WifiNetwork("Workshop", 75)]));

        public Task<OperationResult<IReadOnlyList<WifiInterface>>> ListInterfacesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(OperationResult<IReadOnlyList<WifiInterface>>.Success([new WifiInterface("wlan0", "Wireless", true, ["192.168.1.8"])]));

        public Task<OperationResult> ConnectAsync(WifiConnectionRequest request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(OperationResult.Success());
        }
    }

    private sealed class TestPaths(string root) : IAppPaths
    {
        public string ConfigDirectory => root;
        public string DataDirectory => root;
        public string CacheDirectory => root;
        public string LogDirectory => root;
    }
}
