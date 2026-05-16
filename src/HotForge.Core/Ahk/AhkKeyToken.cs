using System.Text.RegularExpressions;

namespace HotForge.Core.Ahk;

internal static partial class AhkKeyToken
{
    public static string? ToCanonical(string token)
    {
        var t = token.Trim();
        if (t.Length == 1)
        {
            char c = t[0];
            if (char.IsLetter(c)) return char.ToUpperInvariant(c).ToString();
            if (char.IsDigit(c)) return "D" + c;
        }

        var fn = FunctionKey().Match(t);
        if (fn.Success)
        {
            int n = int.Parse(fn.Groups[1].Value);
            if (n is >= 1 and <= 12) return "F" + n;
        }

        return t.ToLowerInvariant() switch
        {
            "space" => "Space",
            "enter" or "return" => "Enter",
            "esc" or "escape" => "Escape",
            "tab" => "Tab",
            "bs" or "backspace" => "Backspace",
            _ => null,
        };
    }

    public static string ToAhk(string canonical)
    {
        if (canonical.Length == 1) return canonical.ToLowerInvariant();
        if (canonical.Length == 2 && canonical[0] == 'D' && char.IsDigit(canonical[1]))
            return canonical[1].ToString();
        if (canonical.Length >= 2 && canonical[0] == 'F' && char.IsDigit(canonical[1]))
            return canonical;

        return canonical switch
        {
            "Space" => "Space",
            "Enter" => "Enter",
            "Escape" => "Esc",
            "Tab" => "Tab",
            "Backspace" => "BS",
            _ => canonical,
        };
    }

    [GeneratedRegex(@"^[fF](\d{1,2})$")]
    private static partial Regex FunctionKey();
}
