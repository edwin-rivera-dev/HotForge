# Starter Backlog

Each line is one issue = one PR. Recipes:
[docs/WRITING_A_TRIGGER_OR_ACTION.md](WRITING_A_TRIGGER_OR_ACTION.md). The
backlog is the OS × trigger × action matrix — it does not run dry.

## OS backends (`IInputBackend`)

- [x] Windows: WH_KEYBOARD_LL hook + Unicode SendInput *(reference, done)*
- [ ] Linux X11: XRecord/XInput2 capture + XTEST injection
- [ ] Linux Wayland: libei / portal-based capture + injection
- [ ] macOS: CGEventTap capture + CGEventPost injection
- [ ] widen the canonical `Key` enum + per-OS maps (punctuation, numpad, media keys)

## Actions (`IActionExecutor`) — easiest first PRs

- [x] run process *(reference)*
- [x] type text *(reference)*
- [ ] window: move / resize / focus / minimize the active window
- [ ] clipboard: get / set / transform (upper, trim, template)
- [ ] http: fire a configurable request
- [ ] keystroke: send a synthetic chord (not just literal text)
- [ ] shell: run a command and capture output
- [ ] delay / sequence: compose multiple actions

## Triggers (`ITrigger`)

- [x] hotkey *(reference)*
- [ ] hotstring: expand a typed abbreviation (`btw ` → `by the way `)
- [ ] window-focus: fire when a window matching a title/class gains focus
- [ ] file-change: fire on a watched path change
- [ ] clipboard-change: fire when clipboard content changes
- [ ] timer: fire on a schedule / interval

## Engine / host

- [ ] config hot-reload (re-read config.json without restart)
- [ ] profiles: enable rule sets per active app / per OS
- [ ] system-tray host (replace the bare message pump)
- [ ] Avalonia rule/script editor
- [ ] shareable rule packs (import/export)
- [ ] structured logging + a rule-test harness

## Known build follow-ups (good first issues)

- [ ] verify `HotForge.App/Program.cs` interop compiles under
      `TreatWarningsAsErrors`; adjust P/Invoke marshalling attributes as needed
- [ ] add Linux/macOS backend project shells once their `IInputBackend`
      implementations begin (kept out of the solution until then)
