using KillerBench.Engine;

internal static class InstallerOperationalCorrelationChecks
{
    internal static void Run()
    {
        var clear = InstallerOperationalCorrelation.Check(Service(), EngineFiles(), Registration(), Activity(),
            Pending(), InstallerHistory(), ServiceHistory());
        Require(clear is { State: CheckState.Clear, Repair: null } &&
            clear.Detail.Contains("current Windows Installer service state, engine files, service registration", StringComparison.Ordinal) &&
            clear.Detail.Contains("No issue was established within this bounded Installer evidence", StringComparison.Ordinal),
            "Complete current and historical Installer evidence produces a bounded clear correlation without selecting repair");

        var attention = InstallerOperationalCorrelation.Check(Service(CheckState.Attention),
            EngineFiles(CheckState.Attention), Registration(), Activity(CheckState.Attention), Pending(),
            InstallerHistory(CheckState.Attention), ServiceHistory(CheckState.Attention));
        Require(attention is { State: CheckState.Attention, Repair: null } &&
            attention.Detail.Contains("Current evidence: 3 need attention", StringComparison.Ordinal) &&
            attention.Detail.Contains("History: 2 need attention", StringComparison.Ordinal) &&
            attention.Detail.Contains("before selecting a product, registration, service or restart action", StringComparison.Ordinal),
            "Current service, engine, activity and history findings produce one review outcome without choosing an action");

        var incomplete = InstallerOperationalCorrelation.Check(Service(), EngineFiles(), Registration(CheckState.Unknown),
            Activity(), Pending(), InstallerHistory(CheckState.Attention), ServiceHistory());
        Require(incomplete is { State: CheckState.Unknown, Repair: null } &&
            incomplete.Detail.Contains("cannot be fully correlated", StringComparison.Ordinal),
            "Incomplete current Installer evidence takes precedence over historical attention");

        var wrongIdentity = InstallerOperationalCorrelation.Check(Service() with { Name = "Other service" },
            EngineFiles(), Registration(), Activity(), Pending(), InstallerHistory(), ServiceHistory());
        var wrongHistory = InstallerOperationalCorrelation.Check(Service(), EngineFiles(), Registration(), Activity(),
            Pending(), InstallerHistory() with { Name = "Other history" }, ServiceHistory());
        Require(wrongIdentity.State == CheckState.Unknown && wrongHistory.State == CheckState.Unknown &&
            wrongIdentity.Repair is null && wrongHistory.Repair is null,
            "Mismatched current and historical identities fail closed without selecting repair");

        try
        {
            InstallerOperationalCorrelation.Check(Service(), EngineFiles(), Registration(), Activity(), Pending(),
                InstallerHistory(), ServiceHistory(), new CancellationToken(true));
            throw new InvalidOperationException("Canceled Installer correlation completed unexpectedly");
        }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: Windows Installer service, engine, registration, activity, pending-operation and history correlation is deterministic and read-only.");
    }

    private static WorkflowCheck Service(CheckState state = CheckState.Information) =>
        new(RepairKind.WindowsInstaller.ToString(), state, "fixture");

    private static WorkflowCheck EngineFiles(CheckState state = CheckState.Information) =>
        new("Windows Installer engine files", state, "fixture");

    private static WorkflowCheck Registration(CheckState state = CheckState.Information) =>
        new("Windows Installer service registration", state, "fixture");

    private static WorkflowCheck Activity(CheckState state = CheckState.Information) =>
        new("Installer activity", state, "fixture");

    private static WorkflowCheck Pending(CheckState state = CheckState.Information) =>
        new("Pending file operations", state, "fixture");

    private static WorkflowCheck InstallerHistory(CheckState state = CheckState.Information) =>
        new("Recent installer events", state, "fixture");

    private static WorkflowCheck ServiceHistory(CheckState state = CheckState.Information) =>
        new("Windows Installer service failure history", state, "fixture");

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
