using System.Diagnostics;
using HotForge.Core.Abstractions;
using HotForge.Core.Model;

namespace HotForge.Core.Actions;

/// <summary>
/// Launches a process. Config:
/// <c>{ "kind": "run", "args": { "path": "notepad", "args": "" } }</c>.
/// </summary>
public sealed class RunProcessAction : IActionExecutor
{
    public string Kind => "run";

    public Task ExecuteAsync(AutomationRule rule, ActionContext ctx)
    {
        if (!rule.ActionArgs.TryGetValue("path", out var path) || string.IsNullOrWhiteSpace(path))
        {
            ctx.Log("run: missing 'path' arg");
            return Task.CompletedTask;
        }

        rule.ActionArgs.TryGetValue("args", out var args);

        var psi = new ProcessStartInfo
        {
            FileName = path,
            Arguments = args ?? string.Empty,
            UseShellExecute = true,
        };

        try
        {
            Process.Start(psi);
            ctx.Log($"run: started '{path}'");
        }
        catch (Exception ex)
        {
            ctx.Log($"run: failed to start '{path}': {ex.Message}");
        }

        return Task.CompletedTask;
    }
}
