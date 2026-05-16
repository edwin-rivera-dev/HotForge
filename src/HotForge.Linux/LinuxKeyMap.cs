using HotForge.Core.Model;

namespace HotForge.Linux;

internal static class LinuxKeyMap
{
    public const int KeyEsc = 1;
    public const int KeyBackspace = 14;
    public const int KeyTab = 15;
    public const int KeyEnter = 28;
    public const int KeyLeftCtrl = 29;
    public const int KeyLeftShift = 42;
    public const int KeyRightShift = 54;
    public const int KeyLeftAlt = 56;
    public const int KeySpace = 57;
    public const int KeyRightCtrl = 97;
    public const int KeyRightAlt = 100;
    public const int KeyLeftMeta = 125;
    public const int KeyRightMeta = 126;

    private static readonly IReadOnlyDictionary<int, Key> Letters = new Dictionary<int, Key>
    {
        [16] = Key.Q,
        [17] = Key.W,
        [18] = Key.E,
        [19] = Key.R,
        [20] = Key.T,
        [21] = Key.Y,
        [22] = Key.U,
        [23] = Key.I,
        [24] = Key.O,
        [25] = Key.P,
        [30] = Key.A,
        [31] = Key.S,
        [32] = Key.D,
        [33] = Key.F,
        [34] = Key.G,
        [35] = Key.H,
        [36] = Key.J,
        [37] = Key.K,
        [38] = Key.L,
        [44] = Key.Z,
        [45] = Key.X,
        [46] = Key.C,
        [47] = Key.V,
        [48] = Key.B,
        [49] = Key.N,
        [50] = Key.M,
    };

    public static Key FromCode(int code)
    {
        if (Letters.TryGetValue(code, out var letter)) return letter;
        if (code >= 2 && code <= 10) return (Key)((int)Key.D1 + (code - 2));
        if (code == 11) return Key.D0;
        if (code >= 59 && code <= 68) return (Key)((int)Key.F1 + (code - 59));
        if (code == 87) return Key.F11;
        if (code == 88) return Key.F12;
        return code switch
        {
            KeySpace => Key.Space,
            KeyEnter => Key.Enter,
            KeyEsc => Key.Escape,
            KeyTab => Key.Tab,
            KeyBackspace => Key.Backspace,
            _ => Key.None,
        };
    }

    public static KeyModifiers ModifierFor(int code) => code switch
    {
        KeyLeftCtrl or KeyRightCtrl => KeyModifiers.Ctrl,
        KeyLeftAlt or KeyRightAlt => KeyModifiers.Alt,
        KeyLeftShift or KeyRightShift => KeyModifiers.Shift,
        KeyLeftMeta or KeyRightMeta => KeyModifiers.Meta,
        _ => KeyModifiers.None,
    };

    public static bool TryFromChar(char ch, out int code, out bool shift)
    {
        shift = false;
        if (ch >= 'a' && ch <= 'z')
        {
            code = LetterCode(ch);
            return code != 0;
        }
        if (ch >= 'A' && ch <= 'Z')
        {
            shift = true;
            code = LetterCode(char.ToLowerInvariant(ch));
            return code != 0;
        }
        if (ch >= '1' && ch <= '9')
        {
            code = 2 + (ch - '1');
            return true;
        }

        switch (ch)
        {
            case '0': code = 11; return true;
            case ' ': code = KeySpace; return true;
            case '\n': code = KeyEnter; return true;
            case '\t': code = KeyTab; return true;
            default: code = 0; return false;
        }
    }

    private static int LetterCode(char lower)
    {
        foreach (var pair in Letters)
        {
            if (char.ToLowerInvariant(pair.Value.ToString()[0]) == lower)
                return pair.Key;
        }
        return 0;
    }
}
