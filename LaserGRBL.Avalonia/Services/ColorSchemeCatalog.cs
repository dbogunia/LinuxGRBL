using Avalonia.Media;

namespace LaserGRBL.Avalonia.Services;

public sealed class ColorSchemeCatalog
{
    private readonly Dictionary<string, SemanticColorScheme> schemes;

    public ColorSchemeCatalog(IEnumerable<SemanticColorScheme> schemes) => this.schemes = schemes.ToDictionary(scheme => scheme.Name, StringComparer.OrdinalIgnoreCase);

    public static ColorSchemeCatalog Default { get; } = new([
        new SemanticColorScheme("Default", Brush("#F5F7FA"), Brush("#FFFFFF"), Brush("#EEF2F6"), Brush("#1F2937"), Brush("#64748B"), Brush("#CBD5E1"), Brush("#0F172A"), Brush("#111827"), Brush("#38BDF8"), Brush("#22C55E"), Brush("#EF4444"), Brush("#2563EB"), Brush("#CBD5E1"), Brush("#94A3B8")),
        new SemanticColorScheme("Safety Glasses Green", Brush("#F3FAF4"), Brush("#FFFFFF"), Brush("#E6F4EA"), Brush("#14351F"), Brush("#4B6B55"), Brush("#B7D7C0"), Brush("#102A18"), Brush("#0B1F12"), Brush("#16A34A"), Brush("#22C55E"), Brush("#DC2626"), Brush("#047857"), Brush("#B7D7C0"), Brush("#86A891")),
        new SemanticColorScheme("Safety Glasses Amber", Brush("#FFF8ED"), Brush("#FFFFFF"), Brush("#F7E6C8"), Brush("#3D2E15"), Brush("#7A6238"), Brush("#E1C99E"), Brush("#241A0B"), Brush("#1F1608"), Brush("#D97706"), Brush("#16A34A"), Brush("#B91C1C"), Brush("#0F766E"), Brush("#E1C99E"), Brush("#B99A65"))
    ]);

    public IReadOnlyList<string> Names => schemes.Keys.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray();

    public SemanticColorScheme Get(string name) => schemes.TryGetValue(name, out var scheme) ? scheme : schemes["Default"];

    private static ISolidColorBrush Brush(string hex) => new SolidColorBrush(Color.Parse(hex));
}

public sealed record SemanticColorScheme(
    string Name,
    ISolidColorBrush WindowBackground,
    ISolidColorBrush SurfaceBackground,
    ISolidColorBrush PanelBackground,
    ISolidColorBrush Text,
    ISolidColorBrush MutedText,
    ISolidColorBrush Border,
    ISolidColorBrush HeaderBackground,
    ISolidColorBrush LogBackground,
    ISolidColorBrush PreviewPath,
    ISolidColorBrush Command,
    ISolidColorBrush Warning,
    ISolidColorBrush Link,
    ISolidColorBrush Disabled,
    ISolidColorBrush PreviewBackground)
{
    public ISolidColorBrush LogText => Text;
}
