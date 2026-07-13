using System.Globalization;

namespace LaserGRBL.Avalonia.Services;

public sealed class LocalizationCatalog
{
    private readonly Dictionary<string, Dictionary<string, string>> resources;
    private readonly List<string> missingKeys = [];
    private readonly string defaultCulture;

    public LocalizationCatalog(IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> resources, string? defaultCulture = null)
    {
        this.resources = resources.ToDictionary(
            pair => NormalizeCulture(pair.Key),
            pair => new Dictionary<string, string>(pair.Value, StringComparer.Ordinal),
            StringComparer.OrdinalIgnoreCase);
        this.defaultCulture = NormalizeCulture(defaultCulture);
    }

    public static LocalizationCatalog Default { get; } = new(new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
    {
        ["en"] = new Dictionary<string, string>
        {
            ["App.Title"] = "LaserGRBL",
            ["Status.Disconnected"] = "Disconnected",
            ["Firmware.NotSelected"] = "Firmware: not selected",
            ["Shell.WorkflowDeferred"] = "Main workflow, dialogs, and preview renderer are implemented in later tasks.",
            ["Shell.Initialized"] = "Application shell initialized.",
            ["Shell.Started"] = "Avalonia shell started.",
            ["Shell.ServicesRegistered"] = "Core and Linux platform services registered.",
            ["Shell.ConnectionSummary"] = "No machine connected",
            ["Shell.DeviceAccessSummary"] = "Serial, TCP, WebSocket, emulator, process, and WiFi services are registered for future workflow screens.",
            ["Diagnostics.SleepInhibitionUnavailable"] = "Sleep inhibition is unavailable in this shell build; active-job integration starts in later tasks.",
            ["Diagnostics.SecretStoreUnavailable"] = "Secure secret storage is unavailable in this shell build; credentials must be re-entered when feature UI arrives.",
            ["Safety.StartJob"] = "Review laser safety and legal warnings before starting a job.",
            ["Safety.FirmwareFlash"] = "Firmware flashing can leave a controller unusable; confirm the warning before continuing.",
            ["Safety.Reset"] = "Soft reset can interrupt motion; confirm current machine state before reset.",
            ["Safety.Abort"] = "Abort sends a best-effort laser-off command; verify the machine is safe.",
            ["Safety.LaserTest"] = "Laser test commands require explicit per-operation acknowledgement.",
            ["Tool.Settings.Name"] = "Settings",
            ["Tool.Settings.Description"] = "Port, firmware, streaming, theme and recent files.",
            ["Tool.CustomButtons.Name"] = "Custom buttons",
            ["Tool.CustomButtons.Description"] = "Add/edit/delete/import/export command buttons.",
            ["Tool.Hotkeys.Name"] = "Hotkeys",
            ["Tool.Hotkeys.Description"] = "Assign shortcuts and detect conflicts.",
            ["Tool.Materials.Name"] = "Materials",
            ["Tool.Materials.Description"] = "Local material database editing scaffold.",
            ["Tool.Import.Name"] = "Import",
            ["Tool.Import.Description"] = "Raster/SVG/project option routing through file dialogs.",
            ["Tool.Firmware.Name"] = "Firmware",
            ["Tool.Firmware.Description"] = "Dry-run and real firmware flash initiation.",
            ["Tool.Wifi.Name"] = "WiFi",
            ["Tool.Wifi.Description"] = "Discovery, interface listing and explicit configuration.",
            ["Tool.GrblConfig.Name"] = "GRBL config",
            ["Tool.GrblConfig.Description"] = "Import/export settings text and validation errors.",
            ["Tool.Emulator.Name"] = "Emulator",
            ["Tool.Emulator.Description"] = "Read-only bounded emulator activity console."
        },
        ["pl-PL"] = new Dictionary<string, string>
        {
            ["App.Title"] = "LaserGRBL",
            ["Status.Disconnected"] = "Rozłączony",
            ["Firmware.NotSelected"] = "Firmware: niewybrane",
            ["Shell.WorkflowDeferred"] = "Główny workflow, narzędzia i podgląd są portowane etapami.",
            ["Shell.Initialized"] = "Powłoka aplikacji zainicjalizowana.",
            ["Shell.Started"] = "Powłoka Avalonia uruchomiona.",
            ["Shell.ServicesRegistered"] = "Usługi Core i Linux platform zostały zarejestrowane.",
            ["Shell.ConnectionSummary"] = "Brak połączenia z maszyną",
            ["Shell.DeviceAccessSummary"] = "Usługi serial, TCP, WebSocket, emulator, procesów i WiFi są zarejestrowane dla przyszłych ekranów workflow.",
            ["Diagnostics.SleepInhibitionUnavailable"] = "Blokada uśpienia jest niedostępna w tej kompilacji powłoki; integracja aktywnego zadania jest w późniejszych etapach.",
            ["Diagnostics.SecretStoreUnavailable"] = "Bezpieczny magazyn sekretów jest niedostępny w tej kompilacji; dane trzeba będzie wpisać ponownie w UI funkcji.",
            ["Safety.StartJob"] = "Potwierdź ostrzeżenia bezpieczeństwa i prawne przed uruchomieniem zadania.",
            ["Safety.FirmwareFlash"] = "Flashowanie firmware może unieruchomić kontroler; potwierdź ostrzeżenie przed kontynuacją.",
            ["Safety.Reset"] = "Soft reset może przerwać ruch; potwierdź stan maszyny przed resetem.",
            ["Safety.Abort"] = "Abort wysyła najlepszą możliwą komendę wyłączenia lasera; sprawdź, czy maszyna jest bezpieczna.",
            ["Safety.LaserTest"] = "Komendy testu lasera wymagają osobnego potwierdzenia dla operacji.",
            ["Tool.Settings.Name"] = "Ustawienia",
            ["Tool.Settings.Description"] = "Port, firmware, streaming, motyw i ostatnie pliki."
        }
    });

    public IReadOnlyList<string> SupportedCultures => resources.Keys.OrderBy(culture => culture, StringComparer.OrdinalIgnoreCase).ToArray();

    public IReadOnlyList<string> MissingKeys => missingKeys;

    private LocalizationCatalog(Dictionary<string, Dictionary<string, string>> resources, List<string> missingKeys, string? defaultCulture)
    {
        this.resources = resources;
        this.missingKeys = missingKeys;
        this.defaultCulture = NormalizeCulture(defaultCulture);
    }

    public string Get(string key) => Get(key, defaultCulture);

    public string Get(string key, string? cultureName)
    {
        foreach (var culture in FallbackCultures(cultureName))
        {
            if (resources.TryGetValue(culture, out var strings) && strings.TryGetValue(key, out var value))
                return value;
        }

        if (!missingKeys.Contains(key, StringComparer.Ordinal)) missingKeys.Add(key);
        return key;
    }

    public LocalizationCatalog ForCulture(string? cultureName) => new(resources, missingKeys, cultureName);

    public static string NormalizeCulture(string? cultureName)
    {
        if (string.IsNullOrWhiteSpace(cultureName)) return "en";
        try
        {
            return CultureInfo.GetCultureInfo(cultureName).Name is { Length: > 0 } normalized ? normalized : "en";
        }
        catch (CultureNotFoundException)
        {
            return "en";
        }
    }

    private IEnumerable<string> FallbackCultures(string? cultureName)
    {
        var normalized = NormalizeCulture(cultureName);
        yield return normalized;

        var neutral = normalized.Split('-', 2)[0];
        if (!neutral.Equals(normalized, StringComparison.OrdinalIgnoreCase)) yield return neutral;
        if (!normalized.Equals("en", StringComparison.OrdinalIgnoreCase)) yield return "en";
    }
}
