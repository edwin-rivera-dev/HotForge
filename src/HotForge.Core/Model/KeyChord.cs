namespace HotForge.Core.Model;

/// <summary>A modifier+key combination, e.g. <c>Ctrl+Alt+H</c>.</summary>
public sealed record KeyChord(KeyModifiers Modifiers, Key Key)
{
    /// <summary>Parse "Ctrl+Alt+H". Throws FormatException on an unknown token.</summary>
    public static KeyChord Parse(string text)
    {
        var mods = KeyModifiers.None;
        Key key = Key.None;

        foreach (var raw in text.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            switch (raw.ToLowerInvariant())
            {
                case "ctrl" or "control": mods |= KeyModifiers.Ctrl; break;
                case "alt": mods |= KeyModifiers.Alt; break;
                case "shift": mods |= KeyModifiers.Shift; break;
                case "meta" or "win" or "cmd": mods |= KeyModifiers.Meta; break;
                default:
                    if (!Enum.TryParse<Key>(raw, ignoreCase: true, out key) || key == Key.None)
                        throw new FormatException($"Unknown key token '{raw}' in chord '{text}'");
                    break;
            }
        }

        if (key == Key.None)
            throw new FormatException($"Chord '{text}' has no non-modifier key");

        return new KeyChord(mods, key);
    }

    public bool Matches(KeyEvent e) => e.IsKeyDown && e.Key == Key && e.Modifiers == Modifiers;
}
