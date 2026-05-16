using HotForge.Core.Abstractions;
using HotForge.Core.Model;

namespace HotForge.Core.Triggers;

/// <summary>
/// Fires when the pressed key event matches a chord. Config:
/// <c>{ "kind": "hotkey", "args": { "chord": "Ctrl+Alt+H" } }</c>.
/// Reference trigger — copy this shape for new trigger kinds.
/// </summary>
public sealed class HotkeyTrigger : ITrigger
{
    public string Kind => "hotkey";

    public bool Matches(KeyEvent e, AutomationRule rule, TriggerState state)
    {
        if (!rule.TriggerArgs.TryGetValue("chord", out var chordText))
            return false;

        KeyChord chord;
        try
        {
            chord = KeyChord.Parse(chordText);
        }
        catch (FormatException)
        {
            return false;
        }

        return chord.Matches(e);
    }
}
