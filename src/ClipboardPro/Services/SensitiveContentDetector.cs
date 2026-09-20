using System.Text.RegularExpressions;

namespace ClipboardPro.Services;

public static partial class SensitiveContentDetector
{
    [GeneratedRegex("(?:sk-|ghp_|xox[baprs]-|AKIA)[A-Za-z0-9_\\-]{16,}", RegexOptions.IgnoreCase)] private static partial Regex Token();
    [GeneratedRegex("(?:password|contraseña|secret|api[_ -]?key)\\s*[:=]", RegexOptions.IgnoreCase)] private static partial Regex Label();
    [GeneratedRegex("\\b(?:\\d[ -]?){13,19}\\b")] private static partial Regex Card();
    public static bool LooksSensitive(string value) => value.Length > 0 && (Token().IsMatch(value) || Label().IsMatch(value) || Card().IsMatch(value));
}
