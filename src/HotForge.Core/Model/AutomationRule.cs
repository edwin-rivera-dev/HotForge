namespace HotForge.Core.Model;

/// <summary>
/// One rule: a trigger (by kind + args) bound to an action (by kind + args).
/// Generic arg dictionaries keep new trigger/action types from changing this
/// contract — that is what makes each new type a one-PR change.
/// </summary>
public sealed record AutomationRule(
    string TriggerKind,
    IReadOnlyDictionary<string, string> TriggerArgs,
    string ActionKind,
    IReadOnlyDictionary<string, string> ActionArgs);
