using TAttr = Terminal.Gui.Drawing.Attribute;

namespace OpenMono.Tui.Rendering;

public sealed class RenderedSpan
{
    public string Text { get; init; } = "";
    public TAttr Attribute { get; init; }
}
