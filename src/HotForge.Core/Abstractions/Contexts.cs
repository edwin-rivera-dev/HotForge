namespace HotForge.Core.Abstractions;

/// <summary>Mutable per-engine state triggers may consult (e.g. recent keys).</summary>
public sealed class TriggerState
{
    public DateTimeOffset Now { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>What an action is given to do its work.</summary>
public sealed class ActionContext
{
    public required IInputBackend Backend { get; init; }
    public required Action<string> Log { get; init; }
}
