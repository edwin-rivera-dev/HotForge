namespace HotForge.Core.Ahk;

public enum AhkActionKind
{
    None,
    Run,
    Send,
    Unsupported,
}

public sealed record AhkAction(AhkActionKind Kind, string Primary, string Secondary)
{
    public static readonly AhkAction None = new(AhkActionKind.None, string.Empty, string.Empty);
}

public abstract record AhkStatement;

public sealed record HotkeyStatement(
    string Modifiers,
    string Key,
    AhkAction Action,
    IReadOnlyList<string> Body) : AhkStatement;

public sealed record RemapStatement(string FromKey, string ToKey) : AhkStatement;

public sealed record HotstringStatement(
    string Options,
    string Abbreviation,
    string Replacement) : AhkStatement;

public sealed record DirectiveStatement(string Raw) : AhkStatement;

public sealed record UnknownStatement(string Raw) : AhkStatement;

public sealed record AhkProgram(IReadOnlyList<AhkStatement> Statements);
