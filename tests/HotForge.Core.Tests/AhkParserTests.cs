using HotForge.Core.Ahk;
using Xunit;

namespace HotForge.Core.Tests;

public class AhkParserTests
{
    [Fact]
    public void Inline_run_hotkey_lowers_to_run_rule()
    {
        var rules = AhkScript.Parse("^!h::Run, notepad.exe");

        var rule = Assert.Single(rules);
        Assert.Equal("hotkey", rule.TriggerKind);
        Assert.Equal("Ctrl+Alt+H", rule.TriggerArgs["chord"]);
        Assert.Equal("run", rule.ActionKind);
        Assert.Equal("notepad.exe", rule.ActionArgs["path"]);
    }

    [Fact]
    public void Send_hotkey_lowers_to_type_rule()
    {
        var rules = AhkScript.Parse("#n::Send Hello world");

        var rule = Assert.Single(rules);
        Assert.Equal("Win+N", rule.TriggerArgs["chord"]);
        Assert.Equal("type", rule.ActionKind);
        Assert.Equal("Hello world", rule.ActionArgs["text"]);
    }

    [Fact]
    public void Multiline_hotkey_block_uses_first_action()
    {
        var script = "^!t::\n    Send typed\n    return\n";
        var rules = AhkScript.Parse(script);

        var rule = Assert.Single(rules);
        Assert.Equal("Ctrl+Alt+T", rule.TriggerArgs["chord"]);
        Assert.Equal("type", rule.ActionKind);
        Assert.Equal("typed", rule.ActionArgs["text"]);
    }

    [Fact]
    public void Comments_and_directives_are_ignored_in_lowering()
    {
        var script = "; a comment\n#SingleInstance Force\n^j::Run calc";
        var program = AhkScript.ParseProgram(script);
        var rules = AhkScript.Parse(script);

        Assert.Contains(program.Statements, s => s is DirectiveStatement);
        Assert.Single(rules);
        Assert.Equal("Ctrl+J", rules[0].TriggerArgs["chord"]);
    }

    [Fact]
    public void Bare_key_to_key_is_a_remap()
    {
        var program = AhkScript.ParseProgram("a::b");
        Assert.Contains(program.Statements, s => s is RemapStatement);

        var rules = AhkScript.Parse("a::b");
        var rule = Assert.Single(rules);
        Assert.Equal("A", rule.TriggerArgs["chord"]);
        Assert.Equal("type", rule.ActionKind);
        Assert.Equal("b", rule.ActionArgs["text"]);
    }

    [Fact]
    public void Hotstring_is_parsed_but_not_yet_executable()
    {
        var program = AhkScript.ParseProgram("::btw::by the way");
        var hs = Assert.IsType<HotstringStatement>(Assert.Single(program.Statements));
        Assert.Equal("btw", hs.Abbreviation);
        Assert.Equal("by the way", hs.Replacement);

        Assert.Empty(AhkScript.Parse("::btw::by the way"));
    }

    [Fact]
    public void Unknown_statements_are_preserved_not_dropped()
    {
        var program = AhkScript.ParseProgram("MsgBox, hi\nx := 1 + 2");
        Assert.Equal(2, program.Statements.Count);
        Assert.All(program.Statements, s => Assert.IsType<UnknownStatement>(s));
    }

    [Fact]
    public void Write_then_parse_round_trips_a_rule()
    {
        var original = AhkScript.Parse("^!h::Run, notepad.exe");
        var text = AhkScript.Write(original);
        var reparsed = AhkScript.Parse(text);

        var rule = Assert.Single(reparsed);
        Assert.Equal("Ctrl+Alt+H", rule.TriggerArgs["chord"]);
        Assert.Equal("run", rule.ActionKind);
        Assert.Equal("notepad.exe", rule.ActionArgs["path"]);
    }
}
