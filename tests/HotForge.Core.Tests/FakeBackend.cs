using HotForge.Core.Abstractions;
using HotForge.Core.Model;

namespace HotForge.Core.Tests;

/// <summary>An IInputBackend with no OS dependency so the engine is unit-testable in CI.</summary>
internal sealed class FakeBackend : IInputBackend
{
    public string Platform => "fake";
    public List<string> Injected { get; } = new();

    public event Action<KeyEvent>? KeyEvent;

    public void Start() { }

    public void Emit(KeyEvent e) => KeyEvent?.Invoke(e);

    public void InjectText(string text) => Injected.Add(text);

    public void Dispose() { }
}
