using KillerBench.Engine;

internal static class NetworkSetupOperationalCorrelationChecks
{
    internal static void Run()
    {
        Require(Check().State == CheckState.Information, "Complete clear Network Setup evidence remains informational");
        Require(Check(bindings: CheckState.Attention).State == CheckState.Attention, "Current adapter binding concern requests review");
        Require(Check(service: CheckState.Attention).State == CheckState.Attention, "Current Network Setup service concern requests review");
        Require(Check(history: CheckState.Attention).State == CheckState.Information, "Historical Network Setup failures remain informational");
        Require(Check(bindings: CheckState.Unknown, service: CheckState.Attention).State == CheckState.Unknown, "Incomplete binding evidence takes precedence");
        Require(NetworkSetupOperationalCorrelation.Check([.. Inputs().Append(new("Network adapter bindings", CheckState.Information, "duplicate"))]).State == CheckState.Unknown,
            "Duplicate Network Setup evidence fails closed");
        Require(NetworkSetupOperationalCorrelation.Check([.. Inputs().Where(check => check.Name != "Focused service readiness")]).State == CheckState.Unknown,
            "Missing Network Setup evidence fails closed");
        try { NetworkSetupOperationalCorrelation.Check(Inputs(), new CancellationToken(true)); throw new Exception("Canceled Network Setup correlation completed"); }
        catch (OperationCanceledException) { }
        var attention = Check(bindings: CheckState.Attention);
        Require(attention.Repair is null && attention.Detail.Contains("cannot authorize service recovery or a setup change", StringComparison.Ordinal) &&
            attention.Detail.Contains("does not establish adapter availability", StringComparison.Ordinal),
            "Network Setup conclusion preserves driver, binding and service boundaries");
        Console.WriteLine("PASS: adapter bindings, exact history and Network Setup service readiness correlate without selecting changes.");
    }

    private static WorkflowCheck Check(CheckState bindings = CheckState.Information, CheckState history = CheckState.Information,
        CheckState service = CheckState.Information) => NetworkSetupOperationalCorrelation.Check(Inputs(bindings, history, service));
    private static IReadOnlyList<WorkflowCheck> Inputs(CheckState bindings = CheckState.Information,
        CheckState history = CheckState.Information, CheckState service = CheckState.Information) =>
    [
        new("Network adapter bindings", bindings, "fixture"),
        new("Network Setup service failure history", history, "fixture"),
        new("Focused service readiness", service, "fixture")
    ];
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
