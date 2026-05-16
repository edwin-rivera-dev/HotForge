using HotForge.Core.Model;

namespace HotForge.Windows;

/// <summary>
/// Maps Win32 virtual-key codes to canonical <see cref="Key"/>. Intentionally
/// covers the common subset; widening it (and the canonical enum) is a tracked
/// one-PR contribution.
/// </summary>
internal static class WindowsKeyMap
{
    public static Key FromVk(int vk) => vk switch
    {
        >= 0x41 and <= 0x5A => (Key)((int)Key.A + (vk - 0x41)),   // A-Z
        >= 0x30 and <= 0x39 => (Key)((int)Key.D0 + (vk - 0x30)),  // 0-9
        >= 0x70 and <= 0x7B => (Key)((int)Key.F1 + (vk - 0x70)),  // F1-F12
        0x20 => Key.Space,
        0x0D => Key.Enter,
        0x1B => Key.Escape,
        0x09 => Key.Tab,
        0x08 => Key.Backspace,
        _ => Key.None,
    };
}
