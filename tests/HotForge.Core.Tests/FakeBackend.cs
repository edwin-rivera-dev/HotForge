using HotForge.Core.Abstractions;
using HotForge.Core.Model;

namespace HotForge.Core.Tests;

/// <summary>An IInputBackend with no OS dependency so the engine is unit-testable in CI.</summary>
internal sealed class FakeBackend : IInputBackend
{
    public string Platform => "fake";
    public List<string> Injected { get; } = new();

    public Func<KeyEvent, bool>? OnKey { get; set; }

    public void Start() { }

    /// <summary>Drive a synthetic event; returns true if a rule consumed it.</summary>
    public bool Emit(KeyEvent e) => OnKey?.Invoke(e) ?? false;

    public void InjectText(string text) => Injected.Add(text);

    public void Dispose() { }
}
