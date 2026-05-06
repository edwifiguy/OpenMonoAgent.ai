using System.Diagnostics;

namespace OpenMono.Tui;

public class StreamingMetrics
{
    private readonly Stopwatch _stopwatch = new();

    public bool IsStreaming { get; private set; }
    public double TokensPerSecond { get; private set; }
    public int TotalCompletionTokens { get; private set; }

    public void OnStreamStart()
    {
        IsStreaming = true;
        TotalCompletionTokens = 0;
        TokensPerSecond = 0;
        _stopwatch.Restart();
    }

    public void OnTokenReceived(int totalTokens)
    {
        TotalCompletionTokens = totalTokens;
        var elapsed = _stopwatch.Elapsed.TotalSeconds;
        if (elapsed > 0)
            TokensPerSecond = totalTokens / elapsed;
    }

    public void OnStreamEnd()
    {
        IsStreaming = false;
        _stopwatch.Stop();
    }
}
