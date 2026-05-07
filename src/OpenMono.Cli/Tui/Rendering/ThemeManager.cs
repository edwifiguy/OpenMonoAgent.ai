using System.Text.Json;
using System.Text.Json.Nodes;
using Terminal.Gui.Drawing;
using TAttr = Terminal.Gui.Drawing.Attribute;

namespace OpenMono.Tui.Rendering;

public sealed class Theme
{
    public Color Background { get; init; }
    public Color Foreground { get; init; }
    public Color Muted { get; init; }
    public Color CodeBlockBg { get; init; }
    public Color SyntaxKeyword { get; init; }
    public Color SyntaxString { get; init; }
    public Color SyntaxComment { get; init; }
    public Color SyntaxNumber { get; init; }
    public Color SyntaxFunction { get; init; }
    public Color SyntaxPlain { get; init; }

    public TAttr Normal => new(Foreground, Background);
    public TAttr Dim => new(Muted, Background);
    public TAttr Focus => new(Foreground, Background);

    public TAttr GetSyntaxAttribute(TokenType token) => token switch
    {
        TokenType.Keyword  => new TAttr(SyntaxKeyword,  CodeBlockBg),
        TokenType.String   => new TAttr(SyntaxString,   CodeBlockBg),
        TokenType.Comment  => new TAttr(SyntaxComment,  CodeBlockBg),
        TokenType.Number   => new TAttr(SyntaxNumber,   CodeBlockBg),
        TokenType.Function => new TAttr(SyntaxFunction, CodeBlockBg),
        _                  => new TAttr(SyntaxPlain,    CodeBlockBg),
    };

    public Scheme MakeRoleScheme(Color color) => new()
    {
        Normal = new TAttr(color, Background),
        Focus  = new TAttr(color, Background),
    };
}

public static class ThemeManager
{
    public static Theme Dark { get; } = new()
    {
        Background    = Color.Black,
        Foreground    = Color.White,
        Muted         = Color.Gray,
        CodeBlockBg   = Color.DarkGray,
        SyntaxKeyword  = Color.BrightCyan,
        SyntaxString   = Color.BrightGreen,
        SyntaxComment  = Color.Gray,
        SyntaxNumber   = Color.BrightYellow,
        SyntaxFunction = Color.BrightBlue,
        SyntaxPlain    = Color.White,
    };

    public static Theme Light { get; } = new()
    {
        Background    = Color.White,
        Foreground    = Color.Black,
        Muted         = Color.Gray,
        CodeBlockBg   = Color.Gray,
        SyntaxKeyword  = Color.Blue,
        SyntaxString   = Color.Green,
        SyntaxComment  = Color.Gray,
        SyntaxNumber   = Color.Red,
        SyntaxFunction = Color.Magenta,
        SyntaxPlain    = Color.Black,
    };

    // Monokai-inspired (not black background)
    private static readonly Color MonokaiBg     = new(39,  40,  34,  255);
    private static readonly Color MonokaiWhite  = new(248, 248, 242, 255);
    private static readonly Color MonokaiGray   = new(117, 113, 94,  255);
    private static readonly Color MonokaiGreen  = new(166, 226, 46,  255);
    private static readonly Color MonokaiPink   = new(249, 38,  114, 255);
    private static readonly Color MonokaiOrange = new(253, 151, 31,  255);
    private static readonly Color MonokaiBlue   = new(102, 217, 239, 255);
    private static readonly Color MonokaiPurple = new(174, 129, 255, 255);

    public static Theme Monokai { get; } = new()
    {
        Background    = MonokaiBg,
        Foreground    = MonokaiWhite,
        Muted         = MonokaiGray,
        CodeBlockBg   = MonokaiBg,
        SyntaxKeyword  = MonokaiPink,
        SyntaxString   = MonokaiGreen,
        SyntaxComment  = MonokaiGray,
        SyntaxNumber   = MonokaiOrange,
        SyntaxFunction = MonokaiBlue,
        SyntaxPlain    = MonokaiWhite,
    };

    // Solarized dark
    private static readonly Color SolarBg      = new(0,   43,  54,  255);
    private static readonly Color SolarFg      = new(131, 148, 150, 255);
    private static readonly Color SolarMuted   = new(88,  110, 117, 255);
    private static readonly Color SolarYellow  = new(181, 137, 0,   255);
    private static readonly Color SolarOrange  = new(203, 75,  22,  255);
    private static readonly Color SolarGreen   = new(133, 153, 0,   255);
    private static readonly Color SolarCyan    = new(42,  161, 152, 255);
    private static readonly Color SolarBlue    = new(38,  139, 210, 255);

    public static Theme Solarized { get; } = new()
    {
        Background    = SolarBg,
        Foreground    = SolarFg,
        Muted         = SolarMuted,
        CodeBlockBg   = SolarBg,
        SyntaxKeyword  = SolarGreen,
        SyntaxString   = SolarYellow,
        SyntaxComment  = SolarMuted,
        SyntaxNumber   = SolarOrange,
        SyntaxFunction = SolarBlue,
        SyntaxPlain    = SolarFg,
    };

    private static Theme _current = Dark;
    public static Theme Current => _current;

    public static Theme ResolveBuiltIn(string name) => name.ToLowerInvariant() switch
    {
        "light"     => Light,
        "monokai"   => Monokai,
        "solarized" => Solarized,
        _           => Dark,
    };

    public static void Load(string? configPath)
    {
        if (configPath == null || !File.Exists(configPath))
        {
            _current = Dark;
            return;
        }

        try
        {
            var json = File.ReadAllText(configPath);
            var node = JsonNode.Parse(json);
            if (node == null) { _current = Dark; return; }

            var themeName = node["theme"]?.GetValue<string>() ?? "dark";
            var baseTheme = ResolveBuiltIn(themeName);

            var custom = node["customTheme"];
            if (custom == null) { _current = baseTheme; return; }

            _current = ApplyCustomOverrides(baseTheme, custom);
        }
        catch (JsonException)
        {
            _current = Dark;
        }
        catch (Exception)
        {
            _current = Dark;
        }
    }

    private static Theme ApplyCustomOverrides(Theme base_, JsonNode custom)
    {
        var bg        = ParseColorOr(custom["background"]?.GetValue<string>(), base_.Background);
        var fg        = ParseColorOr(custom["foreground"]?.GetValue<string>(), base_.Foreground);
        var muted     = ParseColorOr(custom["muted"]?.GetValue<string>(),      base_.Muted);
        var codeBlockBg = ParseColorOr(custom["codeBlockBg"]?.GetValue<string>(), base_.CodeBlockBg);

        var syntax    = custom["syntax"];
        var keyword   = ParseColorOr(syntax?["keyword"]?.GetValue<string>(),   base_.SyntaxKeyword);
        var str       = ParseColorOr(syntax?["string"]?.GetValue<string>(),    base_.SyntaxString);
        var comment   = ParseColorOr(syntax?["comment"]?.GetValue<string>(),   base_.SyntaxComment);
        var number    = ParseColorOr(syntax?["number"]?.GetValue<string>(),    base_.SyntaxNumber);
        var function  = ParseColorOr(syntax?["function"]?.GetValue<string>(),  base_.SyntaxFunction);
        var plain     = ParseColorOr(syntax?["plain"]?.GetValue<string>(),     base_.SyntaxPlain);

        return new Theme
        {
            Background    = bg,
            Foreground    = fg,
            Muted         = muted,
            CodeBlockBg   = codeBlockBg,
            SyntaxKeyword  = keyword,
            SyntaxString   = str,
            SyntaxComment  = comment,
            SyntaxNumber   = number,
            SyntaxFunction = function,
            SyntaxPlain    = plain,
        };
    }

    private static Color ParseColorOr(string? hex, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(hex)) return fallback;
        hex = hex.TrimStart('#');
        if (hex.Length != 6) return fallback;
        if (!int.TryParse(hex[0..2], System.Globalization.NumberStyles.HexNumber, null, out var r)) return fallback;
        if (!int.TryParse(hex[2..4], System.Globalization.NumberStyles.HexNumber, null, out var g)) return fallback;
        if (!int.TryParse(hex[4..6], System.Globalization.NumberStyles.HexNumber, null, out var b)) return fallback;
        return new Color(r, g, b, 255);
    }
}
