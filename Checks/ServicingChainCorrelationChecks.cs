using KillerBench.Engine;

internal static class ServicingChainCorrelationChecks
{
    internal static void Run()
    {
        var clear = ServicingChainCorrelation.Check(States(), Dll(), Histories());
        Require(clear is { State: CheckState.Clear, Repair: null } &&
            clear.Detail.Contains("six current service states, one registered-DLL result and six exact service histories", StringComparison.Ordinal) &&
            clear.Detail.Contains("No issue was established within this bounded servicing-chain evidence", StringComparison.Ordinal),
            "Complete informational servicing-chain evidence produces a bounded clear correlation without selecting repair");

        var stopped = States();
        stopped[5] = stopped[5] with { State = CheckState.Attention };
        var attention = ServicingChainCorrelation.Check(stopped, Dll(CheckState.Attention),
            Histories("BITS service failure history", CheckState.Attention));
        Require(attention is { State: CheckState.Attention, Repair: null } &&
            attention.Detail.Contains("Current state: 1 need attention", StringComparison.Ordinal) &&
            attention.Detail.Contains("Registered DLLs: review", StringComparison.Ordinal) &&
            attention.Detail.Contains("History: 1 need attention", StringComparison.Ordinal) &&
            attention.Detail.Contains("Review the exact underlying results together", StringComparison.Ordinal),
            "Current state, DLL and exact history findings are correlated into one review outcome without choosing an action");

        var incomplete = ServicingChainCorrelation.Check(States(0, CheckState.Unknown), Dll(), Histories());
        Require(incomplete is { State: CheckState.Unknown, Repair: null } &&
            incomplete.Detail.Contains("cannot be fully correlated", StringComparison.Ordinal),
            "Incomplete current evidence takes precedence over attention and cannot select repair");

        var malformed = ServicingChainCorrelation.Check([.. States().Take(5)], Dll(), Histories());
        var duplicateHistory = Histories();
        duplicateHistory[5] = duplicateHistory[0];
        var duplicate = ServicingChainCorrelation.Check(States(), Dll(), duplicateHistory);
        Require(malformed.State == CheckState.Unknown && duplicate.State == CheckState.Unknown &&
            malformed.Repair is null && duplicate.Repair is null,
            "Missing and duplicate exact identities fail closed without selecting repair");

        try
        {
            ServicingChainCorrelation.Check(States(), Dll(), Histories(), new CancellationToken(true));
            throw new InvalidOperationException("Canceled servicing-chain correlation completed unexpectedly");
        }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: Windows Update servicing-chain state, registered DLL and exact failure history correlation is deterministic and read-only.");
    }

    private static WorkflowCheck[] States(int changed = -1, CheckState state = CheckState.Information) =>
        [.. ServicingServiceStateDiagnostics.Services.Select((service, index) => new WorkflowCheck(service.DisplayName + " state",
            index == changed ? state : CheckState.Information, "fixture"))];

    private static WorkflowCheck Dll(CheckState state = CheckState.Information) =>
        new("Windows Update service DLLs", state, "fixture");

    private static WorkflowCheck[] Histories(string? changed = null, CheckState state = CheckState.Information) =>
    [
        History("Windows Update service failure history", changed, state),
        History("BITS service failure history", changed, state),
        History("Cryptographic Services failure history", changed, state),
        History("Windows Modules Installer failure history", changed, state),
        History("Windows Update Medic failure history", changed, state),
        History("Update Orchestrator failure history", changed, state)
    ];

    private static WorkflowCheck History(string name, string? changed, CheckState state) =>
        new(name, name == changed ? state : CheckState.Information, "fixture");

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
