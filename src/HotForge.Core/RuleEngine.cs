using HotForge.Core.Abstractions;
using HotForge.Core.Actions;
using HotForge.Core.Model;
using HotForge.Core.Triggers;

namespace HotForge.Core;

/// <summary>
/// OS-agnostic dispatcher: subscribes to an IInputBackend, matches each event
/// against the rule set, and runs the bound action. No platform code here.
/// </summary>
public sealed class RuleEngine
{
    private readonly IReadOnlyDictionary<string, ITrigger> _triggers;
    private readonly IReadOnlyDictionary<string, IActionExecutor> _actions;
    private readonly IReadOnlyList<AutomationRule> _rules;
    private readonly IInputBackend _backend;
    private readonly Action<string> _log;

    public RuleEngine(
        IInputBackend backend,
        IReadOnlyList<AutomationRule> rules,
        Action<string>? log = null,
        IEnumerable<ITrigger>? triggers = null,
        IEnumerable<IActionExecutor>? actions = null)
    {
        _backend = backend;
        _rules = rules;
        _log = log ?? (_ => { });
        _triggers = (triggers ?? DefaultTriggers()).ToDictionary(t => t.Kind);
        _actions = (actions ?? DefaultActions()).ToDictionary(a => a.Kind);
    }

    public static IEnumerable<ITrigger> DefaultTriggers() => new ITrigger[]
    {
        new HotkeyTrigger(),
    };

    public static IEnumerable<IActionExecutor> DefaultActions() => new IActionExecutor[]
    {
        new RunProcessAction(),
        new TypeTextAction(),
    };

    public void Start()
    {
        _backend.OnKey = OnKeyEvent;
        _backend.Start();
        _log($"engine started on {_backend.Platform} with {_rules.Count} rule(s)");
    }

    /// <summary>
    /// Handle one key event: run every matching rule's action. Returns
    /// <c>true</c> if any rule matched, signalling the backend to consume
    /// (swallow) the event so the OS never sees it. Also the test entry point
    /// for driving synthetic events without an OS.
    /// </summary>
    public bool OnKeyEvent(KeyEvent e)
    {
        var state = new TriggerState();
        var matched = false;
        foreach (var rule in _rules)
        {
            if (!_triggers.TryGetValue(rule.TriggerKind, out var trigger))
                continue;
            if (!trigger.Matches(e, rule, state))
                continue;

            matched = true;
            if (!_actions.TryGetValue(rule.ActionKind, out var action))
            {
                _log($"no action registered for kind '{rule.ActionKind}'");
                continue;
            }

            var ctx = new ActionContext { Backend = _backend, Log = _log };
            _ = action.ExecuteAsync(rule, ctx);
        }
        return matched;
    }
}
