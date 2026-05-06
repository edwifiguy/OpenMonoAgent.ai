namespace OpenMono.Tui;

public class PauseController
{
    private volatile bool _isPaused;
    private TaskCompletionSource _pauseTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public bool IsPaused => _isPaused;

    public event EventHandler<bool>? OnPauseStateChanged;

    public void TogglePause()
    {
        if (_isPaused)
        {
            _pauseTcs.TrySetResult();
            _isPaused = false;
            OnPauseStateChanged?.Invoke(this, false);
        }
        else
        {
            _pauseTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _isPaused = true;
            OnPauseStateChanged?.Invoke(this, true);
        }
    }

    public async Task WaitIfPausedAsync(CancellationToken ct)
    {
        if (!_isPaused) return;

        var tcs = _pauseTcs;
        using var reg = ct.Register(() => tcs.TrySetCanceled(ct));
        await tcs.Task;
    }
}
