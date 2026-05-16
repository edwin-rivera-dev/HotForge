namespace HotForge.Core.Model;

/// <summary>A normalized key event emitted by an IInputBackend.</summary>
public sealed record KeyEvent(Key Key, KeyModifiers Modifiers, bool IsKeyDown);
