using System.Text.Json;
using HotForge.Core.Model;

namespace HotForge.Core;

/// <summary>Parses a HotForge script (.hotforge / JSON) into AutomationRules.</summary>
public static class ConfigLoader
{
    /// <summary>Load and parse a script file from disk.</summary>
    public static IReadOnlyList<AutomationRule> Load(string path)
        => Parse(File.ReadAllText(path));

    /// <summary>Parse script text directly (e.g. from the GUI editor buffer).</summary>
    public static IReadOnlyList<AutomationRule> Parse(string scriptText)
    {
        using var doc = JsonDocument.Parse(scriptText);
        var rules = new List<AutomationRule>();

        foreach (var r in doc.RootElement.GetProperty("rules").EnumerateArray())
        {
            var trigger = r.GetProperty("trigger");
            var action = r.GetProperty("action");
            rules.Add(new AutomationRule(
                trigger.GetProperty("kind").GetString() ?? "",
                ReadArgs(trigger),
                action.GetProperty("kind").GetString() ?? "",
                ReadArgs(action)));
        }

        return rules;
    }

    private static Dictionary<string, string> ReadArgs(JsonElement node)
    {
        var args = new Dictionary<string, string>();
        if (node.TryGetProperty("args", out var a))
            foreach (var prop in a.EnumerateObject())
                args[prop.Name] = prop.Value.GetString() ?? "";
        return args;
    }
}
