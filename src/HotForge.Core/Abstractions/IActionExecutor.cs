using HotForge.Core.Model;

namespace HotForge.Core.Abstractions;

/// <summary>Performs one effect when a rule fires. One kind per PR.</summary>
public interface IActionExecutor
{
    /// <summary>Config key matched against <see cref="AutomationRule.ActionKind"/>.</summary>
    string Kind { get; }

    Task ExecuteAsync(AutomationRule rule, ActionContext ctx);
}
