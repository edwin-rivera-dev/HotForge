using System.Runtime.InteropServices;
using HotForge.Core.Abstractions;
using HotForge.Core.Model;

namespace HotForge.Windows;

/// <summary>
/// Real Windows input backend: a low-level keyboard hook (WH_KEYBOARD_LL)
/// installed via SetWindowsHookEx, plus Unicode text injection via SendInput.
/// Requires a pumped message loop on the installing thread (the App host runs
/// one). Reference backend — Linux/macOS backends mirror this contract.
/// </summary>
public sealed class WindowsInputBackend : IInputBackend
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYUP = 0x0105;

    private const int VK_SHIFT = 0x10, VK_CONTROL = 0x11, VK_MENU = 0x12;
    private const int VK_LWIN = 0x5B, VK_RWIN = 0x5C;

    private readonly LowLevelKeyboardProc _proc;
    private IntPtr _hook = IntPtr.Zero;
    private bool _started;

    public WindowsInputBackend() => _proc = HookCallback;

    public string Platform => "windows";

    public Func<KeyEvent, bool>? OnKey { get; set; }

    public void Start()
    {
        if (_started) return;
        _hook = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(null), 0);
        if (_hook == IntPtr.Zero)
            throw new InvalidOperationException(
                $"SetWindowsHookEx failed (error {Marshal.GetLastWin32Error()})");
        _started = true;
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int msg = wParam.ToInt32();
            bool down = msg is WM_KEYDOWN or WM_SYSKEYDOWN;
            bool up = msg is WM_KEYUP or WM_SYSKEYUP;
            if (down || up)
            {
                int vk = Marshal.ReadInt32(lParam);
                var key = WindowsKeyMap.FromVk(vk);
                if (key != Key.None
                    && OnKey?.Invoke(new KeyEvent(key, CurrentModifiers(), down)) == true)
                {
                    // A rule matched — swallow it so the foreground app and
                    // OS shortcuts never see this keystroke.
                    return (IntPtr)1;
                }
            }
        }

        return CallNextHookEx(_hook, nCode, wParam, lParam);
    }

    private static KeyModifiers CurrentModifiers()
    {
        var m = KeyModifiers.None;
        if (IsDown(VK_CONTROL)) m |= KeyModifiers.Ctrl;
        if (IsDown(VK_MENU)) m |= KeyModifiers.Alt;
        if (IsDown(VK_SHIFT)) m |= KeyModifiers.Shift;
        if (IsDown(VK_LWIN) || IsDown(VK_RWIN)) m |= KeyModifiers.Meta;
        return m;
    }

    private static bool IsDown(int vk) => (GetKeyState(vk) & 0x8000) != 0;

    public void InjectText(string text)
    {
        foreach (var ch in text)
        {
            SendUnicode(ch, keyUp: false);
            SendUnicode(ch, keyUp: true);
        }
    }

    private static void SendUnicode(char ch, bool keyUp)
    {
        var input = new INPUT
        {
            type = 1, // INPUT_KEYBOARD
            U = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = 0,
                    wScan = ch,
                    dwFlags = 0x0004 | (keyUp ? 0x0002u : 0u), // KEYEVENTF_UNICODE | KEYUP
                    time = 0,
                    dwExtraInfo = IntPtr.Zero,
                },
            },
        };
        SendInput(1, new[] { input }, Marshal.SizeOf<INPUT>());
    }

    public void Dispose()
    {
        if (_hook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hook);
            _hook = IntPtr.Zero;
        }
        _started = false;
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT { public uint type; public InputUnion U; }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion { [FieldOffset(0)] public KEYBDINPUT ki; }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern short GetKeyState(int nVirtKey);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);
}
