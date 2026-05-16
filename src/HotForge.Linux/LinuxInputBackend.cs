using System.Runtime.InteropServices;
using System.Text;
using HotForge.Core.Abstractions;
using HotForge.Core.Model;

namespace HotForge.Linux;

/// <summary>
/// Real Linux input backend: reads normalized key events directly from evdev
/// character devices (/dev/input/event*) and synthesizes text through a
/// /dev/uinput virtual keyboard. No X11 or Wayland dependency. Mirrors the
/// WindowsInputBackend contract; needs read access to /dev/input and write
/// access to /dev/uinput (typically the 'input' group or root).
/// </summary>
public sealed class LinuxInputBackend : IInputBackend
{
    private const int OReadOnly = 0;
    private const int OWriteOnly = 1;

    private const ushort EvSyn = 0;
    private const ushort EvKey = 1;
    private const ushort SynReport = 0;

    private const ulong UiSetEvBit = 0x40045564;
    private const ulong UiSetKeyBit = 0x40045565;
    private const ulong UiDevCreate = 0x5501;
    private const ulong UiDevDestroy = 0x5502;

    private const int InputEventSize = 24;
    private const int UinputUserDevSize = 1116;

    private readonly List<int> _deviceFds = new();
    private readonly List<Thread> _readers = new();
    private readonly HashSet<int> _heldModifierCodes = new();
    private readonly object _modLock = new();
    private int _uinputFd = -1;
    private volatile bool _running;
    private bool _started;

    public string Platform => "linux";

    public event Action<KeyEvent>? KeyEvent;

    public void Start()
    {
        if (_started) return;

        foreach (var path in DiscoverKeyboardDevices())
        {
            int fd = open(path, OReadOnly);
            if (fd < 0) continue;
            _deviceFds.Add(fd);
        }

        if (_deviceFds.Count == 0)
            throw new InvalidOperationException(
                "No readable keyboard devices under /dev/input. Run with read "
                + "access to /dev/input (add the user to the 'input' group, or "
                + "run as root).");

        _running = true;
        _started = true;

        foreach (var fd in _deviceFds)
        {
            var t = new Thread(() => ReadLoop(fd)) { IsBackground = true, Name = "hotforge-evdev" };
            _readers.Add(t);
            t.Start();
        }
    }

    private void ReadLoop(int fd)
    {
        var buf = new byte[InputEventSize];
        while (_running)
        {
            int got = read(fd, buf, (nuint)InputEventSize);
            if (got != InputEventSize) break;

            ushort type = BitConverter.ToUInt16(buf, 16);
            ushort code = BitConverter.ToUInt16(buf, 18);
            int value = BitConverter.ToInt32(buf, 20);
            if (type != EvKey) continue;

            var modifier = LinuxKeyMap.ModifierFor(code);
            if (modifier != KeyModifiers.None)
            {
                lock (_modLock)
                {
                    if (value == 0) _heldModifierCodes.Remove(code);
                    else _heldModifierCodes.Add(code);
                }
                continue;
            }

            var key = LinuxKeyMap.FromCode(code);
            if (key == Key.None) continue;

            KeyEvent?.Invoke(new KeyEvent(key, CurrentModifiers(), value != 0));
        }
    }

    private KeyModifiers CurrentModifiers()
    {
        var m = KeyModifiers.None;
        lock (_modLock)
        {
            foreach (var code in _heldModifierCodes)
                m |= LinuxKeyMap.ModifierFor(code);
        }
        return m;
    }

    public void InjectText(string text)
    {
        EnsureUinput();
        foreach (var ch in text)
        {
            if (!LinuxKeyMap.TryFromChar(ch, out int code, out bool shift))
                continue;

            if (shift) EmitKey(LinuxKeyMap.KeyLeftShift, true);
            EmitKey(code, true);
            EmitKey(code, false);
            if (shift) EmitKey(LinuxKeyMap.KeyLeftShift, false);
        }
    }

    private void EnsureUinput()
    {
        if (_uinputFd >= 0) return;

        int fd = open("/dev/uinput", OWriteOnly);
        if (fd < 0)
            throw new InvalidOperationException(
                "Cannot open /dev/uinput for text injection. Load the 'uinput' "
                + "module and grant write access (the 'input' group, or root).");

        ioctl(fd, UiSetEvBit, EvKey);
        ioctl(fd, UiSetEvBit, EvSyn);
        for (int c = 1; c < 128; c++)
            ioctl(fd, UiSetKeyBit, c);

        var dev = new byte[UinputUserDevSize];
        var name = Encoding.ASCII.GetBytes("HotForge Virtual Keyboard");
        Array.Copy(name, dev, Math.Min(name.Length, 79));
        BitConverter.GetBytes((ushort)0x03).CopyTo(dev, 80);
        BitConverter.GetBytes((ushort)1).CopyTo(dev, 82);
        BitConverter.GetBytes((ushort)1).CopyTo(dev, 84);
        BitConverter.GetBytes((ushort)1).CopyTo(dev, 86);
        WriteAll(fd, dev);

        if (ioctl(fd, UiDevCreate) < 0)
        {
            close(fd);
            throw new InvalidOperationException("UI_DEV_CREATE failed on /dev/uinput.");
        }

        Thread.Sleep(200);
        _uinputFd = fd;
    }

    private void EmitKey(int code, bool down)
    {
        WriteEvent(EvKey, (ushort)code, down ? 1 : 0);
        WriteEvent(EvSyn, SynReport, 0);
    }

    private void WriteEvent(ushort type, ushort code, int value)
    {
        var ev = new byte[InputEventSize];
        BitConverter.GetBytes(type).CopyTo(ev, 16);
        BitConverter.GetBytes(code).CopyTo(ev, 18);
        BitConverter.GetBytes(value).CopyTo(ev, 20);
        WriteAll(_uinputFd, ev);
    }

    private static IEnumerable<string> DiscoverKeyboardDevices()
    {
        const string list = "/proc/bus/input/devices";
        if (!File.Exists(list)) yield break;

        string handlers = string.Empty;
        bool isKeyboard = false;
        foreach (var line in File.ReadLines(list))
        {
            if (line.Length == 0)
            {
                if (isKeyboard)
                {
                    foreach (var tok in handlers.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                        if (tok.StartsWith("event", StringComparison.Ordinal))
                            yield return "/dev/input/" + tok;
                }
                handlers = string.Empty;
                isKeyboard = false;
                continue;
            }

            if (line.StartsWith("H: Handlers=", StringComparison.Ordinal))
            {
                handlers = line["H: Handlers=".Length..];
                if (handlers.Contains("kbd", StringComparison.Ordinal))
                    isKeyboard = true;
            }
        }
    }

    private static void WriteAll(int fd, byte[] buf)
        => write(fd, buf, (nuint)buf.Length);

    public void Dispose()
    {
        _running = false;

        if (_uinputFd >= 0)
        {
            ioctl(_uinputFd, UiDevDestroy);
            close(_uinputFd);
            _uinputFd = -1;
        }

        foreach (var fd in _deviceFds)
            close(fd);
        _deviceFds.Clear();
        _started = false;
    }

    [DllImport("libc", SetLastError = true)]
    private static extern int open(string path, int flags);

    [DllImport("libc", SetLastError = true)]
    private static extern int close(int fd);

    [DllImport("libc", SetLastError = true)]
    private static extern int read(int fd, [In, Out] byte[] buf, nuint count);

    [DllImport("libc", SetLastError = true)]
    private static extern int write(int fd, [In] byte[] buf, nuint count);

    [DllImport("libc", SetLastError = true, EntryPoint = "ioctl")]
    private static extern int ioctl(int fd, ulong request, int arg);

    [DllImport("libc", SetLastError = true, EntryPoint = "ioctl")]
    private static extern int ioctl(int fd, ulong request);
}
