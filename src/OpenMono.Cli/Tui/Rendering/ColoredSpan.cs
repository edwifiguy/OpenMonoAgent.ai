namespace OpenMono.Tui.Rendering;

public readonly struct ColoredSpan
{
    public TokenType Token { get; init; }
    public int Start { get; init; }
    public int Length { get; init; }
}
