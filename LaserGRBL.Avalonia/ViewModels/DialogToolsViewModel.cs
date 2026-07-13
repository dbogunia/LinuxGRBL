using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using LaserGRBL.Avalonia.Services;
using LaserGRBL.Core.Abstractions;
using LaserGRBL.Core.Protocol;
using LaserGRBL.Core.Safety;
using LaserGRBL.Core.Settings;
using LaserGRBL.Platform.Contracts;
using LaserGRBL.Platform.Implementations;

namespace LaserGRBL.Avalonia.ViewModels;

public sealed class DialogToolsViewModel
{
    public DialogToolsViewModel(
        JsonSettingsStore settings,
        IFileDialogService files,
        IMessageService messages,
        IWifiService wifi,
        IFirmwareFlashService firmware,
        LocalizationCatalog? localization = null,
        EmulatorActivityConsoleViewModel? emulator = null,
        ISafetyGate? safetyGate = null)
    {
        localization ??= LocalizationCatalog.Default;
        Settings = new SettingsToolViewModel(settings, messages);
        CustomButtons = new CustomButtonsToolViewModel(files);
        Hotkeys = new HotkeyManagerViewModel();
        Materials = new MaterialEditorViewModel();
        Imports = new ImportOptionsToolViewModel(files, messages);
        Firmware = new FirmwareFlashToolViewModel(firmware, files, messages, safetyGate);
        Wifi = new WifiToolViewModel(wifi, messages);
        GrblConfiguration = new GrblConfigurationToolViewModel(files, messages);
        Projects = new ProjectFileToolViewModel(files, messages);
        Emulator = emulator ?? new EmulatorActivityConsoleViewModel();
        Groups =
        [
            new ToolGroupViewModel(localization.Get("Tool.Settings.Name"), localization.Get("Tool.Settings.Description"), Settings.Status),
            new ToolGroupViewModel(localization.Get("Tool.CustomButtons.Name"), localization.Get("Tool.CustomButtons.Description"), CustomButtons.Status),
            new ToolGroupViewModel(localization.Get("Tool.Hotkeys.Name"), localization.Get("Tool.Hotkeys.Description"), Hotkeys.Status),
            new ToolGroupViewModel(localization.Get("Tool.Materials.Name"), localization.Get("Tool.Materials.Description"), Materials.Status),
            new ToolGroupViewModel(localization.Get("Tool.Import.Name"), localization.Get("Tool.Import.Description"), Imports.Status),
            new ToolGroupViewModel(localization.Get("Tool.Firmware.Name"), localization.Get("Tool.Firmware.Description"), Firmware.Status),
            new ToolGroupViewModel(localization.Get("Tool.Wifi.Name"), localization.Get("Tool.Wifi.Description"), Wifi.Status),
            new ToolGroupViewModel(localization.Get("Tool.GrblConfig.Name"), localization.Get("Tool.GrblConfig.Description"), GrblConfiguration.Status),
            new ToolGroupViewModel(localization.Get("Tool.Emulator.Name"), localization.Get("Tool.Emulator.Description"), Emulator.Status)
        ];
    }

    public SettingsToolViewModel Settings { get; }
    public CustomButtonsToolViewModel CustomButtons { get; }
    public HotkeyManagerViewModel Hotkeys { get; }
    public MaterialEditorViewModel Materials { get; }
    public ImportOptionsToolViewModel Imports { get; }
    public FirmwareFlashToolViewModel Firmware { get; }
    public WifiToolViewModel Wifi { get; }
    public GrblConfigurationToolViewModel GrblConfiguration { get; }
    public ProjectFileToolViewModel Projects { get; }
    public EmulatorActivityConsoleViewModel Emulator { get; }
    public IReadOnlyList<ToolGroupViewModel> Groups { get; }
}

public sealed record ToolGroupViewModel(string Name, string Description, string Status);

public abstract class ToolViewModelBase : INotifyPropertyChanged
{
    private string status = "Ready";
    private bool isBusy;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Status { get => status; protected set => Set(ref status, value); }
    public bool IsBusy { get => isBusy; protected set => Set(ref isBusy, value); }

    protected async Task RunAsync(Func<Task> action)
    {
        IsBusy = true;
        try { await action(); }
        finally { IsBusy = false; }
    }

    protected bool Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}

public sealed class SettingsToolViewModel(JsonSettingsStore settings, IMessageService messages) : ToolViewModelBase
{
    private PortSettings draft = PortSettings.Default;
    private int baudRate = 115200;

    public PortSettings Draft { get => draft; private set => Set(ref draft, value); }
    public int BaudRate { get => baudRate; private set => Set(ref baudRate, value); }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        await RunAsync(async () =>
        {
            var result = await settings.LoadAsync(cancellationToken);
            Draft = result.Value ?? PortSettings.Default;
            Status = result.Succeeded ? "Settings loaded." : $"Settings fallback loaded: {result.Error?.Message}";
        });
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        await RunAsync(async () =>
        {
            var result = await settings.SaveAsync(Draft, cancellationToken);
            Status = result.Succeeded ? "Settings saved." : $"Settings save failed: {result.Error?.Message}";
            if (!result.Succeeded) await messages.ShowAsync(new MessageRequest("Settings", Status, MessageSeverity.Error), cancellationToken);
        });
    }

    public void Cancel() => Status = "Settings edit cancelled.";

    public void Update(int baudRate, FirmwareType firmware, StreamingMode streaming, string colorScheme)
    {
        BaudRate = baudRate;
        Draft = Draft with { Firmware = firmware, StreamingMode = streaming, ColorScheme = colorScheme };
        Status = "Settings draft updated.";
    }
}

public sealed class CustomButtonsToolViewModel(IFileDialogService files) : ToolViewModelBase
{
    private int nextId = 1;

    public ObservableCollection<CustomButtonModel> Buttons { get; } = [];

    public CustomButtonModel Add(string label, string command)
    {
        var item = new CustomButtonModel(nextId++, label.Trim(), command.Trim());
        Buttons.Add(item);
        Status = $"Custom button '{item.Label}' added.";
        return item;
    }

    public void Edit(int id, string label, string command)
    {
        var item = Buttons.FirstOrDefault(button => button.Id == id);
        if (item is null) return;
        var index = Buttons.IndexOf(item);
        Buttons[index] = item with { Label = label.Trim(), Command = command.Trim() };
        Status = $"Custom button '{label.Trim()}' updated.";
    }

    public void Delete(int id)
    {
        var item = Buttons.FirstOrDefault(button => button.Id == id);
        if (item is null) return;
        Buttons.Remove(item);
        Status = $"Custom button '{item.Label}' deleted.";
    }

    public async Task<OperationResult<IReadOnlyList<string>>> ImportAsync(CancellationToken cancellationToken = default)
    {
        var result = await files.OpenAsync(new FileDialogRequest("Import custom buttons", [new FileTypeFilter("LaserGRBL buttons", [".zbn", ".json"])]), cancellationToken);
        Status = result.Succeeded ? "Custom button import file selected." : $"Custom button import failed: {result.Error?.Message}";
        return result;
    }

    public async Task<OperationResult<string>> ExportAsync(CancellationToken cancellationToken = default)
    {
        var result = await files.SaveAsync(new FileDialogRequest("Export custom buttons", [new FileTypeFilter("LaserGRBL buttons", [".json"])]), cancellationToken);
        Status = result.Succeeded ? "Custom button export target selected." : $"Custom button export failed: {result.Error?.Message}";
        return result;
    }
}

public sealed record CustomButtonModel(int Id, string Label, string Command);

public sealed class HotkeyManagerViewModel : ToolViewModelBase
{
    public ObservableCollection<HotkeyBindingModel> Bindings { get; } = [];

    public OperationResult Assign(string action, string gesture)
    {
        if (Bindings.Any(binding => binding.Gesture.Equals(gesture, StringComparison.OrdinalIgnoreCase) && !binding.Action.Equals(action, StringComparison.OrdinalIgnoreCase)))
        {
            Status = $"Hotkey conflict for {gesture}.";
            return OperationResult.Failure(Status);
        }

        var existing = Bindings.FirstOrDefault(binding => binding.Action.Equals(action, StringComparison.OrdinalIgnoreCase));
        if (existing is not null) Bindings[Bindings.IndexOf(existing)] = existing with { Gesture = gesture };
        else Bindings.Add(new HotkeyBindingModel(action, gesture));
        Status = $"Hotkey assigned: {action} = {gesture}.";
        return OperationResult.Success();
    }
}

public sealed record HotkeyBindingModel(string Action, string Gesture);

public sealed class MaterialEditorViewModel : ToolViewModelBase
{
    public ObservableCollection<MaterialProfileModel> Materials { get; } = [];

    public MaterialProfileModel Upsert(string name, int power, int speed)
    {
        var normalized = new MaterialProfileModel(name.Trim(), Math.Clamp(power, 0, 1000), Math.Max(1, speed));
        var existing = Materials.FirstOrDefault(material => material.Name.Equals(normalized.Name, StringComparison.OrdinalIgnoreCase));
        if (existing is null) Materials.Add(normalized);
        else Materials[Materials.IndexOf(existing)] = normalized;
        Status = $"Material '{normalized.Name}' saved.";
        return normalized;
    }
}

public sealed record MaterialProfileModel(string Name, int Power, int Speed);

public sealed class ImportOptionsToolViewModel(IFileDialogService files, IMessageService messages) : ToolViewModelBase
{
    public RasterImportOptions Raster { get; private set; } = new(1000, 300, true);
    public SvgImportOptions Svg { get; private set; } = new(1000, true);

    public void UpdateRaster(int speed, int power, bool dither)
    {
        Raster = new RasterImportOptions(Math.Max(1, speed), Math.Clamp(power, 0, 1000), dither);
        Status = "Raster import options updated.";
    }

    public void UpdateSvg(int speed, bool preserveColors)
    {
        Svg = new SvgImportOptions(Math.Max(1, speed), preserveColors);
        Status = "SVG import options updated.";
    }

    public async Task<OperationResult<IReadOnlyList<string>>> SelectRasterAsync(CancellationToken cancellationToken = default)
    {
        var result = await files.OpenAsync(new FileDialogRequest("Open raster image", [new FileTypeFilter("Raster images", [".png", ".jpg", ".jpeg", ".bmp", ".gif"])]), cancellationToken);
        Status = result.Succeeded ? "Raster import file selected." : $"Raster import cancelled: {result.Error?.Message}";
        if (!result.Succeeded) await messages.ShowAsync(new MessageRequest("Raster import", Status, MessageSeverity.Warning), cancellationToken);
        return result;
    }
}

public sealed record RasterImportOptions(int Speed, int Power, bool Dither);
public sealed record SvgImportOptions(int Speed, bool PreserveColors);

public sealed class FirmwareFlashToolViewModel(IFirmwareFlashService firmware, IFileDialogService files, IMessageService messages, ISafetyGate? safetyGate = null) : ToolViewModelBase
{
    public string DevicePath { get; set; } = "/dev/ttyACM0";
    public string FirmwarePath { get; private set; } = "";
    public int BaudRate { get; set; } = 115200;
    public bool DryRun { get; set; } = true;

    public async Task<OperationResult<IReadOnlyList<string>>> SelectFirmwareAsync(CancellationToken cancellationToken = default)
    {
        var result = await files.OpenAsync(new FileDialogRequest("Select firmware", [new FileTypeFilter("Firmware", [".hex", ".bin"])]), cancellationToken);
        if (result.Succeeded && result.Value?.FirstOrDefault() is { } path) FirmwarePath = path;
        Status = result.Succeeded ? "Firmware file selected." : $"Firmware selection failed: {result.Error?.Message}";
        return result;
    }

    public async Task<OperationResult> FlashAsync(CancellationToken cancellationToken = default)
    {
        var allowed = (safetyGate ?? PermissiveSafetyGate.Instance).EnsureAllowed(RiskyOperation.FirmwareFlash);
        if (!allowed.Succeeded)
        {
            Status = $"Firmware flash blocked: {allowed.Error?.Message}";
            await messages.ShowAsync(new MessageRequest("Firmware flash", Status, MessageSeverity.Warning), cancellationToken);
            return allowed;
        }

        var result = await firmware.FlashAsync(new FirmwareFlashRequest(DevicePath, FirmwarePath, BaudRate, DryRun), cancellationToken);
        Status = result.Succeeded ? (DryRun ? "Firmware flash dry-run completed." : "Firmware flash command completed.") : $"Firmware flash failed: {result.Error?.Message}";
        if (!result.Succeeded) await messages.ShowAsync(new MessageRequest("Firmware flash", Status, MessageSeverity.Error), cancellationToken);
        return result;
    }
}

public sealed class WifiToolViewModel(IWifiService wifi, IMessageService messages) : ToolViewModelBase
{
    public ObservableCollection<WifiNetwork> Networks { get; } = [];
    public ObservableCollection<WifiInterface> Interfaces { get; } = [];
    public string Password { get; set; } = "";
    public bool DryRun { get; set; } = true;

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        await RunAsync(async () =>
        {
            var networks = await wifi.ListAsync(cancellationToken);
            var interfaces = await wifi.ListInterfacesAsync(cancellationToken);
            Networks.Clear();
            Interfaces.Clear();
            foreach (var item in networks.Value ?? []) Networks.Add(item);
            foreach (var item in interfaces.Value ?? []) Interfaces.Add(item);
            Status = networks.Succeeded && interfaces.Succeeded ? $"Found {Networks.Count} network(s), {Interfaces.Count} interface(s)." : "WiFi discovery failed.";
        });
    }

    public async Task<OperationResult> ConnectAsync(string ssid, string? interfaceName = null, CancellationToken cancellationToken = default)
    {
        var result = await wifi.ConnectAsync(new WifiConnectionRequest(ssid, Password, interfaceName, DryRun), cancellationToken);
        Status = result.Succeeded ? $"WiFi configuration {(DryRun ? "dry-run" : "request")} completed for {ssid}." : $"WiFi configuration failed: {result.Error?.Message}";
        if (!result.Succeeded) await messages.ShowAsync(new MessageRequest("WiFi", Status, MessageSeverity.Error), cancellationToken);
        return result;
    }
}

public sealed class GrblConfigurationToolViewModel(IFileDialogService files, IMessageService messages) : ToolViewModelBase
{
    public ObservableCollection<string> Lines { get; } = [];

    public OperationResult ImportText(string text)
    {
        Lines.Clear();
        foreach (var line in text.Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0))
        {
            if (!line.StartsWith('$')) return OperationResult.Failure($"Invalid GRBL setting line: {line}");
            Lines.Add(line);
        }
        Status = $"Imported {Lines.Count} GRBL setting line(s).";
        return OperationResult.Success();
    }

    public async Task<OperationResult<IReadOnlyList<string>>> SelectImportFileAsync(CancellationToken cancellationToken = default)
    {
        var result = await files.OpenAsync(new FileDialogRequest("Import GRBL configuration", [new FileTypeFilter("GRBL configuration", [".txt", ".nc", ".gcode"])]), cancellationToken);
        Status = result.Succeeded ? "GRBL configuration import file selected." : $"GRBL configuration import failed: {result.Error?.Message}";
        if (!result.Succeeded) await messages.ShowAsync(new MessageRequest("GRBL configuration", Status, MessageSeverity.Warning), cancellationToken);
        return result;
    }
}

public sealed class ProjectFileToolViewModel(IFileDialogService files, IMessageService messages) : ToolViewModelBase
{
    public async Task<OperationResult<IReadOnlyList<string>>> OpenProjectAsync(CancellationToken cancellationToken = default)
    {
        var result = await files.OpenAsync(new FileDialogRequest("Open LaserGRBL project", [new FileTypeFilter("LaserGRBL project", [".lps"])]), cancellationToken);
        Status = result.Succeeded ? "Project file selected; compatibility import is deferred to Task 18." : $"Project open failed: {result.Error?.Message}";
        if (result.Succeeded) await messages.ShowAsync(new MessageRequest("Project import", Status, MessageSeverity.Warning), cancellationToken);
        return result;
    }

    public async Task<OperationResult<string>> SaveProjectAsync(CancellationToken cancellationToken = default)
    {
        var result = await files.SaveAsync(new FileDialogRequest("Save LaserGRBL project", [new FileTypeFilter("LaserGRBL project", [".lps"])]), cancellationToken);
        Status = result.Succeeded ? "Project save target selected; compatibility writer is deferred to Task 18." : $"Project save failed: {result.Error?.Message}";
        return result;
    }
}

public sealed class EmulatorActivityConsoleViewModel(int historyLimit = 200) : ToolViewModelBase
{
    public ObservableCollection<TransportActivity> Activity { get; } = [];

    public void Attach(EmulatorMachineTransport transport)
    {
        foreach (var item in transport.ActivityHistory) Append(item);
        transport.Activity += (_, item) => Append(item);
        Status = "Emulator activity console attached.";
    }

    public void Append(TransportActivity activity)
    {
        Activity.Add(activity);
        while (Activity.Count > historyLimit) Activity.RemoveAt(0);
        Status = $"{Activity.Count} emulator event(s).";
    }
}
