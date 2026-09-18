using KillerBench.Engine;

internal static class ServiceTransitionChecks
{
    internal static void Run()
    {
        var pending = Inventory(Service("alpha", "Alpha", "Start Pending"), Service("beta", "Beta", "Running"));
        var same = Inventory(Service("alpha", "Alpha", "Start Pending"), Service("beta", "Beta", "Running"));
        var persistent = ServiceTransitionDiagnostics.Check(pending, same);
        Require(persistent.State == CheckState.Attention && persistent.Repair is null && persistent.Evidence == same.Evidence &&
            persistent.Detail.Contains("Alpha [alpha]: Start Pending", StringComparison.Ordinal),
            "Exact repeated transitional state requests review without selecting a repair");

        foreach (string state in new[] { "Start Pending", "Stop Pending", "Continue Pending", "Pause Pending" })
        {
            var first = Inventory(Service("fixture", "Fixture", state));
            var settled = ServiceTransitionDiagnostics.Check(first, Inventory(Service("fixture", "Fixture", "Running")));
            Require(settled.State == CheckState.Information && settled.Repair is null && settled.Detail.Contains("no persistent transition", StringComparison.OrdinalIgnoreCase),
                "A changed transition is not reported as persistent");
        }

        var ordinary = Inventory(Service("fixture", "Fixture", "Stopped"));
        int calls = 0;
        var withoutSecondRead = ServiceTransitionDiagnostics.CheckAsync(ordinary, collect: _ =>
        {
            calls++;
            return Task.FromResult(ordinary);
        }, observationInterval: TimeSpan.FromMilliseconds(1)).GetAwaiter().GetResult();
        Require(withoutSecondRead.State == CheckState.Information && calls == 0,
            "A stable initial inventory does not dispatch an unnecessary second collection");
        var isolatedFailure = ServiceTransitionDiagnostics.CheckAsync(pending,
            collect: _ => throw new UnauthorizedAccessException(), observationInterval: TimeSpan.FromMilliseconds(1)).GetAwaiter().GetResult();
        Require(isolatedFailure.State == CheckState.Unknown && isolatedFailure.Repair is null,
            "A failed second collection stays unknown without escaping the service readiness scope");

        var changedIdentity = ServiceTransitionDiagnostics.Check(pending, Inventory(Service("gamma", "Gamma", "Start Pending")));
        Require(changedIdentity.State == CheckState.Information && !changedIdentity.Detail.Contains("Alpha [alpha]", StringComparison.Ordinal),
            "A different transitional identity does not satisfy exact persistence");
        var incomplete = pending with { State = CheckState.Unknown };
        Require(ServiceTransitionDiagnostics.Check(incomplete, same).State == CheckState.Unknown &&
            ServiceTransitionDiagnostics.Check(pending, same with { Evidence = same.Evidence! with { Truncated = true } }).State == CheckState.Unknown,
            "Incomplete snapshots cannot establish a persistent transition");
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        try { ServiceTransitionDiagnostics.Check(pending, same, canceled.Token); throw new Exception("Transition cancellation ignored"); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: exact repeated service transitions remain bounded, diagnostic-only and read only; fixtures only.");
    }

    private static WorkflowCheck Inventory(params object[] services)
    {
        var evidence = new ToolResult(0, System.Text.Json.JsonSerializer.Serialize(new { complete = true, services }), "", false);
        return ServiceInventoryDiagnostics.Interpret(evidence);
    }

    private static object Service(string name, string displayName, string state) => new
    {
        name,
        displayName,
        state,
        startMode = "Manual",
        delayedAutoStart = false,
        serviceType = "Own Process"
    };

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
