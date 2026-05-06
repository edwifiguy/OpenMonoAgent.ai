namespace OpenMono.Tui.Rendering;

public sealed class RenderedBlock
{
    public BlockKind Kind { get; init; }
    public List<RenderedSpan> Spans { get; init; } = [];
    public string? Language { get; init; }
    public string? RawCode { get; init; }
    public List<ColoredSpan>? HighlightedSpans { get; init; }
}
