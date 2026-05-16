# Contributing

Most contributions are a new **trigger**, a new **action**, or a new **OS
backend** — each small and independent.

## Setup

```bash
dotnet build
dotnet test          # engine tests, no OS hooks required
dotnet run --project src/HotForge.App   # Windows: live hotkey host
```

Requires the .NET SDK pinned in `global.json`.

## What to work on

See [docs/BACKLOG.md](docs/BACKLOG.md). Recipes:
[docs/WRITING_A_TRIGGER_OR_ACTION.md](docs/WRITING_A_TRIGGER_OR_ACTION.md).

- **Action** — smallest unit, best first PR (run, type, window, http…).
- **Trigger** — hotstring, window-focus, file-change, clipboard, timer.
- **OS backend** — Linux X11, Linux Wayland, macOS CGEventTap.

## Ground rules

- One concern per PR.
- Engine/triggers/actions contain **no platform code** — that lives only in a
  per-OS backend.
- New behavior needs an engine test driven by a fake `IInputBackend`.
- `dotnet test` and `dotnet format --verify-no-changes` must pass.
