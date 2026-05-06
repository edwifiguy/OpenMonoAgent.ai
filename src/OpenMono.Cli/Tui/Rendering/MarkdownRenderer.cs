using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Terminal.Gui.Drawing;
using TAttr = Terminal.Gui.Drawing.Attribute;

namespace OpenMono.Tui.Rendering;

public static class MarkdownRenderer
{
    private static readonly MarkdownPipeline Pipeline =
        new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    public static List<RenderedBlock> Render(string markdown)
    {
        if (string.IsNullOrEmpty(markdown)) return [];

        var doc = Markdig.Markdown.Parse(markdown, Pipeline);
        var blocks = new List<RenderedBlock>();

        foreach (var block in doc)
            ProcessBlock(block, blocks);

        return blocks;
    }

    public static bool HasIncompleteCodeFence(string text)
    {
        int count = 0;
        int pos = 0;
        while (pos < text.Length)
        {
            int idx = text.IndexOf("```", pos, StringComparison.Ordinal);
            if (idx < 0) break;
            count++;
            pos = idx + 3;
        }
        return count % 2 != 0;
    }

    private static void ProcessBlock(Markdig.Syntax.Block block, List<RenderedBlock> result)
    {
        switch (block)
        {
            case HeadingBlock heading:
                result.Add(new RenderedBlock
                {
                    Kind  = BlockKind.Heading,
                    Spans = RenderInlines(heading.Inline),
                });
                break;

            case FencedCodeBlock fenced:
                var lang   = fenced.Info ?? "";
                var code   = fenced.Lines.ToString();
                var highlighted = string.IsNullOrEmpty(lang) ? null : TryHighlight(code, lang);
                result.Add(new RenderedBlock
                {
                    Kind             = BlockKind.CodeBlock,
                    Language         = string.IsNullOrEmpty(lang) ? null : lang,
                    RawCode          = code,
                    HighlightedSpans = highlighted,
                    Spans            = [MakePlain(code)],
                });
                break;

            case CodeBlock indented:
                var rawCode = indented.Lines.ToString();
                result.Add(new RenderedBlock
                {
                    Kind    = BlockKind.CodeBlock,
                    RawCode = rawCode,
                    Spans   = [MakePlain(rawCode)],
                });
                break;

            case ListBlock list:
                int orderedIndex = 1;
                foreach (var item in list.OfType<ListItemBlock>())
                {
                    var prefix = list.IsOrdered ? $"{orderedIndex++}." : "•";
                    var spans  = new List<RenderedSpan> { MakePlain(prefix + " ") };
                    foreach (var child in item)
                    {
                        if (child is ParagraphBlock para)
                            spans.AddRange(RenderInlines(para.Inline));
                    }
                    result.Add(new RenderedBlock { Kind = BlockKind.ListItem, Spans = spans });
                }
                break;

            case QuoteBlock quote:
                var qSpans = new List<RenderedSpan> { MakePlain("│ ") };
                foreach (var child in quote)
                    if (child is ParagraphBlock para)
                        qSpans.AddRange(RenderInlines(para.Inline));
                result.Add(new RenderedBlock { Kind = BlockKind.BlockQuote, Spans = qSpans });
                break;

            case ThematicBreakBlock:
                result.Add(new RenderedBlock { Kind = BlockKind.HorizontalRule, Spans = [MakePlain("─────────────────")] });
                break;

            case ParagraphBlock para:
                result.Add(new RenderedBlock
                {
                    Kind  = BlockKind.Text,
                    Spans = RenderInlines(para.Inline),
                });
                break;

            case ContainerBlock container:
                foreach (var child in container)
                    ProcessBlock(child, result);
                break;
        }
    }

    private static List<RenderedSpan> RenderInlines(ContainerInline? inlines)
    {
        var spans = new List<RenderedSpan>();
        if (inlines == null) return spans;

        foreach (var inline in inlines)
            ProcessInline(inline, spans, parentBold: false);

        return spans;
    }

    private static void ProcessInline(Inline inline, List<RenderedSpan> spans, bool parentBold)
    {
        switch (inline)
        {
            case LiteralInline literal:
                spans.Add(MakeSpan(literal.Content.ToString(), parentBold));
                break;

            case EmphasisInline emphasis:
                bool isBold = emphasis.DelimiterCount >= 2;
                foreach (var child in emphasis)
                    ProcessInline(child, spans, parentBold || isBold);
                break;

            case CodeInline code:
                var codeAttr = new TAttr(ThemeManager.Current.Foreground, ThemeManager.Current.CodeBlockBg);
                spans.Add(new RenderedSpan { Text = code.Content, Attribute = codeAttr });
                break;

            case LinkInline link:
                var title = string.Concat(link.OfType<LiteralInline>().Select(l => l.Content.ToString()));
                spans.Add(MakePlain("[" + title + "]"));
                if (link.Url != null)
                    spans.Add(MakePlain("(" + link.Url + ")"));
                break;

            case ContainerInline container:
                foreach (var child in container)
                    ProcessInline(child, spans, parentBold);
                break;

            case LineBreakInline:
                spans.Add(MakePlain("\n"));
                break;
        }
    }

    private static RenderedSpan MakePlain(string text)
    {
        return new RenderedSpan { Text = text, Attribute = ThemeManager.Current.Normal };
    }

    private static RenderedSpan MakeSpan(string text, bool bold)
    {
        if (!bold) return MakePlain(text);
        var boldAttr = new TAttr(ThemeManager.Current.Foreground, ThemeManager.Current.Background, TextStyle.Bold);
        return new RenderedSpan { Text = text, Attribute = boldAttr };
    }

    private static List<ColoredSpan>? TryHighlight(string code, string lang)
    {
        try
        {
            var spans = SyntaxHighlighter.Highlight(code, lang);
            // Only return if we got keyword spans (meaning we recognized the language)
            return spans.Any(s => s.Token != TokenType.Plain) ? spans : null;
        }
        catch { return null; }
    }
}
