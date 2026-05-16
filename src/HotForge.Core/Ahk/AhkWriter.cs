using System.Text;
using HotForge.Core.Model;

namespace HotForge.Core.Ahk;

public static class AhkWriter
{
    public static string Write(IEnumerable<AutomationRule> rules)
    {
        var sb = new StringBuilder();
        sb.AppendLine("; HotForge script");
        sb.AppendLine();
        foreach (var rule in rules)
        {
            var line = WriteRule(rule);
            if (line.Length > 0)
                sb.AppendLine(line);
        }
        return sb.ToString();
    }

    public static string WriteRule(AutomationRule rule)
    {
        if (rule.TriggerKind != "hotkey"
            || !rule.TriggerArgs.TryGetValue("chord", out var chord))
            return string.Empty;

        var prefix = ChordToAhk(chord);
        if (prefix is null)
            return string.Empty;

        return rule.ActionKind switch
        {
            "run" => $"{prefix}::Run, {RunTarget(rule)}",
            "type" => $"{prefix}::Send, {rule.ActionArgs.GetValueOrDefault("text", string.Empty)}",
            _ => string.Empty,
        };
    }

    private static string RunTarget(AutomationRule rule)
    {
        var path = rule.ActionArgs.GetValueOrDefault("path", string.Empty);
        var args = rule.ActionArgs.GetValueOrDefault("args", string.Empty);
        return string.IsNullOrWhiteSpace(args) ? path : $"{path} {args}";
    }

    private static string? ChordToAhk(string chord)
    {
        var tokens = chord.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length == 0)
            return null;

        var key = tokens[^1];
        var symbols = new StringBuilder();
        for (int i = 0; i < tokens.Length - 1; i++)
        {
            switch (tokens[i].ToLowerInvariant())
            {
                case "ctrl" or "control": symbols.Append('^'); break;
                case "alt": symbols.Append('!'); break;
                case "shift": symbols.Append('+'); break;
                case "win" or "meta" or "cmd": symbols.Append('#'); break;
            }
        }

        return symbols + AhkKeyToken.ToAhk(key);
    }
}
