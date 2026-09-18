using KillerBench.Engine;

internal static class ApplicationDeploymentOperationalCorrelationChecks
{
    internal static void Run()
    {
        var clear = Invoke();
        Require(clear is { State: CheckState.Information, Repair: null } && clear.Detail.Contains("0 need attention and 0 incomplete"), "Clear deployment evidence stays bounded");
        var attention = Invoke(history: [new("Recent application errors", CheckState.Attention, "fixture")]);
        Require(attention is { State: CheckState.Attention, Repair: null }, "Deployment attention remains review-only");
        var incomplete = Invoke(installer: [new("Windows Installer operational correlation", CheckState.Unknown, "fixture")]);
        Require(incomplete.State == CheckState.Unknown, "Incomplete deployment evidence takes precedence");
        Require(Invoke(installer: []).State == CheckState.Unknown && Invoke(restart: [Info("Restart"), Info("Restart")]).State == CheckState.Unknown,
            "Missing and duplicate deployment identities fail closed");
        try { Invoke(token: new CancellationToken(true)); throw new Exception("Deployment cancellation ignored"); } catch (OperationCanceledException) { }
        Console.WriteLine("PASS: application deployment operational correlation combines registrations, Installer, history and restart evidence without selecting repair.");
    }
    private static WorkflowCheck Invoke(IReadOnlyList<WorkflowCheck>? installer = null, IReadOnlyList<WorkflowCheck>? history = null,
        IReadOnlyList<WorkflowCheck>? restart = null, CancellationToken token = default) =>
        ApplicationDeploymentOperationalCorrelation.Check(Info("Registered app package summary"), Info("Provisioned app packages"),
            Info("Application registrations"), Info("Application registrations"), installer ?? [Info("Windows Installer operational correlation")],
            history ?? [Info("Recent application errors")], restart ?? [Info("Restart")], token);
    private static WorkflowCheck Info(string name) => new(name, CheckState.Information, "fixture");
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
