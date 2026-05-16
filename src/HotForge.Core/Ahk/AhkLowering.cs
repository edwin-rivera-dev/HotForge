using HotForge.Core.Model;

namespace HotForge.Core.Ahk;

public static class AhkLowering
{
    public static IReadOnlyList<AutomationRule> Lower(AhkProgram program)
    {
        var rules = new List<AutomationRule>();

        foreach (var statement in program.Statements)
        {
            switch (statement)
            {
                case HotkeyStatement h:
                    if (TryHotkey(h, out var hotkeyRule))
                        rules.Add(hotkeyRule);
                    break;
                case RemapStatement r:
                    if (TryRemap(r, out var remapRule))
                        rules.Add(remapRule);
                    break;
            }
        }

        return rules;
    }

    private static bool TryHotkey(HotkeyStatement h, out AutomationRule rule)
    {
        rule = default!;
        var chord = BuildChord(h.Modifiers, h.Key);
        if (chord is null)
            return false;

        switch (h.Action.Kind)
        {
            case AhkActionKind.Run:
                var (path, args) = SplitCommand(h.Action.Primary);
                rule = new AutomationRule(
                    "hotkey",
                    new Dictionary<string, string> { ["chord"] = chord },
                    "run",
                    new Dictionary<string, string> { ["path"] = path, ["args"] = args });
                return true;

            case AhkActionKind.Send:
                rule = new AutomationRule(
                    "hotkey",
                    new Dictionary<string, string> { ["chord"] = chord },
                    "type",
                    new Dictionary<string, string> { ["text"] = h.Action.Primary });
                return true;

            default:
                return false;
        }
    }

    private static bool TryRemap(RemapStatement r, out AutomationRule rule)
    {
        rule = default!;
        var chord = BuildChord(string.Empty, r.FromKey);
        if (chord is null || r.ToKey.Trim().Length == 0)
            return false;

        rule = new AutomationRule(
            "hotkey",
            new Dictionary<string, string> { ["chord"] = chord },
            "type",
            new Dictionary<string, string> { ["text"] = r.ToKey.Trim() });
        return true;
    }

    private static string? BuildChord(string modifierSymbols, string keyToken)
    {
        var canonical = AhkKeyToken.ToCanonical(keyToken);
        if (canonical is null)
            return null;

        var parts = new List<string>();
        if (modifierSymbols.Contains('^')) parts.Add("Ctrl");
        if (modifierSymbols.Contains('!')) parts.Add("Alt");
        if (modifierSymbols.Contains('+')) parts.Add("Shift");
        if (modifierSymbols.Contains('#')) parts.Add("Win");
        parts.Add(canonical);
        return string.Join("+", parts);
    }

    private static (string Path, string Args) SplitCommand(string target)
    {
        var t = target.Trim();
        if (t.Length == 0)
            return (string.Empty, string.Empty);

        int space = t.IndexOf(' ');
        return space < 0
            ? (t, string.Empty)
            : (t[..space], t[(space + 1)..].Trim());
    }
}
