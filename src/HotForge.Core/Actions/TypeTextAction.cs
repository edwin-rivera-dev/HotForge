using HotForge.Core.Abstractions;
using HotForge.Core.Model;

namespace HotForge.Core.Actions;

/// <summary>
/// Types literal text into the focused window via the active input backend.
/// Config: <c>{ "kind": "type", "args": { "text": "hello" } }</c>.
/// </summary>
public sealed class TypeTextAction : IActionExecutor
{
    public string Kind => "type";

    public Task ExecuteAsync(AutomationRule rule, ActionContext ctx)
    {
        if (!rule.ActionArgs.TryGetValue("text", out var text))
        {
            ctx.Log("type: missing 'text' arg");
            return Task.CompletedTask;
        }

        ctx.Backend.InjectText(text);
        ctx.Log($"type: injected {text.Length} chars");
        return Task.CompletedTask;
    }
}
