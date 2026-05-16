using System.Text;
using System.Text.RegularExpressions;

namespace HotForge.Core.Ahk;

public static partial class AhkParser
{
    private const string ModifierSymbols = "#!^+<>*~$";
    private static readonly string[] RunCommands = { "run", "runwait" };
    private static readonly string[] SendCommands =
    {
        "send", "sendinput", "sendraw", "sendtext", "sendevent", "sendplay",
    };

    public static AhkProgram Parse(string text)
    {
        var lines = StripComments(text ?? string.Empty);
        var statements = new List<AhkStatement>();

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var trimmed = line.Trim();
            if (trimmed.Length == 0)
                continue;

            var hot = HotstringPattern().Match(trimmed);
            if (hot.Success)
            {
                statements.Add(new HotstringStatement(
                    hot.Groups[1].Value, hot.Groups[2].Value.Trim(), hot.Groups[3].Value));
                continue;
            }

            if (trimmed[0] == '#' && !trimmed.Contains("::", StringComparison.Ordinal))
            {
                statements.Add(new DirectiveStatement(trimmed));
                continue;
            }

            int sep = trimmed.IndexOf("::", StringComparison.Ordinal);
            if (sep < 0)
            {
                statements.Add(new UnknownStatement(trimmed));
                continue;
            }

            var left = trimmed[..sep];
            var right = trimmed[(sep + 2)..].Trim();
            var (modifiers, key) = SplitTrigger(left);

            if (key.Length == 0 || key.Contains('&'))
            {
                statements.Add(new UnknownStatement(trimmed));
                continue;
            }

            var body = new List<string>();
            if (right.Length > 0)
            {
                body.Add(right);
            }
            else
            {
                for (int j = i + 1; j < lines.Count; j++)
                {
                    var b = lines[j].Trim();
                    if (b.Length == 0) { i = j; break; }
                    if (b.Equals("return", StringComparison.OrdinalIgnoreCase)) { i = j; break; }
                    if (IsLabelLine(b)) { i = j - 1; break; }
                    body.Add(b);
                    i = j;
                }
            }

            if (modifiers.Length == 0 && body.Count == 1 && IsBareKey(body[0]))
            {
                statements.Add(new RemapStatement(key, body[0].Trim()));
                continue;
            }

            var action = body.Count > 0 ? ParseAction(body[0]) : AhkAction.None;
            statements.Add(new HotkeyStatement(modifiers, key, action, body));
        }

        return new AhkProgram(statements);
    }

    private static (string Modifiers, string Key) SplitTrigger(string spec)
    {
        var mods = new StringBuilder();
        int k = 0;
        while (k < spec.Length && ModifierSymbols.IndexOf(spec[k]) >= 0)
        {
            if (spec[k] is '#' or '!' or '^' or '+')
                mods.Append(spec[k]);
            k++;
        }
        return (mods.ToString(), spec[k..].Trim());
    }

    private static AhkAction ParseAction(string line)
    {
        var text = line.Trim();
        int space = text.IndexOfAny(new[] { ' ', '\t', ',', '(' });
        var word = (space < 0 ? text : text[..space]).Trim().ToLowerInvariant();
        var rest = StripLeadingSeparators(space < 0 ? string.Empty : text[space..]);
        rest = Unwrap(rest);

        if (Array.IndexOf(RunCommands, word) >= 0)
        {
            var target = rest.Split(',')[0].Trim();
            return new AhkAction(AhkActionKind.Run, target, string.Empty);
        }

        if (Array.IndexOf(SendCommands, word) >= 0)
            return new AhkAction(AhkActionKind.Send, rest, string.Empty);

        return new AhkAction(AhkActionKind.Unsupported, text, string.Empty);
    }

    private static bool IsBareKey(string s)
    {
        var t = s.Trim();
        if (t.Length == 0) return false;
        var lower = t.ToLowerInvariant();
        if (Array.IndexOf(RunCommands, lower) >= 0) return false;
        if (Array.IndexOf(SendCommands, lower) >= 0) return false;
        return !t.Contains(' ') && !t.Contains(',') && !t.Contains('(');
    }

    private static bool IsLabelLine(string s)
        => HotstringPattern().IsMatch(s)
           || (s.Contains("::", StringComparison.Ordinal) && !s.StartsWith(';'));

    private static string StripLeadingSeparators(string s)
    {
        int k = 0;
        while (k < s.Length && (s[k] == ',' || s[k] == ' ' || s[k] == '\t'))
            k++;
        return s[k..];
    }

    private static string Unwrap(string s)
    {
        var t = s.Trim();
        if (t.Length >= 2 && t[0] == '(' && t[^1] == ')')
            t = t[1..^1].Trim();
        if (t.Length >= 2 && t[0] == '"' && t[^1] == '"')
            t = t[1..^1];
        return t;
    }

    private static List<string> StripComments(string text)
    {
        var raw = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var result = new List<string>(raw.Length);
        bool inBlock = false;

        foreach (var original in raw)
        {
            var line = original;
            if (inBlock)
            {
                int end = line.IndexOf("*/", StringComparison.Ordinal);
                if (end < 0) { result.Add(string.Empty); continue; }
                line = line[(end + 2)..];
                inBlock = false;
            }

            var trimmedStart = line.TrimStart();
            if (trimmedStart.StartsWith("/*", StringComparison.Ordinal))
            {
                int end = line.IndexOf("*/", StringComparison.Ordinal);
                if (end < 0) { inBlock = true; result.Add(string.Empty); continue; }
                line = line[..line.IndexOf("/*", StringComparison.Ordinal)]
                       + line[(end + 2)..];
            }

            if (trimmedStart.StartsWith(';'))
            {
                result.Add(string.Empty);
                continue;
            }

            for (int k = 1; k < line.Length; k++)
            {
                if (line[k] == ';' && (line[k - 1] == ' ' || line[k - 1] == '\t'))
                {
                    line = line[..k];
                    break;
                }
            }

            result.Add(line);
        }

        return result;
    }

    [GeneratedRegex(@"^:([^:]*):(.+?)::(.*)$")]
    private static partial Regex HotstringPattern();
}
