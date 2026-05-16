# Writing a Trigger or an Action

Each is an independent one-PR contribution. Pick one type; don't bundle.

## A new Action (`IActionExecutor`)

1. One file in `src/HotForge.Core/Actions/<Name>Action.cs`.
2. Implement:

```csharp
public sealed class MyAction : IActionExecutor
{
    public string Kind => "my-action";          // referenced from config
    public Task ExecuteAsync(ActionContext ctx) { /* one effect */ }
}
```

3. Register it in `RuleEngine.DefaultActions`.
4. Test in `tests/HotForge.Core.Tests/` with a fake context — no OS needed.

## A new Trigger (`ITrigger`)

1. One file in `src/HotForge.Core/Triggers/<Name>Trigger.cs`.
2. Implement:

```csharp
public sealed class MyTrigger : ITrigger
{
    public string Kind => "my-trigger";
    public bool Matches(KeyEvent e, TriggerState state) { /* fire? */ }
}
```

3. Register it in `RuleEngine.DefaultTriggers`.
4. Test by feeding synthetic `KeyEvent`s through the engine with a fake backend.

## A new OS backend (`IInputBackend`)

Bigger, but still self-contained. New project `src/HotForge.<Os>/`,
implement `IInputBackend` (emit normalized `KeyEvent`s, support
`InjectText`/`InjectChord`), wire it in `HotForge.App` behind an OS check.
Never touch the engine, triggers, or actions.

## Acceptance checklist (put in every PR)

- [ ] One trigger / one action / one backend — nothing else
- [ ] Registered in the relevant `RuleEngine.Default*`
- [ ] Engine-level test with a fake backend (OS backends: a manual test note)
- [ ] No platform code outside a per-OS backend
- [ ] `dotnet test` green
