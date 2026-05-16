# HotForge

A modern, cross-platform OS automation engine — global hotkeys, hotstrings,
window rules, clipboard and file triggers, macros — without a 2003-era DSL.

The gap: AutoHotkey is Windows-only with a bespoke language; Hammerspoon is
macOS-only; espanso is text-expansion only; AutoKey is Linux-only and
stagnant. There is no good, modern, cross-platform, OSS automation app.
HotForge is that.

## Status

**Windows and Linux work.** The engine is OS-agnostic. Both the Windows
(`WH_KEYBOARD_LL` hook) and Linux (evdev grab + uinput) backends are real and
**suppress** matched hotkeys AutoHotkey-style — the keystroke never reaches the
desktop or other apps. macOS is still a stub: the contribution surface, not
work hidden under the rug. See [docs/BACKLOG.md](docs/BACKLOG.md).

## Pieces

- **`src/HotForge.Core`** — OS-agnostic engine. A rule = a trigger + an
  action. Triggers and actions are independent, registered units.
- **`src/HotForge.Windows`** — real `WH_KEYBOARD_LL` global hook.
- **`src/HotForge.Linux`** — real backend: exclusively grabs the keyboard
  (`EVIOCGRAB`), re-injects through `/dev/uinput`, swallows matched hotkeys.
  No X11/Wayland dependency.
- **`src/HotForge.Mac`** — backend stub (backlog).
- **`src/HotForge.App`** — console host: loads a script, wires backend +
  engine, runs.
- **`src/HotForge.Gui`** — Avalonia desktop app: a script editor with
  New / Open / Save / Run. No tray icon — on Linux it runs as a plain window.

## Scripts

A HotForge script is a JSON rules document (no bespoke DSL) saved with the
`.hotforge` extension. The GUI reads, edits, and writes these files; the
console host takes one as its argument. See
[config.sample.json](src/HotForge.App/config.sample.json) for the format.

## Quick start

GUI (recommended — built-in editor):

```bash
dotnet run --project src/HotForge.Gui
# write/open a .hotforge script, then click ▶ Run
```

Console host:

```bash
dotnet run --project src/HotForge.App -- src/HotForge.App/config.sample.json
```

### Linux setup

The backend needs access to `/dev/input` and `/dev/uinput`, which are
root-only by default (otherwise Run reports
`backend unavailable: No readable keyboard devices`). Grant your user
access once:

```bash
./scripts/setup-linux.sh
newgrp input            # apply the new group without logging out
dotnet run --project src/HotForge.Gui
```

While a script runs, the keyboard is grabbed exclusively and re-injected;
**Ctrl+C / Stop releases it.** Keep an SSH session as an escape hatch if a
script hangs.

### Windows

```bash
dotnet run --project src/HotForge.App
# config.sample.json binds Ctrl+Alt+H -> launch notepad
```

## Architecture

[docs/ARCHITECTURE.md](docs/ARCHITECTURE.md). Invariant:
**one trigger or one action = one PR.** Per-OS backends, per-trigger types,
and per-action types are all independent — the backlog never runs dry.

## License

MIT — see [LICENSE](LICENSE).
