using HotForge.Core.Model;

namespace HotForge.Core.Ahk;

public static class AhkScript
{
    public static AhkProgram ParseProgram(string text) => AhkParser.Parse(text);

    public static IReadOnlyList<AutomationRule> Parse(string text)
        => AhkLowering.Lower(AhkParser.Parse(text));

    public static string Write(IEnumerable<AutomationRule> rules) => AhkWriter.Write(rules);

    public static string WriteRule(AutomationRule rule) => AhkWriter.WriteRule(rule);
}
