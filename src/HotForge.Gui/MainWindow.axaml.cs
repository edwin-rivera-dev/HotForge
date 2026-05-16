using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using HotForge.Core;
using HotForge.Core.Abstractions;
using HotForge.Core.Model;
using HotForge.Linux;
using HotForge.Windows;

namespace HotForge.Gui;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<string> _rules = new();
    private readonly ObservableCollection<string> _log = new();

    private IReadOnlyList<AutomationRule> _loadedRules = Array.Empty<AutomationRule>();
    private IInputBackend? _backend;
    private RuleEngine? _engine;
    private string? _configPath;

    public MainWindow()
    {
        InitializeComponent();
        RulesList.ItemsSource = _rules;
        LogList.ItemsSource = _log;

        var defaultConfig = Path.Combine(AppContext.BaseDirectory, "config.sample.json");
        if (File.Exists(defaultConfig))
            LoadConfig(defaultConfig);
    }

    private async void OnLoad(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select a HotForge config",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("JSON config") { Patterns = new[] { "*.json" } },
            },
        });

        var path = files.Count > 0 ? files[0].TryGetLocalPath() : null;
        if (path is not null)
            LoadConfig(path);
    }

    private void LoadConfig(string path)
    {
        try
        {
            _loadedRules = ConfigLoader.Load(path);
            _configPath = path;
            _rules.Clear();
            foreach (var r in _loadedRules)
                _rules.Add(Describe(r));
            StatusBar.Text = $"Loaded {_loadedRules.Count} rule(s) from {Path.GetFileName(path)}.";
        }
        catch (Exception ex)
        {
            AppendLog($"config error: {ex.Message}");
            StatusBar.Text = "Failed to load config.";
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

    private void OnStart(object? sender, RoutedEventArgs e)
    {
        if (_engine is not null)
            return;
        if (_loadedRules.Count == 0)
        {
            AppendLog("nothing to run — load a config first.");
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

        _engine = new RuleEngine(_backend, _loadedRules, log: AppendLog);
        try
        {
            _engine.Start();
            SetRunning(true);
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
        SetRunning(false);
        AppendLog("engine stopped.");
    }

    private void SetRunning(bool running)
    {
        StartButton.IsEnabled = !running;
        StopButton.IsEnabled = running;
        LoadButton.IsEnabled = !running;
        StatusText.Text = running ? "● Running" : "● Stopped";
        StatusText.Foreground = SolidColorBrush.Parse(running ? "#7BE0A8" : "#FF8AA0");
        StatusPill.Background = SolidColorBrush.Parse(running ? "#1F3A2C" : "#3A2530");
        if (running && _configPath is not null)
            StatusBar.Text = $"Running {_loadedRules.Count} rule(s) — {Path.GetFileName(_configPath)}.";
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
