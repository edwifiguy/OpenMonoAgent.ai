using System.Text.RegularExpressions;
using TAttr = Terminal.Gui.Drawing.Attribute;

namespace OpenMono.Tui.Rendering;

public static class SyntaxHighlighter
{
    // Language alias normalization
    private static string NormalizeLanguage(string lang) => lang.ToLowerInvariant() switch
    {
        "cs" or "c#" => "csharp",
        "js" => "javascript",
        "ts" => "typescript",
        _ => lang.ToLowerInvariant(),
    };

    public static List<ColoredSpan> Highlight(string code, string lang)
    {
        if (string.IsNullOrEmpty(code)) return [];

        var normalized = NormalizeLanguage(lang);

        var rawSpans = normalized switch
        {
            "csharp" => TokenizeCSharp(code),
            "python" => TokenizePython(code),
            "json" => TokenizeJson(code),
            "bash" => TokenizeBash(code),
            "go" => TokenizeGo(code),
            "rust" => TokenizeRust(code),
            "sql" => TokenizeSql(code),
            "yaml" => TokenizeYaml(code),
            "javascript" or "typescript" => TokenizeJsTs(code),
            _ => null,
        };

        if (rawSpans == null)
            return [new ColoredSpan { Token = TokenType.Plain, Start = 0, Length = code.Length }];

        return FillGaps(code, rawSpans);
    }

    public static string? DetectLanguage(string fenceLine)
    {
        var trimmed = fenceLine.TrimStart('`').Trim();
        if (string.IsNullOrEmpty(trimmed)) return null;

        return NormalizeLanguage(trimmed) switch
        {
            "csharp" => "csharp",
            "python" => "python",
            "json" => "json",
            "bash" => "bash",
            "go" => "go",
            "rust" => "rust",
            "sql" => "sql",
            "yaml" => "yaml",
            "javascript" => "javascript",
            "typescript" => "typescript",
            var other => other,
        };
    }

    public static TAttr GetAttribute(TokenType token) =>
        ThemeManager.Current.GetSyntaxAttribute(token);

    // Produce a gap-free sorted list of spans covering [0, code.Length)
    private static List<ColoredSpan> FillGaps(string code, List<ColoredSpan> spans)
    {
        var sorted = spans.OrderBy(s => s.Start).ToList();
        var result = new List<ColoredSpan>();
        int pos = 0;

        foreach (var span in sorted)
        {
            if (span.Start < pos) continue; // skip overlapping
            if (span.Start > pos)
                result.Add(new ColoredSpan { Token = TokenType.Plain, Start = pos, Length = span.Start - pos });
            if (span.Length > 0)
                result.Add(span);
            pos = span.Start + span.Length;
        }

        if (pos < code.Length)
            result.Add(new ColoredSpan { Token = TokenType.Plain, Start = pos, Length = code.Length - pos });

        return result;
    }

    // ── CSharp ───────────────────────────────────────────────────────────────

    private static readonly HashSet<string> CSharpKeywords =
    [
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch",
        "char", "checked", "class", "const", "continue", "decimal", "default",
        "delegate", "do", "double", "else", "enum", "event", "explicit",
        "extern", "false", "finally", "fixed", "float", "for", "foreach",
        "goto", "if", "implicit", "in", "int", "interface", "internal",
        "is", "lock", "long", "namespace", "new", "null", "object", "operator",
        "out", "override", "params", "private", "protected", "public",
        "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof",
        "stackalloc", "static", "string", "struct", "switch", "this", "throw",
        "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe",
        "ushort", "using", "var", "virtual", "void", "volatile", "while",
        "async", "await", "yield",
    ];

    private static List<ColoredSpan> TokenizeCSharp(string code)
    {
        var spans = new List<ColoredSpan>();
        // Comments take priority — collect them first
        CollectLineComments(code, "//", spans);
        CollectBlockComments(code, "/*", "*/", spans);
        CollectStrings(code, spans, skipRanges: spans.Select(s => (s.Start, s.Start + s.Length)).ToList());
        CollectNumbers(code, spans);
        CollectKeywordsAndFunctions(code, CSharpKeywords, spans);
        return spans;
    }

    // ── Python ───────────────────────────────────────────────────────────────

    private static readonly HashSet<string> PythonKeywords =
    [
        "False", "None", "True", "and", "as", "assert", "async", "await",
        "break", "class", "continue", "def", "del", "elif", "else", "except",
        "finally", "for", "from", "global", "if", "import", "in", "is",
        "lambda", "nonlocal", "not", "or", "pass", "raise", "return",
        "try", "while", "with", "yield",
    ];

    private static List<ColoredSpan> TokenizePython(string code)
    {
        var spans = new List<ColoredSpan>();
        CollectLineComments(code, "#", spans);
        CollectStrings(code, spans, skipRanges: spans.Select(s => (s.Start, s.Start + s.Length)).ToList());
        CollectNumbers(code, spans);
        CollectKeywordsAndFunctions(code, PythonKeywords, spans);
        return spans;
    }

    // ── JSON ─────────────────────────────────────────────────────────────────

    private static List<ColoredSpan> TokenizeJson(string code)
    {
        var spans = new List<ColoredSpan>();

        // JSON keys: "key":
        var keyRegex = new Regex(@"""([^""\\]|\\.)*""\s*:");
        foreach (Match m in keyRegex.Matches(code))
        {
            spans.Add(new ColoredSpan { Token = TokenType.Keyword, Start = m.Index, Length = m.Length });
        }

        // JSON string values (not already covered as keys)
        var taken = spans.Select(s => (s.Start, s.Start + s.Length)).ToList();
        CollectStrings(code, spans, skipRanges: taken);

        // JSON numbers
        CollectNumbers(code, spans);

        // JSON booleans/null
        var boolNullRegex = new Regex(@"\b(true|false|null)\b");
        foreach (Match m in boolNullRegex.Matches(code))
            if (!IsInSpans(spans, m.Index, m.Length))
                spans.Add(new ColoredSpan { Token = TokenType.Keyword, Start = m.Index, Length = m.Length });

        return spans;
    }

    // ── Bash ─────────────────────────────────────────────────────────────────

    private static readonly HashSet<string> BashKeywords =
    [
        "if", "then", "else", "elif", "fi", "for", "do", "done", "while",
        "case", "esac", "in", "function", "return", "exit", "echo", "read",
        "export", "local", "readonly", "unset", "shift", "break", "continue",
    ];

    private static List<ColoredSpan> TokenizeBash(string code)
    {
        var spans = new List<ColoredSpan>();
        // Shebang line
        if (code.StartsWith("#!"))
        {
            var end = code.IndexOf('\n');
            if (end < 0) end = code.Length;
            spans.Add(new ColoredSpan { Token = TokenType.Comment, Start = 0, Length = end });
        }
        CollectLineComments(code, "#", spans);
        CollectStrings(code, spans, skipRanges: spans.Select(s => (s.Start, s.Start + s.Length)).ToList());
        CollectNumbers(code, spans);
        CollectKeywordsAndFunctions(code, BashKeywords, spans);
        return spans;
    }

    // ── Go ───────────────────────────────────────────────────────────────────

    private static readonly HashSet<string> GoKeywords =
    [
        "break", "case", "chan", "const", "continue", "default", "defer",
        "else", "fallthrough", "for", "func", "go", "goto", "if", "import",
        "interface", "map", "package", "range", "return", "select", "struct",
        "switch", "type", "var",
        "true", "false", "nil", "iota",
        "int", "int8", "int16", "int32", "int64",
        "uint", "uint8", "uint16", "uint32", "uint64", "uintptr",
        "float32", "float64", "complex64", "complex128",
        "bool", "byte", "rune", "string", "error",
    ];

    private static List<ColoredSpan> TokenizeGo(string code)
    {
        var spans = new List<ColoredSpan>();
        CollectLineComments(code, "//", spans);
        CollectBlockComments(code, "/*", "*/", spans);
        CollectStrings(code, spans, skipRanges: spans.Select(s => (s.Start, s.Start + s.Length)).ToList());
        CollectNumbers(code, spans);
        CollectKeywordsAndFunctions(code, GoKeywords, spans);
        return spans;
    }

    // ── Rust ─────────────────────────────────────────────────────────────────

    private static readonly HashSet<string> RustKeywords =
    [
        "as", "async", "await", "break", "const", "continue", "crate",
        "dyn", "else", "enum", "extern", "false", "fn", "for", "if",
        "impl", "in", "let", "loop", "match", "mod", "move", "mut",
        "pub", "ref", "return", "self", "Self", "static", "struct",
        "super", "trait", "true", "type", "unsafe", "use", "where", "while",
        "i8", "i16", "i32", "i64", "i128", "isize",
        "u8", "u16", "u32", "u64", "u128", "usize",
        "f32", "f64", "bool", "char", "str", "String",
    ];

    private static List<ColoredSpan> TokenizeRust(string code)
    {
        var spans = new List<ColoredSpan>();
        CollectLineComments(code, "//", spans);
        CollectBlockComments(code, "/*", "*/", spans);
        CollectStrings(code, spans, skipRanges: spans.Select(s => (s.Start, s.Start + s.Length)).ToList());
        CollectNumbers(code, spans);
        CollectKeywordsAndFunctions(code, RustKeywords, spans);
        return spans;
    }

    // ── SQL ──────────────────────────────────────────────────────────────────

    private static readonly HashSet<string> SqlKeywords =
    [
        "SELECT", "FROM", "WHERE", "INSERT", "INTO", "VALUES", "UPDATE",
        "SET", "DELETE", "CREATE", "TABLE", "DROP", "ALTER", "ADD",
        "INDEX", "VIEW", "JOIN", "LEFT", "RIGHT", "INNER", "OUTER",
        "ON", "AND", "OR", "NOT", "NULL", "IS", "IN", "LIKE",
        "ORDER", "BY", "GROUP", "HAVING", "LIMIT", "OFFSET",
        "DISTINCT", "COUNT", "SUM", "MAX", "MIN", "AVG",
        "AS", "CASE", "WHEN", "THEN", "ELSE", "END",
    ];

    private static List<ColoredSpan> TokenizeSql(string code)
    {
        var spans = new List<ColoredSpan>();
        CollectLineComments(code, "--", spans);
        CollectStrings(code, spans, skipRanges: spans.Select(s => (s.Start, s.Start + s.Length)).ToList());
        CollectNumbers(code, spans);
        // SQL keywords are case-insensitive — match and check against uppercase set
        var kwRegex = new Regex(@"\b[A-Za-z_][A-Za-z_0-9]*\b");
        foreach (Match m in kwRegex.Matches(code))
        {
            if (IsInSpans(spans, m.Index, m.Length)) continue;
            if (SqlKeywords.Contains(m.Value.ToUpperInvariant()))
                spans.Add(new ColoredSpan { Token = TokenType.Keyword, Start = m.Index, Length = m.Length });
        }
        return spans;
    }

    // ── YAML ─────────────────────────────────────────────────────────────────

    private static List<ColoredSpan> TokenizeYaml(string code)
    {
        var spans = new List<ColoredSpan>();
        // YAML comments
        CollectLineComments(code, "#", spans);

        // YAML keys: word followed by :
        var keyRegex = new Regex(@"^\s*([A-Za-z_][A-Za-z_0-9]*)\s*:", RegexOptions.Multiline);
        foreach (Match m in keyRegex.Matches(code))
        {
            var grp = m.Groups[1];
            if (!IsInSpans(spans, grp.Index, grp.Length))
                spans.Add(new ColoredSpan { Token = TokenType.Keyword, Start = grp.Index, Length = grp.Length });
        }

        CollectNumbers(code, spans);
        return spans;
    }

    // ── JS/TS ────────────────────────────────────────────────────────────────

    private static readonly HashSet<string> JsTsKeywords =
    [
        "break", "case", "catch", "class", "const", "continue", "debugger",
        "default", "delete", "do", "else", "export", "extends", "false",
        "finally", "for", "function", "if", "import", "in", "instanceof",
        "let", "new", "null", "return", "static", "super", "switch",
        "this", "throw", "true", "try", "typeof", "undefined", "var",
        "void", "while", "with", "yield", "async", "await",
        // TS extras
        "type", "interface", "enum", "namespace", "declare", "abstract",
        "as", "from", "of", "readonly", "implements", "public", "private",
        "protected", "override",
        // types
        "string", "number", "boolean", "any", "never", "unknown", "object",
    ];

    private static List<ColoredSpan> TokenizeJsTs(string code)
    {
        var spans = new List<ColoredSpan>();
        CollectLineComments(code, "//", spans);
        CollectBlockComments(code, "/*", "*/", spans);
        CollectStrings(code, spans, skipRanges: spans.Select(s => (s.Start, s.Start + s.Length)).ToList());
        CollectNumbers(code, spans);
        CollectKeywordsAndFunctions(code, JsTsKeywords, spans);
        return spans;
    }

    // ── Shared helpers ───────────────────────────────────────────────────────

    private static void CollectLineComments(string code, string marker, List<ColoredSpan> spans)
    {
        int pos = 0;
        while (pos < code.Length)
        {
            int idx = code.IndexOf(marker, pos, StringComparison.Ordinal);
            if (idx < 0) break;

            // Find end of line
            int end = code.IndexOf('\n', idx);
            if (end < 0) end = code.Length;
            else end++; // include newline in comment span

            spans.Add(new ColoredSpan { Token = TokenType.Comment, Start = idx, Length = end - idx });
            pos = end;
        }
    }

    private static void CollectBlockComments(string code, string open, string close, List<ColoredSpan> spans)
    {
        int pos = 0;
        while (pos < code.Length)
        {
            int start = code.IndexOf(open, pos, StringComparison.Ordinal);
            if (start < 0) break;
            int end = code.IndexOf(close, start + open.Length, StringComparison.Ordinal);
            if (end < 0)
            {
                spans.Add(new ColoredSpan { Token = TokenType.Comment, Start = start, Length = code.Length - start });
                break;
            }
            end += close.Length;
            spans.Add(new ColoredSpan { Token = TokenType.Comment, Start = start, Length = end - start });
            pos = end;
        }
    }

    private static void CollectStrings(string code, List<ColoredSpan> spans, List<(int Start, int End)> skipRanges)
    {
        int pos = 0;
        while (pos < code.Length)
        {
            char ch = code[pos];
            if (ch != '"' && ch != '\'') { pos++; continue; }

            // Check if inside a skip range
            if (IsInRanges(skipRanges, pos)) { pos++; continue; }

            char delim = ch;
            int start = pos;
            pos++;
            while (pos < code.Length)
            {
                if (code[pos] == '\\') { pos += 2; continue; }
                if (code[pos] == '\n') break; // unterminated
                if (code[pos] == delim) { pos++; break; }
                pos++;
            }
            spans.Add(new ColoredSpan { Token = TokenType.String, Start = start, Length = pos - start });
        }
    }

    private static void CollectNumbers(string code, List<ColoredSpan> spans)
    {
        var regex = new Regex(@"\b\d+(\.\d+)?([eE][+-]?\d+)?\b");
        foreach (Match m in regex.Matches(code))
        {
            if (!IsInSpans(spans, m.Index, m.Length))
                spans.Add(new ColoredSpan { Token = TokenType.Number, Start = m.Index, Length = m.Length });
        }
    }

    private static void CollectKeywordsAndFunctions(string code, HashSet<string> keywords, List<ColoredSpan> spans)
    {
        // Match identifiers; check if followed by ( for function detection
        var regex = new Regex(@"\b([A-Za-z_][A-Za-z_0-9]*)\b");
        foreach (Match m in regex.Matches(code))
        {
            if (IsInSpans(spans, m.Index, m.Length)) continue;

            // Check if it's a function call: identifier followed by (
            int after = m.Index + m.Length;
            while (after < code.Length && code[after] == ' ') after++;
            if (after < code.Length && code[after] == '(')
            {
                spans.Add(new ColoredSpan { Token = TokenType.Function, Start = m.Index, Length = m.Length });
                continue;
            }

            if (keywords.Contains(m.Value))
                spans.Add(new ColoredSpan { Token = TokenType.Keyword, Start = m.Index, Length = m.Length });
        }
    }

    private static bool IsInSpans(List<ColoredSpan> spans, int start, int length)
    {
        int end = start + length;
        foreach (var s in spans)
        {
            int sEnd = s.Start + s.Length;
            if (start < sEnd && end > s.Start) return true;
        }
        return false;
    }

    private static bool IsInRanges(List<(int Start, int End)> ranges, int pos)
    {
        foreach (var (s, e) in ranges)
            if (pos >= s && pos < e) return true;
        return false;
    }
}
