using KillerBench.Engine;

internal static class InstallationEnvironmentCorrelationChecks
{
    internal static void Run()
    {
        var clear = Check();
        Require(clear is { State: CheckState.Information, Repair: null } &&
            clear.Detail.Contains("0 need attention and 0 incomplete", StringComparison.Ordinal),
            "Complete installation environment evidence produces a bounded informational result");

        var current = Check(cache: Group("MSI cache: Fixture", CheckState.Attention));
        Require(current is { State: CheckState.Attention, Repair: null } &&
            current.Detail.Contains("Current evidence: 1 need attention", StringComparison.Ordinal),
            "Current cache attention requests review without selecting repair");

        var historical = Check(appx: Fixed("Recent AppX deployment events", CheckState.Attention));
        Require(historical is { State: CheckState.Information, Repair: null } &&
            historical.Detail.Contains("Current environment evidence did not establish", StringComparison.Ordinal),
            "Historical deployment attention alone does not establish a current installation problem");

        var incomplete = Check(logging: Fixed("Installer logging policy", CheckState.Unknown),
            store: Fixed("Recent Microsoft Store installation events", CheckState.Attention));
        Require(incomplete is { State: CheckState.Unknown, Repair: null },
            "Incomplete current evidence takes precedence over historical attention");
        Require(Check(cache: []).State == CheckState.Unknown && Check(sources: Group("Other source")).State == CheckState.Unknown,
            "Missing groups and unexpected identities fail closed");

        try
        {
            InstallationEnvironmentCorrelation.Check(Fixed("Installer readiness"), Fixed("Installer security and patch policy"),
                Fixed("Installer logging policy"), Group("MSI cached packages"), Group("MSI source metadata"),
                Fixed("Installer attempt correlation"), Fixed("Recent AppX deployment events"),
                Fixed("Recent Microsoft Store installation events"), new CancellationToken(true));
            throw new InvalidOperationException("Canceled installation environment correlation completed unexpectedly");
        }
        catch (OperationCanceledException) { }

        Console.WriteLine("PASS: installation environment correlation combines policy, cache, source and retained histories without selecting repair.");
    }

    private static WorkflowCheck Check(WorkflowCheck? readiness = null, WorkflowCheck? security = null,
        WorkflowCheck? logging = null, IReadOnlyList<WorkflowCheck>? cache = null,
        IReadOnlyList<WorkflowCheck>? sources = null, WorkflowCheck? outcome = null,
        WorkflowCheck? appx = null, WorkflowCheck? store = null) =>
        InstallationEnvironmentCorrelation.Check(readiness ?? Fixed("Installer readiness"),
            security ?? Fixed("Installer security and patch policy"), logging ?? Fixed("Installer logging policy"),
            cache ?? Group("MSI cached packages"), sources ?? Group("MSI source metadata"),
            outcome ?? Fixed("Installer attempt correlation"), appx ?? Fixed("Recent AppX deployment events"),
            store ?? Fixed("Recent Microsoft Store installation events"));

    private static WorkflowCheck Fixed(string name, CheckState state = CheckState.Information) => new(name, state, "fixture");
    private static WorkflowCheck[] Group(string name, CheckState state = CheckState.Information) => [new(name, state, "fixture")];
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
