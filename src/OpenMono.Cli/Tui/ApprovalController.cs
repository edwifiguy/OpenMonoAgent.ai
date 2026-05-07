using OpenMono.Session;

namespace OpenMono.Tui;

public class ApprovalController
{
    private bool _manualApprovalMode;
    private bool _allowAllActive;

    public bool ManualApprovalMode => _manualApprovalMode;

    public event EventHandler<bool>? OnApprovalModeChanged;

    public Func<ToolCall, CancellationToken, Task<ApprovalDecision>>? RequestApprovalFunc { get; set; }

    public void ToggleApprovalMode()
    {
        _manualApprovalMode = !_manualApprovalMode;
        OnApprovalModeChanged?.Invoke(this, _manualApprovalMode);
    }

    public async Task<ApprovalDecision> CheckApprovalAsync(ToolCall call, CancellationToken ct)
    {
        if (!_manualApprovalMode)
            return ApprovalDecision.Allow;

        if (_allowAllActive)
            return ApprovalDecision.Allow;

        if (RequestApprovalFunc == null)
            return ApprovalDecision.Allow;

        var decision = await RequestApprovalFunc(call, ct);

        if (decision == ApprovalDecision.AllowAll)
        {
            _allowAllActive = true;
            return ApprovalDecision.Allow;
        }

        return decision;
    }

    public void ResetTurn()
    {
        _allowAllActive = false;
    }
}
