# HotForge

A modern, cross-platform OS automation engine — global hotkeys, hotstrings,
window rules, clipboard and file triggers, macros — without a 2003-era DSL.

The gap: AutoHotkey is Windows-only with a bespoke language; Hammerspoon is
macOS-only; espanso is text-expansion only; AutoKey is Linux-only and
stagnant. There is no good, modern, cross-platform, OSS automation app.
HotForge is that.

## Status

**Windows-first skeleton.** The engine is OS-agnostic and the Windows input
backend is a real low-level keyboard hook. Linux (X11/Wayland) and macOS
backends are stubs — they are the contribution surface, not missing work
hidden under the rug. See [docs/BACKLOG.md](docs/BACKLOG.md).

## Pieces

- **`src/HotForge.Core`** — OS-agnostic engine. A rule = a trigger + an
  action. Triggers and actions are independent, registered units.
- **`src/HotForge.Windows`** — real `WH_KEYBOARD_LL` global hook.
- **`src/HotForge.Linux` / `.Mac`** — backend stubs (backlog).
- **`src/HotForge.App`** — console host: loads a config, wires backend +
  engine, runs the OS message loop. A tray UI / Avalonia editor is backlog.

## Quick start (Windows)

```bash
dotnet run --project src/HotForge.App
# config.sample.json binds Ctrl+Alt+H -> launch notepad
```

## Linux setup

The backend needs access to `/dev/input` and `/dev/uinput`, which are
root-only by default (otherwise Run reports
`backend unavailable: No readable keyboard devices`). Grant your user access
once:

```bash
./scripts/setup-linux.sh
newgrp input            # apply the new group without logging out
dotnet run --project src/HotForge.Gui
```

## Architecture

[docs/ARCHITECTURE.md](docs/ARCHITECTURE.md). Invariant:
**one trigger or one action = one PR.** Per-OS backends, per-trigger types,
and per-action types are all independent — the backlog never runs dry.

## License

MIT — see [LICENSE](LICENSE).
