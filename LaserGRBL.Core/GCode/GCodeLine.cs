using System.Globalization;
using System.Text.RegularExpressions;

namespace LaserGRBL.Core.GCode;

public sealed partial record GCodeLine(string Text, IReadOnlyDictionary<char, decimal> Words)
{
    public bool IsEmpty => Words.Count == 0 && string.IsNullOrWhiteSpace(Text);

    public static GCodeLine Parse(string text)
    {
        var clean = CommentPattern().Replace(text, string.Empty).Trim();
        var words = new Dictionary<char, decimal>();
        foreach (Match match in WordPattern().Matches(clean))
        {
            if (decimal.TryParse(match.Groups["value"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                words[char.ToUpperInvariant(match.Groups["letter"].Value[0])] = value;
        }

        return new(text, words);
    }

    [GeneratedRegex("\\([^)]*\\)|;.*$", RegexOptions.Multiline)]
    private static partial Regex CommentPattern();

    [GeneratedRegex("(?<letter>[A-Za-z])\\s*(?<value>[+-]?(?:\\d+(?:\\.\\d*)?|\\.\\d+))")]
    private static partial Regex WordPattern();
}
