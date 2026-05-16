using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using HotForge.Core;
using HotForge.Core.Ahk;
using HotForge.Core.Abstractions;
using HotForge.Core.Model;
using HotForge.Linux;
using HotForge.Windows;

namespace HotForge.Gui;

public partial class MainWindow : Window
{
    private const string Extension = "ahk";

    private const string ScriptTemplate = """
        ; HotForge script — AutoHotkey syntax
        ; ^ = Ctrl   ! = Alt   + = Shift   # = Win

        ^!j::Send Hello from HotForge
        """;

    private readonly ObservableCollection<string> _rules = new();
    private readonly ObservableCollection<string> _log = new();

    private IInputBackend? _backend;
    private RuleEngine? _engine;
    private string? _scriptPath;

    public MainWindow()
    {
        InitializeComponent();
        RulesList.ItemsSource = _rules;
        LogList.ItemsSource = _log;

        // Open the bundled sample as the starting script if present.
        var sample = Path.Combine(AppContext.BaseDirectory, "config.sample.json");
        if (File.Exists(sample))
            OpenPath(sample);
        else
            SetScript(ScriptTemplate, path: null);
    }

    // ---- File: New / Open / Save / Save As --------------------------------

    private void OnNew(object? sender, RoutedEventArgs e)
        => SetScript(ScriptTemplate, path: null);

    private async void OnOpen(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open a HotForge script",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("AutoHotkey script") { Patterns = new[] { $"*.{Extension}" } },
                new FilePickerFileType("JSON") { Patterns = new[] { "*.json" } },
                new FilePickerFileType("All files") { Patterns = new[] { "*" } },
            },
        });

        var path = files.Count > 0 ? files[0].TryGetLocalPath() : null;
        if (path is not null)
            OpenPath(path);
    }

    private void OnSave(object? sender, RoutedEventArgs e)
    {
        if (_scriptPath is null)
            OnSaveAs(sender, e);
        else
            WriteScript(_scriptPath);
    }

    private async void OnSaveAs(object? sender, RoutedEventArgs e)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save HotForge script",
            SuggestedFileName = $"untitled.{Extension}",
            DefaultExtension = Extension,
            FileTypeChoices = new[]
            {
                new FilePickerFileType("AutoHotkey script") { Patterns = new[] { $"*.{Extension}" } },
            },
        });

        var path = file?.TryGetLocalPath();
        if (path is not null)
            WriteScript(path);
    }

    private void OpenPath(string path)
    {
        try
        {
            SetScript(File.ReadAllText(path), path);
            AppendLog($"opened {Path.GetFileName(path)}");
        }
        catch (Exception ex)
        {
            AppendLog($"open failed: {ex.Message}");
        }
    }

    private void WriteScript(string path)
    {
        try
        {
            File.WriteAllText(path, ScriptEditor.Text ?? "");
            _scriptPath = path;
            EditorHead.Text = $"SCRIPT — {Path.GetFileName(path)}";
            StatusBar.Text = $"Saved {Path.GetFileName(path)}.";
            RefreshRules();
        }
        catch (Exception ex)
        {
            AppendLog($"save failed: {ex.Message}");
        }
    }

    private void SetScript(string text, string? path)
    {
        ScriptEditor.Text = text;
        _scriptPath = path;
        EditorHead.Text = $"SCRIPT — {(path is null ? $"untitled.{Extension}" : Path.GetFileName(path))}";
        RefreshRules();
    }

    /// <summary>Re-parse the editor buffer and refresh the rules panel.</summary>
    private bool RefreshRules()
    {
        try
        {
            var rules = ParseScript(ScriptEditor.Text ?? "");
            _rules.Clear();
            foreach (var r in rules)
                _rules.Add(Describe(r));
            StatusBar.Text = $"{rules.Count} rule(s) parsed.";
            return true;
        }
        catch (Exception ex)
        {
            _rules.Clear();
            StatusBar.Text = "Script has errors — not runnable.";
            AppendLog($"script error: {ex.Message}");
            return false;
        }
    }

    private static string Describe(AutomationRule r)
    {
        var chord = r.TriggerArgs.TryGetValue("chord", out var c) ? c : r.TriggerKind;
        var detail = r.ActionArgs.TryGetValue("path", out var p) ? p
            : r.ActionArgs.TryGetValue("text", out var t) ? $"\"{t}\""
            : string.Empty;
        return $"{chord}   →   {r.ActionKind} {detail}".TrimEnd();
    }

    // ---- Run / Stop -------------------------------------------------------

    private void OnStart(object? sender, RoutedEventArgs e)
    {
        if (_engine is not null)
            return;

        IReadOnlyList<AutomationRule> rules;
        try
        {
            rules = ParseScript(ScriptEditor.Text ?? "");
        }
        catch (Exception ex)
        {
            AppendLog($"script error: {ex.Message}");
            StatusBar.Text = "Fix the script before running.";
            return;
        }

        if (rules.Count == 0)
        {
            AppendLog("nothing to run — the script has no rules.");
            return;
        }

        if (OperatingSystem.IsWindows())
            _backend = new WindowsInputBackend();
        else if (OperatingSystem.IsLinux())
            _backend = new LinuxInputBackend();
        else
        {
            AppendLog("no input backend for this OS.");
            return;
        }

        _engine = new RuleEngine(_backend, rules, log: AppendLog);
        try
        {
            _engine.Start();
            SetRunning(true, rules.Count);
        }
        catch (InvalidOperationException ex)
        {
            AppendLog($"backend unavailable: {ex.Message}");
            _backend.Dispose();
            _backend = null;
            _engine = null;
        }
    }

    private void OnStop(object? sender, RoutedEventArgs e)
    {
        _backend?.Dispose();
        _backend = null;
        _engine = null;
        SetRunning(false, 0);
        AppendLog("engine stopped.");
    }

    private void SetRunning(bool running, int ruleCount)
    {
        StartButton.IsEnabled = !running;
        StopButton.IsEnabled = running;
        // Editing the script while it runs would desync the live rules.
        ScriptEditor.IsEnabled = !running;
        NewButton.IsEnabled = !running;
        OpenButton.IsEnabled = !running;
        SaveButton.IsEnabled = !running;
        SaveAsButton.IsEnabled = !running;

        StatusText.Text = running ? "● Running" : "● Stopped";
        StatusText.Foreground = SolidColorBrush.Parse(running ? "#7BE0A8" : "#FF8AA0");
        StatusPill.Background = SolidColorBrush.Parse(running ? "#1F3A2C" : "#3A2530");
        if (running)
            StatusBar.Text = $"Running {ruleCount} rule(s) — press your hotkeys.";
    }

    private static IReadOnlyList<AutomationRule> ParseScript(string text)
    {
        var t = text.TrimStart();
        return t.StartsWith('{') ? ConfigLoader.Parse(text) : AhkScript.Parse(text);
    }

    private void OnInsertRule(object? sender, RoutedEventArgs e)
    {
        var key = (KeyInput.Text ?? string.Empty).Trim();
        if (key.Length == 0)
        {
            AppendLog("quick build: enter a key.");
            return;
        }

        var parts = new List<string>();
        if (ModCtrl.IsChecked == true) parts.Add("Ctrl");
        if (ModAlt.IsChecked == true) parts.Add("Alt");
        if (ModShift.IsChecked == true) parts.Add("Shift");
        if (ModWin.IsChecked == true) parts.Add("Win");
        parts.Add(key);
        var chord = string.Join("+", parts);

        var arg = (ArgInput.Text ?? string.Empty).Trim();
        var rule = ActionBox.SelectedIndex == 0
            ? new AutomationRule(
                "hotkey",
                new Dictionary<string, string> { ["chord"] = chord },
                "run",
                new Dictionary<string, string> { ["path"] = arg, ["args"] = string.Empty })
            : new AutomationRule(
                "hotkey",
                new Dictionary<string, string> { ["chord"] = chord },
                "type",
                new Dictionary<string, string> { ["text"] = arg });

        var generated = AhkScript.WriteRule(rule);
        if (generated.Length == 0)
        {
            AppendLog($"quick build: '{key}' is not a supported key.");
            return;
        }

        var buffer = ScriptEditor.Text ?? string.Empty;
        if (buffer.Length > 0 && !buffer.EndsWith('\n'))
            buffer += "\n";
        ScriptEditor.Text = buffer + generated + "\n";
        KeyInput.Text = string.Empty;
        ArgInput.Text = string.Empty;
        RefreshRules();
        AppendLog($"added: {generated}");
    }

    private void AppendLog(string line)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _log.Add($"{DateTime.Now:HH:mm:ss}  {line}");
            while (_log.Count > 500)
                _log.RemoveAt(0);
            if (_log.Count > 0)
                LogList.ScrollIntoView(_log.Count - 1);
        });
    }
}
