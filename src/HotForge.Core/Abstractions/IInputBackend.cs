using HotForge.Core.Model;

namespace HotForge.Core.Abstractions;

/// <summary>
/// Per-OS input layer. Emits normalized key events and can synthesize input.
/// The only place native interop is allowed. One implementation per OS.
/// </summary>
public interface IInputBackend : IDisposable
{
    string Platform { get; }

    /// <summary>
    /// Invoked for every observed key event after OS→canonical mapping.
    /// Return <c>true</c> to consume the event so the OS and other apps never
    /// see it (AutoHotkey-style suppression). Backends that cannot suppress
    /// may ignore the return value.
    /// </summary>
    Func<KeyEvent, bool>? OnKey { get; set; }

    /// <summary>Begin capturing input (installs the OS hook). Idempotent.</summary>
    void Start();

    /// <summary>Type literal text into the focused application.</summary>
    void InjectText(string text);
}
