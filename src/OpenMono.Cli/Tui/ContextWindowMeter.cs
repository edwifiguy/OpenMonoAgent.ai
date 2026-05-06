namespace OpenMono.Tui;

public class ContextWindowMeter
{
    private readonly int _contextSize;

    public ContextWindowMeter(int contextSize = 128_000)
    {
        _contextSize = contextSize > 0 ? contextSize : 128_000;
    }

    public int PromptTokens { get; private set; }

    public int UsagePercent => _contextSize > 0 ? PromptTokens * 100 / _contextSize : 0;

    public int RemainingTokens => Math.Max(0, _contextSize - PromptTokens);

    public void Update(int promptTokens)
    {
        PromptTokens = promptTokens;
    }

    public string FormatRemaining()
    {
        var remaining = RemainingTokens;
        return remaining >= 1000
            ? $"{remaining / 1000}K remaining"
            : $"{remaining} remaining";
    }

    public string FormatProgressBar(int width)
    {
        var filled = Math.Min(width, width * UsagePercent / 100);
        return new string('█', filled) + new string(' ', width - filled);
    }
}
