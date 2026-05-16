using System.Runtime.InteropServices;
using HotForge.App;
using HotForge.Core;
using HotForge.Core.Abstractions;
using HotForge.Windows;

var configPath = args.Length > 0 ? args[0] : "config.sample.json";
if (!File.Exists(configPath))
{
    Console.Error.WriteLine($"config not found: {configPath}");
    return 66;
}

var rules = ConfigLoader.Load(configPath);

IInputBackend backend = OperatingSystem.IsWindows()
    ? new WindowsInputBackend()
    : throw new PlatformNotSupportedException(
        "Only the Windows backend is implemented. Linux/macOS backends are the "
        + "contribution surface — see docs/BACKLOG.md.");

var engine = new RuleEngine(backend, rules, log: Console.WriteLine);
engine.Start();

Console.WriteLine("HotForge running. Press Ctrl+C to quit.");
MessagePump.Run();
backend.Dispose();
return 0;

namespace HotForge.App
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct MSG
    {
        public IntPtr Hwnd;
        public uint Message;
        public IntPtr WParam;
        public IntPtr LParam;
        public uint Time;
        public int PtX;
        public int PtY;
    }

    // The low-level keyboard hook only fires while the installing thread pumps
    // the Win32 message queue. Replacing this loop with a tray/Avalonia host is
    // a tracked task in docs/BACKLOG.md.
    internal static class MessagePump
    {
        public static void Run()
        {
            while (GetMessage(out var msg, IntPtr.Zero, 0, 0) > 0)
            {
                TranslateMessage(ref msg);
                DispatchMessage(ref msg);
            }
        }

        [DllImport("user32.dll")]
        private static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint min, uint max);

        [DllImport("user32.dll")]
        private static extern bool TranslateMessage(ref MSG lpMsg);

        [DllImport("user32.dll")]
        private static extern IntPtr DispatchMessage(ref MSG lpMsg);
    }
}
