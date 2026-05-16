using HotForge.Core.Model;

namespace HotForge.Core.Abstractions;

/// <summary>
/// Decides whether a rule fires for a given key event. Stateless across rules;
/// any cross-event state lives in <see cref="TriggerState"/>. One kind per PR.
/// </summary>
public interface ITrigger
{
    /// <summary>Config key matched against <see cref="AutomationRule.TriggerKind"/>.</summary>
    string Kind { get; }

    bool Matches(KeyEvent e, AutomationRule rule, TriggerState state);
}
