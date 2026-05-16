using HotForge.Core;
using HotForge.Core.Model;
using Xunit;

namespace HotForge.Core.Tests;

public sealed class RuleEngineTests
{
    private static AutomationRule TypeRule(string chord, string text) => new(
        "hotkey",
        new Dictionary<string, string> { ["chord"] = chord },
        "type",
        new Dictionary<string, string> { ["text"] = text });

    [Fact]
    public void Matching_chord_runs_the_bound_action()
    {
        var backend = new FakeBackend();
        var engine = new RuleEngine(backend, new[] { TypeRule("Ctrl+Alt+T", "hi") });
        engine.Start();

        backend.Emit(new KeyEvent(Key.T, KeyModifiers.Ctrl | KeyModifiers.Alt, IsKeyDown: true));

        Assert.Single(backend.Injected);
        Assert.Equal("hi", backend.Injected[0]);
    }

    [Fact]
    public void Wrong_modifiers_do_not_fire()
    {
        var backend = new FakeBackend();
        var engine = new RuleEngine(backend, new[] { TypeRule("Ctrl+Alt+T", "hi") });
        engine.Start();

        backend.Emit(new KeyEvent(Key.T, KeyModifiers.Ctrl, IsKeyDown: true));

        Assert.Empty(backend.Injected);
    }

    [Fact]
    public void Key_up_does_not_fire()
    {
        var backend = new FakeBackend();
        var engine = new RuleEngine(backend, new[] { TypeRule("Ctrl+Alt+T", "hi") });
        engine.Start();

        backend.Emit(new KeyEvent(Key.T, KeyModifiers.Ctrl | KeyModifiers.Alt, IsKeyDown: false));

        Assert.Empty(backend.Injected);
    }

    [Fact]
    public void KeyChord_parse_rejects_modifier_only()
    {
        Assert.Throws<FormatException>(() => KeyChord.Parse("Ctrl+Alt"));
    }
}
