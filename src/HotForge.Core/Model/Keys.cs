namespace HotForge.Core.Model;

/// <summary>Normalized modifier set, OS-independent. Backends map native flags to these.</summary>
[Flags]
public enum KeyModifiers
{
    None = 0,
    Ctrl = 1,
    Alt = 2,
    Shift = 4,
    Meta = 8,
}

/// <summary>
/// Canonical key identifiers. Deliberately a common subset — extending this
/// (and the per-OS maps) is a tracked, one-PR contribution.
/// </summary>
public enum Key
{
    None,
    A, B, C, D, E, F, G, H, I, J, K, L, M,
    N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
    D0, D1, D2, D3, D4, D5, D6, D7, D8, D9,
    F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,
    Space, Enter, Escape, Tab, Backspace,
}
