# Architecture

HotForge is layered so the cross-platform, multi-trigger, multi-action surface
decomposes into independent one-PR units.

```
        ┌────────────────────────────────────────────┐
HotForge.App   │  Host: load config, wire backend+engine,    │
               │  run the OS message loop. (tray/Avalonia = backlog)
        ┌──────┴──────────────────────────────────────┐
HotForge.Core  │  RuleEngine: maps trigger events → actions  │
               │   ├── Abstractions  IInputBackend, ITrigger, IActionExecutor
               │   ├── Triggers      HotkeyTrigger (chord match)
               │   └── Actions       RunProcess, TypeText
        ┌──────┴──────────────────────────────────────┐
per-OS backend │  IInputBackend implementations               │
               │   ├── HotForge.Windows  WH_KEYBOARD_LL  (real)
               │   ├── HotForge.Linux    X11/Wayland      (stub)
               │   └── HotForge.Mac      CGEventTap       (stub)
        └──────────────────────────────────────────────┘
```

## Why this shape — the hard constraint

This is a **native app, not a web app**. Global hotkeys, injecting input into
other apps, and watching the system require OS APIs a browser sandbox forbids.
There is no architectural choice here; the only choice is *how* to isolate the
unavoidable per-OS native code. The answer: one interface, `IInputBackend`,
with a backend per OS. Engine and rules never see platform code.

## Components

### `Abstractions/`

- `IInputBackend` — produces a normalized `KeyEvent` stream; can inject input.
  One implementation per OS. **Backlog axis: one backend per platform.**
- `ITrigger` — given engine state + an event, decides "did I fire?" Stateless,
  independent. **Backlog axis: one trigger type per PR** (hotkey, hotstring,
  window-focus, file-change, clipboard, timer).
- `IActionExecutor` — performs one effect. **Backlog axis: one action per PR**
  (run process, type text, window move/resize, HTTP call, clipboard transform).

### `Model/`

`KeyChord` (normalized modifier+key), `AutomationRule` (trigger + action),
`RuleEngine` (the OS-agnostic dispatcher). Stable contracts; rarely change.

### `HotForge.Core`

Pure, unit-testable with a fake `IInputBackend` — no OS required in CI.

### Per-OS backends

The only place native interop lives. A new backend only has to implement
`IInputBackend`; it never touches triggers, actions, or the engine.

## Why it works for sustained contribution

OS × trigger × action is a multiplicative, naturally-partitioned backlog.
Dozens of contributors can each add a backend, a trigger, or an action in
parallel with no merge conflicts — the property that keeps the issue queue
both deep and mergeable indefinitely.
