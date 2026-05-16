using HotForge.Core.Model;

namespace HotForge.Core.Abstractions;

/// <summary>
/// Per-OS input layer. Emits normalized key events and can synthesize input.
/// The only place native interop is allowed. One implementation per OS.
/// </summary>
public interface IInputBackend : IDisposable
{
    string Platform { get; }

    /// <summary>Raised for every observed key event after OS→canonical mapping.</summary>
    event Action<KeyEvent>? KeyEvent;

    /// <summary>Begin capturing input (installs the OS hook). Idempotent.</summary>
    void Start();

    /// <summary>Type literal text into the focused application.</summary>
    void InjectText(string text);
}
