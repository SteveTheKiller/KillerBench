namespace KillerBench.Engine.Services;

public static class DeliveryOptimizationDiagnostics
{
    public static RepairStep Step() => new("sc.exe", ["query", "DoSvc"], true);

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        return Interpret(await BoundedTool.RunAsync(Step(), TimeSpan.FromSeconds(15), token: token).ConfigureAwait(false));
    }

    public static WorkflowCheck Interpret(ToolResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (!ServiceRecovery.MatchesService(result, "DoSvc") || ServiceRecovery.State(result) is not int state)
            return new("Delivery Optimization", CheckState.Unknown,
                "Delivery Optimization state is unavailable or incomplete. No service or update setting changed.", Evidence: result);
        return state switch
        {
            1 => new("Delivery Optimization", CheckState.Information,
                "Delivery Optimization is stopped. A stopped demand-start service can be normal when idle; no start was selected.", Evidence: result),
            4 => new("Delivery Optimization", CheckState.Information,
                "Delivery Optimization is running. This does not establish transfer activity, cache health or policy effectiveness.", Evidence: result),
            7 => new("Delivery Optimization", CheckState.Repairable,
                "Delivery Optimization reports Paused. The selected recovery can continue this exact service without changing its startup type, cache, peer, bandwidth or managed policy settings.", RepairKind.DeliveryOptimization, result),
            _ => new("Delivery Optimization", CheckState.Information,
                "Delivery Optimization reports a transitional service state. Recheck after current update activity finishes; no change was selected.", Evidence: result)
        };
    }

    public static WorkflowCheck AsReadOnly(WorkflowCheck check)
    {
        ArgumentNullException.ThrowIfNull(check);
        return check.State == CheckState.Repairable
            ? check with
            {
                State = CheckState.Attention,
                Repair = null,
                Detail = "Delivery Optimization reports Paused. This read-only inspection did not continue it. The focused recovery remains subject to administrator, restart and fresh exact-state checks."
            }
            : check with { Repair = null };
    }
}
