using KillerBench.Engine;

internal static class AppPackageOperationalCorrelationChecks
{
    internal static void Run()
    {
        Require(Invoke() is { State: CheckState.Information, Repair: null }, "Complete package evidence remains informational");
        Require(Invoke(registered: Attention("Registered app package summary")).State == CheckState.Attention,
            "Current package attention remains visible");
        var historical = Invoke(deployment: Attention("Recent AppX deployment events"));
        Require(historical.State == CheckState.Information && historical.Detail.Contains("Historical failures direct narrower"),
            "Historical deployment failures do not become a current fault");
        Require(Invoke(store: Unknown("Recent Microsoft Store installation events")).State == CheckState.Unknown,
            "Incomplete history takes precedence");
        Require(AppPackageOperationalCorrelation.Check(Info("Registered app package summary"), null,
            Info("Recent AppX deployment events"), Info("Recent Microsoft Store installation events"),
            Info("Restart")).State == CheckState.Information,
            "Provisioned inventory remains an explicit optional scope");
        Require(Invoke(restart: Info("Unexpected")).State == CheckState.Unknown,
            "Unexpected identities fail closed");
        try { Invoke(token: new CancellationToken(true)); throw new Exception("Package correlation cancellation ignored"); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: app package operational correlation combines current registration, optional provisioning, deployment history and restart evidence without selecting repair.");
    }

    private static WorkflowCheck Invoke(WorkflowCheck? registered = null, WorkflowCheck? provisioned = default,
        WorkflowCheck? deployment = null, WorkflowCheck? store = null, WorkflowCheck? restart = null,
        CancellationToken token = default) => AppPackageOperationalCorrelation.Check(
            registered ?? Info("Registered app package summary"), provisioned ?? Info("Provisioned app packages"),
            deployment ?? Info("Recent AppX deployment events"), store ?? Info("Recent Microsoft Store installation events"),
            restart ?? Info("Restart"), token);

    private static WorkflowCheck Info(string name) => new(name, CheckState.Information, "fixture");
    private static WorkflowCheck Attention(string name) => new(name, CheckState.Attention, "fixture");
    private static WorkflowCheck Unknown(string name) => new(name, CheckState.Unknown, "fixture");
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
