using System.Text.Json;
using HotForge.Core.Model;

namespace HotForge.App;

/// <summary>Parses config.json into AutomationRules. Format: see config.sample.json.</summary>
internal static class ConfigLoader
{
    public static IReadOnlyList<AutomationRule> Load(string path)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
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
