namespace KillerBench.Engine;

public static class WindowsActivationOperationalCorrelation
{
    public static WorkflowCheck Check(WorkflowCheck licensing, WorkflowCheck history,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(licensing);
        ArgumentNullException.ThrowIfNull(history);
        token.ThrowIfCancellationRequested();
        const string name = "Windows activation operational conclusion";
        const string boundary = " Provider status and sanitized history do not verify subscription entitlement, activation-server reachability, token-store integrity, future validity or root cause. No activation, product-key, token, license, account, service or policy action is selected.";
        WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary);
        if (licensing.Name != "Windows activation" || history.Name != "Recent Software Protection events")
            return Unknown("Activation evidence carried an unexpected identity.");
        if (licensing.EffectiveState == CheckState.Unknown || history.EffectiveState == CheckState.Unknown)
            return Unknown("Current licensing or Software Protection history evidence was incomplete. Recollect both sources before drawing a combined conclusion.");
        if (licensing.EffectiveState is CheckState.Attention or CheckState.Repairable)
            return new(name, CheckState.Attention,
                "Current Windows licensing evidence requires review. Historical Software Protection records remain context and do not authorize activation or license changes." + boundary);
        if (history.EffectiveState is CheckState.Attention or CheckState.Repairable)
            return new(name, CheckState.Information,
                "Current Windows licensing evidence reports Licensed, while retained Software Protection warnings or errors require narrower historical review. The records are not presented as a current activation failure." + boundary);
        return new(name, CheckState.Clear,
            "Current Windows licensing evidence reports Licensed and the retained Software Protection history contains no warning, error or critical records. This is a bounded current-state conclusion." + boundary);
    }
}
