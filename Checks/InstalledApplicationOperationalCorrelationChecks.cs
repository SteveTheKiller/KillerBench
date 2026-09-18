using KillerBench.Engine;

internal static class InstalledApplicationOperationalCorrelationChecks
{
    internal static void Run()
    {
        Require(Invoke() is { State: CheckState.Information, Repair: null }, "Complete installed application evidence remains informational");
        Require(Invoke(restart: new("Restart", CheckState.Attention, "fixture")).State == CheckState.Attention,
            "Current restart attention remains visible");
        Require(Invoke(registrations: new("Application registrations", CheckState.Unknown, "fixture")).State == CheckState.Unknown,
            "Incomplete registration evidence takes precedence");
        Require(Invoke(stability: Info("Unexpected")).State == CheckState.Unknown, "Unexpected identity fails closed");
        try { Invoke(token: new CancellationToken(true)); throw new Exception("Installed application correlation cancellation ignored"); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: installed application operational correlation combines scoped registrations, bounded stability history and restart evidence without selecting repair.");
    }

    private static WorkflowCheck Invoke(WorkflowCheck? registrations = null, WorkflowCheck? stability = null,
        WorkflowCheck? restart = null, CancellationToken token = default) => InstalledApplicationOperationalCorrelation.Check(
            registrations ?? Info("Application registrations"), stability ?? Info("Application stability history conclusion"),
            restart ?? Info("Restart"), token);

    private static WorkflowCheck Info(string name) => new(name, CheckState.Information, "fixture");
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
